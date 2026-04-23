import '@angular/compiler';
import { describe, expect, it } from 'vitest';

import { adminOnlyGuard } from './core/guards/auth.guard';
import { routes } from './app.routes';

describe('app routes', () => {
  it('protects the admin users route with adminOnlyGuard', () => {
    const shellRoute = routes.find((route) => route.path === '');
    const adminUsersRoute = shellRoute?.children?.find((route) => route.path === 'admin/users');

    expect(adminUsersRoute).toBeDefined();
    expect(adminUsersRoute?.canActivate).toContain(adminOnlyGuard);
  });
});
