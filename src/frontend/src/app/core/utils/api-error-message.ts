import { HttpErrorResponse } from '@angular/common/http';

export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  const validationErrors = error.error?.errors as Record<string, string[]> | undefined;
  if (validationErrors) {
    const firstMessage = Object.values(validationErrors).flat()[0];
    if (firstMessage) {
      return firstMessage;
    }
  }

  if (typeof error.error?.title === 'string' && error.error.title.trim().length > 0) {
    return error.error.title;
  }

  if (typeof error.error?.message === 'string' && error.error.message.trim().length > 0) {
    return error.error.message;
  }

  if (error.message.trim().length > 0) {
    return error.message;
  }

  return fallback;
}
