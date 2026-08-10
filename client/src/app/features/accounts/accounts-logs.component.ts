import { SlicePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { ReportsService } from '../reports/reports.service';
import type { Schemas } from '../../core/api/api.types';

const PAGE_SIZE = 25;

@Component({
  selector: 'app-accounts-logs',
  standalone: true,
  imports: [SlicePipe, TranslocoPipe, PaginationComponent],
  template: `
    <h1 class="mb-4 text-xl font-semibold">{{ 'Accounts Logs' | transloco }}</h1>
    <p class="mb-4 text-sm text-text-muted">{{ 'Every Bulk Send batch across all accounts.' | transloco }}</p>

    <div class="rounded-card border border-border bg-surface shadow-card">
      <div class="overflow-x-auto">
        <table class="w-full whitespace-nowrap text-sm">
          <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
            <tr>
              <th class="px-4 py-2">{{ 'Batch' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Campaign' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Sender' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Account' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Recipients' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Delivered' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Failed' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Pending' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Cost' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Date (UTC)' | transloco }}</th>
            </tr>
          </thead>
          <tbody>
            @if (rows().length === 0) {
              <tr><td colspan="10" class="px-4 py-6 text-center text-text-muted">{{ 'No batches yet.' | transloco }}</td></tr>
            }
            @for (r of rows(); track r.batchId) {
              <tr class="border-t border-border">
                <td class="px-4 py-2"><code>{{ r.batchId?.slice(0, 8) }}</code></td>
                <td class="px-4 py-2">{{ r.campaignName }}</td>
                <td class="px-4 py-2">{{ r.senderId }}</td>
                <td class="px-4 py-2">{{ r.accountUsername }}</td>
                <td class="px-4 py-2">{{ r.recipients }}</td>
                <td class="px-4 py-2">{{ r.delivered }}</td>
                <td class="px-4 py-2">{{ r.failed }}</td>
                <td class="px-4 py-2">{{ r.pending }}</td>
                <td class="px-4 py-2">{{ r.cost }}</td>
                <td class="px-4 py-2">{{ r.createdAt | slice: 0 : 16 }}</td>
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
export class AccountsLogsComponent {
  private readonly reports = inject(ReportsService);

  protected readonly PAGE_SIZE = PAGE_SIZE;
  protected readonly rows = signal<Schemas['BatchReportRowDto'][]>([]);
  protected readonly page = signal(1);
  protected readonly totalCount = signal(0);
  protected readonly totalPages = signal(0);

  constructor() {
    void this.load(1);
  }

  protected async load(page: number): Promise<void> {
    const result = await this.reports.batches({ source: 'BulkSend' }, page, PAGE_SIZE);
    this.rows.set(result.items ?? []);
    this.page.set(result.pageNumber ?? page);
    this.totalCount.set(result.totalCount ?? 0);
    this.totalPages.set(result.totalPages ?? 0);
  }
}
