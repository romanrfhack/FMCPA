export interface DashboardSummary {
  markets: DashboardMarketsSummary;
  donations: DashboardDonationsSummary;
  financials: DashboardFinancialsSummary;
  federation: DashboardFederationSummary;
  totals: DashboardTotalsSummary;
}

export interface DashboardMarketsSummary {
  totalMarkets: number;
  activeMarkets: number;
  closedOrArchivedMarkets: number;
  tenantCount: number;
  activeIssueCount: number;
  certificateAlertCount: number;
}

export interface DashboardDonationsSummary {
  totalDonations: number;
  notAppliedCount: number;
  partiallyAppliedCount: number;
  closedDonationsCount: number;
  baseAmountTotal: number;
  appliedAmountTotal: number;
  activeAlertCount: number;
}

export interface DashboardFinancialsSummary {
  totalPermits: number;
  closedPermitsCount: number;
  creditCount: number;
  commissionCount: number;
  dueSoonCount: number;
  expiredCount: number;
  renewalCount: number;
  activeAlertCount: number;
}

export interface DashboardFederationSummary {
  totalActions: number;
  closedActionsCount: number;
  actionAlertCount: number;
  totalDonations: number;
  closedDonationsCount: number;
  donationAlertCount: number;
  commissionCount: number;
  evidenceCount: number;
}

export interface DashboardTotalsSummary {
  activeAlertCount: number;
  closedRecordsCount: number;
  commissionCount: number;
  evidenceCount: number;
}

export interface DashboardAlertItem {
  alertKey: string;
  moduleCode: string;
  moduleName: string;
  category: string;
  title: string;
  subtitle: string;
  detail: string;
  alertState: string;
  reference: string | null;
  relevantDate: string | null;
  daysUntilTarget: number | null;
  navigationPath: string;
}

export interface DashboardAlerts {
  marketCertificates: DashboardAlertItem[];
  donations: DashboardAlertItem[];
  financialPermits: DashboardAlertItem[];
  federationActions: DashboardAlertItem[];
  federationDonations: DashboardAlertItem[];
}

export interface ConsolidatedCommissionList {
  items: ConsolidatedCommissionItem[];
  totalCount: number;
  totalBaseAmount: number;
  totalCommissionAmount: number;
}

export interface ConsolidatedCommissionItem {
  commissionId: string;
  sourceModuleCode: string;
  sourceModuleName: string;
  originEntityType: string;
  originEntityId: string;
  operationDate: string;
  commissionTypeId: number;
  commissionTypeCode: string;
  commissionTypeName: string;
  recipientCategory: 'COMPANY' | 'THIRD_PARTY' | 'OTHER_PARTICIPANT' | string;
  recipientName: string;
  baseAmount: number;
  commissionAmount: number;
  originReference: string;
  originPrimaryName: string;
  originSecondaryName: string;
  notes: string | null;
  createdUtc: string;
  navigationPath: string;
}

export interface BitacoraEntry {
  eventKey: string;
  occurredUtc: string;
  moduleCode: string;
  moduleName: string;
  entityType: string;
  entityId: string;
  actionType: string;
  title: string;
  detail: string;
  relatedStatusCode: string | null;
  isCloseEvent: boolean;
  closeEventSource: 'FORMAL_CLOSE_EVENT' | 'LEGACY_CLOSE_NORMALIZED' | null | string;
  reference: string | null;
  navigationPath: string;
  metadataJson: string | null;
}

export interface ClosedItem {
  recordKey: string;
  moduleCode: string;
  moduleName: string;
  itemType: string;
  itemId: string;
  title: string;
  subtitle: string;
  reference: string | null;
  statusCode: string;
  statusName: string;
  historicalTimestampUtc: string;
  historicalTimestampSource: 'FORMAL_CLOSE_EVENT' | 'LEGACY_CLOSE_NORMALIZED' | 'LEGACY_TIMESTAMP_FALLBACK' | string;
  hasFormalCloseEvent: boolean;
  navigationPath: string;
}

export interface CloseRecordRequest {
  reason: string | null;
}

export interface CloseRecordResponse {
  eventId: string;
  moduleCode: string;
  moduleName: string;
  entityType: string;
  entityId: string;
  statusCode: string;
  statusName: string;
  closedUtc: string;
  reason: string | null;
}

export interface DocumentIntegritySummary {
  totalDocumentRecords: number;
  validCount: number;
  missingFileCount: number;
  sizeMismatchCount: number;
  invalidPathCount: number;
  missingDocumentRecordCount: number;
  orphanedDocumentRecordCount: number;
  metadataMismatchCount: number;
}

export interface DocumentIntegrityIssue {
  issueKey: string;
  moduleCode: string;
  moduleName: string;
  documentAreaCode: string;
  entityType: string;
  entityId: string;
  integrityState: string;
  title: string;
  detail: string;
  originalFileName: string | null;
  storedRelativePath: string | null;
  navigationPath: string;
}

export interface DocumentRecord {
  documentKey: string;
  moduleCode: string;
  moduleName: string;
  documentAreaCode: string;
  entityType: string;
  entityId: string;
  originalFileName: string;
  storedRelativePath: string;
  contentType: string;
  sizeBytes: number;
  actualSizeBytes: number | null;
  integrityState: string;
  hasDocumentRecord: boolean;
  isLegacyBackfill: boolean;
  createdUtc: string;
  sha256Hex: string | null;
  navigationPath: string;
}

export interface DocumentIntegrityResponse {
  summary: DocumentIntegritySummary;
  issues: DocumentIntegrityIssue[];
  records: DocumentRecord[];
}
