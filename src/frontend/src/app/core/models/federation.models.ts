export interface FederationActionSummary {
  id: string;
  actionTypeCode: string;
  actionTypeName: string;
  counterpartyOrInstitution: string;
  actionDate: string;
  objective: string;
  statusCatalogEntryId: number;
  statusCode: string;
  statusName: string;
  statusIsClosed: boolean;
  statusAlertsEnabledByDefault: boolean;
  participantCount: number;
  alertState: string;
  notes: string | null;
  createdUtc: string;
  updatedUtc: string | null;
}

export interface FederationActionDetail {
  id: string;
  actionTypeCode: string;
  actionTypeName: string;
  counterpartyOrInstitution: string;
  actionDate: string;
  objective: string;
  statusCatalogEntryId: number;
  statusCode: string;
  statusName: string;
  statusIsClosed: boolean;
  statusAlertsEnabledByDefault: boolean;
  alertState: string;
  notes: string | null;
  createdUtc: string;
  updatedUtc: string | null;
  participants: FederationActionParticipant[];
}

export interface CreateFederationActionRequest {
  actionTypeCode: string;
  counterpartyOrInstitution: string;
  actionDate: string;
  objective: string;
  statusCatalogEntryId: number;
  notes: string | null;
}

export interface CreateFederationActionParticipantRequest {
  contactId: string;
  participantSide: string;
  notes: string | null;
}

export interface FederationActionParticipant {
  id: string;
  federationActionId: string;
  contactId: string;
  participantSide: string;
  contactTypeCode: string;
  contactTypeName: string;
  participantName: string;
  organizationOrDependency: string | null;
  roleTitle: string | null;
  notes: string | null;
  createdUtc: string;
}

export interface FederationDonationSummary {
  id: string;
  donorName: string;
  donationDate: string;
  donationType: string;
  baseAmount: number;
  reference: string;
  notes: string | null;
  statusCatalogEntryId: number;
  statusCode: string;
  statusName: string;
  statusIsClosed: boolean;
  statusAlertsEnabledByDefault: boolean;
  appliedAmountTotal: number;
  remainingAmount: number;
  appliedPercentage: number;
  applicationCount: number;
  evidenceCount: number;
  commissionCount: number;
  alertState: string;
  createdUtc: string;
  updatedUtc: string | null;
}

export interface FederationDonationDetail {
  id: string;
  donorName: string;
  donationDate: string;
  donationType: string;
  baseAmount: number;
  reference: string;
  notes: string | null;
  statusCatalogEntryId: number;
  statusCode: string;
  statusName: string;
  statusIsClosed: boolean;
  statusAlertsEnabledByDefault: boolean;
  appliedAmountTotal: number;
  remainingAmount: number;
  appliedPercentage: number;
  commissionCount: number;
  evidenceCount: number;
  alertState: string;
  createdUtc: string;
  updatedUtc: string | null;
  applications: FederationDonationApplication[];
}

export interface CreateFederationDonationRequest {
  donorName: string;
  donationDate: string;
  donationType: string;
  baseAmount: number;
  reference: string;
  notes: string | null;
  statusCatalogEntryId: number;
}

export interface CreateFederationDonationApplicationRequest {
  beneficiaryOrDestinationName: string;
  applicationDate: string;
  appliedAmount: number;
  statusCatalogEntryId: number;
  verificationDetails: string | null;
  closingDetails: string | null;
}

export interface FederationDonationApplication {
  id: string;
  federationDonationId: string;
  beneficiaryOrDestinationName: string;
  applicationDate: string;
  appliedAmount: number;
  statusCatalogEntryId: number;
  statusCode: string;
  statusName: string;
  statusIsClosed: boolean;
  verificationDetails: string | null;
  closingDetails: string | null;
  evidenceCount: number;
  commissionCount: number;
  evidences: FederationDonationApplicationEvidence[];
  commissions: FederationDonationApplicationCommission[];
  createdUtc: string;
}

export interface CreateFederationDonationApplicationEvidenceRequest {
  evidenceTypeId: number;
  description: string | null;
  file: File;
}

export interface FederationDonationApplicationEvidence {
  id: string;
  federationDonationApplicationId: string;
  evidenceTypeId: number;
  evidenceTypeCode: string;
  evidenceTypeName: string;
  description: string | null;
  originalFileName: string;
  contentType: string | null;
  fileSizeBytes: number;
  uploadedUtc: string;
}

export interface CreateFederationDonationApplicationCommissionRequest {
  commissionTypeId: number;
  recipientCategory: string;
  recipientContactId: string | null;
  recipientName: string;
  baseAmount: number;
  commissionAmount: number;
  notes: string | null;
}

export interface FederationDonationApplicationCommission {
  id: string;
  federationDonationApplicationId: string;
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

export interface FederationActionAlert {
  actionId: string;
  actionTypeCode: string;
  actionTypeName: string;
  counterpartyOrInstitution: string;
  actionDate: string;
  statusCode: string;
  statusName: string;
  alertState: string;
}

export interface FederationDonationAlert {
  donationId: string;
  donorName: string;
  donationType: string;
  statusCode: string;
  statusName: string;
  baseAmount: number;
  appliedAmountTotal: number;
  remainingAmount: number;
  appliedPercentage: number;
  alertState: string;
}

export interface FederationModuleAlerts {
  actions: FederationActionAlert[];
  donations: FederationDonationAlert[];
}
