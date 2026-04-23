import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly authService = inject(AuthService);

  protected readonly navigation = [
    { path: '/dashboard', label: 'Dashboard', exact: true },
    { path: '/history', label: 'Historico' },
    { path: '/commissions', label: 'Comisiones' },
    { path: '/bitacora', label: 'Bitacora' },
    { path: '/contacts', label: 'Contactos' },
    { path: '/admin/users', label: 'Usuarios', requiresAdmin: true },
    { path: '/catalogs/commission-types', label: 'Tipos de comision', requiresAdmin: true },
    { path: '/catalogs/evidence-types', label: 'Tipos de evidencia', requiresAdmin: true },
    { path: '/catalogs/module-statuses', label: 'Estatus por modulo', requiresAdmin: true },
    { path: '/markets', label: 'Mercados' },
    { path: '/donatarias', label: 'Donatarias' },
    { path: '/financials', label: 'Financieras' },
    { path: '/federation', label: 'Federacion' }
  ];

  protected readonly currentUser = this.authService.currentUser;
  protected readonly currentRole = this.authService.currentRole;
  protected readonly canAdminister = this.authService.canAdminister;
  protected readonly isAuthenticated = this.authService.isAuthenticated;
  protected readonly visibleNavigation = computed(() =>
    this.navigation.filter((item) => !item.requiresAdmin || this.canAdminister()));
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
}
