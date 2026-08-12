import { SlicePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import type { ApiErrorResponse } from '../../core/api/api.types';
import { FlashService } from '../../shared/flash/flash.service';
import { SpamBlockedModalComponent } from '../../shared/spam-blocked-modal/spam-blocked-modal.component';
import { CampaignsService, type CampaignListItem } from '../campaigns/campaigns.service';
import { HistoryService, type HistoryListItem } from '../history/history.service';
import { ScheduledSendsService } from '../scheduled-sends/scheduled-sends.service';
import { BulkSendService } from './bulk-send.service';
import { MessageFieldComponent } from './message-field.component';
import { SenderIdFieldComponent } from './sender-id-field.component';
import { SendPolicyService, type SendPolicy } from './send-policy.service';

@Component({
  selector: 'app-bulk-send',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    SlicePipe,
    TranslocoPipe,
    MessageFieldComponent,
    SenderIdFieldComponent,
    SpamBlockedModalComponent,
  ],
  template: `
    <h1 class="mb-4 text-xl font-semibold">{{ 'Bulk Send' | transloco }}</h1>

    <app-spam-blocked-modal [open]="blockedTerms() !== null" [blockedTerms]="blockedTerms() ?? []" (closed)="blockedTerms.set(null)" />

    <div class="grid grid-cols-1 gap-4 lg:grid-cols-12">
      <div class="lg:col-span-5">
        <div class="rounded-card border border-border bg-surface p-4 shadow-card">
          @if (campaigns().length === 0) {
            <p class="mb-3 text-sm text-text-muted">
              {{ 'You have no campaigns yet.' | transloco }}
              <a routerLink="/campaigns/new" class="text-primary-600 hover:underline">{{ 'Create one' | transloco }}</a>
              {{ 'first.' | transloco }}
            </p>
          }

          <div class="mb-3">
            <label for="campaignId" class="mb-1 block text-sm font-medium">{{ 'Campaign' | transloco }}</label>
            <select
              id="campaignId"
              class="w-full rounded-card border border-border px-3 py-2 text-sm"
              [ngModel]="campaignId()"
              (ngModelChange)="campaignId.set($event)"
            >
              <option [value]="0">{{ 'Select a campaign...' | transloco }}</option>
              @for (c of campaigns(); track c.id) {
                <option [value]="c.id">{{ c.name }} ({{ c.recipientCount }} {{ 'Recipients' | transloco }})</option>
              }
            </select>
          </div>

          @if (policy(); as p) {
            <app-message-field #messageField [ratePerPart]="p.creditsPerMessagePart ?? 0" />
            <app-sender-id-field #senderField [policy]="p" />
          }

          <div class="mb-3">
            <label class="inline-flex items-center gap-2 text-sm">
              <input type="checkbox" [checked]="scheduled()" (change)="scheduled.set($any($event.target).checked)" />
              {{ 'Schedule for later' | transloco }}
            </label>
            @if (scheduled()) {
              <input
                type="datetime-local"
                class="mt-2 w-full rounded-card border border-border px-3 py-2 text-sm"
                [ngModel]="scheduledAt()"
                (ngModelChange)="scheduledAt.set($event)"
              />
            }
          </div>

          <button
            type="button"
            class="w-full rounded-card bg-primary-500 px-4 py-2 text-sm font-medium text-white hover:bg-primary-600 disabled:opacity-60"
            [disabled]="sending()"
            (click)="submit()"
          >
            {{ (scheduled() ? 'Schedule' : 'Send') | transloco }}
          </button>
        </div>
      </div>

      <div class="lg:col-span-7">
        <div class="rounded-card border border-border bg-surface shadow-card">
          <div class="border-b border-border px-4 py-3 text-sm font-semibold">
            {{ 'Recent Bulk Send history' | transloco }}
          </div>
          <div class="overflow-x-auto">
            <table class="w-full text-sm">
              <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
                <tr>
                  <th class="px-4 py-2">{{ 'Batch' | transloco }}</th>
                  <th class="px-4 py-2">{{ 'Sender' | transloco }}</th>
                  <th class="px-4 py-2">{{ 'Receiver' | transloco }}</th>
                  <th class="px-4 py-2">{{ 'Status' | transloco }}</th>
                  <th class="px-4 py-2">{{ 'Sent' | transloco }}</th>
                </tr>
              </thead>
              <tbody>
                @if (history().length === 0) {
                  <tr><td colspan="5" class="px-4 py-6 text-center text-text-muted">{{ 'No sends yet.' | transloco }}</td></tr>
                }
                @for (h of history(); track h.id) {
                  <tr class="border-t border-border">
                    <td class="px-4 py-2"><code>{{ h.campaignBatchId?.slice(0, 8) }}</code></td>
                    <td class="px-4 py-2">{{ h.senderNumber }}</td>
                    <td class="px-4 py-2">{{ h.receiverNumber }}</td>
                    <td class="px-4 py-2">{{ h.status }}</td>
                    <td class="px-4 py-2">{{ h.createdAt | slice: 0 : 16 }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class BulkSendComponent {
  private readonly bulkSend = inject(BulkSendService);
  private readonly scheduledSends = inject(ScheduledSendsService);
  private readonly campaignsService = inject(CampaignsService);
  private readonly historyService = inject(HistoryService);
  private readonly sendPolicy = inject(SendPolicyService);
  private readonly flash = inject(FlashService);

  protected readonly campaignId = signal(0);
  protected readonly sending = signal(false);
  protected readonly policy = signal<SendPolicy | null>(null);
  protected readonly campaigns = signal<CampaignListItem[]>([]);
  protected readonly history = signal<HistoryListItem[]>([]);
  protected readonly blockedTerms = signal<readonly string[] | null>(null);
  protected readonly scheduled = signal(false);
  protected readonly scheduledAt = signal('');

  private readonly messageField = viewChild(MessageFieldComponent);
  private readonly senderField = viewChild(SenderIdFieldComponent);

  constructor() {
    void this.loadAll();
  }

  private async loadAll(): Promise<void> {
    const [policy, campaigns, history] = await Promise.all([
      this.sendPolicy.getPolicy(),
      this.campaignsService.list(1, 500),
      this.historyService.list({ source: 'BulkSend' }, 1, 15),
    ]);
    this.policy.set(policy);
    this.campaigns.set(campaigns.items ?? []);
    this.history.set(history.items ?? []);
  }

  protected async submit(): Promise<void> {
    const message = this.messageField()?.message() ?? '';
    const senderId = this.senderField()?.effectiveSenderId() ?? '';

    if (!this.campaignId() || !message.trim()) {
      this.flash.error('Select a campaign and enter a message.');
      return;
    }
    if (this.scheduled() && !this.scheduledAt()) {
      this.flash.error('Pick a date and time to schedule for.');
      return;
    }

    this.sending.set(true);
    try {
      if (this.scheduled()) {
        const created = await this.scheduledSends.create(this.campaignId(), message, senderId, this.scheduledAt());
        this.flash.success(`Scheduled for ${new Date(created.scheduledAtUtc).toLocaleString()}.`);
        this.scheduled.set(false);
        this.scheduledAt.set('');
      } else {
        const summary = await this.bulkSend.submit({ campaignId: this.campaignId(), message, senderId });
        this.flash.success(
          `Queued ${summary.recipientCount} message(s), cost ${(summary.totalCost ?? 0).toFixed(4)}. Remaining balance: ${(summary.remainingBalance ?? 0).toFixed(4)}.`,
        );
      }
      await this.loadAll();
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 422) {
        const body = error.error as ApiErrorResponse;
        this.blockedTerms.set(body.blockedTerms ?? []);
      } else if (error instanceof HttpErrorResponse) {
        this.flash.error((error.error as ApiErrorResponse)?.message ?? 'Unable to send right now.');
      }
    } finally {
      this.sending.set(false);
    }
  }
}
