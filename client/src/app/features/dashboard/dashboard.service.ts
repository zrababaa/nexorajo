import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { Schemas } from '../../core/api/api.types';
import { SessionQueryCache } from '../../core/http/session-query-cache.service';

export type DashboardData = Schemas['DashboardApiResponse'];

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(SessionQueryCache);

  getDashboard(onRefresh?: (data: DashboardData) => void): Promise<DashboardData> {
    return this.cache.get(
      'dashboard',
      () => firstValueFrom(this.http.get<DashboardData>('/api/v1/dashboard')),
      onRefresh,
    );
  }
}
