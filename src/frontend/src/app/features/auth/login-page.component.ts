import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { LoginRequest } from '../../core/models/auth.models';
import { AuthService } from '../../core/services/auth.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  selector: 'app-login-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  template: `
    <section class="login-shell">
      <article class="login-card">
        <div class="login-copy">
          <p class="page-kicker">Track 2 Seguridad</p>
          <h1>Acceso local mínimo</h1>
          <p>
            Base inicial de autenticación para el MVP: login con token JWT, protección de rutas
            principales y sesión local acotada al navegador actual.
          </p>
        </div>

        @if (errorMessage()) {
          <p class="alert error">{{ errorMessage() }}</p>
        }

        <form class="login-form" [formGroup]="loginForm" (ngSubmit)="submit()">
          <label>
            <span>Usuario</span>
            <input type="text" formControlName="userName" autocomplete="username" />
          </label>

          <label>
            <span>Contraseña</span>
            <input type="password" formControlName="password" autocomplete="current-password" />
          </label>

          <button type="submit" [disabled]="isSubmitting()">
            {{ isSubmitting() ? 'Ingresando...' : 'Iniciar sesión' }}
          </button>
        </form>

        <div class="login-note">
          <strong>Convención local</strong>
          <p>
            Usuarios locales sugeridos: <code>admin</code>, <code>operator</code> y
            <code>readonly</code>. Las contraseñas se definen fuera del repo vía
            <code>FMCPA_AUTH_BOOTSTRAP_PASSWORD</code>,
            <code>FMCPA_AUTH_OPERATOR_PASSWORD</code> y
            <code>FMCPA_AUTH_READONLY_PASSWORD</code>.
          </p>
        </div>
      </article>
    </section>
  `,
  styles: [
    `
      :host {
        display: block;
        min-height: 100vh;
      }

      .login-shell {
        min-height: 100vh;
        display: grid;
        place-items: center;
        padding: 1.5rem;
        background:
          radial-gradient(circle at top left, rgba(7, 114, 103, 0.16), transparent 26rem),
          radial-gradient(circle at bottom right, rgba(184, 115, 51, 0.14), transparent 22rem),
          linear-gradient(180deg, #f4f2eb 0%, #f7f6f2 52%, #ede8db 100%);
      }

      .login-card {
        width: min(100%, 28rem);
        padding: 1.5rem;
        border-radius: 1.4rem;
        background: rgba(255, 255, 255, 0.88);
        border: 1px solid rgba(29, 45, 42, 0.08);
        box-shadow: 0 24px 48px rgba(32, 44, 41, 0.12);
      }

      .page-kicker {
        margin: 0 0 0.6rem;
        letter-spacing: 0.12em;
        text-transform: uppercase;
        font-size: 0.78rem;
        font-weight: 700;
        color: #0f766e;
      }

      h1 {
        margin: 0;
        font-size: clamp(2rem, 6vw, 2.8rem);
        line-height: 0.96;
        color: #1d2d2a;
      }

      .login-copy p:not(.page-kicker) {
        margin: 0.9rem 0 0;
        line-height: 1.65;
        color: #445854;
      }

      .login-form {
        display: grid;
        gap: 1rem;
        margin-top: 1.5rem;
      }

      label {
        display: grid;
        gap: 0.45rem;
      }

      label span {
        font-size: 0.9rem;
        font-weight: 700;
        color: #29403b;
      }

      input {
        border: 1px solid rgba(29, 45, 42, 0.14);
        border-radius: 0.9rem;
        padding: 0.85rem 0.95rem;
        font: inherit;
        color: #1d2d2a;
        background: rgba(255, 255, 255, 0.95);
      }

      input:focus {
        outline: 2px solid rgba(15, 118, 110, 0.22);
        border-color: #0f766e;
      }

      button {
        border: none;
        border-radius: 999px;
        padding: 0.9rem 1.15rem;
        font: inherit;
        font-weight: 700;
        color: #f6f6f2;
        background: #123f3b;
        cursor: pointer;
      }

      button:disabled {
        opacity: 0.65;
        cursor: wait;
      }

      .login-note {
        margin-top: 1.25rem;
        padding: 0.95rem 1rem;
        border-radius: 1rem;
        background: rgba(15, 118, 110, 0.08);
        color: #29403b;
      }

      .login-note p {
        margin: 0.45rem 0 0;
        line-height: 1.55;
      }

      code {
        font-size: 0.88rem;
      }

      .alert.error {
        margin-top: 1rem;
        padding: 0.85rem 0.95rem;
        border-radius: 0.95rem;
        background: rgba(178, 34, 34, 0.08);
        color: #8d1f1f;
      }
    `
  ]
})
export class LoginPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly isSubmitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly loginForm = this.formBuilder.nonNullable.group({
    userName: ['admin', Validators.required],
    password: ['', Validators.required]
  });

  protected async submit(): Promise<void> {
    this.errorMessage.set(null);

    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      this.errorMessage.set('Captura usuario y contraseña.');
      return;
    }

    this.isSubmitting.set(true);

    try {
      const request = this.loginForm.getRawValue() as LoginRequest;
      await firstValueFrom(this.authService.login(request));

      const returnUrl = this.activatedRoute.snapshot.queryParamMap.get('returnUrl');
      await this.router.navigateByUrl(returnUrl?.startsWith('/') ? returnUrl : '/dashboard');
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error, 'No fue posible iniciar sesión.'));
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
