import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface SendingWindow {
  isEnabled: boolean;
  startTime: string;
  endTime: string;
}

@Injectable({ providedIn: 'root' })
export class SendingWindowService {
  private readonly http = inject(HttpClient);

  get(): Promise<SendingWindow> {
    return firstValueFrom(this.http.get<SendingWindow>('/api/v1/sending-window'));
  }

  set(isEnabled: boolean, startTime: string, endTime: string): Promise<SendingWindow> {
    return firstValueFrom(this.http.post<SendingWindow>('/api/v1/sending-window', { isEnabled, startTime, endTime }));
  }
}
