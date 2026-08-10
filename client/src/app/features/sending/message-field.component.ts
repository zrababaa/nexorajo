import { Component, computed, inject, input, model } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { countSegments } from '../../shared/segment-counter/segment-counter';

@Component({
  selector: 'app-message-field',
  standalone: true,
  imports: [FormsModule, TranslocoPipe],
  template: `
    <div class="mb-3">
      <label for="messageText" class="mb-1 block text-sm font-medium">{{ 'Message' | transloco }}</label>
      <textarea
        id="messageText"
        rows="4"
        class="w-full rounded-card border border-border px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
        [ngModel]="message()"
        (ngModelChange)="message.set($event)"
      ></textarea>
      <div class="mt-1 text-xs text-text-muted">{{ counterText() }}</div>
    </div>
  `,
})
export class MessageFieldComponent {
  readonly message = model('');
  readonly ratePerPart = input(0);

  private readonly transloco = inject(TranslocoService);

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
}
