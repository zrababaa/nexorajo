import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthService } from '../../core/auth/auth.service';
import { DashboardChartsComponent } from './dashboard-charts.component';
import { DashboardService, type DashboardData } from './dashboard.service';
import { KpiCardComponent } from './kpi-card.component';
import { KpiIconComponent } from './kpi-icon.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, TranslocoPipe, KpiCardComponent, KpiIconComponent, DashboardChartsComponent],
  template: `
    <h1 class="mb-4 text-xl font-semibold">{{ 'Dashboard' | transloco }}</h1>

    @if (error()) {
      <p class="text-sm text-danger" role="alert">{{ error() }}</p>
    } @else if (data()) {
      @let d = data()!;
      <div class="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
        <app-kpi-card [label]="'Credits' | transloco" [value]="format2(d.summary?.balance)">
          <app-kpi-icon name="wallet" />
        </app-kpi-card>
        <app-kpi-card [label]="'Sends today' | transloco" [value]="d.summary?.sendsToday ?? 0" variant="info">
          <app-kpi-icon name="send" />
        </app-kpi-card>
        <app-kpi-card [label]="'Delivery rate' | transloco" [value]="deliveryRateText(d)" variant="success">
          <app-kpi-icon name="check" />
        </app-kpi-card>
        <app-kpi-card
          [label]="'Pending credit requests' | transloco"
          [value]="d.summary?.pendingPayments ?? 0"
          variant="warning"
          [routerLink]="isSuperadmin() ? '/payments/review' : '/payments'"
        >
          <app-kpi-icon name="inbox" />
        </app-kpi-card>

        @if (isSuperadmin()) {
          <app-kpi-card [label]="'Total accounts' | transloco" [value]="d.summary?.totalAccounts ?? 0">
            <app-kpi-icon name="users" />
          </app-kpi-card>
          <app-kpi-card [label]="'Credits issued (accounts)' | transloco" [value]="format2(d.summary?.totalCreditsIssued)">
            <app-kpi-icon name="wallet" />
          </app-kpi-card>
        }
      </div>

      <div class="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-3">
        <a
          routerLink="/quick-send"
          class="rounded-card border border-primary-200 px-4 py-2 text-center text-sm font-medium text-primary-700 hover:bg-primary-50"
        >
          {{ 'Quick Send' | transloco }}
        </a>
        <a
          routerLink="/bulk-send"
          class="rounded-card border border-primary-200 px-4 py-2 text-center text-sm font-medium text-primary-700 hover:bg-primary-50"
        >
          {{ 'Bulk Send' | transloco }}
        </a>
        <a
          routerLink="/history"
          class="rounded-card border border-primary-200 px-4 py-2 text-center text-sm font-medium text-primary-700 hover:bg-primary-50"
        >
          {{ 'View history' | transloco }}
        </a>
      </div>

      <div class="mt-4">
        <app-dashboard-charts [data]="d" />
      </div>
    } @else {
      <p class="text-sm text-text-muted">Loading…</p>
    }
  `,
})
export class DashboardComponent {
  private readonly dashboard = inject(DashboardService);
  private readonly auth = inject(AuthService);

  protected readonly data = signal<DashboardData | null>(null);
  protected readonly error = signal<string | null>(null);

  protected readonly isSuperadmin = this.auth.isSuperadmin;

  constructor() {
    this.dashboard
      .getDashboard()
      .then((data) => this.data.set(data))
      .catch(() => this.error.set('Unable to load the dashboard right now.'));
  }

  protected format2(value: number | null | undefined): string {
    return (value ?? 0).toFixed(2);
  }

  protected deliveryRateText(d: DashboardData): string {
    const rate = d.summary?.deliveryRatePercent;
    return rate === null || rate === undefined ? '—' : `${rate.toFixed(1)}%`;
  }
}
