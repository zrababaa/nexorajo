import { SlicePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import type { ApiErrorResponse } from '../../core/api/api.types';
import { FlashService } from '../../shared/flash/flash.service';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { CampaignsService, type CampaignListItem } from './campaigns.service';

const PAGE_SIZE = 10;

@Component({
  selector: 'app-campaigns-list',
  standalone: true,
  imports: [RouterLink, SlicePipe, TranslocoPipe, PaginationComponent],
  template: `
    <div class="mb-4 flex items-center justify-between">
      <h1 class="text-xl font-semibold">{{ 'Campaigns' | transloco }}</h1>
      <a routerLink="/campaigns/new" class="rounded-card bg-primary-500 px-3 py-1.5 text-sm font-medium text-white hover:bg-primary-600">
        + {{ 'New campaign' | transloco }}
      </a>
    </div>

    <div class="rounded-card border border-border bg-surface shadow-card">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
            <tr>
              <th class="px-4 py-2">{{ 'Name' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Code' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Source' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Recipients' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Created' | transloco }}</th>
              <th class="px-4 py-2 text-right">{{ 'Actions' | transloco }}</th>
            </tr>
          </thead>
          <tbody>
            @if (items().length === 0) {
              <tr><td colspan="6" class="px-4 py-6 text-center text-text-muted">{{ 'No campaigns yet.' | transloco }}</td></tr>
            }
            @for (c of items(); track c.id) {
              <tr class="border-t border-border">
                <td class="px-4 py-2">{{ c.name }}</td>
                <td class="px-4 py-2"><code>{{ c.externalCampaignCode }}</code></td>
                <td class="px-4 py-2">{{ c.sourceType }}</td>
                <td class="px-4 py-2">{{ c.recipientCount }}</td>
                <td class="px-4 py-2">{{ c.createdAt | slice: 0 : 16 }}</td>
                <td class="px-4 py-2 text-right">
                  <a [routerLink]="['/campaigns', c.id, 'edit']" class="text-primary-600 hover:underline">{{ 'Edit' | transloco }}</a>
                  <button type="button" class="ml-3 text-danger hover:underline" (click)="remove(c)">{{ 'Delete' | transloco }}</button>
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
export class CampaignsListComponent {
  private readonly campaigns = inject(CampaignsService);
  private readonly flash = inject(FlashService);
  private readonly transloco = inject(TranslocoService);

  protected readonly PAGE_SIZE = PAGE_SIZE;
  protected readonly items = signal<CampaignListItem[]>([]);
  protected readonly page = signal(1);
  protected readonly totalCount = signal(0);
  protected readonly totalPages = signal(0);

  constructor() {
    void this.load(1);
  }

  protected async load(page: number): Promise<void> {
    const result = await this.campaigns.list(page, PAGE_SIZE);
    this.items.set(result.items ?? []);
    this.page.set(result.pageNumber ?? page);
    this.totalCount.set(result.totalCount ?? 0);
    this.totalPages.set(result.totalPages ?? 0);
  }

  protected async remove(campaign: CampaignListItem): Promise<void> {
    if (!confirm(this.transloco.translate('Delete this campaign?'))) {
      return;
    }
    try {
      await this.campaigns.delete(campaign.id!);
      this.flash.success('Campaign deleted successfully.');
      await this.load(this.page());
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.flash.error((error.error as ApiErrorResponse)?.message ?? 'Unable to delete this campaign.');
      }
    }
  }
}
