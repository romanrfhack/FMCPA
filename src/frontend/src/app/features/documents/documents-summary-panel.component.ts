import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

import { DocumentSummary } from '../../core/models/document-catalog.models';

@Component({
  selector: 'app-documents-summary-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe],
  template: `
    @if (summary) {
      <div class="summary-grid">
        <section>
          <h4>Por modulo</h4>
          <div class="summary-table">
            @for (module of summary.modules; track module.moduleCode) {
              <div>
                <strong>{{ module.moduleName }}</strong>
                <span>{{ module.totalDocuments | number }} documentos</span>
                <span>{{ module.incompleteEntitiesCount | number }} incompletos</span>
                <span>{{ module.integrityIssuesCount | number }} integridad</span>
                <span>{{ module.reviewDueCount | number }} por revisar</span>
                <span>{{ module.expiredRetentionCount | number }} vencidos</span>
                <span>{{ module.administrativeHoldCount | number }} en resguardo</span>
              </div>
            }
          </div>
        </section>

        <section>
          <h4>Estado operativo</h4>
          @if (summary.operationalStatuses.length === 0) {
            <p class="empty-state">Sin estados operativos calculados.</p>
          } @else {
            <div class="summary-table">
              @for (status of summary.operationalStatuses; track status.documentOperationalStatusCode) {
                <div>
                  <strong>{{ operationalStatusLabel(status.documentOperationalStatusCode) }}</strong>
                  <span>{{ severityLabel(status.documentOperationalSeverityCode) }}</span>
                  <span>{{ status.totalCount | number }} documentos</span>
                </div>
              }
            </div>
          }
        </section>

        <section>
          <h4>Trabajo pendiente</h4>
          @if (summary.workQueueCategories.length === 0) {
            <p class="empty-state">Sin categorias pendientes.</p>
          } @else {
            <div class="summary-table">
              @for (category of summary.workQueueCategories; track category.workItemType + category.reasonCode + category.severityCode) {
                <div>
                  <strong>{{ workItemTypeLabel(category.workItemType) }}</strong>
                  <span>{{ reasonLabel(category.reasonCode) }}</span>
                  <span>{{ severityLabel(category.severityCode) }}</span>
                  <span>{{ category.totalCount | number }} pendientes</span>
                </div>
              }
            </div>
          }
        </section>

        @if (summary.documentClasses.length > 0) {
          <section>
            <h4>Por clase</h4>
            <div class="summary-table">
              @for (documentClass of summary.documentClasses; track documentClass.documentClassCode) {
                <div>
                  <strong>{{ documentClassLabel(documentClass.documentClassCode) }}</strong>
                  <span>{{ documentClass.totalDocuments | number }} documentos</span>
                  <span>{{ documentClass.activeDocuments | number }} vigentes</span>
                  <span>{{ documentClass.archivedDocuments | number }} archivados</span>
                </div>
              }
            </div>
          </section>
        }
      </div>
    } @else {
      <p class="empty-state">No se pudo cargar el resumen documental.</p>
    }
  `,
  styles: [
    `
      .summary-grid {
        display: grid;
        gap: 0.75rem;
      }

      h4,
      p {
        margin: 0;
      }

      h4 {
        margin-bottom: 0.5rem;
        color: #123f3b;
      }

      .summary-table {
        display: grid;
        gap: 0.5rem;
      }

      .summary-table div {
        display: grid;
        gap: 0.25rem;
        min-width: 0;
        border-radius: 8px;
        padding: 0.65rem;
        background: #f6f5ef;
      }

      .summary-table span,
      .empty-state {
        color: #60716d;
        overflow-wrap: anywhere;
      }
    `
  ]
})
export class DocumentsSummaryPanelComponent {
  @Input() summary: DocumentSummary | null = null;

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

  protected workItemTypeLabel(workItemType: string): string {
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
}
