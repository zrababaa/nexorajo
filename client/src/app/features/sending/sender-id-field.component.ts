import { Component, computed, effect, input, model } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import type { SendPolicy } from './send-policy.service';

@Component({
  selector: 'app-sender-id-field',
  standalone: true,
  imports: [FormsModule, TranslocoPipe],
  template: `
    @if (hasSenderIdList()) {
      <div class="mb-3">
        <label for="senderIdSelect" class="mb-1 block text-sm font-medium">{{ 'Sender ID' | transloco }}</label>
        <select
          id="senderIdSelect"
          class="w-full rounded-card border border-border px-3 py-2 text-sm disabled:opacity-60"
          [ngModel]="senderId()"
          (ngModelChange)="senderId.set($event)"
          [disabled]="useFree()"
        >
          @for (id of policy().allowedSenderIds ?? []; track id) {
            <option [value]="id">{{ id }}</option>
          }
        </select>
      </div>

      @if (policy().allowFreeSenderId) {
        <div class="mb-3">
          <label class="mb-2 flex items-center gap-2 text-sm">
            <input type="checkbox" [ngModel]="useFree()" (ngModelChange)="useFree.set($event)" />
            {{ 'Use my own Sender ID' | transloco }}
          </label>
          <input
            class="w-full rounded-card border border-border px-3 py-2 text-sm disabled:opacity-60"
            maxlength="20"
            [placeholder]="'e.g. MYSHOP' | transloco"
            [ngModel]="freeSenderId()"
            (ngModelChange)="freeSenderId.set($event)"
            [disabled]="!useFree()"
          />
        </div>
      }
    } @else if (policy().allowFreeSenderId) {
      <div class="mb-3">
        <label for="freeSenderIdOnly" class="mb-1 block text-sm font-medium">{{ 'Sender ID' | transloco }}</label>
        <input
          id="freeSenderIdOnly"
          class="w-full rounded-card border border-border px-3 py-2 text-sm"
          maxlength="20"
          [ngModel]="freeSenderId()"
          (ngModelChange)="freeSenderId.set($event)"
        />
      </div>
    } @else {
      <div class="mb-3 rounded-card border border-warning/30 bg-warning/10 px-3 py-2 text-sm text-warning">
        {{ 'No Sender ID has been assigned to this account. Ask an administrator to assign one before sending.' | transloco }}
      </div>
    }
  `,
})
export class SenderIdFieldComponent {
  readonly policy = input.required<SendPolicy>();

  protected readonly senderId = model('');
  protected readonly freeSenderId = model('');
  protected readonly useFree = model(false);

  protected readonly hasSenderIdList = computed(() => (this.policy().allowedSenderIds?.length ?? 0) > 0);

  readonly effectiveSenderId = computed(() =>
    (this.useFree() || !this.hasSenderIdList() ? this.freeSenderId() : this.senderId()).trim(),
  );

  constructor() {
    effect(() => {
      const first = this.policy().allowedSenderIds?.[0];
      if (first && !this.senderId()) {
        this.senderId.set(first);
      }
    });
  }
}
