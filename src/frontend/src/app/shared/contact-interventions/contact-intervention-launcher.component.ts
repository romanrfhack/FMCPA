import { ChangeDetectionStrategy, Component, HostListener, computed, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import {
  Contact,
  ContactIntervention,
  ContactInterventionHelpType,
  ContactInterventionOutcome,
  CreateContactInterventionRequest
} from '../../core/models/shared-catalogs.models';
import { SharedCatalogsService } from '../../core/services/shared-catalogs.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  selector: 'app-contact-intervention-launcher',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  template: `
    <button type="button" class="intervention-launcher" (click)="open()" [disabled]="disabled()">
      {{ buttonLabel() }}
    </button>

    @if (isOpen()) {
      <div class="modal" (click)="cancel()">
        <article
          class="modal-panel"
          role="dialog"
          aria-modal="true"
          aria-labelledby="contact-intervention-title"
          (click)="$event.stopPropagation()">
          <div class="panel-header">
            <div>
              <p class="panel-kicker">Contactos</p>
              <h3 id="contact-intervention-title">Registrar intervención de contacto</h3>
            </div>
            <button type="button" class="ghost compact" (click)="cancel()" [disabled]="isSubmitting()">
              Cerrar
            </button>
          </div>

          <dl class="context-grid" aria-label="Contexto precargado">
            <div>
              <dt>Módulo</dt>
              <dd>{{ moduleKey() }}</dd>
            </div>
            <div>
              <dt>Origen</dt>
              <dd>{{ originDisplayName() }}</dd>
            </div>
            <div>
              <dt>Tipo</dt>
              <dd>{{ originType() }}</dd>
            </div>
            <div>
              <dt>ID origen</dt>
              <dd>{{ originId() }}</dd>
            </div>
          </dl>

          @if (loadError()) {
            <p class="alert error">{{ loadError() }}</p>
          }

          @if (submitError()) {
            <p class="alert error">{{ submitError() }}</p>
          }

          <form class="form-grid" [formGroup]="form" (ngSubmit)="submit()">
            <label class="full-width">
              <span>Buscar contacto</span>
              <input
                type="search"
                [value]="contactSearch()"
                placeholder="Filtra por nombre, organización, cargo o correo"
                (input)="updateContactSearch($event)"
                [disabled]="isContactsLoading() || isSubmitting()" />
            </label>

            <label class="full-width">
              <span>Contacto</span>
              <select formControlName="contactId" [disabled]="isContactsLoading() || isSubmitting() || contacts().length === 0">
                <option value="">{{ contactPlaceholder() }}</option>
                @for (contact of filteredContacts(); track contact.id) {
                  <option [value]="contact.id">{{ getContactOptionLabel(contact) }}</option>
                }
              </select>
            </label>
            @if (form.controls.contactId.touched && form.controls.contactId.hasError('required')) {
              <p class="field-error full-width">Selecciona un contacto existente.</p>
            }
            <p class="hint full-width">Si el contacto no existe, créalo primero en Contactos.</p>
            @if (!isContactsLoading() && contacts().length > 0 && filteredContacts().length === 0) {
              <p class="field-error full-width">No hay contactos que coincidan con la búsqueda.</p>
            }

            <label>
              <span>Tipo de ayuda</span>
              <select formControlName="helpType" [disabled]="isSubmitting()">
                <option value="">Selecciona un tipo</option>
                @for (option of helpTypeOptions; track option.value) {
                  <option [value]="option.value">{{ option.label }}</option>
                }
              </select>
            </label>
            @if (form.controls.helpType.touched && form.controls.helpType.hasError('required')) {
              <p class="field-error">Selecciona el tipo de ayuda.</p>
            }

            <label>
              <span>Resultado</span>
              <select formControlName="outcome" [disabled]="isSubmitting()">
                <option value="">Selecciona un resultado</option>
                @for (option of outcomeOptions; track option.value) {
                  <option [value]="option.value">{{ option.label }}</option>
                }
              </select>
            </label>
            @if (form.controls.outcome.touched && form.controls.outcome.hasError('required')) {
              <p class="field-error">Selecciona el resultado.</p>
            }

            <label>
              <span>Fecha de intervención</span>
              <input type="datetime-local" formControlName="occurredUtc" [disabled]="isSubmitting()" />
            </label>
            @if (form.controls.occurredUtc.touched && form.controls.occurredUtc.hasError('required')) {
              <p class="field-error">La fecha de intervención es obligatoria.</p>
            }

            <label>
              <span>Asunto</span>
              <input type="text" formControlName="subject" placeholder="Asunto de la intervención" [disabled]="isSubmitting()" />
            </label>
            @if (form.controls.subject.touched && (form.controls.subject.hasError('required') || form.controls.subject.hasError('pattern'))) {
              <p class="field-error">Captura el asunto.</p>
            } @else if (form.controls.subject.touched && form.controls.subject.hasError('maxlength')) {
              <p class="field-error">El asunto no debe exceder 250 caracteres.</p>
            }

            <label class="full-width">
              <span>Observaciones / notas</span>
              <textarea
                formControlName="notes"
                rows="5"
                maxlength="2000"
                placeholder="Describe cómo ayudó, facilitó, validó o desbloqueó la gestión"
                [disabled]="isSubmitting()"></textarea>
            </label>
            <div class="notes-meta full-width">
              <span>{{ notesLength() }}/2000</span>
              @if (form.controls.notes.touched && form.controls.notes.hasError('maxlength')) {
                <span class="field-error">Las notas no deben exceder 2000 caracteres.</span>
              }
            </div>

            <div class="form-actions full-width">
              <button type="submit" [disabled]="isSubmitting() || isContactsLoading()">
                @if (isSubmitting()) {
                  Guardando...
                } @else {
                  Guardar intervención
                }
              </button>
              <button type="button" class="ghost" (click)="cancel()" [disabled]="isSubmitting()">Cancelar</button>
            </div>
          </form>
        </article>
      </div>
    }
  `,
  styles: [
    `
      .intervention-launcher,
      button {
        border: none;
        border-radius: 0.9rem;
        padding: 0.8rem 1rem;
        font: inherit;
        font-weight: 700;
        cursor: pointer;
        background: #123f3b;
        color: #f6f6f2;
      }

      button.ghost {
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
      }

      button.compact {
        padding: 0.62rem 0.78rem;
        border-radius: 0.7rem;
        font-size: 0.9rem;
      }

      button:disabled {
        cursor: wait;
        opacity: 0.75;
      }

      .modal {
        position: fixed;
        inset: 0;
        z-index: 40;
        display: grid;
        place-items: center;
        padding: 1rem;
        background: rgba(18, 31, 29, 0.34);
      }

      .modal-panel {
        width: min(44rem, 100%);
        max-height: min(92vh, 54rem);
        overflow-y: auto;
        display: grid;
        gap: 1rem;
        padding: 1.25rem;
        border: 1px solid rgba(29, 45, 42, 0.08);
        border-radius: 1rem;
        background: #fbfbf8;
        box-shadow: 0 18px 42px rgba(18, 31, 29, 0.18);
      }

      .panel-header {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        align-items: flex-start;
      }

      .panel-kicker {
        margin: 0 0 0.5rem;
        letter-spacing: 0.12em;
        text-transform: uppercase;
        font-size: 0.78rem;
        font-weight: 700;
        color: #0f766e;
      }

      h3,
      p,
      dd {
        margin: 0;
      }

      .context-grid {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 0.75rem;
        margin: 0;
        padding: 0.9rem;
        border-radius: 0.8rem;
        border: 1px solid rgba(29, 45, 42, 0.08);
        background: #f1f4ee;
      }

      dt {
        font-size: 0.76rem;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        color: #5b6b68;
      }

      dd {
        margin-top: 0.28rem;
        color: #223734;
        overflow-wrap: anywhere;
      }

      .alert {
        padding: 0.85rem 0.95rem;
        border-radius: 0.9rem;
        font-weight: 600;
      }

      .alert.error {
        background: rgba(180, 35, 24, 0.08);
        color: #b42318;
      }

      .form-grid {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 0.8rem;
      }

      label {
        display: grid;
        gap: 0.4rem;
        font-size: 0.92rem;
        font-weight: 600;
        color: #29403b;
      }

      input,
      select,
      textarea {
        width: 100%;
        box-sizing: border-box;
        padding: 0.78rem 0.9rem;
        border-radius: 0.85rem;
        border: 1px solid rgba(29, 45, 42, 0.14);
        background: #ffffff;
        color: #1d2d2a;
        font: inherit;
        min-width: 0;
      }

      textarea {
        resize: vertical;
      }

      .full-width {
        grid-column: 1 / -1;
      }

      .hint,
      .field-error,
      .notes-meta {
        font-size: 0.86rem;
        line-height: 1.45;
      }

      .hint {
        margin-top: -0.35rem;
        color: #4d615c;
      }

      .field-error {
        margin-top: -0.35rem;
        color: #b42318;
        font-weight: 700;
      }

      .notes-meta {
        display: flex;
        justify-content: space-between;
        gap: 0.75rem;
        margin-top: -0.4rem;
        color: #5b6b68;
      }

      .form-actions {
        display: flex;
        gap: 0.75rem;
        justify-content: flex-end;
      }

      @media (max-width: 700px) {
        .modal {
          place-items: stretch;
          padding: 0.75rem;
        }

        .modal-panel {
          max-height: calc(100vh - 1.5rem);
        }

        .panel-header,
        .form-actions,
        .notes-meta {
          flex-direction: column;
        }

        .context-grid,
        .form-grid {
          grid-template-columns: 1fr;
        }

        .form-actions button {
          width: 100%;
        }
      }
    `
  ]
})
export class ContactInterventionLauncherComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly sharedCatalogsService = inject(SharedCatalogsService);

  readonly moduleKey = input.required<string>();
  readonly originType = input.required<string>();
  readonly originId = input.required<string>();
  readonly originDisplayName = input.required<string>();
  readonly defaultSubject = input.required<string>();
  readonly occurredUtc = input<string | null>(null);
  readonly buttonLabel = input('Registrar intervención');
  readonly disabled = input(false);

  readonly saved = output<ContactIntervention>();
  readonly cancelled = output<void>();
  readonly closed = output<void>();

  protected readonly helpTypeOptions: ReadonlyArray<{ value: ContactInterventionHelpType; label: string }> = [
    { value: 'INFORMATION', label: 'Información' },
    { value: 'FACILITATION', label: 'Facilitación' },
    { value: 'VALIDATION', label: 'Validación' },
    { value: 'ESCALATION', label: 'Escalamiento' },
    { value: 'FOLLOW_UP', label: 'Seguimiento' },
    { value: 'UNBLOCKING', label: 'Desbloqueo' },
    { value: 'OTHER', label: 'Otro' }
  ];

  protected readonly outcomeOptions: ReadonlyArray<{ value: ContactInterventionOutcome; label: string }> = [
    { value: 'USEFUL', label: 'Útil' },
    { value: 'SUCCESSFUL', label: 'Exitoso' },
    { value: 'PENDING', label: 'Pendiente' },
    { value: 'NO_RESPONSE', label: 'Sin respuesta' },
    { value: 'NOT_APPLICABLE', label: 'No aplica' },
    { value: 'OTHER', label: 'Otro' }
  ];

  protected readonly isOpen = signal(false);
  protected readonly contacts = signal<Contact[]>([]);
  protected readonly contactSearch = signal('');
  protected readonly isContactsLoading = signal(false);
  protected readonly isSubmitting = signal(false);
  protected readonly loadError = signal<string | null>(null);
  protected readonly submitError = signal<string | null>(null);

  protected readonly filteredContacts = computed(() => {
    const search = this.normalizeSearch(this.contactSearch());

    if (!search) {
      return this.contacts();
    }

    return this.contacts().filter((contact) =>
      this.normalizeSearch([
        contact.name,
        contact.organizationOrDependency,
        contact.roleTitle,
        contact.email
      ].filter(Boolean).join(' ')).includes(search));
  });

  protected readonly form = this.formBuilder.nonNullable.group({
    contactId: ['', Validators.required],
    helpType: ['', Validators.required],
    outcome: ['', Validators.required],
    occurredUtc: ['', Validators.required],
    subject: ['', [Validators.required, Validators.pattern(/\S/), Validators.maxLength(250)]],
    notes: ['', Validators.maxLength(2000)]
  });

  @HostListener('document:keydown.escape')
  protected closeOnEscape() {
    if (this.isOpen()) {
      this.cancel();
    }
  }

  protected async open() {
    if (this.disabled()) {
      return;
    }

    this.resetFormForContext();
    this.isOpen.set(true);
    await this.loadContacts();
  }

  protected cancel() {
    if (this.isSubmitting()) {
      return;
    }

    this.cancelled.emit();
    this.closeModal();
  }

  protected updateContactSearch(event: Event) {
    this.contactSearch.set((event.target as HTMLInputElement).value);
  }

  protected getContactOptionLabel(contact: Contact) {
    return [
      contact.name,
      contact.organizationOrDependency,
      contact.roleTitle
    ].filter(Boolean).join(' · ');
  }

  protected notesLength() {
    return this.form.controls.notes.getRawValue().length;
  }

  protected contactPlaceholder() {
    return this.isContactsLoading() ? 'Cargando contactos...' : 'Selecciona un contacto';
  }

  protected async submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.submitError.set('Completa los campos obligatorios de la intervención.');
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);

    try {
      const request: CreateContactInterventionRequest = {
        moduleKey: this.moduleKey().trim(),
        originType: this.originType().trim(),
        originId: this.originId().trim(),
        originDisplayName: this.originDisplayName().trim(),
        subject: this.form.controls.subject.getRawValue().trim(),
        helpType: this.form.controls.helpType.getRawValue() as ContactInterventionHelpType,
        outcome: this.form.controls.outcome.getRawValue() as ContactInterventionOutcome,
        notes: this.normalizeOptional(this.form.controls.notes.getRawValue()),
        occurredUtc: this.toUtcIsoString(this.form.controls.occurredUtc.getRawValue())
      };

      const intervention = await firstValueFrom(
        this.sharedCatalogsService.createContactIntervention(
          this.form.controls.contactId.getRawValue(),
          request)
      );

      this.saved.emit(intervention);
      this.closeModal();
    } catch (error) {
      this.submitError.set(getApiErrorMessage(error, 'No fue posible registrar la intervención.'));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  private async loadContacts() {
    this.isContactsLoading.set(true);
    this.loadError.set(null);

    try {
      const contacts = await firstValueFrom(this.sharedCatalogsService.getContacts());
      this.contacts.set(contacts);
    } catch (error) {
      this.loadError.set(getApiErrorMessage(error, 'No fue posible cargar los contactos disponibles.'));
      this.contacts.set([]);
    } finally {
      this.isContactsLoading.set(false);
    }
  }

  private resetFormForContext() {
    this.contactSearch.set('');
    this.loadError.set(null);
    this.submitError.set(null);
    this.form.reset({
      contactId: '',
      helpType: '',
      outcome: '',
      occurredUtc: this.formatForDateTimeInput(this.resolveInitialOccurredDate()),
      subject: this.defaultSubject(),
      notes: ''
    });
  }

  private closeModal() {
    this.isOpen.set(false);
    this.closed.emit();
  }

  private resolveInitialOccurredDate() {
    const providedOccurredUtc = this.occurredUtc();

    if (!providedOccurredUtc) {
      return new Date();
    }

    const parsedDate = new Date(providedOccurredUtc);
    return Number.isNaN(parsedDate.getTime()) ? new Date() : parsedDate;
  }

  private formatForDateTimeInput(date: Date) {
    const year = date.getFullYear();
    const month = this.padDatePart(date.getMonth() + 1);
    const day = this.padDatePart(date.getDate());
    const hours = this.padDatePart(date.getHours());
    const minutes = this.padDatePart(date.getMinutes());

    return `${year}-${month}-${day}T${hours}:${minutes}`;
  }

  private toUtcIsoString(localDateTime: string) {
    const parsedDate = new Date(localDateTime);

    if (Number.isNaN(parsedDate.getTime())) {
      return new Date().toISOString();
    }

    return parsedDate.toISOString();
  }

  private padDatePart(value: number) {
    return value.toString().padStart(2, '0');
  }

  private normalizeOptional(value: string) {
    const normalizedValue = value.trim();
    return normalizedValue.length > 0 ? normalizedValue : null;
  }

  private normalizeSearch(value: string) {
    return value
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .trim();
  }
}
