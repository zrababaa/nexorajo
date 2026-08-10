import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { Schemas } from '../../core/api/api.types';

export type DashboardData = Schemas['DashboardApiResponse'];

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  getDashboard(): Promise<DashboardData> {
    return firstValueFrom(this.http.get<DashboardData>('/api/v1/dashboard'));
  }
}
