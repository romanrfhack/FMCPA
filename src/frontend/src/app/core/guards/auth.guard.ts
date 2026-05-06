import { inject } from '@angular/core';
import { CanActivateChildFn, CanActivateFn, Router } from '@angular/router';

import { ApplicationPermissionCode } from '../models/auth.models';
import { AuthService } from '../services/auth.service';

function redirectToLogin(targetUrl: string) {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login'], {
    queryParams: {
      returnUrl: targetUrl
    }
  });
}

export const authGuard: CanActivateFn = (_, state) => redirectToLogin(state.url);

export const authChildGuard: CanActivateChildFn = (_, state) => redirectToLogin(state.url);

export const guestOnlyGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.isAuthenticated()
    ? router.createUrlTree(['/dashboard'])
    : true;
};

export const adminOnlyGuard: CanActivateFn = (_, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return redirectToLogin(state.url);
  }

  return authService.canAdminister()
    ? true
    : router.createUrlTree(['/dashboard']);
};

export const permissionGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const requiredPermission = route.data?.['requiredPermission'] as ApplicationPermissionCode | undefined;
  const requiredAnyPermissions = route.data?.['requiredAnyPermissions'] as ApplicationPermissionCode[] | undefined;

  if (!authService.isAuthenticated()) {
    return redirectToLogin(state.url);
  }

  if (!requiredPermission || authService.hasPermission(requiredPermission)) {
    if (!requiredAnyPermissions?.length) {
      return true;
    }

    if (requiredAnyPermissions.some((permissionCode) => authService.hasPermission(permissionCode))) {
      return true;
    }
  }

  if (!requiredPermission && requiredAnyPermissions?.some((permissionCode) => authService.hasPermission(permissionCode))) {
    return true;
  }

  return router.createUrlTree(['/dashboard']);
};
