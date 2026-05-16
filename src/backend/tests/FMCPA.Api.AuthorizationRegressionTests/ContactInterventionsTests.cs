using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FMCPA.Domain.Entities.Security;
using FMCPA.Domain.Entities.Shared;
using FMCPA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FMCPA.Api.AuthorizationRegressionTests;

public sealed class ContactInterventionsTests : IClassFixture<AuthorizationRegressionWebApplicationFactory>
{
    private readonly AuthorizationRegressionWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ContactInterventionsTests(AuthorizationRegressionWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_contact_intervention_persists_creator_dates_and_does_not_create_participation()
    {
        var contactId = await SeedContactAsync();
        var operatorToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.OperatorUserName,
            AuthorizationRegressionWebApplicationFactory.OperatorPassword);
        var occurredUtc = new DateTimeOffset(2026, 5, 16, 18, 30, 0, TimeSpan.Zero);
        var participationCountBefore = await CountContactParticipationsAsync();
        var beforeCreateUtc = DateTimeOffset.UtcNow;

        using var response = await SendPostAsync(
            $"/api/contacts/{contactId}/interventions",
            operatorToken,
            new
            {
                moduleKey = "markets",
                originType = "market_issue",
                originId = "issue-regression-1",
                originDisplayName = "Incidencia Mercado Juarez",
                subject = "Desbloqueo de seguimiento",
                helpType = "unblocking",
                outcome = "useful",
                notes = "Contacto ayudo a desbloquear seguimiento con el area X.",
                occurredUtc
            });
        var body = await response.Content.ReadFromJsonAsync<ContactInterventionTestResponse>();
        var afterCreateUtc = DateTimeOffset.UtcNow;
        var participationCountAfter = await CountContactParticipationsAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.Equal(contactId, body.ContactId);
        Assert.Equal("MARKETS", body.ModuleKey);
        Assert.Equal("MARKET_ISSUE", body.OriginType);
        Assert.Equal("issue-regression-1", body.OriginId);
        Assert.Equal("UNBLOCKING", body.HelpType);
        Assert.Equal("USEFUL", body.Outcome);
        Assert.Equal(occurredUtc, body.OccurredUtc);
        Assert.NotEqual(Guid.Empty, body.CreatedByUserId);
        Assert.Equal("Operator regression", body.CreatedByUserName);
        Assert.InRange(body.CreatedUtc, beforeCreateUtc.AddSeconds(-1), afterCreateUtc.AddSeconds(1));
        Assert.Equal(participationCountBefore, participationCountAfter);
    }

    [Fact]
    public async Task List_by_contact_returns_newest_first_and_filters_modules_without_read_access()
    {
        var contactId = await SeedContactAsync();
        var creatorId = await ResolveUserIdAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName);
        var oldOccurredUtc = new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
        var newOccurredUtc = new DateTimeOffset(2026, 5, 16, 12, 0, 0, TimeSpan.Zero);
        await SeedInterventionAsync(contactId, creatorId, "MARKETS", "MARKET_ISSUE", "issue-old", oldOccurredUtc, "Seguimiento anterior");
        await SeedInterventionAsync(contactId, creatorId, "DOCUMENTS", "STORED_DOCUMENT", "document-sensitive", newOccurredUtc.AddHours(1), "Nota documental sensible");
        await SeedInterventionAsync(contactId, creatorId, "MARKETS", "MARKET_ISSUE", "issue-new", newOccurredUtc, "Seguimiento reciente");
        var readOnlyToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName,
            AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);

        using var response = await SendGetAsync($"/api/contacts/{contactId}/interventions?limit=10", readOnlyToken);
        var body = await response.Content.ReadFromJsonAsync<List<ContactInterventionTestResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(["issue-new", "issue-old"], body!.Select(item => item.OriginId).ToArray());
        Assert.DoesNotContain(body, item => item.ModuleKey == "DOCUMENTS");
    }

    [Fact]
    public async Task List_by_origin_filters_by_module_origin_type_and_origin_id()
    {
        var matchingContactId = await SeedContactAsync("Contacto origen coincidente");
        var otherContactId = await SeedContactAsync("Contacto otro origen");
        var creatorId = await ResolveUserIdAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName);
        await SeedInterventionAsync(matchingContactId, creatorId, "MARKETS", "MARKET_ISSUE", "issue-origin", DateTimeOffset.UtcNow.AddMinutes(-1), "Coincidente");
        await SeedInterventionAsync(otherContactId, creatorId, "MARKETS", "MARKET_ISSUE", "issue-other", DateTimeOffset.UtcNow, "No coincide");
        var adminToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.AdminUserName,
            AuthorizationRegressionWebApplicationFactory.AdminPassword);

        using var response = await SendGetAsync(
            "/api/contact-interventions?moduleKey=MARKETS&originType=MARKET_ISSUE&originId=issue-origin",
            adminToken);
        var body = await response.Content.ReadFromJsonAsync<List<ContactInterventionTestResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = Assert.Single(body!);
        Assert.Equal(matchingContactId, item.ContactId);
        Assert.Equal("issue-origin", item.OriginId);
    }

    [Fact]
    public async Task Create_validates_contact_module_origin_help_outcome_and_notes()
    {
        var contactId = await SeedContactAsync();
        var operatorToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.OperatorUserName,
            AuthorizationRegressionWebApplicationFactory.OperatorPassword);

        using var invalidRequest = await SendPostAsync(
            $"/api/contacts/{contactId}/interventions",
            operatorToken,
            new
            {
                moduleKey = "INVALID",
                originType = "INVALID",
                originId = "",
                originDisplayName = "",
                subject = "",
                helpType = "INVALID",
                outcome = "INVALID",
                notes = new string('x', 2001)
            });
        var invalidBody = await invalidRequest.Content.ReadAsStringAsync();

        using var missingContact = await SendPostAsync(
            $"/api/contacts/{Guid.NewGuid()}/interventions",
            operatorToken,
            new
            {
                moduleKey = "MARKETS",
                originType = "MARKET_ISSUE",
                originId = "issue-missing-contact",
                originDisplayName = "Incidencia sin contacto",
                subject = "Validacion de contacto inexistente",
                helpType = "INFORMATION",
                outcome = "PENDING",
                notes = "Solicitud valida con contacto inexistente."
            });

        Assert.Equal(HttpStatusCode.BadRequest, invalidRequest.StatusCode);
        Assert.Contains("moduleKey", invalidBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("originId", invalidBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("originDisplayName", invalidBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("subject", invalidBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("helpType", invalidBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("outcome", invalidBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("notes", invalidBody, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.NotFound, missingContact.StatusCode);
    }

    [Fact]
    public async Task Authorization_requires_contacts_read_and_origin_module_permissions()
    {
        var contactId = await SeedContactAsync();
        var readOnlyToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName,
            AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);

        using var anonymousList = await _client.GetAsync($"/api/contacts/{contactId}/interventions");
        using var readOnlyCreate = await SendPostAsync(
            $"/api/contacts/{contactId}/interventions",
            readOnlyToken,
            new
            {
                moduleKey = "MARKETS",
                originType = "MARKET_ISSUE",
                originId = "issue-authz",
                originDisplayName = "Incidencia autorizacion",
                subject = "Intento sin escritura",
                helpType = "INFORMATION",
                outcome = "PENDING",
                notes = "No debe crearse."
            });
        using var readOnlyDocumentsOrigin = await SendGetAsync(
            "/api/contact-interventions?moduleKey=DOCUMENTS&originType=STORED_DOCUMENT&originId=document-authz",
            readOnlyToken);

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousList.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, readOnlyCreate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, readOnlyDocumentsOrigin.StatusCode);
    }

    private async Task<Guid> SeedContactAsync(string? name = null)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var contactType = await dbContext.ContactTypes.FirstOrDefaultAsync(type => type.Code == "EXTERNAL");
        if (contactType is null)
        {
            contactType = new ContactType("EXTERNAL", "Externo", "Tipo externo para pruebas", 1);
            dbContext.ContactTypes.Add(contactType);
            await dbContext.SaveChangesAsync();
        }

        var contact = new Contact(
            name ?? $"Contacto intervencion {Guid.NewGuid():N}"[..32],
            contactType.Id,
            "Organizacion de prueba",
            "Enlace",
            "5512345678",
            "5512345678",
            "contacto.prueba@example.com",
            "Contacto creado para pruebas de intervenciones.");

        dbContext.Contacts.Add(contact);
        await dbContext.SaveChangesAsync();
        return contact.Id;
    }

    private async Task SeedInterventionAsync(
        Guid contactId,
        Guid creatorId,
        string moduleKey,
        string originType,
        string originId,
        DateTimeOffset occurredUtc,
        string subject)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        dbContext.ContactInterventions.Add(
            new ContactIntervention(
                contactId,
                moduleKey,
                originType,
                originId,
                $"{originType} {originId}",
                subject,
                ContactInterventionHelpTypes.Information,
                ContactInterventionOutcomes.Useful,
                $"Notas {originId}",
                occurredUtc,
                creatorId,
                DateTimeOffset.UtcNow));
        await dbContext.SaveChangesAsync();
    }

    private async Task<int> CountContactParticipationsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        return await dbContext.ContactParticipations.CountAsync();
    }

    private async Task<Guid> ResolveUserIdAsync(string userName)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var normalizedUserName = ApplicationUser.NormalizeUserName(userName);
        var user = await dbContext.ApplicationUsers.SingleAsync(item => item.NormalizedUserName == normalizedUserName);
        return user.Id;
    }

    private async Task<string> LoginAsync(string userName, string password)
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                userName,
                password
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginTestResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body?.AccessToken));
        return body.AccessToken;
    }

    private async Task<HttpResponseMessage> SendGetAsync(string path, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendPostAsync(string path, string accessToken, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }

    private sealed record LoginTestResponse(string AccessToken);

    private sealed record ContactInterventionTestResponse(
        Guid Id,
        Guid ContactId,
        string ContactName,
        string? ContactTypeName,
        string ModuleKey,
        string OriginType,
        string OriginId,
        string OriginDisplayName,
        string Subject,
        string HelpType,
        string Outcome,
        string? Notes,
        DateTimeOffset OccurredUtc,
        Guid CreatedByUserId,
        string? CreatedByUserName,
        DateTimeOffset CreatedUtc,
        DateTimeOffset? UpdatedUtc,
        DateTimeOffset? ArchivedUtc);
}
