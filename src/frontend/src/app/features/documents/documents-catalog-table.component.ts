import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';

import { DocumentCatalogItem } from '../../core/models/document-catalog.models';

@Component({
  selector: 'app-documents-catalog-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DecimalPipe],
  template: `
    <article class="panel">
      <div class="panel-header">
        <h3>Documentos</h3>
        @if (resultCount !== null) {
          <span>{{ resultCount }} de {{ totalCount }}</span>
        }
      </div>

      @if (isLoading) {
        <p class="empty-state">Cargando documentos...</p>
      } @else if (documents.length === 0) {
        <p class="empty-state">No hay documentos con los filtros actuales.</p>
      } @else {
        <div class="document-list">
          @for (document of documents; track document.id) {
            <article
              class="document-row"
              [class.selected]="selectedDocumentId === document.id">
              <button type="button" class="row-main" (click)="selectDocument.emit(document)">
                <span class="badge">{{ document.moduleName }}</span>
                <strong>{{ document.originalFileName }}</strong>
                <small>{{ documentClassLabel(document.documentClassCode) }} · {{ document.originContext.displayName }}</small>
                <small>{{ entityTypeLabel(document.originContext.entityType) }} · {{ document.originContext.entityId }}</small>
              </button>
              <div class="row-meta">
                <span
                  class="operational"
                  [class.high]="document.documentOperationalSeverityCode === 'HIGH'"
                  [class.medium]="document.documentOperationalSeverityCode === 'MEDIUM'"
                  [class.low]="document.documentOperationalSeverityCode === 'LOW'">
                  {{ operationalStatusLabel(document.documentOperationalStatusCode) }}
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
                  {{ retentionStatusLabel(document.retentionStatusCode) }}
                </span>
                @if (document.hasRetentionOverride) {
                  <span class="retention">Retencion ajustada</span>
                }
                @if (document.isAdministrativeHold) {
                  <span class="hold">En resguardo</span>
                }
                <span class="status" [class.archived]="document.statusCode === 'ARCHIVED'">
                  {{ documentStatusLabel(document.statusCode) }}
                </span>
                <span [class.issue]="document.integrityState !== 'VALID'">
                  {{ integrityLabel(document.integrityState) }}
                </span>
                <span>{{ document.sizeBytes | number }} bytes</span>
                <span>{{ document.createdUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</span>
              </div>
              <button type="button" class="ghost compact" (click)="downloadDocument.emit(document)">
                Descargar
              </button>
            </article>
          }
        </div>
      }
    </article>
  `,
  styles: [
    `
      .panel {
        border: 1px solid rgba(35, 51, 47, 0.12);
        border-radius: 8px;
        padding: 1.25rem;
        background: rgba(255, 255, 255, 0.9);
        box-shadow: 0 14px 26px rgba(32, 44, 41, 0.06);
      }

      .panel-header {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        align-items: center;
        margin-bottom: 1rem;
      }

      h3,
      p {
        margin: 0;
      }

      .panel-header span,
      .empty-state,
      small {
        color: #60716d;
      }

      .document-list {
        display: grid;
        gap: 1rem;
      }

      .document-row {
        display: grid;
        grid-template-columns: minmax(0, 1.4fr) minmax(12rem, 0.8fr) auto;
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
      .row-main small {
        overflow-wrap: anywhere;
      }

      .row-meta {
        display: flex;
        flex-wrap: wrap;
        gap: 0.3rem;
        color: #60716d;
        font-size: 0.85rem;
      }

      .badge,
      .row-meta span {
        width: fit-content;
        border-radius: 999px;
        padding: 0.2rem 0.5rem;
        color: #0f766e;
        background: rgba(15, 118, 110, 0.1);
        font-size: 0.76rem;
        font-weight: 800;
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

      .row-meta span.retention.expired,
      .row-meta span.issue {
        color: #9f1239;
        background: rgba(159, 18, 57, 0.08);
      }

      button {
        min-width: 0;
        border: 0;
        border-radius: 8px;
        padding: 0.68rem 0.9rem;
        color: #fff;
        background: #0f766e;
        cursor: pointer;
        font-weight: 800;
      }

      button.ghost {
        color: #0f766e;
        background: rgba(15, 118, 110, 0.1);
      }

      button.compact {
        padding: 0.55rem 0.8rem;
      }

      @media (max-width: 980px) {
        .document-row {
          grid-template-columns: 1fr;
        }
      }
    `
  ]
})
export class DocumentsCatalogTableComponent {
  @Input() documents: DocumentCatalogItem[] = [];
  @Input() isLoading = false;
  @Input() totalCount: number | null = null;
  @Input() resultCount: number | null = null;
  @Input() selectedDocumentId: string | null = null;

  @Output() selectDocument = new EventEmitter<DocumentCatalogItem>();
  @Output() downloadDocument = new EventEmitter<DocumentCatalogItem>();

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
}
