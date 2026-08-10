import { Component, inject } from '@angular/core';
import { FlashService } from './flash.service';

const KIND_CLASSES: Record<string, string> = {
  success: 'bg-success/10 text-success border-success/20',
  error: 'bg-danger/10 text-danger border-danger/20',
  info: 'bg-primary-50 text-primary-700 border-primary-200',
};

@Component({
  selector: 'app-flash-container',
  standalone: true,
  template: `
    <div class="fixed top-4 right-4 z-50 flex w-full max-w-sm flex-col gap-2" aria-live="polite">
      @for (message of flash.messages(); track message.id) {
        <div
          class="flex items-start justify-between gap-3 rounded-card border px-4 py-3 shadow-card-md"
          [class]="kindClass(message.kind)"
          role="alert"
        >
          <span class="text-sm">{{ message.text }}</span>
          <button
            type="button"
            class="shrink-0 text-sm opacity-60 hover:opacity-100"
            (click)="flash.dismiss(message.id)"
            aria-label="Dismiss"
          >
            &times;
          </button>
        </div>
      }
    </div>
  `,
})
export class FlashContainerComponent {
  protected readonly flash = inject(FlashService);

  protected kindClass(kind: string): string {
    return KIND_CLASSES[kind] ?? KIND_CLASSES['info'];
  }
}
