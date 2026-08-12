import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { PagedResult, Schemas } from '../../core/api/api.types';

export type BatchLinkStats = Schemas['BatchLinkStatsDto'];
export type LinkClickRow = Schemas['LinkClickRowDto'];

@Injectable({ providedIn: 'root' })
export class LinkClicksService {
  private readonly http = inject(HttpClient);

  stats(batchId: string): Promise<BatchLinkStats> {
    return firstValueFrom(this.http.get<BatchLinkStats>(`/api/v1/link-tracking/batches/${batchId}`));
  }

  clicks(batchId: string, page: number, pageSize: number): Promise<PagedResult<LinkClickRow>> {
    const params = new HttpParams({ fromObject: { page, pageSize } });
    return firstValueFrom(this.http.get<PagedResult<LinkClickRow>>(`/api/v1/link-tracking/batches/${batchId}/clicks`, { params }));
  }
}
