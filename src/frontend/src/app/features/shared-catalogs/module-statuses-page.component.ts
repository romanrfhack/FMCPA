import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { ModuleStatusCatalogEntry } from '../../core/models/shared-catalogs.models';
import { SharedCatalogsService } from '../../core/services/shared-catalogs.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  selector: 'app-module-statuses-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  template: `
    <section class="page-shell">
      <article class="hero-card">
        <p class="page-kicker">STAGE-02</p>
        <h2>Estatus por modulo</h2>
        <p>
          Representacion reusable de estatus por modulo para soportar historico y alertas futuras
          sin fijar todavia el modelo definitivo de cada dominio.
        </p>
      </article>

      <div class="page-grid">
        <article class="form-card">
          <div class="card-header">
            <div>
              <h3>Alta minima</h3>
              <p>Modulo, estatus y banderas base para comportamiento transversal.</p>
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
              <span>Codigo de modulo</span>
              <input type="text" formControlName="moduleCode" placeholder="MARKETS" />
            </label>

            <label>
              <span>Nombre de modulo</span>
              <input type="text" formControlName="moduleName" placeholder="Mercados" />
            </label>

            <label>
              <span>Codigo de estatus</span>
              <input type="text" formControlName="statusCode" placeholder="ACTIVE" />
            </label>

            <label>
              <span>Nombre de estatus</span>
              <input type="text" formControlName="statusName" placeholder="Activo" />
            </label>

            <label class="full-width">
              <span>Descripcion</span>
              <textarea formControlName="description" rows="4" placeholder="Descripcion operativa del estatus"></textarea>
            </label>

            <label>
              <span>Orden</span>
              <input type="number" formControlName="sortOrder" min="1" />
            </label>

            <label class="toggle">
              <input type="checkbox" formControlName="isClosed" />
              <span>Marca cierre operativo</span>
            </label>

            <label class="toggle">
              <input type="checkbox" formControlName="alertsEnabledByDefault" />
              <span>Alertas activas por defecto</span>
            </label>

            <div class="form-actions full-width">
              <button type="submit" [disabled]="isSubmitting()">Agregar estatus</button>
              <button type="button" class="ghost" (click)="resetForm()">Limpiar</button>
            </div>
          </form>
        </article>

        <article class="list-card">
          <div class="card-header">
            <div>
              <h3>Estatus registrados</h3>
              <p>Semilla base reusable para modulos presentes en el alcance del sistema.</p>
            </div>
            <button type="button" class="ghost" (click)="reload()">Actualizar</button>
          </div>

          @if (loadError()) {
            <p class="alert error">{{ loadError() }}</p>
          } @else if (isLoading()) {
            <p class="empty-state">Cargando estatus por modulo...</p>
          } @else {
            <div class="status-list">
              @for (item of items(); track item.id) {
                <article class="status-row">
                  <div class="row-top">
                    <div>
                      <h4>{{ item.moduleName }} · {{ item.statusName }}</h4>
                      <p class="meta">
                        {{ item.moduleCode }} / {{ item.contextCode }} / {{ item.statusCode }} · Orden {{ item.sortOrder }}
                      </p>
                    </div>
                    <div class="badges">
                      <span class="badge">{{ item.isClosed ? 'Cierre' : 'Seguimiento' }}</span>
                      <span class="badge">{{ item.alertsEnabledByDefault ? 'Con alertas' : 'Sin alertas' }}</span>
                    </div>
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

      .alert {
        margin-bottom: 1rem;
        padding: 0.75rem 0.9rem;
        border-radius: 0.9rem;
        font-weight: 700;
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

      .toggle {
        grid-template-columns: auto 1fr;
        align-items: center;
        gap: 0.7rem;
        padding: 0.8rem 0.9rem;
        border-radius: 0.9rem;
        background: #f6f5ef;
      }

      .toggle input {
        width: auto;
        margin: 0;
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
        opacity: 0.75;
        cursor: wait;
      }

      .status-list {
        display: grid;
        gap: 0.9rem;
      }

      .status-row {
        display: grid;
        gap: 0.6rem;
        padding: 1rem;
        border-radius: 1rem;
        background: #f6f5ef;
      }

      .badges {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
      }

      .badge {
        padding: 0.55rem 0.75rem;
        border-radius: 999px;
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
        font-size: 0.8rem;
        font-weight: 700;
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
        .form-grid {
          grid-template-columns: 1fr;
        }

        .card-header,
        .row-top,
        .form-actions {
          flex-direction: column;
        }
      }
    `
  ]
})
export class ModuleStatusesPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly sharedCatalogsService = inject(SharedCatalogsService);

  protected readonly items = signal<ModuleStatusCatalogEntry[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly isSubmitting = signal(false);
  protected readonly submitError = signal<string | null>(null);
  protected readonly submitSuccess = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    moduleCode: ['', [Validators.required, Validators.maxLength(50)]],
    moduleName: ['', [Validators.required, Validators.maxLength(100)]],
    statusCode: ['', [Validators.required, Validators.maxLength(50)]],
    statusName: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.maxLength(250)]],
    sortOrder: [10, [Validators.required, Validators.min(1)]],
    isClosed: [false],
    alertsEnabledByDefault: [true]
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
      moduleCode: '',
      moduleName: '',
      statusCode: '',
      statusName: '',
      description: '',
      sortOrder: 1,
      isClosed: false,
      alertsEnabledByDefault: true
    });
  }

  protected async submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.submitError.set('Completa los campos obligatorios del estatus por modulo.');
      this.submitSuccess.set(null);
      return;
    }

    this.isSubmitting.set(true);
    this.submitError.set(null);
    this.submitSuccess.set(null);

    try {
      const item = await firstValueFrom(
        this.sharedCatalogsService.createModuleStatus({
          moduleCode: this.form.controls.moduleCode.getRawValue(),
          moduleName: this.form.controls.moduleName.getRawValue(),
          contextCode: null,
          contextName: null,
          statusCode: this.form.controls.statusCode.getRawValue(),
          statusName: this.form.controls.statusName.getRawValue(),
          description: this.normalizeOptional(this.form.controls.description.getRawValue()),
          sortOrder: this.form.controls.sortOrder.getRawValue(),
          isClosed: this.form.controls.isClosed.getRawValue(),
          alertsEnabledByDefault: this.form.controls.alertsEnabledByDefault.getRawValue()
        })
      );

      this.items.update((currentItems) =>
        [...currentItems, item].sort((left, right) =>
          left.moduleName.localeCompare(right.moduleName, 'es')
          || left.sortOrder - right.sortOrder
          || left.statusName.localeCompare(right.statusName, 'es')));

      this.resetForm();
      this.submitSuccess.set('Estatus por modulo registrado correctamente.');
    } catch (error) {
      this.submitError.set(getApiErrorMessage(error, 'No fue posible registrar el estatus por modulo.'));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  private async load() {
    this.isLoading.set(true);
    this.loadError.set(null);

    try {
      this.items.set(await firstValueFrom(this.sharedCatalogsService.getModuleStatuses()));
    } catch (error) {
      this.loadError.set(getApiErrorMessage(error, 'No fue posible cargar el catalogo de estatus por modulo.'));
    } finally {
      this.isLoading.set(false);
    }
  }

  private normalizeOptional(value: string) {
    const normalizedValue = value.trim();
    return normalizedValue.length > 0 ? normalizedValue : null;
  }
}
