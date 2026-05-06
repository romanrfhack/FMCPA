import { Routes } from '@angular/router';

import { authChildGuard, guestOnlyGuard, permissionGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestOnlyGuard],
    loadComponent: () =>
      import('./features/auth/login-page.component').then(
        (module) => module.LoginPageComponent)
  },
  {
    path: '',
    canActivateChild: [authChildGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'account/password',
        loadComponent: () =>
          import('./features/auth/change-password-page.component').then(
            (module) => module.ChangePasswordPageComponent)
      },
      {
        path: 'dashboard',
        canActivate: [permissionGuard],
        data: { requiredPermission: 'DASHBOARD_READ' },
        loadComponent: () =>
          import('./features/dashboard/dashboard-page.component').then(
            (module) => module.DashboardPageComponent)
      },
      {
        path: 'markets',
        canActivate: [permissionGuard],
        data: { requiredPermission: 'MARKETS_READ' },
        loadComponent: () =>
          import('./features/markets/markets-page.component').then(
            (module) => module.MarketsPageComponent)
      },
      {
        path: 'donatarias',
        canActivate: [permissionGuard],
        data: { requiredPermission: 'DONATIONS_READ' },
        loadComponent: () =>
          import('./features/donatarias/donatarias-page.component').then(
            (module) => module.DonatariasPageComponent)
      },
      {
        path: 'financials',
        canActivate: [permissionGuard],
        data: { requiredPermission: 'FINANCIALS_READ' },
        loadComponent: () =>
          import('./features/financials/financials-page.component').then(
            (module) => module.FinancialsPageComponent)
      },
      {
        path: 'federation',
        canActivate: [permissionGuard],
        data: { requiredPermission: 'FEDERATION_READ' },
        loadComponent: () =>
          import('./features/federation/federation-page.component').then(
            (module) => module.FederationPageComponent)
      },
      {
        path: 'commissions',
        canActivate: [permissionGuard],
        data: { requiredPermission: 'HISTORY_READ' },
        loadComponent: () =>
          import('./features/commissions/commissions-page.component').then(
            (module) => module.CommissionsPageComponent)
      },
      {
        path: 'contacts',
        canActivate: [permissionGuard],
        data: { requiredPermission: 'CONTACTS_READ' },
        loadComponent: () =>
          import('./features/contacts/contacts-page.component').then(
            (module) => module.ContactsPageComponent)
      },
      {
        path: 'documents/review',
        canActivate: [permissionGuard],
        data: { requiredPermission: 'USERS_ADMIN' },
        loadComponent: () =>
          import('./features/documents/documents-review-page.component').then(
            (module) => module.DocumentsReviewPageComponent)
      },
      {
        path: 'documents/work-queue',
        canActivate: [permissionGuard],
        data: { requiredAnyPermissions: ['MARKETS_READ', 'DONATIONS_READ', 'FEDERATION_READ'] },
        loadComponent: () =>
          import('./features/documents/documents-work-queue-page.component').then(
            (module) => module.DocumentsWorkQueuePageComponent)
      },
      {
        path: 'documents',
        canActivate: [permissionGuard],
        data: { requiredAnyPermissions: ['MARKETS_READ', 'DONATIONS_READ', 'FEDERATION_READ'] },
        loadComponent: () =>
          import('./features/documents/documents-page.component').then(
            (module) => module.DocumentsPageComponent)
      },
      {
        path: 'admin/users',
        canActivate: [permissionGuard],
        data: { requiredPermission: 'USERS_ADMIN' },
        loadComponent: () =>
          import('./features/admin/users-admin-page.component').then(
            (module) => module.UsersAdminPageComponent)
      },
      {
        path: 'admin/security',
        canActivate: [permissionGuard],
        data: { requiredPermission: 'USERS_ADMIN' },
        loadComponent: () =>
          import('./features/admin/security-admin-page.component').then(
            (module) => module.SecurityAdminPageComponent)
      },
      {
        path: 'catalogs/commission-types',
        canActivate: [permissionGuard],
        data: { requiredPermission: 'CATALOGS_ADMIN' },
        loadComponent: () =>
          import('./features/shared-catalogs/commission-types-page.component').then(
            (module) => module.CommissionTypesPageComponent)
      },
      {
        path: 'catalogs/evidence-types',
        canActivate: [permissionGuard],
        data: { requiredPermission: 'CATALOGS_ADMIN' },
        loadComponent: () =>
          import('./features/shared-catalogs/evidence-types-page.component').then(
            (module) => module.EvidenceTypesPageComponent)
      },
      {
        path: 'catalogs/module-statuses',
        canActivate: [permissionGuard],
        data: { requiredPermission: 'CATALOGS_ADMIN' },
        loadComponent: () =>
          import('./features/shared-catalogs/module-statuses-page.component').then(
            (module) => module.ModuleStatusesPageComponent)
      },
      {
        path: 'bitacora',
        canActivate: [permissionGuard],
        data: { requiredPermission: 'HISTORY_READ' },
        loadComponent: () =>
          import('./features/bitacora/bitacora-page.component').then(
            (module) => module.BitacoraPageComponent)
      },
      {
        path: 'history',
        canActivate: [permissionGuard],
        data: { requiredPermission: 'HISTORY_READ' },
        loadComponent: () =>
          import('./features/history/history-page.component').then(
            (module) => module.HistoryPageComponent)
      },
      {
        path: '**',
        redirectTo: 'dashboard'
      }
    ]
  }
];
