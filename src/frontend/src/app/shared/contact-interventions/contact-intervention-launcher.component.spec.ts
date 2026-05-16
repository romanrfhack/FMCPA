import '@angular/compiler';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { Contact, ContactIntervention } from '../../core/models/shared-catalogs.models';
import { SharedCatalogsService } from '../../core/services/shared-catalogs.service';
import { ContactInterventionLauncherComponent } from './contact-intervention-launcher.component';

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
  subject: 'Apoyo para gestionar incidencia',
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

describe('ContactInterventionLauncherComponent', () => {
  it('renders the launcher button', async () => {
    const { fixture } = await renderLauncher();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(findButtonByText(compiled, 'Registrar intervención')).not.toBeNull();
  });

  it('opens the modal with preloaded context and default subject', async () => {
    const { fixture } = await renderLauncher();
    const compiled = fixture.nativeElement as HTMLElement;

    await openModal(fixture);

    expect(compiled.textContent).toContain('Registrar intervención de contacto');
    expect(compiled.textContent).toContain('MARKETS');
    expect(compiled.textContent).toContain('MARKET_ISSUE');
    expect(compiled.textContent).toContain('issue-1');
    expect(compiled.textContent).toContain('Incidencia Mercado Juarez');
    expect(queryInput(compiled, 'subject')?.value).toBe('Apoyo para gestionar incidencia');
  });

  it('loads available contacts and filters them locally', async () => {
    const otherContact: Contact = {
      ...contact,
      id: 'contact-2',
      name: 'Ana Lopez',
      organizationOrDependency: 'Area Y',
      email: 'ana@example.com'
    };
    const { fixture, sharedCatalogsService } = await renderLauncher({
      contacts: [contact, otherContact]
    });
    const compiled = fixture.nativeElement as HTMLElement;

    await openModal(fixture);

    expect(sharedCatalogsService.getContacts).toHaveBeenCalled();
    expect(compiled.textContent).toContain('Juan Perez');
    expect(compiled.textContent).toContain('Ana Lopez');

    setInputValue(compiled, 'input[type="search"]', 'ana');
    fixture.detectChanges();

    expect(compiled.textContent).not.toContain('Juan Perez · Area X');
    expect(compiled.textContent).toContain('Ana Lopez');
  });

  it('validates required contact, help type, outcome and subject', async () => {
    const { fixture, sharedCatalogsService } = await renderLauncher();
    const compiled = fixture.nativeElement as HTMLElement;

    await openModal(fixture);
    setInputValue(compiled, '[formControlName="subject"]', '   ');
    findButtonByText(compiled, 'Guardar intervención')?.click();
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Selecciona un contacto existente.');
    expect(compiled.textContent).toContain('Selecciona el tipo de ayuda.');
    expect(compiled.textContent).toContain('Selecciona el resultado.');
    expect(compiled.textContent).toContain('Captura el asunto.');
    expect(sharedCatalogsService.createContactIntervention).not.toHaveBeenCalled();
  });

  it('validates notes length', async () => {
    const { fixture, sharedCatalogsService } = await renderLauncher();
    const compiled = fixture.nativeElement as HTMLElement;

    await openModal(fixture);
    fillValidForm(compiled, { notes: 'x'.repeat(2001) });
    findButtonByText(compiled, 'Guardar intervención')?.click();
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Las notas no deben exceder 2000 caracteres.');
    expect(sharedCatalogsService.createContactIntervention).not.toHaveBeenCalled();
  });

  it('posts the selected contact id and preloaded context', async () => {
    const { fixture, sharedCatalogsService } = await renderLauncher();
    const compiled = fixture.nativeElement as HTMLElement;

    await openModal(fixture);
    fillValidForm(compiled);
    findButtonByText(compiled, 'Guardar intervención')?.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(sharedCatalogsService.createContactIntervention).toHaveBeenCalledWith('contact-1', {
      moduleKey: 'MARKETS',
      originType: 'MARKET_ISSUE',
      originId: 'issue-1',
      originDisplayName: 'Incidencia Mercado Juarez',
      subject: 'Apoyo para gestionar incidencia',
      helpType: 'UNBLOCKING',
      outcome: 'USEFUL',
      notes: 'Ayudo con el area X.',
      occurredUtc: new Date('2026-05-16T12:30').toISOString()
    });
  });

  it('shows an error when the intervention cannot be saved', async () => {
    const { fixture } = await renderLauncher({ failCreate: true });
    const compiled = fixture.nativeElement as HTMLElement;

    await openModal(fixture);
    fillValidForm(compiled);
    findButtonByText(compiled, 'Guardar intervención')?.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(compiled.textContent).toContain('No fue posible registrar la intervención.');
    expect(compiled.textContent).toContain('Registrar intervención de contacto');
  });

  it('emits saved and closes after a successful save', async () => {
    const { fixture } = await renderLauncher();
    const compiled = fixture.nativeElement as HTMLElement;
    const savedSpy = vi.fn();
    const closedSpy = vi.fn();
    fixture.componentInstance.saved.subscribe(savedSpy);
    fixture.componentInstance.closed.subscribe(closedSpy);

    await openModal(fixture);
    fillValidForm(compiled);
    findButtonByText(compiled, 'Guardar intervención')?.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(savedSpy).toHaveBeenCalledWith(intervention);
    expect(closedSpy).toHaveBeenCalled();
    expect(compiled.textContent).not.toContain('Registrar intervención de contacto');
  });

  it('emits cancelled and closes when cancelled', async () => {
    const { fixture } = await renderLauncher();
    const compiled = fixture.nativeElement as HTMLElement;
    const cancelledSpy = vi.fn();
    fixture.componentInstance.cancelled.subscribe(cancelledSpy);

    await openModal(fixture);
    findButtonByText(compiled, 'Cancelar')?.click();
    fixture.detectChanges();

    expect(cancelledSpy).toHaveBeenCalled();
    expect(compiled.textContent).not.toContain('Registrar intervención de contacto');
  });
});

async function renderLauncher(options: {
  contacts?: Contact[];
  failCreate?: boolean;
  createdIntervention?: ContactIntervention;
} = {}) {
  const sharedCatalogsService = {
    getContacts: vi.fn(() => of(options.contacts ?? [contact])),
    createContactIntervention: vi.fn(() =>
      options.failCreate
        ? throwError(() => new Error('Save failed'))
        : of(options.createdIntervention ?? intervention))
  };

  TestBed.resetTestingModule();
  await TestBed.configureTestingModule({
    imports: [ContactInterventionLauncherComponent],
    providers: [
      {
        provide: SharedCatalogsService,
        useValue: sharedCatalogsService
      }
    ]
  }).compileComponents();

  const fixture = TestBed.createComponent(ContactInterventionLauncherComponent);
  fixture.componentRef.setInput('moduleKey', 'MARKETS');
  fixture.componentRef.setInput('originType', 'MARKET_ISSUE');
  fixture.componentRef.setInput('originId', 'issue-1');
  fixture.componentRef.setInput('originDisplayName', 'Incidencia Mercado Juarez');
  fixture.componentRef.setInput('defaultSubject', 'Apoyo para gestionar incidencia');
  fixture.detectChanges();

  return {
    fixture,
    sharedCatalogsService
  };
}

async function openModal(fixture: ComponentFixture<ContactInterventionLauncherComponent>) {
  const compiled = fixture.nativeElement as HTMLElement;
  findButtonByText(compiled, 'Registrar intervención')?.click();
  fixture.detectChanges();
  await fixture.whenStable();
  await Promise.resolve();
  fixture.detectChanges();
}

function fillValidForm(root: HTMLElement, overrides: { notes?: string } = {}) {
  setSelectValue(root, '[formControlName="contactId"]', 'contact-1');
  setSelectValue(root, '[formControlName="helpType"]', 'UNBLOCKING');
  setSelectValue(root, '[formControlName="outcome"]', 'USEFUL');
  setInputValue(root, '[formControlName="occurredUtc"]', '2026-05-16T12:30');
  setInputValue(root, '[formControlName="notes"]', overrides.notes ?? 'Ayudo con el area X.');
}

function queryInput(root: HTMLElement, controlName: string) {
  return root.querySelector<HTMLInputElement>(`[formControlName="${controlName}"]`);
}

function setInputValue(root: HTMLElement, selector: string, value: string) {
  const input = root.querySelector<HTMLInputElement | HTMLTextAreaElement>(selector);
  expect(input, `Could not find input ${selector}`).not.toBeNull();
  input!.value = value;
  input!.dispatchEvent(new Event('input', { bubbles: true }));
}

function setSelectValue(root: HTMLElement, selector: string, value: string) {
  const select = root.querySelector<HTMLSelectElement>(selector);
  expect(select, `Could not find select ${selector}`).not.toBeNull();
  select!.value = value;
  select!.dispatchEvent(new Event('change', { bubbles: true }));
}

function findButtonByText(root: HTMLElement, text: string) {
  return Array.from(root.querySelectorAll<HTMLButtonElement>('button'))
    .find((button) => button.textContent?.trim() === text) ?? null;
}
