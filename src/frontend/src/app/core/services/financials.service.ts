import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';
import {
  CloseRecordRequest,
  CloseRecordResponse
} from '../models/closeout.models';
import {
  CreateFinancialCreditCommissionRequest,
  CreateFinancialCreditRequest,
  CreateFinancialPermitRequest,
  FinancialCredit,
  FinancialCreditCommission,
  FinancialPermitAlert,
  FinancialPermitDetail,
  FinancialPermitSummary
} from '../models/financials.models';

@Injectable({
  providedIn: 'root'
})
export class FinancialsService {
  private readonly httpClient = inject(HttpClient);
  private readonly apiBaseUrl = `${environment.apiBaseUrl}/api/financials`;

  listPermits(filters?: { statusCode?: string | null; alertsOnly?: boolean }) {
    let params = new HttpParams();

    if (filters?.statusCode) {
      params = params.set('statusCode', filters.statusCode);
    }

    if (filters?.alertsOnly) {
      params = params.set('alertsOnly', 'true');
    }

    return this.httpClient.get<FinancialPermitSummary[]>(this.apiBaseUrl, { params });
  }

  getPermit(permitId: string) {
    return this.httpClient.get<FinancialPermitDetail>(`${this.apiBaseUrl}/${permitId}`);
  }

  createPermit(request: CreateFinancialPermitRequest) {
    return this.httpClient.post<FinancialPermitSummary>(this.apiBaseUrl, request);
  }

  closePermit(permitId: string, request: CloseRecordRequest) {
    return this.httpClient.post<CloseRecordResponse>(`${this.apiBaseUrl}/${permitId}/close`, request);
  }

  getPermitAlerts() {
    return this.httpClient.get<FinancialPermitAlert[]>(`${this.apiBaseUrl}/alerts/permits`);
  }

  getPermitCredits(permitId: string) {
    return this.httpClient.get<FinancialCredit[]>(`${this.apiBaseUrl}/${permitId}/credits`);
  }

  createCredit(permitId: string, request: CreateFinancialCreditRequest) {
    return this.httpClient.post<FinancialCredit>(`${this.apiBaseUrl}/${permitId}/credits`, request);
  }

  getCreditCommissions(creditId: string) {
    return this.httpClient.get<FinancialCreditCommission[]>(`${this.apiBaseUrl}/credits/${creditId}/commissions`);
  }

  createCreditCommission(creditId: string, request: CreateFinancialCreditCommissionRequest) {
    return this.httpClient.post<FinancialCreditCommission>(`${this.apiBaseUrl}/credits/${creditId}/commissions`, request);
  }
}
