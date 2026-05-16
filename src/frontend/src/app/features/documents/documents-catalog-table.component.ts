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
        <div class="table-scroll">
          <table>
            <thead>
              <tr>
                <th>Documento</th>
                <th>Origen</th>
                <th>Modulo</th>
                <th>Estado</th>
                <th>Retencion</th>
                <th>Integridad</th>
                <th>Fecha</th>
                <th class="numeric">Tamano</th>
                <th class="actions-column">Acciones</th>
              </tr>
            </thead>
            <tbody>
              @for (document of documents; track document.id) {
                <tr [class.selected]="selectedDocumentId === document.id">
                  <td>
                    <button type="button" class="text-link" (click)="selectDocument.emit(document)">
                      <strong>{{ document.originalFileName }}</strong>
                      <span>{{ documentClassLabel(document.documentClassCode) }}</span>
                    </button>
                  </td>
                  <td>
                    <strong>{{ document.originContext.displayName }}</strong>
                    <span>{{ entityTypeLabel(document.originContext.entityType) }} · {{ document.originContext.entityId }}</span>
                  </td>
                  <td>
                    <span class="badge">{{ document.moduleName }}</span>
                  </td>
                  <td>
                    <span
                      class="pill"
                      [class.high]="document.documentOperationalSeverityCode === 'HIGH'"
                      [class.medium]="document.documentOperationalSeverityCode === 'MEDIUM'"
                      [class.low]="document.documentOperationalSeverityCode === 'LOW'">
                      {{ operationalStatusLabel(document.documentOperationalStatusCode) }}
                    </span>
                    <div class="flag-row">
                      @if (document.isPrimaryDocument) {
                        <span class="flag primary">Principal</span>
                      }
                      @if (document.isSuperseded) {
                        <span class="flag superseded">Reemplazado</span>
                      } @else if (document.replacedDocumentId) {
                        <span class="flag replacement">Reemplazo</span>
                      }
                      @if (document.isAdministrativeHold) {
                        <span class="flag hold">Resguardo</span>
                      }
                    </div>
                  </td>
                  <td>
                    <span
                      class="pill retention"
                      [class.expired]="document.retentionStatusCode === 'EXPIRED_RETENTION'"
                      [class.review]="document.retentionStatusCode === 'REVIEW_DUE'">
                      {{ retentionStatusLabel(document.retentionStatusCode) }}
                    </span>
                    @if (document.hasRetentionOverride) {
                      <span class="flag">Ajustada</span>
                    }
                  </td>
                  <td>
                    <span class="pill" [class.issue]="document.integrityState !== 'VALID'">
                      {{ integrityLabel(document.integrityState) }}
                    </span>
                  </td>
                  <td>{{ document.createdUtc | date: 'yyyy-MM-dd':'UTC' }}</td>
                  <td class="numeric">{{ document.sizeBytes | number }}</td>
                  <td class="row-actions">
                    <button type="button" class="ghost compact" (click)="selectDocument.emit(document)">
                      Detalle
                    </button>
                    <button type="button" class="ghost compact" (click)="downloadDocument.emit(document)">
                      Descargar
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
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

      .table-scroll {
        overflow-x: auto;
      }

      table {
        width: 100%;
        min-width: 920px;
        border-collapse: collapse;
        font-size: 0.88rem;
      }

      th,
      td {
        border-bottom: 1px solid rgba(35, 51, 47, 0.08);
        padding: 0.58rem 0.55rem;
        text-align: left;
        vertical-align: top;
      }

      th {
        color: #60716d;
        font-size: 0.72rem;
        font-weight: 900;
        text-transform: uppercase;
      }

      tbody tr {
        background: #fff;
      }

      tbody tr.selected {
        background: rgba(15, 118, 110, 0.06);
        box-shadow: inset 3px 0 0 #0f766e;
      }

      td strong,
      td span {
        display: block;
      }

      td span {
        color: #60716d;
      }

      .text-link {
        display: grid;
        gap: 0.15rem;
        width: 100%;
        padding: 0;
        color: inherit;
        text-align: left;
        background: transparent;
      }

      .text-link strong,
      td strong,
      td span {
        overflow-wrap: anywhere;
      }

      .badge,
      .pill,
      .flag {
        display: inline-block;
        width: fit-content;
        border-radius: 999px;
        padding: 0.18rem 0.45rem;
        color: #0f766e;
        background: rgba(15, 118, 110, 0.1);
        font-size: 0.72rem;
        font-weight: 800;
      }

      .flag-row {
        display: flex;
        flex-wrap: wrap;
        gap: 0.25rem;
        margin-top: 0.3rem;
      }

      .flag.primary {
        color: #1d4ed8;
        background: rgba(29, 78, 216, 0.08);
      }

      .flag.superseded {
        color: #7c2d12;
        background: rgba(124, 45, 18, 0.08);
      }

      .flag.replacement {
        color: #047857;
        background: rgba(4, 120, 87, 0.08);
      }

      .pill.retention {
        color: #365314;
        background: rgba(77, 124, 15, 0.1);
      }

      .pill.retention.review {
        color: #92400e;
        background: rgba(146, 64, 14, 0.1);
      }

      .pill.retention.expired,
      .pill.issue,
      .pill.high {
        color: #9f1239;
        background: rgba(159, 18, 57, 0.08);
      }

      .pill.medium {
        color: #92400e;
        background: rgba(146, 64, 14, 0.1);
      }

      .pill.low {
        color: #365314;
        background: rgba(77, 124, 15, 0.1);
      }

      .numeric {
        text-align: right;
        white-space: nowrap;
      }

      .actions-column {
        width: 9.5rem;
      }

      .row-actions {
        display: flex;
        gap: 0.35rem;
        justify-content: flex-end;
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
        .panel {
          padding: 0.9rem;
        }

        table {
          min-width: 760px;
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
