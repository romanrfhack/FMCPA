import '@angular/compiler';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import {
  Contact,
  ContactIntervention,
  ContactType
} from '../../core/models/shared-catalogs.models';
import { AuthService } from '../../core/services/auth.service';
import { SharedCatalogsService } from '../../core/services/shared-catalogs.service';
import { ContactsPageComponent } from './contacts-page.component';

const contactTypes: ContactType[] = [
  {
    id: 1,
    code: 'EXTERNAL',
    name: 'Externo',
    description: 'Contacto externo',
    sortOrder: 1
  }
];

const contact: Contact = {
  id: 'contact-1',
  name: 'Juan Perez',
  contactTypeId: 1,
  contactTypeCode: 'EXTERNAL',
  contactTypeName: 'Externo',
  organizationOrDependency: 'Area X',
  roleTitle: 'Enlace',
  mobilePhone: '5512345678',
  whatsAppPhone: '5512345678',
  email: 'juan@example.com',
  notes: 'Contacto operativo',
  createdUtc: '2026-05-01T12:00:00Z',
  updatedUtc: null
};

const intervention: ContactIntervention = {
  id: 'intervention-1',
  contactId: contact.id,
  contactName: contact.name,
  contactTypeName: contact.contactTypeName,
  moduleKey: 'MARKETS',
  originType: 'MARKET_ISSUE',
  originId: 'issue-1',
  originDisplayName: 'Incidencia Mercado Juarez',
  subject: 'Desbloqueo de seguimiento',
  helpType: 'UNBLOCKING',
  outcome: 'USEFUL',
  notes: 'Ayudo con el area X.',
  occurredUtc: '2026-05-16T18:30:00Z',
  createdByUserId: 'user-1',
  createdByUserName: 'Operadora Interna',
  createdUtc: '2026-05-16T18:45:00Z',
  updatedUtc: null,
  archivedUtc: null
};

describe('ContactsPageComponent', () => {
  it('renders a history action for each contact and loads empty history by contact id', async () => {
    const { fixture, sharedCatalogsService } = await renderContactsPage([]);
    const compiled = fixture.nativeElement as HTMLElement;

    clickHistoryButton(compiled);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(sharedCatalogsService.getContactInterventions).toHaveBeenCalledWith('contact-1', { limit: 25 });
    expect(compiled.textContent).toContain('Intervenciones de contacto');
    expect(compiled.textContent).toContain('No hay intervenciones registradas para este contacto.');
  });

  it('renders intervention details with readable labels', async () => {
    const { fixture } = await renderContactsPage([intervention]);
    const compiled = fixture.nativeElement as HTMLElement;

    clickHistoryButton(compiled);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Desbloqueo de seguimiento');
    expect(compiled.textContent).toContain('Mercados');
    expect(compiled.textContent).toContain('Desbloqueo');
    expect(compiled.textContent).toContain('Útil');
    expect(compiled.textContent).toContain('Incidencia Mercado Juarez');
    expect(compiled.textContent).toContain('Operadora Interna');
    expect(compiled.textContent).toContain('Ayudo con el area X.');
  });

  it('shows an error state when history loading fails', async () => {
    const { fixture } = await renderContactsPage([], true);
    const compiled = fixture.nativeElement as HTMLElement;

    clickHistoryButton(compiled);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(compiled.textContent).toContain('No fue posible cargar el historial de intervenciones.');
  });

  it('closes the history panel', async () => {
    const { fixture } = await renderContactsPage([intervention]);
    const compiled = fixture.nativeElement as HTMLElement;

    clickHistoryButton(compiled);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Intervenciones de contacto');

    findButtonByText(compiled, 'Cerrar')?.click();
    fixture.detectChanges();

    expect(compiled.textContent).not.toContain('Intervenciones de contacto');
  });

  it('keeps contact creation working', async () => {
    const createdContact: Contact = {
      ...contact,
      id: 'contact-2',
      name: 'Ana Lopez',
      email: 'ana@example.com'
    };
    const { fixture, sharedCatalogsService } = await renderContactsPage([], false, createdContact);
    const component = fixture.componentInstance as unknown as {
      form: {
        setValue(value: {
          name: string;
          contactTypeId: number;
          organizationOrDependency: string;
          roleTitle: string;
          mobilePhone: string;
          whatsAppPhone: string;
          email: string;
          notes: string;
        }): void;
      };
      submit(): Promise<void>;
    };

    component.form.setValue({
      name: 'Ana Lopez',
      contactTypeId: 1,
      organizationOrDependency: 'Area Y',
      roleTitle: 'Gestora',
      mobilePhone: '5598765432',
      whatsAppPhone: '5598765432',
      email: 'ana@example.com',
      notes: 'Nueva nota'
    });

    await component.submit();
    fixture.detectChanges();

    expect(sharedCatalogsService.createContact).toHaveBeenCalledWith({
      name: 'Ana Lopez',
      contactTypeId: 1,
      organizationOrDependency: 'Area Y',
      roleTitle: 'Gestora',
      mobilePhone: '5598765432',
      whatsAppPhone: '5598765432',
      email: 'ana@example.com',
      notes: 'Nueva nota'
    });
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Ana Lopez');
  });
});

async function renderContactsPage(
  interventions: ContactIntervention[] = [],
  failHistory = false,
  createdContact: Contact = contact
) {
  const sharedCatalogsService = {
    getContactTypes: vi.fn(() => of(contactTypes)),
    getContacts: vi.fn(() => of([contact])),
    createContact: vi.fn(() => of(createdContact)),
    getContactInterventions: vi.fn(() =>
      failHistory
        ? throwError(() => new Error('History failed'))
        : of(interventions))
  };

  TestBed.resetTestingModule();
  await TestBed.configureTestingModule({
    imports: [ContactsPageComponent],
    providers: [
      {
        provide: SharedCatalogsService,
        useValue: sharedCatalogsService
      },
      {
        provide: AuthService,
        useValue: {
          canWriteContacts: signal(true)
        }
      }
    ]
  }).compileComponents();

  const fixture = TestBed.createComponent(ContactsPageComponent);
  fixture.detectChanges();
  await fixture.whenStable();
  await Promise.resolve();
  await Promise.resolve();
  fixture.detectChanges();

  return {
    fixture,
    sharedCatalogsService
  };
}

function findButtonByText(root: HTMLElement, text: string) {
  return Array.from(root.querySelectorAll<HTMLButtonElement>('button'))
    .find((button) => button.textContent?.trim() === text) ?? null;
}

function clickHistoryButton(root: HTMLElement) {
  const buttons = Array.from(root.querySelectorAll<HTMLButtonElement>('button'));
  const button = buttons
    .find((item) => item.textContent?.includes('Historial')) ?? null;

  expect(
    button,
    `Available buttons: ${buttons.map((item) => item.textContent?.trim()).join(' | ')}. Historial context: ${root.innerHTML.match(/.{0,120}Historial.{0,120}/)?.[0] ?? 'missing'}`
  ).not.toBeNull();
  button!.dispatchEvent(new MouseEvent('click', { bubbles: true }));
}
