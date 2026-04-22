export interface DonationSummary {
  id: string;
  donorEntityName: string;
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
  alertState: string;
  createdUtc: string;
  updatedUtc: string | null;
}

export interface DonationDetail {
  id: string;
  donorEntityName: string;
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
  alertState: string;
  createdUtc: string;
  updatedUtc: string | null;
  applications: DonationApplication[];
}

export interface CreateDonationRequest {
  donorEntityName: string;
  donationDate: string;
  donationType: string;
  baseAmount: number;
  reference: string;
  notes: string | null;
  statusCatalogEntryId: number;
}

export interface DonationProgress {
  donationId: string;
  baseAmount: number;
  appliedAmountTotal: number;
  remainingAmount: number;
  appliedPercentage: number;
  applicationCount: number;
}

export interface CreateDonationApplicationRequest {
  beneficiaryName: string;
  responsibleContactId: string | null;
  responsibleName: string;
  applicationDate: string;
  appliedAmount: number;
  statusCatalogEntryId: number;
  verificationDetails: string | null;
  closingDetails: string | null;
}

export interface DonationApplication {
  id: string;
  donationId: string;
  beneficiaryName: string;
  responsibleContactId: string | null;
  responsibleName: string;
  applicationDate: string;
  appliedAmount: number;
  statusCatalogEntryId: number;
  statusCode: string;
  statusName: string;
  statusIsClosed: boolean;
  verificationDetails: string | null;
  closingDetails: string | null;
  evidenceCount: number;
  evidences: DonationApplicationEvidence[];
  createdUtc: string;
}

export interface CreateDonationApplicationEvidenceRequest {
  evidenceTypeId: number;
  description: string | null;
  file: File;
}

export interface DonationApplicationEvidence {
  id: string;
  donationApplicationId: string;
  evidenceTypeId: number;
  evidenceTypeCode: string;
  evidenceTypeName: string;
  description: string | null;
  originalFileName: string;
  contentType: string | null;
  fileSizeBytes: number;
  uploadedUtc: string;
}

export interface DonationAlert {
  donationId: string;
  donorEntityName: string;
  donationType: string;
  statusCode: string;
  statusName: string;
  baseAmount: number;
  appliedAmountTotal: number;
  remainingAmount: number;
  appliedPercentage: number;
  alertState: string;
}
