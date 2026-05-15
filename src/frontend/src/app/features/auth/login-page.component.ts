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
        <header class="login-header">
          <img src="/assets/brand/logo-fmcpa.webp" alt="FMCPA" class="brand-logo" />
          <div>
            <h1>FMCPA Platform</h1>
            <p class="login-subtitle">Acceso al sistema</p>
            <p class="login-help">Ingresa tus credenciales para continuar</p>
          </div>
        </header>

        @if (errorMessage()) {
          <p class="alert error" role="alert">{{ errorMessage() }}</p>
        }

        <form class="login-form" [formGroup]="loginForm" (ngSubmit)="submit()">
          <div class="field">
            <label for="userName">Usuario</label>
            <input id="userName" type="text" formControlName="userName" autocomplete="username" />
          </div>

          <div class="field">
            <label for="password">Contraseña</label>
            <input
              id="password"
              type="password"
              formControlName="password"
              autocomplete="current-password" />
          </div>

          <button type="submit" [disabled]="isSubmitting()">
            {{ isSubmitting() ? 'Ingresando...' : 'Iniciar sesión' }}
          </button>
        </form>
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
        background: linear-gradient(180deg, #f7f4ea 0%, #f1ecdd 100%);
      }

      .login-card {
        width: min(100%, 27.5rem);
        padding: 1.6rem;
        border-radius: 8px;
        background: #ffffff;
        border: 1px solid rgba(29, 45, 42, 0.1);
        border-top: 4px solid #a8302d;
        box-shadow: 0 18px 40px rgba(32, 44, 41, 0.12);
      }

      .login-header {
        display: grid;
        justify-items: center;
        gap: 1rem;
        text-align: center;
      }

      .brand-logo {
        display: block;
        width: 6.2rem;
        max-width: 34vw;
        height: auto;
        object-fit: contain;
      }

      h1 {
        margin: 0;
        font-size: 2rem;
        line-height: 1.08;
        color: #1d2d2a;
      }

      .login-subtitle {
        margin: 0.45rem 0 0;
        font-size: 1.05rem;
        font-weight: 700;
        color: #0f5f58;
      }

      .login-help {
        margin: 0.35rem 0 0;
        line-height: 1.45;
        color: #445854;
      }

      .login-form {
        display: grid;
        gap: 1rem;
        margin-top: 1.5rem;
      }

      .field {
        display: grid;
        gap: 0.45rem;
      }

      label {
        font-size: 0.9rem;
        font-weight: 700;
        color: #29403b;
      }

      input {
        border: 1px solid rgba(29, 45, 42, 0.14);
        border-radius: 8px;
        min-height: 2.9rem;
        padding: 0.82rem 0.95rem;
        font: inherit;
        color: #1d2d2a;
        background: #fbfaf6;
      }

      input:focus {
        outline: 3px solid rgba(15, 118, 110, 0.22);
        outline-offset: 1px;
        border-color: #0f766e;
      }

      button {
        border: none;
        border-radius: 8px;
        min-height: 2.9rem;
        padding: 0.9rem 1.15rem;
        font: inherit;
        font-weight: 700;
        color: #f6f6f2;
        background: #123f3b;
        cursor: pointer;
      }

      button:hover:not(:disabled) {
        background: #0f332f;
      }

      button:focus-visible {
        outline: 3px solid rgba(168, 48, 45, 0.28);
        outline-offset: 2px;
      }

      button:disabled {
        opacity: 0.65;
        cursor: wait;
      }

      .alert.error {
        margin-top: 1rem;
        padding: 0.85rem 0.95rem;
        border-radius: 8px;
        border: 1px solid rgba(168, 48, 45, 0.2);
        background: #fff4f2;
        color: #8d1f1f;
      }

      @media (max-width: 30rem) {
        .login-shell {
          align-items: start;
          padding: 1rem;
        }

        .login-card {
          margin-top: 1rem;
          padding: 1.2rem;
        }

        .brand-logo {
          width: 5.2rem;
          max-width: 38vw;
        }

        h1 {
          font-size: 1.75rem;
        }
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
    userName: ['', Validators.required],
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
