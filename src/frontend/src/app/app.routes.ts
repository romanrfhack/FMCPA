import { Routes } from '@angular/router';

import { adminOnlyGuard, authChildGuard, guestOnlyGuard } from './core/guards/auth.guard';

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
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard-page.component').then(
            (module) => module.DashboardPageComponent)
      },
      {
        path: 'markets',
        loadComponent: () =>
          import('./features/markets/markets-page.component').then(
            (module) => module.MarketsPageComponent)
      },
      {
        path: 'donatarias',
        loadComponent: () =>
          import('./features/donatarias/donatarias-page.component').then(
            (module) => module.DonatariasPageComponent)
      },
      {
        path: 'financials',
        loadComponent: () =>
          import('./features/financials/financials-page.component').then(
            (module) => module.FinancialsPageComponent)
      },
      {
        path: 'federation',
        loadComponent: () =>
          import('./features/federation/federation-page.component').then(
            (module) => module.FederationPageComponent)
      },
      {
        path: 'commissions',
        loadComponent: () =>
          import('./features/commissions/commissions-page.component').then(
            (module) => module.CommissionsPageComponent)
      },
      {
        path: 'contacts',
        loadComponent: () =>
          import('./features/contacts/contacts-page.component').then(
            (module) => module.ContactsPageComponent)
      },
      {
        path: 'admin/users',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./features/admin/users-admin-page.component').then(
            (module) => module.UsersAdminPageComponent)
      },
      {
        path: 'catalogs/commission-types',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./features/shared-catalogs/commission-types-page.component').then(
            (module) => module.CommissionTypesPageComponent)
      },
      {
        path: 'catalogs/evidence-types',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./features/shared-catalogs/evidence-types-page.component').then(
            (module) => module.EvidenceTypesPageComponent)
      },
      {
        path: 'catalogs/module-statuses',
        canActivate: [adminOnlyGuard],
        loadComponent: () =>
          import('./features/shared-catalogs/module-statuses-page.component').then(
            (module) => module.ModuleStatusesPageComponent)
      },
      {
        path: 'bitacora',
        loadComponent: () =>
          import('./features/bitacora/bitacora-page.component').then(
            (module) => module.BitacoraPageComponent)
      },
      {
        path: 'history',
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
