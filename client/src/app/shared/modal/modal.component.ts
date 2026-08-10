import { Component, HostListener, input, output } from '@angular/core';

@Component({
  selector: 'app-modal',
  standalone: true,
  template: `
    @if (open()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div class="absolute inset-0 bg-black/40" (click)="requestClose()"></div>
        <div
          class="relative w-full max-w-md rounded-card bg-surface p-5 shadow-card-md"
          role="dialog"
          aria-modal="true"
          [attr.aria-labelledby]="titleId()"
        >
          <div class="mb-3 flex items-start justify-between gap-4">
            <h2 [id]="titleId()" class="text-base font-semibold">{{ title() }}</h2>
            <button
              type="button"
              class="text-lg leading-none text-text-muted hover:text-black"
              (click)="requestClose()"
              aria-label="Close"
            >
              &times;
            </button>
          </div>
          <ng-content />
        </div>
      </div>
    }
  `,
})
export class ModalComponent {
  readonly open = input(false);
  readonly title = input('');
  readonly closed = output<void>();

  protected titleId(): string {
    return 'modal-title';
  }

  protected requestClose(): void {
    this.closed.emit();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.open()) {
      this.requestClose();
    }
  }
}
