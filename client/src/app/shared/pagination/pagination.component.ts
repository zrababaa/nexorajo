import { Component, computed, inject, input, output } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { formatIndexed } from '../../core/i18n/format-indexed';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [TranslocoPipe],
  template: `
    @if (totalCount() > 0) {
      <div class="flex flex-wrap items-center justify-between gap-3 py-2">
        <div class="text-sm text-text-muted">
          {{ summaryText() }}
        </div>

        @if (totalPages() > 1) {
          <nav [attr.aria-label]="'Page navigation' | transloco">
            <ul class="flex items-center gap-1 text-sm">
              <li>
                <button
                  type="button"
                  class="rounded-card px-2 py-1 hover:bg-surface-muted disabled:opacity-40"
                  [disabled]="pageNumber() === 1"
                  (click)="go(pageNumber() - 1)"
                  [attr.aria-label]="'Previous' | transloco"
                >
                  &laquo;
                </button>
              </li>
              @for (p of pageWindow(); track $index) {
                <li>
                  @if (p === null) {
                    <span class="px-2 py-1 text-text-muted">&hellip;</span>
                  } @else {
                    <button
                      type="button"
                      class="rounded-card px-2.5 py-1"
                      [class.bg-primary-500]="p === pageNumber()"
                      [class.text-white]="p === pageNumber()"
                      [class.hover:bg-surface-muted]="p !== pageNumber()"
                      (click)="go(p)"
                    >
                      {{ p }}
                    </button>
                  }
                </li>
              }
              <li>
                <button
                  type="button"
                  class="rounded-card px-2 py-1 hover:bg-surface-muted disabled:opacity-40"
                  [disabled]="pageNumber() === totalPages()"
                  (click)="go(pageNumber() + 1)"
                  [attr.aria-label]="'Next' | transloco"
                >
                  &raquo;
                </button>
              </li>
            </ul>
          </nav>
        }
      </div>
    }
  `,
})
export class PaginationComponent {
  readonly pageNumber = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly totalPages = input.required<number>();

  readonly pageChange = output<number>();

  private readonly transloco = inject(TranslocoService);

  protected readonly summaryText = computed(() => {
    const first = (this.pageNumber() - 1) * this.pageSize() + 1;
    const last = Math.min(this.pageNumber() * this.pageSize(), this.totalCount());
    const template = this.transloco.translate('Showing {0}-{1} of {2}');
    return formatIndexed(template, first, last, this.totalCount());
  });

  protected readonly pageWindow = computed<(number | null)[]>(() =>
    buildPageWindow(this.pageNumber(), this.totalPages()),
  );

  protected go(page: number): void {
    if (page >= 1 && page <= this.totalPages() && page !== this.pageNumber()) {
      this.pageChange.emit(page);
    }
  }
}

/**
 * Always shows the first and last page, plus a small window around the current page, collapsing
 * gaps into a single `null` "..." entry - mirrors the Razor _Pagination partial's BuildPageWindow.
 */
function buildPageWindow(current: number, total: number): (number | null)[] {
  const edgeCount = 1;
  const aroundCurrent = 1;
  const pages = new Set<number>([1, total]);

  for (let i = current - aroundCurrent; i <= current + aroundCurrent; i++) {
    if (i >= 1 && i <= total) {
      pages.add(i);
    }
  }
  for (let i = 1; i <= edgeCount && i <= total; i++) {
    pages.add(i);
    pages.add(total - i + 1);
  }

  const sorted = [...pages].sort((a, b) => a - b);
  const window: (number | null)[] = [];
  let previous = 0;
  for (const page of sorted) {
    if (previous !== 0 && page - previous > 1) {
      window.push(null);
    }
    window.push(page);
    previous = page;
  }
  return window;
}
