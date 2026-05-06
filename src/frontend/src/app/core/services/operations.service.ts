import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';
import {
  OperationsSummary,
  OperationsWorkQueueResponse
} from '../models/operations.models';

@Injectable({
  providedIn: 'root'
})
export class OperationsService {
  private readonly httpClient = inject(HttpClient);
  private readonly apiBaseUrl = `${environment.apiBaseUrl}/api/operations`;

  getSummary() {
    return this.httpClient.get<OperationsSummary>(`${this.apiBaseUrl}/summary`);
  }

  getWorkQueue(filters: {
    categoryCode?: string | null;
    severityCode?: string | null;
    skip?: number | null;
    take?: number | null;
  } = {}) {
    let params = new HttpParams();

    if (filters.categoryCode?.trim()) {
      params = params.set('categoryCode', filters.categoryCode.trim());
    }

    if (filters.severityCode?.trim()) {
      params = params.set('severityCode', filters.severityCode.trim());
    }

    if (filters.skip != null) {
      params = params.set('skip', String(filters.skip));
    }

    if (filters.take != null) {
      params = params.set('take', String(filters.take));
    }

    return this.httpClient.get<OperationsWorkQueueResponse>(
      `${this.apiBaseUrl}/work-queue`,
      { params: params.keys().length > 0 ? params : undefined });
  }
}
