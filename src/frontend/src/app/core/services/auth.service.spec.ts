import '@angular/compiler';
import { HttpClient } from '@angular/common/http';
import { Injector } from '@angular/core';
import { Router } from '@angular/router';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from './auth.service';

const storageKey = 'fmcpa.auth.session';

describe('AuthService', () => {
  afterEach(() => {
    ensureSessionStorage().clear();
  });

  it('derives frontend capabilities from session permissions', () => {
    const sessionStorage = ensureSessionStorage();
    sessionStorage.setItem(
      storageKey,
      JSON.stringify({
        accessToken: 'operator-token',
        expiresAtUtc: new Date(Date.now() + 60 * 60_000).toISOString(),
        user: {
          id: '2ca1b93e-2b72-4a29-b942-18de39c1a2e4',
          userName: 'operator',
          displayName: 'Operator',
          roleCode: 'OPERATOR',
          permissions: [
            'DASHBOARD_READ',
            'HISTORY_READ',
            'CONTACTS_READ',
            'CONTACTS_WRITE',
            'MARKETS_READ',
            'MARKETS_WRITE',
            'DONATIONS_READ',
            'DONATIONS_WRITE',
            'FINANCIALS_READ',
            'FINANCIALS_WRITE',
            'FEDERATION_READ',
            'FEDERATION_WRITE',
            'CATALOGS_READ'
          ]
        }
      }));

    const authService = createAuthService();

    expect(authService.canWriteMarkets()).toBe(true);
    expect(authService.canAdministerUsers()).toBe(false);
    expect(authService.hasPermission('USERS_ADMIN')).toBe(false);
  });

  it('clears the local session and redirects to login when a protected request becomes unauthorized', () => {
    const sessionStorage = ensureSessionStorage();
    sessionStorage.setItem(
      storageKey,
      JSON.stringify({
        accessToken: 'stale-token',
        expiresAtUtc: new Date(Date.now() + 60 * 60_000).toISOString(),
        user: {
          id: '2ca1b93e-2b72-4a29-b942-18de39c1a2e4',
          userName: 'operator',
          displayName: 'Operator',
          roleCode: 'OPERATOR',
          permissions: [
            'DASHBOARD_READ',
            'HISTORY_READ',
            'CONTACTS_READ',
            'CONTACTS_WRITE',
            'MARKETS_READ',
            'MARKETS_WRITE',
            'DONATIONS_READ',
            'DONATIONS_WRITE',
            'FINANCIALS_READ',
            'FINANCIALS_WRITE',
            'FEDERATION_READ',
            'FEDERATION_WRITE',
            'CATALOGS_READ'
          ]
        }
      }));

    const navigate = vi.fn(() => Promise.resolve(true));
    const authService = createAuthService(navigate);

    expect(authService.isAuthenticated()).toBe(true);

    authService.handleUnauthorized();

    expect(authService.currentSession()).toBeNull();
    expect(sessionStorage.getItem(storageKey)).toBeNull();
    expect(navigate).toHaveBeenCalledWith(['/login'], {
      queryParams: {
        returnUrl: '/dashboard'
      }
    });
  });
});

function createAuthService(navigate = vi.fn(() => Promise.resolve(true))): AuthService {
  const injector = Injector.create({
    providers: [
      AuthService,
      {
        provide: HttpClient,
        useValue: {
          post: vi.fn()
        }
      },
      {
        provide: Router,
        useValue: {
          url: '/dashboard',
          navigate
        }
      }
    ]
  });

  return injector.get(AuthService);
}

function ensureSessionStorage(): Storage {
  if (globalThis.sessionStorage) {
    return globalThis.sessionStorage;
  }

  const data = new Map<string, string>();
  const fallbackSessionStorage = {
    get length() {
      return data.size;
    },
    clear: () => data.clear(),
    getItem: (key: string) => data.get(key) ?? null,
    key: (index: number) => Array.from(data.keys())[index] ?? null,
    removeItem: (key: string) => data.delete(key),
    setItem: (key: string, value: string) => data.set(key, value)
  } satisfies Storage;

  Object.defineProperty(globalThis, 'sessionStorage', {
    configurable: true,
    value: fallbackSessionStorage
  });

  return fallbackSessionStorage;
}
