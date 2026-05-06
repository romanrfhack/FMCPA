import type { DocumentSummary } from './document-catalog.models';
import type { SecurityActivitySummary } from './security-observability.models';

export type OperationsSeverityCode = 'HIGH' | 'MEDIUM' | 'LOW' | string;
export type OperationsCategoryCode = 'BUSINESS' | 'DOCUMENTS' | 'SECURITY' | string;

export interface OperationsSummary {
  generatedAtUtc: string;
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
