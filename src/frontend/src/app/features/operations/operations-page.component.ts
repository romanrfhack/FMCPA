import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { OperationsKpi, OperationsSummary, OperationsTimeWindowCode, OperationsWorkQueueItem } from '../../core/models/operations.models';
import { OperationsService } from '../../core/services/operations.service';
import { UserManagementService } from '../../core/services/user-management.service';
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
        <div class="header-actions">
          <div class="filters">
            @for (option of timeWindowOptions; track option.code) {
              <button
                type="button"
                [class.active]="selectedTimeWindowCode() === option.code"
                (click)="setTimeWindow(option.code)">
                {{ option.label }}
              </button>
            }
          </div>
          <button type="button" class="ghost" (click)="reloadPage()">Actualizar</button>
          <button type="button" class="ghost" [disabled]="isExporting()" (click)="exportSummary()">Exportar summary</button>
          <button type="button" class="ghost" [disabled]="isExporting()" (click)="exportWorkQueue()">Exportar bandeja</button>
        </div>
      </header>

      @if (pageError()) {
        <p class="alert error">{{ pageError() }}</p>
      }

      @if (pageSuccess()) {
        <p class="alert success">{{ pageSuccess() }}</p>
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
                <article class="queue-row">
                  <div class="queue-main">
                    <div class="row-top">
                      <strong>{{ item.title }}</strong>
                      <div class="badges">
                        <span
                          class="badge"
                          [class.high]="isSeverity(item.severityCode, 'HIGH')"
                          [class.medium]="isSeverity(item.severityCode, 'MEDIUM')"
                          [class.low]="isSeverity(item.severityCode, 'LOW')">
                          {{ item.severityCode }}
                        </span>
                        <span class="badge action">{{ item.actionKind }}</span>
                      </div>
                    </div>
                    <p>{{ item.summary }}</p>
                    <small>
                      {{ item.categoryCode }} · {{ item.moduleName }} · {{ item.reasonCode }}
                      @if (item.contextLabel) {
                        · {{ item.contextLabel }}
                      }
                      @if (item.relevantUtc) {
                        · {{ item.relevantUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}
                      }
                    </small>
                  </div>
                  <div class="row-actions">
                    <a class="resolve-link" [routerLink]="item.routeHint">
                      {{ item.actionLabel || 'Ir a resolver' }}
                    </a>
                    @if (canRunQuickAction(item)) {
                      <button
                        type="button"
                        class="ghost"
                        [disabled]="isQuickActionRunning(item)"
                        (click)="runQuickAction(item)">
                        {{ isQuickActionRunning(item) ? 'Procesando...' : (item.quickActionLabel || 'Ejecutar') }}
                      </button>
                    }
                  </div>
                </article>
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
      .badges,
      .row-actions,
      .header-actions,
      .filters {
        display: flex;
        gap: 1rem;
      }

      .page-header,
      .panel-header,
      .row-top,
      .compact-row,
      .queue-row,
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
      .resolve-link,
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

      .badge.action {
        background: rgba(77, 124, 15, 0.1);
        color: #365314;
      }

      .compact-row,
      .queue-row {
        padding: 0.8rem 0;
        border-top: 1px solid rgba(29, 45, 42, 0.08);
      }

      .queue-row {
        display: flex;
        gap: 1rem;
      }

      .queue-main {
        min-width: 0;
        display: grid;
        gap: 0.35rem;
      }

      .badges {
        flex-wrap: wrap;
        justify-content: flex-end;
      }

      .row-actions {
        flex-wrap: wrap;
        justify-content: flex-end;
        min-width: 10rem;
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

      .header-actions {
        flex-wrap: wrap;
        justify-content: flex-end;
      }

      .filters button,
      .ghost,
      .resolve-link {
        border: 1px solid rgba(15, 118, 110, 0.18);
        border-radius: 8px;
        padding: 0.55rem 0.75rem;
        background: transparent;
        color: #0f766e;
        font-weight: 800;
      }

      .filters button.active {
        background: rgba(15, 118, 110, 0.12);
      }

      button:disabled {
        opacity: 0.55;
        cursor: not-allowed;
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

      .alert.success {
        background: rgba(220, 252, 231, 0.9);
        color: #166534;
      }

      @media (max-width: 760px) {
        .queue-row,
        .row-top {
          display: grid;
        }

        .row-actions,
        .badges {
          justify-content: flex-start;
        }
      }
    `
  ]
})
export class OperationsPageComponent {
  private readonly operationsService = inject(OperationsService);
  private readonly userManagementService = inject(UserManagementService);

  protected readonly isLoading = signal(true);
  protected readonly isExporting = signal(false);
  protected readonly pageError = signal('');
  protected readonly pageSuccess = signal('');
  protected readonly summary = signal<OperationsSummary | null>(null);
  protected readonly queueItems = signal<OperationsWorkQueueItem[]>([]);
  protected readonly queueTotalCount = signal(0);
  protected readonly queueFilter = signal('');
  protected readonly selectedTimeWindowCode = signal<OperationsTimeWindowCode>('NEXT_30_DAYS');
  protected readonly quickActionItemKeys = signal<Set<string>>(new Set());
  protected readonly timeWindowOptions: Array<{ code: OperationsTimeWindowCode; label: string }> = [
    { code: 'TODAY', label: 'Hoy' },
    { code: 'LAST_7_DAYS', label: 'Ultimos 7 dias' },
    { code: 'NEXT_30_DAYS', label: 'Proximos 30 dias' },
    { code: 'ALL', label: 'Todo' }
  ];

  protected readonly primaryBusinessKpis = computed(() =>
    this.summary()?.businessKpis.slice(0, 4) ?? []);

  constructor() {
    void this.loadPage();
  }

  protected async reloadPage() {
    await this.loadPage();
  }

  protected async exportSummary() {
    await this.runExport(
      () => this.operationsService.exportSummary(this.buildTimeWindowFilters()),
      'No se pudo exportar el resumen operativo.');
  }

  protected async exportWorkQueue() {
    await this.runExport(
      () => this.operationsService.exportWorkQueue({
        ...this.buildTimeWindowFilters(),
        severityCode: this.queueFilter() || null,
        take: 200
      }),
      'No se pudo exportar la bandeja operativa.');
  }

  protected async setQueueFilter(severityCode: string) {
    this.queueFilter.set(severityCode);
    await this.loadQueue();
  }

  protected async setTimeWindow(timeWindowCode: OperationsTimeWindowCode) {
    this.selectedTimeWindowCode.set(timeWindowCode);
    await this.loadPage();
  }

  protected isSeverity(actual: string, expected: string) {
    return actual.trim().toUpperCase() === expected;
  }

  protected canRunQuickAction(item: OperationsWorkQueueItem) {
    return item.quickActionCode === 'UNLOCK_USER' && !!item.entityId;
  }

  protected isQuickActionRunning(item: OperationsWorkQueueItem) {
    return this.quickActionItemKeys().has(item.workItemKey);
  }

  protected async runQuickAction(item: OperationsWorkQueueItem) {
    if (!this.canRunQuickAction(item) || !item.entityId) {
      return;
    }

    this.pageError.set('');
    this.pageSuccess.set('');
    this.quickActionItemKeys.update((current) => new Set(current).add(item.workItemKey));

    try {
      await firstValueFrom(this.userManagementService.unlockUser(item.entityId));
      this.pageSuccess.set('Accion ejecutada correctamente.');
      const [summary] = await Promise.all([
        firstValueFrom(this.operationsService.getSummary(this.buildTimeWindowFilters())),
        this.loadQueue()
      ]);
      this.summary.set(summary);
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible ejecutar la accion.'));
    } finally {
      this.quickActionItemKeys.update((current) => {
        const next = new Set(current);
        next.delete(item.workItemKey);
        return next;
      });
    }
  }

  private async loadPage() {
    this.isLoading.set(true);
    this.pageError.set('');
    this.pageSuccess.set('');

    try {
      const [summary] = await Promise.all([
        firstValueFrom(this.operationsService.getSummary(this.buildTimeWindowFilters())),
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
      ...this.buildTimeWindowFilters(),
      severityCode: this.queueFilter() || null,
      take: 50
    }));

    this.queueItems.set(response.items);
    this.queueTotalCount.set(response.totalCount);
  }

  private async runExport(exportAction: () => Promise<void>, fallbackMessage: string) {
    if (this.isExporting()) {
      return;
    }

    this.pageError.set('');
    this.pageSuccess.set('');
    this.isExporting.set(true);

    try {
      await exportAction();
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, fallbackMessage));
    } finally {
      this.isExporting.set(false);
    }
  }

  private buildTimeWindowFilters() {
    return {
      timeWindowCode: this.selectedTimeWindowCode()
    };
  }
}
