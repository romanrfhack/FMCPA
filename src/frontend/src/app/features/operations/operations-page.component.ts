import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { OperationsKpi, OperationsSummary, OperationsWorkQueueItem } from '../../core/models/operations.models';
import { OperationsService } from '../../core/services/operations.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  selector: 'app-operations-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DecimalPipe, RouterLink],
  template: `
    <section class="page-shell">
      <header class="page-header">
        <div>
          <p class="page-kicker">TRACK 4</p>
          <h2>Centro operativo</h2>
        </div>
        <button type="button" class="ghost" (click)="reloadPage()">Actualizar</button>
      </header>

      @if (pageError()) {
        <p class="alert error">{{ pageError() }}</p>
      }

      @if (isLoading()) {
        <article class="panel">
          <p class="empty-state">Cargando centro operativo...</p>
        </article>
      } @else if (summary(); as summaryData) {
        <div class="metrics-grid">
          @for (kpi of primaryBusinessKpis(); track kpi.kpiCode) {
            <a class="metric-card" [routerLink]="kpi.routeHint">
              <span
                class="badge"
                [class.high]="isSeverity(kpi.severityCode, 'HIGH')"
                [class.medium]="isSeverity(kpi.severityCode, 'MEDIUM')"
                [class.low]="isSeverity(kpi.severityCode, 'LOW')">
                {{ kpi.severityCode }}
              </span>
              <p class="metric-label">{{ kpi.label }}</p>
              <p class="metric-value">{{ kpi.count }}</p>
              @if (kpi.amount !== null) {
                <p class="metric-meta">{{ kpi.amount | number: '1.0-2' }}</p>
              } @else {
                <p class="metric-meta">{{ kpi.moduleName || 'Operacion' }}</p>
              }
            </a>
          }

          @if (summaryData.documents; as documents) {
            <a class="metric-card" routerLink="/documents">
              <span class="badge" [class.high]="documents.integrityIssuesCount > 0" [class.low]="documents.integrityIssuesCount === 0">DOCUMENTS</span>
              <p class="metric-label">Pendientes documentales</p>
              <p class="metric-value">
                {{ documents.integrityIssuesCount + documents.incompleteEntitiesCount + documents.reviewDueCount + documents.expiredRetentionCount }}
              </p>
              <p class="metric-meta">{{ documents.totalDocuments }} documentos</p>
            </a>
          }

          @if (summaryData.security; as security) {
            <a class="metric-card" routerLink="/admin/security">
              <span class="badge" [class.high]="security.activeLockedUserCount > 0" [class.low]="security.activeLockedUserCount === 0">SECURITY</span>
              <p class="metric-label">Usuarios bloqueados</p>
              <p class="metric-value">{{ security.activeLockedUserCount }}</p>
              <p class="metric-meta">{{ security.recentSecurityEventCount }} eventos recientes</p>
            </a>
          }
        </div>

        <div class="sections-grid">
          <article class="panel">
            <div class="panel-header">
              <h3>Operacion</h3>
              <a routerLink="/dashboard">Dashboard</a>
            </div>
            <div class="compact-list">
              @for (kpi of summaryData.businessKpis; track kpi.kpiCode) {
                <a class="compact-row" [routerLink]="kpi.routeHint">
                  <span>{{ kpi.label }}</span>
                  <strong>{{ kpi.count }}</strong>
                </a>
              }
            </div>
          </article>

          <article class="panel">
            <div class="panel-header">
              <h3>Documentos</h3>
              <a routerLink="/documents/work-queue">Bandeja</a>
            </div>
            @if (summaryData.documents; as documents) {
              <dl class="stats-list">
                <div><dt>Integridad</dt><dd>{{ documents.integrityIssuesCount }}</dd></div>
                <div><dt>Completitud</dt><dd>{{ documents.incompleteEntitiesCount }}</dd></div>
                <div><dt>Review due</dt><dd>{{ documents.reviewDueCount }}</dd></div>
                <div><dt>Retention expired</dt><dd>{{ documents.expiredRetentionCount }}</dd></div>
                <div><dt>Hold administrativo</dt><dd>{{ documents.administrativeHoldCount }}</dd></div>
              </dl>
            } @else {
              <p class="empty-state">Sin modulos documentales visibles.</p>
            }
          </article>

          <article class="panel">
            <div class="panel-header">
              <h3>Seguridad</h3>
              @if (summaryData.security) {
                <a routerLink="/admin/security">Observabilidad</a>
              }
            </div>
            @if (summaryData.security; as security) {
              <dl class="stats-list">
                <div><dt>Eventos recientes</dt><dd>{{ security.recentSecurityEventCount }}</dd></div>
                <div><dt>Login failed</dt><dd>{{ security.loginFailedCount }}</dd></div>
                <div><dt>Lockout activos</dt><dd>{{ security.activeLockedUserCount }}</dd></div>
                <div><dt>Intentos acumulados</dt><dd>{{ security.usersWithFailedAttemptsCount }}</dd></div>
              </dl>
            } @else {
              <p class="empty-state">Superficie restringida.</p>
            }
          </article>
        </div>

        <article class="panel">
          <div class="panel-header">
            <div>
              <h3>Bandeja transversal</h3>
              <p>{{ queueItems().length }} de {{ queueTotalCount() }} items</p>
            </div>
            <div class="filters">
              <button type="button" [class.active]="queueFilter() === ''" (click)="setQueueFilter('')">Todo</button>
              <button type="button" [class.active]="queueFilter() === 'HIGH'" (click)="setQueueFilter('HIGH')">HIGH</button>
              <button type="button" [class.active]="queueFilter() === 'MEDIUM'" (click)="setQueueFilter('MEDIUM')">MEDIUM</button>
              <button type="button" [class.active]="queueFilter() === 'LOW'" (click)="setQueueFilter('LOW')">LOW</button>
            </div>
          </div>

          @if (queueItems().length === 0) {
            <p class="empty-state">Sin items accionables visibles.</p>
          } @else {
            <div class="queue-list">
              @for (item of queueItems(); track item.workItemKey) {
                <a class="queue-row" [routerLink]="item.routeHint">
                  <div class="row-top">
                    <strong>{{ item.title }}</strong>
                    <span
                      class="badge"
                      [class.high]="isSeverity(item.severityCode, 'HIGH')"
                      [class.medium]="isSeverity(item.severityCode, 'MEDIUM')"
                      [class.low]="isSeverity(item.severityCode, 'LOW')">
                      {{ item.severityCode }}
                    </span>
                  </div>
                  <p>{{ item.summary }}</p>
                  <small>
                    {{ item.categoryCode }} · {{ item.moduleName }} · {{ item.reasonCode }}
                    @if (item.relevantUtc) {
                      · {{ item.relevantUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}
                    }
                  </small>
                </a>
              }
            </div>
          }
        </article>
      }
    </section>
  `,
  styles: [
    `
      .page-shell,
      .metrics-grid,
      .sections-grid,
      .queue-list,
      .compact-list,
      .stats-list {
        display: grid;
        gap: 1rem;
      }

      .page-header,
      .panel-header,
      .row-top,
      .compact-row,
      .stats-list div,
      .filters {
        display: flex;
        gap: 1rem;
      }

      .page-header,
      .panel-header,
      .row-top,
      .compact-row,
      .stats-list div {
        align-items: start;
        justify-content: space-between;
      }

      .metrics-grid {
        grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr));
      }

      .sections-grid {
        grid-template-columns: repeat(auto-fit, minmax(18rem, 1fr));
      }

      .panel,
      .metric-card {
        padding: 1rem;
        border: 1px solid rgba(29, 45, 42, 0.1);
        border-radius: 8px;
        background: rgba(255, 255, 255, 0.86);
      }

      .metric-card,
      .compact-row,
      .queue-row,
      .panel-header a {
        color: inherit;
        text-decoration: none;
      }

      .page-kicker,
      .metric-label,
      .badge {
        margin: 0;
        font-size: 0.72rem;
        font-weight: 800;
        letter-spacing: 0.04em;
        text-transform: uppercase;
      }

      h2,
      h3,
      .metric-value {
        margin: 0;
      }

      .metric-value {
        margin-top: 0.45rem;
        font-size: 2rem;
        font-weight: 800;
      }

      .metric-meta,
      .panel-header p,
      .queue-row p,
      .queue-row small,
      .empty-state,
      dt {
        color: #586762;
      }

      .badge {
        display: inline-flex;
        padding: 0.25rem 0.55rem;
        border-radius: 999px;
        background: rgba(15, 118, 110, 0.12);
        color: #0f766e;
      }

      .badge.high {
        background: rgba(244, 67, 54, 0.12);
        color: #b42318;
      }

      .badge.medium {
        background: rgba(234, 179, 8, 0.16);
        color: #8a6116;
      }

      .badge.low {
        background: rgba(59, 130, 246, 0.12);
        color: #175cd3;
      }

      .compact-row,
      .queue-row {
        padding: 0.8rem 0;
        border-top: 1px solid rgba(29, 45, 42, 0.08);
      }

      .compact-row:first-child,
      .queue-row:first-child {
        border-top: 0;
      }

      dd {
        margin: 0;
        font-weight: 800;
      }

      .filters {
        flex-wrap: wrap;
      }

      .filters button,
      .ghost {
        border: 1px solid rgba(15, 118, 110, 0.18);
        background: transparent;
        color: #0f766e;
      }

      .filters button.active {
        background: rgba(15, 118, 110, 0.12);
      }

      .alert {
        padding: 0.9rem 1rem;
        border-radius: 8px;
        font-weight: 700;
      }

      .alert.error {
        background: rgba(254, 243, 242, 0.9);
        color: #b42318;
      }
    `
  ]
})
export class OperationsPageComponent {
  private readonly operationsService = inject(OperationsService);

  protected readonly isLoading = signal(true);
  protected readonly pageError = signal('');
  protected readonly summary = signal<OperationsSummary | null>(null);
  protected readonly queueItems = signal<OperationsWorkQueueItem[]>([]);
  protected readonly queueTotalCount = signal(0);
  protected readonly queueFilter = signal('');

  protected readonly primaryBusinessKpis = computed(() =>
    this.summary()?.businessKpis.slice(0, 4) ?? []);

  constructor() {
    void this.loadPage();
  }

  protected async reloadPage() {
    await this.loadPage();
  }

  protected async setQueueFilter(severityCode: string) {
    this.queueFilter.set(severityCode);
    await this.loadQueue();
  }

  protected isSeverity(actual: string, expected: string) {
    return actual.trim().toUpperCase() === expected;
  }

  private async loadPage() {
    this.isLoading.set(true);
    this.pageError.set('');

    try {
      const [summary] = await Promise.all([
        firstValueFrom(this.operationsService.getSummary()),
        this.loadQueue()
      ]);
      this.summary.set(summary);
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible cargar el centro operativo.'));
    } finally {
      this.isLoading.set(false);
    }
  }

  private async loadQueue() {
    const response = await firstValueFrom(this.operationsService.getWorkQueue({
      severityCode: this.queueFilter() || null,
      take: 50
    }));

    this.queueItems.set(response.items);
    this.queueTotalCount.set(response.totalCount);
  }
}
