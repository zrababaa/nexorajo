import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { PagedResult, Schemas } from '../../core/api/api.types';

export type AdminBudgetLogRow = Schemas['AdminBudgetLogRowDto'];

@Injectable({ providedIn: 'root' })
export class AdminBudgetService {
  private readonly http = inject(HttpClient);

  getBalance(): Promise<{ balance?: number }> {
    return firstValueFrom(this.http.get<{ balance?: number }>('/api/v1/admin-budget'));
  }

  getLog(page: number, pageSize: number): Promise<PagedResult<AdminBudgetLogRow>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return firstValueFrom(this.http.get<PagedResult<AdminBudgetLogRow>>('/api/v1/admin-budget/log', { params }));
  }

  setBalance(newBalance: number, note: string): Promise<{ balance?: number }> {
    return firstValueFrom(
      this.http.post<{ balance?: number }>('/api/v1/admin-budget/balance', { newBalance, note: note || null }),
    );
  }
}
