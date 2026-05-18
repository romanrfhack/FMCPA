import '@angular/compiler';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import {
  FederationActionDetail,
  FederationActionSummary,
  FederationModuleAlerts
} from '../../core/models/federation.models';
import { CatalogItem, Contact, ContactIntervention, ModuleStatusCatalogEntry } from '../../core/models/shared-catalogs.models';
import { AuthService } from '../../core/services/auth.service';
import { FederationService } from '../../core/services/federation.service';
import { SharedCatalogsService } from '../../core/services/shared-catalogs.service';
import { ContactInterventionLauncherComponent } from '../../shared/contact-interventions/contact-intervention-launcher.component';
import { FederationPageComponent } from './federation-page.component';

const contact: Contact = {
  id: 'contact-1',
  name: 'Enlace Operativo',
  contactTypeId: 1,
  contactTypeCode: 'EXTERNAL',
  contactTypeName: 'Externo',
  organizationOrDependency: 'Dependencia aliada',
  roleTitle: 'Enlace',
  mobilePhone: '5512345678',
  whatsAppPhone: '5512345678',
  email: 'enlace@example.com',
  notes: null,
  createdUtc: '2026-05-01T12:00:00Z',
  updatedUtc: null
};

const actionSummary: FederationActionSummary = {
  id: 'action-1',
  actionTypeCode: 'GOVERNMENT_MANAGEMENT',
  actionTypeName: 'Gestión con gobierno',
  counterpartyOrInstitution: 'Secretaría de Desarrollo',
  actionDate: '2026-05-12',
  objective: 'Desbloquear seguimiento institucional.',
  statusCatalogEntryId: 4101,
  statusCode: 'IN_PROCESS',
  statusName: 'En proceso',
  statusIsClosed: false,
  statusAlertsEnabledByDefault: true,
  participantCount: 1,
  alertState: 'IN_PROCESS',
  notes: 'Observación operativa.',
  createdUtc: '2026-05-12T12:00:00Z',
  updatedUtc: null
};

const actionDetail: FederationActionDetail = {
  ...actionSummary,
  participants: [
    {
      id: 'participant-1',
      federationActionId: actionSummary.id,
      contactId: 'participant-contact-1',
      participantSide: 'EXTERNAL',
      contactTypeCode: 'EXTERNAL',
      contactTypeName: 'Externo',
      participantName: 'Participante Aliada',
      organizationOrDependency: 'Institución participante',
      roleTitle: 'Coordinadora',
      notes: 'Participante de la gestión.',
      createdUtc: '2026-05-12T12:15:00Z'
    }
  ]
};

const actionStatus: ModuleStatusCatalogEntry = {
  id: 4101,
  moduleCode: 'FEDERATION',
  moduleName: 'Federación',
  contextCode: 'FEDERATION_ACTION',
  contextName: 'Gestión',
  statusCode: 'IN_PROCESS',
  statusName: 'En proceso',
  description: null,
  sortOrder: 1,
  isClosed: false,
  alertsEnabledByDefault: true,
  isActive: true
};

const donationStatus: ModuleStatusCatalogEntry = {
  id: 4201,
  moduleCode: 'FEDERATION',
  moduleName: 'Federación',
  contextCode: 'FEDERATION_DONATION',
  contextName: 'Donación',
  statusCode: 'NOT_APPLIED',
  statusName: 'No aplicada',
  description: null,
  sortOrder: 1,
  isClosed: false,
  alertsEnabledByDefault: true,
  isActive: true
};

const applicationStatus: ModuleStatusCatalogEntry = {
  id: 4301,
  moduleCode: 'FEDERATION',
  moduleName: 'Federación',
  contextCode: 'FEDERATION_DONATION_APPLICATION',
  contextName: 'Aplicación',
  statusCode: 'PARTIALLY_APPLIED',
  statusName: 'Aplicación parcial',
  description: null,
  sortOrder: 1,
  isClosed: false,
  alertsEnabledByDefault: true,
  isActive: true
};

const evidenceType: CatalogItem = {
  id: 1,
  code: 'PHOTO',
  name: 'Fotografía',
  description: null,
  sortOrder: 1,
  isActive: true
};

const commissionType: CatalogItem = {
  id: 2,
  code: 'OPERATIVE',
  name: 'Operativa',
  description: null,
  sortOrder: 1,
  isActive: true
};

const intervention: ContactIntervention = {
  id: 'intervention-1',
  contactId: contact.id,
  contactName: contact.name,
  contactTypeName: contact.contactTypeName,
  moduleKey: 'FEDERATION',
  originType: 'FEDERATION_ACTION',
  originId: actionDetail.id,
  originDisplayName: 'Gestión con gobierno · Secretaría de Desarrollo · 2026-05-12',
  subject: 'Apoyo en gestión federativa: Gestión con gobierno',
  helpType: 'UNBLOCKING',
  outcome: 'USEFUL',
  notes: 'Ayudó a desbloquear la validación.',
  occurredUtc: '2026-05-16T18:30:00Z',
  createdByUserId: 'user-1',
  createdByUserName: 'Operadora',
  createdUtc: '2026-05-16T18:45:00Z',
  updatedUtc: null,
  archivedUtc: null
};

describe('FederationPageComponent contact interventions', () => {
  it('renders the contact support launcher for the selected federation action', async () => {
    const { fixture } = await renderFederationPage();

    showActionsTab(fixture);
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.directive(ContactInterventionLauncherComponent))).not.toBeNull();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Vincular apoyo de contacto');
  });

  it('does not render the launcher without CONTACTS_READ', async () => {
    const { fixture } = await renderFederationPage({ canReadContacts: false });

    showActionsTab(fixture);
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.directive(ContactInterventionLauncherComponent))).toBeNull();
  });

  it('does not render the launcher without FEDERATION_WRITE', async () => {
    const { fixture } = await renderFederationPage({ canWriteFederation: false });

    showActionsTab(fixture);
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.directive(ContactInterventionLauncherComponent))).toBeNull();
  });

  it('does not render the launcher for a closed action', async () => {
    const { fixture } = await renderFederationPage({
      action: {
        ...actionDetail,
        statusCode: 'CLOSED',
        statusName: 'Cerrada',
        statusIsClosed: true
      }
    });

    showActionsTab(fixture);
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.directive(ContactInterventionLauncherComponent))).toBeNull();
  });

  it('passes the federation action context to the launcher', async () => {
    const { fixture } = await renderFederationPage();

    showActionsTab(fixture);
    fixture.detectChanges();

    const launcher = fixture.debugElement.query(By.directive(ContactInterventionLauncherComponent))
      .componentInstance as ContactInterventionLauncherComponent;
    expect(launcher.moduleKey()).toBe('FEDERATION');
    expect(launcher.originType()).toBe('FEDERATION_ACTION');
    expect(launcher.originId()).toBe(actionDetail.id);
    expect(launcher.originDisplayName()).toBe('Gestión con gobierno · Secretaría de Desarrollo · 2026-05-12');
    expect(launcher.defaultSubject()).toBe('Apoyo en gestión federativa: Gestión con gobierno');
    expect(launcher.buttonLabel()).toBe('Vincular apoyo de contacto');
    expect(launcher.disabled()).toBe(false);
  });

  it('loads action support interventions by origin when Ver apoyos is clicked', async () => {
    const { fixture, sharedCatalogsService } = await renderFederationPage();

    await openActionSupportHistory(fixture);

    expect(sharedCatalogsService.getContactInterventionsByOrigin).toHaveBeenCalledWith({
      moduleKey: 'FEDERATION',
      originType: 'FEDERATION_ACTION',
      originId: actionDetail.id
    });
  });

  it('renders associated action support interventions', async () => {
    const { fixture } = await renderFederationPage();
    const compiled = fixture.nativeElement as HTMLElement;

    await openActionSupportHistory(fixture);

    expect(compiled.textContent).toContain('Apoyos de contacto');
    expect(compiled.textContent).toContain('Enlace Operativo');
    expect(compiled.textContent).toContain('Desbloqueo');
    expect(compiled.textContent).toContain('Útil');
    expect(compiled.textContent).toContain('Ayudó a desbloquear la validación.');
    expect(compiled.textContent).toContain('Operadora');
  });

  it('shows an empty state when the action has no support interventions', async () => {
    const { fixture } = await renderFederationPage({ actionInterventions: [] });
    const compiled = fixture.nativeElement as HTMLElement;

    await openActionSupportHistory(fixture);

    expect(compiled.textContent).toContain('No hay apoyos de contacto registrados para esta gestión.');
  });

  it('refreshes the open support panel after saving an intervention', async () => {
    const { fixture, sharedCatalogsService } = await renderFederationPage();

    await openActionSupportHistory(fixture);
    expect(sharedCatalogsService.getContactInterventionsByOrigin).toHaveBeenCalledTimes(1);

    const launcher = fixture.debugElement.query(By.directive(ContactInterventionLauncherComponent))
      .componentInstance as ContactInterventionLauncherComponent;
    launcher.saved.emit(intervention);
    await fixture.whenStable();
    await flushAsyncTasks();
    fixture.detectChanges();

    expect(sharedCatalogsService.getContactInterventionsByOrigin).toHaveBeenCalledTimes(2);
    expect((fixture.nativeElement as HTMLElement).textContent)
      .toContain('Apoyo de contacto vinculado a la gestión: Gestión con gobierno.');
  });

  it('does not change action participants when saving an intervention', async () => {
    const { fixture, federationService } = await renderFederationPage();

    showActionsTab(fixture);
    fixture.detectChanges();

    const launcher = fixture.debugElement.query(By.directive(ContactInterventionLauncherComponent))
      .componentInstance as ContactInterventionLauncherComponent;
    launcher.saved.emit(intervention);
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Participante Aliada');
    expect(federationService.addActionParticipant).not.toHaveBeenCalled();
  });
});

async function renderFederationPage(options: {
  canReadContacts?: boolean;
  canReadFederation?: boolean;
  canWriteFederation?: boolean;
  action?: FederationActionDetail;
  actionInterventions?: ContactIntervention[];
} = {}) {
  const selectedAction = options.action ?? actionDetail;
  const actionListItem = toActionSummary(selectedAction);
  const alerts: FederationModuleAlerts = {
    actions: [],
    donations: []
  };
  const federationService = {
    getAlerts: vi.fn(() => of(alerts)),
    listActions: vi.fn(() => of([actionListItem])),
    getAction: vi.fn(() => of(selectedAction)),
    createAction: vi.fn(),
    closeAction: vi.fn(),
    getActionParticipants: vi.fn(),
    addActionParticipant: vi.fn(),
    listDonations: vi.fn(() => of([])),
    getDonation: vi.fn(),
    createDonation: vi.fn(),
    closeDonation: vi.fn(),
    getDonationApplications: vi.fn(),
    createDonationApplication: vi.fn(),
    getApplicationEvidences: vi.fn(),
    createApplicationEvidence: vi.fn(),
    downloadEvidence: vi.fn(),
    getApplicationCommissions: vi.fn(),
    createApplicationCommission: vi.fn()
  };
  const sharedCatalogsService = {
    getContacts: vi.fn(() => of([contact])),
    getModuleStatuses: vi.fn((moduleCode?: string, contextCode?: string) => {
      if (contextCode === 'FEDERATION_DONATION') {
        return of([donationStatus]);
      }

      if (contextCode === 'FEDERATION_DONATION_APPLICATION') {
        return of([applicationStatus]);
      }

      return of([actionStatus]);
    }),
    getEvidenceTypes: vi.fn(() => of([evidenceType])),
    getCommissionTypes: vi.fn(() => of([commissionType])),
    getContactInterventionsByOrigin: vi.fn(() => of(options.actionInterventions ?? [intervention])),
    createContactIntervention: vi.fn(() => of(intervention))
  };

  TestBed.resetTestingModule();
  await TestBed.configureTestingModule({
    imports: [FederationPageComponent],
    providers: [
      {
        provide: AuthService,
        useValue: {
          canReadContacts: signal(options.canReadContacts ?? true),
          canReadFederation: signal(options.canReadFederation ?? true),
          canWriteFederation: signal(options.canWriteFederation ?? true),
          canAdministerFormalClose: signal(false)
        }
      },
      {
        provide: FederationService,
        useValue: federationService
      },
      {
        provide: SharedCatalogsService,
        useValue: sharedCatalogsService
      }
    ]
  }).compileComponents();

  const fixture = TestBed.createComponent(FederationPageComponent);
  fixture.detectChanges();
  await fixture.whenStable();
  await flushAsyncTasks();
  fixture.detectChanges();

  const component = fixture.componentInstance as unknown as {
    selectedActionId: { set(value: string | null): void };
    selectedAction: { set(value: FederationActionDetail | null): void };
  };
  component.selectedActionId.set(selectedAction.id);
  component.selectedAction.set(selectedAction);
  fixture.detectChanges();

  return {
    fixture,
    federationService,
    sharedCatalogsService
  };
}

function toActionSummary(action: FederationActionDetail): FederationActionSummary {
  return {
    id: action.id,
    actionTypeCode: action.actionTypeCode,
    actionTypeName: action.actionTypeName,
    counterpartyOrInstitution: action.counterpartyOrInstitution,
    actionDate: action.actionDate,
    objective: action.objective,
    statusCatalogEntryId: action.statusCatalogEntryId,
    statusCode: action.statusCode,
    statusName: action.statusName,
    statusIsClosed: action.statusIsClosed,
    statusAlertsEnabledByDefault: action.statusAlertsEnabledByDefault,
    participantCount: action.participants.length,
    alertState: action.alertState,
    notes: action.notes,
    createdUtc: action.createdUtc,
    updatedUtc: action.updatedUtc
  };
}

function showActionsTab(fixture: ComponentFixture<FederationPageComponent>) {
  (fixture.componentInstance as unknown as { setActiveTab(tab: 'actions'): void }).setActiveTab('actions');
}

async function openActionSupportHistory(fixture: ComponentFixture<FederationPageComponent>) {
  showActionsTab(fixture);
  fixture.detectChanges();

  findButtonByText(fixture.nativeElement as HTMLElement, 'Ver apoyos')?.click();
  fixture.detectChanges();
  await fixture.whenStable();
  await flushAsyncTasks();
  fixture.detectChanges();
}

async function flushAsyncTasks() {
  await new Promise<void>((resolve) => {
    setTimeout(resolve, 0);
  });
}

function findButtonByText(root: HTMLElement, text: string) {
  return Array.from(root.querySelectorAll<HTMLButtonElement>('button'))
    .find((button) => button.textContent?.trim() === text) ?? null;
}
