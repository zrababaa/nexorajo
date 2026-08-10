import { Component, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { ModalComponent } from '../modal/modal.component';

@Component({
  selector: 'app-spam-blocked-modal',
  standalone: true,
  imports: [ModalComponent, TranslocoPipe],
  template: `
    <app-modal [open]="open()" [title]="'Message not sent' | transloco" (closed)="closed.emit()">
      <p class="text-sm text-text-muted">
        {{ 'The content filter found the following blocked term(s) in your message:' | transloco }}
      </p>
      <ul class="my-3 flex flex-wrap gap-2">
        @for (term of blockedTerms(); track term) {
          <li class="rounded-card bg-danger/10 px-2 py-1 text-sm text-danger">{{ term }}</li>
        }
      </ul>
      <p class="text-sm text-text-muted">
        {{ 'Nothing was sent and no credits were charged. Edit the message and try again.' | transloco }}
      </p>
      <div class="mt-4 flex justify-end">
        <button
          type="button"
          class="rounded-card bg-primary-500 px-4 py-2 text-sm font-medium text-white hover:bg-primary-600"
          (click)="closed.emit()"
        >
          {{ 'Edit message' | transloco }}
        </button>
      </div>
    </app-modal>
  `,
})
export class SpamBlockedModalComponent {
  readonly open = input(false);
  readonly blockedTerms = input<readonly string[]>([]);
  readonly closed = output<void>();
}
