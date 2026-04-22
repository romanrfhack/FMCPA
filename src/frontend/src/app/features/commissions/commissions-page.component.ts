import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { ConsolidatedCommissionList } from '../../core/models/closeout.models';
import { CatalogItem } from '../../core/models/shared-catalogs.models';
import { CloseoutService } from '../../core/services/closeout.service';
import { SharedCatalogsService } from '../../core/services/shared-catalogs.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  selector: 'app-commissions-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, ReactiveFormsModule, RouterLink],
  template: `
    <section class="page-shell">
      <article class="hero-card">
        <p class="page-kicker">STAGE-07</p>
        <h2>Comisiones operativas consolidadas</h2>
        <p>
          Consulta transversal minima de comisiones provenientes de Financieras y Federación,
          sin rehacer los modelos internos ni abrir todavía analítica avanzada.
        </p>
      </article>

      @if (pageError()) {
        <p class="alert error">{{ pageError() }}</p>
      }

      <div class="page-grid">
        <aside class="sidebar">
          <article class="card">
            <div class="card-header">
              <div>
                <h3>Filtros</h3>
                <p>Filtra por origen, rango, tipo y categoría del destinatario.</p>
              </div>
            </div>

            <form class="form-grid" [formGroup]="filtersForm" (ngSubmit)="applyFilters()">
              <label>
                <span>Origen</span>
                <select formControlName="sourceModuleCode">
                  <option value="">Todos</option>
                  <option value="FINANCIALS">Financieras</option>
                  <option value="FEDERATION">Federación</option>
                </select>
              </label>

              <label>
                <span>Tipo de comisión</span>
                <select formControlName="commissionTypeId">
                  <option [value]="0">Todos</option>
                  @for (type of commissionTypes(); track type.id) {
                    <option [value]="type.id">{{ type.name }}</option>
                  }
                </select>
              </label>

              <label>
                <span>Categoría</span>
                <select formControlName="recipientCategory">
                  <option value="">Todas</option>
                  <option value="COMPANY">COMPANY</option>
                  <option value="THIRD_PARTY">THIRD_PARTY</option>
                  <option value="OTHER_PARTICIPANT">OTHER_PARTICIPANT</option>
                </select>
              </label>

              <label>
                <span>Desde</span>
                <input type="date" formControlName="fromDate" />
              </label>

              <label>
                <span>Hasta</span>
                <input type="date" formControlName="toDate" />
              </label>

              <label class="full-width">
                <span>Texto libre</span>
                <input type="text" formControlName="q" placeholder="Buscar por origen, referencia o destinatario" />
              </label>

              <div class="form-actions full-width">
                <button type="submit">Aplicar filtro</button>
                <button type="button" class="ghost" (click)="clearFilters()">Limpiar</button>
              </div>
            </form>
          </article>

          @if (list(); as listData) {
            <article class="card">
              <div class="card-header">
                <div>
                  <h3>Resumen</h3>
                  <p>Totales visibles en la consulta operativa actual.</p>
                </div>
                <button type="button" class="ghost" (click)="reloadPage()">Actualizar</button>
              </div>

              <dl class="stats-list">
                <div><dt>Registros</dt><dd>{{ listData.totalCount }}</dd></div>
                <div><dt>Monto base</dt><dd>{{ listData.totalBaseAmount | number: '1.0-2' }}</dd></div>
                <div><dt>Monto comisión</dt><dd>{{ listData.totalCommissionAmount | number: '1.0-2' }}</dd></div>
              </dl>
            </article>
          }
        </aside>

        <div class="detail-column">
          <article class="card">
            <div class="card-header">
              <div>
                <h3>Vista consolidada</h3>
                <p>Origen, destinatario, referencia y montos operativos.</p>
              </div>
            </div>

            @if (isLoading()) {
              <p class="empty-state">Cargando comisiones consolidadas...</p>
            } @else if (!list() || list()!.items.length === 0) {
              <p class="empty-state">No hay comisiones para el filtro actual.</p>
            } @else {
              <div class="rows">
                @for (item of list()!.items; track item.commissionId) {
                  <article class="row-card">
                    <div class="row-top">
                      <div>
                        <strong>{{ item.recipientName }}</strong>
                        <p>{{ item.sourceModuleName }} · {{ item.commissionTypeName }}</p>
                      </div>

                      <span class="status-pill" [class]="recipientCategoryClass(item.recipientCategory)">
                        {{ item.recipientCategory }}
                      </span>
                    </div>

                    <p class="meta">
                      Operación {{ item.operationDate }} · Referencia {{ item.originReference }}
                    </p>
                    <p class="meta">
                      {{ item.originPrimaryName }} · {{ item.originSecondaryName }}
                    </p>

                    <div class="amount-grid">
                      <article>
                        <span>Monto base</span>
                        <strong>{{ item.baseAmount | number: '1.0-2' }}</strong>
                      </article>
                      <article>
                        <span>Comisión</span>
                        <strong>{{ item.commissionAmount | number: '1.0-2' }}</strong>
                      </article>
                    </div>

                    @if (item.notes) {
                      <p class="note">{{ item.notes }}</p>
                    }

                    <a [routerLink]="item.navigationPath">Abrir origen</a>
                  </article>
                }
              </div>
            }
          </article>
        </div>
      </div>
    </section>
  `,
  styles: [
    `
      .page-shell,
      .page-grid {
        display: grid;
        gap: 1rem;
      }

      .page-grid {
        grid-template-columns: minmax(18rem, 24rem) minmax(0, 1fr);
      }

      .hero-card,
      .card,
      .row-card {
        padding: 1.5rem;
        border-radius: 1.35rem;
        background: rgba(255, 255, 255, 0.82);
        border: 1px solid rgba(29, 45, 42, 0.08);
        box-shadow: 0 16px 30px rgba(32, 44, 41, 0.06);
      }

      .page-kicker {
        margin: 0 0 0.45rem;
        letter-spacing: 0.12em;
        text-transform: uppercase;
        font-size: 0.78rem;
        font-weight: 700;
        color: #0f766e;
      }

      .hero-card h2,
      .card h3,
      .row-card strong {
        margin: 0;
      }

      .hero-card p:last-child,
      .card-header p,
      .meta,
      .note {
        color: #4d615c;
      }

      .card-header,
      .row-top {
        display: flex;
        align-items: start;
        justify-content: space-between;
        gap: 1rem;
      }

      .form-grid,
      .rows {
        display: grid;
        gap: 0.9rem;
      }

      label {
        display: grid;
        gap: 0.35rem;
      }

      .full-width {
        grid-column: 1 / -1;
      }

      input,
      select,
      button {
        border-radius: 0.85rem;
      }

      .form-actions {
        display: flex;
        gap: 0.75rem;
      }

      .stats-list {
        display: grid;
        gap: 0.8rem;
        margin: 1rem 0 0;
      }

      .stats-list div {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
      }

      dt {
        color: #5b6b68;
      }

      dd {
        margin: 0;
        font-weight: 800;
        color: #143631;
      }

      .rows {
        margin-top: 1rem;
      }

      .row-card {
        padding: 1rem 1.1rem;
      }

      .row-card p {
        margin: 0.3rem 0 0;
      }

      .amount-grid {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 0.75rem;
        margin-top: 0.9rem;
      }

      .amount-grid article {
        padding: 0.85rem;
        border-radius: 0.95rem;
        background: rgba(244, 247, 246, 0.9);
      }

      .amount-grid span {
        display: block;
        color: #5b6b68;
      }

      .amount-grid strong {
        display: block;
        margin-top: 0.25rem;
        font-size: 1.05rem;
      }

      .status-pill {
        display: inline-flex;
        align-items: center;
        padding: 0.3rem 0.65rem;
        border-radius: 999px;
        font-size: 0.72rem;
        font-weight: 700;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        background: rgba(15, 118, 110, 0.12);
        color: #0f766e;
      }

      .status-pill.company {
        background: rgba(15, 118, 110, 0.12);
        color: #0f766e;
      }

      .status-pill.third-party {
        background: rgba(59, 130, 246, 0.14);
        color: #175cd3;
      }

      .status-pill.other {
        background: rgba(234, 179, 8, 0.14);
        color: #8a6116;
      }

      .alert {
        padding: 0.9rem 1rem;
        border-radius: 0.9rem;
        font-weight: 600;
      }

      .alert.error {
        background: rgba(254, 243, 242, 0.9);
        color: #b42318;
      }

      .empty-state {
        margin: 0;
        color: #5b6b68;
      }

      .ghost {
        border: 1px solid rgba(15, 118, 110, 0.18);
        background: transparent;
        color: #0f766e;
      }

      @media (max-width: 980px) {
        .page-grid {
          grid-template-columns: 1fr;
        }
      }
    `
  ]
})
export class CommissionsPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly closeoutService = inject(CloseoutService);
  private readonly sharedCatalogsService = inject(SharedCatalogsService);

  protected readonly isLoading = signal(true);
  protected readonly pageError = signal('');
  protected readonly list = signal<ConsolidatedCommissionList | null>(null);
  protected readonly commissionTypes = signal<CatalogItem[]>([]);

  protected readonly filtersForm = this.formBuilder.nonNullable.group({
    sourceModuleCode: [''],
    commissionTypeId: [0],
    recipientCategory: [''],
    fromDate: [''],
    toDate: [''],
    q: ['']
  });

  constructor() {
    void this.loadPage();
  }

  protected async applyFilters() {
    await this.loadList();
  }

  protected async clearFilters() {
    this.filtersForm.reset({
      sourceModuleCode: '',
      commissionTypeId: 0,
      recipientCategory: '',
      fromDate: '',
      toDate: '',
      q: ''
    });

    await this.loadList();
  }

  protected async reloadPage() {
    await this.loadPage();
  }

  protected recipientCategoryClass(category: string) {
    switch (category) {
      case 'COMPANY':
        return 'company';
      case 'THIRD_PARTY':
        return 'third-party';
      default:
        return 'other';
    }
  }

  private async loadPage() {
    this.isLoading.set(true);
    this.pageError.set('');

    try {
      const [commissionTypes, list] = await Promise.all([
        firstValueFrom(this.sharedCatalogsService.getCommissionTypes()),
        firstValueFrom(this.closeoutService.getConsolidatedCommissions())
      ]);

      this.commissionTypes.set(commissionTypes);
      this.list.set(list);
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible cargar la vista operativa de comisiones.'));
    } finally {
      this.isLoading.set(false);
    }
  }

  private async loadList() {
    this.isLoading.set(true);
    this.pageError.set('');

    try {
      const filters = this.filtersForm.getRawValue();
      const list = await firstValueFrom(
        this.closeoutService.getConsolidatedCommissions({
          sourceModuleCode: filters.sourceModuleCode || null,
          commissionTypeId: filters.commissionTypeId > 0 ? filters.commissionTypeId : null,
          recipientCategory: filters.recipientCategory || null,
          fromDate: filters.fromDate || null,
          toDate: filters.toDate || null,
          q: filters.q || null
        }));

      this.list.set(list);
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible actualizar la consulta de comisiones.'));
    } finally {
      this.isLoading.set(false);
    }
  }
}
