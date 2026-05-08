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

@Component({
  selector: 'app-documents-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DecimalPipe, ReactiveFormsModule],
  template: `
    <section class="page-shell">
      <header class="page-header">
        <p class="page-kicker">TRACK 3 DOCUMENTOS</p>
        <h2>Catalogo documental</h2>
      </header>

      @if (pageError()) {
        <p class="alert error">{{ pageError() }}</p>
      }

      @if (pageSuccess()) {
        <p class="alert success">{{ pageSuccess() }}</p>
      }

      <article class="panel summary-panel">
        <div class="panel-header">
          <div>
            <h3>Resumen ejecutivo documental</h3>
            @if (isSummaryLoading()) {
              <span>Actualizando...</span>
            }
          </div>
          <div class="summary-actions">
            <a class="origin-link" href="/documents/work-queue">Bandeja documental</a>
            <button type="button" class="ghost compact" [disabled]="isSummaryLoading()" (click)="reloadSummary()">
              Actualizar
            </button>
          </div>
        </div>

        @if (summary(); as documentSummary) {
          <div class="kpi-grid">
            <div>
              <span>Total</span>
              <strong>{{ documentSummary.totalDocuments | number }}</strong>
            </div>
            <div>
              <span>Vigentes</span>
              <strong>{{ documentSummary.activeDocuments | number }}</strong>
            </div>
            <div>
              <span>Archivados</span>
              <strong>{{ documentSummary.archivedDocuments | number }}</strong>
            </div>
            <div>
              <span>Integridad</span>
              <strong>{{ documentSummary.integrityIssuesCount | number }}</strong>
            </div>
            <div>
              <span>Incompletos</span>
              <strong>{{ documentSummary.incompleteEntitiesCount | number }}</strong>
            </div>
            <div>
              <span>Revision</span>
              <strong>{{ documentSummary.reviewDueCount | number }}</strong>
            </div>
            <div>
              <span>Retencion vencida</span>
              <strong>{{ documentSummary.expiredRetentionCount | number }}</strong>
            </div>
            <div>
              <span>Hold admin</span>
              <strong>{{ documentSummary.administrativeHoldCount | number }}</strong>
            </div>
          </div>

          <div class="summary-grid">
            <section>
              <h4>Por modulo</h4>
              <div class="summary-table">
                @for (module of documentSummary.modules; track module.moduleCode) {
                  <div>
                    <strong>{{ module.moduleName }}</strong>
                    <span>{{ module.totalDocuments | number }} docs</span>
                    <span>{{ module.incompleteEntitiesCount | number }} incompletos</span>
                    <span>{{ module.integrityIssuesCount | number }} integridad</span>
                    <span>{{ module.reviewDueCount | number }} revision</span>
                    <span>{{ module.expiredRetentionCount | number }} vencidos</span>
                    <span>{{ module.administrativeHoldCount | number }} hold</span>
                  </div>
                }
              </div>
            </section>

            <section>
              <h4>Estado operativo</h4>
              @if (documentSummary.operationalStatuses.length === 0) {
                <p class="empty-state">Sin estados operativos calculados.</p>
              } @else {
                <div class="summary-table">
                  @for (status of documentSummary.operationalStatuses; track status.documentOperationalStatusCode) {
                    <div>
                      <strong>{{ status.documentOperationalStatusCode }}</strong>
                      <span>{{ status.documentOperationalSeverityCode }}</span>
                      <span>{{ status.totalCount | number }} docs</span>
                    </div>
                  }
                </div>
              }
            </section>

            <section>
              <h4>Trabajo pendiente</h4>
              @if (documentSummary.workQueueCategories.length === 0) {
                <p class="empty-state">Sin categorias pendientes.</p>
              } @else {
                <div class="summary-table">
                  @for (category of documentSummary.workQueueCategories; track category.workItemType + category.reasonCode + category.severityCode) {
                    <div>
                      <strong>{{ category.reasonCode }}</strong>
                      <span>{{ category.workItemType }}</span>
                      <span>{{ category.severityCode }}</span>
                      <span>{{ category.totalCount | number }} items</span>
                    </div>
                  }
                </div>
              }
            </section>
          </div>
        } @else {
          <p class="empty-state">No se pudo cargar el resumen documental.</p>
        }
      </article>

      <form class="filters-panel" [formGroup]="filtersForm" (ngSubmit)="reload()">
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
            <option value="VALID">VALID</option>
            <option value="MISSING_FILE">MISSING_FILE</option>
            <option value="SIZE_MISMATCH">SIZE_MISMATCH</option>
            <option value="INVALID_PATH">INVALID_PATH</option>
          </select>
        </label>

        <label>
          <span>Estado operativo</span>
          <select formControlName="documentOperationalStatusCode">
            <option value="">Todos</option>
            <option value="ACTIVE_OK">ACTIVE_OK</option>
            <option value="INTEGRITY_ISSUE">INTEGRITY_ISSUE</option>
            <option value="ON_HOLD">ON_HOLD</option>
            <option value="REVIEW_DUE">REVIEW_DUE</option>
            <option value="RETENTION_EXPIRED">RETENTION_EXPIRED</option>
            <option value="ARCHIVED">ARCHIVED</option>
            <option value="SUPERSEDED">SUPERSEDED</option>
          </select>
        </label>

        <label>
          <span>Clase</span>
          <select formControlName="documentClassCode">
            <option value="">Todas</option>
            <option value="CERTIFICATE">CERTIFICATE</option>
            <option value="SIGNED_DOCUMENT">SIGNED_DOCUMENT</option>
            <option value="SUPPORTING_DOCUMENT">SUPPORTING_DOCUMENT</option>
            <option value="PHOTO_EVIDENCE">PHOTO_EVIDENCE</option>
            <option value="VIDEO_EVIDENCE">VIDEO_EVIDENCE</option>
            <option value="OTHER">OTHER</option>
          </select>
        </label>

        <label>
          <span>Politica retencion</span>
          <select formControlName="retentionPolicyCode">
            <option value="">Todas</option>
            <option value="CERTIFICATE_REVIEW">CERTIFICATE_REVIEW</option>
            <option value="SIGNED_LONG_TERM">SIGNED_LONG_TERM</option>
            <option value="EVIDENCE_MEDIUM_TERM">EVIDENCE_MEDIUM_TERM</option>
            <option value="GENERIC_REVIEW">GENERIC_REVIEW</option>
          </select>
        </label>

        <label>
          <span>Estado retencion</span>
          <select formControlName="retentionStatusCode">
            <option value="">Todos</option>
            <option value="ACTIVE_RETENTION">ACTIVE_RETENTION</option>
            <option value="REVIEW_DUE">REVIEW_DUE</option>
            <option value="EXPIRED_RETENTION">EXPIRED_RETENTION</option>
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
          <span>Entidad</span>
          <input type="text" formControlName="entityType" placeholder="MARKET_TENANT" />
        </label>

        <label>
          <span>Entity ID</span>
          <input type="text" formControlName="entityId" placeholder="GUID" />
        </label>

        <label>
          <span>Limite</span>
          <input type="number" min="1" max="200" formControlName="take" />
        </label>

        <div class="filter-actions">
          <button type="submit" [disabled]="isLoading()">Buscar</button>
          <button type="button" class="ghost" (click)="resetFilters()">Limpiar</button>
          <button type="button" class="ghost" [disabled]="isExporting()" (click)="exportCatalog()">Exportar CSV</button>
        </div>
      </form>

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

        @if (isPendingLoading()) {
          <p class="empty-state">Cargando pendientes documentales...</p>
        } @else if (pendingItems().length === 0) {
          <p class="empty-state">No hay pendientes documentales con el filtro actual.</p>
        } @else {
          <div class="pending-list">
            @for (item of pendingItems(); track item.moduleCode + item.entityId) {
              <article class="pending-row">
                <div>
                  <span class="badge">{{ item.moduleName }}</span>
                  <h4>{{ item.originContext.displayName }}</h4>
                  <p>{{ item.originContext.entityType }} · {{ item.originContext.entityId }}</p>
                  <p>{{ item.missingReasonDescription || item.requiredDocumentDescription }}</p>
                  <p>
                    Regla {{ item.ruleCode }}
                    · minimo {{ item.minimumRequiredCount }}
                    · clases {{ item.requiredDocumentClassCodes.join(', ') }}
                  </p>
                  @if (item.remediationHint) {
                    <p>{{ item.remediationHint }}</p>
                  }
                </div>
                <div class="pending-row-actions">
                  <span class="pending-status">{{ item.statusCode }}</span>
                  <button type="button" class="ghost compact" (click)="filterByPendingEntity(item)">
                    Ver documentos
                  </button>
                  @if (item.originContext.routeHint) {
                    <a class="origin-link" [href]="item.originContext.routeHint">
                      {{ canRemediatePending(item) ? 'Resolver en origen' : 'Abrir origen' }}
                    </a>
                  }
                </div>
              </article>
            }
          </div>
        }
      </article>

      <div class="content-grid">
        <article class="panel">
          <div class="panel-header">
            <h3>Documentos</h3>
            @if (resultCount() !== null) {
              <span>{{ resultCount() }} de {{ totalCount() }}</span>
            }
          </div>

          @if (isLoading()) {
            <p class="empty-state">Cargando documentos...</p>
          } @else if (documents().length === 0) {
            <p class="empty-state">No hay documentos con los filtros actuales.</p>
          } @else {
            <div class="document-list">
              @for (document of documents(); track document.id) {
                <article
                  class="document-row"
                  [class.selected]="selectedDocument()?.id === document.id">
                  <button type="button" class="row-main" (click)="selectDocument(document)">
                    <span class="badge">{{ document.moduleName }}</span>
                    <strong>{{ document.originalFileName }}</strong>
                    <small>{{ document.documentClassCode }} · {{ document.originContext.displayName }}</small>
                    <small>{{ document.originContext.entityType }} · {{ document.originContext.entityId }}</small>
                  </button>
                  <div class="row-meta">
                    <span
                      class="operational"
                      [class.high]="document.documentOperationalSeverityCode === 'HIGH'"
                      [class.medium]="document.documentOperationalSeverityCode === 'MEDIUM'"
                      [class.low]="document.documentOperationalSeverityCode === 'LOW'">
                      {{ document.documentOperationalStatusCode }}
                    </span>
                    @if (document.isPrimaryDocument) {
                      <span class="primary">Principal</span>
                    }
                    @if (document.isSuperseded) {
                      <span class="superseded">Reemplazado</span>
                    } @else if (document.replacedDocumentId) {
                      <span class="replacement">Vigente reemplazo</span>
                    }
                    <span class="retention" [class.expired]="document.retentionStatusCode === 'EXPIRED_RETENTION'" [class.review]="document.retentionStatusCode === 'REVIEW_DUE'">
                      {{ document.retentionStatusCode }}
                    </span>
                    @if (document.hasRetentionOverride) {
                      <span class="retention">Override retencion</span>
                    }
                    @if (document.isAdministrativeHold) {
                      <span class="hold">Hold admin</span>
                    }
                    <span class="status" [class.archived]="document.statusCode === 'ARCHIVED'">
                      {{ document.statusCode }}
                    </span>
                    <span [class.issue]="document.integrityState !== 'VALID'">
                      {{ document.integrityState }}
                    </span>
                    <span>{{ document.sizeBytes | number }} bytes</span>
                    <span>{{ document.createdUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</span>
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
            <h3>Detalle</h3>
          </div>

          @if (selectedDocument(); as document) {
            <dl>
              <div>
                <dt>Modulo</dt>
                <dd>{{ document.moduleCode }}</dd>
              </div>
              <div>
                <dt>Area</dt>
                <dd>{{ document.documentAreaCode }}</dd>
              </div>
              <div>
                <dt>Entidad</dt>
                <dd>{{ document.entityType }} · {{ document.entityId }}</dd>
              </div>
              <div>
                <dt>Origen</dt>
                <dd>{{ document.originContext.displayName }}</dd>
              </div>
              <div>
                <dt>Entidad origen</dt>
                <dd>{{ document.originContext.entityType }} · {{ document.originContext.entityId }}</dd>
              </div>
              @if (document.originContext.summary) {
                <div>
                  <dt>Resumen origen</dt>
                  <dd>{{ document.originContext.summary }}</dd>
                </div>
              }
              @if (document.originContext.routeHint) {
                <div>
                  <dt>Navegacion</dt>
                  <dd><a class="origin-link" [href]="document.originContext.routeHint">Abrir origen</a></dd>
                </div>
              }
              <div>
                <dt>Content type</dt>
                <dd>{{ document.contentType }}</dd>
              </div>
              <div>
                <dt>Clase</dt>
                <dd>{{ document.documentClassCode }}</dd>
              </div>
              <div>
                <dt>Estado operativo</dt>
                <dd>{{ document.documentOperationalStatusCode }} · {{ document.documentOperationalSeverityCode }}</dd>
              </div>
              <div>
                <dt>Politica efectiva</dt>
                <dd>{{ document.retentionPolicyCode }}</dd>
              </div>
              <div>
                <dt>Estado retencion</dt>
                <dd>{{ document.retentionStatusCode }}</dd>
              </div>
              <div>
                <dt>Revision retencion</dt>
                <dd>{{ document.retentionReviewStatusCode }}</dd>
              </div>
              <div>
                <dt>Hold administrativo</dt>
                <dd>{{ document.isAdministrativeHold ? 'Activo' : 'Inactivo' }}</dd>
              </div>
              @if (document.isAdministrativeHold) {
                <div>
                  <dt>Motivo hold</dt>
                  <dd>{{ document.holdReason || 'Sin motivo registrado' }}</dd>
                </div>
                <div>
                  <dt>Hold colocado UTC</dt>
                  <dd>{{ document.holdPlacedUtc ? (document.holdPlacedUtc | date: 'yyyy-MM-dd HH:mm':'UTC') : 'Sin fecha' }}</dd>
                </div>
                <div>
                  <dt>Colocado por</dt>
                  <dd>{{ document.holdPlacedBy || 'No registrado' }}</dd>
                </div>
              } @else if (document.holdReleasedUtc) {
                <div>
                  <dt>Hold liberado UTC</dt>
                  <dd>{{ document.holdReleasedUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</dd>
                </div>
              }
              <div>
                <dt>Retener efectivo UTC</dt>
                <dd>{{ document.retentionUntilUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</dd>
              </div>
              <div>
                <dt>Baseline retencion</dt>
                <dd>{{ document.retentionBaselinePolicyCode }} · {{ document.retentionBaselineUntilUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</dd>
              </div>
              <div>
                <dt>Effective retencion</dt>
                <dd>{{ document.retentionEffectivePolicyCode }} · {{ document.retentionEffectiveUntilUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</dd>
              </div>
              @if (document.hasRetentionOverride) {
                <div>
                  <dt>Override retencion</dt>
                  <dd>
                    {{ document.retentionOverridePolicyCode || 'Politica baseline' }}
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
                <dd>{{ document.statusCode }}</dd>
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
                <dd>{{ document.integrityState }}</dd>
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
                    Guardar metadata
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
                    Guardar override retencion
                  </button>

                  @if (document.hasRetentionOverride) {
                    <button type="button" class="ghost" [disabled]="isMutating()" (click)="clearRetentionOverride(document)">
                      Limpiar override retencion
                    </button>
                  }
                </form>

                <form class="metadata-form" [formGroup]="holdForm" (ngSubmit)="setAdministrativeHold(document)">
                  <label>
                    <span>Motivo hold administrativo</span>
                    <textarea formControlName="reason" maxlength="500"></textarea>
                  </label>

                  <button type="submit" class="ghost" [disabled]="isMutating()">
                    Guardar hold administrativo
                  </button>

                  @if (document.isAdministrativeHold) {
                    <button type="button" class="ghost" [disabled]="isMutating()" (click)="clearAdministrativeHold(document)">
                      Limpiar hold administrativo
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
    </section>
  `,
  styles: [
    `
      .page-shell,
      .document-list,
      dl {
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
        min-height: 4.75rem;
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
        grid-template-columns: minmax(0, 1fr) minmax(280px, 0.35fr);
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

      .pending-actions {
        display: flex;
        gap: 0.6rem;
        align-items: center;
      }

      .summary-actions {
        display: flex;
        gap: 0.65rem;
      }

      .summary-table {
        display: grid;
        gap: 0.5rem;
      }

      .pending-list {
        display: grid;
        gap: 0.75rem;
      }

      .pending-row {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        padding: 0.95rem 0;
        border-top: 1px solid rgba(35, 51, 47, 0.08);
      }

      .pending-row:first-child {
        border-top: 0;
        padding-top: 0;
      }

      .pending-row h4,
      .pending-row p {
        margin: 0.25rem 0 0;
      }

      .pending-row-actions {
        display: grid;
        gap: 0.5rem;
        justify-items: end;
        align-content: start;
      }

      .pending-status {
        width: fit-content;
        border-radius: 999px;
        padding: 0.2rem 0.5rem;
        color: #92400e;
        background: rgba(146, 64, 14, 0.1);
        font-size: 0.76rem;
        font-weight: 800;
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

      .row-meta span.archived {
        color: #6d28d9;
        background: rgba(109, 40, 217, 0.08);
      }

      .row-meta span.primary {
        color: #1d4ed8;
        background: rgba(29, 78, 216, 0.08);
      }

      .row-meta span.superseded {
        color: #7c2d12;
        background: rgba(124, 45, 18, 0.08);
      }

      .row-meta span.replacement {
        color: #047857;
        background: rgba(4, 120, 87, 0.08);
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

      .row-meta span.issue {
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

      .hash {
        font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
        font-size: 0.82rem;
      }

      .origin-link {
        color: #0f766e;
        font-weight: 800;
      }

      .detail-panel button {
        margin-top: 1rem;
        width: 100%;
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
        .filters-panel,
        .content-grid,
        .document-row,
        .pending-row,
        .pending-actions {
          grid-template-columns: 1fr;
          display: grid;
        }

        .filter-actions {
          flex-wrap: wrap;
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

  protected canRemediatePending(item: DocumentCompleteness): boolean {
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
  }

  protected filterByPendingEntity(item: DocumentCompleteness): void {
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
