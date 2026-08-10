import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import type { ApiErrorResponse } from '../../core/api/api.types';
import { FlashService } from '../../shared/flash/flash.service';
import { CampaignsService } from './campaigns.service';

@Component({
  selector: 'app-campaign-form',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoPipe],
  template: `
    <h1 class="mb-4 text-xl font-semibold">{{ id() ? ('Edit campaign' | transloco) : ('New campaign' | transloco) }}</h1>

    <div class="max-w-xl rounded-card border border-border bg-surface p-4 shadow-card">
      @if (errorMessage()) {
        <p class="mb-3 text-sm text-danger" role="alert">{{ errorMessage() }}</p>
      }

      <div class="mb-3">
        <label for="name" class="mb-1 block text-sm font-medium">{{ 'Name' | transloco }}</label>
        <input
          id="name"
          class="w-full rounded-card border border-border px-3 py-2 text-sm"
          [ngModel]="name()"
          (ngModelChange)="name.set($event)"
        />
      </div>

      <div class="mb-3">
        <label for="code" class="mb-1 block text-sm font-medium">{{ 'External Campaign Code' | transloco }}</label>
        <input
          id="code"
          class="w-full rounded-card border border-border px-3 py-2 text-sm disabled:bg-surface-muted"
          [ngModel]="code()"
          (ngModelChange)="code.set($event)"
          [disabled]="!!id()"
        />
      </div>

      <div class="mb-3">
        <label for="pastedNumbers" class="mb-1 block text-sm font-medium">{{ 'Numbers' | transloco }}</label>
        <textarea
          id="pastedNumbers"
          rows="5"
          placeholder="9715xxxxxxxx, 9715xxxxxxxx, ..."
          class="w-full rounded-card border border-border px-3 py-2 text-sm"
          [ngModel]="pastedNumbers()"
          (ngModelChange)="pastedNumbers.set($event)"
        ></textarea>
      </div>

      @if (!id()) {
        <div class="mb-4">
          <label for="csvFile" class="mb-1 block text-sm font-medium">{{ 'Import from file' | transloco }}</label>
          <input id="csvFile" type="file" accept=".csv,.txt,.xlsx" class="w-full text-sm" (change)="onFileSelected($event)" />
          <div class="mt-1 text-xs text-text-muted">
            {{ 'Provide numbers either pasted above or via file upload here, not both.' | transloco }}
          </div>
        </div>
      }

      <div class="flex gap-2">
        <button
          type="button"
          class="rounded-card bg-primary-500 px-4 py-2 text-sm font-medium text-white hover:bg-primary-600 disabled:opacity-60"
          [disabled]="saving()"
          (click)="save()"
        >
          {{ 'Save' | transloco }}
        </button>
        <a routerLink="/campaigns" class="rounded-card border border-border px-4 py-2 text-sm hover:bg-surface-muted">
          {{ 'Cancel' | transloco }}
        </a>
      </div>
    </div>
  `,
})
export class CampaignFormComponent {
  readonly id = input<string>();

  private readonly campaigns = inject(CampaignsService);
  private readonly flash = inject(FlashService);
  private readonly router = inject(Router);

  protected readonly name = signal('');
  protected readonly code = signal('');
  protected readonly pastedNumbers = signal('');
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  private file: File | null = null;

  constructor() {
    const id = this.id();
    if (id) {
      void this.campaigns.getById(Number(id)).then((c) => {
        this.name.set(c.name ?? '');
        this.code.set(c.externalCampaignCode ?? '');
        this.pastedNumbers.set(c.numbers ?? '');
      });
    }
  }

  protected onFileSelected(event: Event): void {
    this.file = (event.target as HTMLInputElement).files?.[0] ?? null;
  }

  protected async save(): Promise<void> {
    this.errorMessage.set(null);

    const hasPasted = this.pastedNumbers().trim().length > 0;
    const hasFile = this.file !== null;

    if (hasPasted && hasFile) {
      this.errorMessage.set('Please provide numbers either pasted or via file upload, not both.');
      return;
    }
    if (!hasPasted && !hasFile) {
      this.errorMessage.set('Please provide numbers either pasted or via file upload.');
      return;
    }
    if (!this.name().trim()) {
      this.errorMessage.set('Enter a name for the campaign.');
      return;
    }

    this.saving.set(true);
    try {
      const id = this.id();
      if (id) {
        await this.campaigns.update(Number(id), this.name().trim(), this.pastedNumbers());
      } else if (hasFile) {
        await this.campaigns.import(this.name().trim(), this.file!, this.code().trim() || undefined);
      } else {
        await this.campaigns.create(this.name().trim(), this.pastedNumbers(), this.code().trim() || undefined);
      }

      this.flash.success(id ? 'Campaign updated successfully.' : 'Campaign added successfully.');
      await this.router.navigateByUrl('/campaigns');
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.errorMessage.set((error.error as ApiErrorResponse)?.message ?? 'Unable to save this campaign.');
      }
    } finally {
      this.saving.set(false);
    }
  }
}
