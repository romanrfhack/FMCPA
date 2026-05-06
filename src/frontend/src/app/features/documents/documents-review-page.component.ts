import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import {
  DocumentCatalogDetail,
  DocumentCatalogItem
} from '../../core/models/document-catalog.models';
import { DocumentCatalogService } from '../../core/services/document-catalog.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  selector: 'app-documents-review-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DecimalPipe, ReactiveFormsModule],
  template: `
    <section class="page-shell">
      <header class="page-header">
        <p class="page-kicker">TRACK 3 DOCUMENTOS</p>
        <h2>Revision de retencion</h2>
      </header>

      @if (pageError()) {
        <p class="alert error">{{ pageError() }}</p>
      }

      @if (pageSuccess()) {
        <p class="alert success">{{ pageSuccess() }}</p>
      }

      <form class="filters-panel" [formGroup]="filtersForm" (ngSubmit)="reload()">
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
          <span>Estado revision</span>
          <select formControlName="retentionReviewStatusCode">
            <option value="">Pendientes vencidos</option>
            <option value="REVIEW_PENDING">REVIEW_PENDING</option>
            <option value="REVIEW_DEFERRED">REVIEW_DEFERRED</option>
            <option value="REVIEW_COMPLETED">REVIEW_COMPLETED</option>
          </select>
        </label>

        <label>
          <span>Limite</span>
          <input type="number" min="1" max="200" formControlName="take" />
        </label>

        <div class="filter-actions">
          <button type="submit" [disabled]="isLoading()">Buscar</button>
          <button type="button" class="ghost" (click)="resetFilters()">Limpiar</button>
        </div>
      </form>

      <div class="content-grid">
        <article class="panel">
          <div class="panel-header">
            <h3>Documentos por revisar</h3>
            @if (resultCount() !== null) {
              <span>{{ resultCount() }} de {{ totalCount() }}</span>
            }
          </div>

          @if (isLoading()) {
            <p class="empty-state">Cargando bandeja...</p>
          } @else if (documents().length === 0) {
            <p class="empty-state">No hay documentos con los filtros actuales.</p>
          } @else {
            <div class="document-list">
              @for (document of documents(); track document.id) {
                <article class="document-row" [class.selected]="selectedDocument()?.id === document.id">
                  <button type="button" class="row-main" (click)="selectDocument(document)">
                    <span class="badge">{{ document.moduleName }}</span>
                    <strong>{{ document.originalFileName }}</strong>
                    <small>{{ document.retentionPolicyCode }} · {{ document.retentionUntilUtc | date: 'yyyy-MM-dd':'UTC' }}</small>
                  </button>
                  <div class="row-meta">
                    <span class="retention" [class.expired]="document.retentionStatusCode === 'EXPIRED_RETENTION'" [class.review]="document.retentionStatusCode === 'REVIEW_DUE'">
                      {{ document.retentionStatusCode }}
                    </span>
                    <span>{{ document.retentionReviewStatusCode }}</span>
                    @if (document.nextRetentionReviewUtc) {
                      <span>Prox. {{ document.nextRetentionReviewUtc | date: 'yyyy-MM-dd':'UTC' }}</span>
                    }
                    <span>{{ document.sizeBytes | number }} bytes</span>
                  </div>
                  <button type="button" class="ghost compact" (click)="download(document)">
                    Descargar
                  </button>
                </article>
              }
            </div>
          }
        </article>

        <aside class="panel detail-panel">
          <div class="panel-header">
            <h3>Operacion</h3>
          </div>

          @if (selectedDocument(); as document) {
            <dl>
              <div>
                <dt>Documento</dt>
                <dd>{{ document.originalFileName }}</dd>
              </div>
              <div>
                <dt>Modulo</dt>
                <dd>{{ document.moduleCode }}</dd>
              </div>
              <div>
                <dt>Retencion</dt>
                <dd>{{ document.retentionStatusCode }} · {{ document.retentionUntilUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</dd>
              </div>
              <div>
                <dt>Revision</dt>
                <dd>{{ document.retentionReviewStatusCode }}</dd>
              </div>
              @if (document.lastRetentionReviewUtc) {
                <div>
                  <dt>Ultima revision UTC</dt>
                  <dd>{{ document.lastRetentionReviewUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</dd>
                </div>
              }
              @if (document.nextRetentionReviewUtc) {
                <div>
                  <dt>Proxima revision UTC</dt>
                  <dd>{{ document.nextRetentionReviewUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</dd>
                </div>
              }
              @if (document.retentionReviewNotes) {
                <div>
                  <dt>Nota actual</dt>
                  <dd>{{ document.retentionReviewNotes }}</dd>
                </div>
              }
            </dl>

            <form class="review-form" [formGroup]="reviewForm">
              <label>
                <span>Nota operativa</span>
                <textarea formControlName="retentionReviewNotes" maxlength="500"></textarea>
              </label>

              <label>
                <span>Diferir hasta</span>
                <input type="datetime-local" formControlName="nextRetentionReviewLocal" />
              </label>

              <button type="button" [disabled]="isMutating()" (click)="markReviewed(document)">
                Marcar revisado
              </button>
              <button type="button" class="ghost" [disabled]="isMutating()" (click)="deferReview(document)">
                Diferir revision
              </button>
              <button type="button" class="ghost" (click)="download(document)">
                Descargar
              </button>
            </form>
          } @else {
            <p class="empty-state">Selecciona un documento.</p>
          }
        </aside>
      </div>
    </section>
  `,
  styles: [
    `
      .page-shell,
      .document-list,
      dl,
      .review-form {
        display: grid;
        gap: 1rem;
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
      p,
      dl,
      dd {
        margin: 0;
      }

      .page-kicker {
        margin-bottom: 0.4rem;
        font-size: 0.78rem;
        font-weight: 800;
        letter-spacing: 0.08em;
        color: #0f766e;
      }

      .filters-panel {
        display: grid;
        grid-template-columns: repeat(4, minmax(0, 1fr));
        gap: 0.85rem;
        padding: 1rem;
        align-items: end;
      }

      label {
        display: grid;
        gap: 0.35rem;
        font-weight: 700;
      }

      input,
      textarea,
      select {
        width: 100%;
        border: 1px solid rgba(35, 51, 47, 0.16);
        border-radius: 8px;
        padding: 0.7rem 0.8rem;
        font: inherit;
        background: #fff;
      }

      textarea {
        min-height: 5rem;
        resize: vertical;
      }

      button {
        border: 0;
        border-radius: 8px;
        padding: 0.75rem 1rem;
        font-weight: 800;
        color: #fff;
        background: #0f766e;
        cursor: pointer;
      }

      button:disabled {
        opacity: 0.55;
        cursor: not-allowed;
      }

      button.ghost {
        color: #0f766e;
        background: rgba(15, 118, 110, 0.1);
      }

      button.compact {
        padding: 0.55rem 0.8rem;
      }

      .filter-actions {
        display: flex;
        gap: 0.5rem;
      }

      .content-grid {
        display: grid;
        grid-template-columns: minmax(0, 1fr) minmax(280px, 0.36fr);
        gap: 1rem;
        align-items: start;
      }

      .panel-header {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        align-items: center;
        margin-bottom: 1rem;
      }

      .panel-header span,
      .empty-state,
      small,
      dt {
        color: #60716d;
      }

      .document-row {
        display: grid;
        grid-template-columns: minmax(0, 1fr) auto auto;
        gap: 0.9rem;
        align-items: center;
        padding: 1rem;
        border: 1px solid rgba(35, 51, 47, 0.1);
        border-radius: 8px;
        background: #fff;
      }

      .document-row.selected {
        border-color: rgba(15, 118, 110, 0.45);
      }

      .row-main {
        display: grid;
        gap: 0.35rem;
        min-width: 0;
        padding: 0;
        color: inherit;
        text-align: left;
        background: transparent;
      }

      .row-main strong,
      .row-main small,
      dd {
        overflow-wrap: anywhere;
      }

      .row-meta {
        display: grid;
        gap: 0.25rem;
        font-size: 0.85rem;
        color: #60716d;
      }

      .badge,
      .row-meta span {
        width: fit-content;
        border-radius: 999px;
        padding: 0.2rem 0.5rem;
        font-size: 0.76rem;
        font-weight: 800;
        color: #0f766e;
        background: rgba(15, 118, 110, 0.1);
      }

      .row-meta span.retention {
        color: #365314;
        background: rgba(77, 124, 15, 0.1);
      }

      .row-meta span.retention.review {
        color: #92400e;
        background: rgba(146, 64, 14, 0.1);
      }

      .row-meta span.retention.expired {
        color: #9f1239;
        background: rgba(159, 18, 57, 0.08);
      }

      dl div {
        display: grid;
        gap: 0.25rem;
      }

      dt {
        font-size: 0.78rem;
        font-weight: 800;
        text-transform: uppercase;
      }

      .review-form {
        margin-top: 1rem;
      }

      .review-form button {
        width: 100%;
      }

      .alert {
        padding: 0.8rem 1rem;
        border-radius: 8px;
        font-weight: 700;
      }

      .alert.error {
        color: #9f1239;
        background: rgba(159, 18, 57, 0.08);
      }

      .alert.success {
        color: #166534;
        background: rgba(22, 101, 52, 0.08);
      }

      @media (max-width: 980px) {
        .filters-panel,
        .content-grid,
        .document-row {
          grid-template-columns: 1fr;
        }

        .filter-actions {
          flex-wrap: wrap;
        }
      }
    `
  ]
})
export class DocumentsReviewPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly documentCatalogService = inject(DocumentCatalogService);

  protected readonly filtersForm = this.formBuilder.nonNullable.group({
    moduleCode: [''],
    retentionReviewStatusCode: [''],
    take: [50]
  });
  protected readonly reviewForm = this.formBuilder.nonNullable.group({
    retentionReviewNotes: [''],
    nextRetentionReviewLocal: ['']
  });
  protected readonly documents = signal<DocumentCatalogItem[]>([]);
  protected readonly selectedDocument = signal<DocumentCatalogItem | DocumentCatalogDetail | null>(null);
  protected readonly pageError = signal<string | null>(null);
  protected readonly pageSuccess = signal<string | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly isMutating = signal(false);
  protected readonly totalCount = signal<number | null>(null);
  protected readonly resultCount = signal<number | null>(null);

  constructor() {
    void this.reload();
  }

  protected async reload(): Promise<void> {
    this.isLoading.set(true);
    this.pageError.set(null);
    this.pageSuccess.set(null);

    try {
      const filters = this.filtersForm.getRawValue();
      const response = await firstValueFrom(this.documentCatalogService.listRetentionReviewQueue({
        moduleCode: filters.moduleCode,
        retentionReviewStatusCode: filters.retentionReviewStatusCode,
        take: filters.take
      }));
      this.documents.set(response.items);
      this.totalCount.set(response.totalCount);
      this.resultCount.set(response.returnedCount);
      this.selectedDocument.set(response.items[0] ?? null);
      this.syncReviewForm(response.items[0] ?? null);
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No se pudo cargar la bandeja de retencion.'));
    } finally {
      this.isLoading.set(false);
    }
  }

  protected resetFilters(): void {
    this.filtersForm.reset({
      moduleCode: '',
      retentionReviewStatusCode: '',
      take: 50
    });
    void this.reload();
  }

  protected async selectDocument(document: DocumentCatalogItem): Promise<void> {
    this.pageError.set(null);
    this.pageSuccess.set(null);

    try {
      const detail = await firstValueFrom(this.documentCatalogService.getDocument(document.id));
      this.selectedDocument.set(detail);
      this.syncReviewForm(detail);
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No se pudo cargar el detalle documental.'));
    }
  }

  protected async download(document: DocumentCatalogItem): Promise<void> {
    this.pageError.set(null);

    try {
      await this.documentCatalogService.downloadDocument(document);
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No se pudo descargar el documento.'));
    }
  }

  protected async markReviewed(document: DocumentCatalogItem): Promise<void> {
    this.isMutating.set(true);
    this.pageError.set(null);
    this.pageSuccess.set(null);

    try {
      const formValue = this.reviewForm.getRawValue();
      const detail = await firstValueFrom(this.documentCatalogService.updateRetentionReview(
        document.id,
        {
          retentionReviewStatusCode: 'REVIEW_COMPLETED',
          nextRetentionReviewUtc: null,
          retentionReviewNotes: formValue.retentionReviewNotes.trim() || null
        }));
      await this.reload();
      this.selectedDocument.set(detail);
      this.syncReviewForm(detail);
      this.pageSuccess.set('Revision de retencion completada.');
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No se pudo marcar la revision como completada.'));
    } finally {
      this.isMutating.set(false);
    }
  }

  protected async deferReview(document: DocumentCatalogItem): Promise<void> {
    const formValue = this.reviewForm.getRawValue();
    const nextReviewUtc = this.toUtcIsoString(formValue.nextRetentionReviewLocal);
    if (!nextReviewUtc) {
      this.pageError.set('La fecha para diferir revision es requerida.');
      return;
    }

    this.isMutating.set(true);
    this.pageError.set(null);
    this.pageSuccess.set(null);

    try {
      const detail = await firstValueFrom(this.documentCatalogService.updateRetentionReview(
        document.id,
        {
          retentionReviewStatusCode: 'REVIEW_DEFERRED',
          nextRetentionReviewUtc: nextReviewUtc,
          retentionReviewNotes: formValue.retentionReviewNotes.trim() || null
        }));
      await this.reload();
      this.selectedDocument.set(detail);
      this.syncReviewForm(detail);
      this.pageSuccess.set('Revision de retencion diferida.');
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No se pudo diferir la revision de retencion.'));
    } finally {
      this.isMutating.set(false);
    }
  }

  private syncReviewForm(document: DocumentCatalogItem | DocumentCatalogDetail | null): void {
    this.reviewForm.reset({
      retentionReviewNotes: document?.retentionReviewNotes ?? '',
      nextRetentionReviewLocal: this.toLocalDateTimeInput(document?.nextRetentionReviewUtc ?? null)
    });
  }

  private toUtcIsoString(value: string): string | null {
    if (!value.trim()) {
      return null;
    }

    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? null : date.toISOString();
  }

  private toLocalDateTimeInput(value: string | null): string {
    if (!value) {
      return '';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    const offsetDate = new Date(date.getTime() - date.getTimezoneOffset() * 60_000);
    return offsetDate.toISOString().slice(0, 16);
  }
}
