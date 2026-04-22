using FMCPA.Domain.Entities.Audit;
using FMCPA.Domain.Entities;
using FMCPA.Domain.Entities.Donations;
using FMCPA.Domain.Entities.Documents;
using FMCPA.Domain.Entities.Federation;
using FMCPA.Domain.Entities.Financials;
using FMCPA.Domain.Entities.Markets;
using FMCPA.Domain.Entities.Shared;

namespace FMCPA.Application.Abstractions.Persistence;

public interface IPlatformDbContext
{
    IQueryable<SystemSetting> SystemSettings { get; }
    IQueryable<AuditEvent> AuditEvents { get; }
    IQueryable<StoredDocument> StoredDocuments { get; }
    IQueryable<Contact> Contacts { get; }
    IQueryable<ContactType> ContactTypes { get; }
    IQueryable<ContactParticipation> ContactParticipations { get; }
    IQueryable<CommissionType> CommissionTypes { get; }
    IQueryable<EvidenceType> EvidenceTypes { get; }
    IQueryable<ModuleStatusCatalogEntry> ModuleStatusCatalogEntries { get; }
    IQueryable<Market> Markets { get; }
    IQueryable<MarketTenant> MarketTenants { get; }
    IQueryable<MarketIssue> MarketIssues { get; }
    IQueryable<Donation> Donations { get; }
    IQueryable<DonationApplication> DonationApplications { get; }
    IQueryable<DonationApplicationEvidence> DonationApplicationEvidences { get; }
    IQueryable<FederationAction> FederationActions { get; }
    IQueryable<FederationActionParticipant> FederationActionParticipants { get; }
    IQueryable<FederationDonation> FederationDonations { get; }
    IQueryable<FederationDonationApplication> FederationDonationApplications { get; }
    IQueryable<FederationDonationApplicationEvidence> FederationDonationApplicationEvidences { get; }
    IQueryable<FederationDonationApplicationCommission> FederationDonationApplicationCommissions { get; }
    IQueryable<FinancialPermit> FinancialPermits { get; }
    IQueryable<FinancialCredit> FinancialCredits { get; }
    IQueryable<FinancialCreditCommission> FinancialCreditCommissions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
