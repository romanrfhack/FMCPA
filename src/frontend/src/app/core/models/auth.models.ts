export type ApplicationRoleCode = 'ADMIN' | 'OPERATOR' | 'READONLY';
export type ApplicationPermissionCode =
  | 'DASHBOARD_READ'
  | 'HISTORY_READ'
  | 'CONTACTS_READ'
  | 'CONTACTS_WRITE'
  | 'MARKETS_READ'
  | 'MARKETS_WRITE'
  | 'DONATIONS_READ'
  | 'DONATIONS_WRITE'
  | 'FINANCIALS_READ'
  | 'FINANCIALS_WRITE'
  | 'FEDERATION_READ'
  | 'FEDERATION_WRITE'
  | 'CATALOGS_READ'
  | 'CATALOGS_ADMIN'
  | 'USERS_ADMIN'
  | 'FORMAL_CLOSE_ADMIN';

export interface AuthenticatedUser {
  id: string;
  userName: string;
  displayName: string;
  roleCode: ApplicationRoleCode;
  permissions: ApplicationPermissionCode[];
}

export interface LoginRequest {
  userName: string;
  password: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresAtUtc: string;
  user: AuthenticatedUser;
}

export interface CurrentSessionResponse {
  user: AuthenticatedUser;
  expiresAtUtc: string;
}

export interface ChangePasswordResponse {
  message: string;
}

export interface AuthSession {
  accessToken: string;
  expiresAtUtc: string;
  user: AuthenticatedUser;
}
