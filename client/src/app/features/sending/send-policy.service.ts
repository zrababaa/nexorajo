import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { Schemas } from '../../core/api/api.types';

export type SendPolicy = Schemas['SendPolicyApiResponse'];

@Injectable({ providedIn: 'root' })
export class SendPolicyService {
  private readonly http = inject(HttpClient);

  getPolicy(): Promise<SendPolicy> {
    return firstValueFrom(this.http.get<SendPolicy>('/api/v1/messages/send-policy'));
  }
}
