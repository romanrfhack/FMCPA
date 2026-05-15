import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { DocumentWorkQueueFilters, DocumentWorkQueueItem } from '../../core/models/document-catalog.models';
import { DocumentCatalogService } from '../../core/services/document-catalog.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  selector: 'app-documents-work-queue-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  template: `
    <section class="page-shell">
      <header class="page-header">
        <p class="page-kicker">Control documental</p>
        <h2>Bandeja documental</h2>
      </header>

      @if (pageError()) {
        <p class="alert error">{{ pageError() }}</p>
      }

      <section class="filters-panel" aria-label="Filtros de bandeja documental">
        <form class="filters-grid" [formGroup]="filtersForm" (ngSubmit)="reload()">
          <label>
            <span>Modulo</span>
            <select formControlName="moduleCode">
              <option value="">Todos</option>
              <option value="MARKETS">Mercados</option>
              <option value="DONATARIAS">Donatarias</option>
              <option value="FEDERATION">Federacion</option>
            </select>
          </label>

          <label>
            <span>Tipo de pendiente</span>
            <select formControlName="workItemType">
              <option value="">Todos</option>
              <option value="COMPLETENESS_PENDING">Evidencia pendiente</option>
              <option value="DOCUMENT_INTEGRITY_ISSUE">Integridad documental</option>
              <option value="RETENTION_REVIEW">Revision de retencion</option>
            </select>
          </label>

          <label>
            <span>Prioridad</span>
            <select formControlName="severityCode">
              <option value="">Todas</option>
              <option value="HIGH">Alta</option>
              <option value="MEDIUM">Media</option>
              <option value="LOW">Baja</option>
            </select>
          </label>

          <label>
            <span>Limite</span>
            <input type="number" min="1" max="200" formControlName="take" />
          </label>

          <div class="filter-actions">
            <button type="submit" [disabled]="isLoading()">Filtrar</button>
            <button type="button" class="ghost" (click)="resetFilters()">Limpiar</button>
          </div>
        </form>

        <div class="export-actions">
          <button type="button" class="ghost compact" [disabled]="isExporting()" (click)="exportWorkQueue()">
            Exportar CSV
          </button>
        </div>
      </section>

      <article class="panel">
        <div class="panel-header">
          <h3>Trabajo pendiente</h3>
          <span>{{ resultCount() ?? 0 }} de {{ totalCount() ?? 0 }}</span>
        </div>

        @if (isLoading()) {
          <p class="empty-state">Cargando bandeja documental...</p>
        } @else if (items().length === 0) {
          <p class="empty-state">No hay señales documentales con los filtros actuales.</p>
        } @else {
          <div class="queue-list">
            @for (item of items(); track item.workItemKey) {
              <article class="queue-row">
                <div>
                  <div class="badges">
                    <span class="severity" [class.medium]="item.severityCode === 'MEDIUM'" [class.low]="item.severityCode === 'LOW'">
                      {{ severityLabel(item.severityCode) }}
                    </span>
                    <span>{{ labelForType(item.workItemType) }}</span>
                    <span>{{ item.moduleName }}</span>
                    @if (item.documentOperationalStatusCode) {
                      <span
                        class="operational"
                        [class.high]="item.documentOperationalSeverityCode === 'HIGH'"
                        [class.medium]="item.documentOperationalSeverityCode === 'MEDIUM'"
                        [class.low]="item.documentOperationalSeverityCode === 'LOW'">
                        {{ operationalStatusLabel(item.documentOperationalStatusCode) }}
                      </span>
                    }
                  </div>
                  <h4>{{ item.title }}</h4>
                  <p>{{ item.summary }}</p>
                  <small>{{ item.originContext.displayName }} · {{ reasonLabel(item.reasonCode) }} · {{ currentStatusLabel(item.currentStatusCode) }}</small>
                  @if (item.remediationHint) {
                    <p class="hint">{{ item.remediationHint }}</p>
                  }
                </div>
                <div class="row-actions">
                  <a [href]="resolveActionRoute(item)">Ir al origen</a>
                  @if (item.documentId) {
                    <small>Documento {{ item.documentId }}</small>
                  }
                </div>
              </article>
            }
          </div>
        }
      </article>
    </section>
  `,
  styles: [
    `
      .page-shell,
      .queue-list {
        display: grid;
        gap: 1rem;
      }

      :host {
        display: block;
        min-width: 0;
      }

      .page-header,
      .filters-panel,
      .panel {
        border: 1px solid rgba(35, 51, 47, 0.12);
        border-radius: 8px;
        background: rgba(255, 255, 255, 0.9);
        box-shadow: 0 14px 26px rgba(32, 44, 41, 0.06);
      }

      .page-header,
      .panel {
        padding: 1.25rem;
      }

      .page-kicker,
      h2,
      h3,
      h4,
      p {
        margin: 0;
      }

      .page-kicker {
        margin-bottom: 0.4rem;
        font-size: 0.78rem;
        font-weight: 800;
        color: #0f766e;
      }

      .filters-panel {
        display: grid;
        grid-template-columns: minmax(0, 1fr) auto;
        gap: 0.75rem;
        padding: 0.9rem;
        align-items: end;
      }

      .filters-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(10.5rem, 1fr));
        gap: 0.7rem;
        align-items: end;
      }

      label {
        display: grid;
        gap: 0.35rem;
        font-weight: 700;
      }

      input,
      select {
        width: 100%;
        min-width: 0;
        box-sizing: border-box;
        border: 1px solid rgba(35, 51, 47, 0.16);
        border-radius: 8px;
        padding: 0.58rem 0.7rem;
        font: inherit;
        background: #fff;
      }

      button,
      a {
        min-width: 0;
        border: 0;
        border-radius: 8px;
        padding: 0.68rem 0.9rem;
        font-weight: 800;
        color: #fff;
        background: #0f766e;
        text-decoration: none;
        cursor: pointer;
      }

      button.ghost {
        color: #0f766e;
        background: rgba(15, 118, 110, 0.1);
      }

      button.compact {
        padding: 0.58rem 0.8rem;
      }

      .filter-actions,
      .panel-header,
      .queue-row,
      .badges,
      .row-actions {
        display: flex;
        gap: 0.65rem;
      }

      .filter-actions,
      .export-actions {
        flex-wrap: wrap;
        justify-content: flex-end;
      }

      .export-actions {
        display: flex;
      }

      .panel-header,
      .queue-row {
        justify-content: space-between;
        align-items: flex-start;
      }

      .queue-row {
        min-width: 0;
        padding: 1rem 0;
        border-top: 1px solid rgba(35, 51, 47, 0.08);
      }

      .queue-row > div:first-child {
        min-width: 0;
      }

      .queue-row:first-child {
        border-top: 0;
        padding-top: 0;
      }

      .badges {
        flex-wrap: wrap;
        margin-bottom: 0.35rem;
      }

      .badges span,
      .empty-state,
      small {
        color: #60716d;
      }

      .badges span {
        border-radius: 999px;
        padding: 0.2rem 0.5rem;
        color: #0f766e;
        background: rgba(15, 118, 110, 0.1);
        font-size: 0.76rem;
        font-weight: 800;
      }

      .badges .severity {
        color: #be123c;
        background: rgba(190, 18, 60, 0.1);
      }

      .badges .severity.medium {
        color: #92400e;
        background: rgba(146, 64, 14, 0.1);
      }

      .badges .severity.low {
        color: #365314;
        background: rgba(77, 124, 15, 0.1);
      }

      .badges .operational.high {
        color: #be123c;
        background: rgba(190, 18, 60, 0.1);
      }

      .badges .operational.medium {
        color: #92400e;
        background: rgba(146, 64, 14, 0.1);
      }

      .badges .operational.low {
        color: #365314;
        background: rgba(77, 124, 15, 0.1);
      }

      .hint {
        margin-top: 0.45rem;
        color: #7c2d12;
        font-weight: 700;
      }

      .row-actions {
        display: grid;
        justify-items: end;
        min-width: 9rem;
      }

      .row-actions small,
      .queue-row h4,
      .queue-row p {
        overflow-wrap: anywhere;
      }

      .alert.error {
        border-radius: 8px;
        padding: 0.85rem 1rem;
        color: #be123c;
        background: rgba(190, 18, 60, 0.1);
      }

      @media (max-width: 920px) {
        .filters-panel,
        .filters-grid,
        .queue-row {
          display: grid;
          grid-template-columns: 1fr;
        }

        .filter-actions,
        .export-actions,
        .row-actions {
          justify-content: stretch;
          justify-items: stretch;
        }
      }
    `
  ]
})
export class DocumentsWorkQueuePageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly documentCatalogService = inject(DocumentCatalogService);

  protected readonly filtersForm = this.formBuilder.nonNullable.group({
    moduleCode: [''],
    workItemType: [''],
    severityCode: [''],
    take: [50]
  });
  protected readonly items = signal<DocumentWorkQueueItem[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly isExporting = signal(false);
  protected readonly pageError = signal<string | null>(null);
  protected readonly totalCount = signal<number | null>(null);
  protected readonly resultCount = signal<number | null>(null);

  constructor() {
    void this.reload();
  }

  protected async reload(): Promise<void> {
    this.isLoading.set(true);
    this.pageError.set(null);

    try {
      const response = await firstValueFrom(this.documentCatalogService.listWorkQueue(this.buildWorkQueueFilters()));

      this.items.set(response.items);
      this.totalCount.set(response.totalCount);
      this.resultCount.set(response.returnedCount);
    } catch (error) {
      this.items.set([]);
      this.totalCount.set(null);
      this.resultCount.set(null);
      this.pageError.set(getApiErrorMessage(error, 'No se pudo cargar la bandeja documental.'));
    } finally {
      this.isLoading.set(false);
    }
  }

  protected resetFilters(): void {
    this.filtersForm.reset({
      moduleCode: '',
      workItemType: '',
      severityCode: '',
      take: 50
    });
    void this.reload();
  }

  protected async exportWorkQueue(): Promise<void> {
    this.isExporting.set(true);
    this.pageError.set(null);

    try {
      await this.documentCatalogService.exportWorkQueue(this.buildWorkQueueFilters());
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No se pudo exportar la bandeja documental.'));
    } finally {
      this.isExporting.set(false);
    }
  }

  protected labelForType(workItemType: string): string {
    switch (workItemType) {
      case 'COMPLETENESS_PENDING':
        return 'Evidencia pendiente';
      case 'DOCUMENT_INTEGRITY_ISSUE':
        return 'Integridad documental';
      case 'RETENTION_REVIEW':
        return 'Revision de retencion';
      default:
        return workItemType;
    }
  }

  protected severityLabel(severityCode: string): string {
    switch (severityCode) {
      case 'HIGH':
        return 'Alta';
      case 'MEDIUM':
        return 'Media';
      case 'LOW':
        return 'Baja';
      default:
        return severityCode;
    }
  }

  protected operationalStatusLabel(statusCode: string): string {
    switch (statusCode) {
      case 'ACTIVE_OK':
        return 'Disponible';
      case 'INTEGRITY_ISSUE':
        return 'Revisar integridad';
      case 'ON_HOLD':
        return 'En resguardo';
      case 'REVIEW_DUE':
        return 'Requiere revision';
      case 'RETENTION_EXPIRED':
        return 'Retencion vencida';
      case 'ARCHIVED':
        return 'Archivado';
      case 'SUPERSEDED':
        return 'Reemplazado';
      default:
        return statusCode;
    }
  }

  protected reasonLabel(reasonCode: string): string {
    switch (reasonCode) {
      case 'MISSING_REQUIRED_DOCUMENT':
      case 'MISSING_EVIDENCE':
        return 'Falta evidencia';
      case 'INTEGRITY_ISSUE':
        return 'Revisar archivo';
      case 'RETENTION_REVIEW':
      case 'REVIEW_DUE':
        return 'Revision pendiente';
      default:
        return reasonCode;
    }
  }

  protected currentStatusLabel(statusCode: string): string {
    switch (statusCode) {
      case 'COMPLETE':
        return 'Completo';
      case 'INCOMPLETE':
        return 'Pendiente';
      case 'ACTIVE':
        return 'Vigente';
      case 'ARCHIVED':
        return 'Archivado';
      default:
        return statusCode;
    }
  }

  protected resolveActionRoute(item: DocumentWorkQueueItem): string {
    if (item.workItemType === 'RETENTION_REVIEW') {
      return '/documents/review';
    }

    return item.routeHint || '/documents';
  }

  private buildWorkQueueFilters(): DocumentWorkQueueFilters {
    const filters = this.filtersForm.getRawValue();
    return {
      moduleCode: filters.moduleCode,
      workItemType: filters.workItemType,
      severityCode: filters.severityCode,
      take: filters.take
    };
  }
}
