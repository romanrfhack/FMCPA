import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../environments/environment';
import {
  ApplicationUserAdmin,
  ChangeApplicationUserRoleRequest,
  CreateApplicationUserRequest,
  ResetApplicationUserPasswordRequest,
  SetApplicationUserActivationRequest
} from '../models/user-management.models';

@Injectable({
  providedIn: 'root'
})
export class UserManagementService {
  private readonly httpClient = inject(HttpClient);
  private readonly apiBaseUrl = `${environment.apiBaseUrl}/api/admin/users`;

  getUsers() {
    return this.httpClient.get<ApplicationUserAdmin[]>(`${this.apiBaseUrl}/`);
  }

  getUser(userId: string) {
    return this.httpClient.get<ApplicationUserAdmin>(`${this.apiBaseUrl}/${userId}`);
  }

  createUser(request: CreateApplicationUserRequest) {
    return this.httpClient.post<ApplicationUserAdmin>(`${this.apiBaseUrl}/`, request);
  }

  changeUserRole(userId: string, request: ChangeApplicationUserRoleRequest) {
    return this.httpClient.patch<ApplicationUserAdmin>(`${this.apiBaseUrl}/${userId}/role`, request);
  }

  setUserActivation(userId: string, request: SetApplicationUserActivationRequest) {
    return this.httpClient.patch<ApplicationUserAdmin>(`${this.apiBaseUrl}/${userId}/activation`, request);
  }

  resetUserPassword(userId: string, request: ResetApplicationUserPasswordRequest) {
    return this.httpClient.post<ApplicationUserAdmin>(`${this.apiBaseUrl}/${userId}/reset-password`, request);
  }
}
