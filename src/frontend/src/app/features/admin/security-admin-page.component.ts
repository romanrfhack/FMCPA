import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import {
  LockedApplicationUser,
  SecurityActivitySummary,
  SecurityAuditEvent
} from '../../core/models/security-observability.models';
import { SecurityObservabilityService } from '../../core/services/security-observability.service';
import { UserManagementService } from '../../core/services/user-management.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  selector: 'app-security-admin-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, ReactiveFormsModule],
  template: `
    <section class="page-shell">
      <article class="hero-card">
        <p class="page-kicker">TRACK 2 SEGURIDAD</p>
        <h2>Operación mínima de seguridad</h2>
        <p>
          Visibilidad administrativa de eventos SECURITY, usuarios con lockout activo y resumen
          operativo básico sobre la auditoría y usuarios internos ya existentes.
        </p>
      </article>

      @if (pageError()) {
        <p class="alert error">{{ pageError() }}</p>
      }

      @if (pageSuccess()) {
        <p class="alert success">{{ pageSuccess() }}</p>
      }

      <form class="filters-card" [formGroup]="filtersForm" (ngSubmit)="reload()">
        <label>
          <span>Usuario</span>
          <input type="text" formControlName="userName" placeholder="admin, operator..." />
        </label>

        <label>
          <span>Tipo de evento</span>
          <select formControlName="eventType">
            <option value="">Todos</option>
            @for (eventType of eventTypeOptions; track eventType) {
              <option [value]="eventType">{{ eventType }}</option>
            }
          </select>
        </label>

        <label>
          <span>Horas recientes</span>
          <input type="number" min="1" max="720" formControlName="hours" />
        </label>

        <label>
          <span>Eventos</span>
          <input type="number" min="1" max="200" formControlName="take" />
        </label>

        <div class="filter-actions">
          <button type="submit" [disabled]="isLoading()">Actualizar</button>
          <button type="button" class="ghost" (click)="resetFilters()">Limpiar</button>
        </div>
      </form>

      @if (summary(); as currentSummary) {
        <section class="summary-grid" aria-label="Resumen de seguridad">
          <article>
            <strong>{{ currentSummary.recentSecurityEventCount }}</strong>
            <span>Eventos SECURITY</span>
          </article>
          <article>
            <strong>{{ currentSummary.loginFailedCount }}</strong>
            <span>Logins fallidos</span>
          </article>
          <article>
            <strong>{{ currentSummary.activeLockedUserCount }}</strong>
            <span>Usuarios bloqueados</span>
          </article>
          <article>
            <strong>{{ currentSummary.usersWithFailedAttemptsCount }}</strong>
            <span>Con fallos acumulados</span>
          </article>
        </section>
      }

      <div class="content-grid">
        <article class="panel-card">
          <div class="card-header">
            <div>
              <h3>Usuarios bloqueados</h3>
              <p>Lockouts activos calculados contra la hora actual del backend.</p>
            </div>
          </div>

          @if (lockedUsers().length === 0) {
            <p class="empty-state">No hay usuarios bloqueados actualmente.</p>
          } @else {
            <div class="locked-list">
              @for (user of lockedUsers(); track user.id) {
                <article class="locked-row">
                  <div>
                    <h4>{{ user.displayName }}</h4>
                    <p>{{ user.userName }} · {{ user.roleCode }}</p>
                    <small>
                      {{ user.accessFailedCount }} fallos · hasta
                      {{ user.lockoutEndUtc ? (user.lockoutEndUtc | date: 'yyyy-MM-dd HH:mm':'UTC') : 'sin fecha' }}
                    </small>
                  </div>
                  <button type="button" class="ghost" [disabled]="isUnlocking(user.id)" (click)="unlock(user)">
                    Limpiar lockout
                  </button>
                </article>
              }
            </div>
          }
        </article>

        <article class="panel-card">
          <div class="card-header">
            <div>
              <h3>Eventos recientes</h3>
              <p>Eventos SECURITY filtrados por usuario, tipo y rango reciente.</p>
            </div>
          </div>

          @if (isLoading()) {
            <p class="empty-state">Cargando eventos de seguridad...</p>
          } @else if (events().length === 0) {
            <p class="empty-state">No hay eventos con los filtros actuales.</p>
          } @else {
            <div class="event-list">
              @for (event of events(); track event.id) {
                <article class="event-row">
                  <div class="event-main">
                    <span class="badge">{{ event.actionType }}</span>
                    <h4>{{ event.title }}</h4>
                    <p>{{ event.detail }}</p>
                  </div>
                  <dl>
                    <div>
                      <dt>UTC</dt>
                      <dd>{{ event.occurredUtc | date: 'yyyy-MM-dd HH:mm:ss':'UTC' }}</dd>
                    </div>
                    <div>
                      <dt>Usuario</dt>
                      <dd>{{ event.reference || 'Sin referencia' }}</dd>
                    </div>
                    <div>
                      <dt>Entidad</dt>
                      <dd>{{ event.entityType }} · {{ event.entityId }}</dd>
                    </div>
                  </dl>
                </article>
              }
            </div>
          }
        </article>
      </div>
    </section>
  `,
  styles: [
    `
      .page-shell,
      .content-grid,
      .event-list,
      .locked-list {
        display: grid;
        gap: 1rem;
      }

      .hero-card,
      .filters-card,
      .panel-card,
      .summary-grid article {
        padding: 1.25rem;
        border-radius: 1rem;
        background: rgba(255, 255, 255, 0.86);
        border: 1px solid rgba(29, 45, 42, 0.08);
        box-shadow: 0 14px 26px rgba(32, 44, 41, 0.06);
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
      dl,
      dd {
        margin: 0;
      }

      .hero-card p:last-child,
      .card-header p,
      .event-main p,
      .locked-row p,
      .empty-state {
        margin-top: 0.5rem;
        color: #4d615c;
        line-height: 1.5;
      }

      .filters-card {
        display: grid;
        grid-template-columns: repeat(5, minmax(0, 1fr));
        gap: 0.9rem;
        align-items: end;
      }

      label {
        display: grid;
        gap: 0.35rem;
        font-weight: 700;
        color: #23332f;
      }

      input,
      select {
        width: 100%;
        border: 1px solid rgba(35, 51, 47, 0.16);
        border-radius: 0.7rem;
        padding: 0.7rem 0.8rem;
        font: inherit;
        background: #fff;
      }

      button {
        border: 0;
        border-radius: 0.7rem;
        padding: 0.75rem 1rem;
        font-weight: 800;
        color: #fff;
        background: #0f766e;
        cursor: pointer;
      }

      button:disabled {
        opacity: 0.55;
        cursor: not-allowed;
      }

      button.ghost {
        color: #0f766e;
        background: rgba(15, 118, 110, 0.1);
      }

      .filter-actions {
        display: flex;
        gap: 0.5rem;
      }

      .summary-grid {
        display: grid;
        grid-template-columns: repeat(4, minmax(0, 1fr));
        gap: 1rem;
      }

      .summary-grid strong {
        display: block;
        font-size: 1.8rem;
        color: #0f766e;
      }

      .summary-grid span {
        color: #4d615c;
        font-weight: 700;
      }

      .content-grid {
        grid-template-columns: minmax(19rem, 24rem) minmax(0, 1fr);
        align-items: start;
      }

      .card-header,
      .locked-row,
      .event-row {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
      }

      .locked-row,
      .event-row {
        padding: 1rem 0;
        border-top: 1px solid rgba(35, 51, 47, 0.1);
      }

      .locked-row:first-child,
      .event-row:first-child {
        border-top: 0;
      }

      .badge {
        display: inline-flex;
        margin-bottom: 0.45rem;
        border-radius: 999px;
        padding: 0.25rem 0.55rem;
        background: rgba(15, 118, 110, 0.12);
        color: #0f766e;
        font-size: 0.76rem;
        font-weight: 800;
      }

      dl {
        min-width: 15rem;
        display: grid;
        gap: 0.45rem;
        color: #4d615c;
        font-size: 0.88rem;
      }

      dt {
        font-weight: 800;
        color: #23332f;
      }

      .alert {
        padding: 0.85rem 1rem;
        border-radius: 0.85rem;
        font-weight: 700;
      }

      .alert.error {
        color: #991b1b;
        background: #fee2e2;
      }

      .alert.success {
        color: #166534;
        background: #dcfce7;
      }

      @media (max-width: 960px) {
        .filters-card,
        .summary-grid,
        .content-grid {
          grid-template-columns: 1fr;
        }

        .card-header,
        .locked-row,
        .event-row {
          display: grid;
        }
      }
    `
  ]
})
export class SecurityAdminPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly securityService = inject(SecurityObservabilityService);
  private readonly userManagementService = inject(UserManagementService);

  protected readonly eventTypeOptions = [
    'AUTH_LOGIN_SUCCEEDED',
    'AUTH_LOGIN_FAILED',
    'AUTH_LOGIN_LOCKOUT_DENIED',
    'USER_TEMPORARILY_LOCKED',
    'USER_LOCKOUT_RESET',
    'USER_PASSWORD_RESET',
    'USER_ROLE_CHANGED',
    'USER_DEACTIVATED',
    'USER_ACTIVATED',
    'AUTH_PASSWORD_CHANGED_SELF_SERVICE'
  ];
  protected readonly filtersForm = this.formBuilder.nonNullable.group({
    userName: [''],
    eventType: [''],
    hours: [24],
    take: [50]
  });
  protected readonly summary = signal<SecurityActivitySummary | null>(null);
  protected readonly events = signal<SecurityAuditEvent[]>([]);
  protected readonly lockedUsers = signal<LockedApplicationUser[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly unlockingUserIds = signal<Set<string>>(new Set());
  protected readonly pageError = signal<string | null>(null);
  protected readonly pageSuccess = signal<string | null>(null);

  constructor() {
    void this.reload();
  }

  protected async reload() {
    this.isLoading.set(true);
    this.pageError.set(null);
    this.pageSuccess.set(null);

    try {
      const hours = this.normalizeNumber(this.filtersForm.controls.hours.value, 24, 1, 720);
      const take = this.normalizeNumber(this.filtersForm.controls.take.value, 50, 1, 200);
      const fromUtc = new Date(Date.now() - hours * 60 * 60_000).toISOString();
      const [summary, events, lockedUsers] = await Promise.all([
        firstValueFrom(this.securityService.getSummary(hours)),
        firstValueFrom(this.securityService.getEvents({
          userName: this.filtersForm.controls.userName.value,
          eventType: this.filtersForm.controls.eventType.value,
          fromUtc,
          take
        })),
        firstValueFrom(this.securityService.getLockedUsers())
      ]);

      this.summary.set(summary);
      this.events.set(events);
      this.lockedUsers.set(lockedUsers);
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible cargar la operación de seguridad.'));
    } finally {
      this.isLoading.set(false);
    }
  }

  protected resetFilters() {
    this.filtersForm.reset({
      userName: '',
      eventType: '',
      hours: 24,
      take: 50
    });
    void this.reload();
  }

  protected async unlock(user: LockedApplicationUser) {
    this.pageError.set(null);
    this.pageSuccess.set(null);
    this.unlockingUserIds.update((current) => new Set(current).add(user.id));

    try {
      await firstValueFrom(this.userManagementService.unlockUser(user.id));
      this.pageSuccess.set(`Lockout limpiado para ${user.userName}.`);
      await this.reload();
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible limpiar el lockout.'));
    } finally {
      this.unlockingUserIds.update((current) => {
        const next = new Set(current);
        next.delete(user.id);
        return next;
      });
    }
  }

  protected isUnlocking(userId: string): boolean {
    return this.unlockingUserIds().has(userId);
  }

  private normalizeNumber(value: number, fallback: number, min: number, max: number): number {
    if (!Number.isFinite(value)) {
      return fallback;
    }

    return Math.min(Math.max(Math.trunc(value), min), max);
  }
}
