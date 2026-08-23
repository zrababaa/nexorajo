import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { PagedResult, Schemas } from '../../core/api/api.types';
import { cleanParams } from '../../core/http/params';
import { SessionQueryCache } from '../../core/http/session-query-cache.service';

export type ReportType = 'messages' | 'daily-traffic' | 'batches' | 'account-usage' | 'transactions' | 'credit-requests';

export interface ReportFilter {
  dateFrom?: string;
  dateTo?: string;
  source?: Schemas['MessageSource'] | '';
  status?: Schemas['MessageStatus'] | '';
  accountId?: number | null;
}

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly http = inject(HttpClient);
  private readonly cache = inject(SessionQueryCache);

  messages(filter: ReportFilter, page: number, pageSize: number, onRefresh?: (result: PagedResult<Schemas['HistoryExportRowDto']>) => void) {
    return this.get<Schemas['HistoryExportRowDto']>('messages', filter, page, pageSize, onRefresh);
  }

  dailyTraffic(filter: ReportFilter, page: number, pageSize: number, onRefresh?: (result: PagedResult<Schemas['DailyTrafficRowDto']>) => void) {
    return this.get<Schemas['DailyTrafficRowDto']>('daily-traffic', filter, page, pageSize, onRefresh);
  }

  batches(filter: ReportFilter, page: number, pageSize: number, onRefresh?: (result: PagedResult<Schemas['BatchReportRowDto']>) => void) {
    return this.get<Schemas['BatchReportRowDto']>('batches', filter, page, pageSize, onRefresh);
  }

  accountUsage(filter: ReportFilter, page: number, pageSize: number, onRefresh?: (result: PagedResult<Schemas['AccountUsageRowDto']>) => void) {
    return this.get<Schemas['AccountUsageRowDto']>('account-usage', filter, page, pageSize, onRefresh);
  }

  transactions(filter: ReportFilter, page: number, pageSize: number, onRefresh?: (result: PagedResult<Schemas['TransactionReportRowDto']>) => void) {
    return this.get<Schemas['TransactionReportRowDto']>('transactions', filter, page, pageSize, onRefresh);
  }

  creditRequests(filter: ReportFilter, page: number, pageSize: number, onRefresh?: (result: PagedResult<Schemas['CreditRequestRowDto']>) => void) {
    return this.get<Schemas['CreditRequestRowDto']>('credit-requests', filter, page, pageSize, onRefresh);
  }

  private get<T>(
    path: string,
    filter: ReportFilter,
    page: number,
    pageSize: number,
    onRefresh?: (result: PagedResult<T>) => void,
  ): Promise<PagedResult<T>> {
    const params = new HttpParams({ fromObject: cleanParams({ ...filter, page, pageSize }) });
    return this.cache.get(
      `reports:${path}?${params.toString()}`,
      () => firstValueFrom(this.http.get<PagedResult<T>>(`/api/v1/reports/${path}`, { params })),
      onRefresh,
    );
  }
}
