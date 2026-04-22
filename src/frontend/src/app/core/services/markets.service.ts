import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';
import {
  CloseRecordRequest,
  CloseRecordResponse
} from '../models/closeout.models';
import {
  CreateMarketIssueRequest,
  CreateMarketRequest,
  CreateMarketTenantRequest,
  MarketDetail,
  MarketIssue,
  MarketSummary,
  MarketTenant,
  MarketTenantAlert
} from '../models/markets.models';

@Injectable({
  providedIn: 'root'
})
export class MarketsService {
  private readonly httpClient = inject(HttpClient);
  private readonly apiBaseUrl = `${environment.apiBaseUrl}/api/markets`;

  listMarkets(filters?: { statusCode?: string | null; alertsOnly?: boolean }) {
    let params = new HttpParams();

    if (filters?.statusCode) {
      params = params.set('statusCode', filters.statusCode);
    }

    if (filters?.alertsOnly) {
      params = params.set('alertsOnly', 'true');
    }

    return this.httpClient.get<MarketSummary[]>(this.apiBaseUrl, { params });
  }

  getMarket(marketId: string) {
    return this.httpClient.get<MarketDetail>(`${this.apiBaseUrl}/${marketId}`);
  }

  createMarket(request: CreateMarketRequest) {
    return this.httpClient.post<MarketSummary>(this.apiBaseUrl, request);
  }

  closeMarket(marketId: string, request: CloseRecordRequest) {
    return this.httpClient.post<CloseRecordResponse>(`${this.apiBaseUrl}/${marketId}/close`, request);
  }

  getMarketTenants(marketId: string) {
    return this.httpClient.get<MarketTenant[]>(`${this.apiBaseUrl}/${marketId}/tenants`);
  }

  createMarketTenant(marketId: string, request: CreateMarketTenantRequest) {
    const formData = new FormData();

    if (request.contactId) {
      formData.append('contactId', request.contactId);
    }

    formData.append('tenantName', request.tenantName);
    formData.append('certificateNumber', request.certificateNumber);
    formData.append('certificateValidityTo', request.certificateValidityTo);
    formData.append('businessLine', request.businessLine);

    if (request.mobilePhone) {
      formData.append('mobilePhone', request.mobilePhone);
    }

    if (request.whatsAppPhone) {
      formData.append('whatsAppPhone', request.whatsAppPhone);
    }

    if (request.email) {
      formData.append('email', request.email);
    }

    if (request.notes) {
      formData.append('notes', request.notes);
    }

    formData.append('certificateFile', request.certificateFile);

    return this.httpClient.post<MarketTenant>(`${this.apiBaseUrl}/${marketId}/tenants`, formData);
  }

  getMarketIssues(marketId: string) {
    return this.httpClient.get<MarketIssue[]>(`${this.apiBaseUrl}/${marketId}/issues`);
  }

  createMarketIssue(marketId: string, request: CreateMarketIssueRequest) {
    return this.httpClient.post<MarketIssue>(`${this.apiBaseUrl}/${marketId}/issues`, request);
  }

  getTenantAlerts() {
    return this.httpClient.get<MarketTenantAlert[]>(`${this.apiBaseUrl}/alerts/tenants`);
  }

  getTenantCertificateDownloadUrl(tenantId: string) {
    return `${this.apiBaseUrl}/tenants/${tenantId}/cedula`;
  }
}
