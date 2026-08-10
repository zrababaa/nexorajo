import { SlicePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import type { ApiErrorResponse } from '../../core/api/api.types';
import { FlashService } from '../../shared/flash/flash.service';
import { SpamKeywordsService, type SpamKeyword, type SpamKeywordType } from './spam-keywords.service';

const TYPES: SpamKeywordType[] = ['Include', 'Exclude', 'Url'];

@Component({
  selector: 'app-spam-keywords',
  standalone: true,
  imports: [FormsModule, SlicePipe, TranslocoPipe],
  template: `
    <h1 class="mb-4 text-xl font-semibold">{{ 'Content Filter' | transloco }}</h1>

    <div class="mb-4 rounded-card border border-border bg-surface p-4 shadow-card">
      @if (errorMessage()) {
        <p class="mb-3 text-sm text-danger" role="alert">{{ errorMessage() }}</p>
      }
      <div class="flex flex-wrap items-end gap-3">
        <div class="min-w-0 flex-1">
          <label class="mb-1 block text-sm font-medium">{{ 'Keyword' | transloco }}</label>
          <input class="w-full rounded-card border border-border px-3 py-2 text-sm" [ngModel]="keyword()" (ngModelChange)="keyword.set($event)" />
        </div>
        <div>
          <label class="mb-1 block text-sm font-medium">{{ 'Type' | transloco }}</label>
          <select class="rounded-card border border-border px-3 py-2 text-sm" [ngModel]="keywordType()" (ngModelChange)="keywordType.set($event)">
            @for (t of types; track t) {
              <option [value]="t">{{ t | transloco }}</option>
            }
          </select>
        </div>
        <button
          type="button"
          class="rounded-card bg-primary-500 px-4 py-2 text-sm font-medium text-white hover:bg-primary-600 disabled:opacity-60"
          [disabled]="saving()"
          (click)="add()"
        >
          {{ 'Add rule' | transloco }}
        </button>
      </div>
    </div>

    <div class="rounded-card border border-border bg-surface shadow-card">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
            <tr>
              <th class="px-4 py-2">{{ 'Keyword' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Type' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Enabled' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Created' | transloco }}</th>
              <th class="px-4 py-2 text-right">{{ 'Actions' | transloco }}</th>
            </tr>
          </thead>
          <tbody>
            @if (items().length === 0) {
              <tr><td colspan="5" class="px-4 py-6 text-center text-text-muted">{{ 'No rules configured yet.' | transloco }}</td></tr>
            }
            @for (k of items(); track k.id) {
              <tr class="border-t border-border">
                <td class="px-4 py-2">{{ k.keyword }}</td>
                <td class="px-4 py-2">{{ k.keywordType | transloco }}</td>
                <td class="px-4 py-2">
                  <label class="inline-flex items-center gap-2">
                    <input type="checkbox" [checked]="k.isEnabled" (change)="toggle(k, $event)" />
                    {{ (k.isEnabled ? 'Yes' : 'No') | transloco }}
                  </label>
                </td>
                <td class="px-4 py-2">{{ k.createdAt | slice: 0 : 16 }}</td>
                <td class="px-4 py-2 text-right">
                  <button type="button" class="text-danger hover:underline" (click)="remove(k)">{{ 'Delete' | transloco }}</button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `,
})
export class SpamKeywordsComponent {
  private readonly keywords = inject(SpamKeywordsService);
  private readonly flash = inject(FlashService);
  private readonly transloco = inject(TranslocoService);

  protected readonly types = TYPES;
  protected readonly items = signal<SpamKeyword[]>([]);
  protected readonly keyword = signal('');
  protected readonly keywordType = signal<SpamKeywordType>('Include');
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    this.items.set(await this.keywords.list());
  }

  protected async add(): Promise<void> {
    this.errorMessage.set(null);
    if (!this.keyword().trim()) {
      this.errorMessage.set('Enter a keyword.');
      return;
    }

    this.saving.set(true);
    try {
      await this.keywords.create(this.keyword().trim(), this.keywordType());
      this.flash.success('Rule added.');
      this.keyword.set('');
      await this.load();
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.errorMessage.set((error.error as ApiErrorResponse)?.message ?? 'Unable to add this rule.');
      }
    } finally {
      this.saving.set(false);
    }
  }

  protected async toggle(item: SpamKeyword, event: Event): Promise<void> {
    const enabled = (event.target as HTMLInputElement).checked;
    try {
      await this.keywords.setEnabled(item.id!, enabled);
      item.isEnabled = enabled;
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.flash.error((error.error as ApiErrorResponse)?.message ?? 'Unable to update this rule.');
      }
      await this.load();
    }
  }

  protected async remove(item: SpamKeyword): Promise<void> {
    if (!confirm(this.transloco.translate('Delete this rule?'))) {
      return;
    }
    try {
      await this.keywords.delete(item.id!);
      this.flash.success('Rule deleted.');
      await this.load();
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.flash.error((error.error as ApiErrorResponse)?.message ?? 'Unable to delete this rule.');
      }
    }
  }
}
