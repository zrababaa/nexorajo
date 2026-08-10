import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import type { ApiErrorResponse } from '../../core/api/api.types';
import { FlashService } from '../../shared/flash/flash.service';
import { AccountsService } from '../accounts/accounts.service';
import { CompanyProfileService, type CompanyDocument } from './company-profile.service';

@Component({
  selector: 'app-company-profile',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoPipe],
  template: `
    <div class="mb-4 flex items-center justify-between">
      <div>
        <a routerLink="/accounts" class="mb-1 block text-sm text-primary-600 hover:underline">&larr; {{ 'Accounts' | transloco }}</a>
        <h1 class="text-xl font-semibold">
          {{ 'Company Profile' | transloco }}@if (accountName()) { <span class="text-text-muted">— {{ accountName() }}</span> }
        </h1>
      </div>
      @if (isActive()) {
        <button
          type="button"
          class="rounded-card border border-border px-3 py-1.5 text-sm hover:bg-surface-muted"
          [disabled]="toggling()"
          (click)="deactivate()"
        >
          {{ 'Deactivate company mode' | transloco }}
        </button>
      } @else {
        <button
          type="button"
          class="rounded-card bg-primary-500 px-3 py-1.5 text-sm font-medium text-white hover:bg-primary-600"
          [disabled]="toggling()"
          (click)="activate()"
        >
          {{ 'Activate company mode' | transloco }}
        </button>
      }
    </div>

    <div class="grid grid-cols-1 gap-4 lg:grid-cols-12">
      <div class="lg:col-span-7">
        <div class="rounded-card border border-border bg-surface p-4 shadow-card">
          @if (errorMessage()) {
            <p class="mb-3 text-sm text-danger" role="alert">{{ errorMessage() }}</p>
          }

          <div class="mb-3 flex items-center gap-3">
            @if (logoPath()) {
              <img [src]="'/' + logoPath()" alt="" class="h-14 w-14 rounded-card border border-border object-cover" />
            }
            <div>
              <label for="logo" class="mb-1 block text-sm font-medium">{{ 'Logo' | transloco }}</label>
              <input id="logo" type="file" accept="image/*" class="text-sm" (change)="onLogoSelected($event)" />
            </div>
          </div>

          <div class="mb-3">
            <label for="registrationId" class="mb-1 block text-sm font-medium">{{ 'Registration ID' | transloco }}</label>
            <input
              id="registrationId"
              class="w-full rounded-card border border-border px-3 py-2 text-sm"
              [ngModel]="registrationId()"
              (ngModelChange)="registrationId.set($event)"
            />
          </div>

          <div class="mb-3">
            <label for="companyName" class="mb-1 block text-sm font-medium">{{ 'Company Name' | transloco }}</label>
            <input
              id="companyName"
              class="w-full rounded-card border border-border px-3 py-2 text-sm"
              [ngModel]="companyName()"
              (ngModelChange)="companyName.set($event)"
            />
          </div>

          <div class="mb-3 grid grid-cols-2 gap-3">
            <div>
              <label for="phone" class="mb-1 block text-sm font-medium">{{ 'Phone' | transloco }}</label>
              <input
                id="phone"
                class="w-full rounded-card border border-border px-3 py-2 text-sm"
                [ngModel]="phone()"
                (ngModelChange)="phone.set($event)"
              />
            </div>
            <div>
              <label for="email" class="mb-1 block text-sm font-medium">{{ 'Email' | transloco }}</label>
              <input
                id="email"
                class="w-full rounded-card border border-border px-3 py-2 text-sm"
                [ngModel]="email()"
                (ngModelChange)="email.set($event)"
              />
            </div>
          </div>

          <div class="mb-3">
            <label for="website" class="mb-1 block text-sm font-medium">{{ 'Website' | transloco }}</label>
            <input
              id="website"
              class="w-full rounded-card border border-border px-3 py-2 text-sm"
              [ngModel]="website()"
              (ngModelChange)="website.set($event)"
            />
          </div>

          <div class="mb-3">
            <label for="address" class="mb-1 block text-sm font-medium">{{ 'Address' | transloco }}</label>
            <input
              id="address"
              class="w-full rounded-card border border-border px-3 py-2 text-sm"
              [ngModel]="address()"
              (ngModelChange)="address.set($event)"
            />
          </div>

          <div class="mb-4">
            <label for="description" class="mb-1 block text-sm font-medium">{{ 'Description' | transloco }}</label>
            <textarea
              id="description"
              rows="3"
              class="w-full rounded-card border border-border px-3 py-2 text-sm"
              [ngModel]="description()"
              (ngModelChange)="description.set($event)"
            ></textarea>
          </div>

          <button
            type="button"
            class="rounded-card bg-primary-500 px-4 py-2 text-sm font-medium text-white hover:bg-primary-600 disabled:opacity-60"
            [disabled]="saving()"
            (click)="save()"
          >
            {{ 'Save' | transloco }}
          </button>
        </div>
      </div>

      <div class="lg:col-span-5">
        <div class="rounded-card border border-border bg-surface shadow-card">
          <div class="border-b border-border px-4 py-3 text-sm font-semibold">{{ 'Documents' | transloco }}</div>
          <div class="p-4">
            <input type="file" class="mb-3 w-full text-sm" (change)="onDocumentSelected($event)" />
            <ul class="space-y-2">
              @if (documents().length === 0) {
                <li class="text-sm text-text-muted">{{ 'No documents uploaded yet.' | transloco }}</li>
              }
              @for (d of documents(); track d.id) {
                <li class="flex items-center justify-between rounded-card border border-border px-3 py-2 text-sm">
                  <a [href]="'/' + d.filePath" target="_blank" rel="noopener" class="truncate text-primary-600 hover:underline">
                    {{ d.fileName }}
                  </a>
                  <button type="button" class="ml-3 shrink-0 text-danger hover:underline" (click)="removeDocument(d)">
                    {{ 'Delete' | transloco }}
                  </button>
                </li>
              }
            </ul>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class CompanyProfileComponent {
  private readonly profileService = inject(CompanyProfileService);
  private readonly accountsService = inject(AccountsService);
  private readonly flash = inject(FlashService);
  private readonly transloco = inject(TranslocoService);

  readonly id = input<string>();

  protected readonly accountName = signal<string | null>(null);
  protected readonly isActive = signal(false);
  protected readonly registrationId = signal('');
  protected readonly companyName = signal('');
  protected readonly address = signal('');
  protected readonly phone = signal('');
  protected readonly email = signal('');
  protected readonly website = signal('');
  protected readonly description = signal('');
  protected readonly logoPath = signal<string | null>(null);
  protected readonly documents = signal<CompanyDocument[]>([]);
  protected readonly saving = signal(false);
  protected readonly toggling = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  constructor() {
    void this.load();
  }

  private get accountId(): number {
    return Number(this.id());
  }

  private async load(): Promise<void> {
    const accountId = this.accountId;
    const [account, profile] = await Promise.all([
      this.accountsService.getById(accountId),
      this.profileService.get(accountId),
    ]);
    this.accountName.set(account.username ?? null);
    this.applyProfile(profile);
    this.documents.set(await this.profileService.listDocuments(accountId));
  }

  private applyProfile(profile: Awaited<ReturnType<CompanyProfileService['get']>>): void {
    this.isActive.set(profile.isActive ?? false);
    this.registrationId.set(profile.registrationId ?? '');
    this.companyName.set(profile.companyName ?? '');
    this.address.set(profile.address ?? '');
    this.phone.set(profile.phone ?? '');
    this.email.set(profile.email ?? '');
    this.website.set(profile.website ?? '');
    this.description.set(profile.description ?? '');
    this.logoPath.set(profile.logoPath ?? null);
  }

  protected async activate(): Promise<void> {
    this.toggling.set(true);
    try {
      this.applyProfile(await this.profileService.activate(this.accountId));
      this.flash.success('Company mode activated.');
    } finally {
      this.toggling.set(false);
    }
  }

  protected async deactivate(): Promise<void> {
    this.toggling.set(true);
    try {
      this.applyProfile(await this.profileService.deactivate(this.accountId));
      this.flash.success('Company mode deactivated.');
    } finally {
      this.toggling.set(false);
    }
  }

  protected async onLogoSelected(event: Event): Promise<void> {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) {
      return;
    }
    try {
      const uploaded = await this.profileService.uploadLogo(this.accountId, file);
      if (uploaded.path) {
        this.logoPath.set(uploaded.path);
      }
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.flash.error((error.error as ApiErrorResponse)?.message ?? 'Unable to upload this logo.');
      }
    }
  }

  protected async onDocumentSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) {
      return;
    }
    try {
      const uploaded = await this.profileService.uploadDocument(this.accountId, file);
      if (uploaded.path) {
        const document = await this.profileService.addDocument(this.accountId, file.name, uploaded.path, file.size);
        this.documents.update((list) => [document, ...list]);
        this.flash.success('Document uploaded successfully.');
      }
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.flash.error((error.error as ApiErrorResponse)?.message ?? 'Unable to upload this document.');
      }
    } finally {
      input.value = '';
    }
  }

  protected async removeDocument(document: CompanyDocument): Promise<void> {
    if (!confirm(this.transloco.translate('Delete this document?'))) {
      return;
    }
    try {
      await this.profileService.deleteDocument(this.accountId, document.id!);
      this.documents.update((list) => list.filter((d) => d.id !== document.id));
      this.flash.success('Document deleted successfully.');
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.flash.error((error.error as ApiErrorResponse)?.message ?? 'Unable to delete this document.');
      }
    }
  }

  protected async save(): Promise<void> {
    this.errorMessage.set(null);
    this.saving.set(true);
    try {
      this.applyProfile(
        await this.profileService.update(this.accountId, {
          registrationId: this.registrationId().trim() || null,
          companyName: this.companyName().trim() || null,
          address: this.address().trim() || null,
          phone: this.phone().trim() || null,
          email: this.email().trim() || null,
          website: this.website().trim() || null,
          description: this.description().trim() || null,
          logoPath: this.logoPath(),
        }),
      );
      this.flash.success('Company profile saved successfully.');
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.errorMessage.set((error.error as ApiErrorResponse)?.message ?? 'Unable to save the company profile.');
      }
    } finally {
      this.saving.set(false);
    }
  }
}
