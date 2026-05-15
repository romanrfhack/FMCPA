import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { afterEach } from 'vitest';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])]
    }).compileComponents();
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render title for an authenticated shell', async () => {
    sessionStorage.setItem(
      'fmcpa.auth.session',
      JSON.stringify({
        accessToken: 'test-token',
        expiresAtUtc: new Date(Date.now() + 60 * 60_000).toISOString(),
        user: {
          id: '2ca1b93e-2b72-4a29-b942-18de39c1a2e4',
          userName: 'operator',
          displayName: 'Operadora Interna',
          roleCode: 'OPERATOR',
          permissions: ['DASHBOARD_READ']
        }
      }));

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('FMCPA Platform');
    expect(compiled.querySelector<HTMLImageElement>('.shell-logo')?.src).toContain('/assets/brand/logo-fmcpa.webp');
    expect(compiled.textContent).toContain('Gestión operativa');
    expect(compiled.textContent).toContain('Operadora Interna');
    expect(compiled.textContent).not.toContain('Track 2 Seguridad');
    expect(compiled.textContent).not.toContain('Angular 21');
    expect(compiled.textContent).not.toContain('.NET 10');
    expect(compiled.textContent).not.toContain('JWT Local');
    expect(compiled.textContent).not.toContain('MVP operativo');
    expect(compiled.textContent).not.toContain('OPERATOR');
  });

  it('should expose account actions from the compact user menu', async () => {
    sessionStorage.setItem(
      'fmcpa.auth.session',
      JSON.stringify({
        accessToken: 'test-token',
        expiresAtUtc: new Date(Date.now() + 60 * 60_000).toISOString(),
        user: {
          id: '2ca1b93e-2b72-4a29-b942-18de39c1a2e4',
          userName: 'admin',
          displayName: 'Administración FMCPA',
          roleCode: 'ADMIN',
          permissions: ['DASHBOARD_READ']
        }
      }));

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    const trigger = compiled.querySelector<HTMLButtonElement>('.user-menu-trigger');
    expect(trigger?.getAttribute('aria-expanded')).toBe('false');
    expect(compiled.textContent).not.toContain('Cambiar contraseña');
    expect(compiled.textContent).not.toContain('Cerrar sesión');

    trigger?.click();
    fixture.detectChanges();
    await fixture.whenStable();

    const passwordLink = compiled.querySelector<HTMLAnchorElement>('.user-menu-panel a');
    const logoutButton = compiled.querySelector<HTMLButtonElement>('.user-menu-panel button');
    expect(trigger?.getAttribute('aria-expanded')).toBe('true');
    expect(passwordLink?.getAttribute('href')).toBe('/account/password');
    expect(passwordLink?.textContent).toContain('Cambiar contraseña');
    expect(logoutButton?.textContent).toContain('Cerrar sesión');
    expect(compiled.textContent).not.toContain('admin · Admin');
  });

  it('should group and filter shell navigation from session permissions', async () => {
    sessionStorage.setItem(
      'fmcpa.auth.session',
      JSON.stringify({
        accessToken: 'operator-token',
        expiresAtUtc: new Date(Date.now() + 60 * 60_000).toISOString(),
        user: {
          id: '2ca1b93e-2b72-4a29-b942-18de39c1a2e4',
          userName: 'operator',
          displayName: 'Operator',
          roleCode: 'OPERATOR',
          permissions: ['DASHBOARD_READ', 'MARKETS_READ', 'MARKETS_WRITE']
        }
      }));

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    const visibleLabels = Array.from(compiled.querySelectorAll('nav a'))
      .map((item) => item.textContent?.trim());
    const visibleGroups = Array.from(compiled.querySelectorAll('.shell-nav-heading'))
      .map((item) => item.textContent?.trim());

    expect(visibleLabels).toContain('Dashboard');
    expect(visibleLabels).toContain('Centro operativo');
    expect(visibleLabels).toContain('Mercados');
    expect(visibleLabels).toContain('Documentos');
    expect(visibleLabels).toContain('Bandeja documental');
    expect(visibleLabels).not.toContain('Usuarios');
    expect(visibleLabels).not.toContain('Seguridad');
    expect(visibleLabels).not.toContain('Tipos de comisión');
    expect(visibleGroups).toEqual(['Inicio', 'Operación', 'Control']);
    expect(visibleGroups).not.toContain('Administración');
  });
});
