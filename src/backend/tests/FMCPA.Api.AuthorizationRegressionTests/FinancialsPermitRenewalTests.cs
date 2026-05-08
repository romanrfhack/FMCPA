using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FMCPA.Domain.Entities.Shared;
using FMCPA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FMCPA.Api.AuthorizationRegressionTests;

public sealed class FinancialsPermitRenewalTests : IClassFixture<AuthorizationRegressionWebApplicationFactory>
{
    private readonly AuthorizationRegressionWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public FinancialsPermitRenewalTests(AuthorizationRegressionWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Financial_permit_renewal_creates_current_permit_preserves_history_suppresses_old_alerts_and_audits()
    {
        var statusId = await SeedFinancialPermitStatusAsync();
        var commissionTypes = await SeedCommissionTypesAsync();
        var adminToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.AdminUserName,
            AuthorizationRegressionWebApplicationFactory.AdminPassword);
        var readOnlyToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName,
            AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var expiredValidTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var originalCreditDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20));
        var renewedCreditDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));

        using var createResponse = await SendPostAsync(
            "/api/financials",
            adminToken,
            new
            {
                financialName = $"Financiera renovacion {suffix}",
                institutionOrDependency = "Dependencia regression",
                placeOrStand = "Stand original",
                validFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-40)),
                validTo = expiredValidTo,
                schedule = "09:00-14:00",
                negotiatedTerms = "Terminos originales",
                statusCatalogEntryId = statusId,
                notes = "Oficio inicial para renovacion"
            });
        var createdPermit = await createResponse.Content.ReadFromJsonAsync<FinancialPermitSummaryTestResponse>();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createdPermit);
        Assert.True(createdPermit!.IsCurrentVersion);
        Assert.Equal(0, createdPermit.RenewalSequence);
        Assert.Equal(createdPermit.Id, createdPermit.CurrentRootPermitId);

        using var originalCreditResponse = await SendPostAsync(
            $"/api/financials/{createdPermit.Id}/credits",
            adminToken,
            new
            {
                promoterContactId = (Guid?)null,
                promoterName = $"Promotor original {suffix}",
                beneficiaryContactId = (Guid?)null,
                beneficiaryName = $"Beneficiario original {suffix}",
                phoneNumber = "5550101",
                whatsAppPhone = "5550101",
                authorizationDate = originalCreditDate,
                amount = 1000m,
                notes = "Credito historico de la cadena"
            });
        var originalCredit = await originalCreditResponse.Content.ReadFromJsonAsync<FinancialCreditTestResponse>();

        Assert.Equal(HttpStatusCode.Created, originalCreditResponse.StatusCode);
        Assert.NotNull(originalCredit);

        using var adminCommissionResponse = await SendPostAsync(
            $"/api/financials/credits/{originalCredit!.Id}/commissions",
            adminToken,
            new
            {
                commissionTypeId = commissionTypes.AdministrationId,
                recipientCategory = "COMPANY",
                recipientContactId = (Guid?)null,
                recipientName = "Administracion interna",
                baseAmount = 1000m,
                commissionAmount = 200m,
                notes = "Comision administrativa de cadena"
            });

        Assert.Equal(HttpStatusCode.Created, adminCommissionResponse.StatusCode);

        using var alertsBeforeResponse = await SendGetAsync("/api/financials/alerts/permits", adminToken);
        var alertsBefore = await alertsBeforeResponse.Content.ReadFromJsonAsync<List<FinancialPermitAlertTestResponse>>();

        Assert.Equal(HttpStatusCode.OK, alertsBeforeResponse.StatusCode);
        Assert.Contains(alertsBefore ?? [], item => item.PermitId == createdPermit.Id && item.AlertState == "EXPIRED");

        using var renewResponse = await SendPostAsync(
            $"/api/financials/{createdPermit.Id}/renew",
            adminToken,
            new
            {
                validFrom = DateOnly.FromDateTime(DateTime.UtcNow),
                validTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)),
                placeOrStand = "Stand renovado",
                schedule = "10:00-16:00",
                negotiatedTerms = "Terminos renovados",
                notes = "Renovacion formal minima"
            });
        var renewedPermit = await renewResponse.Content.ReadFromJsonAsync<FinancialPermitDetailTestResponse>();

        Assert.Equal(HttpStatusCode.Created, renewResponse.StatusCode);
        Assert.NotNull(renewedPermit);
        Assert.NotEqual(createdPermit.Id, renewedPermit!.Id);
        Assert.Equal(createdPermit.Id, renewedPermit.RenewedFromPermitId);
        Assert.Equal(createdPermit.CurrentRootPermitId, renewedPermit.CurrentRootPermitId);
        Assert.True(renewedPermit.IsCurrentVersion);
        Assert.Equal(1, renewedPermit.RenewalSequence);
        Assert.Equal("Stand renovado", renewedPermit.PlaceOrStand);
        Assert.Equal("10:00-16:00", renewedPermit.Schedule);

        var creditAuditCountBeforeHistoricalBlock = await CountAuditEventsAsync("FINANCIAL_CREDIT");
        using var blockedHistoricalCreditResponse = await SendPostAsync(
            $"/api/financials/{createdPermit.Id}/credits",
            adminToken,
            new
            {
                promoterContactId = (Guid?)null,
                promoterName = $"Promotor bloqueado {suffix}",
                beneficiaryContactId = (Guid?)null,
                beneficiaryName = $"Beneficiario bloqueado {suffix}",
                phoneNumber = "5550999",
                whatsAppPhone = "5550999",
                authorizationDate = renewedCreditDate,
                amount = 500m,
                notes = "Credito no permitido en historico"
            });
        var blockedHistoricalCredit = await blockedHistoricalCreditResponse.Content.ReadFromJsonAsync<FinancialPermitOperationBlockedTestResponse>();

        Assert.Equal(HttpStatusCode.Conflict, blockedHistoricalCreditResponse.StatusCode);
        Assert.NotNull(blockedHistoricalCredit);
        Assert.Equal("FINANCIAL_PERMIT_NOT_CURRENT", blockedHistoricalCredit!.ReasonCode);
        Assert.Equal(createdPermit.Id, blockedHistoricalCredit.PermitId);
        Assert.Equal(createdPermit.CurrentRootPermitId, blockedHistoricalCredit.CurrentRootPermitId);
        Assert.Equal(renewedPermit.Id, blockedHistoricalCredit.CurrentPermitId);
        Assert.Contains("permiso vigente", blockedHistoricalCredit.Message);
        Assert.Equal(creditAuditCountBeforeHistoricalBlock, await CountAuditEventsAsync("FINANCIAL_CREDIT"));

        var commissionAuditCountBeforeHistoricalBlock = await CountAuditEventsAsync("FINANCIAL_CREDIT_COMMISSION");
        using var blockedHistoricalCommissionResponse = await SendPostAsync(
            $"/api/financials/credits/{originalCredit!.Id}/commissions",
            adminToken,
            new
            {
                commissionTypeId = commissionTypes.PromoterId,
                recipientCategory = "OTHER_PARTICIPANT",
                recipientContactId = (Guid?)null,
                recipientName = $"Comision bloqueada {suffix}",
                baseAmount = 1000m,
                commissionAmount = 80m,
                notes = "Comision no permitida en credito historico"
            });
        var blockedHistoricalCommission = await blockedHistoricalCommissionResponse.Content.ReadFromJsonAsync<FinancialPermitOperationBlockedTestResponse>();

        Assert.Equal(HttpStatusCode.Conflict, blockedHistoricalCommissionResponse.StatusCode);
        Assert.NotNull(blockedHistoricalCommission);
        Assert.Equal("FINANCIAL_PERMIT_NOT_CURRENT", blockedHistoricalCommission!.ReasonCode);
        Assert.Equal(createdPermit.Id, blockedHistoricalCommission.PermitId);
        Assert.Equal(createdPermit.CurrentRootPermitId, blockedHistoricalCommission.CurrentRootPermitId);
        Assert.Equal(renewedPermit.Id, blockedHistoricalCommission.CurrentPermitId);
        Assert.Contains("permiso vigente", blockedHistoricalCommission.Message);
        Assert.Equal(commissionAuditCountBeforeHistoricalBlock, await CountAuditEventsAsync("FINANCIAL_CREDIT_COMMISSION"));

        using var renewedCreditResponse = await SendPostAsync(
            $"/api/financials/{renewedPermit.Id}/credits",
            adminToken,
            new
            {
                promoterContactId = (Guid?)null,
                promoterName = $"Promotor renovado {suffix}",
                beneficiaryContactId = (Guid?)null,
                beneficiaryName = $"Beneficiario renovado {suffix}",
                phoneNumber = "5550202",
                whatsAppPhone = "5550202",
                authorizationDate = renewedCreditDate,
                amount = 2000m,
                notes = "Credito vigente de la cadena"
            });
        var renewedCredit = await renewedCreditResponse.Content.ReadFromJsonAsync<FinancialCreditTestResponse>();

        Assert.Equal(HttpStatusCode.Created, renewedCreditResponse.StatusCode);
        Assert.NotNull(renewedCredit);

        using var promoterCommissionResponse = await SendPostAsync(
            $"/api/financials/credits/{renewedCredit!.Id}/commissions",
            adminToken,
            new
            {
                commissionTypeId = commissionTypes.PromoterId,
                recipientCategory = "OTHER_PARTICIPANT",
                recipientContactId = (Guid?)null,
                recipientName = $"Promotor renovado {suffix}",
                baseAmount = 2000m,
                commissionAmount = 150m,
                notes = "Comision de promotor de cadena"
            });

        using var thirdPartyCommissionResponse = await SendPostAsync(
            $"/api/financials/credits/{renewedCredit.Id}/commissions",
            adminToken,
            new
            {
                commissionTypeId = commissionTypes.IntermediationId,
                recipientCategory = "THIRD_PARTY",
                recipientContactId = (Guid?)null,
                recipientName = "Intermediario externo",
                baseAmount = 2000m,
                commissionAmount = 75m,
                notes = "Comision de tercero de cadena"
            });

        Assert.Equal(HttpStatusCode.Created, promoterCommissionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, thirdPartyCommissionResponse.StatusCode);

        using var chainResponse = await SendGetAsync($"/api/financials/{createdPermit.Id}/renewal-chain", adminToken);
        var chain = await chainResponse.Content.ReadFromJsonAsync<FinancialPermitRenewalChainTestResponse>();

        Assert.Equal(HttpStatusCode.OK, chainResponse.StatusCode);
        Assert.NotNull(chain);
        Assert.Equal(createdPermit.CurrentRootPermitId, chain!.CurrentRootPermitId);
        Assert.Equal(renewedPermit.Id, chain.CurrentPermitId);
        Assert.True(chain.CurrentPermit.IsCurrentVersion);
        Assert.Equal(2, chain.Permits.Count);
        Assert.Equal(new[] { 0, 1 }, chain.Permits.Select(item => item.RenewalSequence).ToArray());
        Assert.Equal(2, chain.Summary.TotalCreditsCount);
        Assert.Equal(3000m, chain.Summary.TotalCreditsAmount);
        Assert.Equal(3, chain.Summary.TotalCommissionsCount);
        Assert.Equal(425m, chain.Summary.TotalCommissionsAmount);
        Assert.Equal(150m, chain.Summary.TotalPromoterCommission);
        Assert.Equal(200m, chain.Summary.TotalAdminCommission);
        Assert.Equal(75m, chain.Summary.TotalThirdPartyCommission);
        Assert.Equal(originalCreditDate, chain.Summary.OperationFrom);
        Assert.Equal(renewedCreditDate, chain.Summary.OperationTo);
        Assert.Contains(chain.Credits, item => item.FinancialPermitId == createdPermit.Id);
        Assert.Contains(chain.Credits, item => item.FinancialPermitId == renewedPermit.Id);

        using var oldDetailResponse = await SendGetAsync($"/api/financials/{createdPermit.Id}", adminToken);
        var oldDetail = await oldDetailResponse.Content.ReadFromJsonAsync<FinancialPermitDetailTestResponse>();

        Assert.Equal(HttpStatusCode.OK, oldDetailResponse.StatusCode);
        Assert.NotNull(oldDetail);
        Assert.False(oldDetail!.IsCurrentVersion);
        Assert.Equal("HISTORICAL", oldDetail.AlertState);
        Assert.Contains(oldDetail.RenewalHistory, item => item.Id == createdPermit.Id && !item.IsCurrentVersion);
        Assert.Contains(oldDetail.RenewalHistory, item => item.Id == renewedPermit.Id && item.IsCurrentVersion);

        using var listResponse = await SendGetAsync("/api/financials", adminToken);
        var permits = await listResponse.Content.ReadFromJsonAsync<List<FinancialPermitSummaryTestResponse>>();

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Contains(permits ?? [], item => item.Id == createdPermit.Id && !item.IsCurrentVersion && item.AlertState == "HISTORICAL");
        Assert.Contains(permits ?? [], item => item.Id == renewedPermit.Id && item.IsCurrentVersion);

        using var alertsAfterResponse = await SendGetAsync("/api/financials/alerts/permits", adminToken);
        var alertsAfter = await alertsAfterResponse.Content.ReadFromJsonAsync<List<FinancialPermitAlertTestResponse>>();

        Assert.Equal(HttpStatusCode.OK, alertsAfterResponse.StatusCode);
        Assert.DoesNotContain(alertsAfter ?? [], item => item.PermitId == createdPermit.Id);
        Assert.DoesNotContain(alertsAfter ?? [], item => item.PermitId == renewedPermit.Id);

        using var dashboardAlertsResponse = await SendGetAsync("/api/dashboard/alerts", adminToken);
        var dashboardAlerts = await dashboardAlertsResponse.Content.ReadFromJsonAsync<DashboardAlertsTestResponse>();

        Assert.Equal(HttpStatusCode.OK, dashboardAlertsResponse.StatusCode);
        Assert.DoesNotContain(dashboardAlerts?.FinancialPermits ?? [], item => item.AlertKey == $"FINANCIAL_PERMIT:{createdPermit.Id}");

        using var auditResponse = await SendGetAsync(
            $"/api/bitacora?moduleCode=FINANCIALS&entityType=FINANCIAL_PERMIT&entityId={renewedPermit.Id}&take=20",
            adminToken);
        var audit = await auditResponse.Content.ReadFromJsonAsync<List<BitacoraItemTestResponse>>();

        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);
        Assert.Contains(audit ?? [], item => item.ActionType == "FINANCIAL_PERMIT_RENEWED");

        using var closeRenewedResponse = await SendPostAsync(
            $"/api/financials/{renewedPermit.Id}/close",
            adminToken,
            new
            {
                reason = "Cierre terminal para validar captura bloqueada"
            });

        Assert.Equal(HttpStatusCode.OK, closeRenewedResponse.StatusCode);

        var creditAuditCountBeforeTerminalBlock = await CountAuditEventsAsync("FINANCIAL_CREDIT");
        using var blockedTerminalCreditResponse = await SendPostAsync(
            $"/api/financials/{renewedPermit.Id}/credits",
            adminToken,
            new
            {
                promoterContactId = (Guid?)null,
                promoterName = $"Promotor terminal {suffix}",
                beneficiaryContactId = (Guid?)null,
                beneficiaryName = $"Beneficiario terminal {suffix}",
                phoneNumber = "5550888",
                whatsAppPhone = "5550888",
                authorizationDate = renewedCreditDate,
                amount = 600m,
                notes = "Credito no permitido en terminal"
            });
        var blockedTerminalCredit = await blockedTerminalCreditResponse.Content.ReadFromJsonAsync<FinancialPermitOperationBlockedTestResponse>();

        Assert.Equal(HttpStatusCode.Conflict, blockedTerminalCreditResponse.StatusCode);
        Assert.NotNull(blockedTerminalCredit);
        Assert.Equal("FINANCIAL_PERMIT_TERMINAL", blockedTerminalCredit!.ReasonCode);
        Assert.Equal(renewedPermit.Id, blockedTerminalCredit.PermitId);
        Assert.Equal(renewedPermit.CurrentRootPermitId, blockedTerminalCredit.CurrentRootPermitId);
        Assert.Equal(renewedPermit.Id, blockedTerminalCredit.CurrentPermitId);
        Assert.Contains("estado terminal", blockedTerminalCredit.Message);
        Assert.Equal(creditAuditCountBeforeTerminalBlock, await CountAuditEventsAsync("FINANCIAL_CREDIT"));

        var commissionAuditCountBeforeTerminalBlock = await CountAuditEventsAsync("FINANCIAL_CREDIT_COMMISSION");
        using var blockedTerminalCommissionResponse = await SendPostAsync(
            $"/api/financials/credits/{renewedCredit!.Id}/commissions",
            adminToken,
            new
            {
                commissionTypeId = commissionTypes.PromoterId,
                recipientCategory = "OTHER_PARTICIPANT",
                recipientContactId = (Guid?)null,
                recipientName = $"Comision terminal {suffix}",
                baseAmount = 2000m,
                commissionAmount = 90m,
                notes = "Comision no permitida en terminal"
            });
        var blockedTerminalCommission = await blockedTerminalCommissionResponse.Content.ReadFromJsonAsync<FinancialPermitOperationBlockedTestResponse>();

        Assert.Equal(HttpStatusCode.Conflict, blockedTerminalCommissionResponse.StatusCode);
        Assert.NotNull(blockedTerminalCommission);
        Assert.Equal("FINANCIAL_PERMIT_TERMINAL", blockedTerminalCommission!.ReasonCode);
        Assert.Equal(renewedPermit.Id, blockedTerminalCommission.PermitId);
        Assert.Equal(renewedPermit.CurrentRootPermitId, blockedTerminalCommission.CurrentRootPermitId);
        Assert.Equal(renewedPermit.Id, blockedTerminalCommission.CurrentPermitId);
        Assert.Contains("estado terminal", blockedTerminalCommission.Message);
        Assert.Equal(commissionAuditCountBeforeTerminalBlock, await CountAuditEventsAsync("FINANCIAL_CREDIT_COMMISSION"));

        using var readOnlyRenewResponse = await SendPostAsync(
            $"/api/financials/{renewedPermit.Id}/renew",
            readOnlyToken,
            new
            {
                validFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(91)),
                validTo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(180))
            });

        Assert.Equal(HttpStatusCode.Forbidden, readOnlyRenewResponse.StatusCode);
    }

    private async Task<int> SeedFinancialPermitStatusAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var status = new ModuleStatusCatalogEntry(
            "FINANCIALS",
            "Financieras",
            "FINANCIAL_PERMIT",
            "Oficio o autorizacion",
            "IN_PROCESS",
            "En proceso",
            description: null,
            sortOrder: 1,
            isClosed: false,
            alertsEnabledByDefault: true);
        var closedStatus = new ModuleStatusCatalogEntry(
            "FINANCIALS",
            "Financieras",
            "FINANCIAL_PERMIT",
            "Oficio o autorizacion",
            "CLOSED",
            "Cerrado",
            description: null,
            sortOrder: 2,
            isClosed: true,
            alertsEnabledByDefault: false);

        dbContext.ModuleStatusCatalogEntries.AddRange(status, closedStatus);
        await dbContext.SaveChangesAsync();

        return status.Id;
    }

    private async Task<(int AdministrationId, int PromoterId, int IntermediationId)> SeedCommissionTypesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var administration = new CommissionType(
            "ADMINISTRATION",
            "Administracion",
            description: null,
            sortOrder: 1);
        var promoter = new CommissionType(
            "PROMOTER",
            "Promotor",
            description: null,
            sortOrder: 2);
        var intermediation = new CommissionType(
            "INTERMEDIATION",
            "Intermediacion",
            description: null,
            sortOrder: 3);

        dbContext.CommissionTypes.AddRange(administration, promoter, intermediation);
        await dbContext.SaveChangesAsync();

        return (administration.Id, promoter.Id, intermediation.Id);
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

        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(login);
        return login!.AccessToken;
    }

    private async Task<HttpResponseMessage> SendGetAsync(string path, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendPostAsync(string path, string accessToken, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    private async Task<int> CountAuditEventsAsync(string entityType)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        return await dbContext.AuditEvents.CountAsync(
            item => item.ModuleCode == "FINANCIALS"
                && item.EntityType == entityType
                && item.ActionType == "REGISTERED");
    }

    private sealed record LoginResponse(string AccessToken);

    private sealed record FinancialPermitSummaryTestResponse(
        Guid Id,
        Guid? RenewedFromPermitId,
        Guid CurrentRootPermitId,
        bool IsCurrentVersion,
        int RenewalSequence,
        string AlertState);

    private sealed record FinancialPermitDetailTestResponse(
        Guid Id,
        string PlaceOrStand,
        string Schedule,
        string AlertState,
        Guid? RenewedFromPermitId,
        Guid CurrentRootPermitId,
        bool IsCurrentVersion,
        int RenewalSequence,
        List<FinancialPermitRenewalHistoryTestResponse> RenewalHistory);

    private sealed record FinancialPermitRenewalHistoryTestResponse(
        Guid Id,
        bool IsCurrentVersion);

    private sealed record FinancialPermitRenewalChainTestResponse(
        Guid CurrentRootPermitId,
        Guid CurrentPermitId,
        FinancialPermitRenewalChainPermitTestResponse CurrentPermit,
        List<FinancialPermitRenewalChainPermitTestResponse> Permits,
        FinancialPermitRenewalChainSummaryTestResponse Summary,
        List<FinancialCreditTestResponse> Credits);

    private sealed record FinancialPermitRenewalChainPermitTestResponse(
        Guid Id,
        bool IsCurrentVersion,
        int RenewalSequence);

    private sealed record FinancialPermitRenewalChainSummaryTestResponse(
        int TotalCreditsCount,
        decimal TotalCreditsAmount,
        int TotalCommissionsCount,
        decimal TotalCommissionsAmount,
        decimal TotalPromoterCommission,
        decimal TotalAdminCommission,
        decimal TotalThirdPartyCommission,
        DateOnly? OperationFrom,
        DateOnly? OperationTo);

    private sealed record FinancialCreditTestResponse(
        Guid Id,
        Guid FinancialPermitId);

    private sealed record FinancialPermitOperationBlockedTestResponse(
        string Message,
        string ReasonCode,
        Guid PermitId,
        Guid CurrentRootPermitId,
        Guid? CurrentPermitId);

    private sealed record FinancialPermitAlertTestResponse(
        Guid PermitId,
        string AlertState);

    private sealed record DashboardAlertsTestResponse(
        List<DashboardAlertItemTestResponse> FinancialPermits);

    private sealed record DashboardAlertItemTestResponse(
        string AlertKey);

    private sealed record BitacoraItemTestResponse(
        string ActionType);
}
