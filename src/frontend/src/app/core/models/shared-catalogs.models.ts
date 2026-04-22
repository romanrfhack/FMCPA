export interface ContactType {
  id: number;
  code: string;
  name: string;
  description: string | null;
  sortOrder: number;
}

export interface Contact {
  id: string;
  name: string;
  contactTypeId: number;
  contactTypeCode: string;
  contactTypeName: string;
  organizationOrDependency: string | null;
  roleTitle: string | null;
  mobilePhone: string | null;
  whatsAppPhone: string | null;
  email: string | null;
  notes: string | null;
  createdUtc: string;
  updatedUtc: string | null;
}

export interface CreateContactRequest {
  name: string;
  contactTypeId: number;
  organizationOrDependency: string | null;
  roleTitle: string | null;
  mobilePhone: string | null;
  whatsAppPhone: string | null;
  email: string | null;
  notes: string | null;
}

export interface CatalogItem {
  id: number;
  code: string;
  name: string;
  description: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface CreateCatalogItemRequest {
  code: string;
  name: string;
  description: string | null;
  sortOrder: number;
}

export interface ModuleStatusCatalogEntry {
  id: number;
  moduleCode: string;
  moduleName: string;
  contextCode: string;
  contextName: string;
  statusCode: string;
  statusName: string;
  description: string | null;
  sortOrder: number;
  isClosed: boolean;
  alertsEnabledByDefault: boolean;
  isActive: boolean;
}

export interface CreateModuleStatusCatalogEntryRequest {
  moduleCode: string;
  moduleName: string;
  contextCode: string | null;
  contextName: string | null;
  statusCode: string;
  statusName: string;
  description: string | null;
  sortOrder: number;
  isClosed: boolean;
  alertsEnabledByDefault: boolean;
}
