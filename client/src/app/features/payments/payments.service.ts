import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import type { PagedResult, Schemas } from '../../core/api/api.types';

export type PaymentListItem = Schemas['PaymentListItemDto'];
export type PaymentMethod = Schemas['PaymentMethod'];
export type PaymentStatus = Schemas['PaymentStatus'];

@Injectable({ providedIn: 'root' })
export class PaymentsService {
  private readonly http = inject(HttpClient);

  uploadProof(file: File): Promise<{ path?: string }> {
    const form = new FormData();
    form.set('file', file);
    return firstValueFrom(this.http.post<{ path?: string }>('/api/v1/payments/proof', form));
  }

  submit(amount: number, method: PaymentMethod, transactionRef: string, note: string, proofFilePath: string | null) {
    return firstValueFrom(
      this.http.post<{ id?: number }>('/api/v1/payments', {
        amount,
        method,
        transactionRef: transactionRef || null,
        note: note || null,
        proofFilePath,
      }),
    );
  }

  mine(page: number, pageSize: number): Promise<PagedResult<PaymentListItem>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return firstValueFrom(this.http.get<PagedResult<PaymentListItem>>('/api/v1/payments/mine', { params }));
  }

  review(status: PaymentStatus | '', page: number, pageSize: number): Promise<PagedResult<PaymentListItem>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) {
      params = params.set('status', status);
    }
    return firstValueFrom(this.http.get<PagedResult<PaymentListItem>>('/api/v1/payments/review', { params }));
  }

  approve(id: number): Promise<void> {
    return firstValueFrom(this.http.post<void>(`/api/v1/payments/${id}/approve`, {}));
  }

  reject(id: number, reviewNote: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`/api/v1/payments/${id}/reject`, { reviewNote: reviewNote || null }));
  }
}
