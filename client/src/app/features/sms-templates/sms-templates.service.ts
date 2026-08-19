import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { PagedResult, Schemas } from '../../core/api/api.types';

export type SmsTemplateListItem = Schemas['SmsTemplateListItemDto'];
export type SmsTemplateDetail = Schemas['SmsTemplateDetailDto'];

@Injectable({ providedIn: 'root' })
export class SmsTemplatesService {
  private readonly http = inject(HttpClient);

  list(page: number, pageSize: number): Promise<PagedResult<SmsTemplateListItem>> {
    return firstValueFrom(
      this.http.get<PagedResult<SmsTemplateListItem>>('/api/v1/sms-templates', { params: { page, pageSize } }),
    );
  }

  getById(id: number): Promise<SmsTemplateDetail> {
    return firstValueFrom(this.http.get<SmsTemplateDetail>(`/api/v1/sms-templates/${id}`));
  }

  create(name: string, body: string): Promise<SmsTemplateDetail> {
    return firstValueFrom(this.http.post<SmsTemplateDetail>('/api/v1/sms-templates', { name, body }));
  }

  update(id: number, name: string, body: string): Promise<SmsTemplateDetail> {
    return firstValueFrom(this.http.put<SmsTemplateDetail>(`/api/v1/sms-templates/${id}`, { name, body }));
  }

  delete(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/sms-templates/${id}`));
  }
}
