import '@angular/compiler';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../core/services/auth.service';
import { LoginPageComponent } from './login-page.component';

describe('LoginPageComponent', () => {
  it('renders the institutional login without local security implementation details', async () => {
    await TestBed.configureTestingModule({
      imports: [LoginPageComponent],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            login: vi.fn(() => of({}))
          }
        }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(LoginPageComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const visibleText = compiled.textContent ?? '';
    const logo = compiled.querySelector<HTMLImageElement>('img.brand-logo');
    const userInput = compiled.querySelector<HTMLInputElement>('#userName');
    const passwordInput = compiled.querySelector<HTMLInputElement>('#password');

    expect(logo?.getAttribute('src')).toBe('/assets/brand/logo-fmcpa.webp');
    expect(logo?.getAttribute('alt')).toBe('FMCPA');
    expect(visibleText).toContain('FMCPA Platform');
    expect(visibleText).toContain('Acceso al sistema');
    expect(visibleText).toContain('Usuario');
    expect(visibleText).toContain('Contraseña');
    expect(visibleText).toContain('Iniciar sesión');
    expect(visibleText).not.toContain('Track 2 Seguridad');
    expect(visibleText).not.toContain('Acceso local mínimo');
    expect(visibleText).not.toContain('JWT');
    expect(visibleText).not.toContain('Convención local');
    expect(userInput?.value).toBe('');
    expect(passwordInput?.value).toBe('');
  });

  it('keeps the current login error handling visible to the user', async () => {
    await TestBed.configureTestingModule({
      imports: [LoginPageComponent],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            login: vi.fn(() => throwError(() => new Error('Credenciales invalidas')))
          }
        }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(LoginPageComponent);
    const component = fixture.componentInstance as unknown as {
      loginForm: {
        setValue(value: { userName: string; password: string }): void;
      };
      submit(): Promise<void>;
    };

    component.loginForm.setValue({
      userName: 'usuario',
      password: 'password'
    });

    await component.submit();
    fixture.detectChanges();

    const alert = fixture.nativeElement.querySelector('[role="alert"]') as HTMLElement | null;
    expect(alert?.textContent).toContain('No fue posible iniciar sesión.');
  });
});
