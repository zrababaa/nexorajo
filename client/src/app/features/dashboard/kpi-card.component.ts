import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

export type KpiVariant = 'neutral' | 'info' | 'success' | 'warning';

const ICON_TINT: Record<KpiVariant, string> = {
  neutral: 'bg-primary-100 text-primary-700',
  info: 'bg-primary-100 text-primary-700',
  success: 'bg-success/15 text-success',
  warning: 'bg-warning/15 text-warning',
};

@Component({
  selector: 'app-kpi-card',
  standalone: true,
  imports: [RouterLink],
  template: `
    @if (routerLink()) {
      <a
        [routerLink]="routerLink()"
        class="flex items-center gap-3 rounded-card border border-border bg-surface p-4 shadow-card transition hover:-translate-y-0.5 hover:border-primary-200 hover:shadow-card-md"
      >
        <span class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full" [class]="tintClass()">
          <ng-content />
        </span>
        <div>
          <div class="text-xs font-medium uppercase tracking-wide text-text-muted">{{ label() }}</div>
          <div class="mt-0.5 text-2xl font-bold">{{ value() }}</div>
        </div>
      </a>
    } @else {
      <div class="flex items-center gap-3 rounded-card border border-border bg-surface p-4 shadow-card">
        <span class="flex h-10 w-10 shrink-0 items-center justify-center rounded-full" [class]="tintClass()">
          <ng-content />
        </span>
        <div>
          <div class="text-xs font-medium uppercase tracking-wide text-text-muted">{{ label() }}</div>
          <div class="mt-0.5 text-2xl font-bold">{{ value() }}</div>
        </div>
      </div>
    }
  `,
})
export class KpiCardComponent {
  readonly label = input.required<string>();
  readonly value = input.required<string | number>();
  readonly variant = input<KpiVariant>('neutral');
  readonly routerLink = input<string | null>(null);

  protected tintClass(): string {
    return ICON_TINT[this.variant()];
  }
}
