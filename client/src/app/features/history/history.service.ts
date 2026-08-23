import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { PagedResult, Schemas } from '../../core/api/api.types';
import { cleanParams } from '../../core/http/params';
import { SessionQueryCache } from '../../core/http/session-query-cache.service';

export type HistoryListItem = Schemas['HistoryListItemDto'];
export type HistorySummary = Schemas['HistorySummaryDto'];
export type MessageSource = Schemas['MessageSource'];
export type MessageStatus = Schemas['MessageStatus'];

export interface HistoryFilter {
  source?: MessageSource;
  status?: MessageStatus;
  campaignBatchId?: string;
  receiver?: string;
  dateFrom?: string;
  dateTo?: string;
}

@Injectable({ providedIn: 'root' })
export class HistoryService {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(SessionQueryCache);

  list(
    filter: HistoryFilter,
    page: number,
    pageSize: number,
    onRefresh?: (result: PagedResult<HistoryListItem>) => void,
  ): Promise<PagedResult<HistoryListItem>> {
    const params = new HttpParams({ fromObject: cleanParams({ ...filter, page, pageSize }) });
    return this.cache.get(
      `history:list?${params.toString()}`,
      () => firstValueFrom(this.http.get<PagedResult<HistoryListItem>>('/api/v1/history', { params })),
      onRefresh,
    );
  }

  summary(filter: HistoryFilter, onRefresh?: (result: HistorySummary) => void): Promise<HistorySummary> {
    const params = new HttpParams({ fromObject: cleanParams({ ...filter }) });
    return this.cache.get(
      `history:summary?${params.toString()}`,
      () => firstValueFrom(this.http.get<HistorySummary>('/api/v1/history/summary', { params })),
      onRefresh,
    );
  }

  async resend(id: number, source: MessageSource): Promise<unknown> {
    const params = new HttpParams({ fromObject: { source } });
    const result = await firstValueFrom(this.http.post('/api/v1/history/' + id + '/resend', null, { params }));
    this.cache.invalidate('history:');
    this.cache.invalidate('dashboard');
    this.cache.invalidate('reports:');
    return result;
  }
}
