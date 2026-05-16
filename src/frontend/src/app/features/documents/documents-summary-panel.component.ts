import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

import { DocumentSummary } from '../../core/models/document-catalog.models';

@Component({
  selector: 'app-documents-summary-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe],
  template: `
    @if (summary) {
      <div class="summary-dashboard">
        <section class="summary-section modules-section">
          <div class="section-heading">
            <h4>Por modulo</h4>
            <span>{{ summary.modules.length | number }} modulos</span>
          </div>
          <div class="module-grid">
            @for (module of summary.modules; track module.moduleCode) {
              <article class="module-card">
                <header>
                  <strong>{{ module.moduleName }}</strong>
                  <span>{{ module.totalDocuments | number }} docs</span>
                </header>
                <dl class="metric-grid">
                  <div>
                    <dt>Incompletos</dt>
                    <dd>{{ module.incompleteEntitiesCount | number }}</dd>
                  </div>
                  <div>
                    <dt>Integridad</dt>
                    <dd>{{ module.integrityIssuesCount | number }}</dd>
                  </div>
                  <div>
                    <dt>Por revisar</dt>
                    <dd>{{ module.reviewDueCount | number }}</dd>
                  </div>
                  <div>
                    <dt>Vencidos</dt>
                    <dd>{{ module.expiredRetentionCount | number }}</dd>
                  </div>
                  <div>
                    <dt>Resguardo</dt>
                    <dd>{{ module.administrativeHoldCount | number }}</dd>
                  </div>
                  <div>
                    <dt>Documentos</dt>
                    <dd>{{ module.totalDocuments | number }}</dd>
                  </div>
                </dl>
              </article>
            }
          </div>
        </section>

        <div class="summary-pair">
          <section class="summary-section">
            <div class="section-heading">
              <h4>Estado operativo</h4>
              <span>{{ summary.operationalStatuses.length | number }} estados</span>
            </div>
            @if (summary.operationalStatuses.length === 0) {
              <p class="empty-state">Sin estados operativos calculados.</p>
            } @else {
              <div class="compact-list">
                @for (status of summary.operationalStatuses; track status.documentOperationalStatusCode) {
                  <div class="compact-row">
                    <div>
                      <strong>{{ operationalStatusLabel(status.documentOperationalStatusCode) }}</strong>
                      <span>{{ severityLabel(status.documentOperationalSeverityCode) }}</span>
                    </div>
                    <b>{{ status.totalCount | number }} <small>docs</small></b>
                  </div>
                }
              </div>
            }
          </section>

          <section class="summary-section">
            <div class="section-heading">
              <h4>Trabajo pendiente</h4>
              <span>{{ summary.workQueueCategories.length | number }} categorias</span>
            </div>
            @if (summary.workQueueCategories.length === 0) {
              <p class="empty-state">Sin categorias pendientes.</p>
            } @else {
              <div class="compact-list">
                @for (category of summary.workQueueCategories; track category.workItemType + category.reasonCode + category.severityCode) {
                  <div class="compact-row">
                    <div>
                      <strong>{{ workItemTypeLabel(category.workItemType) }}</strong>
                      <span>{{ reasonLabel(category.reasonCode) }} - {{ severityLabel(category.severityCode) }}</span>
                    </div>
                    <b>{{ category.totalCount | number }} <small>pend.</small></b>
                  </div>
                }
              </div>
            }
          </section>
        </div>

        @if (summary.documentClasses.length > 0) {
          <section class="summary-section class-section">
            <div class="section-heading">
              <h4>Por clase</h4>
              <span>{{ summary.documentClasses.length | number }} clases</span>
            </div>
            <div class="class-grid">
              @for (documentClass of summary.documentClasses; track documentClass.documentClassCode) {
                <article class="class-card">
                  <strong>{{ documentClassLabel(documentClass.documentClassCode) }}</strong>
                  <dl>
                    <div>
                      <dt>Docs</dt>
                      <dd>{{ documentClass.totalDocuments | number }}</dd>
                    </div>
                    <div>
                      <dt>Vigentes</dt>
                      <dd>{{ documentClass.activeDocuments | number }}</dd>
                    </div>
                    <div>
                      <dt>Archivados</dt>
                      <dd>{{ documentClass.archivedDocuments | number }}</dd>
                    </div>
                  </dl>
                </article>
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
      .summary-dashboard {
        display: grid;
        gap: 0.7rem;
        min-width: 0;
      }

      h4,
      p,
      dl,
      dd {
        margin: 0;
      }

      h4 {
        font-size: 0.98rem;
        line-height: 1.2;
        color: #123f3b;
      }

      .summary-section {
        display: grid;
        gap: 0.5rem;
        min-width: 0;
      }

      .section-heading {
        display: flex;
        align-items: baseline;
        justify-content: space-between;
        gap: 0.75rem;
        min-width: 0;
      }

      .section-heading span {
        color: #60716d;
        font-size: 0.78rem;
        font-weight: 800;
        white-space: nowrap;
      }

      .module-grid,
      .class-grid {
        display: grid;
        gap: 0.55rem;
      }

      .module-grid {
        grid-template-columns: repeat(auto-fit, minmax(18rem, 1fr));
      }

      .module-card,
      .class-card {
        min-width: 0;
        border: 1px solid rgba(35, 51, 47, 0.1);
        border-radius: 8px;
        background: #fbfaf6;
      }

      .module-card {
        display: grid;
        gap: 0.45rem;
        padding: 0.6rem;
      }

      .module-card header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 0.75rem;
        min-width: 0;
      }

      .module-card header strong,
      .class-card strong,
      .compact-row strong {
        color: #20332f;
        min-width: 0;
        overflow-wrap: anywhere;
      }

      .module-card header span {
        color: #0f766e;
        font-size: 0.78rem;
        font-weight: 900;
        white-space: nowrap;
      }

      .metric-grid {
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: 0.35rem;
      }

      .metric-grid div,
      .class-card dl div {
        display: grid;
        gap: 0.1rem;
        border-radius: 8px;
        background: #f6f5ef;
        min-width: 0;
      }

      .metric-grid div {
        padding: 0.4rem 0.45rem;
      }

      dt {
        color: #60716d;
        font-size: 0.68rem;
        font-weight: 800;
        line-height: 1.1;
        text-transform: uppercase;
      }

      dd {
        color: #123f3b;
        font-size: 1rem;
        font-weight: 900;
        line-height: 1.1;
      }

      .summary-pair {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 0.7rem;
        align-items: start;
      }

      .compact-list {
        display: grid;
        gap: 0.35rem;
      }

      .compact-row {
        display: grid;
        grid-template-columns: minmax(0, 1fr) auto;
        gap: 0.7rem;
        align-items: center;
        min-width: 0;
        border: 1px solid rgba(35, 51, 47, 0.08);
        border-radius: 8px;
        padding: 0.45rem 0.55rem;
        background: #fbfaf6;
      }

      .compact-row div {
        display: grid;
        gap: 0.1rem;
        min-width: 0;
      }

      .compact-row span {
        color: #60716d;
        font-size: 0.78rem;
        overflow-wrap: anywhere;
      }

      .compact-row b {
        color: #123f3b;
        font-size: 1.05rem;
        line-height: 1;
        white-space: nowrap;
      }

      .compact-row b small {
        color: #60716d;
        font-size: 0.7rem;
        font-weight: 800;
      }

      .class-grid {
        grid-template-columns: repeat(auto-fit, minmax(13rem, 1fr));
      }

      .class-card {
        display: grid;
        gap: 0.4rem;
        padding: 0.55rem;
      }

      .class-card dl {
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: 0.35rem;
      }

      .class-card dl div {
        padding: 0.35rem 0.4rem;
      }

      .empty-state {
        color: #60716d;
        overflow-wrap: anywhere;
      }

      .empty-state {
        border: 1px dashed rgba(35, 51, 47, 0.18);
        border-radius: 8px;
        padding: 0.55rem;
        background: #fbfaf6;
      }

      @media (max-width: 760px) {
        .summary-pair {
          grid-template-columns: 1fr;
        }

        .module-grid,
        .class-grid {
          grid-template-columns: 1fr;
        }

        .metric-grid {
          grid-template-columns: repeat(2, minmax(0, 1fr));
        }
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
