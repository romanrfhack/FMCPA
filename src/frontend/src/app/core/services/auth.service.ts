import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ApplicationPermissionCode,
  ApplicationRoleCode,
  AuthSession,
  ChangePasswordRequest,
  ChangePasswordResponse,
  CurrentSessionResponse,
  LoginRequest,
  LoginResponse
} from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly httpClient = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly storageKey = 'fmcpa.auth.session';
  private readonly sessionState = signal<AuthSession | null>(this.restoreSession());

  readonly currentSession = this.sessionState.asReadonly();
  readonly currentUser = computed(() => this.sessionState()?.user ?? null);
  readonly currentRole = computed<ApplicationRoleCode | null>(() => this.sessionState()?.user.roleCode ?? null);
  readonly currentPermissions = computed<ApplicationPermissionCode[]>(() => this.sessionState()?.user.permissions ?? []);
  readonly isAuthenticated = computed(() => {
    const session = this.sessionState();
    return session !== null && !this.isExpired(session.expiresAtUtc);
  });
  readonly canReadDashboard = computed(() => this.hasPermission('DASHBOARD_READ'));
  readonly canReadHistory = computed(() => this.hasPermission('HISTORY_READ'));
  readonly canReadContacts = computed(() => this.hasPermission('CONTACTS_READ'));
  readonly canWriteContacts = computed(() => this.hasPermission('CONTACTS_WRITE'));
  readonly canReadMarkets = computed(() => this.hasPermission('MARKETS_READ'));
  readonly canWriteMarkets = computed(() => this.hasPermission('MARKETS_WRITE'));
  readonly canReadDonations = computed(() => this.hasPermission('DONATIONS_READ'));
  readonly canWriteDonations = computed(() => this.hasPermission('DONATIONS_WRITE'));
  readonly canReadFinancials = computed(() => this.hasPermission('FINANCIALS_READ'));
  readonly canWriteFinancials = computed(() => this.hasPermission('FINANCIALS_WRITE'));
  readonly canReadFederation = computed(() => this.hasPermission('FEDERATION_READ'));
  readonly canWriteFederation = computed(() => this.hasPermission('FEDERATION_WRITE'));
  readonly canReadCatalogs = computed(() => this.hasPermission('CATALOGS_READ'));
  readonly canAdministerCatalogs = computed(() => this.hasPermission('CATALOGS_ADMIN'));
  readonly canAdministerUsers = computed(() => this.hasPermission('USERS_ADMIN'));
  readonly canAdministerFormalClose = computed(() => this.hasPermission('FORMAL_CLOSE_ADMIN'));
  readonly canWrite = computed(() =>
    this.hasAnyPermission([
      'CONTACTS_WRITE',
      'MARKETS_WRITE',
      'DONATIONS_WRITE',
      'FINANCIALS_WRITE',
      'FEDERATION_WRITE'
    ]));
  readonly canReadDocuments = computed(() =>
    this.hasAnyPermission([
      'MARKETS_READ',
      'DONATIONS_READ',
      'FEDERATION_READ'
    ]));
  readonly canAdminister = computed(() =>
    this.hasAnyPermission(['CATALOGS_ADMIN', 'USERS_ADMIN', 'FORMAL_CLOSE_ADMIN']));

  login(request: LoginRequest) {
    return this.httpClient
      .post<LoginResponse>(`${environment.apiBaseUrl}/api/auth/login`, request)
      .pipe(tap((response) => this.setSession(response)));
  }

  changePassword(request: ChangePasswordRequest) {
    return this.httpClient.post<ChangePasswordResponse>(
      `${environment.apiBaseUrl}/api/auth/change-password`,
      request);
  }

  getAccessToken(): string | null {
    return this.getValidSession()?.accessToken ?? null;
  }

  hasPermission(permissionCode: ApplicationPermissionCode): boolean {
    return this.currentPermissions().includes(permissionCode);
  }

  logout(): void {
    this.clearSession();
    void this.router.navigate(['/login']);
  }

  handleUnauthorized(): void {
    const currentUrl = this.router.url.startsWith('/login') ? null : this.router.url;
    this.clearSession();

    void this.router.navigate(
      ['/login'],
      currentUrl ? { queryParams: { returnUrl: currentUrl } } : undefined);
  }

  syncCurrentSession(currentSession: CurrentSessionResponse): void {
    const existingSession = this.getValidSession();
    if (!existingSession) {
      return;
    }

    this.persistSession({
      ...existingSession,
      expiresAtUtc: currentSession.expiresAtUtc,
      user: currentSession.user
    });
  }

  private getValidSession(): AuthSession | null {
    const session = this.sessionState();
    if (!session) {
      return null;
    }

    if (this.isExpired(session.expiresAtUtc)) {
      this.clearSession();
      return null;
    }

    return session;
  }

  private setSession(response: LoginResponse): void {
    this.persistSession({
      accessToken: response.accessToken,
      expiresAtUtc: response.expiresAtUtc,
      user: response.user
    });
  }

  private persistSession(session: AuthSession): void {
    if (this.isExpired(session.expiresAtUtc)) {
      this.clearSession();
      return;
    }

    this.sessionState.set(session);
    globalThis.sessionStorage?.setItem(this.storageKey, JSON.stringify(session));
  }

  private clearSession(): void {
    this.sessionState.set(null);
    globalThis.sessionStorage?.removeItem(this.storageKey);
  }

  private restoreSession(): AuthSession | null {
    const storedSession = globalThis.sessionStorage?.getItem(this.storageKey);
    if (!storedSession) {
      return null;
    }

    try {
      const parsedSession = JSON.parse(storedSession) as AuthSession;
      if (!parsedSession.accessToken
        || !parsedSession.expiresAtUtc
        || !parsedSession.user?.id
        || !parsedSession.user?.roleCode
        || !Array.isArray(parsedSession.user.permissions)) {
        globalThis.sessionStorage?.removeItem(this.storageKey);
        return null;
      }

      if (this.isExpired(parsedSession.expiresAtUtc)) {
        globalThis.sessionStorage?.removeItem(this.storageKey);
        return null;
      }

      return parsedSession;
    } catch {
      globalThis.sessionStorage?.removeItem(this.storageKey);
      return null;
    }
  }

  private isExpired(expiresAtUtc: string): boolean {
    const expirationTimestamp = Date.parse(expiresAtUtc);
    return Number.isNaN(expirationTimestamp) || expirationTimestamp <= Date.now() + 30_000;
  }

  private hasAnyPermission(permissionCodes: ApplicationPermissionCode[]): boolean {
    return permissionCodes.some((permissionCode) => this.hasPermission(permissionCode));
  }
}
