import { SlicePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthService } from '../../core/auth/auth.service';
import { FlashService } from '../../shared/flash/flash.service';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { TrackingLinksService, type TrackedLinkRow } from './tracking-links.service';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-tracking-links',
  standalone: true,
  imports: [RouterLink, SlicePipe, TranslocoPipe, PaginationComponent],
  template: `
    <h1 class="mb-4 text-xl font-semibold">{{ 'Tracking Links' | transloco }}</h1>

    <p class="mb-4 text-sm text-text-muted">
      {{ 'Every link found in a sent message is rewritten to a short tracking URL. Each visit is counted here.' | transloco }}
    </p>

    <div class="rounded-card border border-border bg-surface shadow-card">
      <div class="overflow-x-auto">
        <table class="w-full whitespace-nowrap text-sm">
          <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
            <tr>
              <th class="px-4 py-2">{{ 'Destination' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Short link' | transloco }}</th>
              @if (auth.isSuperadmin()) {
                <th class="px-4 py-2">{{ 'Owner' | transloco }}</th>
              }
              <th class="px-4 py-2">{{ 'Batch' | transloco }}</th>
              <th class="px-4 py-2 text-right">{{ 'Clicks' | transloco }}</th>
              <th class="px-4 py-2">{{ 'First click' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Last click' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Created' | transloco }}</th>
            </tr>
          </thead>
          <tbody>
            @if (rows().length === 0) {
              <tr>
                <td [attr.colspan]="auth.isSuperadmin() ? 8 : 7" class="px-4 py-6 text-center text-text-muted">
                  {{ 'No tracking links yet.' | transloco }}
                </td>
              </tr>
            }
            @for (r of rows(); track r.token) {
              <tr class="border-t border-border">
                <td class="max-w-xs truncate px-4 py-2" [title]="r.destinationUrl">
                  <a [href]="r.destinationUrl" target="_blank" rel="noopener" class="text-primary-700 hover:underline">
                    {{ r.destinationUrl }}
                  </a>
                </td>
                <td class="px-4 py-2">
                  <button
                    type="button"
                    class="inline-flex items-center gap-1 text-primary-700 hover:underline"
                    (click)="copy(r.shortUrl)"
                    [title]="'Copy' | transloco"
                  >
                    <code>{{ r.shortUrl }}</code>
                  </button>
                </td>
                @if (auth.isSuperadmin()) {
                  <td class="px-4 py-2">{{ r.ownerUsername }}</td>
                }
                <td class="px-4 py-2">
                  <a [routerLink]="['/link-clicks', r.batchId]" class="text-primary-700 hover:underline">
                    <code>{{ r.batchId | slice: 0 : 8 }}</code>
                  </a>
                </td>
                <td class="px-4 py-2 text-right font-medium">{{ r.clickCount }}</td>
                <td class="px-4 py-2">{{ r.firstClickedAt ? (r.firstClickedAt | slice: 0 : 16) : '—' }}</td>
                <td class="px-4 py-2">{{ r.lastClickedAt ? (r.lastClickedAt | slice: 0 : 16) : '—' }}</td>
                <td class="px-4 py-2">{{ r.createdAt | slice: 0 : 16 }}</td>
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
export class TrackingLinksComponent {
  protected readonly auth = inject(AuthService);
  private readonly trackingLinks = inject(TrackingLinksService);
  private readonly flash = inject(FlashService);

  protected readonly PAGE_SIZE = PAGE_SIZE;
  protected readonly rows = signal<TrackedLinkRow[]>([]);
  protected readonly page = signal(1);
  protected readonly totalCount = signal(0);
  protected readonly totalPages = signal(0);
  protected readonly isEmpty = computed(() => this.totalCount() === 0);

  constructor() {
    void this.load(1);
  }

  protected async load(page: number): Promise<void> {
    const result = await this.trackingLinks.list(page, PAGE_SIZE);
    this.rows.set(result.items ?? []);
    this.page.set(result.pageNumber ?? page);
    this.totalCount.set(result.totalCount ?? 0);
    this.totalPages.set(result.totalPages ?? 0);
  }

  protected async copy(url: string | null | undefined): Promise<void> {
    if (!url) {
      return;
    }
    const absolute = url.startsWith('http') ? url : `${location.origin}${url}`;
    try {
      await navigator.clipboard.writeText(absolute);
      this.flash.success('Link copied.');
    } catch {
      this.flash.error('Could not copy the link.');
    }
  }
}
