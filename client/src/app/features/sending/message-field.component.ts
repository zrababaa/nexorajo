import { Component, computed, ElementRef, inject, input, model, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { countSegments } from '../../shared/segment-counter/segment-counter';

/** http(s) URLs only - matches the backend UrlExtractor scope that drives link-tracking rewriting. */
const URL_PATTERN = /https?:\/\/\S+/gi;

@Component({
  selector: 'app-message-field',
  standalone: true,
  imports: [FormsModule, TranslocoPipe],
  template: `
    <div class="mb-3">
      <label for="messageText" class="mb-1 block text-sm font-medium">{{ 'Message' | transloco }}</label>
      <textarea
        #textarea
        id="messageText"
        rows="4"
        class="w-full rounded-card border border-border px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
        [ngModel]="message()"
        (ngModelChange)="message.set($event)"
      ></textarea>
      <div class="mt-1 text-xs text-text-muted">{{ counterText() }}</div>

      <div class="mt-2 flex flex-wrap items-center gap-2">
        <input
          type="url"
          inputmode="url"
          [placeholder]="'https://example.com/page' | transloco"
          class="min-w-0 flex-1 rounded-card border border-border px-3 py-1.5 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
          [ngModel]="linkUrl()"
          (ngModelChange)="onLinkUrlChange($event)"
          (keydown.enter)="$event.preventDefault(); insertLink()"
        />
        <button
          type="button"
          class="shrink-0 rounded-card border border-border px-3 py-1.5 text-sm hover:bg-surface-muted"
          (click)="insertLink()"
        >
          {{ 'Insert link' | transloco }}
        </button>
      </div>

      @if (linkError()) {
        <p class="mt-1 text-xs text-danger">{{ linkError() }}</p>
      } @else if (trackedLinkCount() > 0) {
        <div class="mt-1 text-xs text-text-muted">
          <p>🔗 {{ trackedNotice() }}</p>
          <div class="mt-1 flex flex-wrap gap-x-4 gap-y-1">
            <label class="inline-flex items-center gap-1.5">
              <input type="radio" name="shortenLinks" [checked]="!shortenLinks()" (change)="shortenLinks.set(false)" />
              {{ 'Keep the full link' | transloco }}
            </label>
            <label class="inline-flex items-center gap-1.5">
              <input type="radio" name="shortenLinks" [checked]="shortenLinks()" (change)="shortenLinks.set(true)" />
              {{ 'Shorten the link' | transloco }}
            </label>
          </div>
        </div>
      }
    </div>
  `,
})
export class MessageFieldComponent {
  readonly message = model('');
  readonly ratePerPart = input(0);

  /**
   * Whether the send should shorten each tracking link (via Short.io) instead of sending the full
   * {BaseUrl}/l/{token} URL. Read by the Quick Send / Bulk Send components at submit time. Only
   * surfaced when the message actually contains a link.
   */
  readonly shortenLinks = model(false);

  private readonly transloco = inject(TranslocoService);
  private readonly textarea = viewChild<ElementRef<HTMLTextAreaElement>>('textarea');

  protected readonly linkUrl = signal('');
  protected readonly linkError = signal<string | null>(null);

  /** Distinct http(s) URLs already present in the message - each becomes one tracked link on send. */
  protected readonly trackedLinkCount = computed(() => {
    const matches = this.message().match(URL_PATTERN) ?? [];
    return new Set(matches.map((m) => m.replace(/[.,!?;:'")\]}>]+$/, ''))).size;
  });

  protected readonly trackedNotice = computed(() =>
    this.transloco
      .translate('{count} link(s) in this message will be tracked. Clicks show on the Tracking Links page.')
      .replace('{count}', String(this.trackedLinkCount())),
  );

  protected readonly counterText = computed(() => {
    const info = countSegments(this.message());
    const encoding = info.isUnicode ? this.transloco.translate('Unicode') : this.transloco.translate('GSM');
    let text = this.transloco
      .translate('{chars} characters · {parts} part(s) · {encoding}')
      .replace('{chars}', String(info.characters))
      .replace('{parts}', String(info.segments))
      .replace('{encoding}', encoding);

    if (this.ratePerPart() > 0) {
      const credits = Math.round(info.segments * this.ratePerPart() * 10000) / 10000;
      text += ' · ' + this.transloco.translate('{credits} credits per recipient').replace('{credits}', String(credits));
    }

    return text;
  });

  protected onLinkUrlChange(value: string): void {
    this.linkUrl.set(value);
    this.linkError.set(null);
  }

  /** Inserts the URL into the message at the caret (or appends it), so the send-time rewrite tracks it. */
  protected insertLink(): void {
    const url = this.linkUrl().trim();
    if (!/^https?:\/\/\S+$/i.test(url)) {
      this.linkError.set(this.transloco.translate('Enter a full http:// or https:// URL.'));
      return;
    }
    this.linkError.set(null);

    const el = this.textarea()?.nativeElement;
    const current = this.message();

    if (!el) {
      this.message.set(current ? `${current.replace(/\s*$/, '')} ${url}` : url);
      this.linkUrl.set('');
      return;
    }

    const start = el.selectionStart ?? current.length;
    const end = el.selectionEnd ?? current.length;
    const leading = start > 0 && !/\s$/.test(current.slice(0, start)) ? ' ' : '';
    const trailing = end < current.length && !/^\s/.test(current.slice(end)) ? ' ' : '';
    const chunk = `${leading}${url}${trailing}`;

    this.message.set(current.slice(0, start) + chunk + current.slice(end));
    this.linkUrl.set('');

    const caret = start + chunk.length;
    setTimeout(() => {
      el.focus();
      el.setSelectionRange(caret, caret);
    });
  }
}
