import { AfterViewInit, Component, ElementRef, OnDestroy, inject, input, viewChild } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Chart, type ChartData, type ChartOptions, registerables } from 'chart.js';
import type { DashboardData } from './dashboard.service';
import { sourceColor, sourceLabel, statusColor } from './chart-colors';

Chart.register(...registerables);

const GRID_COLOR = '#e6e8f0';

@Component({
  selector: 'app-dashboard-charts',
  standalone: true,
  imports: [TranslocoPipe],
  template: `
    <div class="grid grid-cols-1 gap-4 lg:grid-cols-12">
      <div class="rounded-card border border-border bg-surface p-4 lg:col-span-7">
        <div class="mb-3 flex items-center justify-between">
          <span class="text-sm font-medium">{{ 'Sends — last 14 days' | transloco }}</span>
          <span class="text-xs text-text-muted">{{ 'Total vs. delivered' | transloco }}</span>
        </div>
        <canvas
          #trendCanvas
          height="110"
          role="img"
          [attr.aria-label]="'Line chart of total and delivered messages per day over the last 14 days' | transloco"
        ></canvas>
      </div>
      <div class="rounded-card border border-border bg-surface p-4 lg:col-span-5">
        <div class="mb-3 text-sm font-medium">{{ 'Sends by channel' | transloco }}</div>
        <canvas
          #sourceCanvas
          height="110"
          role="img"
          [attr.aria-label]="'Bar chart of message counts by sending channel' | transloco"
        ></canvas>
      </div>
    </div>

    <div class="mt-4 rounded-card border border-border bg-surface p-4">
      <div class="mb-3 text-sm font-medium">{{ 'Delivery status breakdown (all time)' | transloco }}</div>
      <canvas
        #statusCanvas
        height="70"
        role="img"
        [attr.aria-label]="'Stacked bar chart of message counts by delivery status' | transloco"
      ></canvas>
      <ul class="mt-3 flex flex-wrap gap-3 text-sm">
        @for (slice of data().statusBreakdown ?? []; track slice.status) {
          <li class="flex items-center gap-2">
            <span class="inline-block h-2.5 w-2.5 rounded-sm" [style.background]="statusColor(slice.status!)"></span>
            <span>{{ slice.status! | transloco }}: <strong>{{ slice.count }}</strong></span>
          </li>
        }
      </ul>
    </div>
  `,
})
export class DashboardChartsComponent implements AfterViewInit, OnDestroy {
  readonly data = input.required<DashboardData>();

  private readonly transloco = inject(TranslocoService);
  private readonly trendCanvas = viewChild.required<ElementRef<HTMLCanvasElement>>('trendCanvas');
  private readonly sourceCanvas = viewChild.required<ElementRef<HTMLCanvasElement>>('sourceCanvas');
  private readonly statusCanvas = viewChild.required<ElementRef<HTMLCanvasElement>>('statusCanvas');
  private charts: Chart[] = [];

  protected statusColor = statusColor;

  ngAfterViewInit(): void {
    const d = this.data();
    const trend = d.trend ?? [];
    const sourceBreakdown = d.sourceBreakdown ?? [];
    const statusBreakdown = d.statusBreakdown ?? [];
    const t = (key: string) => this.transloco.translate(key);
    const dateFormatter = new Intl.DateTimeFormat(this.transloco.getActiveLang(), {
      month: 'short',
      day: 'numeric',
    });

    this.charts.push(
      this.render(this.trendCanvas().nativeElement, 'line', {
        labels: trend.map((p) => dateFormatter.format(new Date(p.date!))),
        datasets: [
          {
            label: t('Total'),
            data: trend.map((p) => p.total!),
            borderColor: '#2a78d6',
            backgroundColor: '#2a78d6',
            tension: 0.25,
            borderWidth: 2,
            pointRadius: 3,
          },
          {
            label: t('Delivered'),
            data: trend.map((p) => p.delivered!),
            borderColor: '#1baf7a',
            backgroundColor: '#1baf7a',
            tension: 0.25,
            borderWidth: 2,
            pointRadius: 3,
          },
        ],
      } as ChartData<'line'>, {
        responsive: true,
        interaction: { mode: 'index', intersect: false },
        plugins: { legend: { position: 'bottom' } },
        scales: {
          x: { grid: { display: false } },
          y: { beginAtZero: true, ticks: { precision: 0 }, grid: { color: GRID_COLOR } },
        },
      }),
    );

    this.charts.push(
      this.render(this.sourceCanvas().nativeElement, 'bar', {
        labels: sourceBreakdown.map((s) => t(sourceLabel(s.source!))),
        datasets: [
          {
            label: t('Messages'),
            data: sourceBreakdown.map((s) => s.count!),
            backgroundColor: sourceBreakdown.map((s) => sourceColor(s.source!)),
            borderRadius: 4,
            maxBarThickness: 56,
          },
        ],
      } as ChartData<'bar'>, {
        responsive: true,
        plugins: { legend: { display: false } },
        scales: {
          x: { grid: { display: false } },
          y: { beginAtZero: true, ticks: { precision: 0 }, grid: { color: GRID_COLOR } },
        },
      }),
    );

    this.charts.push(
      this.render(this.statusCanvas().nativeElement, 'bar', {
        labels: [t('Messages')],
        datasets: statusBreakdown.map((s) => ({
          label: t(s.status!),
          data: [s.count!],
          backgroundColor: statusColor(s.status!),
        })),
      } as ChartData<'bar'>, {
        indexAxis: 'y',
        responsive: true,
        plugins: { legend: { display: false } },
        scales: {
          x: { stacked: true, beginAtZero: true, grid: { color: GRID_COLOR } },
          y: { stacked: true, grid: { display: false } },
        },
      }),
    );
  }

  ngOnDestroy(): void {
    this.charts.forEach((chart) => chart.destroy());
  }

  private render(
    canvas: HTMLCanvasElement,
    type: 'line' | 'bar',
    data: ChartData,
    options: ChartOptions,
  ): Chart {
    Chart.defaults.font.family = "system-ui, -apple-system, 'Segoe UI', sans-serif";
    Chart.defaults.color = '#6b7280';
    return new Chart(canvas, { type, data, options });
  }
}
