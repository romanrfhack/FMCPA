import { Component, HostListener, computed, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { ApplicationPermissionCode } from './core/models/auth.models';
import { AuthService } from './core/services/auth.service';

type NavigationItem = {
  path: string;
  label: string;
  exact?: boolean;
  requiredPermission?: ApplicationPermissionCode;
  requiredAnyPermissions?: ApplicationPermissionCode[];
};

type NavigationGroup = {
  id: string;
  label: string;
  items: NavigationItem[];
};

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly authService = inject(AuthService);

  protected readonly navigationGroups: NavigationGroup[] = [
    {
      id: 'shell-nav-inicio',
      label: 'Inicio',
      items: [
        { path: '/dashboard', label: 'Dashboard', exact: true, requiredPermission: 'DASHBOARD_READ' },
        { path: '/operations', label: 'Centro operativo', requiredPermission: 'DASHBOARD_READ' }
      ]
    },
    {
      id: 'shell-nav-operacion',
      label: 'Operación',
      items: [
        { path: '/markets', label: 'Mercados', requiredPermission: 'MARKETS_READ' },
        { path: '/donatarias', label: 'Donatarias', requiredPermission: 'DONATIONS_READ' },
        { path: '/financials', label: 'Financieras', requiredPermission: 'FINANCIALS_READ' },
        { path: '/federation', label: 'Federación', requiredPermission: 'FEDERATION_READ' }
      ]
    },
    {
      id: 'shell-nav-control',
      label: 'Control',
      items: [
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
        { path: '/documents/review', label: 'Revisión documental', requiredPermission: 'USERS_ADMIN' },
        { path: '/commissions', label: 'Comisiones', requiredPermission: 'HISTORY_READ' },
        { path: '/history', label: 'Histórico', requiredPermission: 'HISTORY_READ' },
        { path: '/bitacora', label: 'Bitácora', requiredPermission: 'HISTORY_READ' }
      ]
    },
    {
      id: 'shell-nav-administracion',
      label: 'Administración',
      items: [
        { path: '/contacts', label: 'Contactos', requiredPermission: 'CONTACTS_READ' },
        { path: '/admin/users', label: 'Usuarios', requiredPermission: 'USERS_ADMIN' },
        { path: '/admin/security', label: 'Seguridad', requiredPermission: 'USERS_ADMIN' },
        { path: '/catalogs/commission-types', label: 'Tipos de comisión', requiredPermission: 'CATALOGS_ADMIN' },
        { path: '/catalogs/evidence-types', label: 'Tipos de evidencia', requiredPermission: 'CATALOGS_ADMIN' },
        { path: '/catalogs/module-statuses', label: 'Estatus por módulo', requiredPermission: 'CATALOGS_ADMIN' }
      ]
    }
  ];

  protected readonly currentUser = this.authService.currentUser;
  protected readonly isAuthenticated = this.authService.isAuthenticated;
  protected readonly isUserMenuOpen = signal(false);
  protected readonly visibleNavigationGroups = computed(() =>
    this.navigationGroups
      .map((group) => ({
        ...group,
        items: group.items.filter((item) => this.hasNavigationAccess(item))
      }))
      .filter((group) => group.items.length > 0));
  protected readonly currentUserDisplayName = computed(() => {
    const user = this.currentUser();
    return user?.displayName?.trim() || user?.userName?.trim() || 'Usuario';
  });
  protected readonly currentUserInitials = computed(() => {
    const displayName = this.currentUserDisplayName();
    const parts = displayName
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2);

    return (parts.length ? parts.map((part) => part[0]).join('') : 'U').toUpperCase();
  });

  @HostListener('document:click')
  protected closeUserMenu(): void {
    this.isUserMenuOpen.set(false);
  }

  @HostListener('document:keydown.escape')
  protected closeUserMenuWithEscape(): void {
    this.isUserMenuOpen.set(false);
  }

  protected toggleUserMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.isUserMenuOpen.update((isOpen) => !isOpen);
  }

  protected keepUserMenuOpen(event: MouseEvent): void {
    event.stopPropagation();
  }

  protected closeUserMenuFromAction(): void {
    this.isUserMenuOpen.set(false);
  }

  protected logout(): void {
    this.closeUserMenuFromAction();
    this.authService.logout();
  }

  private hasNavigationAccess(item: NavigationItem): boolean {
    if (item.requiredPermission && this.authService.hasPermission(item.requiredPermission)) {
      return true;
    }

    if (item.requiredAnyPermissions?.some((permissionCode) => this.authService.hasPermission(permissionCode))) {
      return true;
    }

    return !item.requiredPermission && !item.requiredAnyPermissions?.length;
  }
}
