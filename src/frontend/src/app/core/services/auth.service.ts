import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ApplicationRoleCode,
  AuthSession,
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
  readonly isAuthenticated = computed(() => {
    const session = this.sessionState();
    return session !== null && !this.isExpired(session.expiresAtUtc);
  });
  readonly canWrite = computed(() => {
    const roleCode = this.currentRole();
    return roleCode === 'ADMIN' || roleCode === 'OPERATOR';
  });
  readonly canAdminister = computed(() => this.currentRole() === 'ADMIN');

  login(request: LoginRequest) {
    return this.httpClient
      .post<LoginResponse>(`${environment.apiBaseUrl}/api/auth/login`, request)
      .pipe(tap((response) => this.setSession(response)));
  }

  getAccessToken(): string | null {
    return this.getValidSession()?.accessToken ?? null;
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
      if (!parsedSession.accessToken || !parsedSession.expiresAtUtc || !parsedSession.user?.id || !parsedSession.user?.roleCode) {
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
}
