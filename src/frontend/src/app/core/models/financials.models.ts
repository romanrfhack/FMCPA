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
  notes: string | null;
  createdUtc: string;
  updatedUtc: string | null;
  credits: FinancialCredit[];
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
