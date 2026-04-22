import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { ClosedItem } from '../../core/models/closeout.models';
import { CloseoutService } from '../../core/services/closeout.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  selector: 'app-history-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, ReactiveFormsModule, RouterLink],
  template: `
    <section class="page-shell">
      <article class="hero-card">
        <p class="page-kicker">TRACK 1 POST-MVP</p>
        <h2>Histórico y cerrados</h2>
        <p>
          Consulta simple de registros cerrados o archivados del MVP. Los elementos históricos se
          consultan sin mezclarse con las alertas activas ni reactivarse desde esta vista.
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
                <h3>Filtro</h3>
                <p>Recorta por módulo o por texto de referencia.</p>
              </div>
            </div>

            <form class="form-grid" [formGroup]="filtersForm" (ngSubmit)="applyFilters()">
              <label>
                <span>Módulo</span>
                <select formControlName="moduleCode">
                  <option value="">Todos</option>
                  <option value="MARKETS">Mercados</option>
                  <option value="DONATARIAS">Donatarias</option>
                  <option value="FINANCIALS">Financieras</option>
                  <option value="FEDERATION">Federación</option>
                </select>
              </label>

              <label>
                <span>Texto</span>
                <input type="text" formControlName="q" placeholder="Buscar por título, subtítulo o referencia" />
              </label>

              <div class="form-actions">
                <button type="submit">Aplicar filtro</button>
                <button type="button" class="ghost" (click)="clearFilters()">Limpiar</button>
              </div>
            </form>
          </article>

          <article class="card">
            <div class="card-header">
              <div>
                <h3>Conteo visible</h3>
                <p>Resumen mínimo por módulo dentro del histórico actual.</p>
              </div>
            </div>

            <dl class="stats-list">
              @for (item of groupedCounts(); track item.moduleCode) {
                <div><dt>{{ item.moduleName }}</dt><dd>{{ item.count }}</dd></div>
              }
            </dl>
          </article>
        </aside>

        <div class="detail-column">
          <article class="card">
            <div class="card-header">
              <div>
                <h3>Registros cerrados</h3>
                <p>Prioriza cierre formal real, luego cierre retrospectivo normalizado y solo al final fallback histórico legado.</p>
              </div>
              <button type="button" class="ghost" (click)="reloadPage()">Actualizar</button>
            </div>

            @if (isLoading()) {
              <p class="empty-state">Cargando histórico de cerrados...</p>
            } @else if (items().length === 0) {
              <p class="empty-state">No hay registros cerrados para el filtro seleccionado.</p>
            } @else {
              <div class="rows">
                @for (item of items(); track item.recordKey) {
                  <article class="row-card">
                    <div class="row-top">
                      <div>
                        <strong>{{ item.title }}</strong>
                        <p>{{ item.moduleName }} · {{ item.itemType }} · {{ item.subtitle }}</p>
                      </div>
                      <span class="status-pill">{{ item.statusName }}</span>
                    </div>

                    <p class="meta">
                      {{ item.historicalTimestampUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}
                      @if (item.reference) {
                        · {{ item.reference }}
                      }
                    </p>

                    <p class="meta">
                      Fuente: {{ getHistoricalSourceLabel(item) }}
                    </p>

                    <a [routerLink]="item.navigationPath">Abrir módulo</a>
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
        grid-template-columns: minmax(17rem, 22rem) minmax(0, 1fr);
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
        color: #5b6b68;
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
        background: rgba(71, 84, 103, 0.12);
        color: #344054;
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
export class HistoryPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly closeoutService = inject(CloseoutService);

  protected readonly isLoading = signal(true);
  protected readonly pageError = signal('');
  protected readonly items = signal<ClosedItem[]>([]);

  protected readonly filtersForm = this.formBuilder.nonNullable.group({
    moduleCode: [''],
    q: ['']
  });

  protected readonly groupedCounts = computed(() => {
    const groups = new Map<string, { moduleCode: string; moduleName: string; count: number }>();

    for (const item of this.items()) {
      const current = groups.get(item.moduleCode);
      if (current) {
        current.count += 1;
        continue;
      }

      groups.set(item.moduleCode, {
        moduleCode: item.moduleCode,
        moduleName: item.moduleName,
        count: 1
      });
    }

    return Array.from(groups.values()).sort((left, right) => left.moduleName.localeCompare(right.moduleName));
  });

  constructor() {
    void this.loadItems();
  }

  protected async applyFilters() {
    await this.loadItems();
  }

  protected async clearFilters() {
    this.filtersForm.reset({
      moduleCode: '',
      q: ''
    });

    await this.loadItems();
  }

  protected async reloadPage() {
    await this.loadItems();
  }

  protected getHistoricalSourceLabel(item: ClosedItem) {
    switch (item.historicalTimestampSource) {
      case 'FORMAL_CLOSE_EVENT':
        return 'cierre formal';
      case 'LEGACY_CLOSE_NORMALIZED':
        return 'cierre retrospectivo normalizado';
      default:
        return 'fallback histórico legado';
    }
  }

  private async loadItems() {
    this.isLoading.set(true);
    this.pageError.set('');

    try {
      const filters = this.filtersForm.getRawValue();
      const items = await firstValueFrom(
        this.closeoutService.getClosedItems({
          moduleCode: filters.moduleCode || null,
          q: filters.q || null
        }));

      this.items.set(items);
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible cargar la consulta histórica del MVP.'));
    } finally {
      this.isLoading.set(false);
    }
  }
}
