import '@angular/compiler';
import { Injector, runInInjectionContext } from '@angular/core';
import { Router } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';

import { AuthService } from '../services/auth.service';
import { adminOnlyGuard, authChildGuard, guestOnlyGuard, permissionGuard } from './auth.guard';

describe('auth guards', () => {
  it('redirects unauthenticated child navigation to login', () => {
    const createUrlTree = vi.fn((commands: unknown[], extras?: unknown) => ({
      commands,
      extras
    }));

    const injector = Injector.create({
      providers: [
        {
          provide: Router,
          useValue: {
            createUrlTree,
            url: '/dashboard'
          }
        },
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: vi.fn(() => false)
          }
        }
      ]
    });

    const result = runInInjectionContext(injector, () =>
      authChildGuard({} as never, { url: '/dashboard' } as never));

    expect(result).toEqual({
      commands: ['/login'],
      extras: {
        queryParams: {
          returnUrl: '/dashboard'
        }
      }
    });
  });

  it('allows authenticated child navigation', () => {
    const createUrlTree = vi.fn((commands: unknown[], extras?: unknown) => ({
      commands,
      extras
    }));

    const injector = Injector.create({
      providers: [
        {
          provide: Router,
          useValue: {
            createUrlTree,
            url: '/dashboard'
          }
        },
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: vi.fn(() => true)
          }
        }
      ]
    });

    const result = runInInjectionContext(injector, () =>
      authChildGuard({} as never, { url: '/dashboard' } as never));

    expect(result).toBe(true);
    expect(createUrlTree).not.toHaveBeenCalled();
  });

  it('redirects authenticated guests away from login', () => {
    const createUrlTree = vi.fn((commands: unknown[], extras?: unknown) => ({
      commands,
      extras
    }));

    const injector = Injector.create({
      providers: [
        {
          provide: Router,
          useValue: {
            createUrlTree,
            url: '/login'
          }
        },
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: vi.fn(() => true)
          }
        }
      ]
    });

    const result = runInInjectionContext(injector, () => guestOnlyGuard({} as never, {} as never));

    expect(result).toEqual({
      commands: ['/dashboard'],
      extras: undefined
    });
  });

  it('redirects authenticated non-admin users away from admin-only routes', () => {
    const createUrlTree = vi.fn((commands: unknown[], extras?: unknown) => ({
      commands,
      extras
    }));

    const injector = Injector.create({
      providers: [
        {
          provide: Router,
          useValue: {
            createUrlTree,
            url: '/catalogs/commission-types'
          }
        },
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: vi.fn(() => true),
            canAdminister: vi.fn(() => false)
          }
        }
      ]
    });

    const result = runInInjectionContext(injector, () =>
      adminOnlyGuard({} as never, { url: '/catalogs/commission-types' } as never));

    expect(result).toEqual({
      commands: ['/dashboard'],
      extras: undefined
    });
  });

  it('allows authenticated users when the required permission is present', () => {
    const createUrlTree = vi.fn((commands: unknown[], extras?: unknown) => ({
      commands,
      extras
    }));

    const injector = Injector.create({
      providers: [
        {
          provide: Router,
          useValue: {
            createUrlTree,
            url: '/markets'
          }
        },
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: vi.fn(() => true),
            hasPermission: vi.fn((permissionCode: string) => permissionCode === 'MARKETS_READ')
          }
        }
      ]
    });

    const result = runInInjectionContext(injector, () =>
      permissionGuard({ data: { requiredPermission: 'MARKETS_READ' } } as never, { url: '/markets' } as never));

    expect(result).toBe(true);
    expect(createUrlTree).not.toHaveBeenCalled();
  });

  it('redirects authenticated users away from routes without the required permission', () => {
    const createUrlTree = vi.fn((commands: unknown[], extras?: unknown) => ({
      commands,
      extras
    }));

    const injector = Injector.create({
      providers: [
        {
          provide: Router,
          useValue: {
            createUrlTree,
            url: '/admin/users'
          }
        },
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: vi.fn(() => true),
            hasPermission: vi.fn(() => false)
          }
        }
      ]
    });

    const result = runInInjectionContext(injector, () =>
      permissionGuard({ data: { requiredPermission: 'USERS_ADMIN' } } as never, { url: '/admin/users' } as never));

    expect(result).toEqual({
      commands: ['/dashboard'],
      extras: undefined
    });
  });
});
