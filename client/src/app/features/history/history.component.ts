import { SlicePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import type { ApiErrorResponse } from '../../core/api/api.types';
import { FileDownloadService } from '../../core/http/file-download.service';
import { cleanParams } from '../../core/http/params';
import { AuthService } from '../../core/auth/auth.service';
import { FlashService } from '../../shared/flash/flash.service';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { sourceLabel, statusColor } from '../dashboard/chart-colors';
import { HistoryService, type HistoryFilter, type HistoryListItem, type HistorySummary } from './history.service';

const PAGE_SIZE = 20;
const RESENDABLE = new Set(['Failed', 'Undelivered', 'Expired']);
const SOURCES = ['QuickSend', 'BulkSend', 'PublicApi'] as const;
const STATUSES = ['Processing', 'Sent', 'Delivered', 'Undelivered', 'Expired', 'Failed'] as const;

@Component({
  selector: 'app-history',
  standalone: true,
  imports: [FormsModule, SlicePipe, TranslocoPipe, PaginationComponent],
  template: `
    <div class="mb-4 flex flex-wrap items-center justify-between gap-2">
      <h1 class="text-xl font-semibold">{{ 'Message History' | transloco }}</h1>
      <div class="flex gap-2">
        <button type="button" class="rounded-card border border-border px-3 py-1.5 text-sm hover:bg-surface-muted" (click)="exportFile('csv')">
          {{ 'Export CSV' | transloco }}
        </button>
        <button type="button" class="rounded-card border border-border px-3 py-1.5 text-sm hover:bg-surface-muted" (click)="exportFile('excel')">
          {{ 'Export Excel' | transloco }}
        </button>
      </div>
    </div>

    <div class="mb-4 rounded-card border border-border bg-surface p-4 shadow-card">
      <div class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
        <div>
          <label class="mb-1 block text-xs font-medium text-text-muted">{{ 'Channel' | transloco }}</label>
          <select class="w-full rounded-card border border-border px-2 py-1.5 text-sm" [ngModel]="source()" (ngModelChange)="source.set($event)">
            <option value="">{{ 'All' | transloco }}</option>
            @for (s of sources; track s) {
              <option [value]="s">{{ sourceLabel(s) | transloco }}</option>
            }
          </select>
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-text-muted">{{ 'Status' | transloco }}</label>
          <select class="w-full rounded-card border border-border px-2 py-1.5 text-sm" [ngModel]="status()" (ngModelChange)="status.set($event)">
            <option value="">{{ 'All' | transloco }}</option>
            @for (s of statuses; track s) {
              <option [value]="s">{{ s | transloco }}</option>
            }
          </select>
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-text-muted">{{ 'Receiver' | transloco }}</label>
          <input class="w-full rounded-card border border-border px-2 py-1.5 text-sm" [ngModel]="receiver()" (ngModelChange)="receiver.set($event)" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-text-muted">{{ 'Batch Id' | transloco }}</label>
          <input class="w-full rounded-card border border-border px-2 py-1.5 text-sm" [ngModel]="campaignBatchId()" (ngModelChange)="campaignBatchId.set($event)" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-text-muted">{{ 'From' | transloco }}</label>
          <input type="date" class="w-full rounded-card border border-border px-2 py-1.5 text-sm" [ngModel]="dateFrom()" (ngModelChange)="dateFrom.set($event)" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-text-muted">{{ 'To' | transloco }}</label>
          <input type="date" class="w-full rounded-card border border-border px-2 py-1.5 text-sm" [ngModel]="dateTo()" (ngModelChange)="dateTo.set($event)" />
        </div>
      </div>
      <div class="mt-3 flex gap-2">
        <button type="button" class="rounded-card bg-primary-500 px-3 py-1.5 text-sm font-medium text-white hover:bg-primary-600" (click)="load(1)">
          {{ 'Filter' | transloco }}
        </button>
        <button type="button" class="rounded-card border border-border px-3 py-1.5 text-sm hover:bg-surface-muted" (click)="clear()">
          {{ 'Clear' | transloco }}
        </button>
      </div>
    </div>

    @if (summary(); as s) {
      <div class="mb-4 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
        <div class="rounded-card border border-border bg-surface p-3 shadow-card">
          <div class="text-xs uppercase text-text-muted">{{ 'Total' | transloco }}</div>
          <div class="text-xl font-bold">{{ s.total }}</div>
        </div>
        <div class="rounded-card border border-border bg-surface p-3 shadow-card">
          <div class="text-xs uppercase text-text-muted">{{ 'Delivered' | transloco }}</div>
          <div class="text-xl font-bold" [style.color]="statusColor('Delivered')">{{ s.delivered }}</div>
        </div>
        <div class="rounded-card border border-border bg-surface p-3 shadow-card">
          <div class="text-xs uppercase text-text-muted">{{ 'Sent' | transloco }}</div>
          <div class="text-xl font-bold" [style.color]="statusColor('Sent')">{{ s.sent }}</div>
        </div>
        <div class="rounded-card border border-border bg-surface p-3 shadow-card">
          <div class="text-xs uppercase text-text-muted">{{ 'Processing' | transloco }}</div>
          <div class="text-xl font-bold" [style.color]="statusColor('Processing')">{{ s.processing }}</div>
        </div>
        <div class="rounded-card border border-border bg-surface p-3 shadow-card">
          <div class="text-xs uppercase text-text-muted">{{ 'Undelivered' | transloco }}</div>
          <div class="text-xl font-bold" [style.color]="statusColor('Undelivered')">{{ s.undelivered }}</div>
        </div>
        <div class="rounded-card border border-border bg-surface p-3 shadow-card">
          <div class="text-xs uppercase text-text-muted">{{ 'Failed / Expired' | transloco }}</div>
          <div class="text-xl font-bold" [style.color]="statusColor('Failed')">{{ (s.failed ?? 0) + (s.expired ?? 0) }}</div>
        </div>
      </div>
    }

    <div class="rounded-card border border-border bg-surface shadow-card">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
            <tr>
              <th class="px-4 py-2">{{ 'Batch' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Channel' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Sender' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Receiver' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Status' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Date (UTC)' | transloco }}</th>
              <th class="px-4 py-2 text-right">{{ 'Actions' | transloco }}</th>
            </tr>
          </thead>
          <tbody>
            @if (items().length === 0) {
              <tr><td colspan="7" class="px-4 py-6 text-center text-text-muted">{{ 'No messages match these filters.' | transloco }}</td></tr>
            }
            @for (h of items(); track h.id) {
              <tr class="border-t border-border">
                <td class="px-4 py-2"><code [title]="h.campaignBatchId">{{ h.campaignBatchId?.slice(0, 8) }}</code></td>
                <td class="px-4 py-2">{{ sourceLabel(h.source!) | transloco }}</td>
                <td class="px-4 py-2">{{ h.senderNumber }}</td>
                <td class="px-4 py-2">{{ h.receiverNumber }}</td>
                <td class="px-4 py-2">
                  <span class="mr-1" [style.color]="statusColor(h.status!)">&#9679;</span>{{ h.status | transloco }}
                </td>
                <td class="px-4 py-2">{{ h.createdAt | slice: 0 : 16 }}</td>
                <td class="px-4 py-2 text-right">
                  @if (canResend(h)) {
                    <button type="button" class="rounded-card border border-border px-2 py-1 text-xs hover:bg-surface-muted" (click)="resend(h)">
                      {{ 'Resend' | transloco }}
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
export class HistoryComponent {
  private readonly historyService = inject(HistoryService);
  private readonly download = inject(FileDownloadService);
  private readonly flash = inject(FlashService);
  private readonly auth = inject(AuthService);

  protected readonly PAGE_SIZE = PAGE_SIZE;
  protected readonly sources = SOURCES;
  protected readonly statuses = STATUSES;
  protected readonly sourceLabel = sourceLabel;
  protected readonly statusColor = statusColor;

  protected readonly source = signal<HistoryFilter['source'] | ''>('');
  protected readonly status = signal<HistoryFilter['status'] | ''>('');
  protected readonly receiver = signal('');
  protected readonly campaignBatchId = signal('');
  protected readonly dateFrom = signal('');
  protected readonly dateTo = signal('');

  protected readonly items = signal<HistoryListItem[]>([]);
  protected readonly summary = signal<HistorySummary | null>(null);
  protected readonly page = signal(1);
  protected readonly totalCount = signal(0);
  protected readonly totalPages = signal(0);

  private readonly filter = computed<HistoryFilter>(() => ({
    source: this.source() || undefined,
    status: this.status() || undefined,
    receiver: this.receiver() || undefined,
    campaignBatchId: this.campaignBatchId() || undefined,
    dateFrom: this.dateFrom() || undefined,
    dateTo: this.dateTo() || undefined,
  }));

  constructor() {
    void this.load(1);
  }

  protected async load(page: number): Promise<void> {
    const filter = this.filter();
    const [list, summary] = await Promise.all([
      this.historyService.list(filter, page, PAGE_SIZE),
      this.historyService.summary(filter),
    ]);
    this.items.set(list.items ?? []);
    this.page.set(list.pageNumber ?? page);
    this.totalCount.set(list.totalCount ?? 0);
    this.totalPages.set(list.totalPages ?? 0);
    this.summary.set(summary);
  }

  protected clear(): void {
    this.source.set('');
    this.status.set('');
    this.receiver.set('');
    this.campaignBatchId.set('');
    this.dateFrom.set('');
    this.dateTo.set('');
    void this.load(1);
  }

  protected canResend(item: HistoryListItem): boolean {
    return item.createdByUserId === this.auth.user()?.id && RESENDABLE.has(item.status ?? '');
  }

  protected async resend(item: HistoryListItem): Promise<void> {
    try {
      await this.historyService.resend(item.id!, item.source!);
      this.flash.success('Message resent.');
      await this.load(this.page());
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.flash.error((error.error as ApiErrorResponse)?.message ?? 'Unable to resend this message.');
      }
    }
  }

  protected async exportFile(format: 'csv' | 'excel'): Promise<void> {
    await this.download.download('/api/v1/history/export', cleanParams({ ...this.filter(), format }));
  }
}
