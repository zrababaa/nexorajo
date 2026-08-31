import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface PreparedLink {
  /** The link to drop into the message body - a Short.io URL when shortened, else {BaseUrl}/l/{token}. */
  trackingUrl: string;
  destinationUrl: string;
  /** False when shortening was asked for but fell back to the full-length link. */
  shortened: boolean;
}

@Injectable({ providedIn: 'root' })
export class LinkTrackingService {
  private readonly http = inject(HttpClient);

  /** Mints a tracking link for `url` (optionally shortened) before the send that will carry it. */
  prepare(url: string, shorten: boolean): Promise<PreparedLink> {
    return firstValueFrom(
      this.http.post<PreparedLink>('/api/v1/link-tracking/prepare', { url, shorten }),
    );
  }
}
