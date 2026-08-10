import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { Schemas } from '../../core/api/api.types';

export type BulkSendRequest = Schemas['BulkSendApiRequest'];
export type SendSummary = Schemas['SendSummaryDto'];

@Injectable({ providedIn: 'root' })
export class BulkSendService {
  private readonly http = inject(HttpClient);

  submit(request: BulkSendRequest): Promise<SendSummary> {
    return firstValueFrom(this.http.post<SendSummary>('/api/v1/messages/bulk-send', request));
  }
}
