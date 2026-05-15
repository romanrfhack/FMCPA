import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { ApplicationRoleCode } from '../../core/models/auth.models';
import { ApplicationUserAdmin } from '../../core/models/user-management.models';
import { AuthService } from '../../core/services/auth.service';
import { UserManagementService } from '../../core/services/user-management.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  selector: 'app-users-admin-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, ReactiveFormsModule],
  template: `
    <section class="page-shell">
      <article class="hero-card">
        <div>
          <p class="page-kicker">Administración</p>
          <h2>Usuarios internos</h2>
          <p>
            Gestiona altas, roles, estado, contraseñas temporales y bloqueos operativos de las cuentas internas.
          </p>
        </div>

        <div class="hero-actions">
          <button type="button" (click)="openCreateDialog()">Crear usuario</button>
          <button type="button" class="ghost" (click)="reload()">Actualizar</button>
        </div>
      </article>

      @if (pageError()) {
        <p class="alert error">{{ pageError() }}</p>
      }

      @if (pageSuccess()) {
        <p class="alert success">{{ pageSuccess() }}</p>
      }

      <article class="list-card">
        <div class="card-header">
          <div>
            <h3>Usuarios registrados</h3>
            <p>Administración de cuentas internas sin invitaciones ni borrado físico.</p>
          </div>
          <span class="badge neutral">{{ users().length }} usuarios</span>
        </div>

        @if (isLoading()) {
          <p class="empty-state">Cargando usuarios internos...</p>
        } @else if (users().length === 0) {
          <p class="empty-state">No hay usuarios registrados.</p>
        } @else {
          <div class="user-list">
            @for (user of users(); track user.id) {
              <article class="user-row">
                <div class="row-top">
                  <div class="identity">
                    <h4>{{ user.displayName }}</h4>
                    <p class="meta">{{ user.userName }}</p>
                  </div>

                  <div class="row-badges" aria-label="Estado del usuario">
                    @if (currentUserId() === user.id) {
                      <span class="badge neutral">Tu sesión</span>
                    }
                    <span class="badge">{{ getRoleLabel(user.roleCode) }}</span>
                    <span class="badge" [class.inactive]="!user.isActive">
                      {{ user.isActive ? 'Activo' : 'Inactivo' }}
                    </span>
                    @if (user.isLockedOut) {
                      <span class="badge inactive">Bloqueado</span>
                    }
                  </div>
                </div>

                <dl class="detail-grid">
                  <div>
                    <dt>Alta</dt>
                    <dd>{{ user.createdUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</dd>
                  </div>
                  <div>
                    <dt>Actualización</dt>
                    <dd>{{ user.updatedUtc ? (user.updatedUtc | date: 'yyyy-MM-dd HH:mm':'UTC') : 'Sin cambios' }}</dd>
                  </div>
                  <div>
                    <dt>Último acceso</dt>
                    <dd>{{ user.lastLoginUtc ? (user.lastLoginUtc | date: 'yyyy-MM-dd HH:mm':'UTC') : 'Sin acceso registrado' }}</dd>
                  </div>
                  <div>
                    <dt>Fallos</dt>
                    <dd>{{ user.accessFailedCount }}</dd>
                  </div>
                  <div>
                    <dt>Bloqueado hasta</dt>
                    <dd>{{ user.lockoutEndUtc ? (user.lockoutEndUtc | date: 'yyyy-MM-dd HH:mm':'UTC') : 'Sin bloqueo' }}</dd>
                  </div>
                </dl>

                <div class="row-actions">
                  <button
                    type="button"
                    class="ghost compact-button"
                    [attr.aria-expanded]="isUserActionsOpen(user.id)"
                    (click)="toggleUserActions(user.id)">
                    {{ isUserActionsOpen(user.id) ? 'Cerrar gestión' : 'Gestionar' }}
                  </button>

                  <button
                    type="button"
                    class="ghost compact-button"
                    [disabled]="isRowBusy(user.id)"
                    (click)="toggleActivation(user)">
                    {{ user.isActive ? 'Desactivar' : 'Reactivar' }}
                  </button>
                </div>

                @if (isUserActionsOpen(user.id)) {
                  <div class="actions-panel" role="group" [attr.aria-label]="'Acciones para ' + user.displayName">
                    <div class="action-group">
                      <label>
                        <span>Rol</span>
                        <select
                          [value]="getRoleDraft(user)"
                          [disabled]="isRowBusy(user.id)"
                          (change)="setRoleDraft(user.id, $any($event.target).value)">
                          @for (roleCode of roleOptions; track roleCode) {
                            <option [value]="roleCode">{{ getRoleLabel(roleCode) }}</option>
                          }
                        </select>
                      </label>

                      <button
                        type="button"
                        [disabled]="isRowBusy(user.id) || getRoleDraft(user) === user.roleCode"
                        (click)="applyRoleChange(user)">
                        Guardar rol
                      </button>
                    </div>

                    <div class="action-group password-action">
                      <label class="password-field">
                        <span>Contraseña temporal</span>
                        <input
                          type="password"
                          [value]="getPasswordDraft(user.id)"
                          [disabled]="isRowBusy(user.id)"
                          placeholder="12+ con mayúscula, minúscula y número"
                          (input)="setPasswordDraft(user.id, $any($event.target).value)" />
                      </label>

                      <button
                        type="button"
                        [disabled]="isRowBusy(user.id) || !passwordMeetsPolicy(getPasswordDraft(user.id))"
                        (click)="resetPassword(user)">
                        Restablecer contraseña
                      </button>
                    </div>

                    <div class="action-group lockout-action">
                      <p>
                        Bloqueo:
                        <strong>{{ user.isLockedOut ? 'activo' : 'sin bloqueo activo' }}</strong>
                      </p>
                      <button
                        type="button"
                        class="ghost"
                        [disabled]="isRowBusy(user.id) || (user.accessFailedCount === 0 && !user.lockoutEndUtc)"
                        (click)="unlockUser(user)">
                        Limpiar bloqueo
                      </button>
                    </div>
                  </div>
                }
              </article>
            }
          </div>
        }
      </article>

      @if (isCreateDialogOpen()) {
        <div class="dialog-backdrop" (click)="closeCreateDialog()">
          <section
            class="dialog-panel"
            role="dialog"
            aria-modal="true"
            aria-labelledby="create-user-title"
            (click)="$event.stopPropagation()"
            (keydown.escape)="closeCreateDialog()">
            <div class="card-header">
              <div>
                <h3 id="create-user-title">Crear usuario</h3>
                <p>El usuario se crea activo y con contraseña temporal definida por administración.</p>
              </div>
              @if (isSubmitting()) {
                <span class="badge neutral">Guardando...</span>
              }
            </div>

            <form class="form-grid" [formGroup]="form" (ngSubmit)="submitCreateUser()">
              <label>
                <span>Usuario</span>
                <input type="text" formControlName="userName" placeholder="operador-interno" />
              </label>

              <label>
                <span>Nombre visible</span>
                <input type="text" formControlName="displayName" placeholder="Operador interno" />
              </label>

              <label>
                <span>Rol</span>
                <select formControlName="roleCode">
                  @for (roleCode of roleOptions; track roleCode) {
                    <option [value]="roleCode">{{ getRoleLabel(roleCode) }}</option>
                  }
                </select>
              </label>

              <label>
                <span>Contraseña inicial</span>
                <input type="password" formControlName="password" placeholder="12+ con mayúscula, minúscula y número" />
              </label>

              <div class="form-actions">
                <button type="submit" [disabled]="isSubmitting()">Crear usuario</button>
                <button type="button" class="ghost" (click)="resetCreateForm()">Limpiar</button>
                <button type="button" class="ghost" (click)="closeCreateDialog()">Cancelar</button>
              </div>
            </form>

            <p class="card-note">
              Los cambios de rol, estado o contraseña invalidan accesos previos del usuario afectado.
            </p>
          </section>
        </div>
      }
    </section>
  `,
  styles: [
    `
      .page-shell,
      .form-grid,
      .user-list {
        display: grid;
        gap: 1rem;
        min-width: 0;
      }

      .hero-card,
      .list-card {
        padding: 1.15rem;
        border-radius: 1rem;
        background: rgba(255, 255, 255, 0.86);
        border: 1px solid rgba(29, 45, 42, 0.08);
        box-shadow: 0 14px 26px rgba(32, 44, 41, 0.06);
        min-width: 0;
      }

      .hero-card {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        align-items: flex-start;
      }

      .page-kicker {
        margin: 0 0 0.5rem;
        letter-spacing: 0.08em;
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
        overflow-wrap: anywhere;
      }

      .hero-card p:last-child,
      .card-header p,
      .meta,
      .empty-state,
      .card-note {
        margin-top: 0.5rem;
        line-height: 1.5;
        color: #4d615c;
      }

      .hero-actions,
      .card-header,
      .row-top,
      .row-actions {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        align-items: flex-start;
      }

      .card-header {
        margin-bottom: 0.9rem;
      }

      .alert,
      .badge {
        padding: 0.55rem 0.75rem;
        border-radius: 999px;
        font-weight: 700;
      }

      .alert.error {
        background: rgba(180, 35, 24, 0.08);
        color: #b42318;
      }

      .alert.success {
        background: rgba(15, 118, 110, 0.1);
        color: #0f766e;
      }

      .badge {
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
        font-size: 0.8rem;
        line-height: 1;
      }

      .badge.neutral {
        background: rgba(17, 24, 39, 0.08);
        color: #374151;
      }

      .badge.inactive {
        background: rgba(180, 35, 24, 0.08);
        color: #b42318;
      }

      .row-badges {
        display: flex;
        flex-wrap: wrap;
        justify-content: flex-end;
        gap: 0.5rem;
      }

      label {
        display: grid;
        gap: 0.4rem;
        font-size: 0.92rem;
        font-weight: 600;
        color: #29403b;
        min-width: 0;
      }

      input,
      select {
        width: 100%;
        min-width: 0;
        padding: 0.7rem 0.8rem;
        border-radius: 0.75rem;
        border: 1px solid rgba(29, 45, 42, 0.14);
        background: #fbfbf8;
        color: #1d2d2a;
        font: inherit;
      }

      button {
        border: none;
        border-radius: 0.75rem;
        padding: 0.72rem 0.95rem;
        font: inherit;
        font-weight: 700;
        cursor: pointer;
        background: #123f3b;
        color: #f6f6f2;
        white-space: nowrap;
      }

      button.ghost {
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
      }

      button:focus-visible,
      input:focus-visible,
      select:focus-visible {
        outline: 3px solid rgba(15, 118, 110, 0.35);
        outline-offset: 2px;
      }

      button:disabled {
        opacity: 0.7;
        cursor: wait;
      }

      .hero-actions,
      .form-actions {
        display: flex;
        gap: 0.75rem;
        flex-wrap: wrap;
        justify-content: flex-end;
      }

      .user-row {
        display: grid;
        gap: 0.85rem;
        padding: 1rem;
        border-radius: 1rem;
        background: #f6f5ef;
        border: 1px solid rgba(29, 45, 42, 0.07);
        min-width: 0;
      }

      .detail-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(9rem, 1fr));
        gap: 0.75rem;
        margin: 0;
      }

      dt {
        font-size: 0.75rem;
        font-weight: 700;
        letter-spacing: 0.04em;
        text-transform: uppercase;
        color: #5d736f;
      }

      dd {
        margin-top: 0.35rem;
        color: #23312f;
        line-height: 1.5;
      }

      .row-actions {
        justify-content: flex-end;
        flex-wrap: wrap;
      }

      .compact-button {
        padding-inline: 0.85rem;
      }

      .actions-panel {
        display: grid;
        grid-template-columns: minmax(13rem, 0.8fr) minmax(16rem, 1fr) minmax(12rem, 0.85fr);
        gap: 0.75rem;
        align-items: stretch;
        padding: 0.85rem;
        border-radius: 0.85rem;
        background: rgba(255, 255, 255, 0.72);
        border: 1px solid rgba(15, 118, 110, 0.12);
      }

      .action-group {
        display: grid;
        grid-template-columns: minmax(0, 1fr) auto;
        gap: 0.65rem;
        align-items: end;
        min-width: 0;
      }

      .lockout-action {
        grid-template-columns: minmax(0, 1fr) auto;
        align-items: center;
      }

      .lockout-action p {
        color: #4d615c;
      }

      .password-field {
        min-width: 0;
      }

      .dialog-backdrop {
        position: fixed;
        inset: 0;
        z-index: 20;
        display: grid;
        place-items: center;
        padding: 1rem;
        background: rgba(10, 23, 20, 0.42);
      }

      .dialog-panel {
        width: min(100%, 39rem);
        max-height: min(92vh, 46rem);
        overflow: auto;
        padding: 1.15rem;
        border-radius: 1rem;
        background: #fffdf8;
        border: 1px solid rgba(29, 45, 42, 0.1);
        box-shadow: 0 24px 60px rgba(18, 63, 59, 0.2);
      }

      @media (max-width: 1180px) {
        .actions-panel {
          grid-template-columns: 1fr;
        }

        .action-group {
          grid-template-columns: minmax(0, 1fr) max-content;
        }
      }

      @media (max-width: 720px) {
        .hero-card,
        .card-header,
        .row-top {
          flex-direction: column;
        }

        .hero-actions,
        .row-actions {
          width: 100%;
          justify-content: stretch;
        }

        .hero-actions button,
        .row-actions button,
        .form-actions button {
          width: 100%;
        }

        .form-actions,
        .action-group,
        .lockout-action {
          grid-template-columns: 1fr;
          display: grid;
        }

        button {
          white-space: normal;
        }

        .row-badges {
          justify-content: flex-start;
        }
      }
    `
  ]
})
export class UsersAdminPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly userManagementService = inject(UserManagementService);
  private readonly passwordPolicyPattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{12,}$/;

  protected readonly roleOptions: ApplicationRoleCode[] = ['ADMIN', 'OPERATOR', 'READONLY'];
  protected readonly users = signal<ApplicationUserAdmin[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly isSubmitting = signal(false);
  protected readonly pageError = signal<string | null>(null);
  protected readonly pageSuccess = signal<string | null>(null);
  protected readonly rowBusyState = signal<Record<string, boolean>>({});
  protected readonly roleDrafts = signal<Record<string, ApplicationRoleCode>>({});
  protected readonly passwordDrafts = signal<Record<string, string>>({});
  protected readonly isCreateDialogOpen = signal(false);
  protected readonly expandedUserActionsId = signal<string | null>(null);
  protected readonly currentUserId = computed(() => this.authService.currentUser()?.id ?? null);

  protected readonly form = this.formBuilder.nonNullable.group({
    userName: ['', [Validators.required, Validators.maxLength(64)]],
    displayName: ['', [Validators.required, Validators.maxLength(128)]],
    roleCode: ['READONLY' as ApplicationRoleCode, [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(12), Validators.pattern(this.passwordPolicyPattern)]]
  });

  constructor() {
    void this.reload();
  }

  protected async reload(): Promise<void> {
    this.pageError.set(null);

    if (this.users().length === 0) {
      this.isLoading.set(true);
    }

    try {
      const users = await firstValueFrom(this.userManagementService.getUsers());
      this.users.set(users);
      this.roleDrafts.set(
        Object.fromEntries(users.map((user) => [user.id, user.roleCode])) as Record<string, ApplicationRoleCode>);
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No se pudo cargar la administración de usuarios.'));
    } finally {
      this.isLoading.set(false);
    }
  }

  protected resetCreateForm(clearFeedback = true): void {
    this.form.reset({
      userName: '',
      displayName: '',
      roleCode: 'READONLY',
      password: ''
    });

    if (clearFeedback) {
      this.pageError.set(null);
      this.pageSuccess.set(null);
    }
  }

  protected openCreateDialog(): void {
    this.resetCreateForm();
    this.isCreateDialogOpen.set(true);
  }

  protected closeCreateDialog(): void {
    if (this.isSubmitting()) {
      return;
    }

    this.isCreateDialogOpen.set(false);
  }

  protected isUserActionsOpen(userId: string): boolean {
    return this.expandedUserActionsId() === userId;
  }

  protected toggleUserActions(userId: string): void {
    this.expandedUserActionsId.update((currentUserId) => (currentUserId === userId ? null : userId));
  }

  protected async submitCreateUser(): Promise<void> {
    this.pageError.set(null);
    this.pageSuccess.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.pageError.set('Completa usuario, nombre visible, rol y una contraseña inicial de 12+ caracteres con mayúscula, minúscula y número.');
      return;
    }

    this.isSubmitting.set(true);

    try {
      const createdUser = await firstValueFrom(this.userManagementService.createUser(this.form.getRawValue()));
      this.resetCreateForm(false);
      this.isCreateDialogOpen.set(false);
      this.pageSuccess.set(`Usuario ${createdUser.userName} creado con rol ${this.getRoleLabel(createdUser.roleCode)}.`);
      await this.reload();
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No se pudo crear el usuario.'));
    } finally {
      this.isSubmitting.set(false);
    }
  }

  protected getRoleLabel(roleCode: ApplicationRoleCode): string {
    switch (roleCode) {
      case 'ADMIN':
        return 'Administrador';
      case 'OPERATOR':
        return 'Operador';
      case 'READONLY':
        return 'Consulta';
    }
  }

  protected getRoleDraft(user: ApplicationUserAdmin): ApplicationRoleCode {
    return this.roleDrafts()[user.id] ?? user.roleCode;
  }

  protected setRoleDraft(userId: string, roleCode: string): void {
    if (!this.isRoleCode(roleCode)) {
      return;
    }

    this.roleDrafts.update((current) => ({
      ...current,
      [userId]: roleCode
    }));
  }

  protected getPasswordDraft(userId: string): string {
    return this.passwordDrafts()[userId] ?? '';
  }

  protected setPasswordDraft(userId: string, value: string): void {
    this.passwordDrafts.update((current) => ({
      ...current,
      [userId]: value
    }));
  }

  protected isRowBusy(userId: string): boolean {
    return this.rowBusyState()[userId] === true;
  }

  protected async applyRoleChange(user: ApplicationUserAdmin): Promise<void> {
    const nextRoleCode = this.getRoleDraft(user);
    if (nextRoleCode === user.roleCode) {
      return;
    }

    await this.runRowAction(
      user.id,
      async () => {
        const updatedUser = await firstValueFrom(
          this.userManagementService.changeUserRole(user.id, { roleCode: nextRoleCode }));
        this.pageSuccess.set(`Rol actualizado para ${updatedUser.userName}: ${this.getRoleLabel(updatedUser.roleCode)}.`);
        await this.reload();
      },
      'No se pudo actualizar el rol del usuario.');
  }

  protected async toggleActivation(user: ApplicationUserAdmin): Promise<void> {
    await this.runRowAction(
      user.id,
      async () => {
        const updatedUser = await firstValueFrom(
          this.userManagementService.setUserActivation(user.id, { isActive: !user.isActive }));
        this.pageSuccess.set(`Usuario ${updatedUser.userName} ${updatedUser.isActive ? 'reactivado' : 'desactivado'}.`);
        await this.reload();
      },
      'No se pudo actualizar el estado del usuario.');
  }

  protected async resetPassword(user: ApplicationUserAdmin): Promise<void> {
    const newPassword = this.getPasswordDraft(user.id).trim();
    if (!this.passwordMeetsPolicy(newPassword)) {
      this.pageError.set('La contraseña temporal debe tener 12+ caracteres e incluir mayúscula, minúscula y número.');
      return;
    }

    await this.runRowAction(
      user.id,
      async () => {
        await firstValueFrom(this.userManagementService.resetUserPassword(user.id, { newPassword }));
        this.passwordDrafts.update((current) => ({
          ...current,
          [user.id]: ''
        }));
        this.pageSuccess.set(`Contraseña restablecida para ${user.userName}.`);
        await this.reload();
      },
      'No se pudo restablecer la contraseña del usuario.');
  }

  protected async unlockUser(user: ApplicationUserAdmin): Promise<void> {
    await this.runRowAction(
      user.id,
      async () => {
        const updatedUser = await firstValueFrom(this.userManagementService.unlockUser(user.id));
        this.pageSuccess.set(`Bloqueo limpiado para ${updatedUser.userName}.`);
        await this.reload();
      },
      'No se pudo limpiar el bloqueo del usuario.');
  }

  protected passwordMeetsPolicy(value: string): boolean {
    return this.passwordPolicyPattern.test(value.trim());
  }

  private async runRowAction(userId: string, action: () => Promise<void>, fallbackMessage: string): Promise<void> {
    this.pageError.set(null);
    this.pageSuccess.set(null);
    this.patchRowBusyState(userId, true);

    try {
      await action();
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, fallbackMessage));
    } finally {
      this.patchRowBusyState(userId, false);
    }
  }

  private patchRowBusyState(userId: string, isBusy: boolean): void {
    this.rowBusyState.update((current) => ({
      ...current,
      [userId]: isBusy
    }));
  }

  private isRoleCode(value: string): value is ApplicationRoleCode {
    return value === 'ADMIN' || value === 'OPERATOR' || value === 'READONLY';
  }
}
