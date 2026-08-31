import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, ElementRef, inject, input, model, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import type { ApiErrorResponse } from '../../core/api/api.types';
import { countSegments } from '../../shared/segment-counter/segment-counter';
import { LinkTrackingService } from './link-tracking.service';

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
          [disabled]="inserting()"
        />
        <label class="inline-flex shrink-0 items-center gap-1.5 text-sm text-text-muted">
          <input type="checkbox" [ngModel]="shorten()" (ngModelChange)="shorten.set($event)" [disabled]="inserting()" />
          {{ 'Shorten link' | transloco }}
        </label>
        <button
          type="button"
          class="shrink-0 rounded-card border border-border px-3 py-1.5 text-sm hover:bg-surface-muted disabled:opacity-60"
          (click)="insertLink()"
          [disabled]="inserting()"
        >
          {{ (inserting() ? 'Preparing link…' : 'Insert link') | transloco }}
        </button>
      </div>

      @if (linkError()) {
        <p class="mt-1 text-xs text-danger">{{ linkError() }}</p>
      } @else if (linkNotice()) {
        <p class="mt-1 text-xs text-text-muted">{{ linkNotice() }}</p>
      }
      @if (trackedLinkCount() > 0) {
        <p class="mt-1 text-xs text-text-muted">🔗 {{ trackedNotice() }}</p>
      }
    </div>
  `,
})
export class MessageFieldComponent {
  readonly message = model('');
  readonly ratePerPart = input(0);

  private readonly transloco = inject(TranslocoService);
  private readonly linkTracking = inject(LinkTrackingService);
  private readonly textarea = viewChild<ElementRef<HTMLTextAreaElement>>('textarea');

  protected readonly linkUrl = signal('');
  protected readonly linkError = signal<string | null>(null);
  protected readonly linkNotice = signal<string | null>(null);
  protected readonly shorten = signal(false);
  protected readonly inserting = signal(false);

  /** Distinct http(s) URLs already present in the message - each is a tracked link on send. */
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
    this.linkNotice.set(null);
  }

  /**
   * Turns the typed URL into its tracking link on the server (a redirect that is then run through
   * Short.io when "Shorten link" is ticked) and inserts that link into the message at the caret.
   */
  protected async insertLink(): Promise<void> {
    if (this.inserting()) {
      return;
    }

    const url = this.linkUrl().trim();
    if (!/^https?:\/\/\S+$/i.test(url)) {
      this.linkError.set(this.transloco.translate('Enter a full http:// or https:// URL.'));
      return;
    }

    this.linkError.set(null);
    this.linkNotice.set(null);
    this.inserting.set(true);
    try {
      const prepared = await this.linkTracking.prepare(url, this.shorten());
      this.insertAtCaret(prepared.trackingUrl);
      this.linkUrl.set('');
      if (this.shorten() && !prepared.shortened) {
        this.linkNotice.set(this.transloco.translate('Shortening was unavailable — inserted the full tracking link.'));
      }
    } catch (error) {
      const message =
        error instanceof HttpErrorResponse
          ? ((error.error as ApiErrorResponse)?.message ?? null)
          : null;
      this.linkError.set(message ?? this.transloco.translate('Could not create the tracking link.'));
    } finally {
      this.inserting.set(false);
    }
  }

  private insertAtCaret(text: string): void {
    const el = this.textarea()?.nativeElement;
    const current = this.message();

    if (!el) {
      this.message.set(current ? `${current.replace(/\s*$/, '')} ${text}` : text);
      return;
    }

    const start = el.selectionStart ?? current.length;
    const end = el.selectionEnd ?? current.length;
    const leading = start > 0 && !/\s$/.test(current.slice(0, start)) ? ' ' : '';
    const trailing = end < current.length && !/^\s/.test(current.slice(end)) ? ' ' : '';
    const chunk = `${leading}${text}${trailing}`;

    this.message.set(current.slice(0, start) + chunk + current.slice(end));

    const caret = start + chunk.length;
    setTimeout(() => {
      el.focus();
      el.setSelectionRange(caret, caret);
    });
  }
}
