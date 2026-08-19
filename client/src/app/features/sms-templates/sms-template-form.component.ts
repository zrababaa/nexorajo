import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import type { ApiErrorResponse } from '../../core/api/api.types';
import { FlashService } from '../../shared/flash/flash.service';
import { SmsTemplatesService } from './sms-templates.service';
import { extractPlaceholders } from './template-placeholders';

@Component({
  selector: 'app-sms-template-form',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoPipe],
  template: `
    <h1 class="mb-4 text-xl font-semibold">{{ id() ? ('Edit SMS template' | transloco) : ('New SMS template' | transloco) }}</h1>

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
        <label for="body" class="mb-1 block text-sm font-medium">{{ 'Message' | transloco }}</label>
        <textarea
          id="body"
          rows="5"
          placeholder="Hello [Name], please come at this [Date]"
          class="w-full rounded-card border border-border px-3 py-2 text-sm"
          [ngModel]="body()"
          (ngModelChange)="body.set($event)"
        ></textarea>
        <div class="mt-1 text-xs text-text-muted">
          {{ 'Wrap any word in square brackets to make it a placeholder, e.g. [Name] or [Date].' | transloco }}
        </div>
      </div>

      @if (placeholders().length > 0) {
        <div class="mb-4">
          <div class="mb-1 text-xs font-medium uppercase tracking-wide text-text-muted">{{ 'Placeholders found' | transloco }}</div>
          <div class="flex flex-wrap gap-1">
            @for (p of placeholders(); track p) {
              <code class="rounded bg-surface-muted px-1.5 py-0.5 text-xs">[{{ p }}]</code>
            }
          </div>
          <div class="mt-1 text-xs text-text-muted">
            {{
              '[Name], [CompanyName], [Email], [Phone], and [Address] are filled in automatically per recipient from your Customers when sent against a Campaign. Any other placeholder must be given a value when you send.'
                | transloco
            }}
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
        <a routerLink="/sms-templates" class="rounded-card border border-border px-4 py-2 text-sm hover:bg-surface-muted">
          {{ 'Cancel' | transloco }}
        </a>
      </div>
    </div>
  `,
})
export class SmsTemplateFormComponent {
  readonly id = input<string>();

  private readonly templates = inject(SmsTemplatesService);
  private readonly flash = inject(FlashService);
  private readonly router = inject(Router);

  protected readonly name = signal('');
  protected readonly body = signal('');
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly placeholders = computed(() => extractPlaceholders(this.body()));

  constructor() {
    const id = this.id();
    if (id) {
      void this.templates.getById(Number(id)).then((t) => {
        this.name.set(t.name ?? '');
        this.body.set(t.body ?? '');
      });
    }
  }

  protected async save(): Promise<void> {
    this.errorMessage.set(null);

    if (!this.name().trim()) {
      this.errorMessage.set('Enter a name for the template.');
      return;
    }
    if (!this.body().trim()) {
      this.errorMessage.set('Enter the message text.');
      return;
    }

    this.saving.set(true);
    try {
      const id = this.id();
      if (id) {
        await this.templates.update(Number(id), this.name().trim(), this.body());
      } else {
        await this.templates.create(this.name().trim(), this.body());
      }

      this.flash.success(id ? 'SMS template updated successfully.' : 'SMS template added successfully.');
      await this.router.navigateByUrl('/sms-templates');
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.errorMessage.set((error.error as ApiErrorResponse)?.message ?? 'Unable to save this SMS template.');
      }
    } finally {
      this.saving.set(false);
    }
  }
}
