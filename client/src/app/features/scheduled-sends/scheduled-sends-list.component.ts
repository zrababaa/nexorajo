import { SlicePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import type { ApiErrorResponse } from '../../core/api/api.types';
import { FlashService } from '../../shared/flash/flash.service';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { ScheduledSendsService, type ScheduledSend } from './scheduled-sends.service';

const PAGE_SIZE = 10;

@Component({
  selector: 'app-scheduled-sends-list',
  standalone: true,
  imports: [SlicePipe, TranslocoPipe, PaginationComponent],
  template: `
    <h1 class="mb-4 text-xl font-semibold">{{ 'Scheduled Sends' | transloco }}</h1>

    <div class="rounded-card border border-border bg-surface shadow-card">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
            <tr>
              <th class="px-4 py-2">{{ 'Campaign' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Message' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Scheduled for' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Status' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Detail' | transloco }}</th>
              <th class="px-4 py-2 text-right">{{ 'Actions' | transloco }}</th>
            </tr>
          </thead>
          <tbody>
            @if (items().length === 0) {
              <tr><td colspan="6" class="px-4 py-6 text-center text-text-muted">{{ 'No scheduled sends yet.' | transloco }}</td></tr>
            }
            @for (s of items(); track s.id) {
              <tr class="border-t border-border">
                <td class="px-4 py-2">{{ s.campaignName }}</td>
                <td class="px-4 py-2">{{ s.message | slice: 0 : 40 }}</td>
                <td class="px-4 py-2">{{ s.scheduledAtUtc | slice: 0 : 16 }}</td>
                <td class="px-4 py-2">{{ s.status | transloco }}</td>
                <td class="px-4 py-2">
                  <code>{{ s.batchId }}</code>
                  <span class="text-danger">{{ s.errorMessage }}</span>
                </td>
                <td class="px-4 py-2 text-right">
                  @if (s.status === 'Pending') {
                    <button type="button" class="text-danger hover:underline" (click)="cancel(s)">{{ 'Cancel' | transloco }}</button>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
      <div class="border-t border-border px-4">
        <app-pagination
          [pageNumber]="page()"
          [pageSize]="PAGE_SIZE"
          [totalCount]="totalCount()"
          [totalPages]="totalPages()"
          (pageChange)="load($event)"
        />
      </div>
    </div>
  `,
})
export class ScheduledSendsListComponent {
  private readonly scheduledSends = inject(ScheduledSendsService);
  private readonly flash = inject(FlashService);
  private readonly transloco = inject(TranslocoService);

  protected readonly PAGE_SIZE = PAGE_SIZE;
  protected readonly items = signal<ScheduledSend[]>([]);
  protected readonly page = signal(1);
  protected readonly totalCount = signal(0);
  protected readonly totalPages = signal(0);

  constructor() {
    void this.load(1);
  }

  protected async load(page: number): Promise<void> {
    const result = await this.scheduledSends.list(page, PAGE_SIZE);
    this.items.set(result.items ?? []);
    this.page.set(result.pageNumber ?? page);
    this.totalCount.set(result.totalCount ?? 0);
    this.totalPages.set(result.totalPages ?? 0);
  }

  protected async cancel(item: ScheduledSend): Promise<void> {
    if (!confirm(this.transloco.translate('Cancel this scheduled send?'))) {
      return;
    }
    try {
      await this.scheduledSends.cancel(item.id);
      this.flash.success('Scheduled send cancelled.');
      await this.load(this.page());
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.flash.error((error.error as ApiErrorResponse)?.message ?? 'Unable to cancel this scheduled send.');
      }
    }
  }
}
