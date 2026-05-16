import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

import { DocumentSummary } from '../../core/models/document-catalog.models';

@Component({
  selector: 'app-documents-kpi-strip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe],
  template: `
    @if (summary) {
      <div class="kpi-grid">
        <div class="neutral">
          <span>Total</span>
          <strong>{{ summary.totalDocuments | number }}</strong>
        </div>
        <div class="neutral">
          <span>Vigentes</span>
          <strong>{{ summary.activeDocuments | number }}</strong>
        </div>
        <div class="neutral">
          <span>Archivados</span>
          <strong>{{ summary.archivedDocuments | number }}</strong>
        </div>
        <div class="critical" [class.clear]="summary.integrityIssuesCount === 0">
          <span>Integridad</span>
          <strong>{{ summary.integrityIssuesCount | number }}</strong>
        </div>
        <div class="critical" [class.clear]="summary.incompleteEntitiesCount === 0">
          <span>Incompletos</span>
          <strong>{{ summary.incompleteEntitiesCount | number }}</strong>
        </div>
        <div class="warning" [class.clear]="summary.reviewDueCount === 0">
          <span>Por revisar</span>
          <strong>{{ summary.reviewDueCount | number }}</strong>
        </div>
        <div class="critical" [class.clear]="summary.expiredRetentionCount === 0">
          <span>Retencion vencida</span>
          <strong>{{ summary.expiredRetentionCount | number }}</strong>
        </div>
        <div class="warning" [class.clear]="summary.administrativeHoldCount === 0">
          <span>En resguardo</span>
          <strong>{{ summary.administrativeHoldCount | number }}</strong>
        </div>
      </div>
    } @else if (isLoading) {
      <p class="empty-state">Actualizando resumen documental...</p>
    }
  `,
  styles: [
    `
      .kpi-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(7.5rem, 1fr));
        gap: 0.75rem;
      }

      .kpi-grid div {
        min-width: 0;
        border-radius: 8px;
        padding: 0.75rem;
        background: #f6f5ef;
      }

      .kpi-grid div.critical {
        background: rgba(159, 18, 57, 0.08);
      }

      .kpi-grid div.warning {
        background: rgba(146, 64, 14, 0.1);
      }

      .kpi-grid div.clear {
        background: rgba(22, 101, 52, 0.08);
      }

      .kpi-grid span,
      .empty-state {
        color: #60716d;
        overflow-wrap: anywhere;
      }

      .kpi-grid strong {
        display: block;
        margin-top: 0.25rem;
        color: #123f3b;
        font-size: 1.35rem;
      }
    `
  ]
})
export class DocumentsKpiStripComponent {
  @Input() summary: DocumentSummary | null = null;
  @Input() isLoading = false;
}
