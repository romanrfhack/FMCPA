import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';
import {
  CloseRecordRequest,
  CloseRecordResponse
} from '../models/closeout.models';
import {
  CreateFederationActionParticipantRequest,
  CreateFederationActionRequest,
  CreateFederationDonationApplicationCommissionRequest,
  CreateFederationDonationApplicationEvidenceRequest,
  CreateFederationDonationApplicationRequest,
  CreateFederationDonationRequest,
  FederationActionDetail,
  FederationActionParticipant,
  FederationActionSummary,
  FederationDonationApplication,
  FederationDonationApplicationCommission,
  FederationDonationApplicationEvidence,
  FederationDonationDetail,
  FederationDonationSummary,
  FederationModuleAlerts
} from '../models/federation.models';

@Injectable({
  providedIn: 'root'
})
export class FederationService {
  private readonly httpClient = inject(HttpClient);
  private readonly apiBaseUrl = `${environment.apiBaseUrl}/api/federation`;

  getAlerts() {
    return this.httpClient.get<FederationModuleAlerts>(`${this.apiBaseUrl}/alerts`);
  }

  listActions(filters?: { statusCode?: string | null; alertsOnly?: boolean }) {
    let params = new HttpParams();

    if (filters?.statusCode) {
      params = params.set('statusCode', filters.statusCode);
    }

    if (filters?.alertsOnly) {
      params = params.set('alertsOnly', 'true');
    }

    return this.httpClient.get<FederationActionSummary[]>(`${this.apiBaseUrl}/actions`, { params });
  }

  getAction(actionId: string) {
    return this.httpClient.get<FederationActionDetail>(`${this.apiBaseUrl}/actions/${actionId}`);
  }

  createAction(request: CreateFederationActionRequest) {
    return this.httpClient.post<FederationActionSummary>(`${this.apiBaseUrl}/actions`, request);
  }

  closeAction(actionId: string, request: CloseRecordRequest) {
    return this.httpClient.post<CloseRecordResponse>(`${this.apiBaseUrl}/actions/${actionId}/close`, request);
  }

  getActionParticipants(actionId: string) {
    return this.httpClient.get<FederationActionParticipant[]>(`${this.apiBaseUrl}/actions/${actionId}/participants`);
  }

  addActionParticipant(actionId: string, request: CreateFederationActionParticipantRequest) {
    return this.httpClient.post<FederationActionParticipant>(`${this.apiBaseUrl}/actions/${actionId}/participants`, request);
  }

  listDonations(filters?: { statusCode?: string | null; alertsOnly?: boolean }) {
    let params = new HttpParams();

    if (filters?.statusCode) {
      params = params.set('statusCode', filters.statusCode);
    }

    if (filters?.alertsOnly) {
      params = params.set('alertsOnly', 'true');
    }

    return this.httpClient.get<FederationDonationSummary[]>(`${this.apiBaseUrl}/donations`, { params });
  }

  getDonation(donationId: string) {
    return this.httpClient.get<FederationDonationDetail>(`${this.apiBaseUrl}/donations/${donationId}`);
  }

  createDonation(request: CreateFederationDonationRequest) {
    return this.httpClient.post<FederationDonationSummary>(`${this.apiBaseUrl}/donations`, request);
  }

  closeDonation(donationId: string, request: CloseRecordRequest) {
    return this.httpClient.post<CloseRecordResponse>(`${this.apiBaseUrl}/donations/${donationId}/close`, request);
  }

  getDonationApplications(donationId: string) {
    return this.httpClient.get<FederationDonationApplication[]>(`${this.apiBaseUrl}/donations/${donationId}/applications`);
  }

  createDonationApplication(donationId: string, request: CreateFederationDonationApplicationRequest) {
    return this.httpClient.post<FederationDonationApplication>(`${this.apiBaseUrl}/donations/${donationId}/applications`, request);
  }

  getApplicationEvidences(applicationId: string) {
    return this.httpClient.get<FederationDonationApplicationEvidence[]>(`${this.apiBaseUrl}/applications/${applicationId}/evidences`);
  }

  createApplicationEvidence(applicationId: string, request: CreateFederationDonationApplicationEvidenceRequest) {
    const formData = new FormData();
    formData.append('evidenceTypeId', String(request.evidenceTypeId));

    if (request.description) {
      formData.append('description', request.description);
    }

    formData.append('file', request.file);

    return this.httpClient.post<FederationDonationApplicationEvidence>(
      `${this.apiBaseUrl}/applications/${applicationId}/evidences`,
      formData);
  }

  getEvidenceDownloadUrl(evidenceId: string) {
    return `${this.apiBaseUrl}/applications/evidences/${evidenceId}/download`;
  }

  getApplicationCommissions(applicationId: string) {
    return this.httpClient.get<FederationDonationApplicationCommission[]>(`${this.apiBaseUrl}/applications/${applicationId}/commissions`);
  }

  createApplicationCommission(applicationId: string, request: CreateFederationDonationApplicationCommissionRequest) {
    return this.httpClient.post<FederationDonationApplicationCommission>(
      `${this.apiBaseUrl}/applications/${applicationId}/commissions`,
      request);
  }
}
