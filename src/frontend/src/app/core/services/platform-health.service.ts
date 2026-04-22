import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { PlatformHealthResponse } from '../models/platform-health.model';

@Injectable({ providedIn: 'root' })
export class PlatformHealthService {
  private readonly http = inject(HttpClient);

  getHealth(): Observable<PlatformHealthResponse> {
    return this.http.get<PlatformHealthResponse>(`${environment.apiBaseUrl}/health`);
  }
}
