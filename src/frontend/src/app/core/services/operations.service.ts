import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';
import {
  OperationsSummary,
  OperationsTimeWindowCode,
  OperationsWorkQueueResponse
} from '../models/operations.models';
import { ProtectedDownloadService } from './protected-download.service';

@Injectable({
  providedIn: 'root'
})
export class OperationsService {
  private readonly httpClient = inject(HttpClient);
  private readonly protectedDownloadService = inject(ProtectedDownloadService);
  private readonly apiBaseUrl = `${environment.apiBaseUrl}/api/operations`;

  getSummary(filters: OperationsTimeWindowFilters = {}) {
    const params = this.buildTimeWindowParams(filters);
    return this.httpClient.get<OperationsSummary>(
      `${this.apiBaseUrl}/summary`,
      { params: params.keys().length > 0 ? params : undefined });
  }

  exportSummary(filters: OperationsTimeWindowFilters = {}) {
    return this.protectedDownloadService.download(
      this.buildUrlWithParams(`${this.apiBaseUrl}/summary/export`, this.buildTimeWindowParams(filters)),
      'operations-summary-export.csv');
  }

  getWorkQueue(filters: OperationsWorkQueueFilters = {}) {
    const params = this.buildWorkQueueParams(filters);

    return this.httpClient.get<OperationsWorkQueueResponse>(
      `${this.apiBaseUrl}/work-queue`,
      { params: params.keys().length > 0 ? params : undefined });
  }

  exportWorkQueue(filters: OperationsWorkQueueFilters = {}) {
    return this.protectedDownloadService.download(
      this.buildUrlWithParams(`${this.apiBaseUrl}/work-queue/export`, this.buildWorkQueueParams(filters)),
      'operations-work-queue-export.csv');
  }

  private buildWorkQueueParams(filters: OperationsWorkQueueFilters): HttpParams {
    let params = this.buildTimeWindowParams(filters);

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

    return params;
  }

  private buildTimeWindowParams(filters: OperationsTimeWindowFilters): HttpParams {
    let params = new HttpParams();

    if (filters.timeWindowCode?.trim()) {
      params = params.set('timeWindowCode', filters.timeWindowCode.trim());
    }

    if (filters.fromUtc?.trim()) {
      params = params.set('fromUtc', filters.fromUtc.trim());
    }

    if (filters.toUtc?.trim()) {
      params = params.set('toUtc', filters.toUtc.trim());
    }

    return params;
  }

  private buildUrlWithParams(url: string, params: HttpParams): string {
    const serialized = params.toString();
    return serialized ? `${url}?${serialized}` : url;
  }
}

export interface OperationsTimeWindowFilters {
  timeWindowCode?: OperationsTimeWindowCode | null;
  fromUtc?: string | null;
  toUtc?: string | null;
}

export interface OperationsWorkQueueFilters extends OperationsTimeWindowFilters {
  categoryCode?: string | null;
  severityCode?: string | null;
  skip?: number | null;
  take?: number | null;
}
