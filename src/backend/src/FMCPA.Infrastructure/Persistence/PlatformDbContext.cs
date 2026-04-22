using FMCPA.Application.Abstractions.Persistence;
using FMCPA.Domain.Entities.Audit;
using FMCPA.Domain.Entities;
using FMCPA.Domain.Entities.Donations;
using FMCPA.Domain.Entities.Documents;
using FMCPA.Domain.Entities.Federation;
using FMCPA.Domain.Entities.Financials;
using FMCPA.Domain.Entities.Markets;
using FMCPA.Domain.Entities.Shared;
using Microsoft.EntityFrameworkCore;

namespace FMCPA.Infrastructure.Persistence;

public sealed class PlatformDbContext : DbContext, IPlatformDbContext
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options)
        : base(options)
    {
    }

    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<StoredDocument> StoredDocuments => Set<StoredDocument>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<ContactType> ContactTypes => Set<ContactType>();
    public DbSet<ContactParticipation> ContactParticipations => Set<ContactParticipation>();
    public DbSet<CommissionType> CommissionTypes => Set<CommissionType>();
    public DbSet<EvidenceType> EvidenceTypes => Set<EvidenceType>();
    public DbSet<ModuleStatusCatalogEntry> ModuleStatusCatalogEntries => Set<ModuleStatusCatalogEntry>();
    public DbSet<Market> Markets => Set<Market>();
    public DbSet<MarketTenant> MarketTenants => Set<MarketTenant>();
    public DbSet<MarketIssue> MarketIssues => Set<MarketIssue>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<DonationApplication> DonationApplications => Set<DonationApplication>();
    public DbSet<DonationApplicationEvidence> DonationApplicationEvidences => Set<DonationApplicationEvidence>();
    public DbSet<FederationAction> FederationActions => Set<FederationAction>();
    public DbSet<FederationActionParticipant> FederationActionParticipants => Set<FederationActionParticipant>();
    public DbSet<FederationDonation> FederationDonations => Set<FederationDonation>();
    public DbSet<FederationDonationApplication> FederationDonationApplications => Set<FederationDonationApplication>();
    public DbSet<FederationDonationApplicationEvidence> FederationDonationApplicationEvidences => Set<FederationDonationApplicationEvidence>();
    public DbSet<FederationDonationApplicationCommission> FederationDonationApplicationCommissions => Set<FederationDonationApplicationCommission>();
    public DbSet<FinancialPermit> FinancialPermits => Set<FinancialPermit>();
    public DbSet<FinancialCredit> FinancialCredits => Set<FinancialCredit>();
    public DbSet<FinancialCreditCommission> FinancialCreditCommissions => Set<FinancialCreditCommission>();

    IQueryable<SystemSetting> IPlatformDbContext.SystemSettings => SystemSettings.AsQueryable();
    IQueryable<AuditEvent> IPlatformDbContext.AuditEvents => AuditEvents.AsQueryable();
    IQueryable<StoredDocument> IPlatformDbContext.StoredDocuments => StoredDocuments.AsQueryable();
    IQueryable<Contact> IPlatformDbContext.Contacts => Contacts.AsQueryable();
    IQueryable<ContactType> IPlatformDbContext.ContactTypes => ContactTypes.AsQueryable();
    IQueryable<ContactParticipation> IPlatformDbContext.ContactParticipations => ContactParticipations.AsQueryable();
    IQueryable<CommissionType> IPlatformDbContext.CommissionTypes => CommissionTypes.AsQueryable();
    IQueryable<EvidenceType> IPlatformDbContext.EvidenceTypes => EvidenceTypes.AsQueryable();
    IQueryable<ModuleStatusCatalogEntry> IPlatformDbContext.ModuleStatusCatalogEntries => ModuleStatusCatalogEntries.AsQueryable();
    IQueryable<Market> IPlatformDbContext.Markets => Markets.AsQueryable();
    IQueryable<MarketTenant> IPlatformDbContext.MarketTenants => MarketTenants.AsQueryable();
    IQueryable<MarketIssue> IPlatformDbContext.MarketIssues => MarketIssues.AsQueryable();
    IQueryable<Donation> IPlatformDbContext.Donations => Donations.AsQueryable();
    IQueryable<DonationApplication> IPlatformDbContext.DonationApplications => DonationApplications.AsQueryable();
    IQueryable<DonationApplicationEvidence> IPlatformDbContext.DonationApplicationEvidences => DonationApplicationEvidences.AsQueryable();
    IQueryable<FederationAction> IPlatformDbContext.FederationActions => FederationActions.AsQueryable();
    IQueryable<FederationActionParticipant> IPlatformDbContext.FederationActionParticipants => FederationActionParticipants.AsQueryable();
    IQueryable<FederationDonation> IPlatformDbContext.FederationDonations => FederationDonations.AsQueryable();
    IQueryable<FederationDonationApplication> IPlatformDbContext.FederationDonationApplications => FederationDonationApplications.AsQueryable();
    IQueryable<FederationDonationApplicationEvidence> IPlatformDbContext.FederationDonationApplicationEvidences => FederationDonationApplicationEvidences.AsQueryable();
    IQueryable<FederationDonationApplicationCommission> IPlatformDbContext.FederationDonationApplicationCommissions => FederationDonationApplicationCommissions.AsQueryable();
    IQueryable<FinancialPermit> IPlatformDbContext.FinancialPermits => FinancialPermits.AsQueryable();
    IQueryable<FinancialCredit> IPlatformDbContext.FinancialCredits => FinancialCredits.AsQueryable();
    IQueryable<FinancialCreditCommission> IPlatformDbContext.FinancialCreditCommissions => FinancialCreditCommissions.AsQueryable();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlatformDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
