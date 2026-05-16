import { describe, expect, it } from 'vitest';

import { buildCacheBustedUrl, isDynamicImportFailure } from './stale-chunk-reload';

describe('stale chunk reload support', () => {
  it('recognizes stale dynamic import failures', () => {
    expect(isDynamicImportFailure(
      new TypeError(
        'Failed to fetch dynamically imported module: https://fmcpa.com.mx/chunk-IISUAAIB.js'
      )
    )).toBe(true);

    expect(isDynamicImportFailure('ChunkLoadError: Loading chunk 123 failed.')).toBe(true);
    expect(isDynamicImportFailure(new Error('Regular navigation failure'))).toBe(false);
  });

  it('adds a cache buster without changing the route', () => {
    expect(buildCacheBustedUrl('https://fmcpa.com.mx/contacts?tab=admin#list', 12345)).toBe(
      'https://fmcpa.com.mx/contacts?tab=admin&fmcpa_reload=12345#list'
    );
  });
});
