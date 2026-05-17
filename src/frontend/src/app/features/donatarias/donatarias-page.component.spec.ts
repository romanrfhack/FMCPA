import '@angular/compiler';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import {
  DonationAlert,
  DonationApplication,
  DonationDetail,
  DonationDocumentaryStatus,
  DonationSummary,
  DonationTransparencyReport
} from '../../core/models/donations.models';
import { CatalogItem, Contact, ContactIntervention, ModuleStatusCatalogEntry } from '../../core/models/shared-catalogs.models';
import { AuthService } from '../../core/services/auth.service';
import { DonationsService } from '../../core/services/donations.service';
import { SharedCatalogsService } from '../../core/services/shared-catalogs.service';
import { ContactInterventionLauncherComponent } from '../../shared/contact-interventions/contact-intervention-launcher.component';
import { DonatariasPageComponent } from './donatarias-page.component';

const contact: Contact = {
  id: 'contact-1',
  name: 'Enlace Operativo',
  contactTypeId: 1,
  contactTypeCode: 'EXTERNAL',
  contactTypeName: 'Externo',
  organizationOrDependency: 'Fundacion aliada',
  roleTitle: 'Coordinadora',
  mobilePhone: '5512345678',
  whatsAppPhone: '5512345678',
  email: 'enlace@example.com',
  notes: null,
  createdUtc: '2026-05-01T12:00:00Z',
  updatedUtc: null
};

const application: DonationApplication = {
  id: 'application-1',
  donationId: 'donation-1',
  beneficiaryName: 'Comedor Central',
  responsibleContactId: 'responsible-contact-1',
  responsibleName: 'Responsable A',
  applicationDate: '2026-05-12',
  appliedAmount: 25000,
  statusCatalogEntryId: 3201,
  statusCode: 'PARTIALLY_APPLIED',
  statusName: 'Aplicacion parcial',
  statusIsClosed: false,
  verificationDetails: 'Validacion inicial.',
  closingDetails: null,
  evidenceCount: 0,
  evidences: [],
  createdUtc: '2026-05-12T12:00:00Z'
};

const donationSummary: DonationSummary = {
  id: 'donation-1',
  donorEntityName: 'Fundacion Norte',
  donationDate: '2026-05-01',
  donationType: 'Efectivo',
  baseAmount: 100000,
  reference: 'DON-2026-01',
  notes: null,
  statusCatalogEntryId: 3101,
  statusCode: 'PARTIALLY_APPLIED',
  statusName: 'Aplicacion parcial',
  statusIsClosed: false,
  statusAlertsEnabledByDefault: true,
  appliedAmountTotal: 25000,
  remainingAmount: 75000,
  appliedPercentage: 25,
  applicationCount: 1,
  evidenceCount: 0,
  alertState: 'PARTIALLY_APPLIED',
  createdUtc: '2026-05-01T12:00:00Z',
  updatedUtc: null
};

const donationDetail: DonationDetail = {
  ...donationSummary,
  applications: [application]
};

const donationStatus: ModuleStatusCatalogEntry = {
  id: 3101,
  moduleCode: 'DONATARIAS',
  moduleName: 'Donatarias',
  contextCode: 'DONATION',
  contextName: 'Donacion',
  statusCode: 'PARTIALLY_APPLIED',
  statusName: 'Aplicacion parcial',
  description: null,
  sortOrder: 1,
  isClosed: false,
  alertsEnabledByDefault: true,
  isActive: true
};

const applicationStatus: ModuleStatusCatalogEntry = {
  id: 3201,
  moduleCode: 'DONATARIAS',
  moduleName: 'Donatarias',
  contextCode: 'DONATION_APPLICATION',
  contextName: 'Aplicacion',
  statusCode: 'PARTIALLY_APPLIED',
  statusName: 'Aplicacion parcial',
  description: null,
  sortOrder: 1,
  isClosed: false,
  alertsEnabledByDefault: true,
  isActive: true
};

const evidenceType: CatalogItem = {
  id: 1,
  code: 'PHOTO',
  name: 'Fotografia',
  description: null,
  sortOrder: 1,
  isActive: true
};

const documentaryStatus: DonationDocumentaryStatus = {
  donationId: donationDetail.id,
  totalApplications: 1,
  applicationsWithEvidence: 0,
  applicationsMissingEvidence: 1,
  documentaryStatusCode: 'EVIDENCE_PENDING',
  documentaryStatusLabel: 'Evidencia pendiente',
  isMinimumEvidenceComplete: false,
  applicationStatuses: [
    {
      applicationId: application.id,
      beneficiaryName: application.beneficiaryName,
      applicationDate: application.applicationDate,
      appliedAmount: application.appliedAmount,
      evidenceCount: 0,
      activeDocumentCount: 0,
      requirementStatus: 'EVIDENCE_PENDING',
      missingReasonCode: 'MISSING_EVIDENCE',
      requiredDocumentClassCodes: ['EVIDENCE'],
      routeHint: null
    }
  ]
};

const transparencyReport: DonationTransparencyReport = {
  donationId: donationDetail.id,
  donorEntityName: donationDetail.donorEntityName,
  donationDate: donationDetail.donationDate,
  donationType: donationDetail.donationType,
  reference: donationDetail.reference,
  notes: null,
  reportGeneratedUtc: '2026-05-16T12:00:00Z',
  financialSummary: {
    baseAmount: donationDetail.baseAmount,
    appliedAmountTotal: donationDetail.appliedAmountTotal,
    remainingAmount: donationDetail.remainingAmount,
    appliedPercentage: donationDetail.appliedPercentage,
    applicationCount: donationDetail.applications.length
  },
  operationalStatus: {
    donationStatusCode: donationDetail.statusCode,
    donationStatusName: donationDetail.statusName,
    statusIsClosed: donationDetail.statusIsClosed,
    financialStatusLabel: 'Aplicacion parcial',
    operationalStatusLabel: 'En seguimiento'
  },
  documentarySummary: {
    documentaryStatusCode: documentaryStatus.documentaryStatusCode,
    documentaryStatusLabel: documentaryStatus.documentaryStatusLabel,
    totalApplications: documentaryStatus.totalApplications,
    applicationsWithEvidence: documentaryStatus.applicationsWithEvidence,
    applicationsMissingEvidence: documentaryStatus.applicationsMissingEvidence,
    isMinimumEvidenceComplete: documentaryStatus.isMinimumEvidenceComplete
  },
  applications: [],
  presentationReadiness: {
    readinessCode: 'PARTIAL',
    readinessLabel: 'Parcial',
    reasons: ['Evidencia pendiente']
  },
  scopeNotes: []
};

const intervention: ContactIntervention = {
  id: 'intervention-1',
  contactId: contact.id,
  contactName: contact.name,
  contactTypeName: contact.contactTypeName,
  moduleKey: 'DONATARIAS',
  originType: 'DONATION_APPLICATION',
  originId: application.id,
  originDisplayName: 'Fundacion Norte · DON-2026-01 · Comedor Central · 2026-05-12',
  subject: 'Apoyo en aplicación de donación: Comedor Central',
  helpType: 'UNBLOCKING',
  outcome: 'USEFUL',
  notes: 'Ayudo a validar la aplicación.',
  occurredUtc: '2026-05-16T18:30:00Z',
  createdByUserId: 'user-1',
  createdByUserName: 'Operadora',
  createdUtc: '2026-05-16T18:45:00Z',
  updatedUtc: null,
  archivedUtc: null
};

describe('DonatariasPageComponent contact interventions', () => {
  it('renders the contact support launcher for the selected donation application with the expected context', async () => {
    const { fixture } = await renderDonatariasPage();

    showApplicationsTab(fixture);
    fixture.detectChanges();

    const launcherDebug = fixture.debugElement.query(By.directive(ContactInterventionLauncherComponent));
    expect(launcherDebug).not.toBeNull();

    const launcher = launcherDebug.componentInstance as ContactInterventionLauncherComponent;
    expect(launcher.moduleKey()).toBe('DONATARIAS');
    expect(launcher.originType()).toBe('DONATION_APPLICATION');
    expect(launcher.originId()).toBe(application.id);
    expect(launcher.originDisplayName()).toBe('Fundacion Norte · DON-2026-01 · Comedor Central · 2026-05-12');
    expect(launcher.defaultSubject()).toBe('Apoyo en aplicación de donación: Comedor Central');
    expect(launcher.buttonLabel()).toBe('Vincular apoyo de contacto');
    expect(launcher.disabled()).toBe(false);
  });

  it('does not render the launcher without CONTACTS_READ', async () => {
    const { fixture } = await renderDonatariasPage({ canReadContacts: false });

    showApplicationsTab(fixture);
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.directive(ContactInterventionLauncherComponent))).toBeNull();
  });

  it('does not render the launcher without DONATIONS_WRITE', async () => {
    const { fixture } = await renderDonatariasPage({ canWriteDonations: false });

    showApplicationsTab(fixture);
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.directive(ContactInterventionLauncherComponent))).toBeNull();
  });

  it('does not render the launcher for a terminal application', async () => {
    const closedApplication: DonationApplication = {
      ...application,
      statusCode: 'CLOSED',
      statusName: 'Cerrada',
      statusIsClosed: true
    };
    const { fixture } = await renderDonatariasPage({
      donation: {
        ...donationDetail,
        applications: [closedApplication]
      }
    });

    showApplicationsTab(fixture);
    fixture.detectChanges();

    expect(fixture.debugElement.query(By.directive(ContactInterventionLauncherComponent))).toBeNull();
  });

  it('loads application support interventions by origin when Ver apoyos is clicked', async () => {
    const { fixture, sharedCatalogsService } = await renderDonatariasPage();

    await openApplicationSupportHistory(fixture);

    expect(sharedCatalogsService.getContactInterventionsByOrigin).toHaveBeenCalledWith({
      moduleKey: 'DONATARIAS',
      originType: 'DONATION_APPLICATION',
      originId: application.id
    });
  });

  it('renders associated application support interventions', async () => {
    const { fixture } = await renderDonatariasPage();
    const compiled = fixture.nativeElement as HTMLElement;

    await openApplicationSupportHistory(fixture);

    expect(compiled.textContent).toContain('Apoyos de contacto');
    expect(compiled.textContent).toContain('Enlace Operativo');
    expect(compiled.textContent).toContain('Desbloqueo');
    expect(compiled.textContent).toContain('Útil');
    expect(compiled.textContent).toContain('Ayudo a validar la aplicación.');
    expect(compiled.textContent).toContain('Operadora');
  });

  it('shows an empty state when the application has no support interventions', async () => {
    const { fixture } = await renderDonatariasPage({ applicationInterventions: [] });
    const compiled = fixture.nativeElement as HTMLElement;

    await openApplicationSupportHistory(fixture);

    expect(compiled.textContent).toContain('No hay apoyos de contacto registrados para esta aplicación.');
  });

  it('refreshes the open support panel after saving an intervention', async () => {
    const { fixture, sharedCatalogsService } = await renderDonatariasPage();

    await openApplicationSupportHistory(fixture);
    expect(sharedCatalogsService.getContactInterventionsByOrigin).toHaveBeenCalledTimes(1);

    const launcher = fixture.debugElement.query(By.directive(ContactInterventionLauncherComponent))
      .componentInstance as ContactInterventionLauncherComponent;
    launcher.saved.emit(intervention);
    await fixture.whenStable();
    await flushAsyncTasks();
    fixture.detectChanges();

    expect(sharedCatalogsService.getContactInterventionsByOrigin).toHaveBeenCalledTimes(2);
    expect((fixture.nativeElement as HTMLElement).textContent)
      .toContain('Apoyo de contacto vinculado a la aplicación: Comedor Central.');
  });

  it('does not change the application responsible when saving an intervention', async () => {
    const { fixture, donationsService } = await renderDonatariasPage();

    showApplicationsTab(fixture);
    fixture.detectChanges();

    const launcher = fixture.debugElement.query(By.directive(ContactInterventionLauncherComponent))
      .componentInstance as ContactInterventionLauncherComponent;
    launcher.saved.emit(intervention);
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Responsable A');
    expect(donationsService.createDonationApplication).not.toHaveBeenCalled();
  });
});

async function renderDonatariasPage(options: {
  canWriteDonations?: boolean;
  canReadDonations?: boolean;
  canReadContacts?: boolean;
  donation?: DonationDetail;
  applicationInterventions?: ContactIntervention[];
} = {}) {
  const selectedDonation = options.donation ?? donationDetail;
  const donationAlerts: DonationAlert[] = [];
  const donationsService = {
    listDonations: vi.fn(() => of([donationSummary])),
    getDonation: vi.fn(() => of(selectedDonation)),
    createDonation: vi.fn(),
    closeDonation: vi.fn(),
    getDonationProgress: vi.fn(),
    getDonationDocumentaryStatus: vi.fn(() => of(documentaryStatus)),
    getDonationTransparencyReport: vi.fn(() => of(transparencyReport)),
    getDonationApplications: vi.fn(),
    createDonationApplication: vi.fn(),
    getDonationAlerts: vi.fn(() => of(donationAlerts)),
    getApplicationEvidences: vi.fn(),
    createApplicationEvidence: vi.fn(),
    downloadEvidence: vi.fn()
  };
  const sharedCatalogsService = {
    getContacts: vi.fn(() => of([contact])),
    getModuleStatuses: vi.fn((moduleCode?: string, contextCode?: string) =>
      of(contextCode === 'DONATION_APPLICATION' ? [applicationStatus] : [donationStatus])),
    getEvidenceTypes: vi.fn(() => of([evidenceType])),
    getContactInterventionsByOrigin: vi.fn(() => of(options.applicationInterventions ?? [intervention])),
    createContactIntervention: vi.fn(() => of(intervention))
  };

  TestBed.resetTestingModule();
  await TestBed.configureTestingModule({
    imports: [DonatariasPageComponent],
    providers: [
      {
        provide: ActivatedRoute,
        useValue: {
          snapshot: {
            queryParamMap: {
              get: vi.fn(() => null)
            }
          }
        }
      },
      {
        provide: AuthService,
        useValue: {
          canReadDonations: signal(options.canReadDonations ?? true),
          canWriteDonations: signal(options.canWriteDonations ?? true),
          canReadContacts: signal(options.canReadContacts ?? true),
          canAdministerFormalClose: signal(false)
        }
      },
      {
        provide: DonationsService,
        useValue: donationsService
      },
      {
        provide: SharedCatalogsService,
        useValue: sharedCatalogsService
      }
    ]
  }).compileComponents();

  const fixture = TestBed.createComponent(DonatariasPageComponent);
  fixture.detectChanges();
  await fixture.whenStable();
  await flushAsyncTasks();
  fixture.detectChanges();

  const component = fixture.componentInstance as unknown as {
    selectedDonationId: { set(value: string | null): void };
    selectedDonation: { set(value: DonationDetail | null): void };
    selectedApplicationId: { set(value: string | null): void };
  };
  component.selectedDonationId.set(selectedDonation.id);
  component.selectedDonation.set(selectedDonation);
  component.selectedApplicationId.set(selectedDonation.applications[0]?.id ?? null);
  fixture.detectChanges();

  return {
    fixture,
    donationsService,
    sharedCatalogsService
  };
}

function showApplicationsTab(fixture: ComponentFixture<DonatariasPageComponent>) {
  (fixture.componentInstance as unknown as { setActiveTab(tab: 'applications'): void }).setActiveTab('applications');
}

async function openApplicationSupportHistory(fixture: ComponentFixture<DonatariasPageComponent>) {
  showApplicationsTab(fixture);
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
