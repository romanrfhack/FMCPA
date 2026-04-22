import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { SharedCatalogsService } from '../../core/services/shared-catalogs.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';
import { Contact, ContactType } from '../../core/models/shared-catalogs.models';

@Component({
  selector: 'app-contacts-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, ReactiveFormsModule],
  template: `
    <section class="page-shell">
      <article class="hero-card">
        <p class="page-kicker">STAGE-02</p>
        <h2>Contactos compartidos</h2>
        <p>
          Catalogo reutilizable de contactos internos y externos listo para relacionarse con
          proyectos, gestiones y modulos futuros sin modelar todavia esas entidades de negocio.
        </p>
      </article>

      <div class="page-grid">
        <article class="form-card">
          <div class="card-header">
            <div>
              <h3>Alta minima</h3>
              <p>Captura base reutilizable para operacion interna y externa.</p>
            </div>
            @if (isSubmitting()) {
              <span class="badge neutral">Guardando...</span>
            }
          </div>

          @if (submitError()) {
            <p class="alert error">{{ submitError() }}</p>
          }

          @if (submitSuccess()) {
            <p class="alert success">{{ submitSuccess() }}</p>
          }

          <form class="form-grid" [formGroup]="form" (ngSubmit)="submit()">
            <label>
              <span>Nombre</span>
              <input type="text" formControlName="name" placeholder="Nombre completo" />
            </label>

            <label>
              <span>Tipo</span>
              <select formControlName="contactTypeId">
                <option [value]="0">Selecciona un tipo</option>
                @for (contactType of contactTypes(); track contactType.id) {
                  <option [value]="contactType.id">{{ contactType.name }}</option>
                }
              </select>
            </label>

            <label>
              <span>Organizacion o dependencia</span>
              <input type="text" formControlName="organizationOrDependency" placeholder="Area, dependencia u organizacion" />
            </label>

            <label>
              <span>Cargo o rol</span>
              <input type="text" formControlName="roleTitle" placeholder="Cargo o rol del contacto" />
            </label>

            <label>
              <span>Celular</span>
              <input type="text" formControlName="mobilePhone" placeholder="Telefono celular" />
            </label>

            <label>
              <span>WhatsApp</span>
              <input type="text" formControlName="whatsAppPhone" placeholder="Numero de WhatsApp" />
            </label>

            <label>
              <span>Correo</span>
              <input type="email" formControlName="email" placeholder="correo@ejemplo.com" />
            </label>

            <label class="full-width">
              <span>Observaciones</span>
              <textarea formControlName="notes" rows="4" placeholder="Notas operativas reutilizables"></textarea>
            </label>

            <div class="form-actions full-width">
              <button type="submit" [disabled]="isSubmitting()">Registrar contacto</button>
              <button type="button" class="ghost" (click)="resetForm()">Limpiar</button>
            </div>
          </form>
        </article>

        <article class="list-card">
          <div class="card-header">
            <div>
              <h3>Contactos registrados</h3>
              <p>Listado disponible para futuras relaciones y reutilizacion transversal.</p>
            </div>
            <button type="button" class="ghost" (click)="reload()">Actualizar</button>
          </div>

          @if (loadError()) {
            <p class="alert error">{{ loadError() }}</p>
          } @else if (isLoading()) {
            <p class="empty-state">Cargando contactos y tipos base...</p>
          } @else if (contacts().length === 0) {
            <p class="empty-state">Aun no hay contactos registrados en este catalogo.</p>
          } @else {
            <div class="contact-list">
              @for (contact of contacts(); track contact.id) {
                <article class="contact-row">
                  <div>
                    <h4>{{ contact.name }}</h4>
                    <p class="meta">
                      {{ contact.contactTypeName }}
                      @if (contact.organizationOrDependency) {
                        <span> · {{ contact.organizationOrDependency }}</span>
                      }
                      @if (contact.roleTitle) {
                        <span> · {{ contact.roleTitle }}</span>
                      }
                    </p>
                  </div>

                  <dl class="detail-grid">
                    <div>
                      <dt>Celular</dt>
                      <dd>{{ contact.mobilePhone || 'Sin dato' }}</dd>
                    </div>
                    <div>
                      <dt>WhatsApp</dt>
                      <dd>{{ contact.whatsAppPhone || 'Sin dato' }}</dd>
                    </div>
                    <div>
                      <dt>Correo</dt>
                      <dd>{{ contact.email || 'Sin dato' }}</dd>
                    </div>
                    <div>
                      <dt>Alta UTC</dt>
                      <dd>{{ contact.createdUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</dd>
                    </div>
                  </dl>

                  @if (contact.notes) {
                    <p class="notes">{{ contact.notes }}</p>
                  }
                </article>
              }
            </div>
          }
        </article>
      </div>
    </section>
  `,
  styles: [
    `
      .page-shell,
      .page-grid {
        display: grid;
        gap: 1.25rem;
      }

      .page-grid {
        grid-template-columns: minmax(20rem, 24rem) minmax(0, 1fr);
        align-items: start;
      }

      .hero-card,
      .form-card,
      .list-card {
        padding: 1.5rem;
        border-radius: 1.35rem;
        background: rgba(255, 255, 255, 0.82);
        border: 1px solid rgba(29, 45, 42, 0.08);
        box-shadow: 0 16px 30px rgba(32, 44, 41, 0.06);
      }

      .page-kicker {
        margin: 0 0 0.5rem;
        letter-spacing: 0.12em;
        text-transform: uppercase;
        font-size: 0.78rem;
        font-weight: 700;
        color: #0f766e;
      }

      h2,
      h3,
      h4,
      p,
      dd {
        margin: 0;
      }

      .hero-card p:last-child,
      .card-header p,
      .meta,
      .notes,
      .empty-state {
        margin-top: 0.75rem;
        line-height: 1.6;
        color: #4d615c;
      }

      .card-header {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        align-items: flex-start;
        margin-bottom: 1rem;
      }

      .badge {
        padding: 0.55rem 0.75rem;
        border-radius: 999px;
        font-size: 0.8rem;
        font-weight: 700;
      }

      .badge.neutral {
        background: rgba(148, 98, 0, 0.12);
        color: #7a5400;
      }

      .alert {
        margin-bottom: 1rem;
        padding: 0.85rem 0.95rem;
        border-radius: 0.9rem;
        font-weight: 600;
      }

      .alert.error {
        background: rgba(180, 35, 24, 0.08);
        color: #b42318;
      }

      .alert.success {
        background: rgba(15, 118, 110, 0.1);
        color: #0f766e;
      }

      .form-grid {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 0.9rem;
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
        padding: 0.8rem 0.9rem;
        border-radius: 0.9rem;
        border: 1px solid rgba(29, 45, 42, 0.14);
        background: #fbfbf8;
        color: #1d2d2a;
        font: inherit;
      }

      textarea {
        resize: vertical;
      }

      .full-width {
        grid-column: 1 / -1;
      }

      .form-actions {
        display: flex;
        gap: 0.75rem;
      }

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

      button:disabled {
        cursor: wait;
        opacity: 0.75;
      }

      .contact-list {
        display: grid;
        gap: 0.9rem;
      }

      .contact-row {
        display: grid;
        gap: 0.8rem;
        padding: 1rem;
        border-radius: 1rem;
        background: #f6f5ef;
      }

      .detail-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr));
        gap: 0.8rem;
        margin: 0;
      }

      dt {
        font-size: 0.78rem;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        color: #5b6b68;
      }

      dd {
        margin-top: 0.3rem;
        color: #223734;
      }

      .notes {
        margin-top: 0;
      }

      @media (max-width: 1100px) {
        .page-grid {
          grid-template-columns: 1fr;
        }
      }

      @media (max-width: 700px) {
        .form-grid {
          grid-template-columns: 1fr;
        }

        .card-header,
        .form-actions {
          flex-direction: column;
        }
      }
    `
  ]
})
export class ContactsPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly sharedCatalogsService = inject(SharedCatalogsService);

  protected readonly contactTypes = signal<ContactType[]>([]);
  protected readonly contacts = signal<Contact[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly isSubmitting = signal(false);
  protected readonly submitError = signal<string | null>(null);
  protected readonly submitSuccess = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    contactTypeId: [0, [Validators.required, Validators.min(1)]],
    organizationOrDependency: ['', [Validators.maxLength(200)]],
    roleTitle: ['', [Validators.maxLength(150)]],
    mobilePhone: ['', [Validators.maxLength(30)]],
    whatsAppPhone: ['', [Validators.maxLength(30)]],
    email: ['', [Validators.email, Validators.maxLength(200)]],
    notes: ['', [Validators.maxLength(1000)]]
  });

  constructor() {
    void this.load();
  }

  protected async reload() {
    await this.load();
  }

  protected resetForm() {
    this.submitError.set(null);
    this.submitSuccess.set(null);
    this.form.reset({
      name: '',
      contactTypeId: this.contactTypes()[0]?.id ?? 0,
      organizationOrDependency: '',
      roleTitle: '',
      mobilePhone: '',
      whatsAppPhone: '',
      email: '',
      notes: ''
    });
  }

  protected async submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.submitError.set('Completa los campos obligatorios del contacto compartido.');
      this.submitSuccess.set(null);
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);
    this.submitSuccess.set(null);

    try {
      const contact = await firstValueFrom(
        this.sharedCatalogsService.createContact({
          name: this.form.controls.name.getRawValue(),
          contactTypeId: this.form.controls.contactTypeId.getRawValue(),
          organizationOrDependency: this.normalizeOptional(this.form.controls.organizationOrDependency.getRawValue()),
          roleTitle: this.normalizeOptional(this.form.controls.roleTitle.getRawValue()),
          mobilePhone: this.normalizeOptional(this.form.controls.mobilePhone.getRawValue()),
          whatsAppPhone: this.normalizeOptional(this.form.controls.whatsAppPhone.getRawValue()),
          email: this.normalizeOptional(this.form.controls.email.getRawValue()),
          notes: this.normalizeOptional(this.form.controls.notes.getRawValue())
        })
      );

      this.contacts.update((currentContacts) =>
        [...currentContacts, contact].sort((left, right) => left.name.localeCompare(right.name, 'es')));

      this.resetForm();
      this.submitSuccess.set('Contacto registrado correctamente.');
    } catch (error) {
      this.submitError.set(getApiErrorMessage(error, 'No fue posible registrar el contacto.'));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  private async load() {
    this.isLoading.set(true);
    this.loadError.set(null);

    try {
      const [contactTypes, contacts] = await Promise.all([
        firstValueFrom(this.sharedCatalogsService.getContactTypes()),
        firstValueFrom(this.sharedCatalogsService.getContacts())
      ]);

      this.contactTypes.set(contactTypes);
      this.contacts.set(contacts);

      if (this.form.controls.contactTypeId.getRawValue() === 0 && contactTypes.length > 0) {
        this.form.patchValue({ contactTypeId: contactTypes[0].id });
      }
    } catch (error) {
      this.loadError.set(getApiErrorMessage(error, 'No fue posible cargar el catalogo de contactos.'));
    } finally {
      this.isLoading.set(false);
    }
  }

  private normalizeOptional(value: string) {
    const normalizedValue = value.trim();
    return normalizedValue.length > 0 ? normalizedValue : null;
  }
}
