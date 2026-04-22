import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';
import {
  CloseRecordRequest,
  CloseRecordResponse
} from '../models/closeout.models';
import {
  CreateDonationApplicationEvidenceRequest,
  CreateDonationApplicationRequest,
  CreateDonationRequest,
  DonationAlert,
  DonationApplication,
  DonationApplicationEvidence,
  DonationDetail,
  DonationProgress,
  DonationSummary
} from '../models/donations.models';

@Injectable({
  providedIn: 'root'
})
export class DonationsService {
  private readonly httpClient = inject(HttpClient);
  private readonly apiBaseUrl = `${environment.apiBaseUrl}/api/donations`;

  listDonations(filters?: { statusCode?: string | null; alertsOnly?: boolean }) {
    let params = new HttpParams();

    if (filters?.statusCode) {
      params = params.set('statusCode', filters.statusCode);
    }

    if (filters?.alertsOnly) {
      params = params.set('alertsOnly', 'true');
    }

    return this.httpClient.get<DonationSummary[]>(this.apiBaseUrl, { params });
  }

  getDonation(donationId: string) {
    return this.httpClient.get<DonationDetail>(`${this.apiBaseUrl}/${donationId}`);
  }

  createDonation(request: CreateDonationRequest) {
    return this.httpClient.post<DonationSummary>(this.apiBaseUrl, request);
  }

  closeDonation(donationId: string, request: CloseRecordRequest) {
    return this.httpClient.post<CloseRecordResponse>(`${this.apiBaseUrl}/${donationId}/close`, request);
  }

  getDonationProgress(donationId: string) {
    return this.httpClient.get<DonationProgress>(`${this.apiBaseUrl}/${donationId}/progress`);
  }

  getDonationApplications(donationId: string) {
    return this.httpClient.get<DonationApplication[]>(`${this.apiBaseUrl}/${donationId}/applications`);
  }

  createDonationApplication(donationId: string, request: CreateDonationApplicationRequest) {
    return this.httpClient.post<DonationApplication>(`${this.apiBaseUrl}/${donationId}/applications`, request);
  }

  getDonationAlerts() {
    return this.httpClient.get<DonationAlert[]>(`${this.apiBaseUrl}/alerts`);
  }

  getApplicationEvidences(applicationId: string) {
    return this.httpClient.get<DonationApplicationEvidence[]>(`${this.apiBaseUrl}/applications/${applicationId}/evidences`);
  }

  createApplicationEvidence(applicationId: string, request: CreateDonationApplicationEvidenceRequest) {
    const formData = new FormData();
    formData.append('evidenceTypeId', String(request.evidenceTypeId));

    if (request.description) {
      formData.append('description', request.description);
    }

    formData.append('file', request.file);

    return this.httpClient.post<DonationApplicationEvidence>(
      `${this.apiBaseUrl}/applications/${applicationId}/evidences`,
      formData);
  }

  getEvidenceDownloadUrl(evidenceId: string) {
    return `${this.apiBaseUrl}/applications/evidences/${evidenceId}/download`;
  }
}
