import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { PagedResult, Schemas } from '../../core/api/api.types';
import { cleanParams } from '../../core/http/params';

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

  messages(filter: ReportFilter, page: number, pageSize: number) {
    return this.get<Schemas['HistoryExportRowDto']>('messages', filter, page, pageSize);
  }

  dailyTraffic(filter: ReportFilter, page: number, pageSize: number) {
    return this.get<Schemas['DailyTrafficRowDto']>('daily-traffic', filter, page, pageSize);
  }

  batches(filter: ReportFilter, page: number, pageSize: number) {
    return this.get<Schemas['BatchReportRowDto']>('batches', filter, page, pageSize);
  }

  accountUsage(filter: ReportFilter, page: number, pageSize: number) {
    return this.get<Schemas['AccountUsageRowDto']>('account-usage', filter, page, pageSize);
  }

  transactions(filter: ReportFilter, page: number, pageSize: number) {
    return this.get<Schemas['TransactionReportRowDto']>('transactions', filter, page, pageSize);
  }

  creditRequests(filter: ReportFilter, page: number, pageSize: number) {
    return this.get<Schemas['CreditRequestRowDto']>('credit-requests', filter, page, pageSize);
  }

  private get<T>(path: string, filter: ReportFilter, page: number, pageSize: number): Promise<PagedResult<T>> {
    const params = new HttpParams({ fromObject: cleanParams({ ...filter, page, pageSize }) });
    return firstValueFrom(this.http.get<PagedResult<T>>(`/api/v1/reports/${path}`, { params }));
  }
}
