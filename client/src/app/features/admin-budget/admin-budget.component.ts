import { SlicePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import type { ApiErrorResponse } from '../../core/api/api.types';
import { FlashService } from '../../shared/flash/flash.service';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { AdminBudgetService, type AdminBudgetLogRow } from './admin-budget.service';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-admin-budget',
  standalone: true,
  imports: [FormsModule, SlicePipe, TranslocoPipe, PaginationComponent],
  template: `
    <h1 class="mb-4 text-xl font-semibold">{{ 'Admin Budget' | transloco }}</h1>

    <div class="mb-4 grid grid-cols-1 gap-4 lg:grid-cols-12">
      <div class="lg:col-span-4">
        <div class="rounded-card border border-border bg-surface p-4 shadow-card">
          <div class="text-xs font-medium uppercase tracking-wide text-text-muted">{{ 'Pool balance' | transloco }}</div>
          <div class="mt-1 text-2xl font-bold">{{ balance() }}</div>
        </div>
      </div>

      <div class="lg:col-span-8">
        <div class="rounded-card border border-border bg-surface p-4 shadow-card">
          @if (errorMessage()) {
            <p class="mb-3 text-sm text-danger" role="alert">{{ errorMessage() }}</p>
          }
          <div class="flex flex-wrap items-end gap-3">
            <div>
              <label class="mb-1 block text-sm font-medium">{{ 'Amount' | transloco }}</label>
              <input type="number" min="0" step="0.0001" class="w-40 rounded-card border border-border px-3 py-2 text-sm" [ngModel]="amount()" (ngModelChange)="amount.set($event)" />
            </div>
            <div class="min-w-0 flex-1">
              <label class="mb-1 block text-sm font-medium">{{ 'Note' | transloco }}</label>
              <input class="w-full rounded-card border border-border px-3 py-2 text-sm" [ngModel]="note()" (ngModelChange)="note.set($event)" />
            </div>
            <button
              type="button"
              class="rounded-card bg-success px-4 py-2 text-sm font-medium text-white hover:bg-success/90 disabled:opacity-60"
              [disabled]="saving()"
              (click)="apply('Credit')"
            >
              {{ 'Add Balance' | transloco }}
            </button>
            <button
              type="button"
              class="rounded-card bg-danger px-4 py-2 text-sm font-medium text-white hover:bg-danger/90 disabled:opacity-60"
              [disabled]="saving()"
              (click)="apply('Debit')"
            >
              {{ 'Deduct Balance' | transloco }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <div class="rounded-card border border-border bg-surface shadow-card">
      <div class="border-b border-border px-4 py-3 text-sm font-semibold">{{ 'Pool history' | transloco }}</div>
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
            <tr>
              <th class="px-4 py-2">{{ 'Date' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Performed by' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Kind' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Source' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Amount' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Balance after' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Related account' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Note' | transloco }}</th>
            </tr>
          </thead>
          <tbody>
            @if (rows().length === 0) {
              <tr><td colspan="8" class="px-4 py-6 text-center text-text-muted">{{ 'No pool activity yet.' | transloco }}</td></tr>
            }
            @for (r of rows(); track r.id) {
              <tr class="border-t border-border">
                <td class="px-4 py-2">{{ r.createdAt | slice: 0 : 16 }}</td>
                <td class="px-4 py-2">{{ r.performedByUsername }}</td>
                <td class="px-4 py-2">{{ r.kind | transloco }}</td>
                <td class="px-4 py-2">{{ r.source }}</td>
                <td class="px-4 py-2">{{ r.amount }}</td>
                <td class="px-4 py-2">{{ r.balanceAfter }}</td>
                <td class="px-4 py-2">{{ r.relatedUsername }}</td>
                <td class="px-4 py-2">{{ r.note }}</td>
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
export class AdminBudgetComponent {
  private readonly adminBudget = inject(AdminBudgetService);
  private readonly flash = inject(FlashService);

  protected readonly PAGE_SIZE = PAGE_SIZE;
  protected readonly balance = signal(0);
  protected readonly amount = signal<number | null>(null);
  protected readonly note = signal('');
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly rows = signal<AdminBudgetLogRow[]>([]);
  protected readonly page = signal(1);
  protected readonly totalCount = signal(0);
  protected readonly totalPages = signal(0);

  constructor() {
    void this.adminBudget.getBalance().then((r) => this.balance.set(r.balance ?? 0));
    void this.load(1);
  }

  protected async load(page: number): Promise<void> {
    const result = await this.adminBudget.getLog(page, PAGE_SIZE);
    this.rows.set(result.items ?? []);
    this.page.set(result.pageNumber ?? page);
    this.totalCount.set(result.totalCount ?? 0);
    this.totalPages.set(result.totalPages ?? 0);
  }

  protected async apply(kind: 'Credit' | 'Debit'): Promise<void> {
    this.errorMessage.set(null);
    if (this.amount() === null || this.amount()! <= 0) {
      this.errorMessage.set('Enter a valid amount.');
      return;
    }

    this.saving.set(true);
    try {
      const result = await this.adminBudget.adjustBalance(this.amount()!, kind, this.note());
      this.balance.set(result.balance ?? 0);
      this.flash.success(kind === 'Credit' ? 'Balance added.' : 'Balance deducted.');
      this.amount.set(null);
      this.note.set('');
      await this.load(1);
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.errorMessage.set((error.error as ApiErrorResponse)?.message ?? 'Unable to update the balance.');
      }
    } finally {
      this.saving.set(false);
    }
  }
}
