import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { SmsTemplatesService, type SmsTemplateListItem } from './sms-templates.service';
import { globalPlaceholdersOf } from './template-placeholders';

/**
 * Lets a Bulk Send / Scheduled Send form pick either a raw message (unchanged behavior) or an
 * SMS Template plus values for whichever of its placeholders aren't resolved per-recipient from
 * Customers (see template-placeholders.ts). Consumers read `useTemplate()`/`templateId()`/
 * `variables()` via `viewChild`, same pattern as MessageFieldComponent/SenderIdFieldComponent.
 */
@Component({
  selector: 'app-template-picker',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoPipe],
  template: `
    <div class="mb-3 flex gap-4">
      <label class="inline-flex items-center gap-2 text-sm">
        <input type="radio" name="msgSource" [checked]="!useTemplate()" (change)="useTemplate.set(false)" />
        {{ 'Write a message' | transloco }}
      </label>
      <label class="inline-flex items-center gap-2 text-sm">
        <input type="radio" name="msgSource" [checked]="useTemplate()" (change)="useTemplate.set(true)" />
        {{ 'Use a template' | transloco }}
      </label>
    </div>

    @if (useTemplate()) {
      <div class="mb-3">
        <label for="templateId" class="mb-1 block text-sm font-medium">{{ 'SMS Template' | transloco }}</label>
        <select
          id="templateId"
          class="w-full rounded-card border border-border px-3 py-2 text-sm"
          [ngModel]="templateId()"
          (ngModelChange)="templateId.set(+$event)"
        >
          <option [value]="0">{{ 'Select a template...' | transloco }}</option>
          @for (t of templates(); track t.id) {
            <option [value]="t.id">{{ t.name }}</option>
          }
        </select>
        @if (templates().length === 0) {
          <div class="mt-1 text-xs text-text-muted">
            {{ 'You have no SMS templates yet.' | transloco }}
            <a routerLink="/sms-templates/new" class="text-primary-600 hover:underline">{{ 'Create one' | transloco }}</a>
          </div>
        }
      </div>

      @if (selectedTemplate(); as t) {
        <div class="mb-3 rounded-card border border-border bg-surface-muted p-3 text-xs text-text-muted">{{ t.body }}</div>

        @for (key of globalPlaceholders(); track key) {
          <div class="mb-3">
            <label [for]="'tplvar-' + key" class="mb-1 block text-sm font-medium">[{{ key }}]</label>
            <input
              [id]="'tplvar-' + key"
              class="w-full rounded-card border border-border px-3 py-2 text-sm"
              [ngModel]="variables()[key]"
              (ngModelChange)="setVariable(key, $event)"
            />
          </div>
        }
      }
    }
  `,
})
export class TemplatePickerComponent {
  private readonly templatesService = inject(SmsTemplatesService);

  readonly useTemplate = signal(false);
  readonly templateId = signal(0);
  readonly variables = signal<Record<string, string>>({});
  readonly templates = signal<SmsTemplateListItem[]>([]);

  protected readonly selectedTemplate = computed(() => this.templates().find((t) => t.id === this.templateId()) ?? null);
  protected readonly globalPlaceholders = computed(() => globalPlaceholdersOf(this.selectedTemplate()?.body ?? ''));

  readonly isValid = computed(() => {
    if (!this.useTemplate()) {
      return true;
    }
    if (!this.templateId()) {
      return false;
    }
    const values = this.variables();
    return this.globalPlaceholders().every((key) => (values[key] ?? '').trim().length > 0);
  });

  constructor() {
    void this.templatesService.list(1, 500).then((result) => this.templates.set(result.items ?? []));
  }

  protected setVariable(key: string, value: string): void {
    this.variables.update((current) => ({ ...current, [key]: value }));
  }
}
