import '@angular/compiler';
import { describe, expect, it } from 'vitest';

import { permissionGuard } from './core/guards/auth.guard';
import { routes } from './app.routes';

describe('app routes', () => {
  it('protects the admin users route with USERS_ADMIN permission', () => {
    const shellRoute = routes.find((route) => route.path === '');
    const adminUsersRoute = shellRoute?.children?.find((route) => route.path === 'admin/users');

    expect(adminUsersRoute).toBeDefined();
    expect(adminUsersRoute?.canActivate).toContain(permissionGuard);
    expect(adminUsersRoute?.data?.['requiredPermission']).toBe('USERS_ADMIN');
  });

  it('protects the admin security route with USERS_ADMIN permission', () => {
    const shellRoute = routes.find((route) => route.path === '');
    const adminSecurityRoute = shellRoute?.children?.find((route) => route.path === 'admin/security');

    expect(adminSecurityRoute).toBeDefined();
    expect(adminSecurityRoute?.canActivate).toContain(permissionGuard);
    expect(adminSecurityRoute?.data?.['requiredPermission']).toBe('USERS_ADMIN');
  });

  it('exposes self-service password change only under the authenticated shell', () => {
    const shellRoute = routes.find((route) => route.path === '');
    const changePasswordRoute = shellRoute?.children?.find((route) => route.path === 'account/password');

    expect(changePasswordRoute).toBeDefined();
    expect(changePasswordRoute?.canActivate).toBeUndefined();
    expect(changePasswordRoute?.data?.['requiredPermission']).toBeUndefined();
  });

  it('protects the transversal documents route with document module read permissions', () => {
    const shellRoute = routes.find((route) => route.path === '');
    const documentsRoute = shellRoute?.children?.find((route) => route.path === 'documents');

    expect(documentsRoute).toBeDefined();
    expect(documentsRoute?.canActivate).toContain(permissionGuard);
    expect(documentsRoute?.data?.['requiredAnyPermissions']).toEqual([
      'MARKETS_READ',
      'DONATIONS_READ',
      'FEDERATION_READ'
    ]);
  });

  it('protects the retention review route with USERS_ADMIN permission', () => {
    const shellRoute = routes.find((route) => route.path === '');
    const documentsReviewRoute = shellRoute?.children?.find((route) => route.path === 'documents/review');

    expect(documentsReviewRoute).toBeDefined();
    expect(documentsReviewRoute?.canActivate).toContain(permissionGuard);
    expect(documentsReviewRoute?.data?.['requiredPermission']).toBe('USERS_ADMIN');
  });

  it('protects the document work queue route with document module read permissions', () => {
    const shellRoute = routes.find((route) => route.path === '');
    const documentsWorkQueueRoute = shellRoute?.children?.find((route) => route.path === 'documents/work-queue');

    expect(documentsWorkQueueRoute).toBeDefined();
    expect(documentsWorkQueueRoute?.canActivate).toContain(permissionGuard);
    expect(documentsWorkQueueRoute?.data?.['requiredAnyPermissions']).toEqual([
      'MARKETS_READ',
      'DONATIONS_READ',
      'FEDERATION_READ'
    ]);
  });
});
