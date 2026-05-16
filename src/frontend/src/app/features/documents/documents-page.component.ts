import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import {
  DocumentCatalogDetail,
  DocumentCatalogFilters,
  DocumentCatalogItem,
  DocumentCompleteness,
  DocumentSummary,
  DocumentTimelineEvent
} from '../../core/models/document-catalog.models';
import { DocumentCatalogService } from '../../core/services/document-catalog.service';
import { AuthService } from '../../core/services/auth.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';
import { DocumentsCatalogTableComponent } from './documents-catalog-table.component';
import { DocumentsKpiStripComponent } from './documents-kpi-strip.component';
import { DocumentsPendingTableComponent } from './documents-pending-table.component';
import { DocumentsSummaryPanelComponent } from './documents-summary-panel.component';

@Component({
  selector: 'app-documents-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    DocumentsCatalogTableComponent,
    DocumentsKpiStripComponent,
    DocumentsPendingTableComponent,
    DocumentsSummaryPanelComponent
  ],
  template: `
    <section class="page-shell">
      <header class="page-header compact-header">
        <div>
          <p class="page-kicker">Control documental</p>
          <h2>Documentos</h2>
        </div>
        <div class="header-actions">
          <a class="origin-link" href="/documents/work-queue">Cola</a>
          <a class="origin-link" href="/documents/review">Revision</a>
          <button type="button" class="ghost compact" [disabled]="isSummaryLoading()" (click)="reloadSummary()">
            Actualizar KPIs
          </button>
        </div>
      </header>

      @if (pageError()) {
        <p class="alert error">{{ pageError() }}</p>
      }

      @if (pageSuccess()) {
        <p class="alert success">{{ pageSuccess() }}</p>
      }

      <article class="panel kpi-panel">
        <app-documents-kpi-strip [summary]="summary()" [isLoading]="isSummaryLoading()" />
      </article>

      <nav class="tabs" aria-label="Secciones documentales">
        <button type="button" [class.active]="activeTab() === 'catalog'" (click)="activeTab.set('catalog')">
          Catalogo
        </button>
        <button type="button" [class.active]="activeTab() === 'pending'" (click)="activeTab.set('pending')">
          Pendientes
        </button>
        <button type="button" [class.active]="activeTab() === 'summary'" (click)="activeTab.set('summary')">
          Resumen
        </button>
      </nav>

      @if (activeTab() === 'catalog') {
        <form class="filters-panel" [formGroup]="filtersForm" (ngSubmit)="reload()" aria-label="Filtros documentales">
          <div class="basic-filters">
            <label>
              <span>Modulo</span>
              <select formControlName="moduleCode">
                <option value="">Todos permitidos</option>
                <option value="MARKETS">Mercados</option>
                <option value="DONATARIAS">Donatarias</option>
                <option value="FEDERATION">Federacion</option>
              </select>
            </label>

            <label>
              <span>Estado</span>
              <select formControlName="statusCode">
                <option value="ACTIVE">Vigentes</option>
                <option value="ARCHIVED">Archivados</option>
                <option value="ALL">Todos</option>
              </select>
            </label>

            <label>
              <span>Estado operativo</span>
              <select formControlName="documentOperationalStatusCode">
                <option value="">Todos</option>
                <option value="ACTIVE_OK">Disponible</option>
                <option value="INTEGRITY_ISSUE">Revisar integridad</option>
                <option value="ON_HOLD">En resguardo</option>
                <option value="REVIEW_DUE">Requiere revision</option>
                <option value="RETENTION_EXPIRED">Retencion vencida</option>
                <option value="ARCHIVED">Archivado</option>
                <option value="SUPERSEDED">Reemplazado</option>
              </select>
            </label>

            <label>
              <span>Limite</span>
              <input type="number" min="1" max="200" formControlName="take" />
            </label>

            <div class="filter-actions">
              <button type="submit" [disabled]="isLoading()">Buscar</button>
              <button type="button" class="ghost" (click)="resetFilters()">Limpiar</button>
              <button type="button" class="ghost compact" [disabled]="isExporting()" (click)="exportCatalog()">Exportar</button>
            </div>
          </div>

          <details class="advanced-filters">
            <summary>Filtros avanzados</summary>
            <div class="advanced-grid">
              <label>
                <span>Area documental</span>
                <select formControlName="documentAreaCode">
                  <option value="">Todas</option>
                  <option value="MARKETS_TENANT_CERTIFICATES">Cedulas de mercados</option>
                  <option value="DONATIONS_APPLICATION_EVIDENCES">Evidencias donatarias</option>
                  <option value="FEDERATION_APPLICATION_EVIDENCES">Evidencias federacion</option>
                </select>
              </label>

              <label>
                <span>Integridad</span>
                <select formControlName="integrityState">
                  <option value="">Todos</option>
                  <option value="VALID">Correcta</option>
                  <option value="MISSING_FILE">Archivo no localizado</option>
                  <option value="SIZE_MISMATCH">Tamano distinto</option>
                  <option value="INVALID_PATH">Ruta no valida</option>
                </select>
              </label>

              <label>
                <span>Clase</span>
                <select formControlName="documentClassCode">
                  <option value="">Todas</option>
                  <option value="CERTIFICATE">Cedula o certificado</option>
                  <option value="SIGNED_DOCUMENT">Documento firmado</option>
                  <option value="SUPPORTING_DOCUMENT">Soporte documental</option>
                  <option value="PHOTO_EVIDENCE">Evidencia fotografica</option>
                  <option value="VIDEO_EVIDENCE">Evidencia en video</option>
                  <option value="OTHER">Otro</option>
                </select>
              </label>

              <label>
                <span>Politica de retencion</span>
                <select formControlName="retentionPolicyCode">
                  <option value="">Todas</option>
                  <option value="CERTIFICATE_REVIEW">Revision de cedulas</option>
                  <option value="SIGNED_LONG_TERM">Resguardo largo</option>
                  <option value="EVIDENCE_MEDIUM_TERM">Evidencia operativa</option>
                  <option value="GENERIC_REVIEW">Revision general</option>
                </select>
              </label>

              <label>
                <span>Estado de retencion</span>
                <select formControlName="retentionStatusCode">
                  <option value="">Todos</option>
                  <option value="ACTIVE_RETENTION">Dentro de periodo</option>
                  <option value="REVIEW_DUE">Por revisar</option>
                  <option value="EXPIRED_RETENTION">Vencida</option>
                </select>
              </label>

              <label>
                <span>Tipo de origen</span>
                <input type="text" formControlName="entityType" placeholder="Tipo de origen" />
              </label>

              <label>
                <span>ID de origen</span>
                <input type="text" formControlName="entityId" placeholder="Identificador" />
              </label>
            </div>
          </details>
        </form>

        <div class="content-grid">
        <app-documents-catalog-table
          [documents]="documents()"
          [isLoading]="isLoading()"
          [resultCount]="resultCount()"
          [totalCount]="totalCount()"
          [selectedDocumentId]="selectedDocument()?.id ?? null"
          (selectDocument)="selectDocument($event)"
          (downloadDocument)="download($event)" />

        @if (isDetailDrawerOpen()) {
          <button type="button" class="drawer-backdrop" aria-label="Cerrar detalle" (click)="closeDetailDrawer()"></button>
        }

        <aside class="panel detail-panel" [class.open]="isDetailDrawerOpen()">
          <div class="panel-header">
            <h3>Detalle</h3>
            <button type="button" class="ghost compact close-drawer" (click)="closeDetailDrawer()">
              Cerrar
            </button>
          </div>

          @if (selectedDocument(); as document) {
            <section class="detail-summary">
              <span class="badge">{{ document.moduleName }}</span>
              <h4>{{ document.originalFileName }}</h4>
              <p>{{ document.originContext.displayName }}</p>
              <div class="detail-badges">
                <span class="operational">{{ operationalStatusLabel(document.documentOperationalStatusCode) }}</span>
                <span>{{ documentClassLabel(document.documentClassCode) }}</span>
                <span>{{ integrityLabel(document.integrityState) }}</span>
                <span>{{ retentionStatusLabel(document.retentionStatusCode) }}</span>
              </div>
            </section>

            <dl class="detail-list priority-detail">
              <div>
                <dt>Modulo</dt>
                <dd>{{ document.moduleName }}</dd>
              </div>
              <div>
                <dt>Area</dt>
                <dd>{{ documentAreaLabel(document.documentAreaCode) }}</dd>
              </div>
              <div>
                <dt>Origen</dt>
                <dd>{{ document.originContext.displayName }}</dd>
              </div>
              <div>
                <dt>Tipo de origen</dt>
                <dd>{{ entityTypeLabel(document.originContext.entityType) }}</dd>
              </div>
              @if (document.originContext.summary) {
                <div>
                  <dt>Resumen origen</dt>
                  <dd>{{ document.originContext.summary }}</dd>
                </div>
              }
              @if (document.originContext.routeHint) {
                <div>
                  <dt>Origen</dt>
                  <dd><a class="origin-link" [href]="document.originContext.routeHint">Ir al origen</a></dd>
                </div>
              }
              <div>
                <dt>Clase documental</dt>
                <dd>{{ documentClassLabel(document.documentClassCode) }}</dd>
              </div>
              <div>
                <dt>Estado operativo</dt>
                <dd>{{ operationalStatusLabel(document.documentOperationalStatusCode) }} · {{ severityLabel(document.documentOperationalSeverityCode) }}</dd>
              </div>
              <div>
                <dt>Integridad</dt>
                <dd>{{ integrityLabel(document.integrityState) }}</dd>
              </div>
              <div>
                <dt>Retencion</dt>
                <dd>{{ retentionStatusLabel(document.retentionStatusCode) }}</dd>
              </div>
              <div>
                <dt>Vigente hasta</dt>
                <dd>{{ document.retentionUntilUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</dd>
              </div>
              <div>
                <dt>Resguardo administrativo</dt>
                <dd>{{ document.isAdministrativeHold ? 'Activo' : 'Inactivo' }}</dd>
              </div>
            </dl>

            <details class="technical-details">
              <summary>Detalles tecnicos</summary>
              <dl class="detail-list">
                <div>
                  <dt>Codigo de modulo</dt>
                  <dd>{{ document.moduleCode }}</dd>
                </div>
                <div>
                  <dt>ID de origen</dt>
                  <dd>{{ document.entityId }}</dd>
                </div>
                <div>
                  <dt>Tipo de origen</dt>
                  <dd>{{ document.entityType }}</dd>
                </div>
                <div>
                  <dt>Content type</dt>
                  <dd>{{ document.contentType }}</dd>
                </div>
                <div>
                  <dt>Politica efectiva</dt>
                  <dd>{{ retentionPolicyLabel(document.retentionPolicyCode) }}</dd>
                </div>
                <div>
                  <dt>Revision de retencion</dt>
                  <dd>{{ retentionReviewStatusLabel(document.retentionReviewStatusCode) }}</dd>
                </div>
              @if (document.isAdministrativeHold) {
                <div>
                  <dt>Motivo de resguardo</dt>
                  <dd>{{ document.holdReason || 'Sin motivo registrado' }}</dd>
                </div>
                <div>
                  <dt>Resguardo desde UTC</dt>
                  <dd>{{ document.holdPlacedUtc ? (document.holdPlacedUtc | date: 'yyyy-MM-dd HH:mm':'UTC') : 'Sin fecha' }}</dd>
                </div>
                <div>
                  <dt>Colocado por</dt>
                  <dd>{{ document.holdPlacedBy || 'No registrado' }}</dd>
                </div>
              } @else if (document.holdReleasedUtc) {
                <div>
                  <dt>Resguardo liberado UTC</dt>
                  <dd>{{ document.holdReleasedUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</dd>
                </div>
              }
              <div>
                <dt>Baseline retencion</dt>
                <dd>{{ retentionPolicyLabel(document.retentionBaselinePolicyCode) }} · {{ document.retentionBaselineUntilUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</dd>
              </div>
              <div>
                <dt>Effective retencion</dt>
                <dd>{{ retentionPolicyLabel(document.retentionEffectivePolicyCode) }} · {{ document.retentionEffectiveUntilUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</dd>
              </div>
              @if (document.hasRetentionOverride) {
                <div>
                  <dt>Override retencion</dt>
                  <dd>
                    {{ retentionPolicyLabel(document.retentionOverridePolicyCode || 'Politica baseline') }}
                    · {{ document.retentionOverrideUntilUtc ? (document.retentionOverrideUntilUtc | date: 'yyyy-MM-dd HH:mm':'UTC') : 'Fecha calculada' }}
                  </dd>
                </div>
                <div>
                  <dt>Motivo override</dt>
                  <dd>{{ document.retentionOverrideReason || 'Sin motivo registrado' }}</dd>
                </div>
              }
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
                  <dt>Notas de revision</dt>
                  <dd>{{ document.retentionReviewNotes }}</dd>
                </div>
              }
              <div>
                <dt>Principal</dt>
                <dd>{{ document.isPrimaryDocument ? 'Si' : 'No' }}</dd>
              </div>
              <div>
                <dt>Proposito</dt>
                <dd>{{ document.businessPurpose || 'No clasificado' }}</dd>
              </div>
              @if (document.classificationNotes) {
                <div>
                  <dt>Notas de clasificacion</dt>
                  <dd>{{ document.classificationNotes }}</dd>
                </div>
              }
              <div>
                <dt>Tamano</dt>
                <dd>{{ document.sizeBytes | number }} bytes</dd>
              </div>
              <div>
                <dt>Estado</dt>
                <dd>{{ documentStatusLabel(document.statusCode) }}</dd>
              </div>
              @if (document.statusCode === 'ARCHIVED') {
                <div>
                  <dt>Archivado UTC</dt>
                  <dd>{{ document.archivedUtc ? (document.archivedUtc | date: 'yyyy-MM-dd HH:mm':'UTC') : 'Sin fecha' }}</dd>
                </div>
                <div>
                  <dt>Motivo</dt>
                  <dd>{{ document.archiveReason || 'Sin motivo registrado' }}</dd>
                </div>
              }
              <div>
                <dt>Reemplazo</dt>
                <dd>
                  @if (document.isSuperseded) {
                    Reemplazado por {{ document.supersededByDocumentId }}
                  } @else if (document.replacedDocumentId) {
                    Vigente; reemplaza {{ document.replacedDocumentId }}
                  } @else {
                    Sin reemplazo registrado
                  }
                </dd>
              </div>
              <div>
                <dt>Grupo reemplazo</dt>
                <dd>{{ document.replacementGroupKey }}</dd>
              </div>
              <div>
                <dt>Integridad</dt>
                <dd>{{ integrityLabel(document.integrityState) }}</dd>
              </div>
              <div>
                <dt>Checksum</dt>
                <dd>{{ document.hasChecksum ? 'Disponible' : 'No disponible' }}</dd>
              </div>
              @if (isDetail(document) && document.sha256Hex) {
                <div>
                  <dt>SHA-256</dt>
                  <dd class="hash">{{ document.sha256Hex }}</dd>
                </div>
              }
              </dl>
            </details>

            <section class="timeline-section">
              <div class="timeline-header">
                <h4>Historia documental</h4>
                <button type="button" class="ghost compact" [disabled]="isTimelineLoading()" (click)="loadDocumentTimeline(document.id)">
                  Actualizar
                </button>
              </div>
              @if (isTimelineLoading()) {
                <p class="empty-state">Cargando historia documental...</p>
              } @else if (selectedTimeline().length === 0) {
                <p class="empty-state">Sin eventos documentales registrados.</p>
              } @else {
                <ol class="timeline-list">
                  @for (event of selectedTimeline(); track event.occurredUtc + event.eventType + event.documentId) {
                    <li>
                      <time>{{ event.occurredUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</time>
                      <strong>{{ event.title }}</strong>
                      <span>{{ event.eventType }}</span>
                      <p>{{ event.detail }}</p>
                      @if (event.relatedDocumentId) {
                        <small>
                          Relacionado: {{ event.relatedDocumentOriginalFileName || event.relatedDocumentId }}
                        </small>
                      }
                    </li>
                  }
                </ol>
              }
            </section>

            <button type="button" (click)="download(document)">Descargar</button>

            @if (canManageDocuments()) {
              <div class="document-actions">
                <form class="metadata-form" [formGroup]="metadataForm" (ngSubmit)="saveMetadata(document)">
                  <label>
                    <span>Clase</span>
                    <select formControlName="documentClassCode">
                      <option value="CERTIFICATE">CERTIFICATE</option>
                      <option value="SIGNED_DOCUMENT">SIGNED_DOCUMENT</option>
                      <option value="SUPPORTING_DOCUMENT">SUPPORTING_DOCUMENT</option>
                      <option value="PHOTO_EVIDENCE">PHOTO_EVIDENCE</option>
                      <option value="VIDEO_EVIDENCE">VIDEO_EVIDENCE</option>
                      <option value="OTHER">OTHER</option>
                    </select>
                  </label>

                  <label>
                    <span>Proposito</span>
                    <textarea formControlName="businessPurpose" maxlength="500"></textarea>
                  </label>

                  <label class="checkbox-label">
                    <input type="checkbox" formControlName="isPrimaryDocument" />
                    <span>Documento principal</span>
                  </label>

                  <label>
                    <span>Notas</span>
                    <textarea formControlName="classificationNotes" maxlength="500"></textarea>
                  </label>

                  <button type="submit" class="ghost" [disabled]="isMutating()">
                    Guardar datos
                  </button>
                </form>

                <form class="metadata-form" [formGroup]="retentionOverrideForm" (ngSubmit)="setRetentionOverride(document)">
                  <label>
                    <span>Politica override</span>
                    <select formControlName="retentionOverridePolicyCode">
                      <option value="">Mantener politica baseline</option>
                      <option value="CERTIFICATE_REVIEW">CERTIFICATE_REVIEW</option>
                      <option value="SIGNED_LONG_TERM">SIGNED_LONG_TERM</option>
                      <option value="EVIDENCE_MEDIUM_TERM">EVIDENCE_MEDIUM_TERM</option>
                      <option value="GENERIC_REVIEW">GENERIC_REVIEW</option>
                    </select>
                  </label>

                  <label>
                    <span>Retener hasta override UTC</span>
                    <input type="datetime-local" formControlName="retentionOverrideUntilUtc" />
                  </label>

                  <label>
                    <span>Motivo override</span>
                    <textarea formControlName="retentionOverrideReason" maxlength="500"></textarea>
                  </label>

                  <button type="submit" class="ghost" [disabled]="isMutating()">
                    Guardar ajuste de retencion
                  </button>

                  @if (document.hasRetentionOverride) {
                    <button type="button" class="ghost" [disabled]="isMutating()" (click)="clearRetentionOverride(document)">
                      Limpiar ajuste
                    </button>
                  }
                </form>

                <form class="metadata-form" [formGroup]="holdForm" (ngSubmit)="setAdministrativeHold(document)">
                  <label>
                    <span>Motivo de resguardo administrativo</span>
                    <textarea formControlName="reason" maxlength="500"></textarea>
                  </label>

                  <button type="submit" class="ghost" [disabled]="isMutating()">
                    Guardar resguardo
                  </button>

                  @if (document.isAdministrativeHold) {
                    <button type="button" class="ghost" [disabled]="isMutating()" (click)="clearAdministrativeHold(document)">
                      Liberar resguardo
                    </button>
                  }
                </form>

                @if (document.statusCode === 'ARCHIVED' && !document.isSuperseded) {
                  <button type="button" class="ghost" [disabled]="isMutating()" (click)="restore(document)">
                    Restaurar
                  </button>
                } @else if (document.isSuperseded) {
                  <p class="immutable-note">Documento reemplazado: se conserva para trazabilidad y descarga, pero no se restaura como vigente.</p>
                } @else {
                  <label>
                    <span>Motivo de archivado</span>
                    <input type="text" [formControl]="archiveReasonControl" maxlength="500" />
                  </label>
                  <button type="button" class="danger" [disabled]="isMutating()" (click)="archive(document)">
                    Archivar
                  </button>
                }
              </div>
            }
          } @else {
            <p class="empty-state">Selecciona un documento.</p>
          }
        </aside>
      </div>
      } @else if (activeTab() === 'pending') {
        <article class="panel pending-panel">
          <div class="panel-header">
            <div>
              <h3>Pendientes documentales</h3>
              @if (pendingResultCount() !== null) {
                <span>{{ pendingResultCount() }} de {{ pendingTotalCount() }}</span>
              }
            </div>
            <form class="pending-actions" [formGroup]="pendingFiltersForm" (ngSubmit)="reloadPending()">
              <select formControlName="moduleCode">
                <option value="">Todos permitidos</option>
                <option value="MARKETS">Mercados</option>
                <option value="DONATARIAS">Donatarias</option>
                <option value="FEDERATION">Federacion</option>
              </select>
              <button type="submit" class="ghost compact" [disabled]="isPendingLoading()">Actualizar</button>
            </form>
          </div>

          <app-documents-pending-table
            [pendingItems]="pendingItems()"
            [isLoading]="isPendingLoading()"
            [canRemediateItem]="canRemediatePending"
            (filterByEntity)="filterByPendingEntity($event)" />
        </article>
      } @else {
        <article class="panel summary-panel">
          <div class="panel-header">
            <div>
              <h3>Resumen ejecutivo documental</h3>
              @if (isSummaryLoading()) {
                <span>Actualizando...</span>
              }
            </div>
            <div class="summary-actions">
              <a class="origin-link" href="/documents/work-queue">Ver cola</a>
              <button type="button" class="ghost compact" [disabled]="isSummaryLoading()" (click)="reloadSummary()">
                Actualizar
              </button>
            </div>
          </div>

          <app-documents-summary-panel [summary]="summary()" />
        </article>
      }
    </section>
  `,
  styles: [
    `
      .page-shell,
      dl {
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

      .compact-header {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        align-items: center;
        padding: 0.85rem 1rem;
      }

      .compact-header h2 {
        font-size: 1.35rem;
        line-height: 1.1;
      }

      .header-actions {
        display: flex;
        gap: 0.55rem;
        flex-wrap: wrap;
        justify-content: flex-end;
        align-items: center;
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
        margin-bottom: 0.18rem;
        font-size: 0.78rem;
        font-weight: 800;
        letter-spacing: 0.08em;
        color: #0f766e;
      }

      .kpi-panel {
        padding: 0.9rem;
      }

      .tabs {
        display: flex;
        gap: 0.35rem;
        flex-wrap: wrap;
        border-bottom: 1px solid rgba(35, 51, 47, 0.12);
      }

      .tabs button {
        border-radius: 8px 8px 0 0;
        padding: 0.62rem 1rem;
        color: #334641;
        background: transparent;
      }

      .tabs button.active {
        color: #0f766e;
        background: rgba(15, 118, 110, 0.1);
        box-shadow: inset 0 -2px 0 #0f766e;
      }

      .filters-panel {
        display: grid;
        gap: 0.75rem;
        padding: 0.9rem;
      }

      .basic-filters,
      .advanced-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(10.5rem, 1fr));
        gap: 0.7rem;
        align-items: end;
      }

      .advanced-filters {
        border-top: 1px solid rgba(35, 51, 47, 0.08);
        padding-top: 0.7rem;
      }

      .advanced-filters summary {
        width: fit-content;
        color: #0f5f58;
        cursor: pointer;
        font-weight: 900;
      }

      .advanced-filters[open] .advanced-grid {
        margin-top: 0.75rem;
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
        min-width: 0;
        box-sizing: border-box;
        border: 1px solid rgba(35, 51, 47, 0.16);
        border-radius: 8px;
        padding: 0.58rem 0.7rem;
        font: inherit;
        background: #fff;
      }

      textarea {
        min-height: 4.75rem;
        resize: vertical;
      }

      button {
        min-width: 0;
        border: 0;
        border-radius: 8px;
        padding: 0.68rem 0.9rem;
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
        flex-wrap: wrap;
        justify-content: flex-end;
      }

      .content-grid {
        display: grid;
        grid-template-columns: minmax(0, 1fr);
        align-items: start;
      }

      .panel-header {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        align-items: center;
        margin-bottom: 1rem;
      }

      .pending-actions {
        display: flex;
        gap: 0.6rem;
        align-items: center;
      }

      .summary-actions {
        display: flex;
        gap: 0.65rem;
        flex-wrap: wrap;
        justify-content: flex-end;
      }

      .panel-header span,
      .empty-state,
      small,
      dt {
        color: #60716d;
      }

      dd {
        overflow-wrap: anywhere;
      }

      .badge {
        width: fit-content;
        border-radius: 999px;
        padding: 0.2rem 0.5rem;
        font-size: 0.76rem;
        font-weight: 800;
        color: #0f766e;
        background: rgba(15, 118, 110, 0.1);
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

      .hash {
        font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
        font-size: 0.82rem;
      }

      .origin-link {
        color: #0f766e;
        font-weight: 800;
      }

      .detail-summary {
        display: grid;
        gap: 0.45rem;
        margin-bottom: 1rem;
        padding-bottom: 1rem;
        border-bottom: 1px solid rgba(35, 51, 47, 0.1);
      }

      .drawer-backdrop {
        position: fixed;
        inset: 0;
        z-index: 30;
        border-radius: 0;
        background: rgba(18, 31, 28, 0.32);
      }

      .detail-panel {
        position: fixed;
        top: 0;
        right: 0;
        bottom: 0;
        z-index: 40;
        width: min(35rem, calc(100vw - 1.5rem));
        overflow-y: auto;
        border-radius: 8px 0 0 8px;
        transform: translateX(105%);
        transition: transform 160ms ease;
      }

      .detail-panel.open {
        transform: translateX(0);
      }

      .detail-panel .panel-header {
        position: sticky;
        top: 0;
        z-index: 1;
        margin: -1.25rem -1.25rem 1rem;
        padding: 0.85rem 1.25rem;
        border-bottom: 1px solid rgba(35, 51, 47, 0.1);
        background: rgba(255, 255, 255, 0.96);
      }

      .detail-summary h4 {
        margin: 0;
        color: #123f3b;
        overflow-wrap: anywhere;
      }

      .detail-badges {
        display: flex;
        flex-wrap: wrap;
        gap: 0.35rem;
      }

      .detail-badges span {
        width: fit-content;
        border-radius: 999px;
        padding: 0.22rem 0.55rem;
        font-size: 0.76rem;
        font-weight: 800;
        color: #0f766e;
        background: rgba(15, 118, 110, 0.1);
      }

      .priority-detail {
        grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr));
        gap: 0.75rem;
      }

      .technical-details {
        margin-top: 1rem;
        border-top: 1px solid rgba(35, 51, 47, 0.1);
        padding-top: 0.75rem;
      }

      .technical-details summary {
        color: #0f5f58;
        cursor: pointer;
        font-weight: 800;
      }

      .technical-details dl {
        margin-top: 0.75rem;
      }

      .detail-panel button {
        margin-top: 1rem;
        width: 100%;
      }

      .detail-panel .close-drawer {
        width: auto;
        margin-top: 0;
      }

      .document-actions {
        display: grid;
        gap: 0.75rem;
        margin-top: 1rem;
        padding-top: 1rem;
        border-top: 1px solid rgba(35, 51, 47, 0.1);
      }

      .metadata-form {
        display: grid;
        gap: 0.75rem;
      }

      .immutable-note {
        margin: 0;
        color: #7c2d12;
        font-size: 0.85rem;
        font-weight: 700;
      }

      .checkbox-label {
        grid-template-columns: auto minmax(0, 1fr);
        align-items: center;
      }

      .checkbox-label input {
        width: auto;
      }

      button.danger {
        background: #9f1239;
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
        .compact-header,
        .basic-filters,
        .advanced-grid,
        .pending-actions {
          grid-template-columns: 1fr;
          display: grid;
        }

        .filter-actions {
          justify-content: stretch;
        }

        .filter-actions button {
          flex: 1;
        }
      }
    `
  ]
})
export class DocumentsPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly documentCatalogService = inject(DocumentCatalogService);
  private readonly authService = inject(AuthService);

  protected readonly filtersForm = this.formBuilder.nonNullable.group({
    moduleCode: [''],
    documentAreaCode: [''],
    integrityState: [''],
    documentOperationalStatusCode: [''],
    documentClassCode: [''],
    retentionPolicyCode: [''],
    retentionStatusCode: [''],
    statusCode: ['ACTIVE'],
    entityType: [''],
    entityId: [''],
    take: [50]
  });
  protected readonly pendingFiltersForm = this.formBuilder.nonNullable.group({
    moduleCode: [''],
    take: [20]
  });
  protected readonly metadataForm = this.formBuilder.nonNullable.group({
    documentClassCode: ['OTHER'],
    businessPurpose: [''],
    isPrimaryDocument: [false],
    classificationNotes: ['']
  });
  protected readonly retentionOverrideForm = this.formBuilder.nonNullable.group({
    retentionOverridePolicyCode: [''],
    retentionOverrideUntilUtc: [''],
    retentionOverrideReason: ['']
  });
  protected readonly holdForm = this.formBuilder.nonNullable.group({
    reason: ['']
  });
  protected readonly archiveReasonControl = this.formBuilder.nonNullable.control('');
  protected readonly summary = signal<DocumentSummary | null>(null);
  protected readonly documents = signal<DocumentCatalogItem[]>([]);
  protected readonly pendingItems = signal<DocumentCompleteness[]>([]);
  protected readonly activeTab = signal<'catalog' | 'pending' | 'summary'>('catalog');
  protected readonly isDetailDrawerOpen = signal(false);
  protected readonly selectedDocument = signal<DocumentCatalogItem | DocumentCatalogDetail | null>(null);
  protected readonly selectedTimeline = signal<DocumentTimelineEvent[]>([]);
  protected readonly pageError = signal<string | null>(null);
  protected readonly pageSuccess = signal<string | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly isSummaryLoading = signal(false);
  protected readonly isPendingLoading = signal(false);
  protected readonly isTimelineLoading = signal(false);
  protected readonly isExporting = signal(false);
  protected readonly isMutating = signal(false);
  protected readonly totalCount = signal<number | null>(null);
  protected readonly resultCount = signal<number | null>(null);
  protected readonly pendingTotalCount = signal<number | null>(null);
  protected readonly pendingResultCount = signal<number | null>(null);
  protected readonly canManageDocuments = this.authService.canAdministerUsers;

  constructor() {
    void this.reloadSummary();
    void this.reload();
    void this.reloadPending();
  }

  protected async reloadSummary(): Promise<void> {
    this.isSummaryLoading.set(true);
    this.pageError.set(null);

    try {
      this.summary.set(await firstValueFrom(this.documentCatalogService.getSummary()));
    } catch (error) {
      this.summary.set(null);
      this.pageError.set(getApiErrorMessage(error, 'No se pudo cargar el resumen documental.'));
    } finally {
      this.isSummaryLoading.set(false);
    }
  }

  protected async reload(): Promise<void> {
    this.isLoading.set(true);
    this.pageError.set(null);
    this.pageSuccess.set(null);

    try {
      const response = await firstValueFrom(this.documentCatalogService.listDocuments(this.buildCatalogFilters()));

      this.documents.set(response.items);
      this.totalCount.set(response.totalCount);
      this.resultCount.set(response.returnedCount);
      const firstDocument = response.items[0] ?? null;
      this.selectedDocument.set(firstDocument);
      this.syncMetadataForm(firstDocument);
      this.syncRetentionOverrideForm(firstDocument);
      this.syncHoldForm(firstDocument);
      if (firstDocument) {
        await this.loadDocumentTimeline(firstDocument.id);
      } else {
        this.selectedTimeline.set([]);
      }
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No se pudo cargar el catalogo documental.'));
    } finally {
      this.isLoading.set(false);
    }
  }

  protected async reloadPending(): Promise<void> {
    this.isPendingLoading.set(true);
    this.pageError.set(null);

    try {
      const filters = this.pendingFiltersForm.getRawValue();
      const response = await firstValueFrom(this.documentCatalogService.listPendingCompleteness({
        moduleCode: filters.moduleCode,
        take: filters.take
      }));

      this.pendingItems.set(response.items);
      this.pendingTotalCount.set(response.totalCount);
      this.pendingResultCount.set(response.returnedCount);
    } catch (error) {
      this.pendingItems.set([]);
      this.pendingTotalCount.set(null);
      this.pendingResultCount.set(null);
      this.pageError.set(getApiErrorMessage(error, 'No se pudieron cargar los pendientes documentales.'));
    } finally {
      this.isPendingLoading.set(false);
    }
  }

  protected resetFilters(): void {
    this.filtersForm.reset({
      moduleCode: '',
      documentAreaCode: '',
      integrityState: '',
      documentOperationalStatusCode: '',
      documentClassCode: '',
      retentionPolicyCode: '',
      retentionStatusCode: '',
      statusCode: 'ACTIVE',
      entityType: '',
      entityId: '',
      take: 50
    });
    void this.reload();
  }

  protected async exportCatalog(): Promise<void> {
    this.isExporting.set(true);
    this.pageError.set(null);

    try {
      await this.documentCatalogService.exportDocuments(this.buildCatalogFilters());
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No se pudo exportar el catalogo documental.'));
    } finally {
      this.isExporting.set(false);
    }
  }

  protected documentClassLabel(documentClassCode: string): string {
    switch (documentClassCode) {
      case 'CERTIFICATE':
        return 'Cedula o certificado';
      case 'SIGNED_DOCUMENT':
        return 'Documento firmado';
      case 'SUPPORTING_DOCUMENT':
        return 'Soporte documental';
      case 'PHOTO_EVIDENCE':
        return 'Evidencia fotografica';
      case 'VIDEO_EVIDENCE':
        return 'Evidencia en video';
      case 'OTHER':
        return 'Otro';
      default:
        return documentClassCode;
    }
  }

  protected documentAreaLabel(documentAreaCode: string): string {
    switch (documentAreaCode) {
      case 'MARKETS_TENANT_CERTIFICATES':
        return 'Cedulas de mercados';
      case 'DONATIONS_APPLICATION_EVIDENCES':
        return 'Evidencias donatarias';
      case 'FEDERATION_APPLICATION_EVIDENCES':
        return 'Evidencias federacion';
      default:
        return documentAreaCode;
    }
  }

  protected operationalStatusLabel(statusCode: string | null): string {
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
        return statusCode || 'Sin estado';
    }
  }

  protected severityLabel(severityCode: string | null): string {
    switch (severityCode) {
      case 'HIGH':
        return 'Alta';
      case 'MEDIUM':
        return 'Media';
      case 'LOW':
        return 'Baja';
      case 'NONE':
        return 'Sin prioridad';
      default:
        return severityCode || 'Sin prioridad';
    }
  }

  protected retentionStatusLabel(statusCode: string): string {
    switch (statusCode) {
      case 'ACTIVE_RETENTION':
        return 'Dentro de periodo';
      case 'REVIEW_DUE':
        return 'Por revisar';
      case 'EXPIRED_RETENTION':
        return 'Vencida';
      default:
        return statusCode;
    }
  }

  protected retentionPolicyLabel(policyCode: string): string {
    switch (policyCode) {
      case 'CERTIFICATE_REVIEW':
        return 'Revision de cedulas';
      case 'SIGNED_LONG_TERM':
        return 'Resguardo largo';
      case 'EVIDENCE_MEDIUM_TERM':
        return 'Evidencia operativa';
      case 'GENERIC_REVIEW':
        return 'Revision general';
      default:
        return policyCode;
    }
  }

  protected retentionReviewStatusLabel(statusCode: string): string {
    switch (statusCode) {
      case 'REVIEW_PENDING':
        return 'Pendiente';
      case 'REVIEW_COMPLETED':
        return 'Revisada';
      case 'REVIEW_DEFERRED':
        return 'Diferida';
      default:
        return statusCode;
    }
  }

  protected integrityLabel(integrityState: string): string {
    switch (integrityState) {
      case 'VALID':
      case 'OK':
        return 'Correcta';
      case 'MISSING_FILE':
        return 'Archivo no localizado';
      case 'SIZE_MISMATCH':
        return 'Tamano distinto';
      case 'INVALID_PATH':
        return 'Ruta no valida';
      default:
        return integrityState;
    }
  }

  protected documentStatusLabel(statusCode: string): string {
    switch (statusCode) {
      case 'ACTIVE':
        return 'Vigente';
      case 'ARCHIVED':
        return 'Archivado';
      default:
        return statusCode;
    }
  }

  protected entityTypeLabel(entityType: string): string {
    switch (entityType) {
      case 'MARKET_TENANT':
        return 'Locatario';
      case 'DONATION_APPLICATION':
        return 'Aplicacion donataria';
      case 'FEDERATION_DONATION_APPLICATION':
        return 'Aplicacion federacion';
      default:
        return entityType;
    }
  }

  protected readonly canRemediatePending = (item: DocumentCompleteness): boolean => {
    switch (item.moduleCode) {
      case 'MARKETS':
        return this.authService.canWriteMarkets();
      case 'DONATARIAS':
        return this.authService.canWriteDonations();
      case 'FEDERATION':
        return this.authService.canWriteFederation();
      default:
        return false;
    }
  };

  protected filterByPendingEntity(item: DocumentCompleteness): void {
    this.activeTab.set('catalog');
    this.filtersForm.patchValue({
      moduleCode: item.moduleCode,
      documentAreaCode: '',
      integrityState: '',
      documentOperationalStatusCode: '',
      documentClassCode: '',
      retentionPolicyCode: '',
      retentionStatusCode: '',
      statusCode: 'ALL',
      entityType: item.entityType,
      entityId: item.entityId,
      take: 50
    });
    void this.reload();
  }

  protected async selectDocument(document: DocumentCatalogItem): Promise<void> {
    this.pageError.set(null);
    this.pageSuccess.set(null);
    this.isDetailDrawerOpen.set(true);

    try {
      const [detail, timeline] = await Promise.all([
        firstValueFrom(this.documentCatalogService.getDocument(document.id)),
        firstValueFrom(this.documentCatalogService.getDocumentTimeline(document.id))
      ]);
      this.selectedDocument.set(detail);
      this.selectedTimeline.set(timeline.items);
      this.archiveReasonControl.setValue(detail.archiveReason ?? '');
      this.syncMetadataForm(detail);
      this.syncRetentionOverrideForm(detail);
      this.syncHoldForm(detail);
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

  protected closeDetailDrawer(): void {
    this.isDetailDrawerOpen.set(false);
  }

  protected async archive(document: DocumentCatalogItem): Promise<void> {
    this.isMutating.set(true);
    this.pageError.set(null);
    this.pageSuccess.set(null);

    try {
      const detail = await firstValueFrom(this.documentCatalogService.archiveDocument(
        document.id,
        { reason: this.archiveReasonControl.value.trim() || null }));
      this.selectedDocument.set(detail);
      this.syncMetadataForm(detail);
      this.syncRetentionOverrideForm(detail);
      this.syncHoldForm(detail);
      await this.reloadSummary();
      await this.reloadPending();
      await this.reload();
      this.selectedDocument.set(detail);
      this.syncMetadataForm(detail);
      this.syncRetentionOverrideForm(detail);
      this.syncHoldForm(detail);
      await this.loadDocumentTimeline(detail.id);
      this.pageSuccess.set('Documento archivado.');
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No se pudo archivar el documento.'));
    } finally {
      this.isMutating.set(false);
    }
  }

  protected async restore(document: DocumentCatalogItem): Promise<void> {
    this.isMutating.set(true);
    this.pageError.set(null);
    this.pageSuccess.set(null);

    try {
      const detail = await firstValueFrom(this.documentCatalogService.restoreDocument(document.id));
      this.selectedDocument.set(detail);
      this.syncMetadataForm(detail);
      this.syncRetentionOverrideForm(detail);
      this.syncHoldForm(detail);
      this.archiveReasonControl.setValue('');
      await this.reloadSummary();
      await this.reloadPending();
      await this.reload();
      this.selectedDocument.set(detail);
      this.syncMetadataForm(detail);
      this.syncRetentionOverrideForm(detail);
      this.syncHoldForm(detail);
      await this.loadDocumentTimeline(detail.id);
      this.pageSuccess.set('Documento restaurado.');
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No se pudo restaurar el documento.'));
    } finally {
      this.isMutating.set(false);
    }
  }

  protected isDetail(document: DocumentCatalogItem | DocumentCatalogDetail): document is DocumentCatalogDetail {
    return 'sha256Hex' in document;
  }

  protected async saveMetadata(document: DocumentCatalogItem): Promise<void> {
    this.isMutating.set(true);
    this.pageError.set(null);
    this.pageSuccess.set(null);

    try {
      const metadata = this.metadataForm.getRawValue();
      const detail = await firstValueFrom(this.documentCatalogService.updateMetadata(
        document.id,
        {
          documentClassCode: metadata.documentClassCode,
          businessPurpose: metadata.businessPurpose.trim() || null,
          isPrimaryDocument: metadata.isPrimaryDocument,
          classificationNotes: metadata.classificationNotes.trim() || null
      }));
      this.selectedDocument.set(detail);
      this.syncMetadataForm(detail);
      this.syncRetentionOverrideForm(detail);
      this.syncHoldForm(detail);
      await this.reloadSummary();
      await this.reloadPending();
      await this.reload();
      this.selectedDocument.set(detail);
      this.syncMetadataForm(detail);
      this.syncRetentionOverrideForm(detail);
      this.syncHoldForm(detail);
      await this.loadDocumentTimeline(detail.id);
      this.pageSuccess.set('Metadata documental actualizada.');
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No se pudo actualizar la metadata documental.'));
    } finally {
      this.isMutating.set(false);
    }
  }

  protected async setRetentionOverride(document: DocumentCatalogItem): Promise<void> {
    this.isMutating.set(true);
    this.pageError.set(null);
    this.pageSuccess.set(null);

    try {
      const override = this.retentionOverrideForm.getRawValue();
      const detail = await firstValueFrom(this.documentCatalogService.setRetentionOverride(
        document.id,
        {
          retentionOverridePolicyCode: override.retentionOverridePolicyCode || null,
          retentionOverrideUntilUtc: this.toUtcDateTimeOffset(override.retentionOverrideUntilUtc),
          retentionOverrideReason: override.retentionOverrideReason.trim() || null
      }));
      this.selectedDocument.set(detail);
      this.syncMetadataForm(detail);
      this.syncRetentionOverrideForm(detail);
      this.syncHoldForm(detail);
      await this.reloadSummary();
      await this.reloadPending();
      await this.reload();
      this.selectedDocument.set(detail);
      this.syncMetadataForm(detail);
      this.syncRetentionOverrideForm(detail);
      this.syncHoldForm(detail);
      await this.loadDocumentTimeline(detail.id);
      this.pageSuccess.set('Override de retencion actualizado.');
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No se pudo actualizar el override de retencion.'));
    } finally {
      this.isMutating.set(false);
    }
  }

  protected async clearRetentionOverride(document: DocumentCatalogItem): Promise<void> {
    this.isMutating.set(true);
    this.pageError.set(null);
    this.pageSuccess.set(null);

    try {
      const detail = await firstValueFrom(this.documentCatalogService.clearRetentionOverride(document.id));
      this.selectedDocument.set(detail);
      this.syncMetadataForm(detail);
      this.syncRetentionOverrideForm(detail);
      this.syncHoldForm(detail);
      await this.reloadSummary();
      await this.reloadPending();
      await this.reload();
      this.selectedDocument.set(detail);
      this.syncMetadataForm(detail);
      this.syncRetentionOverrideForm(detail);
      this.syncHoldForm(detail);
      await this.loadDocumentTimeline(detail.id);
      this.pageSuccess.set('Override de retencion limpiado.');
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No se pudo limpiar el override de retencion.'));
    } finally {
      this.isMutating.set(false);
    }
  }

  protected async setAdministrativeHold(document: DocumentCatalogItem): Promise<void> {
    const reason = this.holdForm.controls.reason.value.trim();
    if (!reason) {
      this.pageError.set('El motivo del hold administrativo es requerido.');
      return;
    }

    this.isMutating.set(true);
    this.pageError.set(null);
    this.pageSuccess.set(null);

    try {
      const detail = await firstValueFrom(this.documentCatalogService.setAdministrativeHold(
        document.id,
        { reason }));
      this.selectedDocument.set(detail);
      this.syncMetadataForm(detail);
      this.syncRetentionOverrideForm(detail);
      this.syncHoldForm(detail);
      await this.reloadSummary();
      await this.reloadPending();
      await this.reload();
      this.selectedDocument.set(detail);
      this.syncMetadataForm(detail);
      this.syncRetentionOverrideForm(detail);
      this.syncHoldForm(detail);
      await this.loadDocumentTimeline(detail.id);
      this.pageSuccess.set('Hold administrativo aplicado.');
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No se pudo aplicar el hold administrativo.'));
    } finally {
      this.isMutating.set(false);
    }
  }

  protected async clearAdministrativeHold(document: DocumentCatalogItem): Promise<void> {
    this.isMutating.set(true);
    this.pageError.set(null);
    this.pageSuccess.set(null);

    try {
      const detail = await firstValueFrom(this.documentCatalogService.clearAdministrativeHold(document.id));
      this.selectedDocument.set(detail);
      this.syncMetadataForm(detail);
      this.syncRetentionOverrideForm(detail);
      this.syncHoldForm(detail);
      await this.reloadSummary();
      await this.reloadPending();
      await this.reload();
      this.selectedDocument.set(detail);
      this.syncMetadataForm(detail);
      this.syncRetentionOverrideForm(detail);
      this.syncHoldForm(detail);
      await this.loadDocumentTimeline(detail.id);
      this.pageSuccess.set('Hold administrativo limpiado.');
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No se pudo limpiar el hold administrativo.'));
    } finally {
      this.isMutating.set(false);
    }
  }

  private buildCatalogFilters(): DocumentCatalogFilters {
    const filters = this.filtersForm.getRawValue();
    const statusCode = filters.statusCode === 'ALL' ? null : filters.statusCode;

    return {
      moduleCode: filters.moduleCode,
      documentAreaCode: filters.documentAreaCode,
      integrityState: filters.integrityState,
      documentOperationalStatusCode: filters.documentOperationalStatusCode,
      documentClassCode: filters.documentClassCode,
      retentionPolicyCode: filters.retentionPolicyCode,
      retentionStatusCode: filters.retentionStatusCode,
      statusCode,
      includeArchived: filters.statusCode === 'ALL',
      entityType: filters.entityType,
      entityId: filters.entityId,
      take: filters.take
    };
  }

  private syncMetadataForm(document: DocumentCatalogItem | DocumentCatalogDetail | null): void {
    this.metadataForm.reset({
      documentClassCode: document?.documentClassCode ?? 'OTHER',
      businessPurpose: document?.businessPurpose ?? '',
      isPrimaryDocument: document?.isPrimaryDocument ?? false,
      classificationNotes: document?.classificationNotes ?? ''
    });
  }

  private syncRetentionOverrideForm(document: DocumentCatalogItem | DocumentCatalogDetail | null): void {
    this.retentionOverrideForm.reset({
      retentionOverridePolicyCode: document?.retentionOverridePolicyCode ?? '',
      retentionOverrideUntilUtc: this.toDateTimeLocal(document?.retentionOverrideUntilUtc ?? null),
      retentionOverrideReason: document?.retentionOverrideReason ?? ''
    });
  }

  private syncHoldForm(document: DocumentCatalogItem | DocumentCatalogDetail | null): void {
    this.holdForm.reset({
      reason: document?.holdReason ?? ''
    });
  }

  private toDateTimeLocal(value: string | null): string {
    return value ? new Date(value).toISOString().slice(0, 16) : '';
  }

  private toUtcDateTimeOffset(value: string): string | null {
    return value ? `${value}:00Z` : null;
  }

  protected async loadDocumentTimeline(documentId: string): Promise<void> {
    this.isTimelineLoading.set(true);

    try {
      const timeline = await firstValueFrom(this.documentCatalogService.getDocumentTimeline(documentId));
      this.selectedTimeline.set(timeline.items);
    } catch (error) {
      this.selectedTimeline.set([]);
      this.pageError.set(getApiErrorMessage(error, 'No se pudo cargar la historia documental.'));
    } finally {
      this.isTimelineLoading.set(false);
    }
  }
}
