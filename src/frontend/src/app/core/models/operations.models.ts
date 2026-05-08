import type { DocumentSummary } from './document-catalog.models';
import type { SecurityActivitySummary } from './security-observability.models';

export type OperationsSeverityCode = 'HIGH' | 'MEDIUM' | 'LOW' | string;
export type OperationsCategoryCode = 'BUSINESS' | 'DOCUMENTS' | 'SECURITY' | string;
export type OperationsActionKind =
  | 'VIEW'
  | 'REMEDIATE'
  | 'REVIEW'
  | 'SECURITY_ADMIN'
  | 'DOCUMENTS_QUEUE'
  | string;

export interface OperationsSummary {
  generatedAtUtc: string;
  timeWindow: OperationsTimeWindow;
  severityConvention: OperationsSeverityConvention[];
  businessKpis: OperationsKpi[];
  documents: DocumentSummary | null;
  security: SecurityActivitySummary | null;
}

export interface OperationsKpi {
  kpiCode: string;
  categoryCode: OperationsCategoryCode;
  categoryName: string;
  moduleCode: string | null;
  moduleName: string | null;
  label: string;
  count: number;
  amount: number | null;
  severityCode: OperationsSeverityCode;
  routeHint: string;
}

export interface OperationsWorkQueueResponse {
  totalCount: number;
  returnedCount: number;
  skip: number;
  take: number;
  timeWindow: OperationsTimeWindow;
  severityConvention: OperationsSeverityConvention[];
  items: OperationsWorkQueueItem[];
}

export interface OperationsWorkQueueItem {
  workItemKey: string;
  categoryCode: OperationsCategoryCode;
  typeCode: string;
  severityCode: OperationsSeverityCode;
  moduleCode: string;
  moduleName: string;
  title: string;
  summary: string;
  reasonCode: string;
  routeHint: string;
  actionKind: OperationsActionKind;
  actionLabel: string;
  contextLabel: string | null;
  quickActionCode: 'UNLOCK_USER' | string | null;
  quickActionLabel: string | null;
  sourceItemKey: string | null;
  entityType: string | null;
  entityId: string | null;
  documentId: string | null;
  relevantUtc: string | null;
}

export interface OperationsSeverityConvention {
  severityCode: OperationsSeverityCode;
  sortOrder: number;
  description: string;
}

export type OperationsTimeWindowCode = 'ALL' | 'TODAY' | 'LAST_7_DAYS' | 'NEXT_30_DAYS' | 'CUSTOM' | string;

export interface OperationsTimeWindow {
  timeWindowCode: OperationsTimeWindowCode;
  fromUtc: string | null;
  toUtc: string | null;
  isApplied: boolean;
  description: string;
}
