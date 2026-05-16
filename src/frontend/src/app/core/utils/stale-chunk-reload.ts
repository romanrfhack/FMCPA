import { NavigationError } from '@angular/router';

const staleChunkReloadStorageKey = 'fmcpa.stale-chunk-reload-at';
const staleChunkReloadWindowMs = 10_000;
const cacheBusterParamName = 'fmcpa_reload';

const dynamicImportFailureMessages = [
  'Failed to fetch dynamically imported module',
  'Importing a module script failed',
  'ChunkLoadError',
  'Loading chunk'
];

export function handleStaleChunkNavigationError(navigationError: NavigationError): void {
  if (!isDynamicImportFailure(navigationError.error)) {
    return;
  }

  const now = Date.now();
  const lastReloadAt = readLastStaleChunkReloadAt();
  if (lastReloadAt !== null && now - lastReloadAt < staleChunkReloadWindowMs) {
    return;
  }

  writeLastStaleChunkReloadAt(now);
  globalThis.location.replace(buildCacheBustedUrl(globalThis.location.href, now));
}

export function isDynamicImportFailure(error: unknown): boolean {
  const message = getErrorMessage(error);

  return dynamicImportFailureMessages.some((knownMessage) => message.includes(knownMessage));
}

export function buildCacheBustedUrl(href: string, stamp: number): string {
  const url = new URL(href);
  url.searchParams.set(cacheBusterParamName, String(stamp));

  return url.toString();
}

function getErrorMessage(error: unknown): string {
  if (error instanceof Error) {
    return error.message;
  }

  if (typeof error === 'string') {
    return error;
  }

  if (typeof error === 'object' && error !== null && 'message' in error) {
    const message = (error as { message: unknown }).message;

    return typeof message === 'string' ? message : '';
  }

  return '';
}

function readLastStaleChunkReloadAt(): number | null {
  try {
    const rawValue = globalThis.sessionStorage.getItem(staleChunkReloadStorageKey);
    if (!rawValue) {
      return null;
    }

    const parsedValue = Number(rawValue);

    return Number.isFinite(parsedValue) ? parsedValue : null;
  } catch {
    return null;
  }
}

function writeLastStaleChunkReloadAt(value: number): void {
  try {
    globalThis.sessionStorage.setItem(staleChunkReloadStorageKey, String(value));
  } catch {
    // If sessionStorage is unavailable, still let the cache-busting reload happen.
  }
}
