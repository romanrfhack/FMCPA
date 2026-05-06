import '@angular/compiler';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { afterEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../core/services/auth.service';
import { ChangePasswordPageComponent } from './change-password-page.component';

describe('ChangePasswordPageComponent', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('logs out after a successful password change', async () => {
    vi.useFakeTimers();

    const authService = {
      changePassword: vi.fn(() => of({ message: 'Password actualizado. Inicia sesion nuevamente.' })),
      logout: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [ChangePasswordPageComponent],
      providers: [
        {
          provide: AuthService,
          useValue: authService
        }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(ChangePasswordPageComponent);
    const component = fixture.componentInstance as unknown as {
      form: {
        setValue(value: {
          currentPassword: string;
          newPassword: string;
          confirmNewPassword: string;
        }): void;
      };
      submit(): Promise<void>;
    };

    component.form.setValue({
      currentPassword: 'SelfProbe123',
      newPassword: 'SelfProbe456',
      confirmNewPassword: 'SelfProbe456'
    });

    await component.submit();

    expect(authService.changePassword).toHaveBeenCalledWith({
      currentPassword: 'SelfProbe123',
      newPassword: 'SelfProbe456',
      confirmNewPassword: 'SelfProbe456'
    });
    expect(authService.logout).not.toHaveBeenCalled();

    vi.advanceTimersByTime(1200);

    expect(authService.logout).toHaveBeenCalledTimes(1);
  });
});
