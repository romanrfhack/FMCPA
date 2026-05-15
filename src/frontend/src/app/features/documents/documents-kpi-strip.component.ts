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
        <div>
          <span>Total</span>
          <strong>{{ summary.totalDocuments | number }}</strong>
        </div>
        <div>
          <span>Vigentes</span>
          <strong>{{ summary.activeDocuments | number }}</strong>
        </div>
        <div>
          <span>Archivados</span>
          <strong>{{ summary.archivedDocuments | number }}</strong>
        </div>
        <div>
          <span>Integridad</span>
          <strong>{{ summary.integrityIssuesCount | number }}</strong>
        </div>
        <div>
          <span>Incompletos</span>
          <strong>{{ summary.incompleteEntitiesCount | number }}</strong>
        </div>
        <div>
          <span>Por revisar</span>
          <strong>{{ summary.reviewDueCount | number }}</strong>
        </div>
        <div>
          <span>Retencion vencida</span>
          <strong>{{ summary.expiredRetentionCount | number }}</strong>
        </div>
        <div>
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
        margin-bottom: 1rem;
      }

      .kpi-grid div {
        min-width: 0;
        border-radius: 8px;
        padding: 0.75rem;
        background: #f6f5ef;
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
