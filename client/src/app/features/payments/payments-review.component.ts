import { SlicePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import type { ApiErrorResponse } from '../../core/api/api.types';
import { FlashService } from '../../shared/flash/flash.service';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { PaymentsService, type PaymentListItem, type PaymentStatus } from './payments.service';

const PAGE_SIZE = 20;
const STATUSES: PaymentStatus[] = ['Pending', 'Approved', 'Rejected'];

@Component({
  selector: 'app-payments-review',
  standalone: true,
  imports: [FormsModule, SlicePipe, TranslocoPipe, PaginationComponent],
  template: `
    <h1 class="mb-4 text-xl font-semibold">{{ 'Credit Requests' | transloco }}</h1>

    <div class="mb-4 flex items-center gap-2">
      <label class="text-sm font-medium">{{ 'Status' | transloco }}</label>
      <select class="rounded-card border border-border px-2 py-1.5 text-sm" [ngModel]="status()" (ngModelChange)="onStatusChange($event)">
        <option value="">{{ 'All' | transloco }}</option>
        @for (s of statuses; track s) {
          <option [value]="s">{{ s | transloco }}</option>
        }
      </select>
    </div>

    <div class="rounded-card border border-border bg-surface shadow-card">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
            <tr>
              <th class="px-4 py-2">{{ 'Date' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Account' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Amount' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Method' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Reference' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Status' | transloco }}</th>
              <th class="px-4 py-2 text-right">{{ 'Actions' | transloco }}</th>
            </tr>
          </thead>
          <tbody>
            @if (items().length === 0) {
              <tr><td colspan="7" class="px-4 py-6 text-center text-text-muted">{{ 'No credit requests found.' | transloco }}</td></tr>
            }
            @for (p of items(); track p.id) {
              <tr class="border-t border-border">
                <td class="px-4 py-2">{{ p.createdAt | slice: 0 : 16 }}</td>
                <td class="px-4 py-2">{{ p.submittedByUsername }}</td>
                <td class="px-4 py-2">{{ p.amount }}</td>
                <td class="px-4 py-2">{{ p.method }}</td>
                <td class="px-4 py-2">{{ p.transactionRef }}</td>
                <td class="px-4 py-2">{{ p.status | transloco }}</td>
                <td class="px-4 py-2 text-right">
                  @if (p.status === 'Pending') {
                    <button type="button" class="rounded-card border border-border px-2 py-1 text-xs text-success hover:bg-surface-muted" (click)="approve(p)">
                      {{ 'Approve' | transloco }}
                    </button>
                    <button type="button" class="ml-2 rounded-card border border-border px-2 py-1 text-xs text-danger hover:bg-surface-muted" (click)="reject(p)">
                      {{ 'Reject' | transloco }}
                    </button>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
      <div class="border-t border-border px-4">
        <app-pagination [pageNumber]="page()" [pageSize]="PAGE_SIZE" [totalCount]="totalCount()" [totalPages]="totalPages()" (pageChange)="load($event)" />
      </div>
    </div>
  `,
})
export class PaymentsReviewComponent {
  private readonly payments = inject(PaymentsService);
  private readonly flash = inject(FlashService);
  private readonly transloco = inject(TranslocoService);

  protected readonly PAGE_SIZE = PAGE_SIZE;
  protected readonly statuses = STATUSES;

  protected readonly status = signal<PaymentStatus | ''>('Pending');
  protected readonly items = signal<PaymentListItem[]>([]);
  protected readonly page = signal(1);
  protected readonly totalCount = signal(0);
  protected readonly totalPages = signal(0);

  constructor() {
    void this.load(1);
  }

  protected onStatusChange(value: PaymentStatus | ''): void {
    this.status.set(value);
    void this.load(1);
  }

  protected async load(page: number): Promise<void> {
    const result = await this.payments.review(this.status(), page, PAGE_SIZE);
    this.items.set(result.items ?? []);
    this.page.set(result.pageNumber ?? page);
    this.totalCount.set(result.totalCount ?? 0);
    this.totalPages.set(result.totalPages ?? 0);
  }

  protected async approve(item: PaymentListItem): Promise<void> {
    try {
      await this.payments.approve(item.id!);
      this.flash.success('Request approved and the account credited.');
      await this.load(this.page());
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.flash.error((error.error as ApiErrorResponse)?.message ?? 'Unable to approve this request.');
      }
    }
  }

  protected async reject(item: PaymentListItem): Promise<void> {
    const note = prompt(this.transloco.translate('Reason for rejecting (optional)')) ?? '';
    try {
      await this.payments.reject(item.id!, note);
      this.flash.success('Request rejected.');
      await this.load(this.page());
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.flash.error((error.error as ApiErrorResponse)?.message ?? 'Unable to reject this request.');
      }
    }
  }
}
