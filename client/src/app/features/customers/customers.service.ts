import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { PagedResult, Schemas } from '../../core/api/api.types';

export type CustomerListItem = Schemas['CustomerListItemDto'];
export type CustomerDetail = Schemas['CustomerDetailDto'];
export type CustomerStatus = Schemas['CustomerStatus'];

export interface CustomerFields {
  name: string;
  companyName: string | null;
  email: string | null;
  phone: string | null;
  address: string | null;
  notes: string | null;
  status: CustomerStatus;
}

@Injectable({ providedIn: 'root' })
export class CustomersService {
  private readonly http = inject(HttpClient);

  list(search: string, status: CustomerStatus | '', page: number, pageSize: number): Promise<PagedResult<CustomerListItem>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) {
      params = params.set('search', search);
    }
    if (status) {
      params = params.set('status', status);
    }
    return firstValueFrom(this.http.get<PagedResult<CustomerListItem>>('/api/v1/customers', { params }));
  }

  getById(id: number): Promise<CustomerDetail> {
    return firstValueFrom(this.http.get<CustomerDetail>(`/api/v1/customers/${id}`));
  }

  create(fields: CustomerFields): Promise<CustomerDetail> {
    return firstValueFrom(this.http.post<CustomerDetail>('/api/v1/customers', fields));
  }

  update(id: number, fields: CustomerFields): Promise<CustomerDetail> {
    return firstValueFrom(this.http.put<CustomerDetail>(`/api/v1/customers/${id}`, fields));
  }

  delete(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/customers/${id}`));
  }
}
