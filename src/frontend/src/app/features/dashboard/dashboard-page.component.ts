import { DecimalPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { ClosedItem, DashboardAlerts, DashboardSummary } from '../../core/models/closeout.models';
import { CloseoutService } from '../../core/services/closeout.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  selector: 'app-dashboard-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DecimalPipe, RouterLink],
  template: `
    <section class="page-shell">
      <article class="hero-card">
        <p class="page-kicker">STAGE-07</p>
        <h2>Dashboard ejecutivo y cierre operativo del MVP</h2>
        <p>
          Vista global minima para operar el cierre del MVP: resumen por modulo, alertas activas,
          acceso a historico y visibilidad transversal de comisiones, sin abrir analitica avanzada.
        </p>
      </article>

      @if (pageError()) {
        <p class="alert error">{{ pageError() }}</p>
      }

      @if (isLoading()) {
        <article class="card">
          <p class="empty-state">Cargando resumen ejecutivo y alertas del MVP...</p>
        </article>
      } @else if (summary(); as summaryData) {
        <div class="metrics-grid">
          <article class="metric-card accent">
            <p class="metric-label">Alertas activas</p>
            <p class="metric-value">{{ summaryData.totals.activeAlertCount }}</p>
            <p class="metric-meta">Cédulas, donaciones, oficios y gestiones con seguimiento activo.</p>
          </article>

          <article class="metric-card">
            <p class="metric-label">Cerrados / histórico</p>
            <p class="metric-value">{{ summaryData.totals.closedRecordsCount }}</p>
            <p class="metric-meta">Registros cerrados consultables sin reactivarlos.</p>
          </article>

          <article class="metric-card">
            <p class="metric-label">Comisiones visibles</p>
            <p class="metric-value">{{ summaryData.totals.commissionCount }}</p>
            <p class="metric-meta">Financieras y Federación integradas en vista operativa única.</p>
          </article>

          <article class="metric-card">
            <p class="metric-label">Evidencias</p>
            <p class="metric-value">{{ summaryData.totals.evidenceCount }}</p>
            <p class="metric-meta">Soportes de Donatarias, Federación y cédulas de Mercados.</p>
          </article>
        </div>

        <div class="module-grid">
          <article class="card">
            <div class="card-header">
              <div>
                <h3>Mercados</h3>
                <p>Operacion activa y control de vigencias.</p>
              </div>
              <a routerLink="/markets">Abrir modulo</a>
            </div>
            <dl class="stats-list">
              <div><dt>Mercados activos</dt><dd>{{ summaryData.markets.activeMarkets }}</dd></div>
              <div><dt>Cédulas con alerta</dt><dd>{{ summaryData.markets.certificateAlertCount }}</dd></div>
              <div><dt>Locatarios</dt><dd>{{ summaryData.markets.tenantCount }}</dd></div>
              <div><dt>Cerrados / archivados</dt><dd>{{ summaryData.markets.closedOrArchivedMarkets }}</dd></div>
            </dl>
          </article>

          <article class="card">
            <div class="card-header">
              <div>
                <h3>Donatarias</h3>
                <p>Donacion maestra, aplicaciones y seguimiento operativo.</p>
              </div>
              <a routerLink="/donatarias">Abrir modulo</a>
            </div>
            <dl class="stats-list">
              <div><dt>No aplicadas</dt><dd>{{ summaryData.donations.notAppliedCount }}</dd></div>
              <div><dt>Aplicacion parcial</dt><dd>{{ summaryData.donations.partiallyAppliedCount }}</dd></div>
              <div><dt>Monto base</dt><dd>{{ summaryData.donations.baseAmountTotal | number: '1.0-2' }}</dd></div>
              <div><dt>Monto aplicado</dt><dd>{{ summaryData.donations.appliedAmountTotal | number: '1.0-2' }}</dd></div>
            </dl>
          </article>

          <article class="card">
            <div class="card-header">
              <div>
                <h3>Financieras</h3>
                <p>Oficios, créditos individuales y comisiones por crédito.</p>
              </div>
              <a routerLink="/financials">Abrir modulo</a>
            </div>
            <dl class="stats-list">
              <div><dt>Oficios con alerta</dt><dd>{{ summaryData.financials.activeAlertCount }}</dd></div>
              <div><dt>Por vencer</dt><dd>{{ summaryData.financials.dueSoonCount }}</dd></div>
              <div><dt>En renovar</dt><dd>{{ summaryData.financials.renewalCount }}</dd></div>
              <div><dt>Comisiones</dt><dd>{{ summaryData.financials.commissionCount }}</dd></div>
            </dl>
          </article>

          <article class="card">
            <div class="card-header">
              <div>
                <h3>Federación</h3>
                <p>Gestiones, donaciones, aplicaciones y comisión por aplicación.</p>
              </div>
              <a routerLink="/federation">Abrir modulo</a>
            </div>
            <dl class="stats-list">
              <div><dt>Gestiones con seguimiento</dt><dd>{{ summaryData.federation.actionAlertCount }}</dd></div>
              <div><dt>Donaciones con alerta</dt><dd>{{ summaryData.federation.donationAlertCount }}</dd></div>
              <div><dt>Comisiones</dt><dd>{{ summaryData.federation.commissionCount }}</dd></div>
              <div><dt>Evidencias</dt><dd>{{ summaryData.federation.evidenceCount }}</dd></div>
            </dl>
          </article>
        </div>

        <div class="workspace-grid">
          <article class="card">
            <div class="card-header">
              <div>
                <h3>Alertas activas principales</h3>
                <p>Consolidacion minima dentro de la app, sin notificaciones externas.</p>
              </div>
              <button type="button" class="ghost" (click)="reloadPage()">Actualizar</button>
            </div>

            <div class="alerts-grid">
              @for (section of alertSections(); track section.title) {
                <section class="alert-section">
                  <div class="section-heading">
                    <h4>{{ section.title }}</h4>
                    <span>{{ section.items.length }}</span>
                  </div>

                  @if (section.items.length === 0) {
                    <p class="empty-state">Sin alertas activas.</p>
                  } @else {
                    @for (item of section.items.slice(0, 4); track item.alertKey) {
                      <a class="alert-row" [routerLink]="item.navigationPath">
                        <div class="row-top">
                          <strong>{{ item.title }}</strong>
                          <span class="status-pill" [class]="alertStateClass(item.alertState)">
                            {{ alertStateLabel(item.alertState) }}
                          </span>
                        </div>
                        <p>{{ item.subtitle }}</p>
                        <small>{{ item.detail }}</small>
                      </a>
                    }
                  }
                </section>
              }
            </div>
          </article>

          <article class="card">
            <div class="card-header">
              <div>
                <h3>Historico reciente</h3>
                <p>Cerrados visibles para consulta operativa y cierre del MVP.</p>
              </div>
              <a routerLink="/history">Abrir historico</a>
            </div>

            @if (closedPreview().length === 0) {
              <p class="empty-state">No hay registros cerrados visibles en esta base local.</p>
            } @else {
              <div class="history-list">
                @for (item of closedPreview(); track item.recordKey) {
                  <a class="history-row" [routerLink]="item.navigationPath">
                    <div class="row-top">
                      <strong>{{ item.title }}</strong>
                      <span class="status-pill closed">{{ item.statusName }}</span>
                    </div>
                    <p>{{ item.moduleName }} · {{ item.subtitle }}</p>
                    <small>
                      {{ item.historicalTimestampUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}
                      @if (item.reference) {
                        · {{ item.reference }}
                      }
                    </small>
                  </a>
                }
              </div>
            }
          </article>
        </div>
      }
    </section>
  `,
  styles: [
    `
      .page-shell,
      .metrics-grid,
      .module-grid,
      .workspace-grid {
        display: grid;
        gap: 1rem;
      }

      .metrics-grid {
        grid-template-columns: repeat(auto-fit, minmax(13rem, 1fr));
      }

      .module-grid,
      .workspace-grid {
        grid-template-columns: repeat(auto-fit, minmax(19rem, 1fr));
      }

      .hero-card,
      .metric-card,
      .card {
        padding: 1.5rem;
        border-radius: 1.35rem;
        background: rgba(255, 255, 255, 0.82);
        border: 1px solid rgba(29, 45, 42, 0.08);
        box-shadow: 0 16px 30px rgba(32, 44, 41, 0.06);
      }

      .metric-card.accent {
        background: linear-gradient(135deg, rgba(15, 118, 110, 0.14), rgba(255, 255, 255, 0.9));
      }

      .page-kicker,
      .metric-label,
      .status-pill {
        letter-spacing: 0.08em;
        text-transform: uppercase;
      }

      .page-kicker {
        margin: 0 0 0.45rem;
        font-size: 0.78rem;
        font-weight: 700;
        color: #0f766e;
      }

      .hero-card h2,
      .card h3,
      .metric-value,
      .section-heading h4 {
        margin: 0;
      }

      .hero-card p:last-child,
      .card-header p,
      .metric-meta {
        color: #4d615c;
        line-height: 1.6;
      }

      .metric-value {
        margin-top: 0.55rem;
        font-size: 2.1rem;
        font-weight: 800;
        color: #143631;
      }

      .metric-meta {
        margin: 0.45rem 0 0;
      }

      .card-header,
      .row-top,
      .section-heading {
        display: flex;
        align-items: start;
        justify-content: space-between;
        gap: 1rem;
      }

      .card-header a,
      .alert-row,
      .history-row {
        text-decoration: none;
      }

      .card-header a {
        color: #0f766e;
        font-weight: 700;
      }

      .stats-list {
        display: grid;
        gap: 0.8rem;
        margin: 1rem 0 0;
      }

      .stats-list div {
        display: flex;
        align-items: baseline;
        justify-content: space-between;
        gap: 1rem;
        padding-bottom: 0.5rem;
        border-bottom: 1px solid rgba(29, 45, 42, 0.08);
      }

      dt {
        color: #5b6b68;
      }

      dd {
        margin: 0;
        font-weight: 800;
        color: #143631;
      }

      .alerts-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(15rem, 1fr));
        gap: 0.9rem;
        margin-top: 1rem;
      }

      .alert-section {
        padding: 1rem;
        border-radius: 1rem;
        background: rgba(244, 247, 246, 0.9);
        border: 1px solid rgba(29, 45, 42, 0.06);
      }

      .section-heading span {
        min-width: 2rem;
        text-align: center;
        padding: 0.2rem 0.55rem;
        border-radius: 999px;
        background: rgba(15, 118, 110, 0.12);
        color: #0f766e;
        font-weight: 700;
      }

      .alert-row,
      .history-row {
        display: block;
        padding: 0.85rem 0;
        border-top: 1px solid rgba(29, 45, 42, 0.08);
        color: inherit;
      }

      .alert-row:first-of-type,
      .history-row:first-of-type {
        border-top: 0;
      }

      .alert-row p,
      .history-row p,
      .alert-row small,
      .history-row small {
        margin: 0.25rem 0 0;
        color: #5b6b68;
      }

      .history-list {
        margin-top: 0.85rem;
      }

      .status-pill {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        padding: 0.3rem 0.65rem;
        border-radius: 999px;
        font-size: 0.72rem;
        font-weight: 700;
        background: rgba(15, 118, 110, 0.12);
        color: #0f766e;
      }

      .status-pill.closed {
        background: rgba(71, 84, 103, 0.12);
        color: #344054;
      }

      .status-pill.expired,
      .status-pill.rejected,
      .status-pill.not-applied {
        background: rgba(244, 67, 54, 0.12);
        color: #b42318;
      }

      .status-pill.warning,
      .status-pill.partial {
        background: rgba(234, 179, 8, 0.14);
        color: #8a6116;
      }

      .status-pill.renewal,
      .status-pill.follow-up {
        background: rgba(59, 130, 246, 0.14);
        color: #175cd3;
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

      button,
      a {
        border-radius: 0.9rem;
      }
    `
  ]
})
export class DashboardPageComponent {
  private readonly closeoutService = inject(CloseoutService);

  protected readonly isLoading = signal(true);
  protected readonly pageError = signal('');
  protected readonly summary = signal<DashboardSummary | null>(null);
  protected readonly alerts = signal<DashboardAlerts | null>(null);
  protected readonly closedItems = signal<ClosedItem[]>([]);

  protected readonly alertSections = computed(() => {
    const alerts = this.alerts();
    if (!alerts) {
      return [];
    }

    return [
      { title: 'Mercados', items: alerts.marketCertificates },
      { title: 'Donatarias', items: alerts.donations },
      { title: 'Financieras', items: alerts.financialPermits },
      { title: 'Federacion gestiones', items: alerts.federationActions },
      { title: 'Federacion donaciones', items: alerts.federationDonations }
    ];
  });

  protected readonly closedPreview = computed(() => this.closedItems().slice(0, 6));

  constructor() {
    void this.loadDashboard();
  }

  protected async reloadPage() {
    await this.loadDashboard();
  }

  protected alertStateLabel(alertState: string) {
    switch (alertState) {
      case 'DUE_SOON':
        return 'Por vencer';
      case 'EXPIRED':
        return 'Vencido';
      case 'RENEWAL':
        return 'Renovar';
      case 'NOT_APPLIED':
        return 'No aplicada';
      case 'PARTIALLY_APPLIED':
        return 'Parcial';
      case 'IN_PROCESS':
        return 'En proceso';
      case 'FOLLOW_UP_PENDING':
        return 'Seguimiento';
      default:
        return alertState;
    }
  }

  protected alertStateClass(alertState: string) {
    switch (alertState) {
      case 'EXPIRED':
      case 'NOT_APPLIED':
        return 'expired';
      case 'DUE_SOON':
        return 'warning';
      case 'RENEWAL':
        return 'renewal';
      case 'PARTIALLY_APPLIED':
        return 'partial';
      case 'FOLLOW_UP_PENDING':
        return 'follow-up';
      default:
        return '';
    }
  }

  private async loadDashboard() {
    this.isLoading.set(true);
    this.pageError.set('');

    try {
      const [summary, alerts, closedItems] = await Promise.all([
        firstValueFrom(this.closeoutService.getDashboardSummary()),
        firstValueFrom(this.closeoutService.getDashboardAlerts()),
        firstValueFrom(this.closeoutService.getClosedItems())
      ]);

      this.summary.set(summary);
      this.alerts.set(alerts);
      this.closedItems.set(closedItems);
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible cargar el dashboard ejecutivo del MVP.'));
    } finally {
      this.isLoading.set(false);
    }
  }
}
