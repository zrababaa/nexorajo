import { SlicePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import type { ApiErrorResponse } from '../../core/api/api.types';
import { FlashService } from '../../shared/flash/flash.service';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { SmsTemplatesService, type SmsTemplateListItem } from './sms-templates.service';

const PAGE_SIZE = 10;

@Component({
  selector: 'app-sms-templates-list',
  standalone: true,
  imports: [RouterLink, SlicePipe, TranslocoPipe, PaginationComponent],
  template: `
    <div class="mb-4 flex items-center justify-between">
      <h1 class="text-xl font-semibold">{{ 'SMS Templates' | transloco }}</h1>
      <a
        routerLink="/sms-templates/new"
        class="rounded-card bg-primary-500 px-3 py-1.5 text-sm font-medium text-white hover:bg-primary-600"
      >
        + {{ 'New template' | transloco }}
      </a>
    </div>

    <p class="mb-4 text-sm text-text-muted">
      {{
        'Write a message once with [Name], [Date], or any other [Placeholder], and it gets filled in for each recipient when you send it against a Campaign.'
          | transloco
      }}
    </p>

    <div class="rounded-card border border-border bg-surface shadow-card">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
            <tr>
              <th class="px-4 py-2">{{ 'Name' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Body' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Placeholders' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Created' | transloco }}</th>
              <th class="px-4 py-2 text-right">{{ 'Actions' | transloco }}</th>
            </tr>
          </thead>
          <tbody>
            @if (items().length === 0) {
              <tr><td colspan="5" class="px-4 py-6 text-center text-text-muted">{{ 'No SMS templates yet.' | transloco }}</td></tr>
            }
            @for (t of items(); track t.id) {
              <tr class="border-t border-border">
                <td class="px-4 py-2">{{ t.name }}</td>
                <td class="max-w-xs truncate px-4 py-2 text-text-muted" [title]="t.body ?? ''">{{ t.body | slice: 0 : 60 }}</td>
                <td class="px-4 py-2">
                  @for (p of t.placeholders; track p) {
                    <code class="mr-1 rounded bg-surface-muted px-1.5 py-0.5 text-xs">[{{ p }}]</code>
                  }
                </td>
                <td class="px-4 py-2">{{ t.createdAt | slice: 0 : 16 }}</td>
                <td class="px-4 py-2 text-right">
                  <a [routerLink]="['/sms-templates', t.id, 'edit']" class="text-primary-600 hover:underline">{{ 'Edit' | transloco }}</a>
                  <button type="button" class="ml-3 text-danger hover:underline" (click)="remove(t)">{{ 'Delete' | transloco }}</button>
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
export class SmsTemplatesListComponent {
  private readonly templates = inject(SmsTemplatesService);
  private readonly flash = inject(FlashService);
  private readonly transloco = inject(TranslocoService);

  protected readonly PAGE_SIZE = PAGE_SIZE;
  protected readonly items = signal<SmsTemplateListItem[]>([]);
  protected readonly page = signal(1);
  protected readonly totalCount = signal(0);
  protected readonly totalPages = signal(0);

  constructor() {
    void this.load(1);
  }

  protected async load(page: number): Promise<void> {
    const result = await this.templates.list(page, PAGE_SIZE);
    this.items.set(result.items ?? []);
    this.page.set(result.pageNumber ?? page);
    this.totalCount.set(result.totalCount ?? 0);
    this.totalPages.set(result.totalPages ?? 0);
  }

  protected async remove(template: SmsTemplateListItem): Promise<void> {
    if (!confirm(this.transloco.translate('Delete this SMS template?'))) {
      return;
    }
    try {
      await this.templates.delete(template.id!);
      this.flash.success('SMS template deleted successfully.');
      await this.load(this.page());
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.flash.error((error.error as ApiErrorResponse)?.message ?? 'Unable to delete this SMS template.');
      }
    }
  }
}
