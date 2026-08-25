import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { CampaignsService, type CampaignDetail } from './campaigns.service';

/**
 * Read-only view of one Campaign: for a file import with a header row, shows the full imported
 * table (every column, including the phone column, per SMPP.Infrastructure.Files.CampaignNumberParser)
 * so a user can confirm exactly what got stored and which column names are available as SMS
 * Template placeholders. Falls back to a flat number list for a pasted/no-header campaign.
 */
@Component({
  selector: 'app-campaign-view',
  standalone: true,
  imports: [RouterLink, TranslocoPipe],
  template: `
    @if (campaign(); as c) {
      <div class="mb-4 flex items-center justify-between">
        <div>
          <h1 class="text-xl font-semibold">{{ c.name }}</h1>
          <p class="text-sm text-text-muted">
            <code>{{ c.externalCampaignCode }}</code> &middot; {{ c.sourceType }} &middot;
            {{ c.recipientCount }} {{ 'Recipients' | transloco }}
          </p>
        </div>
        <div class="flex gap-2">
          <a [routerLink]="['/campaigns', id(), 'edit']" class="rounded-card border border-border px-3 py-1.5 text-sm hover:bg-surface-muted">
            {{ 'Edit' | transloco }}
          </a>
          <a routerLink="/campaigns" class="rounded-card border border-border px-3 py-1.5 text-sm hover:bg-surface-muted">
            {{ 'Back to Campaigns' | transloco }}
          </a>
        </div>
      </div>

      @if (columns().length > 0) {
        <div class="mb-3 text-sm text-text-muted">
          {{ 'Columns detected from the imported file:' | transloco }}
          @for (col of columns(); track col) {
            <code class="ml-1 rounded bg-surface-muted px-1.5 py-0.5">{{ col }}</code>
          }
          {{ '- use any of these as [Placeholder]s in an SMS Template.' | transloco }}
        </div>

        <div class="rounded-card border border-border bg-surface shadow-card">
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
                <tr>
                  <th class="px-4 py-2">{{ 'Number' | transloco }}</th>
                  @for (col of columns(); track col) {
                    <th class="px-4 py-2">{{ col }}</th>
                  }
                </tr>
              </thead>
              <tbody>
                @for (row of c.recipients ?? []; track row.number) {
                  <tr class="border-t border-border">
                    <td class="px-4 py-2"><code>{{ row.number }}</code></td>
                    @for (col of columns(); track col) {
                      <td class="px-4 py-2">{{ row.values?.[col] ?? '' }}</td>
                    }
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      } @else {
        <div class="rounded-card border border-border bg-surface p-4 shadow-card">
          <p class="mb-2 text-sm text-text-muted">
            {{ 'This campaign has no imported columns (pasted numbers, or a file with no header row). Numbers:' | transloco }}
          </p>
          <p class="break-all text-sm">{{ c.numbers }}</p>
        </div>
      }
    }
  `,
})
export class CampaignViewComponent {
  readonly id = input<string>();

  private readonly campaigns = inject(CampaignsService);

  protected readonly campaign = signal<CampaignDetail | null>(null);
  protected readonly columns = computed(() => this.campaign()?.importedColumns ?? []);

  constructor() {
    // A router-bound input (withComponentInputBinding) is set via ComponentRef.setInput() after
    // the component is constructed, not before - reading it directly in the constructor body can
    // see it as still unset. effect() defers to the point where it's actually available, and
    // re-runs if the route navigates to a different campaign id while reusing this instance.
    effect(() => {
      const id = this.id();
      if (id) {
        void this.campaigns.getById(Number(id)).then((c) => this.campaign.set(c));
      }
    });
  }
}
