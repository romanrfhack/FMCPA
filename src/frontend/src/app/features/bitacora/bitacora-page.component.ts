import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { BitacoraEntry, DocumentIntegrityResponse } from '../../core/models/closeout.models';
import { CloseoutService } from '../../core/services/closeout.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  selector: 'app-bitacora-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, ReactiveFormsModule, RouterLink],
  template: `
    <section class="page-shell">
      <article class="hero-card">
        <p class="page-kicker">TRACK 1 POST-MVP</p>
        <h2>Bitácora transversal real</h2>
        <p>
          Cobertura mínima real desde el hardening inicial: altas nuevas, cargas relevantes, cierres
          formales y cierres retrospectivos normalizados capturados en una bitácora transversal compartida.
        </p>
      </article>

      @if (pageError()) {
        <p class="alert error">{{ pageError() }}</p>
      }

      <div class="page-grid">
        <aside class="sidebar">
          <article class="card">
            <div class="card-header">
              <div>
                <h3>Filtro</h3>
                <p>Consulta por módulo o por texto libre sobre el detalle del evento.</p>
              </div>
            </div>

            <form class="form-grid" [formGroup]="filtersForm" (ngSubmit)="applyFilters()">
              <label>
                <span>Módulo</span>
                <select formControlName="moduleCode">
                  <option value="">Todos</option>
                  <option value="MARKETS">Mercados</option>
                  <option value="DONATARIAS">Donatarias</option>
                  <option value="FINANCIALS">Financieras</option>
                  <option value="FEDERATION">Federación</option>
                </select>
              </label>

              <label>
                <span>Entidad</span>
                <select formControlName="entityType">
                  <option value="">Todas</option>
                  <option value="MARKET">Mercado</option>
                  <option value="MARKET_TENANT">Locatario</option>
                  <option value="MARKET_ISSUE">Incidencia</option>
                  <option value="DONATION">Donación</option>
                  <option value="DONATION_APPLICATION">Aplicación de donación</option>
                  <option value="DONATION_APPLICATION_EVIDENCE">Evidencia de donación</option>
                  <option value="FINANCIAL_PERMIT">Oficio</option>
                  <option value="FINANCIAL_CREDIT">Crédito</option>
                  <option value="FINANCIAL_CREDIT_COMMISSION">Comisión financiera</option>
                  <option value="FEDERATION_ACTION">Gestión</option>
                  <option value="FEDERATION_ACTION_PARTICIPANT">Participante</option>
                  <option value="FEDERATION_DONATION">Donación de federación</option>
                  <option value="FEDERATION_DONATION_APPLICATION">Aplicación de federación</option>
                  <option value="FEDERATION_DONATION_APPLICATION_EVIDENCE">Evidencia de federación</option>
                  <option value="FEDERATION_DONATION_APPLICATION_COMMISSION">Comisión de federación</option>
                </select>
              </label>

              <label>
                <span>Texto</span>
                <input type="text" formControlName="q" placeholder="Buscar por referencia, nombre o detalle" />
              </label>

              <label>
                <span>Desde</span>
                <input type="date" formControlName="fromDate" />
              </label>

              <label>
                <span>Hasta</span>
                <input type="date" formControlName="toDate" />
              </label>

              <label>
                <span>Máximo visible</span>
                <input type="number" min="20" max="300" step="10" formControlName="take" />
              </label>

              <div class="form-actions">
                <button type="submit">Aplicar filtro</button>
                <button type="button" class="ghost" (click)="clearFilters()">Limpiar</button>
              </div>
            </form>
          </article>
        </aside>

        <div class="detail-column">
          <article class="card">
            <div class="card-header">
              <div>
                <h3>Eventos visibles</h3>
                <p>Eventos reales nuevos, cierres formales y regularizaciones retrospectivas. La trazabilidad derivada previa sigue documentada aparte.</p>
              </div>
              <button type="button" class="ghost" (click)="reloadPage()">Actualizar</button>
            </div>

            @if (isLoading()) {
              <p class="empty-state">Cargando bitácora operativa...</p>
            } @else if (entries().length === 0) {
              <p class="empty-state">No hay eventos para el filtro seleccionado.</p>
            } @else {
              <div class="timeline">
                @for (entry of entries(); track entry.eventKey) {
                  <article class="timeline-item">
                    <div class="row-top">
                      <div>
                        <strong>{{ entry.title }}</strong>
                        <p>
                          {{ entry.moduleName }} · {{ entry.entityType }} · {{ entry.actionType }}
                          @if (entry.relatedStatusCode) {
                            · Estatus {{ entry.relatedStatusCode }}
                          }
                        </p>
                      </div>
                      <div class="timeline-flags">
                        @if (entry.isCloseEvent) {
                          <span class="status-pill close">{{ getCloseBadgeLabel(entry) }}</span>
                        }
                        <span class="timestamp">{{ entry.occurredUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</span>
                      </div>
                    </div>

                    <p class="detail">{{ entry.detail }}</p>

                    @if (entry.reference) {
                      <p class="reference">Referencia: {{ entry.reference }}</p>
                    }

                    @if (entry.metadataJson) {
                      <p class="meta">Metadata serializable registrada en bitácora.</p>
                    }

                    <a [routerLink]="entry.navigationPath">Abrir módulo</a>
                  </article>
                }
              </div>
            }
          </article>

          <article class="card">
            <div class="card-header">
              <div>
                <h3>Integridad documental mínima</h3>
                <p>Resumen operativo del storage actual por módulo y de los registros documentales transversales.</p>
              </div>
            </div>

            @if (documentIntegrity()) {
              <div class="integrity-summary">
                <div>
                  <span>Total revisado</span>
                  <strong>{{ documentIntegrity()!.summary.totalDocumentRecords }}</strong>
                </div>
                <div>
                  <span>Válidos</span>
                  <strong>{{ documentIntegrity()!.summary.validCount }}</strong>
                </div>
                <div>
                  <span>Archivo faltante</span>
                  <strong>{{ documentIntegrity()!.summary.missingFileCount }}</strong>
                </div>
                <div>
                  <span>Sin registro</span>
                  <strong>{{ documentIntegrity()!.summary.missingDocumentRecordCount }}</strong>
                </div>
                <div>
                  <span>Huérfanos</span>
                  <strong>{{ documentIntegrity()!.summary.orphanedDocumentRecordCount }}</strong>
                </div>
                <div>
                  <span>Metadatos no alineados</span>
                  <strong>{{ documentIntegrity()!.summary.metadataMismatchCount }}</strong>
                </div>
              </div>

              @if (documentIntegrity()!.issues.length === 0) {
                <p class="empty-state">No se detectaron inconsistencias documentales para el filtro actual.</p>
              } @else {
                <div class="integrity-issues">
                  @for (issue of documentIntegrity()!.issues; track issue.issueKey) {
                    <article class="timeline-item">
                      <div class="row-top">
                        <div>
                          <strong>{{ issue.title }}</strong>
                          <p>{{ issue.moduleName }} · {{ issue.entityType }} · {{ issue.integrityState }}</p>
                        </div>
                        <span class="status-pill integrity">{{ issue.integrityState }}</span>
                      </div>
                      <p class="detail">{{ issue.detail }}</p>
                      @if (issue.originalFileName) {
                        <p class="reference">Archivo: {{ issue.originalFileName }}</p>
                      }
                      @if (issue.storedRelativePath) {
                        <p class="meta">Ruta relativa: {{ issue.storedRelativePath }}</p>
                      }
                      <a [routerLink]="issue.navigationPath">Abrir módulo</a>
                    </article>
                  }
                </div>
              }
            }
          </article>
        </div>
      </div>
    </section>
  `,
  styles: [
    `
      .page-shell,
      .page-grid {
        display: grid;
        gap: 1rem;
      }

      .page-grid {
        grid-template-columns: minmax(17rem, 22rem) minmax(0, 1fr);
      }

      .hero-card,
      .card,
      .timeline-item {
        padding: 1.5rem;
        border-radius: 1.35rem;
        background: rgba(255, 255, 255, 0.82);
        border: 1px solid rgba(29, 45, 42, 0.08);
        box-shadow: 0 16px 30px rgba(32, 44, 41, 0.06);
      }

      .page-kicker {
        margin: 0 0 0.45rem;
        letter-spacing: 0.12em;
        text-transform: uppercase;
        font-size: 0.78rem;
        font-weight: 700;
        color: #0f766e;
      }

      .card-header,
      .row-top {
        display: flex;
        align-items: start;
        justify-content: space-between;
        gap: 1rem;
      }

      .form-grid,
      .timeline {
        display: grid;
        gap: 0.9rem;
      }

      label {
        display: grid;
        gap: 0.35rem;
      }

      input,
      select,
      button {
        border-radius: 0.85rem;
      }

      .form-actions {
        display: flex;
        gap: 0.75rem;
      }

      .timeline {
        margin-top: 1rem;
      }

      .integrity-summary {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(9rem, 1fr));
        gap: 0.75rem;
        margin-top: 1rem;
        margin-bottom: 1rem;
      }

      .integrity-summary div {
        display: grid;
        gap: 0.15rem;
        padding: 0.85rem 0.95rem;
        border-radius: 1rem;
        background: rgba(243, 244, 246, 0.82);
      }

      .integrity-summary span {
        font-size: 0.8rem;
        color: #667085;
      }

      .integrity-summary strong {
        font-size: 1.2rem;
      }

      .integrity-issues {
        display: grid;
        gap: 0.9rem;
      }

      .timeline-item {
        padding: 1rem 1.1rem;
      }

      .timeline-item p {
        margin: 0.3rem 0 0;
        color: #5b6b68;
      }

      .timeline-flags {
        display: grid;
        justify-items: end;
        gap: 0.45rem;
      }

      .detail {
        color: #344054;
      }

      .timestamp {
        white-space: nowrap;
        font-size: 0.85rem;
        color: #667085;
      }

      .reference {
        font-weight: 600;
      }

      .meta {
        font-size: 0.88rem;
      }

      .status-pill {
        display: inline-flex;
        align-items: center;
        padding: 0.3rem 0.65rem;
        border-radius: 999px;
        font-size: 0.72rem;
        font-weight: 700;
        letter-spacing: 0.08em;
        text-transform: uppercase;
      }

      .status-pill.close {
        background: rgba(180, 35, 24, 0.12);
        color: #b42318;
      }

      .status-pill.integrity {
        background: rgba(180, 83, 9, 0.12);
        color: #b45309;
      }

      .alert {
        padding: 0.9rem 1rem;
        border-radius: 0.9rem;
        font-weight: 600;
      }

      .alert.error {
        background: rgba(254, 243, 242, 0.9);
        color: #b42318;
      }

      .empty-state {
        margin: 0;
        color: #5b6b68;
      }

      .ghost {
        border: 1px solid rgba(15, 118, 110, 0.18);
        background: transparent;
        color: #0f766e;
      }

      @media (max-width: 980px) {
        .page-grid {
          grid-template-columns: 1fr;
        }
      }
    `
  ]
})
export class BitacoraPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly closeoutService = inject(CloseoutService);

  protected readonly isLoading = signal(true);
  protected readonly pageError = signal('');
  protected readonly entries = signal<BitacoraEntry[]>([]);
  protected readonly documentIntegrity = signal<DocumentIntegrityResponse | null>(null);

  protected readonly filtersForm = this.formBuilder.nonNullable.group({
    moduleCode: [''],
    entityType: [''],
    fromDate: [''],
    toDate: [''],
    q: [''],
    take: [120]
  });

  constructor() {
    void this.loadEntries();
  }

  protected async applyFilters() {
    await this.loadEntries();
  }

  protected async clearFilters() {
    this.filtersForm.reset({
      moduleCode: '',
      entityType: '',
      fromDate: '',
      toDate: '',
      q: '',
      take: 120
    });

    await this.loadEntries();
  }

  protected async reloadPage() {
    await this.loadEntries();
  }

  protected getCloseBadgeLabel(entry: BitacoraEntry) {
    return entry.closeEventSource === 'LEGACY_CLOSE_NORMALIZED'
      ? 'Cierre retrospectivo'
      : 'Cierre formal';
  }

  private async loadEntries() {
    this.isLoading.set(true);
    this.pageError.set('');

    try {
      const filters = this.filtersForm.getRawValue();
      const [entries, documentIntegrity] = await Promise.all([
        firstValueFrom(
          this.closeoutService.getBitacora({
            moduleCode: filters.moduleCode || null,
            entityType: filters.entityType || null,
            fromDate: filters.fromDate || null,
            toDate: filters.toDate || null,
            q: filters.q || null,
            take: filters.take
          })),
        firstValueFrom(
          this.closeoutService.getDocumentIntegrity({
            moduleCode: filters.moduleCode || null,
            entityType: filters.entityType || null,
            take: filters.take
          }))
      ]);

      this.entries.set(entries);
      this.documentIntegrity.set(documentIntegrity);
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible cargar la bitácora transversal real.'));
    } finally {
      this.isLoading.set(false);
    }
  }
}
