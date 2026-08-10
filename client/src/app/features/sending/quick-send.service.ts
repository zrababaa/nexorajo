import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { Schemas } from '../../core/api/api.types';

export type QuickSendRequest = Schemas['QuickSendApiRequest'];
export type QuickSendResponse = Schemas['QuickSendApiResponse'];

@Injectable({ providedIn: 'root' })
export class QuickSendService {
  private readonly http = inject(HttpClient);

  submit(request: QuickSendRequest): Promise<QuickSendResponse> {
    return firstValueFrom(this.http.post<QuickSendResponse>('/api/v1/messages/quick-send', request));
  }
}
