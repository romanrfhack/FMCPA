import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';
import {
  BitacoraEntry,
  ClosedItem,
  ConsolidatedCommissionList,
  DocumentIntegrityResponse,
  DashboardAlerts,
  DashboardSummary
} from '../models/closeout.models';

@Injectable({
  providedIn: 'root'
})
export class CloseoutService {
  private readonly httpClient = inject(HttpClient);
  private readonly apiBaseUrl = `${environment.apiBaseUrl}/api`;

  getDashboardSummary() {
    return this.httpClient.get<DashboardSummary>(`${this.apiBaseUrl}/dashboard/summary`);
  }

  getDashboardAlerts() {
    return this.httpClient.get<DashboardAlerts>(`${this.apiBaseUrl}/dashboard/alerts`);
  }

  getConsolidatedCommissions(filters?: {
    sourceModuleCode?: string | null;
    commissionTypeId?: number | null;
    recipientCategory?: string | null;
    fromDate?: string | null;
    toDate?: string | null;
    q?: string | null;
  }) {
    let params = new HttpParams();

    if (filters?.sourceModuleCode) {
      params = params.set('sourceModuleCode', filters.sourceModuleCode);
    }

    if (filters?.commissionTypeId) {
      params = params.set('commissionTypeId', String(filters.commissionTypeId));
    }

    if (filters?.recipientCategory) {
      params = params.set('recipientCategory', filters.recipientCategory);
    }

    if (filters?.fromDate) {
      params = params.set('fromDate', filters.fromDate);
    }

    if (filters?.toDate) {
      params = params.set('toDate', filters.toDate);
    }

    if (filters?.q) {
      params = params.set('q', filters.q);
    }

    return this.httpClient.get<ConsolidatedCommissionList>(
      `${this.apiBaseUrl}/commissions/consolidated`,
      { params: params.keys().length > 0 ? params : undefined });
  }

  getBitacora(filters?: {
    moduleCode?: string | null;
    entityType?: string | null;
    entityId?: string | null;
    fromDate?: string | null;
    toDate?: string | null;
    q?: string | null;
    take?: number | null;
  }) {
    let params = new HttpParams();

    if (filters?.moduleCode) {
      params = params.set('moduleCode', filters.moduleCode);
    }

    if (filters?.entityType) {
      params = params.set('entityType', filters.entityType);
    }

    if (filters?.entityId) {
      params = params.set('entityId', filters.entityId);
    }

    if (filters?.fromDate) {
      params = params.set('fromDate', filters.fromDate);
    }

    if (filters?.toDate) {
      params = params.set('toDate', filters.toDate);
    }

    if (filters?.q) {
      params = params.set('q', filters.q);
    }

    if (filters?.take) {
      params = params.set('take', String(filters.take));
    }

    return this.httpClient.get<BitacoraEntry[]>(
      `${this.apiBaseUrl}/bitacora`,
      { params: params.keys().length > 0 ? params : undefined });
  }

  getClosedItems(filters?: { moduleCode?: string | null; q?: string | null }) {
    let params = new HttpParams();

    if (filters?.moduleCode) {
      params = params.set('moduleCode', filters.moduleCode);
    }

    if (filters?.q) {
      params = params.set('q', filters.q);
    }

    return this.httpClient.get<ClosedItem[]>(
      `${this.apiBaseUrl}/history/closed-items`,
      { params: params.keys().length > 0 ? params : undefined });
  }

  getDocumentIntegrity(filters?: {
    moduleCode?: string | null;
    entityType?: string | null;
    entityId?: string | null;
    take?: number | null;
  }) {
    let params = new HttpParams();

    if (filters?.moduleCode) {
      params = params.set('moduleCode', filters.moduleCode);
    }

    if (filters?.entityType) {
      params = params.set('entityType', filters.entityType);
    }

    if (filters?.entityId) {
      params = params.set('entityId', filters.entityId);
    }

    if (filters?.take) {
      params = params.set('take', String(filters.take));
    }

    return this.httpClient.get<DocumentIntegrityResponse>(
      `${this.apiBaseUrl}/documents/integrity`,
      { params: params.keys().length > 0 ? params : undefined });
  }
}
