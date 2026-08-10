import { DecimalPipe, SlicePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import type { Schemas } from '../../core/api/api.types';
import { AuthService } from '../../core/auth/auth.service';
import { FileDownloadService } from '../../core/http/file-download.service';
import { cleanParams } from '../../core/http/params';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { AccountsService, type AccountOption } from '../accounts/accounts.service';
import { sourceLabel } from '../dashboard/chart-colors';
import { ReportsService, type ReportFilter, type ReportType } from './reports.service';

const PAGE_SIZE = 25;

const TABS: { type: ReportType; label: string; exportName: string }[] = [
  { type: 'messages', label: 'Messages', exportName: 'Messages' },
  { type: 'daily-traffic', label: 'Daily Traffic', exportName: 'DailyTraffic' },
  { type: 'batches', label: 'Batches', exportName: 'Batches' },
  { type: 'account-usage', label: 'Account Usage', exportName: 'AccountUsage' },
  { type: 'transactions', label: 'Transactions', exportName: 'Transactions' },
  { type: 'credit-requests', label: 'Credit Requests', exportName: 'CreditRequests' },
];

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [FormsModule, DecimalPipe, SlicePipe, TranslocoPipe, PaginationComponent],
  template: `
    <h1 class="mb-4 text-xl font-semibold">{{ 'Reports' | transloco }}</h1>

    <div class="mb-4 flex flex-wrap gap-1 border-b border-border">
      @for (tab of tabs; track tab.type) {
        <button
          type="button"
          class="rounded-t-card px-3 py-2 text-sm font-medium"
          [class.border-b-2]="tab.type === activeTab().type"
          [class.border-primary-500]="tab.type === activeTab().type"
          [class.text-primary-700]="tab.type === activeTab().type"
          [class.text-text-muted]="tab.type !== activeTab().type"
          (click)="selectTab(tab)"
        >
          {{ tab.label | transloco }}
        </button>
      }
    </div>

    <div class="mb-4 rounded-card border border-border bg-surface p-4 shadow-card">
      <div class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
        <div>
          <label class="mb-1 block text-xs font-medium text-text-muted">{{ 'From' | transloco }}</label>
          <input type="date" class="w-full rounded-card border border-border px-2 py-1.5 text-sm" [ngModel]="dateFrom()" (ngModelChange)="dateFrom.set($event)" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-text-muted">{{ 'To' | transloco }}</label>
          <input type="date" class="w-full rounded-card border border-border px-2 py-1.5 text-sm" [ngModel]="dateTo()" (ngModelChange)="dateTo.set($event)" />
        </div>
        <div>
          <label class="mb-1 block text-xs font-medium text-text-muted">{{ 'Channel' | transloco }}</label>
          <select class="w-full rounded-card border border-border px-2 py-1.5 text-sm" [ngModel]="source()" (ngModelChange)="source.set($event)">
            <option value="">{{ 'All' | transloco }}</option>
            <option value="QuickSend">{{ sourceLabel('QuickSend') | transloco }}</option>
            <option value="BulkSend">{{ sourceLabel('BulkSend') | transloco }}</option>
            <option value="PublicApi">{{ sourceLabel('PublicApi') | transloco }}</option>
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
        @if (auth.isSuperadmin()) {
          <div>
            <label class="mb-1 block text-xs font-medium text-text-muted">{{ 'Account' | transloco }}</label>
            <select class="w-full rounded-card border border-border px-2 py-1.5 text-sm" [ngModel]="accountId()" (ngModelChange)="accountId.set($event)">
              <option [value]="null">{{ 'All' | transloco }}</option>
              @for (a of accounts(); track a.id) {
                <option [value]="a.id">{{ a.username }}</option>
              }
            </select>
          </div>
        }
      </div>
      <div class="mt-3 flex gap-2">
        <button type="button" class="rounded-card bg-primary-500 px-3 py-1.5 text-sm font-medium text-white hover:bg-primary-600" (click)="load(1)">
          {{ 'Filter' | transloco }}
        </button>
        <button type="button" class="rounded-card border border-border px-3 py-1.5 text-sm hover:bg-surface-muted" (click)="exportFile('csv')">
          {{ 'Export CSV' | transloco }}
        </button>
        <button type="button" class="rounded-card border border-border px-3 py-1.5 text-sm hover:bg-surface-muted" (click)="exportFile('excel')">
          {{ 'Export Excel' | transloco }}
        </button>
      </div>
    </div>

    <div class="rounded-card border border-border bg-surface shadow-card">
      <div class="overflow-x-auto">
        <table class="w-full whitespace-nowrap text-sm">
          @switch (activeTab().type) {
            @case ('messages') {
              <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
                <tr><th class="px-4 py-2">{{ 'Batch' | transloco }}</th><th class="px-4 py-2">{{ 'Channel' | transloco }}</th><th class="px-4 py-2">{{ 'Sender' | transloco }}</th><th class="px-4 py-2">{{ 'Receiver' | transloco }}</th><th class="px-4 py-2">{{ 'Message' | transloco }}</th><th class="px-4 py-2">{{ 'Status' | transloco }}</th><th class="px-4 py-2">{{ 'Date (UTC)' | transloco }}</th></tr>
              </thead>
              <tbody>
                @for (r of messageRows(); track r.id) {
                  <tr class="border-t border-border">
                    <td class="px-4 py-2"><code>{{ r.campaignBatchId?.slice(0, 8) }}</code></td>
                    <td class="px-4 py-2">{{ sourceLabel(r.source!) | transloco }}</td>
                    <td class="px-4 py-2">{{ r.senderNumber }}</td>
                    <td class="px-4 py-2">{{ r.receiverNumber }}</td>
                    <td class="max-w-xs truncate px-4 py-2">{{ r.messageText }}</td>
                    <td class="px-4 py-2">{{ r.status | transloco }}</td>
                    <td class="px-4 py-2">{{ r.createdAt | slice: 0 : 16 }}</td>
                  </tr>
                }
              </tbody>
            }
            @case ('daily-traffic') {
              <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
                <tr><th class="px-4 py-2">{{ 'Date' | transloco }}</th><th class="px-4 py-2">{{ 'Total' | transloco }}</th><th class="px-4 py-2">{{ 'Delivered' | transloco }}</th><th class="px-4 py-2">{{ 'Sent' | transloco }}</th><th class="px-4 py-2">{{ 'Processing' | transloco }}</th><th class="px-4 py-2">{{ 'Undelivered' | transloco }}</th><th class="px-4 py-2">{{ 'Failed' | transloco }}</th><th class="px-4 py-2">{{ 'Expired' | transloco }}</th><th class="px-4 py-2">{{ 'Delivery rate' | transloco }}</th></tr>
              </thead>
              <tbody>
                @for (r of dailyTrafficRows(); track r.date) {
                  <tr class="border-t border-border">
                    <td class="px-4 py-2">{{ r.date }}</td>
                    <td class="px-4 py-2">{{ r.total }}</td>
                    <td class="px-4 py-2">{{ r.delivered }}</td>
                    <td class="px-4 py-2">{{ r.sent }}</td>
                    <td class="px-4 py-2">{{ r.processing }}</td>
                    <td class="px-4 py-2">{{ r.undelivered }}</td>
                    <td class="px-4 py-2">{{ r.failed }}</td>
                    <td class="px-4 py-2">{{ r.expired }}</td>
                    <td class="px-4 py-2">{{ r.deliveryRate !== null && r.deliveryRate !== undefined ? (r.deliveryRate | number: '1.1-1') + '%' : '—' }}</td>
                  </tr>
                }
              </tbody>
            }
            @case ('batches') {
              <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
                <tr><th class="px-4 py-2">{{ 'Batch' | transloco }}</th><th class="px-4 py-2">{{ 'Campaign' | transloco }}</th><th class="px-4 py-2">{{ 'Channel' | transloco }}</th><th class="px-4 py-2">{{ 'Sender' | transloco }}</th><th class="px-4 py-2">{{ 'Account' | transloco }}</th><th class="px-4 py-2">{{ 'Recipients' | transloco }}</th><th class="px-4 py-2">{{ 'Delivered' | transloco }}</th><th class="px-4 py-2">{{ 'Failed' | transloco }}</th><th class="px-4 py-2">{{ 'Pending' | transloco }}</th><th class="px-4 py-2">{{ 'Cost' | transloco }}</th><th class="px-4 py-2">{{ 'Date (UTC)' | transloco }}</th></tr>
              </thead>
              <tbody>
                @for (r of batchRows(); track r.batchId) {
                  <tr class="border-t border-border">
                    <td class="px-4 py-2"><code>{{ r.batchId?.slice(0, 8) }}</code></td>
                    <td class="px-4 py-2">{{ r.campaignName }}</td>
                    <td class="px-4 py-2">{{ sourceLabel(r.source!) | transloco }}</td>
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
            }
            @case ('account-usage') {
              <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
                <tr><th class="px-4 py-2">{{ 'Account' | transloco }}</th><th class="px-4 py-2">{{ 'Full name' | transloco }}</th><th class="px-4 py-2">{{ 'Active' | transloco }}</th><th class="px-4 py-2">{{ 'Total sent' | transloco }}</th><th class="px-4 py-2">{{ 'Delivered' | transloco }}</th><th class="px-4 py-2">{{ 'Failed' | transloco }}</th><th class="px-4 py-2">{{ 'Delivery rate' | transloco }}</th><th class="px-4 py-2">{{ 'Credits consumed' | transloco }}</th><th class="px-4 py-2">{{ 'Credits added' | transloco }}</th><th class="px-4 py-2">{{ 'Balance' | transloco }}</th></tr>
              </thead>
              <tbody>
                @for (r of accountUsageRows(); track r.userId) {
                  <tr class="border-t border-border">
                    <td class="px-4 py-2">{{ r.username }}</td>
                    <td class="px-4 py-2">{{ r.fullName }}</td>
                    <td class="px-4 py-2">{{ r.isActive ? ('Yes' | transloco) : ('No' | transloco) }}</td>
                    <td class="px-4 py-2">{{ r.totalSent }}</td>
                    <td class="px-4 py-2">{{ r.delivered }}</td>
                    <td class="px-4 py-2">{{ r.failed }}</td>
                    <td class="px-4 py-2">{{ r.deliveryRate !== null && r.deliveryRate !== undefined ? (r.deliveryRate | number: '1.1-1') + '%' : '—' }}</td>
                    <td class="px-4 py-2">{{ r.creditsConsumed }}</td>
                    <td class="px-4 py-2">{{ r.creditsAdded }}</td>
                    <td class="px-4 py-2">{{ r.balance }}</td>
                  </tr>
                }
              </tbody>
            }
            @case ('transactions') {
              <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
                <tr><th class="px-4 py-2">{{ 'Date (UTC)' | transloco }}</th><th class="px-4 py-2">{{ 'Account' | transloco }}</th><th class="px-4 py-2">{{ 'Kind' | transloco }}</th><th class="px-4 py-2">{{ 'Source' | transloco }}</th><th class="px-4 py-2">{{ 'Amount' | transloco }}</th><th class="px-4 py-2">{{ 'Related batch' | transloco }}</th></tr>
              </thead>
              <tbody>
                @for (r of transactionRows(); track r.id) {
                  <tr class="border-t border-border">
                    <td class="px-4 py-2">{{ r.createdAt | slice: 0 : 16 }}</td>
                    <td class="px-4 py-2">{{ r.username }}</td>
                    <td class="px-4 py-2">{{ r.kind | transloco }}</td>
                    <td class="px-4 py-2">{{ r.source }}</td>
                    <td class="px-4 py-2">{{ r.amount }}</td>
                    <td class="px-4 py-2"><code>{{ r.relatedBatchId?.slice(0, 8) }}</code></td>
                  </tr>
                }
              </tbody>
            }
            @case ('credit-requests') {
              <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
                <tr><th class="px-4 py-2">{{ 'Date (UTC)' | transloco }}</th><th class="px-4 py-2">{{ 'Account' | transloco }}</th><th class="px-4 py-2">{{ 'Amount' | transloco }}</th><th class="px-4 py-2">{{ 'Method' | transloco }}</th><th class="px-4 py-2">{{ 'Reference' | transloco }}</th><th class="px-4 py-2">{{ 'Status' | transloco }}</th><th class="px-4 py-2">{{ 'Reviewed' | transloco }}</th></tr>
              </thead>
              <tbody>
                @for (r of creditRequestRows(); track r.id) {
                  <tr class="border-t border-border">
                    <td class="px-4 py-2">{{ r.createdAt | slice: 0 : 16 }}</td>
                    <td class="px-4 py-2">{{ r.username }}</td>
                    <td class="px-4 py-2">{{ r.amount }}</td>
                    <td class="px-4 py-2">{{ r.method }}</td>
                    <td class="px-4 py-2">{{ r.transactionRef }}</td>
                    <td class="px-4 py-2">{{ r.status | transloco }}</td>
                    <td class="px-4 py-2">{{ r.reviewedAt ? (r.reviewedAt | slice: 0 : 16) : '—' }}</td>
                  </tr>
                }
              </tbody>
            }
          }
        </table>
        @if (isEmpty()) {
          <div class="px-4 py-6 text-center text-text-muted">{{ 'No data for these filters.' | transloco }}</div>
        }
      </div>
      <div class="border-t border-border px-4">
        <app-pagination [pageNumber]="page()" [pageSize]="PAGE_SIZE" [totalCount]="totalCount()" [totalPages]="totalPages()" (pageChange)="load($event)" />
      </div>
    </div>
  `,
})
export class ReportsComponent {
  private readonly reports = inject(ReportsService);
  private readonly accountsService = inject(AccountsService);
  private readonly download = inject(FileDownloadService);
  protected readonly auth = inject(AuthService);

  protected readonly PAGE_SIZE = PAGE_SIZE;
  protected readonly tabs = TABS;
  protected readonly sourceLabel = sourceLabel;
  protected readonly statuses = ['Processing', 'Sent', 'Delivered', 'Undelivered', 'Expired', 'Failed'] as const;

  protected readonly activeTab = signal(TABS[0]);
  protected readonly dateFrom = signal('');
  protected readonly dateTo = signal('');
  protected readonly source = signal('');
  protected readonly status = signal('');
  protected readonly accountId = signal<number | null>(null);
  protected readonly accounts = signal<AccountOption[]>([]);

  protected readonly page = signal(1);
  protected readonly totalCount = signal(0);
  protected readonly totalPages = signal(0);

  protected readonly messageRows = signal<Schemas['HistoryExportRowDto'][]>([]);
  protected readonly dailyTrafficRows = signal<Schemas['DailyTrafficRowDto'][]>([]);
  protected readonly batchRows = signal<Schemas['BatchReportRowDto'][]>([]);
  protected readonly accountUsageRows = signal<Schemas['AccountUsageRowDto'][]>([]);
  protected readonly transactionRows = signal<Schemas['TransactionReportRowDto'][]>([]);
  protected readonly creditRequestRows = signal<Schemas['CreditRequestRowDto'][]>([]);

  constructor() {
    if (this.auth.isSuperadmin()) {
      void this.accountsService.options().then((options) => this.accounts.set(options));
    }
    void this.load(1);
  }

  protected selectTab(tab: (typeof TABS)[number]): void {
    this.activeTab.set(tab);
    void this.load(1);
  }

  private filter(): ReportFilter {
    return {
      dateFrom: this.dateFrom() || undefined,
      dateTo: this.dateTo() || undefined,
      source: (this.source() || undefined) as ReportFilter['source'],
      status: (this.status() || undefined) as ReportFilter['status'],
      accountId: this.accountId(),
    };
  }

  protected async load(page: number): Promise<void> {
    const filter = this.filter();
    const type = this.activeTab().type;

    this.messageRows.set([]);
    this.dailyTrafficRows.set([]);
    this.batchRows.set([]);
    this.accountUsageRows.set([]);
    this.transactionRows.set([]);
    this.creditRequestRows.set([]);

    let result;
    switch (type) {
      case 'messages':
        result = await this.reports.messages(filter, page, PAGE_SIZE);
        this.messageRows.set(result.items ?? []);
        break;
      case 'daily-traffic':
        result = await this.reports.dailyTraffic(filter, page, PAGE_SIZE);
        this.dailyTrafficRows.set(result.items ?? []);
        break;
      case 'batches':
        result = await this.reports.batches(filter, page, PAGE_SIZE);
        this.batchRows.set(result.items ?? []);
        break;
      case 'account-usage':
        result = await this.reports.accountUsage(filter, page, PAGE_SIZE);
        this.accountUsageRows.set(result.items ?? []);
        break;
      case 'transactions':
        result = await this.reports.transactions(filter, page, PAGE_SIZE);
        this.transactionRows.set(result.items ?? []);
        break;
      case 'credit-requests':
        result = await this.reports.creditRequests(filter, page, PAGE_SIZE);
        this.creditRequestRows.set(result.items ?? []);
        break;
    }

    this.page.set(result.pageNumber ?? page);
    this.totalCount.set(result.totalCount ?? 0);
    this.totalPages.set(result.totalPages ?? 0);
  }

  protected isEmpty(): boolean {
    return this.totalCount() === 0;
  }

  protected async exportFile(format: 'csv' | 'excel'): Promise<void> {
    const filter = this.filter();
    await this.download.download(`/api/v1/reports/${this.activeTab().exportName}/export`, cleanParams({ ...filter, format }));
  }
}
