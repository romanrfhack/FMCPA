import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { ChangePasswordRequest } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  selector: 'app-change-password-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  template: `
    <section class="page-shell">
      <article class="form-card">
        <div class="card-header">
          <div>
            <p class="page-kicker">TRACK 2 SEGURIDAD</p>
            <h2>Cambiar contraseña</h2>
            <p>
              Actualiza tu contraseña con validación de la contraseña actual y cierre de sesión al terminar.
            </p>
          </div>
        </div>

        @if (errorMessage()) {
          <p class="alert error">{{ errorMessage() }}</p>
        }

        @if (successMessage()) {
          <p class="alert success">{{ successMessage() }}</p>
        }

        <form class="form-grid" [formGroup]="form" (ngSubmit)="submit()">
          <label>
            <span>Contraseña actual</span>
            <input type="password" formControlName="currentPassword" autocomplete="current-password" />
          </label>

          <label>
            <span>Nueva contraseña</span>
            <input
              type="password"
              formControlName="newPassword"
              autocomplete="new-password"
              placeholder="12+ con mayúscula, minúscula y número" />
          </label>

          <label>
            <span>Confirmar nueva contraseña</span>
            <input type="password" formControlName="confirmNewPassword" autocomplete="new-password" />
          </label>

          <div class="form-actions">
            <button type="submit" [disabled]="isSubmitting() || successMessage() !== null">
              {{ isSubmitting() ? 'Actualizando...' : 'Cambiar contraseña' }}
            </button>
          </div>
        </form>

        <p class="card-note">
          Después del cambio se invalida el token actual y deberás iniciar sesión nuevamente.
        </p>
      </article>
    </section>
  `,
  styles: [
    `
      .page-shell,
      .form-grid {
        display: grid;
        gap: 1.25rem;
      }

      .form-card {
        width: min(100%, 34rem);
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
      p {
        margin: 0;
      }

      .card-header p:last-child,
      .card-note {
        margin-top: 0.75rem;
        line-height: 1.6;
        color: #4d615c;
      }

      label {
        display: grid;
        gap: 0.4rem;
        font-size: 0.92rem;
        font-weight: 600;
        color: #29403b;
      }

      input {
        width: 100%;
        box-sizing: border-box;
        padding: 0.8rem 0.9rem;
        border-radius: 0.9rem;
        border: 1px solid rgba(29, 45, 42, 0.14);
        background: #fbfbf8;
        color: #1d2d2a;
        font: inherit;
      }

      input:focus {
        outline: 2px solid rgba(15, 118, 110, 0.22);
        border-color: #0f766e;
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

      button:disabled {
        opacity: 0.7;
        cursor: wait;
      }

      .alert {
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
    `
  ]
})
export class ChangePasswordPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly passwordPolicyPattern = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{12,}$/;

  protected readonly isSubmitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(12), Validators.pattern(this.passwordPolicyPattern)]],
    confirmNewPassword: ['', Validators.required]
  });

  protected async submit(): Promise<void> {
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const request = this.form.getRawValue() as ChangePasswordRequest;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage.set('Completa los campos y usa una contraseña de 12+ caracteres con mayúscula, minúscula y número.');
      return;
    }

    if (request.newPassword !== request.confirmNewPassword) {
      this.errorMessage.set('La confirmación debe coincidir con la nueva contraseña.');
      return;
    }

    if (request.currentPassword === request.newPassword) {
      this.errorMessage.set('La nueva contraseña debe ser diferente a la actual.');
      return;
    }

    this.isSubmitting.set(true);

    try {
      const response = await firstValueFrom(this.authService.changePassword(request));
      this.form.disable();
      this.successMessage.set(response.message);
      globalThis.setTimeout(() => this.authService.logout(), 1200);
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error, 'No se pudo cambiar la contraseña.'));
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
