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
          displayName: 'Operator',
          roleCode: 'OPERATOR',
          permissions: ['DASHBOARD_READ']
        }
      }));

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('FMCPA Platform');
  });

  it('should filter shell navigation from session permissions', async () => {
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

    expect(visibleLabels).toContain('Dashboard');
    expect(visibleLabels).toContain('Mercados');
    expect(visibleLabels).toContain('Documentos');
    expect(visibleLabels).toContain('Bandeja documental');
    expect(visibleLabels).not.toContain('Usuarios');
    expect(visibleLabels).not.toContain('Seguridad');
    expect(visibleLabels).not.toContain('Tipos de comision');
  });
});
