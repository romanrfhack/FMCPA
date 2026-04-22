import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  CreateMarketIssueRequest,
  CreateMarketRequest,
  CreateMarketTenantRequest,
  MarketDetail,
  MarketIssue,
  MarketSummary,
  MarketTenant,
  MarketTenantAlert
} from '../../core/models/markets.models';
import { Contact, ModuleStatusCatalogEntry } from '../../core/models/shared-catalogs.models';
import { MarketsService } from '../../core/services/markets.service';
import { SharedCatalogsService } from '../../core/services/shared-catalogs.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  selector: 'app-markets-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, ReactiveFormsModule],
  template: `
    <section class="page-shell">
      <article class="hero-card">
        <p class="page-kicker">STAGE-03</p>
        <h2>Mercados</h2>
        <p>
          Primer modulo de negocio real sobre la base compartida de contactos y catalogos.
          Incluye mercados, locatarios, incidencias y alertas minimas de vigencia de cédulas.
        </p>
      </article>

      @if (pageError()) {
        <p class="alert error">{{ pageError() }}</p>
      }

      <div class="page-grid">
        <aside class="sidebar">
          <article class="filter-card">
            <div class="card-header">
              <div>
                <h3>Filtro</h3>
                <p>Reduce la lista por estatus o solo por alertas activas.</p>
              </div>
            </div>

            <form class="form-grid" [formGroup]="filtersForm" (ngSubmit)="applyFilters()">
              <label>
                <span>Estatus</span>
                <select formControlName="statusCode">
                  <option value="">Todos</option>
                  @for (status of marketStatuses(); track status.id) {
                    <option [value]="status.statusCode">{{ status.statusName }}</option>
                  }
                </select>
              </label>

              <label class="toggle">
                <input type="checkbox" formControlName="alertsOnly" />
                <span>Solo con alertas activas</span>
              </label>

              <div class="form-actions">
                <button type="submit">Aplicar filtro</button>
                <button type="button" class="ghost" (click)="clearFilters()">Limpiar</button>
              </div>
            </form>
          </article>

          <article class="form-card">
            <div class="card-header">
              <div>
                <h3>Alta de mercado</h3>
                <p>Registro base del mercado y su secretario general.</p>
              </div>
            </div>

            @if (marketFormError()) {
              <p class="alert error">{{ marketFormError() }}</p>
            }

            @if (marketFormSuccess()) {
              <p class="alert success">{{ marketFormSuccess() }}</p>
            }

            <form class="form-grid" [formGroup]="marketForm" (ngSubmit)="submitMarket()">
              <label>
                <span>Nombre</span>
                <input type="text" formControlName="name" placeholder="Mercado ejemplo" />
              </label>

              <label>
                <span>Alcaldia</span>
                <input type="text" formControlName="borough" placeholder="Alcaldia" />
              </label>

              <label>
                <span>Estatus</span>
                <select formControlName="statusCatalogEntryId">
                  <option [value]="0">Selecciona un estatus</option>
                  @for (status of marketStatuses(); track status.id) {
                    <option [value]="status.id">{{ status.statusName }}</option>
                  }
                </select>
              </label>

              <label>
                <span>Contacto secretario general</span>
                <select formControlName="secretaryGeneralContactId" (change)="syncSecretaryGeneralFromContact()">
                  <option value="">Sin vincular</option>
                  @for (contact of contacts(); track contact.id) {
                    <option [value]="contact.id">{{ contact.name }}</option>
                  }
                </select>
              </label>

              <label class="full-width">
                <span>Secretario general</span>
                <input type="text" formControlName="secretaryGeneralName" placeholder="Nombre del secretario general" />
              </label>

              <label class="full-width">
                <span>Observaciones</span>
                <textarea formControlName="notes" rows="4" placeholder="Observaciones del mercado"></textarea>
              </label>

              <div class="form-actions full-width">
                <button type="submit" [disabled]="isSubmittingMarket()">Registrar mercado</button>
                <button type="button" class="ghost" (click)="resetMarketForm()">Limpiar</button>
              </div>
            </form>
          </article>

          <article class="list-card">
            <div class="card-header">
              <div>
                <h3>Mercados</h3>
                <p>Lista operativa con conteos y alertas activas.</p>
              </div>
              <button type="button" class="ghost" (click)="reloadPage()">Actualizar</button>
            </div>

            @if (isBootstrapping()) {
              <p class="empty-state">Cargando modulo de Mercados...</p>
            } @else if (markets().length === 0) {
              <p class="empty-state">No hay mercados registrados con el filtro actual.</p>
            } @else {
              <div class="market-list">
                @for (market of markets(); track market.id) {
                  <button
                    type="button"
                    class="market-card"
                    [class.is-selected]="market.id === selectedMarketId()"
                    (click)="selectMarket(market.id)">
                    <div class="row-top">
                      <h4>{{ market.name }}</h4>
                      <span class="status-pill" [class]="marketStatusClass(market.statusCode)">
                        {{ market.statusName }}
                      </span>
                    </div>
                    <p class="meta">{{ market.borough }} · {{ market.secretaryGeneralName }}</p>
                    <div class="market-stats">
                      <span>Locatarios {{ market.tenantCount }}</span>
                      <span>Incidencias {{ market.issueCount }}</span>
                      <span>Alertas {{ market.activeTenantAlertsCount }}</span>
                    </div>
                  </button>
                }
              </div>
            }
          </article>

          <article class="list-card">
            <div class="card-header">
              <div>
                <h3>Alertas activas</h3>
                <p>Cédulas por vencer o vencidas en mercados operativos.</p>
              </div>
            </div>

            @if (tenantAlerts().length === 0) {
              <p class="empty-state">No hay alertas activas de vigencia.</p>
            } @else {
              <div class="alert-list">
                @for (alert of tenantAlerts(); track alert.tenantId) {
                  <article class="alert-row">
                    <div class="row-top">
                      <h4>{{ alert.tenantName }}</h4>
                      <span class="status-pill" [class]="tenantAlertClass(alert.alertState)">
                        {{ tenantAlertLabel(alert.alertState) }}
                      </span>
                    </div>
                    <p class="meta">{{ alert.marketName }} · Cédula {{ alert.certificateNumber }}</p>
                    <p class="meta">
                      Vigencia {{ alert.certificateValidityTo }}
                      · {{ expirationLabel(alert.daysUntilExpiration) }}
                    </p>
                  </article>
                }
              </div>
            }
          </article>
        </aside>

        <div class="detail-column">
          @if (selectedMarket(); as marketDetail) {
            <article class="detail-card">
              <div class="detail-header">
                <div>
                  <p class="page-kicker">Mercado seleccionado</p>
                  <h3>{{ marketDetail.name }}</h3>
                  <p class="meta">
                    {{ marketDetail.borough }} · Secretario general: {{ marketDetail.secretaryGeneralName }}
                  </p>
                </div>
                <div class="detail-badges">
                  <span class="status-pill" [class]="marketStatusClass(marketDetail.statusCode)">
                    {{ marketDetail.statusName }}
                  </span>
                  <span class="status-pill neutral">
                    Alertas {{ selectedMarketAlertCount() }}
                  </span>
                  <button
                    type="button"
                    class="ghost"
                    (click)="closeSelectedMarket()"
                    [disabled]="marketDetail.statusIsClosed"
                    [attr.title]="marketDetail.statusIsClosed ? 'El mercado ya se encuentra en estado terminal.' : 'Registrar cierre formal.'">
                    {{ marketDetail.statusIsClosed ? 'Ya terminal' : 'Cerrar formalmente' }}
                  </button>
                </div>
              </div>

              @if (marketDetail.notes) {
                <p class="detail-notes">{{ marketDetail.notes }}</p>
              }

              <div class="summary-grid">
                <article>
                  <h4>Locatarios</h4>
                  <p>{{ marketDetail.tenants.length }}</p>
                </article>
                <article>
                  <h4>Incidencias</h4>
                  <p>{{ marketDetail.issues.length }}</p>
                </article>
                <article>
                  <h4>Alta UTC</h4>
                  <p>{{ marketDetail.createdUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</p>
                </article>
              </div>
            </article>

            <div class="detail-grid">
              <article class="form-card">
                <div class="card-header">
                  <div>
                    <h3>Alta de locatario</h3>
                    <p>Cédula digitalizada con vigencia y datos operativos.</p>
                  </div>
                </div>

                @if (tenantFormError()) {
                  <p class="alert error">{{ tenantFormError() }}</p>
                }

                @if (tenantFormSuccess()) {
                  <p class="alert success">{{ tenantFormSuccess() }}</p>
                }

                <form class="form-grid" [formGroup]="tenantForm" (ngSubmit)="submitTenant()">
                  <label>
                    <span>Contacto compartido</span>
                    <select formControlName="contactId" (change)="syncTenantFromContact()">
                      <option value="">Sin vincular</option>
                      @for (contact of contacts(); track contact.id) {
                        <option [value]="contact.id">{{ contact.name }}</option>
                      }
                    </select>
                  </label>

                  <label>
                    <span>Locatario</span>
                    <input type="text" formControlName="tenantName" placeholder="Nombre del locatario" />
                  </label>

                  <label>
                    <span>Número de cédula</span>
                    <input type="text" formControlName="certificateNumber" placeholder="Número de cédula" />
                  </label>

                  <label>
                    <span>Vigencia</span>
                    <input type="date" formControlName="certificateValidityTo" />
                  </label>

                  <label>
                    <span>Giro</span>
                    <input type="text" formControlName="businessLine" placeholder="Giro comercial" />
                  </label>

                  <label>
                    <span>Celular</span>
                    <input type="text" formControlName="mobilePhone" placeholder="Celular" />
                  </label>

                  <label>
                    <span>WhatsApp</span>
                    <input type="text" formControlName="whatsAppPhone" placeholder="WhatsApp" />
                  </label>

                  <label>
                    <span>Correo</span>
                    <input type="email" formControlName="email" placeholder="correo@ejemplo.com" />
                  </label>

                  <label class="full-width">
                    <span>Cédula digitalizada</span>
                    <input type="file" accept=".pdf,.jpg,.jpeg,.png,.webp" (change)="onCertificateSelected($event)" />
                  </label>

                  @if (selectedCertificateFileName()) {
                    <p class="inline-note full-width">Archivo seleccionado: {{ selectedCertificateFileName() }}</p>
                  }

                  <label class="full-width">
                    <span>Observaciones</span>
                    <textarea formControlName="notes" rows="3" placeholder="Observaciones del locatario"></textarea>
                  </label>

                  <div class="form-actions full-width">
                    <button type="submit" [disabled]="isSubmittingTenant()">Registrar locatario</button>
                    <button type="button" class="ghost" (click)="resetTenantForm()">Limpiar</button>
                  </div>
                </form>
              </article>

              <article class="list-card">
                <div class="card-header">
                  <div>
                    <h3>Locatarios del mercado</h3>
                    <p>Vigencias visibles con indicador de alerta.</p>
                  </div>
                </div>

                @if (marketDetail.tenants.length === 0) {
                  <p class="empty-state">Aun no hay locatarios registrados.</p>
                } @else {
                  <div class="entity-list">
                    @for (tenant of marketDetail.tenants; track tenant.id) {
                      <article class="entity-row">
                        <div class="row-top">
                          <div>
                            <h4>{{ tenant.tenantName }}</h4>
                            <p class="meta">
                              Cédula {{ tenant.certificateNumber }} · {{ tenant.businessLine }}
                            </p>
                          </div>
                          <span class="status-pill" [class]="tenantAlertClass(tenant.certificateAlertState)">
                            {{ tenantAlertLabel(tenant.certificateAlertState) }}
                          </span>
                        </div>

                        <dl class="detail-grid-list">
                          <div>
                            <dt>Vigencia</dt>
                            <dd>{{ tenant.certificateValidityTo }}</dd>
                          </div>
                          <div>
                            <dt>Celular</dt>
                            <dd>{{ tenant.mobilePhone || 'Sin dato' }}</dd>
                          </div>
                          <div>
                            <dt>WhatsApp</dt>
                            <dd>{{ tenant.whatsAppPhone || 'Sin dato' }}</dd>
                          </div>
                          <div>
                            <dt>Correo</dt>
                            <dd>{{ tenant.email || 'Sin dato' }}</dd>
                          </div>
                        </dl>

                        <p class="meta">{{ expirationLabel(tenant.daysUntilExpiration) }}</p>

                        <div class="row-actions">
                          @if (tenant.hasDigitalCertificate) {
                            <a [href]="tenantCertificateUrl(tenant.id)" target="_blank" rel="noopener noreferrer">
                              Descargar cédula
                            </a>
                          }
                          @if (tenant.notes) {
                            <span>{{ tenant.notes }}</span>
                          }
                        </div>
                      </article>
                    }
                  </div>
                }
              </article>
            </div>

            <div class="detail-grid">
              <article class="form-card">
                <div class="card-header">
                  <div>
                    <h3>Alta de incidencia o mejora</h3>
                    <p>Seguimiento mínimo del mercado con estatus reusable.</p>
                  </div>
                </div>

                @if (issueFormError()) {
                  <p class="alert error">{{ issueFormError() }}</p>
                }

                @if (issueFormSuccess()) {
                  <p class="alert success">{{ issueFormSuccess() }}</p>
                }

                <form class="form-grid" [formGroup]="issueForm" (ngSubmit)="submitIssue()">
                  <label>
                    <span>Tipo</span>
                    <input type="text" formControlName="issueType" placeholder="Queja, mejora u observacion" />
                  </label>

                  <label>
                    <span>Fecha</span>
                    <input type="date" formControlName="issueDate" />
                  </label>

                  <label class="full-width">
                    <span>Descripcion</span>
                    <textarea formControlName="description" rows="4" placeholder="Descripcion de la incidencia o mejora"></textarea>
                  </label>

                  <label class="full-width">
                    <span>Avance</span>
                    <textarea formControlName="advanceSummary" rows="3" placeholder="Avance actual"></textarea>
                  </label>

                  <label>
                    <span>Estatus</span>
                    <select formControlName="statusCatalogEntryId">
                      <option [value]="0">Selecciona un estatus</option>
                      @for (status of issueStatuses(); track status.id) {
                        <option [value]="status.id">{{ status.statusName }}</option>
                      }
                    </select>
                  </label>

                  <label class="full-width">
                    <span>Seguimiento / resolución</span>
                    <textarea formControlName="followUpOrResolution" rows="3" placeholder="Seguimiento o resolucion"></textarea>
                  </label>

                  <label class="full-width">
                    <span>Satisfaccion final</span>
                    <input type="text" formControlName="finalSatisfaction" placeholder="Si aplica" />
                  </label>

                  <div class="form-actions full-width">
                    <button type="submit" [disabled]="isSubmittingIssue()">Registrar incidencia</button>
                    <button type="button" class="ghost" (click)="resetIssueForm()">Limpiar</button>
                  </div>
                </form>
              </article>

              <article class="list-card">
                <div class="card-header">
                  <div>
                    <h3>Incidencias del mercado</h3>
                    <p>Listado cronológico del seguimiento del mercado.</p>
                  </div>
                </div>

                @if (marketDetail.issues.length === 0) {
                  <p class="empty-state">Aun no hay incidencias registradas.</p>
                } @else {
                  <div class="entity-list">
                    @for (issue of marketDetail.issues; track issue.id) {
                      <article class="entity-row">
                        <div class="row-top">
                          <div>
                            <h4>{{ issue.issueType }}</h4>
                            <p class="meta">{{ issue.issueDate }} · {{ issue.statusName }}</p>
                          </div>
                          <span class="status-pill" [class]="issueStatusClass(issue)">
                            {{ issue.statusName }}
                          </span>
                        </div>

                        <p class="description">{{ issue.description }}</p>
                        <p class="meta"><strong>Avance:</strong> {{ issue.advanceSummary }}</p>

                        @if (issue.followUpOrResolution) {
                          <p class="meta"><strong>Seguimiento:</strong> {{ issue.followUpOrResolution }}</p>
                        }

                        @if (issue.finalSatisfaction) {
                          <p class="meta"><strong>Satisfaccion final:</strong> {{ issue.finalSatisfaction }}</p>
                        }
                      </article>
                    }
                  </div>
                }
              </article>
            </div>
          } @else {
            <article class="empty-card">
              <h3>Selecciona un mercado</h3>
              <p>
                Cuando exista al menos un mercado en la lista, su detalle quedara disponible aqui
                para alta de locatarios, incidencias y consulta de vigencias.
              </p>
            </article>
          }
        </div>
      </div>
    </section>
  `,
  styles: [
    `
      .page-shell,
      .sidebar,
      .detail-column,
      .detail-grid,
      .entity-list,
      .market-list,
      .alert-list {
        display: grid;
        gap: 1.25rem;
      }

      .page-grid {
        display: grid;
        grid-template-columns: minmax(22rem, 25rem) minmax(0, 1fr);
        gap: 1.25rem;
        align-items: start;
      }

      .detail-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
      }

      .hero-card,
      .filter-card,
      .form-card,
      .list-card,
      .detail-card,
      .empty-card {
        padding: 1.5rem;
        border-radius: 1.35rem;
        background: rgba(255, 255, 255, 0.82);
        border: 1px solid rgba(29, 45, 42, 0.08);
        box-shadow: 0 16px 30px rgba(32, 44, 41, 0.06);
      }

      .page-kicker {
        margin: 0 0 0.5rem;
        letter-spacing: 0.12em;
        text-transform: uppercase;
        font-size: 0.78rem;
        font-weight: 700;
        color: #0f766e;
      }

      h2,
      h3,
      h4,
      p,
      dd {
        margin: 0;
      }

      .hero-card p:last-child,
      .meta,
      .detail-notes,
      .empty-state,
      .description,
      .inline-note {
        margin-top: 0.75rem;
        line-height: 1.6;
        color: #4d615c;
      }

      .card-header,
      .row-top,
      .detail-header {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        align-items: flex-start;
      }

      .card-header {
        margin-bottom: 1rem;
      }

      .detail-badges,
      .market-stats,
      .row-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.55rem;
      }

      .summary-grid {
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: 0.75rem;
        margin-top: 1rem;
      }

      .summary-grid article {
        padding: 0.9rem;
        border-radius: 1rem;
        background: #f6f5ef;
      }

      .summary-grid h4 {
        font-size: 0.82rem;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        color: #5b6b68;
      }

      .summary-grid p {
        margin-top: 0.4rem;
        font-size: 1.05rem;
        font-weight: 700;
        color: #203734;
      }

      .form-grid {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 0.9rem;
      }

      label {
        display: grid;
        gap: 0.4rem;
        font-size: 0.92rem;
        font-weight: 600;
        color: #29403b;
      }

      input,
      select,
      textarea {
        width: 100%;
        padding: 0.8rem 0.9rem;
        border-radius: 0.9rem;
        border: 1px solid rgba(29, 45, 42, 0.14);
        background: #fbfbf8;
        color: #1d2d2a;
        font: inherit;
      }

      textarea {
        resize: vertical;
      }

      .toggle {
        grid-template-columns: auto 1fr;
        align-items: center;
        gap: 0.7rem;
        padding: 0.8rem 0.9rem;
        border-radius: 0.9rem;
        background: #f6f5ef;
      }

      .toggle input {
        width: auto;
        margin: 0;
      }

      .full-width {
        grid-column: 1 / -1;
      }

      .form-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
      }

      button,
      .market-card {
        border: none;
        border-radius: 0.9rem;
        padding: 0.8rem 1rem;
        font: inherit;
      }

      button {
        cursor: pointer;
        font-weight: 700;
        background: #123f3b;
        color: #f6f6f2;
      }

      button.ghost {
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
      }

      button:disabled {
        opacity: 0.75;
        cursor: wait;
      }

      .market-card,
      .entity-row,
      .alert-row {
        display: grid;
        gap: 0.7rem;
        padding: 1rem;
        border-radius: 1rem;
        background: #f6f5ef;
      }

      .market-card {
        text-align: left;
        cursor: pointer;
      }

      .market-card.is-selected {
        outline: 2px solid rgba(15, 118, 110, 0.35);
      }

      .market-stats span,
      .row-actions span,
      .row-actions a {
        padding: 0.45rem 0.65rem;
        border-radius: 999px;
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
        font-size: 0.82rem;
        text-decoration: none;
      }

      .status-pill {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        padding: 0.5rem 0.72rem;
        border-radius: 999px;
        font-size: 0.82rem;
        font-weight: 700;
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
      }

      .status-pill.market-active,
      .status-pill.issue-progress,
      .status-pill.valid {
        background: rgba(15, 118, 110, 0.12);
        color: #0f766e;
      }

      .status-pill.market-inactive {
        background: rgba(148, 98, 0, 0.12);
        color: #7a5400;
      }

      .status-pill.market-closed,
      .status-pill.issue-closed,
      .status-pill.neutral,
      .status-pill.alerts-disabled {
        background: rgba(91, 107, 104, 0.14);
        color: #40524f;
      }

      .status-pill.market-archived,
      .status-pill.issue-muted {
        background: rgba(82, 39, 255, 0.08);
        color: #5233a8;
      }

      .status-pill.due-soon {
        background: rgba(148, 98, 0, 0.16);
        color: #7a5400;
      }

      .status-pill.expired {
        background: rgba(180, 35, 24, 0.12);
        color: #b42318;
      }

      .alert {
        padding: 0.85rem 0.95rem;
        border-radius: 0.9rem;
        font-weight: 600;
      }

      .alert.error {
        background: rgba(180, 35, 24, 0.08);
        color: #b42318;
      }

      .alert.success {
        background: rgba(15, 118, 110, 0.1);
        color: #0f766e;
      }

      .detail-grid-list {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(10rem, 1fr));
        gap: 0.8rem;
        margin: 0;
      }

      dt {
        font-size: 0.78rem;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        color: #5b6b68;
      }

      dd {
        margin-top: 0.3rem;
        color: #203734;
      }

      .empty-card p {
        margin-top: 0.85rem;
        line-height: 1.6;
        color: #4d615c;
      }

      @media (max-width: 1200px) {
        .page-grid,
        .detail-grid {
          grid-template-columns: 1fr;
        }
      }

      @media (max-width: 720px) {
        .form-grid,
        .summary-grid {
          grid-template-columns: 1fr;
        }

        .card-header,
        .detail-header,
        .row-top,
        .form-actions {
          flex-direction: column;
        }
      }
    `
  ]
})
export class MarketsPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly marketsService = inject(MarketsService);
  private readonly sharedCatalogsService = inject(SharedCatalogsService);

  protected readonly contacts = signal<Contact[]>([]);
  protected readonly marketStatuses = signal<ModuleStatusCatalogEntry[]>([]);
  protected readonly issueStatuses = signal<ModuleStatusCatalogEntry[]>([]);
  protected readonly markets = signal<MarketSummary[]>([]);
  protected readonly tenantAlerts = signal<MarketTenantAlert[]>([]);
  protected readonly selectedMarketId = signal<string | null>(null);
  protected readonly selectedMarket = signal<MarketDetail | null>(null);

  protected readonly isBootstrapping = signal(true);
  protected readonly pageError = signal<string | null>(null);
  protected readonly isSubmittingMarket = signal(false);
  protected readonly isSubmittingTenant = signal(false);
  protected readonly isSubmittingIssue = signal(false);

  protected readonly marketFormError = signal<string | null>(null);
  protected readonly marketFormSuccess = signal<string | null>(null);
  protected readonly tenantFormError = signal<string | null>(null);
  protected readonly tenantFormSuccess = signal<string | null>(null);
  protected readonly issueFormError = signal<string | null>(null);
  protected readonly issueFormSuccess = signal<string | null>(null);

  protected readonly selectedCertificateFile = signal<File | null>(null);
  protected readonly selectedCertificateFileName = computed(() => this.selectedCertificateFile()?.name ?? null);
  protected readonly selectedMarketAlertCount = computed(() =>
    this.tenantAlerts().filter((item) => item.marketId === this.selectedMarketId()).length);

  protected readonly filtersForm = this.formBuilder.nonNullable.group({
    statusCode: [''],
    alertsOnly: [false]
  });

  protected readonly marketForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    borough: ['', [Validators.required, Validators.maxLength(120)]],
    statusCatalogEntryId: [0, [Validators.required, Validators.min(1)]],
    secretaryGeneralContactId: [''],
    secretaryGeneralName: ['', [Validators.required, Validators.maxLength(200)]],
    notes: ['', [Validators.maxLength(1500)]]
  });

  protected readonly tenantForm = this.formBuilder.nonNullable.group({
    contactId: [''],
    tenantName: ['', [Validators.required, Validators.maxLength(200)]],
    certificateNumber: ['', [Validators.required, Validators.maxLength(80)]],
    certificateValidityTo: ['', Validators.required],
    businessLine: ['', [Validators.required, Validators.maxLength(120)]],
    mobilePhone: ['', [Validators.maxLength(30)]],
    whatsAppPhone: ['', [Validators.maxLength(30)]],
    email: ['', [Validators.email, Validators.maxLength(200)]],
    notes: ['', [Validators.maxLength(1500)]]
  });

  protected readonly issueForm = this.formBuilder.nonNullable.group({
    issueType: ['', [Validators.required, Validators.maxLength(120)]],
    description: ['', [Validators.required, Validators.maxLength(2000)]],
    issueDate: [this.todayIso(), Validators.required],
    advanceSummary: ['', [Validators.required, Validators.maxLength(1000)]],
    statusCatalogEntryId: [0, [Validators.required, Validators.min(1)]],
    followUpOrResolution: ['', [Validators.maxLength(1500)]],
    finalSatisfaction: ['', [Validators.maxLength(200)]]
  });

  constructor() {
    void this.loadPage();
  }

  protected async reloadPage() {
    await this.loadPage(this.selectedMarketId());
  }

  protected async applyFilters() {
    await this.reloadMarketsAndSelection(this.selectedMarketId());
  }

  protected async clearFilters() {
    this.filtersForm.reset({
      statusCode: '',
      alertsOnly: false
    });

    await this.reloadMarketsAndSelection(this.selectedMarketId());
  }

  protected async selectMarket(marketId: string) {
    this.selectedMarketId.set(marketId);
    await this.loadMarketDetail(marketId);
  }

  protected async closeSelectedMarket() {
    const market = this.selectedMarket();
    if (!market) {
      return;
    }

    if (market.statusIsClosed) {
      this.pageError.set('El mercado ya se encuentra en estado terminal y no admite un nuevo cierre formal.');
      return;
    }

    const reason = globalThis.prompt('Motivo breve de cierre formal del mercado. Deja vacío si no aplica.', '');
    if (reason === null) {
      return;
    }

    this.pageError.set(null);

    try {
      await firstValueFrom(this.marketsService.closeMarket(market.id, { reason: this.normalizeOptional(reason) }));
      await this.reloadMarketsAndSelection(market.id);
      globalThis.alert('Cierre formal registrado en bitácora.');
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible registrar el cierre formal del mercado.'));
    }
  }

  protected syncSecretaryGeneralFromContact() {
    const contact = this.resolveSelectedContact(this.marketForm.controls.secretaryGeneralContactId.getRawValue());
    if (!contact) {
      return;
    }

    this.marketForm.patchValue({
      secretaryGeneralName: contact.name
    });
  }

  protected syncTenantFromContact() {
    const contact = this.resolveSelectedContact(this.tenantForm.controls.contactId.getRawValue());
    if (!contact) {
      return;
    }

    this.tenantForm.patchValue({
      tenantName: contact.name,
      mobilePhone: contact.mobilePhone ?? '',
      whatsAppPhone: contact.whatsAppPhone ?? '',
      email: contact.email ?? ''
    });
  }

  protected onCertificateSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    this.selectedCertificateFile.set(input.files?.[0] ?? null);
  }

  protected async submitMarket() {
    if (this.marketForm.invalid) {
      this.marketForm.markAllAsTouched();
      this.marketFormError.set('Completa los campos obligatorios del mercado.');
      this.marketFormSuccess.set(null);
      return;
    }

    this.isSubmittingMarket.set(true);
    this.marketFormError.set(null);
    this.marketFormSuccess.set(null);

    try {
      const request: CreateMarketRequest = {
        name: this.marketForm.controls.name.getRawValue(),
        borough: this.marketForm.controls.borough.getRawValue(),
        statusCatalogEntryId: this.marketForm.controls.statusCatalogEntryId.getRawValue(),
        secretaryGeneralContactId: this.normalizeGuid(this.marketForm.controls.secretaryGeneralContactId.getRawValue()),
        secretaryGeneralName: this.marketForm.controls.secretaryGeneralName.getRawValue(),
        notes: this.normalizeOptional(this.marketForm.controls.notes.getRawValue())
      };

      const market = await firstValueFrom(this.marketsService.createMarket(request));
      await this.reloadMarketsAndSelection(market.id);
      this.resetMarketForm();
      this.marketFormSuccess.set('Mercado registrado correctamente.');
    } catch (error) {
      this.marketFormError.set(getApiErrorMessage(error, 'No fue posible registrar el mercado.'));
    } finally {
      this.isSubmittingMarket.set(false);
    }
  }

  protected async submitTenant() {
    const marketId = this.selectedMarketId();
    if (!marketId) {
      this.tenantFormError.set('Selecciona un mercado antes de registrar locatarios.');
      this.tenantFormSuccess.set(null);
      return;
    }

    if (this.tenantForm.invalid || !this.selectedCertificateFile()) {
      this.tenantForm.markAllAsTouched();
      this.tenantFormError.set('Completa los campos obligatorios y adjunta la cédula digitalizada.');
      this.tenantFormSuccess.set(null);
      return;
    }

    this.isSubmittingTenant.set(true);
    this.tenantFormError.set(null);
    this.tenantFormSuccess.set(null);

    try {
      const request: CreateMarketTenantRequest = {
        contactId: this.normalizeGuid(this.tenantForm.controls.contactId.getRawValue()),
        tenantName: this.tenantForm.controls.tenantName.getRawValue(),
        certificateNumber: this.tenantForm.controls.certificateNumber.getRawValue(),
        certificateValidityTo: this.tenantForm.controls.certificateValidityTo.getRawValue(),
        businessLine: this.tenantForm.controls.businessLine.getRawValue(),
        mobilePhone: this.normalizeOptional(this.tenantForm.controls.mobilePhone.getRawValue()),
        whatsAppPhone: this.normalizeOptional(this.tenantForm.controls.whatsAppPhone.getRawValue()),
        email: this.normalizeOptional(this.tenantForm.controls.email.getRawValue()),
        notes: this.normalizeOptional(this.tenantForm.controls.notes.getRawValue()),
        certificateFile: this.selectedCertificateFile()!
      };

      await firstValueFrom(this.marketsService.createMarketTenant(marketId, request));
      await this.reloadMarketsAndSelection(marketId);
      this.resetTenantForm();
      this.tenantFormSuccess.set('Locatario registrado correctamente.');
    } catch (error) {
      this.tenantFormError.set(getApiErrorMessage(error, 'No fue posible registrar el locatario.'));
    } finally {
      this.isSubmittingTenant.set(false);
    }
  }

  protected async submitIssue() {
    const marketId = this.selectedMarketId();
    if (!marketId) {
      this.issueFormError.set('Selecciona un mercado antes de registrar incidencias.');
      this.issueFormSuccess.set(null);
      return;
    }

    if (this.issueForm.invalid) {
      this.issueForm.markAllAsTouched();
      this.issueFormError.set('Completa los campos obligatorios de la incidencia.');
      this.issueFormSuccess.set(null);
      return;
    }

    this.isSubmittingIssue.set(true);
    this.issueFormError.set(null);
    this.issueFormSuccess.set(null);

    try {
      const request: CreateMarketIssueRequest = {
        issueType: this.issueForm.controls.issueType.getRawValue(),
        description: this.issueForm.controls.description.getRawValue(),
        issueDate: this.issueForm.controls.issueDate.getRawValue(),
        advanceSummary: this.issueForm.controls.advanceSummary.getRawValue(),
        statusCatalogEntryId: this.issueForm.controls.statusCatalogEntryId.getRawValue(),
        followUpOrResolution: this.normalizeOptional(this.issueForm.controls.followUpOrResolution.getRawValue()),
        finalSatisfaction: this.normalizeOptional(this.issueForm.controls.finalSatisfaction.getRawValue())
      };

      await firstValueFrom(this.marketsService.createMarketIssue(marketId, request));
      await this.reloadMarketsAndSelection(marketId);
      this.resetIssueForm();
      this.issueFormSuccess.set('Incidencia registrada correctamente.');
    } catch (error) {
      this.issueFormError.set(getApiErrorMessage(error, 'No fue posible registrar la incidencia.'));
    } finally {
      this.isSubmittingIssue.set(false);
    }
  }

  protected resetMarketForm() {
    this.marketForm.reset({
      name: '',
      borough: '',
      statusCatalogEntryId: this.marketStatuses()[0]?.id ?? 0,
      secretaryGeneralContactId: '',
      secretaryGeneralName: '',
      notes: ''
    });
  }

  protected resetTenantForm() {
    this.selectedCertificateFile.set(null);
    this.tenantForm.reset({
      contactId: '',
      tenantName: '',
      certificateNumber: '',
      certificateValidityTo: '',
      businessLine: '',
      mobilePhone: '',
      whatsAppPhone: '',
      email: '',
      notes: ''
    });
  }

  protected resetIssueForm() {
    this.issueForm.reset({
      issueType: '',
      description: '',
      issueDate: this.todayIso(),
      advanceSummary: '',
      statusCatalogEntryId: this.issueStatuses()[0]?.id ?? 0,
      followUpOrResolution: '',
      finalSatisfaction: ''
    });
  }

  protected marketStatusClass(statusCode: string) {
    switch (statusCode) {
      case 'ACTIVE':
        return 'market-active';
      case 'INACTIVE':
        return 'market-inactive';
      case 'CLOSED':
        return 'market-closed';
      case 'ARCHIVED':
        return 'market-archived';
      default:
        return 'neutral';
    }
  }

  protected issueStatusClass(issue: MarketIssue) {
    if (issue.statusCode === 'CLOSED') {
      return 'issue-closed';
    }

    if (issue.statusCode === 'ATTENDED_SATISFACTORILY' || issue.statusCode === 'CONCLUDED_UNSATISFACTORILY') {
      return 'issue-muted';
    }

    return 'issue-progress';
  }

  protected tenantAlertClass(alertState: string) {
    switch (alertState) {
      case 'EXPIRED':
        return 'expired';
      case 'DUE_SOON':
        return 'due-soon';
      case 'ALERTS_DISABLED':
        return 'alerts-disabled';
      default:
        return 'valid';
    }
  }

  protected tenantAlertLabel(alertState: string) {
    switch (alertState) {
      case 'EXPIRED':
        return 'Vencida';
      case 'DUE_SOON':
        return 'Vence pronto';
      case 'ALERTS_DISABLED':
        return 'Alertas detenidas';
      default:
        return 'Vigente';
    }
  }

  protected expirationLabel(daysUntilExpiration: number) {
    if (daysUntilExpiration < 0) {
      return `Vencida hace ${Math.abs(daysUntilExpiration)} dia(s)`;
    }

    if (daysUntilExpiration <= 30) {
      return `Vence en ${daysUntilExpiration} dia(s)`;
    }

    return `Vigente por ${daysUntilExpiration} dia(s)`;
  }

  protected tenantCertificateUrl(tenantId: string) {
    return this.marketsService.getTenantCertificateDownloadUrl(tenantId);
  }

  private async loadPage(preferredMarketId?: string | null) {
    this.isBootstrapping.set(true);
    this.pageError.set(null);

    try {
      const [contacts, marketStatuses, issueStatuses] = await Promise.all([
        firstValueFrom(this.sharedCatalogsService.getContacts()),
        firstValueFrom(this.sharedCatalogsService.getModuleStatuses('MARKETS', 'MARKET')),
        firstValueFrom(this.sharedCatalogsService.getModuleStatuses('MARKETS', 'MARKET_ISSUE'))
      ]);

      this.contacts.set(contacts);
      this.marketStatuses.set(marketStatuses);
      this.issueStatuses.set(issueStatuses);

      if (this.marketForm.controls.statusCatalogEntryId.getRawValue() === 0 && marketStatuses.length > 0) {
        this.marketForm.patchValue({ statusCatalogEntryId: marketStatuses[0].id });
      }

      if (this.issueForm.controls.statusCatalogEntryId.getRawValue() === 0 && issueStatuses.length > 0) {
        this.issueForm.patchValue({ statusCatalogEntryId: issueStatuses[0].id });
      }

      await this.reloadMarketsAndSelection(preferredMarketId ?? this.selectedMarketId());
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible cargar el modulo de Mercados.'));
    } finally {
      this.isBootstrapping.set(false);
    }
  }

  private async reloadMarketsAndSelection(preferredMarketId?: string | null) {
    const filters = {
      statusCode: this.normalizeOptional(this.filtersForm.controls.statusCode.getRawValue()),
      alertsOnly: this.filtersForm.controls.alertsOnly.getRawValue()
    };

    const [markets, alerts] = await Promise.all([
      firstValueFrom(this.marketsService.listMarkets(filters)),
      firstValueFrom(this.marketsService.getTenantAlerts())
    ]);

    this.markets.set(markets);
    this.tenantAlerts.set(alerts);

    const nextMarketId = preferredMarketId && markets.some((item) => item.id === preferredMarketId)
      ? preferredMarketId
      : markets[0]?.id ?? null;

    this.selectedMarketId.set(nextMarketId);

    if (nextMarketId) {
      await this.loadMarketDetail(nextMarketId);
    } else {
      this.selectedMarket.set(null);
    }
  }

  private async loadMarketDetail(marketId: string) {
    this.selectedMarket.set(await firstValueFrom(this.marketsService.getMarket(marketId)));
  }

  private resolveSelectedContact(contactId: string) {
    const normalizedContactId = this.normalizeGuid(contactId);
    if (!normalizedContactId) {
      return null;
    }

    return this.contacts().find((item) => item.id === normalizedContactId) ?? null;
  }

  private normalizeOptional(value: string) {
    const normalizedValue = value.trim();
    return normalizedValue.length > 0 ? normalizedValue : null;
  }

  private normalizeGuid(value: string) {
    const normalizedValue = value.trim();
    return normalizedValue.length > 0 ? normalizedValue : null;
  }

  private todayIso() {
    return new Date().toISOString().slice(0, 10);
  }

  protected readonly environment = environment;
}
