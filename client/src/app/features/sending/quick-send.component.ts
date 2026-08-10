import { SlicePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import type { ApiErrorResponse } from '../../core/api/api.types';
import { FlashService } from '../../shared/flash/flash.service';
import { SpamBlockedModalComponent } from '../../shared/spam-blocked-modal/spam-blocked-modal.component';
import { CampaignsService, type CampaignListItem } from '../campaigns/campaigns.service';
import { HistoryService, type HistoryListItem } from '../history/history.service';
import { MessageFieldComponent } from './message-field.component';
import { QuickSendService } from './quick-send.service';
import { SenderIdFieldComponent } from './sender-id-field.component';
import { SendPolicyService, type SendPolicy } from './send-policy.service';

@Component({
  selector: 'app-quick-send',
  standalone: true,
  imports: [FormsModule, SlicePipe, TranslocoPipe, MessageFieldComponent, SenderIdFieldComponent, SpamBlockedModalComponent],
  template: `
    <h1 class="mb-4 text-xl font-semibold">{{ 'Quick Send' | transloco }}</h1>

    <app-spam-blocked-modal [open]="blockedTerms() !== null" [blockedTerms]="blockedTerms() ?? []" (closed)="blockedTerms.set(null)" />

    <div class="grid grid-cols-1 gap-4 lg:grid-cols-12">
      <div class="lg:col-span-5">
        <div class="rounded-card border border-border bg-surface p-4 shadow-card">
          @if (savedLists().length > 0) {
            <div class="mb-3">
              <label for="savedList" class="mb-1 block text-sm font-medium">{{ 'Use a saved list' | transloco }}</label>
              <select
                id="savedList"
                class="w-full rounded-card border border-border px-3 py-2 text-sm"
                (change)="onPickList($event)"
              >
                <option value="">{{ 'Select a saved list...' | transloco }}</option>
                @for (l of savedLists(); track l.id) {
                  <option [value]="l.id">{{ l.name }} ({{ l.recipientCount }} {{ 'Recipients' | transloco }})</option>
                }
              </select>
            </div>
          }

          <div class="mb-3">
            <label for="rawNumbers" class="mb-1 block text-sm font-medium">{{ 'Numbers' | transloco }}</label>
            <textarea
              id="rawNumbers"
              rows="5"
              placeholder="9715xxxxxxxx, 9715xxxxxxxx, ..."
              class="w-full rounded-card border border-border px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
              [ngModel]="rawNumbers()"
              (ngModelChange)="rawNumbers.set($event)"
            ></textarea>
          </div>

          @if (policy(); as p) {
            <app-message-field #messageField [ratePerPart]="p.creditsPerMessagePart ?? 0" />
            <app-sender-id-field #senderField [policy]="p" />
          }

          <button
            type="button"
            class="mb-3 w-full rounded-card bg-primary-500 px-4 py-2 text-sm font-medium text-white hover:bg-primary-600 disabled:opacity-60"
            [disabled]="sending()"
            (click)="submit()"
          >
            {{ 'Send' | transloco }}
          </button>

          <div class="border-t border-border pt-3">
            <label for="saveListName" class="mb-1 block text-sm font-medium">{{ 'Save the numbers above as a list' | transloco }}</label>
            <div class="flex gap-2">
              <input
                id="saveListName"
                class="min-w-0 flex-1 rounded-card border border-border px-3 py-2 text-sm"
                [placeholder]="'e.g. VIP customers' | transloco"
                [ngModel]="saveListName()"
                (ngModelChange)="saveListName.set($event)"
              />
              <button
                type="button"
                class="shrink-0 rounded-card border border-border px-3 py-2 text-sm hover:bg-surface-muted disabled:opacity-60"
                [disabled]="savingList()"
                (click)="saveList()"
              >
                {{ 'Save as list' | transloco }}
              </button>
            </div>
          </div>
        </div>
      </div>

      <div class="lg:col-span-7">
        <div class="rounded-card border border-border bg-surface shadow-card">
          <div class="border-b border-border px-4 py-3 text-sm font-semibold">
            {{ 'Recent Quick Send history' | transloco }}
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
export class QuickSendComponent {
  private readonly quickSend = inject(QuickSendService);
  private readonly campaigns = inject(CampaignsService);
  private readonly historyService = inject(HistoryService);
  private readonly sendPolicy = inject(SendPolicyService);
  private readonly flash = inject(FlashService);

  protected readonly rawNumbers = signal('');
  protected readonly saveListName = signal('');
  protected readonly sending = signal(false);
  protected readonly savingList = signal(false);
  protected readonly policy = signal<SendPolicy | null>(null);
  protected readonly savedLists = signal<CampaignListItem[]>([]);
  protected readonly history = signal<HistoryListItem[]>([]);
  protected readonly blockedTerms = signal<readonly string[] | null>(null);

  private readonly messageField = viewChild(MessageFieldComponent);
  private readonly senderField = viewChild(SenderIdFieldComponent);

  constructor() {
    void this.loadAll();
  }

  private async loadAll(): Promise<void> {
    const [policy, lists, history] = await Promise.all([
      this.sendPolicy.getPolicy(),
      this.campaigns.list(1, 500),
      this.historyService.list({ source: 'QuickSend' }, 1, 15),
    ]);
    this.policy.set(policy);
    this.savedLists.set(lists.items ?? []);
    this.history.set(history.items ?? []);
  }

  protected async onPickList(event: Event): Promise<void> {
    const id = Number((event.target as HTMLSelectElement).value);
    if (!id) {
      return;
    }
    const campaign = await this.campaigns.getById(id);
    this.rawNumbers.set(campaign.numbers ?? '');
  }

  protected async submit(): Promise<void> {
    const message = this.messageField()?.message() ?? '';
    const senderId = this.senderField()?.effectiveSenderId() ?? '';

    if (!this.rawNumbers().trim() || !message.trim()) {
      this.flash.error('Enter recipient numbers and a message.');
      return;
    }

    this.sending.set(true);
    try {
      const summary = await this.quickSend.submit({ numbers: this.rawNumbers(), message, senderId });
      this.flash.success(
        `Queued ${summary.recipientCount} message(s), cost ${(summary.totalCost ?? 0).toFixed(4)}. Remaining balance: ${(summary.remainingBalance ?? 0).toFixed(4)}.`,
      );
      this.rawNumbers.set('');
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

  protected async saveList(): Promise<void> {
    if (!this.saveListName().trim()) {
      this.flash.error('Enter a name for the list before saving it.');
      return;
    }
    if (!this.rawNumbers().trim()) {
      this.flash.error('No valid phone numbers were found in the input provided.');
      return;
    }

    this.savingList.set(true);
    try {
      const code = `QS${Date.now()}`;
      const saved = await this.campaigns.create(this.saveListName().trim(), this.rawNumbers(), code);
      this.flash.success(`Saved "${saved.name}" as a list with ${saved.recipientCount} number(s).`);
      this.saveListName.set('');
      const lists = await this.campaigns.list(1, 500);
      this.savedLists.set(lists.items ?? []);
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.flash.error((error.error as ApiErrorResponse)?.message ?? 'Unable to save the list.');
      }
    } finally {
      this.savingList.set(false);
    }
  }
}
