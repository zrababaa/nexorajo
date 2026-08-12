import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import type { ApiErrorResponse } from '../../core/api/api.types';
import { FlashService } from '../../shared/flash/flash.service';
import { SendingWindowService } from './sending-window.service';

@Component({
  selector: 'app-sending-window',
  standalone: true,
  imports: [FormsModule, TranslocoPipe],
  template: `
    <h1 class="mb-4 text-xl font-semibold">{{ 'Sending Window' | transloco }}</h1>

    <div class="max-w-xl rounded-card border border-border bg-surface p-4 shadow-card">
      <p class="mb-4 text-sm text-text-muted">
        {{
          'Restrict Bulk Send to a daily time window. Outside it, Bulk Send is blocked for every account. Quick Send is never affected.'
            | transloco
        }}
      </p>

      @if (errorMessage()) {
        <p class="mb-3 text-sm text-danger" role="alert">{{ errorMessage() }}</p>
      }

      <label class="mb-4 inline-flex items-center gap-2">
        <input type="checkbox" [checked]="isEnabled()" (change)="isEnabled.set($any($event.target).checked)" />
        {{ 'Restrict Bulk Send to this window' | transloco }}
      </label>

      <div class="mb-4 flex flex-wrap items-end gap-3">
        <div>
          <label class="mb-1 block text-sm font-medium">{{ 'Start time' | transloco }}</label>
          <input type="time" class="rounded-card border border-border px-3 py-2 text-sm" [ngModel]="startTime()" (ngModelChange)="startTime.set($event)" />
        </div>
        <div>
          <label class="mb-1 block text-sm font-medium">{{ 'End time' | transloco }}</label>
          <input type="time" class="rounded-card border border-border px-3 py-2 text-sm" [ngModel]="endTime()" (ngModelChange)="endTime.set($event)" />
        </div>
      </div>
      <div class="mb-4 text-xs text-text-muted">
        {{ 'A window where the end time is earlier than the start time wraps past midnight (e.g. 21:00 to 09:00).' | transloco }}
      </div>

      <button
        type="button"
        class="rounded-card bg-primary-500 px-4 py-2 text-sm font-medium text-white hover:bg-primary-600 disabled:opacity-60"
        [disabled]="saving()"
        (click)="save()"
      >
        {{ 'Save' | transloco }}
      </button>
    </div>
  `,
})
export class SendingWindowComponent {
  private readonly sendingWindow = inject(SendingWindowService);
  private readonly flash = inject(FlashService);

  protected readonly isEnabled = signal(false);
  protected readonly startTime = signal('21:00');
  protected readonly endTime = signal('09:00');
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  constructor() {
    void this.load();
  }

  private async load(): Promise<void> {
    const window = await this.sendingWindow.get();
    this.isEnabled.set(window.isEnabled);
    this.startTime.set(window.startTime.slice(0, 5));
    this.endTime.set(window.endTime.slice(0, 5));
  }

  protected async save(): Promise<void> {
    this.errorMessage.set(null);
    this.saving.set(true);
    try {
      const window = await this.sendingWindow.set(this.isEnabled(), this.startTime(), this.endTime());
      this.isEnabled.set(window.isEnabled);
      this.startTime.set(window.startTime.slice(0, 5));
      this.endTime.set(window.endTime.slice(0, 5));
      this.flash.success('Sending window updated.');
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.errorMessage.set((error.error as ApiErrorResponse)?.message ?? 'Unable to update the sending window.');
      }
    } finally {
      this.saving.set(false);
    }
  }
}
