import '@angular/compiler';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { MarketDetail, MarketIssue, MarketSummary, MarketTenantAlert } from '../../core/models/markets.models';
import { Contact, ContactIntervention, ModuleStatusCatalogEntry } from '../../core/models/shared-catalogs.models';
import { AuthService } from '../../core/services/auth.service';
import { MarketsService } from '../../core/services/markets.service';
import { SharedCatalogsService } from '../../core/services/shared-catalogs.service';
import { ContactInterventionLauncherComponent } from '../contacts/contact-intervention-launcher.component';
import { MarketsPageComponent } from './markets-page.component';

const contact: Contact = {
  id: 'contact-1',
  name: 'Enlace Operativo',
  contactTypeId: 1,
  contactTypeCode: 'EXTERNAL',
  contactTypeName: 'Externo',
  organizationOrDependency: 'Alcaldia Centro',
  roleTitle: 'Enlace',
  mobilePhone: '5512345678',
  whatsAppPhone: '5512345678',
  email: 'enlace@example.com',
  notes: null,
  createdUtc: '2026-05-01T12:00:00Z',
  updatedUtc: null
};

const issue: MarketIssue = {
  id: 'issue-1',
  marketId: 'market-1',
  issueType: 'Mejora de limpieza',
  description: 'Seguimiento de limpieza en pasillo principal.',
  issueDate: '2026-05-10',
  advanceSummary: 'Pendiente de coordinar brigada.',
  statusCatalogEntryId: 2201,
  statusCode: 'IN_PROGRESS',
  statusName: 'En seguimiento',
  statusIsClosed: false,
  followUpOrResolution: null,
  finalSatisfaction: null,
  createdUtc: '2026-05-10T12:00:00Z'
};

const marketSummary: MarketSummary = {
  id: 'market-1',
  name: 'Mercado Hidalgo',
  borough: 'Centro',
  statusCatalogEntryId: 2101,
  statusCode: 'ACTIVE',
  statusName: 'Activo',
  statusIsClosed: false,
  statusAlertsEnabledByDefault: true,
  secretaryGeneralContactId: 'contact-1',
  secretaryGeneralName: 'Secretaria General',
  notes: null,
  tenantCount: 0,
  issueCount: 1,
  activeTenantAlertsCount: 0,
  createdUtc: '2026-05-01T12:00:00Z',
  updatedUtc: null
};

const marketDetail: MarketDetail = {
  ...marketSummary,
  tenants: [],
  issues: [issue]
};

const marketStatus: ModuleStatusCatalogEntry = {
  id: 2101,
  moduleCode: 'MARKETS',
  moduleName: 'Mercados',
  contextCode: 'MARKET',
  contextName: 'Mercado',
  statusCode: 'ACTIVE',
  statusName: 'Activo',
  description: null,
  sortOrder: 1,
  isClosed: false,
  alertsEnabledByDefault: true,
  isActive: true
};

const issueStatus: ModuleStatusCatalogEntry = {
  id: 2201,
  moduleCode: 'MARKETS',
  moduleName: 'Mercados',
  contextCode: 'MARKET_ISSUE',
  contextName: 'Incidencia',
  statusCode: 'IN_PROGRESS',
  statusName: 'En seguimiento',
  description: null,
  sortOrder: 1,
  isClosed: false,
  alertsEnabledByDefault: true,
  isActive: true
};

const intervention: ContactIntervention = {
  id: 'intervention-1',
  contactId: 'contact-1',
  contactName: 'Enlace Operativo',
  contactTypeName: 'Externo',
  moduleKey: 'MARKETS',
  originType: 'MARKET_ISSUE',
  originId: issue.id,
  originDisplayName: 'Mercado Hidalgo · Mejora de limpieza · 2026-05-10',
  subject: 'Apoyo en incidencia de mercado: Mejora de limpieza',
  helpType: 'UNBLOCKING',
  outcome: 'USEFUL',
  notes: 'Ayudo a coordinar.',
  occurredUtc: '2026-05-16T18:30:00Z',
  createdByUserId: 'user-1',
  createdByUserName: 'Operadora',
  createdUtc: '2026-05-16T18:45:00Z',
  updatedUtc: null,
  archivedUtc: null
};

describe('MarketsPageComponent contact intervention pilot', () => {
  it('renders the contact support launcher for market issues with the expected context', async () => {
    const { fixture } = await renderMarketsPage();

    showIssuesTab(fixture);
    fixture.detectChanges();

    const launcherDebug = fixture.debugElement.query(By.directive(ContactInterventionLauncherComponent));
    expect(launcherDebug).not.toBeNull();

    const launcher = launcherDebug.componentInstance as ContactInterventionLauncherComponent;
    expect(launcher.moduleKey()).toBe('MARKETS');
    expect(launcher.originType()).toBe('MARKET_ISSUE');
    expect(launcher.originId()).toBe(issue.id);
    expect(launcher.originDisplayName()).toBe('Mercado Hidalgo · Mejora de limpieza · 2026-05-10');
    expect(launcher.defaultSubject()).toBe('Apoyo en incidencia de mercado: Mejora de limpieza');
    expect(launcher.buttonLabel()).toBe('Vincular apoyo de contacto');
    expect(launcher.disabled()).toBe(false);
  });

  it('does not render the launcher when the user cannot write market operations', async () => {
    const { fixture } = await renderMarketsPage({ canWriteMarkets: false });

    showIssuesTab(fixture);
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.directive(ContactInterventionLauncherComponent))).toBeNull();
  });

  it('does not render the launcher when the user cannot read contacts', async () => {
    const { fixture } = await renderMarketsPage({ canReadContacts: false });

    showIssuesTab(fixture);
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.directive(ContactInterventionLauncherComponent))).toBeNull();
  });

  it('keeps the issue list stable and shows local feedback when a contact intervention is saved', async () => {
    const { fixture } = await renderMarketsPage();

    showIssuesTab(fixture);
    fixture.detectChanges();

    const launcher = fixture.debugElement.query(By.directive(ContactInterventionLauncherComponent))
      .componentInstance as ContactInterventionLauncherComponent;
    launcher.saved.emit(intervention);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Apoyo de contacto vinculado a la incidencia: Mejora de limpieza.');
    expect(compiled.textContent).toContain('Mejora de limpieza');
  });
});

async function renderMarketsPage(options: {
  canWriteMarkets?: boolean;
  canReadContacts?: boolean;
  market?: MarketDetail;
} = {}) {
  const selectedMarket = options.market ?? marketDetail;
  const marketAlerts: MarketTenantAlert[] = [];
  const marketsService = {
    listMarkets: vi.fn(() => of([marketSummary])),
    getMarket: vi.fn(() => of(selectedMarket)),
    createMarket: vi.fn(),
    closeMarket: vi.fn(),
    getMarketTenants: vi.fn(),
    createMarketTenant: vi.fn(),
    uploadTenantCertificate: vi.fn(),
    getMarketIssues: vi.fn(),
    createMarketIssue: vi.fn(),
    getTenantAlerts: vi.fn(() => of(marketAlerts)),
    downloadTenantCertificate: vi.fn()
  };
  const sharedCatalogsService = {
    getContacts: vi.fn(() => of([contact])),
    getModuleStatuses: vi.fn((moduleCode?: string, contextCode?: string) =>
      of(contextCode === 'MARKET_ISSUE' ? [issueStatus] : [marketStatus])),
    createContactIntervention: vi.fn(() => of(intervention))
  };

  TestBed.resetTestingModule();
  await TestBed.configureTestingModule({
    imports: [MarketsPageComponent],
    providers: [
      {
        provide: AuthService,
        useValue: {
          canWriteMarkets: signal(options.canWriteMarkets ?? true),
          canReadContacts: signal(options.canReadContacts ?? true),
          canAdministerFormalClose: signal(false)
        }
      },
      {
        provide: MarketsService,
        useValue: marketsService
      },
      {
        provide: SharedCatalogsService,
        useValue: sharedCatalogsService
      }
    ]
  }).compileComponents();

  const fixture = TestBed.createComponent(MarketsPageComponent);
  fixture.detectChanges();
  await fixture.whenStable();
  await Promise.resolve();

  const component = fixture.componentInstance as unknown as {
    selectedMarketId: { set(value: string | null): void };
    selectedMarket: { set(value: MarketDetail | null): void };
  };
  component.selectedMarketId.set(selectedMarket.id);
  component.selectedMarket.set(selectedMarket);
  fixture.detectChanges();

  return {
    fixture,
    marketsService,
    sharedCatalogsService
  };
}

function showIssuesTab(fixture: ComponentFixture<MarketsPageComponent>) {
  (fixture.componentInstance as unknown as { setActiveTab(tab: 'issues'): void }).setActiveTab('issues');
}
