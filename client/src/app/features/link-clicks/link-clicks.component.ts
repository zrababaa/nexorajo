import { SlicePipe } from '@angular/common';
import { Component, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { LinkClicksService, type BatchLinkStats, type LinkClickRow } from './link-clicks.service';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-link-clicks',
  standalone: true,
  imports: [RouterLink, SlicePipe, TranslocoPipe, PaginationComponent],
  template: `
    <div class="mb-4 flex items-center justify-between">
      <h1 class="text-xl font-semibold">{{ 'Clicks' | transloco }} — <code>{{ batchId() }}</code></h1>
      <a routerLink="/reports" class="text-sm text-primary-700 hover:underline">{{ 'Back' | transloco }}</a>
    </div>

    @if (error()) {
      <p class="text-sm text-danger" role="alert">{{ error() }}</p>
    } @else if (stats()) {
      @let s = stats()!;
      <div class="mb-4 grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        @for (link of s.links; track link.token) {
          <div class="rounded-card border border-border bg-surface p-3 shadow-card">
            <div class="truncate text-sm font-medium" [title]="link.destinationUrl">{{ link.destinationUrl }}</div>
            <div class="mt-2 text-2xl font-semibold">{{ link.clickCount }}</div>
            <div class="text-xs text-text-muted">{{ 'Total clicks' | transloco }}</div>
            <div class="mt-2 text-xs text-text-muted">
              {{ 'First click' | transloco }}: {{ link.firstClickedAt ? (link.firstClickedAt | slice: 0 : 16) : '—' }}<br />
              {{ 'Last click' | transloco }}: {{ link.lastClickedAt ? (link.lastClickedAt | slice: 0 : 16) : '—' }}
            </div>
          </div>
        }
      </div>

      <div class="rounded-card border border-border bg-surface shadow-card">
        <div class="overflow-x-auto">
          <table class="w-full whitespace-nowrap text-sm">
            <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
              <tr>
                <th class="px-4 py-2">{{ 'Date (UTC)' | transloco }}</th>
                <th class="px-4 py-2">{{ 'Link' | transloco }}</th>
                <th class="px-4 py-2">{{ 'IP address' | transloco }}</th>
                <th class="px-4 py-2">{{ 'User agent' | transloco }}</th>
              </tr>
            </thead>
            <tbody>
              @for (c of clickRows(); track $index) {
                <tr class="border-t border-border">
                  <td class="px-4 py-2">{{ c.clickedAt | slice: 0 : 16 }}</td>
                  <td class="px-4 py-2"><code>{{ c.token }}</code></td>
                  <td class="px-4 py-2">{{ c.ipAddress }}</td>
                  <td class="max-w-md truncate px-4 py-2">{{ c.userAgent }}</td>
                </tr>
              }
            </tbody>
          </table>
          @if (clickRows().length === 0) {
            <div class="px-4 py-6 text-center text-text-muted">{{ 'No clicks yet.' | transloco }}</div>
          }
        </div>
        <div class="border-t border-border px-4">
          <app-pagination
            [pageNumber]="page()"
            [pageSize]="PAGE_SIZE"
            [totalCount]="totalCount()"
            [totalPages]="totalPages()"
            (pageChange)="loadClicks($event)"
          />
        </div>
      </div>
    } @else {
      <p class="text-sm text-text-muted">Loading…</p>
    }
  `,
})
export class LinkClicksComponent {
  readonly batchId = input<string>('');

  private readonly linkClicks = inject(LinkClicksService);

  protected readonly PAGE_SIZE = PAGE_SIZE;
  protected readonly stats = signal<BatchLinkStats | null>(null);
  protected readonly error = signal<string | null>(null);

  protected readonly clickRows = signal<LinkClickRow[]>([]);
  protected readonly page = signal(1);
  protected readonly totalCount = signal(0);
  protected readonly totalPages = signal(0);

  constructor() {
    this.linkClicks
      .stats(this.batchId())
      .then((s) => {
        this.stats.set(s);
        return this.loadClicks(1);
      })
      .catch(() => this.error.set('No tracked links for this batch.'));
  }

  protected async loadClicks(page: number): Promise<void> {
    const result = await this.linkClicks.clicks(this.batchId(), page, PAGE_SIZE);
    this.clickRows.set(result.items ?? []);
    this.page.set(result.pageNumber ?? page);
    this.totalCount.set(result.totalCount ?? 0);
    this.totalPages.set(result.totalPages ?? 0);
  }
}
