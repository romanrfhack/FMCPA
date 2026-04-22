import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { CatalogItem } from '../../core/models/shared-catalogs.models';
import { SharedCatalogsService } from '../../core/services/shared-catalogs.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  selector: 'app-evidence-types-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  template: `
    <section class="page-shell">
      <article class="hero-card">
        <p class="page-kicker">STAGE-02</p>
        <h2>Catalogo de tipos de evidencia</h2>
        <p>
          Base compartida para clasificar soportes y evidencias de etapas posteriores sin
          definir todavia expedientes o procesos finales.
        </p>
      </article>

      <div class="page-grid">
        <article class="form-card">
          <div class="card-header">
            <div>
              <h3>Alta minima</h3>
              <p>Registro controlado de tipos base reutilizables.</p>
            </div>
          </div>

          @if (submitError()) {
            <p class="alert error">{{ submitError() }}</p>
          }

          @if (submitSuccess()) {
            <p class="alert success">{{ submitSuccess() }}</p>
          }

          <form class="form-grid" [formGroup]="form" (ngSubmit)="submit()">
            <label>
              <span>Codigo</span>
              <input type="text" formControlName="code" placeholder="PHOTO" />
            </label>

            <label>
              <span>Nombre</span>
              <input type="text" formControlName="name" placeholder="Fotografia" />
            </label>

            <label class="full-width">
              <span>Descripcion</span>
              <textarea formControlName="description" rows="4" placeholder="Uso base del tipo de evidencia"></textarea>
            </label>

            <label>
              <span>Orden</span>
              <input type="number" formControlName="sortOrder" min="1" />
            </label>

            <div class="form-actions full-width">
              <button type="submit" [disabled]="isSubmitting()">Agregar tipo</button>
              <button type="button" class="ghost" (click)="resetForm()">Limpiar</button>
            </div>
          </form>
        </article>

        <article class="list-card">
          <div class="card-header">
            <div>
              <h3>Tipos registrados</h3>
              <p>Catalogo sembrado en STAGE-02 y ampliable de forma controlada.</p>
            </div>
            <button type="button" class="ghost" (click)="reload()">Actualizar</button>
          </div>

          @if (loadError()) {
            <p class="alert error">{{ loadError() }}</p>
          } @else if (isLoading()) {
            <p class="empty-state">Cargando tipos de evidencia...</p>
          } @else {
            <div class="catalog-list">
              @for (item of items(); track item.id) {
                <article class="catalog-row">
                  <div class="row-top">
                    <div>
                      <h4>{{ item.name }}</h4>
                      <p class="meta">{{ item.code }} · Orden {{ item.sortOrder }}</p>
                    </div>
                    <span class="badge">{{ item.isActive ? 'Activo' : 'Inactivo' }}</span>
                  </div>
                  <p class="description">{{ item.description || 'Sin descripcion adicional.' }}</p>
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
        grid-template-columns: minmax(20rem, 23rem) minmax(0, 1fr);
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
      p {
        margin: 0;
      }

      .card-header,
      .row-top {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        align-items: flex-start;
      }

      .card-header {
        margin-bottom: 1rem;
      }

      .alert,
      .badge {
        padding: 0.75rem 0.9rem;
        border-radius: 0.9rem;
        font-weight: 700;
      }

      .alert.error {
        margin-bottom: 1rem;
        background: rgba(180, 35, 24, 0.08);
        color: #b42318;
      }

      .alert.success {
        margin-bottom: 1rem;
        background: rgba(15, 118, 110, 0.1);
        color: #0f766e;
      }

      .badge {
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
        font-size: 0.8rem;
      }

      .form-grid {
        display: grid;
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
        opacity: 0.75;
        cursor: wait;
      }

      .catalog-list {
        display: grid;
        gap: 0.9rem;
      }

      .catalog-row {
        display: grid;
        gap: 0.6rem;
        padding: 1rem;
        border-radius: 1rem;
        background: #f6f5ef;
      }

      .meta,
      .description,
      .empty-state,
      .hero-card p:last-child,
      .card-header p {
        margin-top: 0.75rem;
        line-height: 1.6;
        color: #4d615c;
      }

      .description {
        margin-top: 0;
      }

      @media (max-width: 1100px) {
        .page-grid {
          grid-template-columns: 1fr;
        }
      }

      @media (max-width: 700px) {
        .card-header,
        .row-top,
        .form-actions {
          flex-direction: column;
        }
      }
    `
  ]
})
export class EvidenceTypesPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly sharedCatalogsService = inject(SharedCatalogsService);

  protected readonly items = signal<CatalogItem[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly isSubmitting = signal(false);
  protected readonly submitError = signal<string | null>(null);
  protected readonly submitSuccess = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    code: ['', [Validators.required, Validators.maxLength(50)]],
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(250)]],
    sortOrder: [10, [Validators.required, Validators.min(1)]]
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
      code: '',
      name: '',
      description: '',
      sortOrder: this.nextSortOrder()
    });
  }

  protected async submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.submitError.set('Completa los campos obligatorios del catalogo.');
      this.submitSuccess.set(null);
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);
    this.submitSuccess.set(null);

    try {
      const item = await firstValueFrom(
        this.sharedCatalogsService.createEvidenceType({
          code: this.form.controls.code.getRawValue(),
          name: this.form.controls.name.getRawValue(),
          description: this.normalizeOptional(this.form.controls.description.getRawValue()),
          sortOrder: this.form.controls.sortOrder.getRawValue()
        })
      );

      this.items.update((currentItems) =>
        [...currentItems, item].sort((left, right) => left.sortOrder - right.sortOrder || left.name.localeCompare(right.name, 'es')));

      this.resetForm();
      this.submitSuccess.set('Tipo de evidencia registrado correctamente.');
    } catch (error) {
      this.submitError.set(getApiErrorMessage(error, 'No fue posible registrar el tipo de evidencia.'));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  private async load() {
    this.isLoading.set(true);
    this.loadError.set(null);

    try {
      this.items.set(await firstValueFrom(this.sharedCatalogsService.getEvidenceTypes()));
      this.form.patchValue({ sortOrder: this.nextSortOrder() });
    } catch (error) {
      this.loadError.set(getApiErrorMessage(error, 'No fue posible cargar el catalogo de tipos de evidencia.'));
    } finally {
      this.isLoading.set(false);
    }
  }

  private nextSortOrder() {
    const items = this.items();
    return items.length > 0 ? Math.max(...items.map((item) => item.sortOrder)) + 1 : 1;
  }

  private normalizeOptional(value: string) {
    const normalizedValue = value.trim();
    return normalizedValue.length > 0 ? normalizedValue : null;
  }
}
