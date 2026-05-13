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

export interface DonationDocumentaryStatus {
  donationId: string;
  totalApplications: number;
  applicationsWithEvidence: number;
  applicationsMissingEvidence: number;
  documentaryStatusCode: string;
  documentaryStatusLabel: string;
  isMinimumEvidenceComplete: boolean;
  applicationStatuses: DonationApplicationDocumentaryStatus[];
}

export interface DonationApplicationDocumentaryStatus {
  applicationId: string;
  beneficiaryName: string;
  applicationDate: string;
  appliedAmount: number;
  evidenceCount: number;
  activeDocumentCount: number;
  requirementStatus: string;
  missingReasonCode: string | null;
  requiredDocumentClassCodes: string[];
  routeHint: string | null;
}

export interface DonationTransparencyReport {
  donationId: string;
  donorEntityName: string;
  donationDate: string;
  donationType: string;
  reference: string;
  notes: string | null;
  reportGeneratedUtc: string;
  financialSummary: DonationTransparencyFinancialSummary;
  operationalStatus: DonationTransparencyOperationalStatus;
  documentarySummary: DonationTransparencyDocumentarySummary;
  applications: DonationTransparencyApplication[];
  presentationReadiness: DonationTransparencyReadiness;
  scopeNotes: string[];
}

export interface DonationTransparencyFinancialSummary {
  baseAmount: number;
  appliedAmountTotal: number;
  remainingAmount: number;
  appliedPercentage: number;
  applicationCount: number;
}

export interface DonationTransparencyOperationalStatus {
  donationStatusCode: string;
  donationStatusName: string;
  statusIsClosed: boolean;
  financialStatusLabel: string;
  operationalStatusLabel: string;
}

export interface DonationTransparencyDocumentarySummary {
  documentaryStatusCode: string;
  documentaryStatusLabel: string;
  totalApplications: number;
  applicationsWithEvidence: number;
  applicationsMissingEvidence: number;
  isMinimumEvidenceComplete: boolean;
}

export interface DonationTransparencyApplication {
  applicationId: string;
  beneficiaryName: string;
  applicationDate: string;
  responsibleName: string;
  appliedAmount: number;
  percentageOfDonation: number;
  statusCode: string;
  statusName: string;
  verificationDetails: string | null;
  closingDetails: string | null;
  evidenceCount: number;
  activeDocumentCount: number;
  requirementStatus: string;
  missingReasonCode: string | null;
  evidences: DonationTransparencyEvidence[];
}

export interface DonationTransparencyEvidence {
  evidenceId: string;
  evidenceTypeName: string;
  originalFileName: string;
  description: string | null;
  uploadedUtc: string;
  downloadUrl: string;
}

export interface DonationTransparencyReadiness {
  readinessCode: 'READY' | 'PARTIAL' | 'NOT_READY' | string;
  readinessLabel: string;
  reasons: string[];
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
