import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';

import { DocumentCompleteness } from '../../core/models/document-catalog.models';

@Component({
  selector: 'app-documents-pending-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (isLoading) {
      <p class="empty-state">Cargando pendientes documentales...</p>
    } @else if (pendingItems.length === 0) {
      <p class="empty-state">No hay pendientes documentales con el filtro actual.</p>
    } @else {
      <div class="pending-list">
        @for (item of pendingItems; track item.moduleCode + item.entityId) {
          <article class="pending-row">
            <div>
              <span class="badge">{{ item.moduleName }}</span>
              <h4>{{ item.originContext.displayName }}</h4>
              <p>{{ entityTypeLabel(item.originContext.entityType) }} · {{ item.originContext.entityId }}</p>
              <p>{{ item.missingReasonDescription || item.requiredDocumentDescription }}</p>
              <p>
                Minimo requerido {{ item.minimumRequiredCount }}
                · {{ documentClassListLabel(item.requiredDocumentClassCodes) }}
              </p>
              @if (item.remediationHint) {
                <p>{{ item.remediationHint }}</p>
              }
            </div>
            <div class="pending-row-actions">
              <span class="pending-status">{{ completenessStatusLabel(item.statusCode) }}</span>
              <button type="button" class="ghost compact" (click)="filterByEntity.emit(item)">
                Ver documentos
              </button>
              @if (item.originContext.routeHint) {
                <a class="origin-link" [href]="item.originContext.routeHint">
                  {{ canRemediateItem(item) ? 'Resolver en origen' : 'Abrir origen' }}
                </a>
              }
            </div>
          </article>
        }
      </div>
    }
  `,
  styles: [
    `
      p,
      h4 {
        margin: 0;
      }

      .empty-state {
        color: #60716d;
      }

      .pending-list {
        display: grid;
        gap: 0.75rem;
      }

      .pending-row {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        min-width: 0;
        padding: 0.95rem 0;
        border-top: 1px solid rgba(35, 51, 47, 0.08);
      }

      .pending-row:first-child {
        border-top: 0;
        padding-top: 0;
      }

      .pending-row h4,
      .pending-row p {
        margin-top: 0.25rem;
      }

      .pending-row-actions {
        display: grid;
        gap: 0.5rem;
        justify-items: end;
        align-content: start;
      }

      .badge,
      .pending-status {
        width: fit-content;
        border-radius: 999px;
        padding: 0.2rem 0.5rem;
        font-size: 0.76rem;
        font-weight: 800;
      }

      .badge {
        color: #0f766e;
        background: rgba(15, 118, 110, 0.1);
      }

      .pending-status {
        color: #92400e;
        background: rgba(146, 64, 14, 0.1);
      }

      .origin-link {
        color: #0f766e;
        font-weight: 800;
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
        .pending-row {
          display: grid;
          grid-template-columns: 1fr;
        }
      }
    `
  ]
})
export class DocumentsPendingTableComponent {
  @Input() pendingItems: DocumentCompleteness[] = [];
  @Input() isLoading = false;
  @Input() canRemediateItem: (item: DocumentCompleteness) => boolean = () => false;

  @Output() filterByEntity = new EventEmitter<DocumentCompleteness>();

  protected documentClassListLabel(documentClassCodes: string[]): string {
    return documentClassCodes.map((documentClassCode) => this.documentClassLabel(documentClassCode)).join(', ');
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

  protected completenessStatusLabel(statusCode: string): string {
    switch (statusCode) {
      case 'COMPLETE':
        return 'Completo';
      case 'INCOMPLETE':
        return 'Pendiente';
      default:
        return statusCode;
    }
  }
}
