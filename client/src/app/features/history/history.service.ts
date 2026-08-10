import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { PagedResult, Schemas } from '../../core/api/api.types';
import { cleanParams } from '../../core/http/params';

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

  list(filter: HistoryFilter, page: number, pageSize: number): Promise<PagedResult<HistoryListItem>> {
    const params = new HttpParams({ fromObject: cleanParams({ ...filter, page, pageSize }) });
    return firstValueFrom(this.http.get<PagedResult<HistoryListItem>>('/api/v1/history', { params }));
  }

  summary(filter: HistoryFilter): Promise<HistorySummary> {
    const params = new HttpParams({ fromObject: cleanParams({ ...filter }) });
    return firstValueFrom(this.http.get<HistorySummary>('/api/v1/history/summary', { params }));
  }

  resend(id: number, source: MessageSource) {
    const params = new HttpParams({ fromObject: { source } });
    return firstValueFrom(this.http.post('/api/v1/history/' + id + '/resend', null, { params }));
  }
}
