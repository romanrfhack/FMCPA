import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly navigation = [
    { path: '/dashboard', label: 'Dashboard', exact: true },
    { path: '/history', label: 'Historico' },
    { path: '/commissions', label: 'Comisiones' },
    { path: '/bitacora', label: 'Bitacora' },
    { path: '/contacts', label: 'Contactos' },
    { path: '/catalogs/commission-types', label: 'Tipos de comision' },
    { path: '/catalogs/evidence-types', label: 'Tipos de evidencia' },
    { path: '/catalogs/module-statuses', label: 'Estatus por modulo' },
    { path: '/markets', label: 'Mercados' },
    { path: '/donatarias', label: 'Donatarias' },
    { path: '/financials', label: 'Financieras' },
    { path: '/federation', label: 'Federacion' }
  ];
}
