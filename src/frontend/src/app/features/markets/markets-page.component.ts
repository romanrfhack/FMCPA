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
import { Contact, ContactIntervention, ModuleStatusCatalogEntry } from '../../core/models/shared-catalogs.models';
import { AuthService } from '../../core/services/auth.service';
import { MarketsService } from '../../core/services/markets.service';
import { SharedCatalogsService } from '../../core/services/shared-catalogs.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';
import { ContactInterventionLauncherComponent } from '../../shared/contact-interventions/contact-intervention-launcher.component';
import { RelatedDocumentsPanelComponent } from '../documents/related-documents-panel.component';

type MarketTab = 'summary' | 'tenants' | 'issues' | 'documents';

interface IssueContactInterventionsState {
  isOpen: boolean;
  isLoading: boolean;
  hasLoaded: boolean;
  error: string | null;
  interventions: ContactIntervention[];
}

const emptyIssueContactInterventionsState: IssueContactInterventionsState = {
  isOpen: false,
  isLoading: false,
  hasLoaded: false,
  error: null,
  interventions: []
};

@Component({
  selector: 'app-markets-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, ReactiveFormsModule, ContactInterventionLauncherComponent, RelatedDocumentsPanelComponent],
  template: `
    <section class="page-shell">
      <article class="hero-card">
        <div>
          <p class="page-kicker">Gestión operativa</p>
          <h2>Mercados</h2>
          <p>
            Consulta mercados, locatarios, cédulas, incidencias y alertas de vigencia desde una vista compacta.
          </p>
        </div>
        @if (canWrite()) {
          <button type="button" (click)="openMarketModal()">Registrar mercado</button>
        }
      </article>

      @if (pageError()) {
        <p class="alert error">{{ pageError() }}</p>
      }

      @if (pageSuccess()) {
        <p class="alert success">{{ pageSuccess() }}</p>
      }

      @if (isMarketModalOpen()) {
        <div class="modal" (click)="closeMarketModal()" (document:keydown.escape)="closeMarketModal()">
          <article
            class="form-card modal-panel"
            role="dialog"
            aria-modal="true"
            aria-labelledby="register-market-title"
            (click)="$event.stopPropagation()">
            <div class="card-header">
              <div>
                <h3 id="register-market-title">Registrar mercado</h3>
                <p>Registro base del mercado y su secretario general.</p>
              </div>
              <button type="button" class="ghost" (click)="closeMarketModal()" [disabled]="isSubmittingMarket()">
                Cerrar
              </button>
            </div>

            @if (marketFormError()) {
              <p class="alert error">{{ marketFormError() }}</p>
            }

            <form class="form-grid" [formGroup]="marketForm" (ngSubmit)="submitMarket()">
              <label>
                <span>Nombre</span>
                <input type="text" formControlName="name" placeholder="Mercado ejemplo" />
              </label>

              <label>
                <span>Alcaldía</span>
                <input type="text" formControlName="borough" placeholder="Alcaldía" />
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
                <button type="submit" [disabled]="isSubmittingMarket() || !canWrite()">Registrar mercado</button>
                <button type="button" class="ghost" (click)="resetMarketForm()" [disabled]="isSubmittingMarket()">
                  Limpiar
                </button>
                <button type="button" class="ghost" (click)="closeMarketModal()" [disabled]="isSubmittingMarket()">
                  Cancelar
                </button>
              </div>
            </form>
          </article>
        </div>
      }

      @if (isTenantModalOpen() && selectedMarket(); as marketDetail) {
        <div class="modal" (click)="closeTenantModal()" (document:keydown.escape)="closeTenantModal()">
          <article
            class="form-card modal-panel"
            role="dialog"
            aria-modal="true"
            aria-labelledby="register-tenant-title"
            (click)="$event.stopPropagation()">
            <div class="card-header">
              <div>
                <h3 id="register-tenant-title">Registrar locatario</h3>
                <p>{{ marketDetail.name }} · cédula digitalizada y datos operativos.</p>
              </div>
              <button type="button" class="ghost" (click)="closeTenantModal()" [disabled]="isSubmittingTenant()">
                Cerrar
              </button>
            </div>

            @if (tenantFormError()) {
              <p class="alert error">{{ tenantFormError() }}</p>
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
                <button type="submit" [disabled]="isSubmittingTenant() || !selectedMarketCanOperate()">
                  Registrar locatario
                </button>
                <button type="button" class="ghost" (click)="resetTenantForm()" [disabled]="isSubmittingTenant()">
                  Limpiar
                </button>
                <button type="button" class="ghost" (click)="closeTenantModal()" [disabled]="isSubmittingTenant()">
                  Cancelar
                </button>
              </div>
            </form>
          </article>
        </div>
      }

      @if (isIssueModalOpen() && selectedMarket(); as marketDetail) {
        <div class="modal" (click)="closeIssueModal()" (document:keydown.escape)="closeIssueModal()">
          <article
            class="form-card modal-panel"
            role="dialog"
            aria-modal="true"
            aria-labelledby="register-issue-title"
            (click)="$event.stopPropagation()">
            <div class="card-header">
              <div>
                <h3 id="register-issue-title">Registrar incidencia / mejora</h3>
                <p>{{ marketDetail.name }} · seguimiento con estatus reusable.</p>
              </div>
              <button type="button" class="ghost" (click)="closeIssueModal()" [disabled]="isSubmittingIssue()">
                Cerrar
              </button>
            </div>

            @if (issueFormError()) {
              <p class="alert error">{{ issueFormError() }}</p>
            }

            <form class="form-grid" [formGroup]="issueForm" (ngSubmit)="submitIssue()">
              <label>
                <span>Tipo</span>
                <input type="text" formControlName="issueType" placeholder="Queja, mejora u observación" />
              </label>

              <label>
                <span>Fecha</span>
                <input type="date" formControlName="issueDate" />
              </label>

              <label class="full-width">
                <span>Descripción</span>
                <textarea formControlName="description" rows="4" placeholder="Descripción de la incidencia o mejora"></textarea>
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
                <textarea formControlName="followUpOrResolution" rows="3" placeholder="Seguimiento o resolución"></textarea>
              </label>

              <label class="full-width">
                <span>Satisfacción final</span>
                <input type="text" formControlName="finalSatisfaction" placeholder="Si aplica" />
              </label>

              <div class="form-actions full-width">
                <button type="submit" [disabled]="isSubmittingIssue() || !selectedMarketCanOperate()">
                  Registrar incidencia
                </button>
                <button type="button" class="ghost" (click)="resetIssueForm()" [disabled]="isSubmittingIssue()">
                  Limpiar
                </button>
                <button type="button" class="ghost" (click)="closeIssueModal()" [disabled]="isSubmittingIssue()">
                  Cancelar
                </button>
              </div>
            </form>
          </article>
        </div>
      }

      <div class="page-grid">
        <aside class="sidebar">
          <article class="filter-card">
            <div class="card-header">
              <div>
                <h3>Mercados</h3>
                <p>Filtros compactos por estatus y alertas.</p>
              </div>
            </div>

            <form class="filter-grid" [formGroup]="filtersForm" (ngSubmit)="applyFilters()">
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
                <button type="submit">Aplicar</button>
                <button type="button" class="ghost" (click)="clearFilters()">Limpiar</button>
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
                    [disabled]="!canAdminister() || marketDetail.statusIsClosed"
                    [attr.title]="!canAdminister()
                      ? 'Solo ADMIN puede registrar cierre formal.'
                      : marketDetail.statusIsClosed
                        ? 'El mercado ya se encuentra en estado terminal.'
                        : 'Registrar cierre formal.'">
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
                  <h4>Cédulas con alerta</h4>
                  <p>{{ selectedMarketAlertCount() }}</p>
                </article>
                <article>
                  <h4>Incidencias abiertas</h4>
                  <p>{{ selectedOpenIssueCount() }}</p>
                </article>
                <article>
                  <h4>Incidencias cerradas</h4>
                  <p>{{ selectedClosedIssueCount() }}</p>
                </article>
                <article>
                  <h4>Secretario general</h4>
                  <p>{{ marketDetail.secretaryGeneralName }}</p>
                </article>
                <article>
                  <h4>Alta UTC</h4>
                  <p>{{ marketDetail.createdUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</p>
                </article>
              </div>
            </article>

            <nav class="tab-nav" aria-label="Secciones de mercado">
              <button
                type="button"
                [class.is-active]="activeTab() === 'summary'"
                (click)="setActiveTab('summary')">
                Resumen
              </button>
              <button
                type="button"
                [class.is-active]="activeTab() === 'tenants'"
                (click)="setActiveTab('tenants')">
                Locatarios
              </button>
              <button
                type="button"
                [class.is-active]="activeTab() === 'issues'"
                (click)="setActiveTab('issues')">
                Incidencias / mejoras
              </button>
              <button
                type="button"
                [class.is-active]="activeTab() === 'documents'"
                (click)="setActiveTab('documents')">
                Documentos / cédulas
              </button>
            </nav>

            @if (activeTab() === 'summary') {
              <article class="list-card">
                <div class="card-header">
                  <div>
                    <h3>Resumen ejecutivo</h3>
                    <p>Lectura rápida del mercado seleccionado y sus alertas principales.</p>
                  </div>
                  <div class="detail-badges">
                    @if (selectedMarketCanOperate()) {
                      <button type="button" (click)="openTenantModal()">Registrar locatario</button>
                      <button type="button" class="ghost" (click)="openIssueModal()">Registrar incidencia</button>
                    }
                  </div>
                </div>

                <div class="signal-grid">
                  <article class="signal-card">
                    <h4>Estatus</h4>
                    <strong>{{ marketDetail.statusName }}</strong>
                    <p>{{ marketDetail.statusIsClosed ? 'Mercado terminal; captura bloqueada.' : 'Mercado disponible para operación contextual.' }}</p>
                  </article>
                  <article class="signal-card">
                    <h4>Alcaldía</h4>
                    <strong>{{ marketDetail.borough }}</strong>
                    <p>Secretario general: {{ marketDetail.secretaryGeneralName }}</p>
                  </article>
                  <article class="signal-card">
                    <h4>Cédulas</h4>
                    <strong>{{ selectedExpiredTenantCount() }} vencidas · {{ selectedDueSoonTenantCount() }} por vencer</strong>
                    <p>{{ selectedValidTenantCount() }} vigentes o sin alerta activa.</p>
                  </article>
                  <article class="signal-card">
                    <h4>Locatarios</h4>
                    <strong>{{ marketDetail.tenants.length }}</strong>
                    <p>Registro operativo con contacto y cédula asociada.</p>
                  </article>
                  <article class="signal-card">
                    <h4>Incidencias / mejoras</h4>
                    <strong>{{ selectedOpenIssueCount() }} abiertas · {{ selectedClosedIssueCount() }} cerradas</strong>
                    <p>Seguimiento calculado con el estatus disponible.</p>
                  </article>
                  <article class="signal-card">
                    <h4>Alertas</h4>
                    <strong>{{ selectedMarketAlertCount() }}</strong>
                    <p>Cédulas vencidas o por vencer reportadas por el servicio actual.</p>
                  </article>
                </div>
              </article>
            }

            @if (activeTab() === 'tenants') {
              <article class="list-card">
                <div class="card-header">
                  <div>
                    <h3>Locatarios</h3>
                    <p>Vigencias visibles con indicador de alerta.</p>
                  </div>
                  @if (selectedMarketCanOperate()) {
                    <button type="button" (click)="openTenantModal()">Registrar locatario</button>
                  }
                </div>

                @if (tenantFormSuccess()) {
                  <p class="alert success">{{ tenantFormSuccess() }}</p>
                }

                @if (marketDetail.tenants.length === 0) {
                  <p class="empty-state">Aún no hay locatarios registrados.</p>
                } @else {
                  <div class="entity-list compact-entity-list">
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

                        <div class="row-actions">
                          <span>{{ expirationLabel(tenant.daysUntilExpiration) }}</span>
                          @if (tenant.hasDigitalCertificate) {
                            <button
                              type="button"
                              class="ghost"
                              (click)="downloadTenantCertificate(tenant)">
                              Descargar cédula
                            </button>
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
            }

            @if (activeTab() === 'issues') {
              <article class="list-card">
                <div class="card-header">
                  <div>
                    <h3>Incidencias / mejoras</h3>
                    <p>Listado cronológico del seguimiento del mercado.</p>
                  </div>
                  @if (selectedMarketCanOperate()) {
                    <button type="button" (click)="openIssueModal()">Registrar incidencia</button>
                  }
                </div>

                @if (issueFormSuccess()) {
                  <p class="alert success">{{ issueFormSuccess() }}</p>
                }

                @if (marketDetail.issues.length === 0) {
                  <p class="empty-state">Aún no hay incidencias registradas.</p>
                } @else {
                  <div class="entity-list compact-entity-list">
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
                          <p class="meta"><strong>Satisfacción final:</strong> {{ issue.finalSatisfaction }}</p>
                        }

                        @if (canLinkContactSupportForMarketIssue() || canViewContactSupportForMarketIssue()) {
                          <div class="row-actions issue-support-actions">
                            @if (canLinkContactSupportForMarketIssue()) {
                              <app-contact-intervention-launcher
                                class="issue-support-launcher"
                                moduleKey="MARKETS"
                                originType="MARKET_ISSUE"
                                [originId]="issue.id"
                                [originDisplayName]="buildMarketIssueOriginDisplayName(marketDetail, issue)"
                                [defaultSubject]="buildMarketIssueDefaultSubject(issue)"
                                buttonLabel="Vincular apoyo de contacto"
                                [disabled]="!selectedMarketCanOperate()"
                                (saved)="onMarketIssueContactInterventionSaved($event, issue)">
                              </app-contact-intervention-launcher>
                            }
                            @if (canViewContactSupportForMarketIssue()) {
                              <button
                                type="button"
                                class="ghost compact"
                                [disabled]="getIssueContactInterventionsState(issue.id).isLoading"
                                (click)="toggleIssueContactInterventions(issue)">
                                @if (isIssueContactInterventionsOpen(issue.id)) {
                                  Ocultar apoyos
                                } @else {
                                  Ver apoyos
                                }
                              </button>
                            }
                          </div>
                        }

                        @if (canViewContactSupportForMarketIssue() && isIssueContactInterventionsOpen(issue.id)) {
                          @if (getIssueContactInterventionsState(issue.id); as supportHistory) {
                            <section class="issue-support-history" aria-label="Apoyos de contacto vinculados">
                              <div class="row-top issue-support-history-header">
                                <div>
                                  <h4>Apoyos de contacto</h4>
                                  <p class="meta">Intervenciones asociadas a esta incidencia.</p>
                                </div>
                                <button
                                  type="button"
                                  class="ghost compact"
                                  [disabled]="supportHistory.isLoading"
                                  (click)="loadIssueContactInterventions(issue, true)">
                                  Actualizar
                                </button>
                              </div>

                              @if (supportHistory.error) {
                                <p class="alert error">{{ supportHistory.error }}</p>
                              } @else if (supportHistory.isLoading) {
                                <p class="empty-state">Cargando apoyos de contacto...</p>
                              } @else if (supportHistory.hasLoaded && supportHistory.interventions.length === 0) {
                                <p class="empty-state">No hay apoyos de contacto registrados para esta incidencia.</p>
                              } @else {
                                <div class="issue-support-history-list">
                                  @for (intervention of supportHistory.interventions; track intervention.id) {
                                    <article class="issue-support-history-item">
                                      <div class="row-top">
                                        <div>
                                          <p class="meta">
                                            {{ intervention.occurredUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}
                                          </p>
                                          <h4>{{ intervention.contactName }}</h4>
                                        </div>
                                        <span class="status-pill neutral">{{ getOutcomeLabel(intervention.outcome) }}</span>
                                      </div>

                                      <dl class="detail-grid-list">
                                        <div>
                                          <dt>Tipo de ayuda</dt>
                                          <dd>{{ getHelpTypeLabel(intervention.helpType) }}</dd>
                                        </div>
                                        <div>
                                          <dt>Resultado</dt>
                                          <dd>{{ getOutcomeLabel(intervention.outcome) }}</dd>
                                        </div>
                                        <div>
                                          <dt>Registró</dt>
                                          <dd>
                                            {{ intervention.createdByUserName || 'Sin usuario' }}
                                            · {{ intervention.createdUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}
                                          </dd>
                                        </div>
                                      </dl>

                                      @if (intervention.notes) {
                                        <p class="description issue-support-notes">{{ intervention.notes }}</p>
                                      }
                                    </article>
                                  }
                                </div>
                              }
                            </section>
                          }
                        }
                      </article>
                    }
                  </div>
                }
              </article>
            }

            @if (activeTab() === 'documents') {
              <article class="list-card">
                <div class="card-header">
                  <div>
                    <h3>Documentos / cédulas</h3>
                    <p>Cédula actual, vigencia, descarga y documentos transversales por locatario.</p>
                  </div>
                </div>

                @if (marketDetail.tenants.length === 0) {
                  <p class="empty-state">No hay locatarios con cédulas para consultar.</p>
                } @else {
                  <div class="entity-list compact-entity-list">
                    @for (tenant of marketDetail.tenants; track tenant.id) {
                      <article class="entity-row">
                        <div class="row-top">
                          <div>
                            <h4>{{ tenant.tenantName }}</h4>
                            <p class="meta">
                              Cédula {{ tenant.certificateNumber }} · Vigencia {{ tenant.certificateValidityTo }}
                            </p>
                          </div>
                          <span class="status-pill" [class]="tenantAlertClass(tenant.certificateAlertState)">
                            {{ tenantAlertLabel(tenant.certificateAlertState) }}
                          </span>
                        </div>

                        <div class="row-actions">
                          <span>{{ tenant.certificateOriginalFileName || 'Sin nombre de archivo' }}</span>
                          <span>{{ expirationLabel(tenant.daysUntilExpiration) }}</span>
                          @if (tenant.hasDigitalCertificate) {
                            <button
                              type="button"
                              class="ghost"
                              (click)="downloadTenantCertificate(tenant)">
                              Descargar cédula
                            </button>
                          }
                        </div>

                        <app-related-documents-panel
                          moduleCode="MARKETS"
                          entityType="MARKET_TENANT"
                          [entityId]="tenant.id"
                          [canRemediate]="selectedMarketCanOperate()"
                          title="Documentos del locatario"
                          subtitle="Cédulas y metadata transversal asociadas al locatario."
                          emptyMessage="No hay documentos transversales asociados a este locatario."
                          (remediated)="reloadPage()">
                        </app-related-documents-panel>
                      </article>
                    }
                  </div>
                }
              </article>
            }
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
        gap: 1rem;
      }

      .page-grid {
        display: grid;
        grid-template-columns: minmax(20rem, 23rem) minmax(0, 1fr);
        gap: 1.25rem;
        align-items: start;
      }

      .detail-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
      }

      .hero-card {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        align-items: flex-start;
        border-top: 4px solid #a8302d;
      }

      .page-kicker {
        margin: 0 0 0.5rem;
        letter-spacing: 0.12em;
        text-transform: uppercase;
        font-size: 0.78rem;
        font-weight: 700;
        color: #0f766e;
      }

      .description {
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
        margin-bottom: 0.85rem;
      }

      .market-stats,
      .row-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.55rem;
      }

      .summary-grid {
        grid-template-columns: repeat(3, minmax(0, 1fr));
      }

      .filter-grid {
        display: grid;
        gap: 0.75rem;
      }

      .form-grid {
        gap: 0.9rem;
      }

      input:focus,
      select:focus,
      textarea:focus,
      button:focus-visible,
      .market-card:focus-visible {
        outline: 3px solid rgba(15, 118, 110, 0.22);
        outline-offset: 1px;
      }

      .toggle {
        padding: 0.8rem 0.9rem;
        border-radius: 0.9rem;
      }

      .form-actions {
        gap: 0.75rem;
      }

      .market-card {
        border: none;
        border-radius: 0.75rem;
        padding: 0.72rem 0.9rem;
        font: inherit;
      }

      .market-card,
      .entity-row,
      .alert-row {
        display: grid;
        min-width: 0;
        gap: 0.6rem;
        padding: 0.85rem;
        border-radius: 0.75rem;
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
        padding: 0.4rem 0.6rem;
        border-radius: 999px;
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
        font-size: 0.82rem;
        text-decoration: none;
      }

      .signal-grid {
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: 0.75rem;
      }

      .signal-card {
        min-width: 0;
        padding: 0.85rem;
        border-radius: 0.75rem;
        border: 1px solid rgba(29, 45, 42, 0.08);
        background: #fbfbf8;
      }

      .signal-card strong {
        display: block;
        margin-top: 0.35rem;
        color: #203734;
        overflow-wrap: anywhere;
      }

      .signal-card p {
        margin-top: 0.45rem;
        color: #4d615c;
        line-height: 1.45;
      }

      .compact-entity-list {
        max-height: 62vh;
        overflow: auto;
        padding-right: 0.2rem;
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

      .issue-support-actions {
        justify-content: flex-end;
      }

      :host ::ng-deep app-contact-intervention-launcher.issue-support-launcher .intervention-launcher {
        border-radius: 0.7rem;
        padding: 0.62rem 0.78rem;
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
        font-size: 0.9rem;
      }

      .issue-support-history {
        display: grid;
        min-width: 0;
        gap: 0.75rem;
        margin-top: 0.35rem;
        padding: 0.85rem;
        border: 1px solid rgba(29, 45, 42, 0.08);
        border-radius: 0.75rem;
        background: #fbfbf8;
      }

      .issue-support-history-list {
        display: grid;
        gap: 0.65rem;
      }

      .issue-support-history-item {
        display: grid;
        min-width: 0;
        gap: 0.7rem;
        padding: 0.75rem;
        border-radius: 0.7rem;
        background: #f6f5ef;
      }

      .issue-support-notes {
        overflow-wrap: anywhere;
      }

      .status-pill.due-soon {
        background: rgba(148, 98, 0, 0.16);
        color: #7a5400;
      }

      .status-pill.expired {
        background: rgba(180, 35, 24, 0.12);
        color: #b42318;
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
        .detail-grid,
        .signal-grid {
          grid-template-columns: 1fr;
        }
      }

      @media (max-width: 720px) {
        .form-grid,
        .summary-grid {
          grid-template-columns: 1fr;
        }

        .page-shell,
        .sidebar,
        .detail-column {
          gap: 1rem;
        }

        .hero-card,
        .card-header,
        .detail-header,
        .row-top,
        .form-actions {
          flex-direction: column;
        }

        .modal {
          align-items: end;
          padding: 0.75rem;
        }

        .modal-panel {
          width: 100%;
          max-height: 92vh;
        }

        .compact-entity-list {
          max-height: none;
        }
      }
    `
  ]
})
export class MarketsPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly marketsService = inject(MarketsService);
  private readonly sharedCatalogsService = inject(SharedCatalogsService);

  protected readonly canReadMarkets = this.authService.canReadMarkets;
  protected readonly canWrite = this.authService.canWriteMarkets;
  protected readonly canReadContacts = this.authService.canReadContacts;
  protected readonly canAdminister = this.authService.canAdministerFormalClose;
  protected readonly contacts = signal<Contact[]>([]);
  protected readonly marketStatuses = signal<ModuleStatusCatalogEntry[]>([]);
  protected readonly issueStatuses = signal<ModuleStatusCatalogEntry[]>([]);
  protected readonly markets = signal<MarketSummary[]>([]);
  protected readonly tenantAlerts = signal<MarketTenantAlert[]>([]);
  protected readonly selectedMarketId = signal<string | null>(null);
  protected readonly selectedMarket = signal<MarketDetail | null>(null);
  protected readonly activeTab = signal<MarketTab>('summary');
  protected readonly issueContactInterventions = signal<Record<string, IssueContactInterventionsState>>({});

  protected readonly isBootstrapping = signal(true);
  protected readonly pageError = signal<string | null>(null);
  protected readonly pageSuccess = signal<string | null>(null);
  protected readonly isSubmittingMarket = signal(false);
  protected readonly isSubmittingTenant = signal(false);
  protected readonly isSubmittingIssue = signal(false);
  protected readonly isMarketModalOpen = signal(false);
  protected readonly isTenantModalOpen = signal(false);
  protected readonly isIssueModalOpen = signal(false);

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
  protected readonly selectedMarketCanOperate = computed(() => {
    const market = this.selectedMarket();
    return this.canWrite() && !!market && !market.statusIsClosed;
  });
  protected readonly selectedOpenIssueCount = computed(() =>
    this.selectedMarket()?.issues.filter((issue) => !issue.statusIsClosed).length ?? 0);
  protected readonly selectedClosedIssueCount = computed(() =>
    this.selectedMarket()?.issues.filter((issue) => issue.statusIsClosed).length ?? 0);
  protected readonly selectedExpiredTenantCount = computed(() => this.countSelectedTenantsByAlertState('EXPIRED'));
  protected readonly selectedDueSoonTenantCount = computed(() => this.countSelectedTenantsByAlertState('DUE_SOON'));
  protected readonly selectedValidTenantCount = computed(() =>
    this.selectedMarket()?.tenants.filter((tenant) =>
      tenant.certificateAlertState !== 'EXPIRED' && tenant.certificateAlertState !== 'DUE_SOON').length ?? 0);

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
    this.pageSuccess.set(null);
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
    this.pageSuccess.set(null);
    this.selectedMarketId.set(marketId);
    await this.loadMarketDetail(marketId);
  }

  protected setActiveTab(tab: MarketTab) {
    this.activeTab.set(tab);
  }

  protected openMarketModal() {
    if (!this.canWrite()) {
      return;
    }

    this.pageError.set(null);
    this.pageSuccess.set(null);
    this.marketFormError.set(null);
    this.marketFormSuccess.set(null);
    this.isMarketModalOpen.set(true);
  }

  protected closeMarketModal() {
    if (this.isSubmittingMarket()) {
      return;
    }

    this.isMarketModalOpen.set(false);
    this.marketFormError.set(null);
    this.resetMarketForm();
  }

  protected openTenantModal() {
    const unavailableMessage = this.selectedMarketOperationUnavailableMessage();
    if (unavailableMessage) {
      this.pageError.set(unavailableMessage);
      return;
    }

    this.pageError.set(null);
    this.pageSuccess.set(null);
    this.tenantFormError.set(null);
    this.tenantFormSuccess.set(null);
    this.isTenantModalOpen.set(true);
  }

  protected closeTenantModal() {
    if (this.isSubmittingTenant()) {
      return;
    }

    this.isTenantModalOpen.set(false);
    this.tenantFormError.set(null);
    this.resetTenantForm();
  }

  protected openIssueModal() {
    const unavailableMessage = this.selectedMarketOperationUnavailableMessage();
    if (unavailableMessage) {
      this.pageError.set(unavailableMessage);
      return;
    }

    this.pageError.set(null);
    this.pageSuccess.set(null);
    this.issueFormError.set(null);
    this.issueFormSuccess.set(null);
    this.isIssueModalOpen.set(true);
  }

  protected closeIssueModal() {
    if (this.isSubmittingIssue()) {
      return;
    }

    this.isIssueModalOpen.set(false);
    this.issueFormError.set(null);
    this.resetIssueForm();
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
    this.pageError.set(null);
    this.pageSuccess.set(null);

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
      this.isMarketModalOpen.set(false);
      this.marketFormSuccess.set('Mercado registrado correctamente.');
      this.pageSuccess.set('Mercado registrado correctamente.');
    } catch (error) {
      this.marketFormError.set(getApiErrorMessage(error, 'No fue posible registrar el mercado.'));
    } finally {
      this.isSubmittingMarket.set(false);
    }
  }

  protected async submitTenant() {
    const marketId = this.selectedMarketId();
    const unavailableMessage = this.selectedMarketOperationUnavailableMessage();
    this.pageError.set(null);
    this.pageSuccess.set(null);

    if (!marketId) {
      this.tenantFormError.set('Selecciona un mercado antes de registrar locatarios.');
      this.tenantFormSuccess.set(null);
      return;
    }

    if (unavailableMessage) {
      this.tenantFormError.set(unavailableMessage);
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
      this.isTenantModalOpen.set(false);
      this.tenantFormSuccess.set('Locatario registrado correctamente.');
      this.pageSuccess.set('Locatario registrado correctamente.');
      this.activeTab.set('tenants');
    } catch (error) {
      this.tenantFormError.set(getApiErrorMessage(error, 'No fue posible registrar el locatario.'));
    } finally {
      this.isSubmittingTenant.set(false);
    }
  }

  protected async submitIssue() {
    const marketId = this.selectedMarketId();
    const unavailableMessage = this.selectedMarketOperationUnavailableMessage();
    this.pageError.set(null);
    this.pageSuccess.set(null);

    if (!marketId) {
      this.issueFormError.set('Selecciona un mercado antes de registrar incidencias.');
      this.issueFormSuccess.set(null);
      return;
    }

    if (unavailableMessage) {
      this.issueFormError.set(unavailableMessage);
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
      this.isIssueModalOpen.set(false);
      this.issueFormSuccess.set('Incidencia registrada correctamente.');
      this.pageSuccess.set('Incidencia registrada correctamente.');
      this.activeTab.set('issues');
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

  protected canLinkContactSupportForMarketIssue() {
    return this.selectedMarketCanOperate() && this.canReadContacts();
  }

  protected canViewContactSupportForMarketIssue() {
    return this.canReadMarkets() && this.canReadContacts();
  }

  protected getIssueContactInterventionsState(issueId: string) {
    return this.issueContactInterventions()[issueId] ?? emptyIssueContactInterventionsState;
  }

  protected isIssueContactInterventionsOpen(issueId: string) {
    return this.getIssueContactInterventionsState(issueId).isOpen;
  }

  protected async toggleIssueContactInterventions(issue: MarketIssue) {
    if (!this.canViewContactSupportForMarketIssue()) {
      return;
    }

    const currentState = this.getIssueContactInterventionsState(issue.id);

    if (currentState.isOpen) {
      this.patchIssueContactInterventionsState(issue.id, { isOpen: false });
      return;
    }

    this.patchIssueContactInterventionsState(issue.id, { isOpen: true });

    if (!currentState.hasLoaded) {
      await this.loadIssueContactInterventions(issue);
    }
  }

  protected async loadIssueContactInterventions(issue: MarketIssue, force = false) {
    if (!this.canViewContactSupportForMarketIssue()) {
      return;
    }

    const currentState = this.getIssueContactInterventionsState(issue.id);
    if ((currentState.isLoading && !force) || (currentState.hasLoaded && !force)) {
      return;
    }

    this.patchIssueContactInterventionsState(issue.id, {
      isOpen: true,
      isLoading: true,
      error: null,
      interventions: currentState.hasLoaded ? currentState.interventions : []
    });

    try {
      const interventions = await firstValueFrom(
        this.sharedCatalogsService.getContactInterventionsByOrigin({
          moduleKey: 'MARKETS',
          originType: 'MARKET_ISSUE',
          originId: issue.id
        })
      );

      this.patchIssueContactInterventionsState(issue.id, {
        isLoading: false,
        hasLoaded: true,
        error: null,
        interventions
      });
    } catch (error) {
      this.patchIssueContactInterventionsState(issue.id, {
        isLoading: false,
        hasLoaded: true,
        error: getApiErrorMessage(error, 'No fue posible cargar los apoyos de contacto.'),
        interventions: []
      });
    }
  }

  protected buildMarketIssueOriginDisplayName(market: MarketDetail, issue: MarketIssue) {
    return `${market.name} · ${issue.issueType} · ${issue.issueDate}`;
  }

  protected buildMarketIssueDefaultSubject(issue: MarketIssue) {
    return `Apoyo en incidencia de mercado: ${issue.issueType}`;
  }

  protected async onMarketIssueContactInterventionSaved(_intervention: ContactIntervention, issue: MarketIssue) {
    this.pageError.set(null);
    this.issueFormError.set(null);
    this.issueFormSuccess.set(`Apoyo de contacto vinculado a la incidencia: ${issue.issueType}.`);

    if (this.isIssueContactInterventionsOpen(issue.id)) {
      await this.loadIssueContactInterventions(issue, true);
    }
  }

  protected getHelpTypeLabel(helpType: string) {
    return this.resolveLabel(helpType, {
      INFORMATION: 'Información',
      FACILITATION: 'Facilitación',
      VALIDATION: 'Validación',
      ESCALATION: 'Escalamiento',
      FOLLOW_UP: 'Seguimiento',
      UNBLOCKING: 'Desbloqueo',
      OTHER: 'Otro'
    });
  }

  protected getOutcomeLabel(outcome: string) {
    return this.resolveLabel(outcome, {
      USEFUL: 'Útil',
      SUCCESSFUL: 'Exitoso',
      PENDING: 'Pendiente',
      NO_RESPONSE: 'Sin respuesta',
      NOT_APPLICABLE: 'No aplica',
      OTHER: 'Otro'
    });
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

  protected async downloadTenantCertificate(tenant: MarketTenant) {
    this.pageError.set(null);

    try {
      await this.marketsService.downloadTenantCertificate(
        tenant.id,
        `cedula-${tenant.certificateNumber || tenant.id}`);
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible descargar la cédula digitalizada.'));
    }
  }

  private selectedMarketOperationUnavailableMessage() {
    const market = this.selectedMarket();
    if (!market) {
      return 'Selecciona un mercado antes de registrar información.';
    }

    if (!this.canWrite()) {
      return 'No tienes permiso para registrar información en mercados.';
    }

    if (market.statusIsClosed) {
      return 'El mercado está en estado terminal y no admite nuevas capturas.';
    }

    return null;
  }

  private countSelectedTenantsByAlertState(alertState: string) {
    return this.selectedMarket()?.tenants.filter((tenant) => tenant.certificateAlertState === alertState).length ?? 0;
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
    this.issueContactInterventions.set({});
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

  private patchIssueContactInterventionsState(issueId: string, patch: Partial<IssueContactInterventionsState>) {
    this.issueContactInterventions.update((currentStates) => {
      const currentState = currentStates[issueId] ?? emptyIssueContactInterventionsState;

      return {
        ...currentStates,
        [issueId]: {
          ...currentState,
          ...patch
        }
      };
    });
  }

  private resolveLabel(code: string, labels: Record<string, string>) {
    const normalizedCode = code.trim().toUpperCase();
    return labels[normalizedCode] ?? normalizedCode;
  }

  private todayIso() {
    return new Date().toISOString().slice(0, 10);
  }

  protected readonly environment = environment;
}
