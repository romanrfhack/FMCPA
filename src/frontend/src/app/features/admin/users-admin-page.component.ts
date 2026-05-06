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
        <p class="page-kicker">TRACK 2 SEGURIDAD</p>
        <h2>Administración mínima de usuarios internos</h2>
        <p>
          Alta controlada, rol base, activación lógica, reset administrativo de password y lockout
          temporal sobre la capa de autenticación/autorización ya existente.
        </p>
      </article>

      @if (pageError()) {
        <p class="alert error">{{ pageError() }}</p>
      }

      @if (pageSuccess()) {
        <p class="alert success">{{ pageSuccess() }}</p>
      }

      <div class="page-grid">
        <article class="form-card">
          <div class="card-header">
            <div>
              <h3>Alta mínima</h3>
              <p>El nuevo usuario se crea activo y con password inicial controlado por ADMIN.</p>
            </div>
            @if (isSubmitting()) {
              <span class="badge neutral">Guardando...</span>
            }
          </div>

          <form class="form-grid" [formGroup]="form" (ngSubmit)="submitCreateUser()">
            <label>
              <span>UserName</span>
              <input type="text" formControlName="userName" placeholder="operador-interno" />
            </label>

            <label>
              <span>Nombre visible</span>
              <input type="text" formControlName="displayName" placeholder="Operador interno" />
            </label>

            <label>
              <span>Rol base</span>
              <select formControlName="roleCode">
                @for (roleCode of roleOptions; track roleCode) {
                  <option [value]="roleCode">{{ getRoleLabel(roleCode) }}</option>
                }
              </select>
            </label>

            <label>
              <span>Password inicial</span>
              <input type="password" formControlName="password" placeholder="12+ con mayúscula, minúscula y número" />
            </label>

            <div class="form-actions">
              <button type="submit" [disabled]="isSubmitting()">Crear usuario</button>
              <button type="button" class="ghost" (click)="resetCreateForm()">Limpiar</button>
            </div>
          </form>

          <p class="card-note">
            Los cambios de rol, activación o password invalidan tokens previos del usuario afectado.
            Los passwords temporales deben cumplir la política mínima documentada.
          </p>
        </article>

        <article class="list-card">
          <div class="card-header">
            <div>
              <h3>Usuarios registrados</h3>
              <p>Vista administrativa mínima sin self-service, invitaciones ni borrado físico.</p>
            </div>
            <button type="button" class="ghost" (click)="reload()">Actualizar</button>
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
                    <div>
                      <h4>{{ user.displayName }}</h4>
                      <p class="meta">{{ user.userName }}</p>
                    </div>

                    <div class="row-badges">
                      @if (currentUserId() === user.id) {
                        <span class="badge neutral">Tu sesión</span>
                      }
                      <span class="badge">{{ getRoleLabel(user.roleCode) }}</span>
                      <span class="badge" [class.inactive]="!user.isActive">
                        {{ user.isActive ? 'Activo' : 'Inactivo' }}
                      </span>
                      @if (user.isLockedOut) {
                        <span class="badge inactive">Lockout</span>
                      }
                    </div>
                  </div>

                  <dl class="detail-grid">
                    <div>
                      <dt>Creado UTC</dt>
                      <dd>{{ user.createdUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</dd>
                    </div>
                    <div>
                      <dt>Actualizado UTC</dt>
                      <dd>{{ user.updatedUtc ? (user.updatedUtc | date: 'yyyy-MM-dd HH:mm':'UTC') : 'Sin cambios administrativos' }}</dd>
                    </div>
                    <div>
                      <dt>Último login UTC</dt>
                      <dd>{{ user.lastLoginUtc ? (user.lastLoginUtc | date: 'yyyy-MM-dd HH:mm':'UTC') : 'Sin login registrado' }}</dd>
                    </div>
                    <div>
                      <dt>Intentos fallidos</dt>
                      <dd>{{ user.accessFailedCount }}</dd>
                    </div>
                    <div>
                      <dt>Lockout hasta UTC</dt>
                      <dd>{{ user.lockoutEndUtc ? (user.lockoutEndUtc | date: 'yyyy-MM-dd HH:mm':'UTC') : 'Sin bloqueo' }}</dd>
                    </div>
                  </dl>

                  <div class="actions-grid">
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

                    <button
                      type="button"
                      class="ghost"
                      [disabled]="isRowBusy(user.id)"
                      (click)="toggleActivation(user)">
                      {{ user.isActive ? 'Desactivar' : 'Reactivar' }}
                    </button>

                    <label class="password-field">
                      <span>Reset password</span>
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
                      Resetear password
                    </button>

                    <button
                      type="button"
                      class="ghost"
                      [disabled]="isRowBusy(user.id) || (user.accessFailedCount === 0 && !user.lockoutEndUtc)"
                      (click)="unlockUser(user)">
                      Limpiar lockout
                    </button>
                  </div>
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
      .page-grid,
      .form-grid,
      .user-list {
        display: grid;
        gap: 1.25rem;
      }

      .page-grid {
        grid-template-columns: minmax(21rem, 24rem) minmax(0, 1fr);
        align-items: start;
      }

      .hero-card,
      .form-card,
      .list-card {
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
      .card-header p,
      .meta,
      .empty-state,
      .card-note {
        margin-top: 0.75rem;
        line-height: 1.6;
        color: #4d615c;
      }

      .card-header,
      .row-top {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        align-items: flex-start;
      }

      .card-header {
        margin-bottom: 1rem;
      }

      .alert,
      .badge {
        padding: 0.75rem 0.9rem;
        border-radius: 0.9rem;
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
      }

      input,
      select {
        width: 100%;
        padding: 0.8rem 0.9rem;
        border-radius: 0.9rem;
        border: 1px solid rgba(29, 45, 42, 0.14);
        background: #fbfbf8;
        color: #1d2d2a;
        font: inherit;
      }

      button {
        border: none;
        border-radius: 0.9rem;
        padding: 0.8rem 1rem;
        font: inherit;
        font-weight: 700;
        cursor: pointer;
        background: #123f3b;
        color: #f6f6f2;
      }

      button.ghost {
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
      }

      button:disabled {
        opacity: 0.7;
        cursor: wait;
      }

      .form-actions {
        display: flex;
        gap: 0.75rem;
      }

      .user-row {
        display: grid;
        gap: 1rem;
        padding: 1rem;
        border-radius: 1rem;
        background: #f6f5ef;
      }

      .detail-grid {
        display: grid;
        grid-template-columns: repeat(5, minmax(0, 1fr));
        gap: 0.9rem;
        margin: 0;
      }

      dt {
        font-size: 0.75rem;
        font-weight: 700;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        color: #5d736f;
      }

      dd {
        margin-top: 0.35rem;
        color: #23312f;
        line-height: 1.5;
      }

      .actions-grid {
        display: grid;
        grid-template-columns: minmax(10rem, 12rem) auto auto minmax(12rem, 1fr) auto auto;
        gap: 0.75rem;
        align-items: end;
      }

      .password-field {
        min-width: 0;
      }

      @media (max-width: 1180px) {
        .page-grid {
          grid-template-columns: 1fr;
        }

        .actions-grid {
          grid-template-columns: 1fr 1fr;
        }

        .detail-grid {
          grid-template-columns: 1fr;
        }
      }

      @media (max-width: 720px) {
        .form-actions,
        .actions-grid {
          grid-template-columns: 1fr;
          display: grid;
        }

        .row-top,
        .card-header {
          flex-direction: column;
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

  protected async submitCreateUser(): Promise<void> {
    this.pageError.set(null);
    this.pageSuccess.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.pageError.set('Completa userName, nombre visible, rol y un password inicial de 12+ caracteres con mayúscula, minúscula y número.');
      return;
    }

    this.isSubmitting.set(true);

    try {
      const createdUser = await firstValueFrom(this.userManagementService.createUser(this.form.getRawValue()));
      this.resetCreateForm(false);
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
        return 'ADMIN';
      case 'OPERATOR':
        return 'OPERATOR';
      case 'READONLY':
        return 'READONLY';
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
      this.pageError.set('El password temporal debe tener 12+ caracteres e incluir mayúscula, minúscula y número.');
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
        this.pageSuccess.set(`Password restablecido para ${user.userName}.`);
        await this.reload();
      },
      'No se pudo restablecer el password del usuario.');
  }

  protected async unlockUser(user: ApplicationUserAdmin): Promise<void> {
    await this.runRowAction(
      user.id,
      async () => {
        const updatedUser = await firstValueFrom(this.userManagementService.unlockUser(user.id));
        this.pageSuccess.set(`Lockout limpiado para ${updatedUser.userName}.`);
        await this.reload();
      },
      'No se pudo limpiar el lockout del usuario.');
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
