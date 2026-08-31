import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { PagedResult, Schemas } from '../../core/api/api.types';

export type TrackedLinkRow = Schemas['TrackedLinkRowDto'];

@Injectable({ providedIn: 'root' })
export class TrackingLinksService {
  private readonly http = inject(HttpClient);

  list(page: number, pageSize: number): Promise<PagedResult<TrackedLinkRow>> {
    const params = new HttpParams({ fromObject: { page, pageSize } });
    return firstValueFrom(this.http.get<PagedResult<TrackedLinkRow>>('/api/v1/link-tracking/links', { params }));
  }
}
