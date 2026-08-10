import { SlicePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import type { ApiErrorResponse } from '../../core/api/api.types';
import { FlashService } from '../../shared/flash/flash.service';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { PaymentsService, type PaymentListItem, type PaymentMethod } from './payments.service';

const PAGE_SIZE = 10;
const METHODS: PaymentMethod[] = ['GPay', 'PhonePe', 'PayTM', 'Other'];

@Component({
  selector: 'app-payments',
  standalone: true,
  imports: [FormsModule, SlicePipe, TranslocoPipe, PaginationComponent],
  template: `
    <h1 class="mb-4 text-xl font-semibold">{{ 'Request Credits' | transloco }}</h1>

    <div class="grid grid-cols-1 gap-4 lg:grid-cols-12">
      <div class="lg:col-span-5">
        <div class="rounded-card border border-border bg-surface p-4 shadow-card">
          @if (errorMessage()) {
            <p class="mb-3 text-sm text-danger" role="alert">{{ errorMessage() }}</p>
          }

          <div class="mb-3">
            <label class="mb-1 block text-sm font-medium">{{ 'Amount' | transloco }}</label>
            <input type="number" min="0.0001" step="0.0001" class="w-full rounded-card border border-border px-3 py-2 text-sm" [ngModel]="amount()" (ngModelChange)="amount.set($event)" />
          </div>

          <div class="mb-3">
            <label class="mb-1 block text-sm font-medium">{{ 'Method' | transloco }}</label>
            <select class="w-full rounded-card border border-border px-3 py-2 text-sm" [ngModel]="method()" (ngModelChange)="method.set($event)">
              @for (m of methods; track m) {
                <option [value]="m">{{ m }}</option>
              }
            </select>
          </div>

          <div class="mb-3">
            <label class="mb-1 block text-sm font-medium">{{ 'Transaction reference' | transloco }}</label>
            <input class="w-full rounded-card border border-border px-3 py-2 text-sm" [ngModel]="transactionRef()" (ngModelChange)="transactionRef.set($event)" />
          </div>

          <div class="mb-3">
            <label class="mb-1 block text-sm font-medium">{{ 'Note' | transloco }}</label>
            <textarea rows="2" class="w-full rounded-card border border-border px-3 py-2 text-sm" [ngModel]="note()" (ngModelChange)="note.set($event)"></textarea>
          </div>

          <div class="mb-4">
            <label class="mb-1 block text-sm font-medium">{{ 'Proof of payment' | transloco }}</label>
            <input type="file" class="w-full text-sm" (change)="onFileSelected($event)" />
          </div>

          <button
            type="button"
            class="w-full rounded-card bg-primary-500 px-4 py-2 text-sm font-medium text-white hover:bg-primary-600 disabled:opacity-60"
            [disabled]="submitting()"
            (click)="submit()"
          >
            {{ 'Submit request' | transloco }}
          </button>
        </div>
      </div>

      <div class="lg:col-span-7">
        <div class="rounded-card border border-border bg-surface shadow-card">
          <div class="border-b border-border px-4 py-3 text-sm font-semibold">{{ 'Your credit requests' | transloco }}</div>
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
                <tr>
                  <th class="px-4 py-2">{{ 'Date' | transloco }}</th>
                  <th class="px-4 py-2">{{ 'Amount' | transloco }}</th>
                  <th class="px-4 py-2">{{ 'Method' | transloco }}</th>
                  <th class="px-4 py-2">{{ 'Status' | transloco }}</th>
                  <th class="px-4 py-2">{{ 'Note' | transloco }}</th>
                </tr>
              </thead>
              <tbody>
                @if (items().length === 0) {
                  <tr><td colspan="5" class="px-4 py-6 text-center text-text-muted">{{ 'No credit requests yet.' | transloco }}</td></tr>
                }
                @for (p of items(); track p.id) {
                  <tr class="border-t border-border">
                    <td class="px-4 py-2">{{ p.createdAt | slice: 0 : 16 }}</td>
                    <td class="px-4 py-2">{{ p.amount }}</td>
                    <td class="px-4 py-2">{{ p.method }}</td>
                    <td class="px-4 py-2">{{ p.status | transloco }}</td>
                    <td class="px-4 py-2">{{ p.reviewNote }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <div class="border-t border-border px-4">
            <app-pagination [pageNumber]="page()" [pageSize]="PAGE_SIZE" [totalCount]="totalCount()" [totalPages]="totalPages()" (pageChange)="load($event)" />
          </div>
        </div>
      </div>
    </div>
  `,
})
export class PaymentsComponent {
  private readonly payments = inject(PaymentsService);
  private readonly flash = inject(FlashService);

  protected readonly PAGE_SIZE = PAGE_SIZE;
  protected readonly methods = METHODS;

  protected readonly amount = signal<number | null>(null);
  protected readonly method = signal<PaymentMethod>('GPay');
  protected readonly transactionRef = signal('');
  protected readonly note = signal('');
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  private file: File | null = null;

  protected readonly items = signal<PaymentListItem[]>([]);
  protected readonly page = signal(1);
  protected readonly totalCount = signal(0);
  protected readonly totalPages = signal(0);

  constructor() {
    void this.load(1);
  }

  protected async load(page: number): Promise<void> {
    const result = await this.payments.mine(page, PAGE_SIZE);
    this.items.set(result.items ?? []);
    this.page.set(result.pageNumber ?? page);
    this.totalCount.set(result.totalCount ?? 0);
    this.totalPages.set(result.totalPages ?? 0);
  }

  protected onFileSelected(event: Event): void {
    this.file = (event.target as HTMLInputElement).files?.[0] ?? null;
  }

  protected async submit(): Promise<void> {
    this.errorMessage.set(null);
    if (!this.amount() || this.amount()! <= 0) {
      this.errorMessage.set('Enter a valid amount.');
      return;
    }

    this.submitting.set(true);
    try {
      let proofFilePath: string | null = null;
      if (this.file) {
        const uploaded = await this.payments.uploadProof(this.file);
        proofFilePath = uploaded.path ?? null;
      }

      await this.payments.submit(this.amount()!, this.method(), this.transactionRef(), this.note(), proofFilePath);
      this.flash.success('Credit request submitted for review.');
      this.amount.set(null);
      this.transactionRef.set('');
      this.note.set('');
      this.file = null;
      await this.load(1);
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.errorMessage.set((error.error as ApiErrorResponse)?.message ?? 'Unable to submit this request.');
      }
    } finally {
      this.submitting.set(false);
    }
  }
}
