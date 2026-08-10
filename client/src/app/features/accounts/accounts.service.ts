import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { PagedResult, Schemas } from '../../core/api/api.types';

export type AccountListItem = Schemas['AccountListItemDto'];
export type AccountDetail = Schemas['AccountDetailDto'];
export type AccountOption = Schemas['AccountOptionDto'];
export type CreateAccountRequest = Schemas['CreateAccountApiRequest'];
export type UpdateAccountRequest = Schemas['UpdateAccountApiRequest'];
export type BalanceResponse = Schemas['BalanceApiResponse'];
export type ApiCredentialsResponse = Schemas['ApiCredentialsApiResponse'];

@Injectable({ providedIn: 'root' })
export class AccountsService {
  private readonly http = inject(HttpClient);

  list(page: number, pageSize: number): Promise<PagedResult<AccountListItem>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return firstValueFrom(this.http.get<PagedResult<AccountListItem>>('/api/v1/accounts', { params }));
  }

  options(): Promise<AccountOption[]> {
    return firstValueFrom(this.http.get<AccountOption[]>('/api/v1/accounts/options'));
  }

  getById(id: number): Promise<AccountDetail> {
    return firstValueFrom(this.http.get<AccountDetail>(`/api/v1/accounts/${id}`));
  }

  create(request: CreateAccountRequest): Promise<AccountDetail> {
    return firstValueFrom(this.http.post<AccountDetail>('/api/v1/accounts', request));
  }

  update(id: number, request: UpdateAccountRequest): Promise<AccountDetail> {
    return firstValueFrom(this.http.put<AccountDetail>(`/api/v1/accounts/${id}`, request));
  }

  regenerateApiCredentials(id: number): Promise<ApiCredentialsResponse> {
    return firstValueFrom(this.http.post<ApiCredentialsResponse>(`/api/v1/accounts/${id}/api-credentials`, {}));
  }

  credit(id: number, amount: number): Promise<BalanceResponse> {
    return firstValueFrom(this.http.post<BalanceResponse>(`/api/v1/accounts/${id}/credit`, { amount }));
  }

  debit(id: number, amount: number): Promise<BalanceResponse> {
    return firstValueFrom(this.http.post<BalanceResponse>(`/api/v1/accounts/${id}/debit`, { amount }));
  }
}
