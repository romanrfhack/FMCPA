import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';
import {
  CatalogItem,
  Contact,
  ContactType,
  CreateCatalogItemRequest,
  CreateContactRequest,
  CreateModuleStatusCatalogEntryRequest,
  ModuleStatusCatalogEntry
} from '../models/shared-catalogs.models';

@Injectable({
  providedIn: 'root'
})
export class SharedCatalogsService {
  private readonly httpClient = inject(HttpClient);
  private readonly apiBaseUrl = `${environment.apiBaseUrl}/api`;

  getContactTypes() {
    return this.httpClient.get<ContactType[]>(`${this.apiBaseUrl}/contact-types`);
  }

  getContacts() {
    return this.httpClient.get<Contact[]>(`${this.apiBaseUrl}/contacts`);
  }

  createContact(request: CreateContactRequest) {
    return this.httpClient.post<Contact>(`${this.apiBaseUrl}/contacts`, request);
  }

  getCommissionTypes() {
    return this.httpClient.get<CatalogItem[]>(`${this.apiBaseUrl}/commission-types`);
  }

  createCommissionType(request: CreateCatalogItemRequest) {
    return this.httpClient.post<CatalogItem>(`${this.apiBaseUrl}/commission-types`, request);
  }

  getEvidenceTypes() {
    return this.httpClient.get<CatalogItem[]>(`${this.apiBaseUrl}/evidence-types`);
  }

  createEvidenceType(request: CreateCatalogItemRequest) {
    return this.httpClient.post<CatalogItem>(`${this.apiBaseUrl}/evidence-types`, request);
  }

  getModuleStatuses(moduleCode?: string, contextCode?: string) {
    let params = new HttpParams();

    if (moduleCode) {
      params = params.set('moduleCode', moduleCode);
    }

    if (contextCode) {
      params = params.set('contextCode', contextCode);
    }

    return this.httpClient.get<ModuleStatusCatalogEntry[]>(
      `${this.apiBaseUrl}/module-statuses`,
      { params: params.keys().length > 0 ? params : undefined });
  }

  createModuleStatus(request: CreateModuleStatusCatalogEntryRequest) {
    return this.httpClient.post<ModuleStatusCatalogEntry>(`${this.apiBaseUrl}/module-statuses`, request);
  }
}
