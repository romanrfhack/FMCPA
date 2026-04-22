export interface MarketSummary {
  id: string;
  name: string;
  borough: string;
  statusCatalogEntryId: number;
  statusCode: string;
  statusName: string;
  statusIsClosed: boolean;
  statusAlertsEnabledByDefault: boolean;
  secretaryGeneralContactId: string | null;
  secretaryGeneralName: string;
  notes: string | null;
  tenantCount: number;
  issueCount: number;
  activeTenantAlertsCount: number;
  createdUtc: string;
  updatedUtc: string | null;
}

export interface MarketDetail {
  id: string;
  name: string;
  borough: string;
  statusCatalogEntryId: number;
  statusCode: string;
  statusName: string;
  statusIsClosed: boolean;
  statusAlertsEnabledByDefault: boolean;
  secretaryGeneralContactId: string | null;
  secretaryGeneralName: string;
  notes: string | null;
  createdUtc: string;
  updatedUtc: string | null;
  tenants: MarketTenant[];
  issues: MarketIssue[];
}

export interface CreateMarketRequest {
  name: string;
  borough: string;
  statusCatalogEntryId: number;
  secretaryGeneralContactId: string | null;
  secretaryGeneralName: string;
  notes: string | null;
}

export interface CreateMarketTenantRequest {
  contactId: string | null;
  tenantName: string;
  certificateNumber: string;
  certificateValidityTo: string;
  businessLine: string;
  mobilePhone: string | null;
  whatsAppPhone: string | null;
  email: string | null;
  notes: string | null;
  certificateFile: File;
}

export interface MarketTenant {
  id: string;
  marketId: string;
  contactId: string | null;
  tenantName: string;
  certificateNumber: string;
  certificateValidityTo: string;
  businessLine: string;
  mobilePhone: string | null;
  whatsAppPhone: string | null;
  email: string | null;
  notes: string | null;
  hasDigitalCertificate: boolean;
  certificateOriginalFileName: string | null;
  certificateContentType: string | null;
  certificateFileSizeBytes: number | null;
  certificateUploadedUtc: string | null;
  certificateAlertState: string;
  daysUntilExpiration: number;
  alertsSuppressed: boolean;
  createdUtc: string;
}

export interface CreateMarketIssueRequest {
  issueType: string;
  description: string;
  issueDate: string;
  advanceSummary: string;
  statusCatalogEntryId: number;
  followUpOrResolution: string | null;
  finalSatisfaction: string | null;
}

export interface MarketIssue {
  id: string;
  marketId: string;
  issueType: string;
  description: string;
  issueDate: string;
  advanceSummary: string;
  statusCatalogEntryId: number;
  statusCode: string;
  statusName: string;
  statusIsClosed: boolean;
  followUpOrResolution: string | null;
  finalSatisfaction: string | null;
  createdUtc: string;
}

export interface MarketTenantAlert {
  marketId: string;
  marketName: string;
  marketStatusCode: string;
  tenantId: string;
  tenantName: string;
  certificateNumber: string;
  certificateValidityTo: string;
  daysUntilExpiration: number;
  alertState: string;
}
