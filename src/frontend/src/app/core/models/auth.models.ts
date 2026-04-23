export type ApplicationRoleCode = 'ADMIN' | 'OPERATOR' | 'READONLY';

export interface AuthenticatedUser {
  id: string;
  userName: string;
  displayName: string;
  roleCode: ApplicationRoleCode;
}

export interface LoginRequest {
  userName: string;
  password: string;
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

export interface AuthSession {
  accessToken: string;
  expiresAtUtc: string;
  user: AuthenticatedUser;
}
