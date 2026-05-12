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

    [Fact]
    public async Task Financial_permit_create_and_renewal_block_active_operational_conflicts()
    {
        var statusId = await SeedFinancialPermitStatusAsync();
        var adminToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.AdminUserName,
            AuthorizationRegressionWebApplicationFactory.AdminPassword);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var basePermitResponse = await SendPostAsync(
            "/api/financials",
            adminToken,
            new
            {
                financialName = $"Financiera Conflicto {suffix}",
                institutionOrDependency = "Dependencia Regression",
                placeOrStand = "Stand Norte",
                validFrom = today,
                validTo = today.AddDays(90),
                schedule = "09:00-14:00",
                negotiatedTerms = "Terminos base",
                statusCatalogEntryId = statusId,
                notes = "Permiso base de conflicto"
            });
        var basePermit = await basePermitResponse.Content.ReadFromJsonAsync<FinancialPermitSummaryTestResponse>();

        Assert.Equal(HttpStatusCode.Created, basePermitResponse.StatusCode);
        Assert.NotNull(basePermit);

        using var normalizedConflictResponse = await SendPostAsync(
            "/api/financials",
            adminToken,
            new
            {
                financialName = $"  financiera conflicto {suffix}  ",
                institutionOrDependency = " dependencia   regression ",
                placeOrStand = " stand   norte ",
                validFrom = today.AddDays(120),
                validTo = today.AddDays(150),
                schedule = "10:00-15:00",
                negotiatedTerms = "Terminos conflictivos",
                statusCatalogEntryId = statusId,
                notes = "Alta conflictiva normalizada"
            });
        var normalizedConflict = await normalizedConflictResponse.Content.ReadFromJsonAsync<FinancialPermitActiveConflictTestResponse>();

        Assert.Equal(HttpStatusCode.Conflict, normalizedConflictResponse.StatusCode);
        Assert.NotNull(normalizedConflict);
        Assert.Equal("FINANCIAL_PERMIT_ACTIVE_CONFLICT", normalizedConflict!.ReasonCode);
        Assert.Equal(basePermit!.Id, normalizedConflict.ConflictingPermitId);
        Assert.Equal(basePermit.CurrentRootPermitId, normalizedConflict.CurrentRootPermitId);
        Assert.Contains("oficio vigente", normalizedConflict.Message);

        using var allowedDifferentStandResponse = await SendPostAsync(
            "/api/financials",
            adminToken,
            new
            {
                financialName = $"Financiera Conflicto {suffix}",
                institutionOrDependency = "Dependencia Regression",
                placeOrStand = "Stand Sur",
                validFrom = today.AddDays(120),
                validTo = today.AddDays(150),
                schedule = "10:00-15:00",
                negotiatedTerms = "Terminos no conflictivos",
                statusCatalogEntryId = statusId,
                notes = "Alta permitida por lugar distinto"
            });

        Assert.Equal(HttpStatusCode.Created, allowedDifferentStandResponse.StatusCode);

        using var competingPermitResponse = await SendPostAsync(
            "/api/financials",
            adminToken,
            new
            {
                financialName = $"Financiera Conflicto {suffix}",
                institutionOrDependency = "Dependencia Regression",
                placeOrStand = "Stand Oriente",
                validFrom = today.AddDays(300),
                validTo = today.AddDays(330),
                schedule = "11:00-16:00",
                negotiatedTerms = "Terminos de otro stand",
                statusCatalogEntryId = statusId,
                notes = "Permiso vigente de otra cadena"
            });
        var competingPermit = await competingPermitResponse.Content.ReadFromJsonAsync<FinancialPermitSummaryTestResponse>();

        Assert.Equal(HttpStatusCode.Created, competingPermitResponse.StatusCode);
        Assert.NotNull(competingPermit);

        using var blockedRenewalResponse = await SendPostAsync(
            $"/api/financials/{basePermit.Id}/renew",
            adminToken,
            new
            {
                validFrom = today.AddDays(91),
                validTo = today.AddDays(180),
                placeOrStand = " stand   oriente ",
                schedule = "12:00-17:00",
                negotiatedTerms = "Renovacion hacia stand conflictivo",
                notes = "Debe bloquearse por conflicto operativo"
            });
        var blockedRenewal = await blockedRenewalResponse.Content.ReadFromJsonAsync<FinancialPermitActiveConflictTestResponse>();

        Assert.Equal(HttpStatusCode.Conflict, blockedRenewalResponse.StatusCode);
        Assert.NotNull(blockedRenewal);
        Assert.Equal("FINANCIAL_PERMIT_ACTIVE_CONFLICT", blockedRenewal!.ReasonCode);
        Assert.Equal(competingPermit!.Id, blockedRenewal.ConflictingPermitId);
        Assert.Equal(competingPermit.CurrentRootPermitId, blockedRenewal.CurrentRootPermitId);

        using var validRenewalResponse = await SendPostAsync(
            $"/api/financials/{basePermit.Id}/renew",
            adminToken,
            new
            {
                validFrom = today.AddDays(91),
                validTo = today.AddDays(180),
                placeOrStand = "Stand Poniente",
                schedule = "12:00-17:00",
                negotiatedTerms = "Renovacion no conflictiva",
                notes = "Debe conservar la cadena valida"
            });
        var validRenewal = await validRenewalResponse.Content.ReadFromJsonAsync<FinancialPermitDetailTestResponse>();

        Assert.Equal(HttpStatusCode.Created, validRenewalResponse.StatusCode);
        Assert.NotNull(validRenewal);
        Assert.True(validRenewal!.IsCurrentVersion);
        Assert.Equal(basePermit.CurrentRootPermitId, validRenewal.CurrentRootPermitId);
        Assert.Equal(1, validRenewal.RenewalSequence);
    }

    [Fact]
    public async Task Financial_contextual_renewal_draft_prefills_last_applicable_permit_and_renews_current_chain()
    {
        var statusId = await SeedFinancialPermitStatusAsync();
        var adminToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.AdminUserName,
            AuthorizationRegressionWebApplicationFactory.AdminPassword);
        var readOnlyToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName,
            AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var financialName = $"Financiera Draft {suffix}";
        const string institutionOrDependency = "Dependencia Draft";
        const string originalStand = "Stand Contextual";

        using var basePermitResponse = await SendPostAsync(
            "/api/financials",
            adminToken,
            new
            {
                financialName,
                institutionOrDependency,
                placeOrStand = originalStand,
                validFrom = today,
                validTo = today.AddDays(30),
                schedule = "08:00-13:00",
                negotiatedTerms = "Terminos aplicables del contexto anterior",
                statusCatalogEntryId = statusId,
                notes = "Observaciones utiles para preparar renovacion"
            });
        var basePermit = await basePermitResponse.Content.ReadFromJsonAsync<FinancialPermitSummaryTestResponse>();

        Assert.Equal(HttpStatusCode.Created, basePermitResponse.StatusCode);
        Assert.NotNull(basePermit);

        using var movedRenewalResponse = await SendPostAsync(
            $"/api/financials/{basePermit!.Id}/renew",
            adminToken,
            new
            {
                validFrom = today.AddDays(31),
                validTo = today.AddDays(120),
                placeOrStand = "Stand Temporal",
                schedule = "10:00-16:00",
                negotiatedTerms = "Terminos temporales",
                notes = "Renovacion intermedia"
            });
        var movedRenewal = await movedRenewalResponse.Content.ReadFromJsonAsync<FinancialPermitDetailTestResponse>();

        Assert.Equal(HttpStatusCode.Created, movedRenewalResponse.StatusCode);
        Assert.NotNull(movedRenewal);
        Assert.Equal(1, movedRenewal!.RenewalSequence);

        using var contextResponse = await SendGetAsync(
            BuildCurrentPermitResolutionPath(financialName, institutionOrDependency, "stand contextual"),
            readOnlyToken);
        var context = await contextResponse.Content.ReadFromJsonAsync<FinancialPermitContextResolutionTestResponse>();

        Assert.Equal(HttpStatusCode.OK, contextResponse.StatusCode);
        Assert.NotNull(context);
        Assert.Equal("RENEW_LAST_PERMIT", context!.SuggestionCode);
        Assert.Null(context.CurrentPermit);
        Assert.NotNull(context.LastKnownPermit);
        Assert.Equal(basePermit.Id, context.LastKnownPermit!.PermitId);

        using var draftResponse = await SendGetAsync(
            $"/api/financials/{context.LastKnownPermit.PermitId}/renewal-draft",
            readOnlyToken);
        var draft = await draftResponse.Content.ReadFromJsonAsync<FinancialPermitRenewalDraftTestResponse>();

        Assert.Equal(HttpStatusCode.OK, draftResponse.StatusCode);
        Assert.NotNull(draft);
        Assert.Equal(basePermit.Id, draft!.SourcePermitId);
        Assert.Equal(movedRenewal.Id, draft.RenewalTargetPermitId);
        Assert.Equal(basePermit.CurrentRootPermitId, draft.CurrentRootPermitId);
        Assert.False(draft.SourceIsCurrentVersion);
        Assert.Equal(0, draft.SourceRenewalSequence);
        Assert.Equal(1, draft.RenewalTargetSequence);
        Assert.Equal(originalStand, draft.PlaceOrStand);
        Assert.Equal("08:00-13:00", draft.Schedule);
        Assert.Equal("Terminos aplicables del contexto anterior", draft.NegotiatedTerms);
        Assert.Equal("Observaciones utiles para preparar renovacion", draft.Notes);
        Assert.Equal(today.AddDays(121), draft.NewStartDate);
        Assert.Equal(today.AddDays(151), draft.NewEndDate);
        Assert.Equal("IN_PROCESS", draft.StatusCode);
        Assert.Contains("newStartDate", draft.FieldsToConfirm);
        Assert.Contains("newEndDate", draft.FieldsToConfirm);

        using var contextualRenewalResponse = await SendPostAsync(
            $"/api/financials/{draft.RenewalTargetPermitId}/renew",
            adminToken,
            new
            {
                validFrom = draft.NewStartDate,
                validTo = draft.NewEndDate,
                placeOrStand = draft.PlaceOrStand,
                schedule = draft.Schedule,
                negotiatedTerms = draft.NegotiatedTerms,
                notes = "Renovacion real iniciada desde draft contextual"
            });
        var contextualRenewal = await contextualRenewalResponse.Content.ReadFromJsonAsync<FinancialPermitDetailTestResponse>();

        Assert.Equal(HttpStatusCode.Created, contextualRenewalResponse.StatusCode);
        Assert.NotNull(contextualRenewal);
        Assert.Equal(draft.RenewalTargetPermitId, contextualRenewal!.RenewedFromPermitId);
        Assert.Equal(basePermit.CurrentRootPermitId, contextualRenewal.CurrentRootPermitId);
        Assert.True(contextualRenewal.IsCurrentVersion);
        Assert.Equal(2, contextualRenewal.RenewalSequence);
        Assert.Equal(originalStand, contextualRenewal.PlaceOrStand);
        Assert.Equal("08:00-13:00", contextualRenewal.Schedule);

        using var chainResponse = await SendGetAsync($"/api/financials/{basePermit.Id}/renewal-chain", readOnlyToken);
        var chain = await chainResponse.Content.ReadFromJsonAsync<FinancialPermitRenewalChainTestResponse>();

        Assert.Equal(HttpStatusCode.OK, chainResponse.StatusCode);
        Assert.NotNull(chain);
        Assert.Equal(contextualRenewal.Id, chain!.CurrentPermitId);
        Assert.Equal(3, chain.Permits.Count);
        Assert.Equal(new[] { 0, 1, 2 }, chain.Permits.Select(item => item.RenewalSequence).ToArray());

        using var resolvedRenewedContextResponse = await SendGetAsync(
            BuildCurrentPermitResolutionPath(financialName, institutionOrDependency, originalStand),
            readOnlyToken);
        var resolvedRenewedContext = await resolvedRenewedContextResponse.Content.ReadFromJsonAsync<FinancialPermitContextResolutionTestResponse>();

        Assert.Equal(HttpStatusCode.OK, resolvedRenewedContextResponse.StatusCode);
        Assert.NotNull(resolvedRenewedContext);
        Assert.Equal("USE_CURRENT_PERMIT", resolvedRenewedContext!.SuggestionCode);
        Assert.NotNull(resolvedRenewedContext.CurrentPermit);
        Assert.Equal(contextualRenewal.Id, resolvedRenewedContext.CurrentPermit!.PermitId);
    }

    [Fact]
    public async Task Financial_contextual_create_new_permit_reuses_create_endpoint_and_becomes_current()
    {
        var statusId = await SeedFinancialPermitStatusAsync();
        var adminToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.AdminUserName,
            AuthorizationRegressionWebApplicationFactory.AdminPassword);
        var readOnlyToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName,
            AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var financialName = $"Financiera Alta Contextual {suffix}";
        const string institutionOrDependency = "Dependencia Alta Contextual";
        const string placeOrStand = "Stand Alta Contextual";

        using var missingContextResponse = await SendGetAsync(
            BuildCurrentPermitResolutionPath(financialName, institutionOrDependency, placeOrStand),
            readOnlyToken);
        var missingContext = await missingContextResponse.Content.ReadFromJsonAsync<FinancialPermitContextResolutionTestResponse>();

        Assert.Equal(HttpStatusCode.OK, missingContextResponse.StatusCode);
        Assert.NotNull(missingContext);
        Assert.Equal("CREATE_NEW_PERMIT", missingContext!.SuggestionCode);
        Assert.Null(missingContext.CurrentPermit);
        Assert.Null(missingContext.LastKnownPermit);
        Assert.Null(missingContext.CurrentRootPermitId);
        Assert.Contains("nuevo oficio", missingContext.SuggestionMessage);
        Assert.Equal("/financials", missingContext.RouteHint);

        using var createResponse = await SendPostAsync(
            "/api/financials",
            adminToken,
            new
            {
                financialName,
                institutionOrDependency,
                placeOrStand,
                validFrom = today,
                validTo = today.AddDays(60),
                schedule = "09:00-13:00",
                negotiatedTerms = "Terminos de alta contextual",
                statusCatalogEntryId = statusId,
                notes = "Alta real desde contexto CREATE_NEW_PERMIT"
            });
        var createdPermit = await createResponse.Content.ReadFromJsonAsync<FinancialPermitSummaryTestResponse>();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createdPermit);
        Assert.True(createdPermit!.IsCurrentVersion);
        Assert.Equal(0, createdPermit.RenewalSequence);
        Assert.Equal(createdPermit.Id, createdPermit.CurrentRootPermitId);

        using var resolvedContextResponse = await SendGetAsync(
            BuildCurrentPermitResolutionPath(
                $" financiera alta contextual {suffix} ",
                " dependencia   alta contextual ",
                " stand alta contextual "),
            readOnlyToken);
        var resolvedContext = await resolvedContextResponse.Content.ReadFromJsonAsync<FinancialPermitContextResolutionTestResponse>();

        Assert.Equal(HttpStatusCode.OK, resolvedContextResponse.StatusCode);
        Assert.NotNull(resolvedContext);
        Assert.Equal("USE_CURRENT_PERMIT", resolvedContext!.SuggestionCode);
        Assert.NotNull(resolvedContext.CurrentPermit);
        Assert.Null(resolvedContext.LastKnownPermit);
        Assert.Equal(createdPermit.Id, resolvedContext.CurrentPermit!.PermitId);
        Assert.Equal(createdPermit.CurrentRootPermitId, resolvedContext.CurrentRootPermitId);

        using var duplicateCreateResponse = await SendPostAsync(
            "/api/financials",
            adminToken,
            new
            {
                financialName = $"FINANCIERA ALTA CONTEXTUAL {suffix}",
                institutionOrDependency,
                placeOrStand,
                validFrom = today.AddDays(90),
                validTo = today.AddDays(120),
                schedule = "14:00-18:00",
                negotiatedTerms = "Terminos duplicados",
                statusCatalogEntryId = statusId,
                notes = "Debe respetar unicidad operativa"
            });
        var duplicateConflict = await duplicateCreateResponse.Content.ReadFromJsonAsync<FinancialPermitActiveConflictTestResponse>();

        Assert.Equal(HttpStatusCode.Conflict, duplicateCreateResponse.StatusCode);
        Assert.NotNull(duplicateConflict);
        Assert.Equal("FINANCIAL_PERMIT_ACTIVE_CONFLICT", duplicateConflict!.ReasonCode);
        Assert.Equal(createdPermit.Id, duplicateConflict.ConflictingPermitId);
    }

    [Fact]
    public async Task Financial_context_card_consolidates_resolution_chain_credit_commissions_and_actions()
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
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var financialName = $"Financiera Ficha {suffix}";
        const string institutionOrDependency = "Dependencia Ficha";
        const string originalStand = "Stand Ficha Origen";
        const string renewedStand = "Stand Ficha Vigente";

        using var createResponse = await SendPostAsync(
            "/api/financials",
            adminToken,
            new
            {
                financialName,
                institutionOrDependency,
                placeOrStand = originalStand,
                validFrom = today,
                validTo = today.AddDays(30),
                schedule = "09:00-14:00",
                negotiatedTerms = "Terminos para ficha",
                statusCatalogEntryId = statusId,
                notes = "Permiso base de ficha"
            });
        var basePermit = await createResponse.Content.ReadFromJsonAsync<FinancialPermitSummaryTestResponse>();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(basePermit);

        using var currentCreditResponse = await SendPostAsync(
            $"/api/financials/{basePermit!.Id}/credits",
            adminToken,
            new
            {
                promoterContactId = (Guid?)null,
                promoterName = $"Promotor ficha {suffix}",
                beneficiaryContactId = (Guid?)null,
                beneficiaryName = $"Beneficiario ficha {suffix}",
                phoneNumber = "5550404",
                whatsAppPhone = "5550404",
                authorizationDate = today,
                amount = 1000m,
                notes = "Credito para ficha vigente"
            });
        var currentCredit = await currentCreditResponse.Content.ReadFromJsonAsync<FinancialCreditTestResponse>();

        Assert.Equal(HttpStatusCode.Created, currentCreditResponse.StatusCode);
        Assert.NotNull(currentCredit);

        using var adminCommissionResponse = await SendPostAsync(
            $"/api/financials/credits/{currentCredit!.Id}/commissions",
            adminToken,
            new
            {
                commissionTypeId = commissionTypes.AdministrationId,
                recipientCategory = "COMPANY",
                recipientContactId = (Guid?)null,
                recipientName = "Administracion ficha",
                baseAmount = 1000m,
                commissionAmount = 100m,
                notes = "Comision administrativa para ficha"
            });

        Assert.Equal(HttpStatusCode.Created, adminCommissionResponse.StatusCode);

        using var currentCardResponse = await SendGetAsync(
            BuildContextCardPath(financialName, institutionOrDependency, originalStand),
            readOnlyToken);
        var currentCard = await currentCardResponse.Content.ReadFromJsonAsync<FinancialContextCardTestResponse>();

        Assert.Equal(HttpStatusCode.OK, currentCardResponse.StatusCode);
        Assert.NotNull(currentCard);
        Assert.Equal("USE_CURRENT_PERMIT", currentCard!.Resolution.SuggestionCode);
        Assert.NotNull(currentCard.Resolution.CurrentPermit);
        Assert.Equal(basePermit.Id, currentCard.Resolution.CurrentPermit!.PermitId);
        Assert.Contains("CAPTURE_CREDIT", currentCard.AvailableActions);
        Assert.Contains("VIEW_CURRENT_PERMIT", currentCard.AvailableActions);
        Assert.Contains("VIEW_CHAIN", currentCard.AvailableActions);
        Assert.Equal(1, currentCard.RenewalChainSummary!.TotalPermitsCount);
        Assert.Equal(0, currentCard.RenewalChainSummary.CurrentRenewalSequence);
        Assert.Equal(today, currentCard.RenewalChainSummary.PeriodFrom);
        Assert.Equal(today.AddDays(30), currentCard.RenewalChainSummary.PeriodTo);
        Assert.Equal(1, currentCard.CreditSummary!.TotalCreditsCount);
        Assert.Equal(1000m, currentCard.CreditSummary.TotalCreditsAmount);
        Assert.Equal(1, currentCard.CommissionSummary!.TotalCommissionsCount);
        Assert.Equal(100m, currentCard.CommissionSummary.TotalAdminCommission);

        using var renewResponse = await SendPostAsync(
            $"/api/financials/{basePermit.Id}/renew",
            adminToken,
            new
            {
                validFrom = today.AddDays(31),
                validTo = today.AddDays(90),
                placeOrStand = renewedStand,
                schedule = "10:00-15:00",
                negotiatedTerms = "Terminos renovados para ficha",
                notes = "Renovacion para ficha"
            });
        var renewedPermit = await renewResponse.Content.ReadFromJsonAsync<FinancialPermitDetailTestResponse>();

        Assert.Equal(HttpStatusCode.Created, renewResponse.StatusCode);
        Assert.NotNull(renewedPermit);

        using var renewedCreditResponse = await SendPostAsync(
            $"/api/financials/{renewedPermit!.Id}/credits",
            adminToken,
            new
            {
                promoterContactId = (Guid?)null,
                promoterName = $"Promotor vigente {suffix}",
                beneficiaryContactId = (Guid?)null,
                beneficiaryName = $"Beneficiario vigente {suffix}",
                phoneNumber = "5550505",
                whatsAppPhone = "5550505",
                authorizationDate = today.AddDays(40),
                amount = 2000m,
                notes = "Credito renovado para ficha"
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
                recipientName = "Promotor ficha",
                baseAmount = 2000m,
                commissionAmount = 150m,
                notes = "Comision promotor para ficha"
            });
        using var thirdPartyCommissionResponse = await SendPostAsync(
            $"/api/financials/credits/{renewedCredit.Id}/commissions",
            adminToken,
            new
            {
                commissionTypeId = commissionTypes.IntermediationId,
                recipientCategory = "THIRD_PARTY",
                recipientContactId = (Guid?)null,
                recipientName = "Tercero ficha",
                baseAmount = 2000m,
                commissionAmount = 75m,
                notes = "Comision tercero para ficha"
            });

        Assert.Equal(HttpStatusCode.Created, promoterCommissionResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, thirdPartyCommissionResponse.StatusCode);

        using var historicalCardResponse = await SendGetAsync(
            BuildContextCardPath(financialName, institutionOrDependency, originalStand),
            readOnlyToken);
        var historicalCard = await historicalCardResponse.Content.ReadFromJsonAsync<FinancialContextCardTestResponse>();

        Assert.Equal(HttpStatusCode.OK, historicalCardResponse.StatusCode);
        Assert.NotNull(historicalCard);
        Assert.Equal("RENEW_LAST_PERMIT", historicalCard!.Resolution.SuggestionCode);
        Assert.Null(historicalCard.Resolution.CurrentPermit);
        Assert.Equal(basePermit.Id, historicalCard.Resolution.LastKnownPermit!.PermitId);
        Assert.Contains("PREPARE_RENEWAL", historicalCard.AvailableActions);
        Assert.Contains("VIEW_CHAIN", historicalCard.AvailableActions);
        Assert.DoesNotContain("CAPTURE_CREDIT", historicalCard.AvailableActions);
        Assert.Equal(2, historicalCard.RenewalChainSummary!.TotalPermitsCount);
        Assert.Equal(1, historicalCard.RenewalChainSummary.CurrentRenewalSequence);
        Assert.Equal(2, historicalCard.CreditSummary!.TotalCreditsCount);
        Assert.Equal(3000m, historicalCard.CreditSummary.TotalCreditsAmount);
        Assert.Equal(3, historicalCard.CommissionSummary!.TotalCommissionsCount);
        Assert.Equal(150m, historicalCard.CommissionSummary.TotalPromoterCommission);
        Assert.Equal(100m, historicalCard.CommissionSummary.TotalAdminCommission);
        Assert.Equal(75m, historicalCard.CommissionSummary.TotalThirdPartyCommission);

        using var missingCardResponse = await SendGetAsync(
            BuildContextCardPath(financialName, institutionOrDependency, "Stand Ficha Nuevo"),
            readOnlyToken);
        var missingCard = await missingCardResponse.Content.ReadFromJsonAsync<FinancialContextCardTestResponse>();

        Assert.Equal(HttpStatusCode.OK, missingCardResponse.StatusCode);
        Assert.NotNull(missingCard);
        Assert.Equal("CREATE_NEW_PERMIT", missingCard!.Resolution.SuggestionCode);
        Assert.Null(missingCard.RenewalChainSummary);
        Assert.Null(missingCard.CreditSummary);
        Assert.Null(missingCard.CommissionSummary);
        Assert.Single(missingCard.AvailableActions);
        Assert.Contains("CREATE_PERMIT", missingCard.AvailableActions);

        using var closeRenewedResponse = await SendPostAsync(
            $"/api/financials/{renewedPermit.Id}/close",
            adminToken,
            new
            {
                reason = "Cierre terminal para ficha contextual"
            });

        Assert.Equal(HttpStatusCode.OK, closeRenewedResponse.StatusCode);

        using var terminalCardResponse = await SendGetAsync(
            BuildContextCardPath(financialName, institutionOrDependency, renewedStand),
            readOnlyToken);
        var terminalCard = await terminalCardResponse.Content.ReadFromJsonAsync<FinancialContextCardTestResponse>();

        Assert.Equal(HttpStatusCode.OK, terminalCardResponse.StatusCode);
        Assert.NotNull(terminalCard);
        Assert.Equal("REVIEW_TERMINAL_CHAIN", terminalCard!.Resolution.SuggestionCode);
        Assert.Contains("VIEW_CURRENT_PERMIT", terminalCard.AvailableActions);
        Assert.Contains("VIEW_CHAIN", terminalCard.AvailableActions);
        Assert.DoesNotContain("CAPTURE_CREDIT", terminalCard.AvailableActions);
        Assert.DoesNotContain("PREPARE_RENEWAL", terminalCard.AvailableActions);
    }

    [Fact]
    public async Task Financial_current_permit_resolution_uses_operational_key_and_current_chain()
    {
        var statusId = await SeedFinancialPermitStatusAsync();
        var adminToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.AdminUserName,
            AuthorizationRegressionWebApplicationFactory.AdminPassword);
        var readOnlyToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName,
            AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var financialName = $"Financiera Ágil {suffix}";
        const string institutionOrDependency = "Institución Pública";

        using var basePermitResponse = await SendPostAsync(
            "/api/financials",
            adminToken,
            new
            {
                financialName,
                institutionOrDependency,
                placeOrStand = "Módulo Norte",
                validFrom = today,
                validTo = today.AddDays(90),
                schedule = "09:00-14:00",
                negotiatedTerms = "Terminos de resolucion",
                statusCatalogEntryId = statusId,
                notes = "Permiso base para resolver vigente"
            });
        var basePermit = await basePermitResponse.Content.ReadFromJsonAsync<FinancialPermitSummaryTestResponse>();

        Assert.Equal(HttpStatusCode.Created, basePermitResponse.StatusCode);
        Assert.NotNull(basePermit);

        using var foundResponse = await SendGetAsync(
            BuildCurrentPermitResolutionPath($" financiera agil {suffix} ", " institucion   publica ", " modulo norte "),
            readOnlyToken);
        var found = await foundResponse.Content.ReadFromJsonAsync<FinancialPermitContextResolutionTestResponse>();

        Assert.Equal(HttpStatusCode.OK, foundResponse.StatusCode);
        Assert.NotNull(found);
        Assert.Equal("USE_CURRENT_PERMIT", found!.SuggestionCode);
        Assert.NotNull(found.CurrentPermit);
        Assert.Null(found.LastKnownPermit);
        Assert.Equal(basePermit!.Id, found.CurrentPermit!.PermitId);
        Assert.Equal(basePermit.CurrentRootPermitId, found.CurrentRootPermitId);
        Assert.Equal(0, found.CurrentPermit.RenewalSequence);
        Assert.Equal("IN_PROCESS", found.CurrentPermit.StatusCode);
        Assert.Contains("Módulo Norte", found.CurrentPermit.Summary);
        Assert.Contains(basePermit.Id.ToString(), found.RouteHint);

        using var renewResponse = await SendPostAsync(
            $"/api/financials/{basePermit.Id}/renew",
            adminToken,
            new
            {
                validFrom = today.AddDays(91),
                validTo = today.AddDays(180),
                placeOrStand = "Módulo Sur",
                schedule = "10:00-15:00",
                negotiatedTerms = "Terminos renovados de resolucion",
                notes = "Renovacion para resolver vigente"
            });
        var renewedPermit = await renewResponse.Content.ReadFromJsonAsync<FinancialPermitDetailTestResponse>();

        Assert.Equal(HttpStatusCode.Created, renewResponse.StatusCode);
        Assert.NotNull(renewedPermit);

        using var renewedFoundResponse = await SendGetAsync(
            BuildCurrentPermitResolutionPath(financialName, institutionOrDependency, "modulo   sur"),
            readOnlyToken);
        var renewedFound = await renewedFoundResponse.Content.ReadFromJsonAsync<FinancialPermitContextResolutionTestResponse>();

        Assert.Equal(HttpStatusCode.OK, renewedFoundResponse.StatusCode);
        Assert.NotNull(renewedFound);
        Assert.Equal("USE_CURRENT_PERMIT", renewedFound!.SuggestionCode);
        Assert.NotNull(renewedFound.CurrentPermit);
        Assert.Equal(renewedPermit!.Id, renewedFound.CurrentPermit!.PermitId);
        Assert.Equal(basePermit.CurrentRootPermitId, renewedFound.CurrentRootPermitId);
        Assert.Equal(1, renewedFound.CurrentPermit.RenewalSequence);

        using var contextualCreditResponse = await SendPostAsync(
            $"/api/financials/{renewedFound.CurrentPermit.PermitId}/credits",
            adminToken,
            new
            {
                promoterContactId = (Guid?)null,
                promoterName = $"Promotor contextual {suffix}",
                beneficiaryContactId = (Guid?)null,
                beneficiaryName = $"Beneficiario contextual {suffix}",
                phoneNumber = "5550303",
                whatsAppPhone = "5550303",
                authorizationDate = today.AddDays(10),
                amount = 1500m,
                notes = "Credito iniciado desde contexto operativo"
            });
        var contextualCredit = await contextualCreditResponse.Content.ReadFromJsonAsync<FinancialCreditTestResponse>();

        Assert.Equal(HttpStatusCode.Created, contextualCreditResponse.StatusCode);
        Assert.NotNull(contextualCredit);
        Assert.Equal(renewedPermit.Id, contextualCredit!.FinancialPermitId);

        using var historicalContextResponse = await SendGetAsync(
            BuildCurrentPermitResolutionPath(financialName, institutionOrDependency, "modulo norte"),
            readOnlyToken);
        var historicalContext = await historicalContextResponse.Content.ReadFromJsonAsync<FinancialPermitContextResolutionTestResponse>();

        Assert.Equal(HttpStatusCode.OK, historicalContextResponse.StatusCode);
        Assert.NotNull(historicalContext);
        Assert.Equal("RENEW_LAST_PERMIT", historicalContext!.SuggestionCode);
        Assert.Null(historicalContext.CurrentPermit);
        Assert.NotNull(historicalContext.LastKnownPermit);
        Assert.Equal(basePermit.Id, historicalContext.LastKnownPermit!.PermitId);
        Assert.Equal(basePermit.CurrentRootPermitId, historicalContext.CurrentRootPermitId);
        Assert.Contains("chain=1", historicalContext.RouteHint);

        using var missingResponse = await SendGetAsync(
            BuildCurrentPermitResolutionPath(financialName, institutionOrDependency, "Modulo inexistente"),
            readOnlyToken);
        var missing = await missingResponse.Content.ReadFromJsonAsync<FinancialPermitContextResolutionTestResponse>();

        Assert.Equal(HttpStatusCode.OK, missingResponse.StatusCode);
        Assert.NotNull(missing);
        Assert.Equal("CREATE_NEW_PERMIT", missing!.SuggestionCode);
        Assert.Null(missing.CurrentPermit);
        Assert.Null(missing.LastKnownPermit);
        Assert.Null(missing.CurrentRootPermitId);
        Assert.Contains("nuevo oficio", missing.SuggestionMessage);

        using var closeRenewedResponse = await SendPostAsync(
            $"/api/financials/{renewedPermit.Id}/close",
            adminToken,
            new
            {
                reason = "Cierre terminal para validar resolucion contextual"
            });

        Assert.Equal(HttpStatusCode.OK, closeRenewedResponse.StatusCode);

        using var terminalContextResponse = await SendGetAsync(
            BuildCurrentPermitResolutionPath(financialName, institutionOrDependency, "modulo sur"),
            readOnlyToken);
        var terminalContext = await terminalContextResponse.Content.ReadFromJsonAsync<FinancialPermitContextResolutionTestResponse>();

        Assert.Equal(HttpStatusCode.OK, terminalContextResponse.StatusCode);
        Assert.NotNull(terminalContext);
        Assert.Equal("REVIEW_TERMINAL_CHAIN", terminalContext!.SuggestionCode);
        Assert.Null(terminalContext.CurrentPermit);
        Assert.NotNull(terminalContext.LastKnownPermit);
        Assert.Equal(renewedPermit.Id, terminalContext.LastKnownPermit!.PermitId);
        Assert.Equal(basePermit.CurrentRootPermitId, terminalContext.LastKnownPermit.CurrentRootPermitId);
        Assert.Contains("chain=1", terminalContext.RouteHint);
    }

    private async Task<int> SeedFinancialPermitStatusAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

        var status = await dbContext.ModuleStatusCatalogEntries
            .SingleOrDefaultAsync(
                item => item.ModuleCode == "FINANCIALS"
                    && item.ContextCode == "FINANCIAL_PERMIT"
                    && item.StatusCode == "IN_PROCESS");
        if (status is null)
        {
            status = new ModuleStatusCatalogEntry(
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
            dbContext.ModuleStatusCatalogEntries.Add(status);
        }

        var closedStatusExists = await dbContext.ModuleStatusCatalogEntries
            .AnyAsync(
                item => item.ModuleCode == "FINANCIALS"
                    && item.ContextCode == "FINANCIAL_PERMIT"
                    && item.StatusCode == "CLOSED");
        if (!closedStatusExists)
        {
            dbContext.ModuleStatusCatalogEntries.Add(
                new ModuleStatusCatalogEntry(
                    "FINANCIALS",
                    "Financieras",
                    "FINANCIAL_PERMIT",
                    "Oficio o autorizacion",
                    "CLOSED",
                    "Cerrado",
                    description: null,
                    sortOrder: 2,
                    isClosed: true,
                    alertsEnabledByDefault: false));
        }

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

    private static string BuildCurrentPermitResolutionPath(
        string financialName,
        string institutionOrDependency,
        string placeOrStand)
    {
        return "/api/financials/current-permit"
            + $"?financialName={Uri.EscapeDataString(financialName)}"
            + $"&institutionOrDependency={Uri.EscapeDataString(institutionOrDependency)}"
            + $"&placeOrStand={Uri.EscapeDataString(placeOrStand)}";
    }

    private static string BuildContextCardPath(
        string financialName,
        string institutionOrDependency,
        string placeOrStand)
    {
        return "/api/financials/context-card"
            + $"?financialName={Uri.EscapeDataString(financialName)}"
            + $"&institutionOrDependency={Uri.EscapeDataString(institutionOrDependency)}"
            + $"&placeOrStand={Uri.EscapeDataString(placeOrStand)}";
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

    private sealed record FinancialPermitActiveConflictTestResponse(
        string Message,
        string ReasonCode,
        Guid ConflictingPermitId,
        Guid CurrentRootPermitId);

    private sealed record FinancialPermitContextResolutionTestResponse(
        Guid? CurrentRootPermitId,
        FinancialPermitContextPermitTestResponse? CurrentPermit,
        FinancialPermitContextPermitTestResponse? LastKnownPermit,
        string SuggestionCode,
        string SuggestionMessage,
        string RouteHint);

    private sealed record FinancialPermitContextPermitTestResponse(
        Guid PermitId,
        Guid CurrentRootPermitId,
        Guid? RenewedFromPermitId,
        bool IsCurrentVersion,
        int RenewalSequence,
        DateOnly ValidFrom,
        DateOnly ValidTo,
        string StatusCode,
        string Summary);

    private sealed record FinancialContextCardTestResponse(
        FinancialPermitContextResolutionTestResponse Resolution,
        FinancialContextCardRenewalChainSummaryTestResponse? RenewalChainSummary,
        FinancialContextCardCreditSummaryTestResponse? CreditSummary,
        FinancialContextCardCommissionSummaryTestResponse? CommissionSummary,
        List<string> AvailableActions);

    private sealed record FinancialContextCardRenewalChainSummaryTestResponse(
        Guid CurrentPermitId,
        string CurrentPermitSummary,
        int TotalPermitsCount,
        int CurrentRenewalSequence,
        DateOnly? PeriodFrom,
        DateOnly? PeriodTo);

    private sealed record FinancialContextCardCreditSummaryTestResponse(
        int TotalCreditsCount,
        decimal TotalCreditsAmount);

    private sealed record FinancialContextCardCommissionSummaryTestResponse(
        int TotalCommissionsCount,
        decimal TotalCommissionsAmount,
        decimal TotalPromoterCommission,
        decimal TotalAdminCommission,
        decimal TotalThirdPartyCommission);

    private sealed record FinancialPermitRenewalDraftTestResponse(
        Guid SourcePermitId,
        Guid RenewalTargetPermitId,
        Guid CurrentRootPermitId,
        bool SourceIsCurrentVersion,
        int SourceRenewalSequence,
        int RenewalTargetSequence,
        string FinancialName,
        string InstitutionOrDependency,
        string PlaceOrStand,
        string Schedule,
        string NegotiatedTerms,
        string? Notes,
        DateOnly PreviousValidFrom,
        DateOnly PreviousValidTo,
        DateOnly NewStartDate,
        DateOnly NewEndDate,
        int StatusCatalogEntryId,
        string StatusCode,
        string StatusName,
        bool StatusIsClosed,
        string SuggestionCode,
        string SuggestionMessage,
        List<string> FieldsToConfirm);

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
