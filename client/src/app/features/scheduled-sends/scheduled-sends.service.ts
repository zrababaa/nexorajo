import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { PagedResult } from '../../core/api/api.types';

export type ScheduledSendStatus = 'Pending' | 'Sent' | 'Failed' | 'Cancelled';

export interface ScheduledSend {
  id: number;
  campaignName: string;
  message: string;
  senderId: string;
  scheduledAtUtc: string;
  status: ScheduledSendStatus;
  batchId: string | null;
  errorMessage: string | null;
}

@Injectable({ providedIn: 'root' })
export class ScheduledSendsService {
  private readonly http = inject(HttpClient);

  create(campaignId: number, message: string, senderId: string, scheduledAt: string): Promise<ScheduledSend> {
    return firstValueFrom(
      this.http.post<ScheduledSend>('/api/v1/scheduled-sends', { campaignId, message, senderId: senderId || null, scheduledAt }),
    );
  }

  list(page: number, pageSize: number): Promise<PagedResult<ScheduledSend>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return firstValueFrom(this.http.get<PagedResult<ScheduledSend>>('/api/v1/scheduled-sends', { params }));
  }

  cancel(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/scheduled-sends/${id}`));
  }
}
