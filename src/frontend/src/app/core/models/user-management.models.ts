import { ApplicationRoleCode } from './auth.models';

export interface ApplicationUserAdmin {
  id: string;
  userName: string;
  displayName: string;
  roleCode: ApplicationRoleCode;
  isActive: boolean;
  createdUtc: string;
  updatedUtc: string | null;
  lastLoginUtc: string | null;
}

export interface CreateApplicationUserRequest {
  userName: string;
  displayName: string;
  roleCode: ApplicationRoleCode;
  password: string;
}

export interface ChangeApplicationUserRoleRequest {
  roleCode: ApplicationRoleCode;
}

export interface SetApplicationUserActivationRequest {
  isActive: boolean;
}

export interface ResetApplicationUserPasswordRequest {
  newPassword: string;
}
