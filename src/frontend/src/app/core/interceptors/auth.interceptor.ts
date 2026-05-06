import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { AuthService } from '../services/auth.service';

const webClientHeaderName = 'X-FMCPA-Client';
const webClientHeaderValue = 'FMCPA-Web';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const isLoginRequest = request.url.endsWith('/api/auth/login');
  const isUnsafeApiRequest = isUnsafeApiMethod(request.method) && isApiRequest(request.url);
  const accessToken = isLoginRequest ? null : authService.getAccessToken();
  const headers: Record<string, string> = {};

  if (accessToken) {
    headers['Authorization'] = `Bearer ${accessToken}`;
  }

  if (isUnsafeApiRequest) {
    headers[webClientHeaderName] = webClientHeaderValue;
  }

  const authenticatedRequest = Object.keys(headers).length > 0
    ? request.clone({ setHeaders: headers })
    : request;

  return next(authenticatedRequest).pipe(
    catchError((error) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !isLoginRequest) {
        authService.handleUnauthorized();
      }

      return throwError(() => error);
    })
  );
};

function isUnsafeApiMethod(method: string): boolean {
  return !['GET', 'HEAD', 'OPTIONS'].includes(method.toUpperCase());
}

function isApiRequest(url: string): boolean {
  return url.startsWith('/api') || url.includes('/api/');
}
