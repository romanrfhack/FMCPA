using FMCPA.Application.Abstractions.Storage;
using FMCPA.Domain.Entities.Documents;

namespace FMCPA.Api.DocumentRules;

public sealed record DocumentRuleDescriptor(
    string RuleCode,
    string ModuleCode,
    string EntityType,
    string CoveredDocumentEntityType,
    string DocumentAreaCode,
    IReadOnlyList<string> RequiredDocumentClassCodes,
    int MinimumRequiredCount,
    string RequiredDocumentCode,
    string RequiredDocumentDescription,
    string MissingReasonCode,
    string MissingReasonDescription,
    string RemediationHint);

public static class DocumentRuleRegistry
{
    public const string MarketTenantEntityType = "MARKET_TENANT";
    public const string DonationApplicationEntityType = "DONATION_APPLICATION";
    public const string DonationEvidenceEntityType = "DONATION_APPLICATION_EVIDENCE";
    public const string FederationDonationApplicationEntityType = "FEDERATION_DONATION_APPLICATION";
    public const string FederationDonationEvidenceEntityType = "FEDERATION_DONATION_APPLICATION_EVIDENCE";

    public const string MarketTenantCertificateRuleCode = "MARKET_TENANT_CERTIFICATE_REQUIRED";
    public const string DonationApplicationEvidenceRuleCode = "DONATION_APPLICATION_EVIDENCE_REQUIRED";
    public const string FederationDonationApplicationEvidenceRuleCode = "FEDERATION_DONATION_APPLICATION_EVIDENCE_REQUIRED";

    private static readonly string[] AnyEvidenceClassCodes =
    [
        DocumentClassCodes.Certificate,
        DocumentClassCodes.SignedDocument,
        DocumentClassCodes.SupportingDocument,
        DocumentClassCodes.PhotoEvidence,
        DocumentClassCodes.VideoEvidence,
        DocumentClassCodes.Other
    ];

    private static readonly DocumentRuleDescriptor[] Rules =
    [
        new(
            MarketTenantCertificateRuleCode,
            "MARKETS",
            MarketTenantEntityType,
            MarketTenantEntityType,
            DocumentAreaCodes.MarketsTenantCertificates,
            [DocumentClassCodes.Certificate],
            MinimumRequiredCount: 1,
            RequiredDocumentCode: "MARKET_TENANT_CERTIFICATE",
            RequiredDocumentDescription: "Cedula o certificado activo del locatario.",
            MissingReasonCode: "MISSING_REQUIRED_DOCUMENT",
            MissingReasonDescription: "El locatario no tiene cedula/certificado activo clasificado como CERTIFICATE.",
            RemediationHint: "Carga la cedula o certificado desde la seccion del locatario en Mercados."),
        new(
            DonationApplicationEvidenceRuleCode,
            "DONATARIAS",
            DonationApplicationEntityType,
            DonationEvidenceEntityType,
            DocumentAreaCodes.DonationsApplicationEvidences,
            AnyEvidenceClassCodes,
            MinimumRequiredCount: 1,
            RequiredDocumentCode: "DONATION_APPLICATION_EVIDENCE",
            RequiredDocumentDescription: "Al menos una evidencia documental activa asociada a la aplicacion.",
            MissingReasonCode: "MISSING_EVIDENCE",
            MissingReasonDescription: "La aplicacion no tiene evidencia documental activa en StoredDocument.",
            RemediationHint: "Agrega una evidencia desde la seccion de evidencias de la aplicacion en Donatarias."),
        new(
            FederationDonationApplicationEvidenceRuleCode,
            "FEDERATION",
            FederationDonationApplicationEntityType,
            FederationDonationEvidenceEntityType,
            DocumentAreaCodes.FederationApplicationEvidences,
            AnyEvidenceClassCodes,
            MinimumRequiredCount: 1,
            RequiredDocumentCode: "FEDERATION_DONATION_APPLICATION_EVIDENCE",
            RequiredDocumentDescription: "Al menos una evidencia documental activa asociada a la aplicacion federacion.",
            MissingReasonCode: "MISSING_EVIDENCE",
            MissingReasonDescription: "La aplicacion federacion no tiene evidencia documental activa en StoredDocument.",
            RemediationHint: "Agrega una evidencia desde la seccion de evidencias de la aplicacion en Federacion.")
    ];

    public static IReadOnlyList<DocumentRuleDescriptor> ActiveRules => Rules;

    public static DocumentRuleDescriptor? FindByEntity(string moduleCode, string entityType)
    {
        return Rules.SingleOrDefault(rule =>
            string.Equals(rule.ModuleCode, moduleCode, StringComparison.Ordinal)
            && string.Equals(rule.EntityType, entityType, StringComparison.Ordinal));
    }

    public static IReadOnlyList<DocumentRuleDescriptor> FindByModules(IReadOnlyCollection<string> moduleCodes)
    {
        return Rules
            .Where(rule => moduleCodes.Contains(rule.ModuleCode, StringComparer.Ordinal))
            .ToArray();
    }

    public static string SupportedEntityCombinationsDescription()
    {
        return string.Join(
            ", ",
            Rules.Select(rule => $"{rule.ModuleCode}/{rule.EntityType}"));
    }
}
