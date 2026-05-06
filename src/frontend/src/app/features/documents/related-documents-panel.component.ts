import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { DocumentCatalogItem, DocumentRequirement, DocumentTimelineEvent } from '../../core/models/document-catalog.models';
import { CatalogItem } from '../../core/models/shared-catalogs.models';
import { DocumentCatalogService } from '../../core/services/document-catalog.service';
import { DonationsService } from '../../core/services/donations.service';
import { FederationService } from '../../core/services/federation.service';
import { MarketsService } from '../../core/services/markets.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  selector: 'app-related-documents-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DecimalPipe],
  template: `
    <section class="related-documents-panel">
      <div class="related-documents-header">
        <div>
          <h4>{{ title }}</h4>
          <p>{{ subtitle }}</p>
        </div>
        <button type="button" class="ghost compact" [disabled]="isLoading()" (click)="reload()">
          Actualizar
        </button>
      </div>

      @if (errorMessage()) {
        <p class="related-alert error">{{ errorMessage() }}</p>
      }

      @if (requirement(); as state) {
        <div class="requirement-summary">
          <p class="requirement-pill" [class.is-complete]="state.isComplete === true">
            @if (!state.appliesRule) {
              Sin requisito documental minimo
            } @else if (state.isComplete) {
              Requisito documental cumplido
            } @else {
              {{ state.missingReasonDescription }}
            }
          </p>
          @if (state.appliesRule) {
            <p>
              Regla {{ state.ruleCode }}
              · {{ state.currentDocumentCount }} de {{ state.minimumRequiredCount }}
              · clases {{ state.requiredDocumentClassCodes.join(', ') }}
            </p>
            @if (state.isComplete === false && state.remediationHint) {
              <p class="remediation-hint">{{ state.remediationHint }}</p>
            }
            @if (shouldShowRemediation(state)) {
              <form class="remediation-form" (submit)="submitRemediation($event)">
                <div class="remediation-title">
                  <strong>Resolver faltante</strong>
                  <span>{{ remediationTitle() }}</span>
                </div>

                @if (requiresEvidenceType()) {
                  <label>
                    <span>Tipo de evidencia</span>
                    <select [value]="remediationEvidenceTypeId()" (change)="setRemediationEvidenceType($event)">
                      <option value="0">Selecciona un tipo</option>
                      @for (type of activeRemediationEvidenceTypes(); track type.id) {
                        <option [value]="type.id">{{ type.name }}</option>
                      }
                    </select>
                  </label>
                }

                <label>
                  <span>Archivo</span>
                  <input type="file" [attr.accept]="remediationAccept()" (change)="onRemediationFileSelected($event)" />
                </label>

                @if (requiresEvidenceType()) {
                  <label>
                    <span>Descripción breve</span>
                    <input
                      type="text"
                      maxlength="500"
                      [value]="remediationDescription()"
                      (input)="setRemediationDescription($event)"
                      placeholder="Contexto operativo de la evidencia" />
                  </label>
                }

                @if (remediationFileName()) {
                  <p class="remediation-file">{{ remediationFileName() }}</p>
                }

                @if (remediationErrorMessage()) {
                  <p class="related-alert error">{{ remediationErrorMessage() }}</p>
                }

                @if (remediationSuccessMessage()) {
                  <p class="related-alert success">{{ remediationSuccessMessage() }}</p>
                }

                <button type="submit" class="ghost compact" [disabled]="!canSubmitRemediation()">
                  {{ isRemediating() ? 'Cargando...' : 'Cargar documento' }}
                </button>
              </form>
            }
          }
        </div>
      }

      @if (isLoading()) {
        <p class="related-empty">Cargando documentos relacionados...</p>
      } @else if (documents().length === 0) {
        <p class="related-empty">{{ emptyMessage }}</p>
      } @else {
        <div class="related-documents-list">
          @for (document of documents(); track document.id) {
            <article class="related-document-row">
              <div>
                <h5>{{ document.originalFileName }}</h5>
                <p>
                  {{ document.documentClassCode }}
                  · {{ document.statusCode }}
                  @if (document.isSuperseded) {
                    · reemplazado
                  } @else if (document.replacedDocumentId) {
                    · reemplaza documento previo
                  }
                  · {{ document.sizeBytes / 1024 | number: '1.0-0' }} KB
                </p>
                @if (document.originContext.summary) {
                  <p>{{ document.originContext.summary }}</p>
                }
              </div>
              <button type="button" class="ghost compact" (click)="download(document)">
                Descargar
              </button>
            </article>
          }
        </div>
      }

      @if (timeline().length > 0) {
        <div class="entity-timeline">
          <h5>Historia documental</h5>
          <ol>
            @for (event of timeline(); track event.occurredUtc + event.eventType + event.documentId) {
              <li>
                <time>{{ event.occurredUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</time>
                <strong>{{ event.title }}</strong>
                <span>{{ event.documentOriginalFileName }}</span>
              </li>
            }
          </ol>
        </div>
      }
    </section>
  `,
  styles: [
    `
      .related-documents-panel {
        display: grid;
        gap: 0.85rem;
        margin-top: 1rem;
        padding-top: 1rem;
        border-top: 1px solid rgba(15, 118, 110, 0.16);
      }

      .related-documents-header,
      .related-document-row {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        align-items: flex-start;
      }

      h4,
      h5,
      p {
        margin: 0;
      }

      h4 {
        font-size: 0.96rem;
      }

      h5 {
        font-size: 0.9rem;
      }

      p {
        color: #66756f;
        font-size: 0.84rem;
        line-height: 1.45;
      }

      .related-documents-list {
        display: grid;
        gap: 0.75rem;
      }

      .related-document-row {
        padding: 0.85rem 0;
        border-top: 1px solid rgba(29, 45, 42, 0.08);
      }

      .related-document-row:first-child {
        border-top: 0;
        padding-top: 0;
      }

      .entity-timeline {
        display: grid;
        gap: 0.5rem;
        padding-top: 0.75rem;
        border-top: 1px solid rgba(15, 118, 110, 0.12);
      }

      .entity-timeline ol {
        display: grid;
        gap: 0.5rem;
        margin: 0;
        padding-left: 1.15rem;
      }

      .entity-timeline li {
        display: grid;
        gap: 0.15rem;
      }

      .entity-timeline time,
      .entity-timeline span {
        color: #66756f;
        font-size: 0.78rem;
      }

      button.ghost {
        border: 1px solid rgba(15, 118, 110, 0.25);
        background: rgba(15, 118, 110, 0.08);
        color: #0f766e;
        border-radius: 999px;
        padding: 0.5rem 0.85rem;
        font-weight: 700;
        cursor: pointer;
      }

      button.compact {
        white-space: nowrap;
        padding-inline: 0.7rem;
      }

      button:disabled {
        cursor: not-allowed;
        opacity: 0.55;
      }

      .related-empty,
      .related-alert {
        border-radius: 0.8rem;
        padding: 0.75rem 0.9rem;
      }

      .related-empty {
        background: rgba(29, 45, 42, 0.05);
      }

      .requirement-pill {
        width: fit-content;
        border-radius: 999px;
        padding: 0.35rem 0.65rem;
        color: #92400e;
        background: rgba(146, 64, 14, 0.1);
        font-weight: 800;
      }

      .requirement-pill.is-complete {
        color: #166534;
        background: rgba(22, 101, 52, 0.1);
      }

      .requirement-summary {
        display: grid;
        gap: 0.35rem;
      }

      .remediation-hint {
        color: #7c2d12;
        font-weight: 700;
      }

      .remediation-form {
        display: grid;
        gap: 0.65rem;
        max-width: 36rem;
        padding: 0.85rem;
        border: 1px solid rgba(146, 64, 14, 0.18);
        border-radius: 0.75rem;
        background: rgba(146, 64, 14, 0.04);
      }

      .remediation-title {
        display: grid;
        gap: 0.15rem;
      }

      .remediation-title strong {
        color: #203734;
        font-size: 0.9rem;
      }

      .remediation-title span,
      label span {
        color: #66756f;
        font-size: 0.82rem;
        font-weight: 700;
      }

      label {
        display: grid;
        gap: 0.35rem;
      }

      input,
      select {
        width: 100%;
        border: 1px solid rgba(29, 45, 42, 0.18);
        border-radius: 0.65rem;
        padding: 0.58rem 0.7rem;
        background: #fff;
        color: #203734;
        font: inherit;
      }

      .remediation-file {
        font-weight: 700;
      }

      .related-alert.error {
        background: rgba(190, 18, 60, 0.1);
        color: #be123c;
      }

      .related-alert.success {
        background: rgba(15, 118, 110, 0.1);
        color: #0f766e;
      }

      @media (max-width: 720px) {
        .related-documents-header,
        .related-document-row {
          display: grid;
        }

        button.compact {
          width: fit-content;
        }
      }
    `
  ]
})
export class RelatedDocumentsPanelComponent implements OnChanges {
  @Input() moduleCode = '';
  @Input() entityType = '';
  @Input() entityId: string | null = null;
  @Input() title = 'Documentos relacionados';
  @Input() subtitle = 'Consulta transversal asociada a esta entidad.';
  @Input() emptyMessage = 'No hay documentos relacionados.';
  @Input() canRemediate = false;
  @Input() remediationEvidenceTypes: CatalogItem[] = [];
  @Output() remediated = new EventEmitter<void>();

  protected readonly documents = signal<DocumentCatalogItem[]>([]);
  protected readonly requirement = signal<DocumentRequirement | null>(null);
  protected readonly timeline = signal<DocumentTimelineEvent[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly remediationFile = signal<File | null>(null);
  protected readonly remediationEvidenceTypeId = signal(0);
  protected readonly remediationDescription = signal('');
  protected readonly remediationErrorMessage = signal<string | null>(null);
  protected readonly remediationSuccessMessage = signal<string | null>(null);
  protected readonly isRemediating = signal(false);
  protected readonly remediationFileName = computed(() => this.remediationFile()?.name ?? null);

  private readonly documentCatalogService = inject(DocumentCatalogService);
  private readonly marketsService = inject(MarketsService);
  private readonly donationsService = inject(DonationsService);
  private readonly federationService = inject(FederationService);

  ngOnChanges(_changes: SimpleChanges): void {
    this.ensureDefaultEvidenceType();
    void this.reload();
  }

  protected async reload(): Promise<void> {
    if (!this.moduleCode || !this.entityType || !this.entityId) {
      this.documents.set([]);
      this.requirement.set(null);
      this.timeline.set([]);
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    try {
      const [response, requirement, timeline] = await Promise.all([
        firstValueFrom(this.documentCatalogService.listDocumentsByEntity(
          this.moduleCode,
          this.entityType,
          this.entityId,
          { take: 20 })),
        firstValueFrom(this.documentCatalogService.getRequirementsByEntity(
          this.moduleCode,
          this.entityType,
          this.entityId)),
        firstValueFrom(this.documentCatalogService.getEntityTimeline(
          this.moduleCode,
          this.entityType,
          this.entityId,
          { take: 20 }))
      ]);
      this.documents.set(response.items);
      this.requirement.set(requirement);
      this.timeline.set(timeline.items);
    } catch (error) {
      this.documents.set([]);
      this.requirement.set(null);
      this.timeline.set([]);
      this.errorMessage.set(getApiErrorMessage(error, 'No se pudieron cargar los documentos relacionados.'));
    } finally {
      this.isLoading.set(false);
    }
  }

  protected async download(document: DocumentCatalogItem): Promise<void> {
    this.errorMessage.set(null);

    try {
      await this.documentCatalogService.downloadDocument(document);
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error, 'No se pudo descargar el documento.'));
    }
  }

  protected shouldShowRemediation(requirement: DocumentRequirement): boolean {
    return this.canRemediate
      && requirement.appliesRule
      && requirement.isComplete === false
      && this.supportsContextualRemediation();
  }

  protected requiresEvidenceType(): boolean {
    return this.normalizedEntityType() === 'DONATION_APPLICATION'
      || this.normalizedEntityType() === 'FEDERATION_DONATION_APPLICATION';
  }

  protected remediationAccept(): string {
    return '.pdf,.jpg,.jpeg,.png,.mp4,application/pdf,image/jpeg,image/png,video/mp4';
  }

  protected remediationTitle(): string {
    switch (this.normalizedEntityType()) {
      case 'MARKET_TENANT':
        return 'Carga la cédula o certificado del locatario.';
      case 'DONATION_APPLICATION':
      case 'FEDERATION_DONATION_APPLICATION':
        return 'Carga una evidencia documental para la aplicación.';
      default:
        return 'Carga el documento requerido.';
    }
  }

  protected activeRemediationEvidenceTypes(): CatalogItem[] {
    return this.remediationEvidenceTypes
      .filter((item) => item.isActive)
      .sort((left, right) => left.sortOrder - right.sortOrder || left.name.localeCompare(right.name));
  }

  protected onRemediationFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.remediationFile.set(input.files?.item(0) ?? null);
    this.remediationErrorMessage.set(null);
    this.remediationSuccessMessage.set(null);
  }

  protected setRemediationEvidenceType(event: Event): void {
    const input = event.target as HTMLSelectElement;
    this.remediationEvidenceTypeId.set(Number(input.value) || 0);
    this.remediationErrorMessage.set(null);
  }

  protected setRemediationDescription(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.remediationDescription.set(input.value);
  }

  protected canSubmitRemediation(): boolean {
    if (this.isRemediating() || !this.remediationFile()) {
      return false;
    }

    return !this.requiresEvidenceType() || this.remediationEvidenceTypeId() > 0;
  }

  protected async submitRemediation(event: Event): Promise<void> {
    event.preventDefault();

    const file = this.remediationFile();
    const entityId = this.entityId;
    if (!entityId || !file) {
      this.remediationErrorMessage.set('Selecciona un archivo para resolver el faltante.');
      return;
    }

    if (this.requiresEvidenceType() && this.remediationEvidenceTypeId() <= 0) {
      this.remediationErrorMessage.set('Selecciona el tipo de evidencia.');
      return;
    }

    this.isRemediating.set(true);
    this.remediationErrorMessage.set(null);
    this.remediationSuccessMessage.set(null);

    try {
      switch (this.normalizedEntityType()) {
        case 'MARKET_TENANT':
          await firstValueFrom(this.marketsService.uploadTenantCertificate(entityId, { certificateFile: file }));
          break;
        case 'DONATION_APPLICATION':
          await firstValueFrom(this.donationsService.createApplicationEvidence(entityId, {
            evidenceTypeId: this.remediationEvidenceTypeId(),
            description: this.normalizeOptional(this.remediationDescription()) ?? 'Remediación documental contextual.',
            file
          }));
          break;
        case 'FEDERATION_DONATION_APPLICATION':
          await firstValueFrom(this.federationService.createApplicationEvidence(entityId, {
            evidenceTypeId: this.remediationEvidenceTypeId(),
            description: this.normalizeOptional(this.remediationDescription()) ?? 'Remediación documental contextual.',
            file
          }));
          break;
        default:
          throw new Error('La entidad no soporta remediación contextual.');
      }

      this.clearRemediationForm();
      this.remediationSuccessMessage.set('Documento cargado. El estado documental se actualizó.');
      await this.reload();
      this.remediated.emit();
    } catch (error) {
      this.remediationErrorMessage.set(getApiErrorMessage(error, 'No fue posible resolver el faltante documental.'));
    } finally {
      this.isRemediating.set(false);
    }
  }

  private supportsContextualRemediation(): boolean {
    const entityType = this.normalizedEntityType();
    return (this.normalizedModuleCode() === 'MARKETS' && entityType === 'MARKET_TENANT')
      || (this.normalizedModuleCode() === 'DONATARIAS' && entityType === 'DONATION_APPLICATION')
      || (this.normalizedModuleCode() === 'FEDERATION' && entityType === 'FEDERATION_DONATION_APPLICATION');
  }

  private ensureDefaultEvidenceType(): void {
    if (!this.requiresEvidenceType() || this.remediationEvidenceTypeId() > 0) {
      return;
    }

    this.remediationEvidenceTypeId.set(this.activeRemediationEvidenceTypes()[0]?.id ?? 0);
  }

  private clearRemediationForm(): void {
    this.remediationFile.set(null);
    this.remediationDescription.set('');
    this.ensureDefaultEvidenceType();
  }

  private normalizedModuleCode(): string {
    return this.moduleCode.trim().toUpperCase();
  }

  private normalizedEntityType(): string {
    return this.entityType.trim().toUpperCase();
  }

  private normalizeOptional(value: string): string | null {
    const normalized = value.trim();
    return normalized.length > 0 ? normalized : null;
  }
}
