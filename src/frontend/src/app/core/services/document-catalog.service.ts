import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';
import {
  DocumentCompleteness,
  DocumentCompletenessFilters,
  DocumentCompletenessListResponse,
  DocumentWorkQueueFilters,
  DocumentWorkQueueResponse,
  DocumentSummary,
  DocumentCatalogDetail,
  DocumentCatalogFilters,
  DocumentCatalogItem,
  DocumentCatalogListResponse,
  DocumentRequirement,
  DocumentRuleListResponse,
  DocumentTimelineResponse,
  DocumentRetentionReviewQueueFilters,
  ArchiveStoredDocumentRequest,
  SetStoredDocumentHoldRequest,
  UpdateStoredDocumentMetadataRequest,
  UpdateStoredDocumentRetentionOverrideRequest,
  UpdateStoredDocumentRetentionReviewRequest
} from '../models/document-catalog.models';
import { ProtectedDownloadService } from './protected-download.service';

@Injectable({
  providedIn: 'root'
})
export class DocumentCatalogService {
  private readonly httpClient = inject(HttpClient);
  private readonly protectedDownloadService = inject(ProtectedDownloadService);
  private readonly apiBaseUrl = `${environment.apiBaseUrl}/api/documents`;

  listDocuments(filters: DocumentCatalogFilters = {}) {
    let params = new HttpParams();

    if (filters.moduleCode?.trim()) {
      params = params.set('moduleCode', filters.moduleCode.trim());
    }

    if (filters.entityType?.trim()) {
      params = params.set('entityType', filters.entityType.trim());
    }

    if (filters.entityId?.trim()) {
      params = params.set('entityId', filters.entityId.trim());
    }

    if (filters.documentAreaCode?.trim()) {
      params = params.set('documentAreaCode', filters.documentAreaCode.trim());
    }

    if (filters.integrityState?.trim()) {
      params = params.set('integrityState', filters.integrityState.trim());
    }

    if (filters.documentOperationalStatusCode?.trim()) {
      params = params.set('documentOperationalStatusCode', filters.documentOperationalStatusCode.trim());
    }

    if (filters.documentClassCode?.trim()) {
      params = params.set('documentClassCode', filters.documentClassCode.trim());
    }

    if (filters.retentionPolicyCode?.trim()) {
      params = params.set('retentionPolicyCode', filters.retentionPolicyCode.trim());
    }

    if (filters.retentionStatusCode?.trim()) {
      params = params.set('retentionStatusCode', filters.retentionStatusCode.trim());
    }

    if (filters.statusCode?.trim()) {
      params = params.set('statusCode', filters.statusCode.trim());
    }

    if (filters.includeArchived) {
      params = params.set('includeArchived', 'true');
    }

    if (filters.fromUtc) {
      params = params.set('fromUtc', filters.fromUtc);
    }

    if (filters.toUtc) {
      params = params.set('toUtc', filters.toUtc);
    }

    if (filters.skip) {
      params = params.set('skip', String(filters.skip));
    }

    if (filters.take) {
      params = params.set('take', String(filters.take));
    }

    return this.httpClient.get<DocumentCatalogListResponse>(
      this.apiBaseUrl,
      { params: params.keys().length > 0 ? params : undefined });
  }

  getSummary() {
    return this.httpClient.get<DocumentSummary>(`${this.apiBaseUrl}/summary`);
  }

  listRetentionReviewQueue(filters: DocumentRetentionReviewQueueFilters = {}) {
    let params = new HttpParams();

    if (filters.moduleCode?.trim()) {
      params = params.set('moduleCode', filters.moduleCode.trim());
    }

    if (filters.retentionReviewStatusCode?.trim()) {
      params = params.set('retentionReviewStatusCode', filters.retentionReviewStatusCode.trim());
    }

    if (filters.skip) {
      params = params.set('skip', String(filters.skip));
    }

    if (filters.take) {
      params = params.set('take', String(filters.take));
    }

    return this.httpClient.get<DocumentCatalogListResponse>(
      `${this.apiBaseUrl}/review-queue`,
      { params: params.keys().length > 0 ? params : undefined });
  }

  listDocumentsByEntity(moduleCode: string, entityType: string, entityId: string, filters: { includeArchived?: boolean | null; take?: number | null } = {}) {
    let params = new HttpParams()
      .set('moduleCode', moduleCode)
      .set('entityType', entityType)
      .set('entityId', entityId);

    if (filters.includeArchived) {
      params = params.set('includeArchived', 'true');
    }

    if (filters.take) {
      params = params.set('take', String(filters.take));
    }

    return this.httpClient.get<DocumentCatalogListResponse>(
      `${this.apiBaseUrl}/by-entity`,
      { params });
  }

  listRules() {
    return this.httpClient.get<DocumentRuleListResponse>(`${this.apiBaseUrl}/rules`);
  }

  getRequirementsByEntity(moduleCode: string, entityType: string, entityId: string) {
    const params = new HttpParams()
      .set('moduleCode', moduleCode)
      .set('entityType', entityType)
      .set('entityId', entityId);

    return this.httpClient.get<DocumentRequirement>(
      `${this.apiBaseUrl}/requirements/by-entity`,
      { params });
  }

  getCompletenessByEntity(moduleCode: string, entityType: string, entityId: string) {
    const params = new HttpParams()
      .set('moduleCode', moduleCode)
      .set('entityType', entityType)
      .set('entityId', entityId);

    return this.httpClient.get<DocumentCompleteness>(
      `${this.apiBaseUrl}/completeness/by-entity`,
      { params });
  }

  listPendingCompleteness(filters: DocumentCompletenessFilters = {}) {
    let params = new HttpParams();

    if (filters.moduleCode?.trim()) {
      params = params.set('moduleCode', filters.moduleCode.trim());
    }

    if (filters.skip) {
      params = params.set('skip', String(filters.skip));
    }

    if (filters.take) {
      params = params.set('take', String(filters.take));
    }

    return this.httpClient.get<DocumentCompletenessListResponse>(
      `${this.apiBaseUrl}/pending`,
      { params: params.keys().length > 0 ? params : undefined });
  }

  listWorkQueue(filters: DocumentWorkQueueFilters = {}) {
    let params = new HttpParams();

    if (filters.moduleCode?.trim()) {
      params = params.set('moduleCode', filters.moduleCode.trim());
    }

    if (filters.workItemType?.trim()) {
      params = params.set('workItemType', filters.workItemType.trim());
    }

    if (filters.severityCode?.trim()) {
      params = params.set('severityCode', filters.severityCode.trim());
    }

    if (filters.skip) {
      params = params.set('skip', String(filters.skip));
    }

    if (filters.take) {
      params = params.set('take', String(filters.take));
    }

    return this.httpClient.get<DocumentWorkQueueResponse>(
      `${this.apiBaseUrl}/work-queue`,
      { params: params.keys().length > 0 ? params : undefined });
  }

  getDocument(documentId: string) {
    return this.httpClient.get<DocumentCatalogDetail>(`${this.apiBaseUrl}/${documentId}`);
  }

  getDocumentTimeline(documentId: string) {
    return this.httpClient.get<DocumentTimelineResponse>(`${this.apiBaseUrl}/${documentId}/timeline`);
  }

  getEntityTimeline(moduleCode: string, entityType: string, entityId: string, filters: { take?: number | null } = {}) {
    let params = new HttpParams()
      .set('moduleCode', moduleCode)
      .set('entityType', entityType)
      .set('entityId', entityId);

    if (filters.take) {
      params = params.set('take', String(filters.take));
    }

    return this.httpClient.get<DocumentTimelineResponse>(
      `${this.apiBaseUrl}/timeline/by-entity`,
      { params });
  }

  archiveDocument(documentId: string, request: ArchiveStoredDocumentRequest) {
    return this.httpClient.post<DocumentCatalogDetail>(`${this.apiBaseUrl}/${documentId}/archive`, request);
  }

  restoreDocument(documentId: string) {
    return this.httpClient.post<DocumentCatalogDetail>(`${this.apiBaseUrl}/${documentId}/restore`, {});
  }

  setAdministrativeHold(documentId: string, request: SetStoredDocumentHoldRequest) {
    return this.httpClient.post<DocumentCatalogDetail>(`${this.apiBaseUrl}/${documentId}/hold`, request);
  }

  clearAdministrativeHold(documentId: string) {
    return this.httpClient.delete<DocumentCatalogDetail>(`${this.apiBaseUrl}/${documentId}/hold`);
  }

  updateMetadata(documentId: string, request: UpdateStoredDocumentMetadataRequest) {
    return this.httpClient.patch<DocumentCatalogDetail>(`${this.apiBaseUrl}/${documentId}/metadata`, request);
  }

  updateRetentionReview(documentId: string, request: UpdateStoredDocumentRetentionReviewRequest) {
    return this.httpClient.patch<DocumentCatalogDetail>(`${this.apiBaseUrl}/${documentId}/retention-review`, request);
  }

  setRetentionOverride(documentId: string, request: UpdateStoredDocumentRetentionOverrideRequest) {
    return this.httpClient.patch<DocumentCatalogDetail>(`${this.apiBaseUrl}/${documentId}/retention-override`, request);
  }

  clearRetentionOverride(documentId: string) {
    return this.httpClient.delete<DocumentCatalogDetail>(`${this.apiBaseUrl}/${documentId}/retention-override`);
  }

  downloadDocument(document: DocumentCatalogItem) {
    return this.protectedDownloadService.download(
      this.resolveApiUrl(document.downloadUrl),
      document.originalFileName);
  }

  private resolveApiUrl(url: string): string {
    return url.startsWith('http://') || url.startsWith('https://')
      ? url
      : `${environment.apiBaseUrl}${url}`;
  }
}
