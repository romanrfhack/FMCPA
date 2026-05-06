import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { ApplicationPermissionCode } from './core/models/auth.models';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly authService = inject(AuthService);

  protected readonly navigation: {
    path: string;
    label: string;
    exact?: boolean;
    requiredPermission?: ApplicationPermissionCode;
    requiredAnyPermissions?: ApplicationPermissionCode[];
  }[] = [
    { path: '/dashboard', label: 'Dashboard', exact: true, requiredPermission: 'DASHBOARD_READ' },
    { path: '/operations', label: 'Operaciones', requiredPermission: 'DASHBOARD_READ' },
    { path: '/history', label: 'Historico', requiredPermission: 'HISTORY_READ' },
    { path: '/commissions', label: 'Comisiones', requiredPermission: 'HISTORY_READ' },
    { path: '/bitacora', label: 'Bitacora', requiredPermission: 'HISTORY_READ' },
    {
      path: '/documents',
      label: 'Documentos',
      requiredAnyPermissions: ['MARKETS_READ', 'DONATIONS_READ', 'FEDERATION_READ']
    },
    {
      path: '/documents/work-queue',
      label: 'Bandeja documental',
      requiredAnyPermissions: ['MARKETS_READ', 'DONATIONS_READ', 'FEDERATION_READ']
    },
    { path: '/documents/review', label: 'Revision documental', requiredPermission: 'USERS_ADMIN' },
    { path: '/contacts', label: 'Contactos', requiredPermission: 'CONTACTS_READ' },
    { path: '/admin/users', label: 'Usuarios', requiredPermission: 'USERS_ADMIN' },
    { path: '/admin/security', label: 'Seguridad', requiredPermission: 'USERS_ADMIN' },
    { path: '/catalogs/commission-types', label: 'Tipos de comision', requiredPermission: 'CATALOGS_ADMIN' },
    { path: '/catalogs/evidence-types', label: 'Tipos de evidencia', requiredPermission: 'CATALOGS_ADMIN' },
    { path: '/catalogs/module-statuses', label: 'Estatus por modulo', requiredPermission: 'CATALOGS_ADMIN' },
    { path: '/markets', label: 'Mercados', requiredPermission: 'MARKETS_READ' },
    { path: '/donatarias', label: 'Donatarias', requiredPermission: 'DONATIONS_READ' },
    { path: '/financials', label: 'Financieras', requiredPermission: 'FINANCIALS_READ' },
    { path: '/federation', label: 'Federacion', requiredPermission: 'FEDERATION_READ' }
  ];

  protected readonly currentUser = this.authService.currentUser;
  protected readonly currentRole = this.authService.currentRole;
  protected readonly isAuthenticated = this.authService.isAuthenticated;
  protected readonly visibleNavigation = computed(() =>
    this.navigation.filter((item) => this.hasNavigationAccess(item)));
  protected readonly currentRoleLabel = computed(() => {
    switch (this.currentRole()) {
      case 'ADMIN':
        return 'Admin';
      case 'OPERATOR':
        return 'Operator';
      case 'READONLY':
        return 'Readonly';
      default:
        return 'Sin rol';
    }
  });

  protected logout() {
    this.authService.logout();
  }

  private hasNavigationAccess(item: {
    requiredPermission?: ApplicationPermissionCode;
    requiredAnyPermissions?: ApplicationPermissionCode[];
  }): boolean {
    if (item.requiredPermission && this.authService.hasPermission(item.requiredPermission)) {
      return true;
    }

    if (item.requiredAnyPermissions?.some((permissionCode) => this.authService.hasPermission(permissionCode))) {
      return true;
    }

    return !item.requiredPermission && !item.requiredAnyPermissions?.length;
  }
}
