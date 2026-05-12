export interface FinancialPermitSummary {
  id: string;
  financialName: string;
  institutionOrDependency: string;
  placeOrStand: string;
  validFrom: string;
  validTo: string;
  schedule: string;
  negotiatedTerms: string;
  statusCatalogEntryId: number;
  statusCode: string;
  statusName: string;
  statusIsClosed: boolean;
  statusAlertsEnabledByDefault: boolean;
  daysUntilExpiration: number;
  alertState: string;
  renewedFromPermitId: string | null;
  currentRootPermitId: string;
  isCurrentVersion: boolean;
  renewalSequence: number;
  creditCount: number;
  commissionCount: number;
  notes: string | null;
  createdUtc: string;
  updatedUtc: string | null;
}

export interface FinancialPermitDetail {
  id: string;
  financialName: string;
  institutionOrDependency: string;
  placeOrStand: string;
  validFrom: string;
  validTo: string;
  schedule: string;
  negotiatedTerms: string;
  statusCatalogEntryId: number;
  statusCode: string;
  statusName: string;
  statusIsClosed: boolean;
  statusAlertsEnabledByDefault: boolean;
  daysUntilExpiration: number;
  alertState: string;
  renewedFromPermitId: string | null;
  currentRootPermitId: string;
  isCurrentVersion: boolean;
  renewalSequence: number;
  notes: string | null;
  createdUtc: string;
  updatedUtc: string | null;
  renewalHistory: FinancialPermitRenewalHistory[];
  credits: FinancialCredit[];
}

export interface FinancialPermitRenewalHistory {
  id: string;
  renewedFromPermitId: string | null;
  currentRootPermitId: string;
  isCurrentVersion: boolean;
  renewalSequence: number;
  validFrom: string;
  validTo: string;
  placeOrStand: string;
  schedule: string;
  statusCode: string;
  statusName: string;
  createdUtc: string;
  updatedUtc: string | null;
}

export interface FinancialPermitRenewalChain {
  currentRootPermitId: string;
  currentPermitId: string;
  currentPermit: FinancialPermitRenewalChainPermit;
  permits: FinancialPermitRenewalChainPermit[];
  summary: FinancialPermitRenewalChainSummary;
  credits: FinancialCredit[];
}

export interface FinancialPermitRenewalChainPermit {
  id: string;
  renewedFromPermitId: string | null;
  currentRootPermitId: string;
  isCurrentVersion: boolean;
  renewalSequence: number;
  financialName: string;
  institutionOrDependency: string;
  placeOrStand: string;
  validFrom: string;
  validTo: string;
  schedule: string;
  statusCatalogEntryId: number;
  statusCode: string;
  statusName: string;
  statusIsClosed: boolean;
  daysUntilExpiration: number;
  alertState: string;
  createdUtc: string;
  updatedUtc: string | null;
}

export interface FinancialPermitRenewalChainSummary {
  permitsCount: number;
  totalCreditsCount: number;
  totalCreditsAmount: number;
  totalCommissionsCount: number;
  totalCommissionsAmount: number;
  totalPromoterCommission: number;
  totalAdminCommission: number;
  totalThirdPartyCommission: number;
  operationFrom: string | null;
  operationTo: string | null;
}

export interface CreateFinancialPermitRequest {
  financialName: string;
  institutionOrDependency: string;
  placeOrStand: string;
  validFrom: string;
  validTo: string;
  schedule: string;
  negotiatedTerms: string;
  statusCatalogEntryId: number;
  notes: string | null;
}

export interface RenewFinancialPermitRequest {
  validFrom: string;
  validTo: string;
  placeOrStand: string | null;
  schedule: string | null;
  negotiatedTerms: string | null;
  notes: string | null;
}

export interface FinancialPermitRenewalDraft {
  sourcePermitId: string;
  renewalTargetPermitId: string;
  currentRootPermitId: string;
  sourceIsCurrentVersion: boolean;
  sourceRenewalSequence: number;
  renewalTargetSequence: number;
  financialName: string;
  institutionOrDependency: string;
  placeOrStand: string;
  schedule: string;
  negotiatedTerms: string;
  notes: string | null;
  previousValidFrom: string;
  previousValidTo: string;
  newStartDate: string;
  newEndDate: string;
  statusCatalogEntryId: number;
  statusCode: string;
  statusName: string;
  statusIsClosed: boolean;
  suggestionCode: FinancialPermitSuggestionCode;
  suggestionMessage: string;
  fieldsToConfirm: string[];
}

export interface FinancialPermitAlert {
  permitId: string;
  financialName: string;
  institutionOrDependency: string;
  placeOrStand: string;
  statusCode: string;
  statusName: string;
  validTo: string;
  daysUntilExpiration: number;
  alertState: string;
}

export interface FinancialPermitActiveConflict {
  message: string;
  reasonCode: string;
  conflictingPermitId: string;
  currentRootPermitId: string;
  financialName: string;
  institutionOrDependency: string;
  placeOrStand: string;
  conflictingValidFrom: string;
  conflictingValidTo: string;
}

export type FinancialPermitSuggestionCode =
  | 'USE_CURRENT_PERMIT'
  | 'RENEW_LAST_PERMIT'
  | 'CREATE_NEW_PERMIT'
  | 'REVIEW_TERMINAL_CHAIN'
  | string;

export interface FinancialPermitContextResolution {
  financialName: string;
  institutionOrDependency: string;
  placeOrStand: string;
  currentPermit: FinancialPermitContextPermit | null;
  currentRootPermitId: string | null;
  lastKnownPermit: FinancialPermitContextPermit | null;
  suggestionCode: FinancialPermitSuggestionCode;
  suggestionMessage: string;
  routeHint: string;
}

export interface FinancialContextCard {
  resolution: FinancialPermitContextResolution;
  renewalChainSummary: FinancialContextCardRenewalChainSummary | null;
  creditSummary: FinancialContextCardCreditSummary | null;
  commissionSummary: FinancialContextCardCommissionSummary | null;
  availableActions: FinancialContextCardAction[];
}

export interface FinancialContextCardRenewalChainSummary {
  currentPermitId: string;
  currentPermitSummary: string;
  totalPermitsCount: number;
  currentRenewalSequence: number;
  periodFrom: string | null;
  periodTo: string | null;
}

export interface FinancialContextCardCreditSummary {
  totalCreditsCount: number;
  totalCreditsAmount: number;
}

export interface FinancialContextCardCommissionSummary {
  totalCommissionsCount: number;
  totalCommissionsAmount: number;
  totalPromoterCommission: number;
  totalAdminCommission: number;
  totalThirdPartyCommission: number;
}

export type FinancialContextCardAction =
  | 'CAPTURE_CREDIT'
  | 'PREPARE_RENEWAL'
  | 'CREATE_PERMIT'
  | 'VIEW_CHAIN'
  | 'VIEW_CURRENT_PERMIT'
  | string;

export interface FinancialPermitContextPermit {
  permitId: string;
  currentRootPermitId: string;
  renewedFromPermitId: string | null;
  isCurrentVersion: boolean;
  renewalSequence: number;
  financialName: string;
  institutionOrDependency: string;
  placeOrStand: string;
  validFrom: string;
  validTo: string;
  schedule: string;
  statusCatalogEntryId: number;
  statusCode: string;
  statusName: string;
  statusIsClosed: boolean;
  daysUntilExpiration: number;
  alertState: string;
  summary: string;
}

export interface FinancialPermitOperationBlocked {
  message: string;
  reasonCode: 'FINANCIAL_PERMIT_NOT_CURRENT' | 'FINANCIAL_PERMIT_TERMINAL' | string;
  permitId: string;
  currentRootPermitId: string;
  currentPermitId: string | null;
}

export interface CreateFinancialCreditRequest {
  promoterContactId: string | null;
  promoterName: string;
  beneficiaryContactId: string | null;
  beneficiaryName: string;
  phoneNumber: string | null;
  whatsAppPhone: string | null;
  authorizationDate: string;
  amount: number;
  notes: string | null;
}

export interface FinancialCredit {
  id: string;
  financialPermitId: string;
  promoterContactId: string | null;
  promoterName: string;
  beneficiaryContactId: string | null;
  beneficiaryName: string;
  phoneNumber: string | null;
  whatsAppPhone: string | null;
  authorizationDate: string;
  amount: number;
  notes: string | null;
  commissionCount: number;
  commissions: FinancialCreditCommission[];
  createdUtc: string;
}

export interface CreateFinancialCreditCommissionRequest {
  commissionTypeId: number;
  recipientCategory: string;
  recipientContactId: string | null;
  recipientName: string;
  baseAmount: number;
  commissionAmount: number;
  notes: string | null;
}

export interface FinancialCreditCommission {
  id: string;
  financialCreditId: string;
  commissionTypeId: number;
  commissionTypeCode: string;
  commissionTypeName: string;
  recipientCategory: string;
  recipientContactId: string | null;
  recipientName: string;
  baseAmount: number;
  commissionAmount: number;
  notes: string | null;
  createdUtc: string;
}
