import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { PagedResult, Schemas } from '../../core/api/api.types';

export type CampaignListItem = Schemas['CampaignListItemDto'];
export type CampaignDetail = Schemas['CampaignDetailDto'];

@Injectable({ providedIn: 'root' })
export class CampaignsService {
  private readonly http = inject(HttpClient);

  list(page: number, pageSize: number): Promise<PagedResult<CampaignListItem>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return firstValueFrom(this.http.get<PagedResult<CampaignListItem>>('/api/v1/campaigns', { params }));
  }

  getById(id: number): Promise<CampaignDetail> {
    return firstValueFrom(this.http.get<CampaignDetail>(`/api/v1/campaigns/${id}`));
  }

  create(name: string, numbers: string, externalCampaignCode?: string): Promise<CampaignDetail> {
    return firstValueFrom(
      this.http.post<CampaignDetail>('/api/v1/campaigns', { name, numbers, externalCampaignCode: externalCampaignCode || null }),
    );
  }

  import(name: string, file: File, externalCampaignCode?: string): Promise<CampaignDetail> {
    const form = new FormData();
    form.set('name', name);
    if (externalCampaignCode) {
      form.set('externalCampaignCode', externalCampaignCode);
    }
    form.set('file', file);
    return firstValueFrom(this.http.post<CampaignDetail>('/api/v1/campaigns/import', form));
  }

  update(id: number, name: string, numbers: string): Promise<CampaignDetail> {
    return firstValueFrom(this.http.put<CampaignDetail>(`/api/v1/campaigns/${id}`, { name, numbers }));
  }

  delete(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/campaigns/${id}`));
  }
}
