import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';
import {
  LockedApplicationUser,
  SecurityActivitySummary,
  SecurityAuditEvent,
  SecurityEventsFilter
} from '../models/security-observability.models';

@Injectable({
  providedIn: 'root'
})
export class SecurityObservabilityService {
  private readonly httpClient = inject(HttpClient);
  private readonly apiBaseUrl = `${environment.apiBaseUrl}/api/admin/security`;

  getSummary(hours = 24) {
    return this.httpClient.get<SecurityActivitySummary>(
      `${this.apiBaseUrl}/summary`,
      { params: new HttpParams().set('hours', hours) });
  }

  getEvents(filter: SecurityEventsFilter) {
    let params = new HttpParams();

    if (filter.userName?.trim()) {
      params = params.set('userName', filter.userName.trim());
    }

    if (filter.eventType?.trim()) {
      params = params.set('eventType', filter.eventType.trim());
    }

    if (filter.fromUtc) {
      params = params.set('fromUtc', filter.fromUtc);
    }

    if (filter.toUtc) {
      params = params.set('toUtc', filter.toUtc);
    }

    if (filter.take) {
      params = params.set('take', filter.take);
    }

    return this.httpClient.get<SecurityAuditEvent[]>(`${this.apiBaseUrl}/events`, { params });
  }

  getLockedUsers() {
    return this.httpClient.get<LockedApplicationUser[]>(`${this.apiBaseUrl}/locked-users`);
  }
}
