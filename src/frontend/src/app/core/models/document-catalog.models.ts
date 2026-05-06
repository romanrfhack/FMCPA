export interface DocumentCatalogFilters {
  moduleCode?: string | null;
  entityType?: string | null;
  entityId?: string | null;
  documentAreaCode?: string | null;
  integrityState?: string | null;
  documentOperationalStatusCode?: string | null;
  documentClassCode?: string | null;
  retentionPolicyCode?: string | null;
  retentionStatusCode?: string | null;
  statusCode?: string | null;
  includeArchived?: boolean | null;
  fromUtc?: string | null;
  toUtc?: string | null;
  skip?: number | null;
  take?: number | null;
}

export interface DocumentCatalogListResponse {
  totalCount: number;
  returnedCount: number;
  skip: number;
  take: number;
  items: DocumentCatalogItem[];
}

export interface DocumentCompletenessFilters {
  moduleCode?: string | null;
  skip?: number | null;
  take?: number | null;
}

export interface DocumentWorkQueueFilters {
  moduleCode?: string | null;
  workItemType?: string | null;
  severityCode?: string | null;
  skip?: number | null;
  take?: number | null;
}

export interface DocumentWorkQueueResponse {
  totalCount: number;
  returnedCount: number;
  skip: number;
  take: number;
  items: DocumentWorkQueueItem[];
}

export interface DocumentWorkQueueItem {
  workItemKey: string;
  workItemType: 'COMPLETENESS_PENDING' | 'DOCUMENT_INTEGRITY_ISSUE' | 'RETENTION_REVIEW' | string;
  severityCode: 'HIGH' | 'MEDIUM' | 'LOW' | string;
  moduleCode: string;
  moduleName: string;
  entityType: string;
  entityId: string;
  originContext: DocumentOriginContext;
  documentId: string | null;
  documentOriginalFileName: string | null;
  title: string;
  summary: string;
  reasonCode: string;
  currentStatusCode: string;
  documentOperationalStatusCode: DocumentOperationalStatusCode | null;
  documentOperationalSeverityCode: DocumentOperationalSeverityCode | null;
  routeHint: string;
  documentDetailUrl: string | null;
  remediationHint: string | null;
}

export interface DocumentSummary {
  totalDocuments: number;
  activeDocuments: number;
  archivedDocuments: number;
  integrityIssuesCount: number;
  incompleteEntitiesCount: number;
  reviewDueCount: number;
  expiredRetentionCount: number;
  administrativeHoldCount: number;
  modules: DocumentSummaryModule[];
  documentClasses: DocumentSummaryClass[];
  operationalStatuses: DocumentSummaryOperationalStatus[];
  workQueueCategories: DocumentSummaryWorkQueueCategory[];
}

export interface DocumentSummaryModule {
  moduleCode: string;
  moduleName: string;
  totalDocuments: number;
  activeDocuments: number;
  archivedDocuments: number;
  integrityIssuesCount: number;
  incompleteEntitiesCount: number;
  reviewDueCount: number;
  expiredRetentionCount: number;
  administrativeHoldCount: number;
}

export interface DocumentSummaryClass {
  documentClassCode: string;
  totalDocuments: number;
  activeDocuments: number;
  archivedDocuments: number;
}

export interface DocumentSummaryOperationalStatus {
  documentOperationalStatusCode: DocumentOperationalStatusCode;
  documentOperationalSeverityCode: DocumentOperationalSeverityCode;
  totalCount: number;
}

export interface DocumentSummaryWorkQueueCategory {
  workItemType: string;
  reasonCode: string;
  severityCode: string;
  totalCount: number;
}

export interface DocumentCompletenessListResponse {
  totalCount: number;
  returnedCount: number;
  skip: number;
  take: number;
  items: DocumentCompleteness[];
}

export interface DocumentRuleListResponse {
  totalCount: number;
  items: DocumentRule[];
}

export interface DocumentRule {
  ruleCode: string;
  moduleCode: string;
  moduleName: string;
  entityType: string;
  coveredDocumentEntityType: string;
  documentAreaCode: string;
  requiredDocumentClassCodes: string[];
  minimumRequiredCount: number;
  requiredDocumentCode: string;
  requiredDocumentDescription: string;
  missingReasonCode: string;
  missingReasonDescription: string;
  remediationHint: string;
}

export interface DocumentRequirement {
  moduleCode: string;
  moduleName: string;
  entityType: string;
  entityId: string;
  originContext: DocumentOriginContext;
  appliesRule: boolean;
  ruleCode: string | null;
  statusCode: 'COMPLETE' | 'INCOMPLETE' | 'NOT_APPLICABLE' | string;
  isComplete: boolean | null;
  requiredDocumentCode: string | null;
  requiredDocumentDescription: string | null;
  requiredDocumentClassCodes: string[];
  minimumRequiredCount: number;
  currentDocumentCount: number;
  missingReasonCode: 'MISSING_REQUIRED_DOCUMENT' | 'MISSING_EVIDENCE' | string | null;
  missingReasonDescription: string | null;
  remediationHint: string | null;
  coveringDocumentIds: string[];
}

export interface DocumentCompleteness {
  moduleCode: string;
  moduleName: string;
  entityType: string;
  entityId: string;
  originContext: DocumentOriginContext;
  ruleCode: string;
  statusCode: 'COMPLETE' | 'INCOMPLETE' | string;
  isComplete: boolean;
  requiredDocumentCode: string;
  requiredDocumentDescription: string;
  requiredDocumentClassCodes: string[];
  minimumRequiredCount: number;
  missingReasonCode: 'MISSING_REQUIRED_DOCUMENT' | 'MISSING_EVIDENCE' | string | null;
  missingReasonDescription: string | null;
  remediationHint: string;
  relatedDocumentCount: number;
  coveringDocumentIds: string[];
}

export interface DocumentCatalogItem {
  id: string;
  moduleCode: string;
  moduleName: string;
  documentAreaCode: string;
  entityType: string;
  entityId: string;
  originContext: DocumentOriginContext;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  createdUtc: string;
  documentClassCode: 'CERTIFICATE' | 'SIGNED_DOCUMENT' | 'SUPPORTING_DOCUMENT' | 'PHOTO_EVIDENCE' | 'VIDEO_EVIDENCE' | 'OTHER' | string;
  businessPurpose: string | null;
  isPrimaryDocument: boolean;
  classificationNotes: string | null;
  retentionPolicyCode: 'CERTIFICATE_REVIEW' | 'SIGNED_LONG_TERM' | 'EVIDENCE_MEDIUM_TERM' | 'GENERIC_REVIEW' | string;
  retentionUntilUtc: string;
  retentionStatusCode: 'ACTIVE_RETENTION' | 'REVIEW_DUE' | 'EXPIRED_RETENTION' | string;
  retentionBaselinePolicyCode: string;
  retentionBaselineUntilUtc: string;
  retentionEffectivePolicyCode: string;
  retentionEffectiveUntilUtc: string;
  hasRetentionOverride: boolean;
  retentionOverridePolicyCode: string | null;
  retentionOverrideUntilUtc: string | null;
  retentionOverrideReason: string | null;
  retentionReviewStatusCode: 'REVIEW_PENDING' | 'REVIEW_COMPLETED' | 'REVIEW_DEFERRED' | string;
  lastRetentionReviewUtc: string | null;
  nextRetentionReviewUtc: string | null;
  retentionReviewNotes: string | null;
  isAdministrativeHold: boolean;
  holdReason: string | null;
  holdPlacedUtc: string | null;
  holdReleasedUtc: string | null;
  holdPlacedBy: string | null;
  documentOperationalStatusCode: DocumentOperationalStatusCode;
  documentOperationalSeverityCode: DocumentOperationalSeverityCode;
  statusCode: 'ACTIVE' | 'ARCHIVED' | string;
  archivedUtc: string | null;
  archiveReason: string | null;
  isSuperseded: boolean;
  supersededByDocumentId: string | null;
  replacedDocumentId: string | null;
  replacementGroupKey: string;
  integrityState: string;
  actualSizeBytes: number | null;
  hasChecksum: boolean;
  isLegacyBackfill: boolean;
  detailUrl: string;
  downloadUrl: string;
}

export type DocumentOperationalStatusCode =
  | 'ACTIVE_OK'
  | 'INTEGRITY_ISSUE'
  | 'ON_HOLD'
  | 'REVIEW_DUE'
  | 'RETENTION_EXPIRED'
  | 'ARCHIVED'
  | 'SUPERSEDED'
  | string;

export type DocumentOperationalSeverityCode = 'HIGH' | 'MEDIUM' | 'LOW' | 'NONE' | string;

export interface DocumentCatalogDetail extends DocumentCatalogItem {
  sha256Hex: string | null;
}

export interface DocumentTimelineResponse {
  totalCount: number;
  items: DocumentTimelineEvent[];
}

export interface DocumentTimelineEvent {
  auditEventId: string | null;
  documentId: string;
  documentOriginalFileName: string;
  moduleCode: string;
  moduleName: string;
  documentAreaCode: string;
  entityType: string;
  entityId: string;
  occurredUtc: string;
  eventType: string;
  title: string;
  detail: string;
  relatedDocumentId: string | null;
  relatedDocumentOriginalFileName: string | null;
  documentStatusCode: string;
  isCurrentDocument: boolean;
  isSuperseded: boolean;
}

export interface DocumentOriginContext {
  moduleCode: string;
  moduleName: string;
  entityType: string;
  entityId: string;
  displayName: string;
  summary: string | null;
  routeHint: string | null;
}

export interface ArchiveStoredDocumentRequest {
  reason: string | null;
}

export interface SetStoredDocumentHoldRequest {
  reason: string | null;
}

export interface UpdateStoredDocumentMetadataRequest {
  documentClassCode: string;
  businessPurpose: string | null;
  isPrimaryDocument: boolean;
  classificationNotes: string | null;
}

export interface DocumentRetentionReviewQueueFilters {
  moduleCode?: string | null;
  retentionReviewStatusCode?: string | null;
  skip?: number | null;
  take?: number | null;
}

export interface UpdateStoredDocumentRetentionReviewRequest {
  retentionReviewStatusCode: 'REVIEW_COMPLETED' | 'REVIEW_DEFERRED' | string;
  nextRetentionReviewUtc: string | null;
  retentionReviewNotes: string | null;
}

export interface UpdateStoredDocumentRetentionOverrideRequest {
  retentionOverridePolicyCode: string | null;
  retentionOverrideUntilUtc: string | null;
  retentionOverrideReason: string | null;
}
