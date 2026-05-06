import '@angular/compiler';
import { HttpErrorResponse, HttpRequest, HttpResponse } from '@angular/common/http';
import { Injector, runInInjectionContext } from '@angular/core';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { AuthService } from '../services/auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  it('adds the bearer token to protected API requests', () => {
    const authService = {
      getAccessToken: vi.fn(() => 'session-token'),
      handleUnauthorized: vi.fn()
    };
    const observedRequests: HttpRequest<unknown>[] = [];
    const injector = Injector.create({
      providers: [
        {
          provide: AuthService,
          useValue: authService
        }
      ]
    });

    runInInjectionContext(injector, () =>
      authInterceptor(new HttpRequest('GET', '/api/auth/session'), (request) => {
        observedRequests.push(request);
        return of(new HttpResponse({ status: 200 }));
      })).subscribe();

    expect(observedRequests).toHaveLength(1);
    expect(observedRequests[0].headers.get('Authorization')).toBe('Bearer session-token');
  });

  it('adds the web client header to unsafe API requests', () => {
    const authService = {
      getAccessToken: vi.fn(() => null),
      handleUnauthorized: vi.fn()
    };
    const observedRequests: HttpRequest<unknown>[] = [];
    const injector = Injector.create({
      providers: [
        {
          provide: AuthService,
          useValue: authService
        }
      ]
    });

    runInInjectionContext(injector, () =>
      authInterceptor(new HttpRequest('POST', '/api/auth/login', {}), (request) => {
        observedRequests.push(request);
        return of(new HttpResponse({ status: 200 }));
      })).subscribe();

    expect(observedRequests).toHaveLength(1);
    expect(observedRequests[0].headers.get('X-FMCPA-Client')).toBe('FMCPA-Web');
    expect(observedRequests[0].headers.has('Authorization')).toBe(false);
  });

  it('handles unauthorized responses from protected API requests', () => {
    const authService = {
      getAccessToken: vi.fn(() => 'stale-token'),
      handleUnauthorized: vi.fn()
    };
    const injector = Injector.create({
      providers: [
        {
          provide: AuthService,
          useValue: authService
        }
      ]
    });
    let observedError: unknown;

    runInInjectionContext(injector, () =>
      authInterceptor(new HttpRequest('GET', '/api/auth/session'), () =>
        throwError(() => new HttpErrorResponse({ status: 401 })))).subscribe({
      error: (error) => {
        observedError = error;
      }
    });

    expect(authService.handleUnauthorized).toHaveBeenCalledTimes(1);
    expect(observedError).toBeInstanceOf(HttpErrorResponse);
  });
});
