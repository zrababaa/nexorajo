import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { Schemas } from '../../core/api/api.types';

export type SpamKeyword = Schemas['SpamKeywordListItemDto'];
export type SpamKeywordType = Schemas['SpamKeywordType'];

@Injectable({ providedIn: 'root' })
export class SpamKeywordsService {
  private readonly http = inject(HttpClient);

  list(): Promise<SpamKeyword[]> {
    return firstValueFrom(this.http.get<SpamKeyword[]>('/api/v1/spam-keywords'));
  }

  create(keyword: string, keywordType: SpamKeywordType): Promise<SpamKeyword> {
    return firstValueFrom(this.http.post<SpamKeyword>('/api/v1/spam-keywords', { keyword, keywordType }));
  }

  delete(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/spam-keywords/${id}`));
  }

  setEnabled(id: number, isEnabled: boolean): Promise<void> {
    return firstValueFrom(this.http.patch<void>(`/api/v1/spam-keywords/${id}/enabled?isEnabled=${isEnabled}`, {}));
  }
}
