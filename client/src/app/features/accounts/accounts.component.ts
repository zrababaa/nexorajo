import { SlicePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import type { ApiErrorResponse } from '../../core/api/api.types';
import { FlashService } from '../../shared/flash/flash.service';
import { NavIconComponent } from '../../shared/layout/nav-icon.component';
import { ModalComponent } from '../../shared/modal/modal.component';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { AccountsService, type AccountListItem } from './accounts.service';

const PAGE_SIZE = 15;

@Component({
  selector: 'app-accounts',
  standalone: true,
  imports: [FormsModule, RouterLink, SlicePipe, TranslocoPipe, ModalComponent, NavIconComponent, PaginationComponent],
  template: `
    <div class="mb-4 flex items-center justify-between">
      <h1 class="text-xl font-semibold">{{ 'Accounts' | transloco }}</h1>
      <button type="button" class="rounded-card bg-primary-500 px-3 py-1.5 text-sm font-medium text-white hover:bg-primary-600" (click)="openCreate()">
        + {{ 'New account' | transloco }}
      </button>
    </div>

    <div class="rounded-card border border-border bg-surface shadow-card">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
            <tr>
              <th class="px-4 py-2">{{ 'Username' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Email' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Full name' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Active' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Balance' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Created' | transloco }}</th>
              <th class="px-4 py-2 text-right">{{ 'Actions' | transloco }}</th>
            </tr>
          </thead>
          <tbody>
            @if (items().length === 0) {
              <tr><td colspan="7" class="px-4 py-6 text-center text-text-muted">{{ 'No accounts yet.' | transloco }}</td></tr>
            }
            @for (a of items(); track a.id) {
              <tr class="border-t border-border">
                <td class="px-4 py-2">{{ a.username }}</td>
                <td class="px-4 py-2">{{ a.email }}</td>
                <td class="px-4 py-2">{{ a.fullName }}</td>
                <td class="px-4 py-2">{{ a.isActive ? ('Yes' | transloco) : ('No' | transloco) }}</td>
                <td class="px-4 py-2">{{ a.balance }}</td>
                <td class="px-4 py-2">{{ a.createdAt | slice: 0 : 16 }}</td>
                <td class="px-4 py-2 text-right">
                  <a
                    [routerLink]="['/accounts', a.id, 'company-profile']"
                    class="inline-flex align-middle text-text-muted hover:text-primary-600"
                    [title]="'Company Profile' | transloco"
                    [attr.aria-label]="'Company Profile' | transloco"
                  ><app-nav-icon name="company" /></a>
                  <button type="button" class="ml-3 text-primary-600 hover:underline" (click)="openEdit(a)">{{ 'Edit' | transloco }}</button>
                  <button type="button" class="ml-3 text-success hover:underline" (click)="credit(a)">{{ 'Credit' | transloco }}</button>
                  <button type="button" class="ml-3 text-danger hover:underline" (click)="debit(a)">{{ 'Debit' | transloco }}</button>
                  <button type="button" class="ml-3 text-text-muted hover:underline" (click)="regenerateCredentials(a)">{{ 'API keys' | transloco }}</button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
      <div class="border-t border-border px-4">
        <app-pagination [pageNumber]="page()" [pageSize]="PAGE_SIZE" [totalCount]="totalCount()" [totalPages]="totalPages()" (pageChange)="load($event)" />
      </div>
    </div>

    <app-modal [open]="modalOpen()" [title]="editingId() ? ('Edit account' | transloco) : ('New account' | transloco)" (closed)="modalOpen.set(false)">
      @if (errorMessage()) {
        <p class="mb-3 text-sm text-danger" role="alert">{{ errorMessage() }}</p>
      }

      @if (!editingId()) {
        <div class="mb-3">
          <label class="mb-1 block text-sm font-medium">{{ 'Username' | transloco }}</label>
          <input class="w-full rounded-card border border-border px-3 py-2 text-sm" [ngModel]="username()" (ngModelChange)="username.set($event)" />
        </div>
        <div class="mb-3">
          <label class="mb-1 block text-sm font-medium">{{ 'Email' | transloco }}</label>
          <input type="email" class="w-full rounded-card border border-border px-3 py-2 text-sm" [ngModel]="email()" (ngModelChange)="email.set($event)" />
        </div>
        <div class="mb-3">
          <label class="mb-1 block text-sm font-medium">{{ 'Password' | transloco }}</label>
          <input type="password" class="w-full rounded-card border border-border px-3 py-2 text-sm" [ngModel]="password()" (ngModelChange)="password.set($event)" />
        </div>
        <div class="mb-3">
          <label class="mb-1 block text-sm font-medium">{{ 'Initial balance' | transloco }}</label>
          <input type="number" min="0" step="0.0001" class="w-full rounded-card border border-border px-3 py-2 text-sm" [ngModel]="initialBalance()" (ngModelChange)="initialBalance.set($event)" />
        </div>
      } @else {
        <div class="mb-3">
          <label class="mb-2 flex items-center gap-2 text-sm">
            <input type="checkbox" [ngModel]="isActive()" (ngModelChange)="isActive.set($event)" />
            {{ 'Active' | transloco }}
          </label>
        </div>
      }

      <div class="mb-3">
        <label class="mb-1 block text-sm font-medium">{{ 'Full name' | transloco }}</label>
        <input class="w-full rounded-card border border-border px-3 py-2 text-sm" [ngModel]="fullName()" (ngModelChange)="fullName.set($event)" />
      </div>
      <div class="mb-3">
        <label class="mb-1 block text-sm font-medium">{{ 'Mobile number' | transloco }}</label>
        <input class="w-full rounded-card border border-border px-3 py-2 text-sm" [ngModel]="mobileNo()" (ngModelChange)="mobileNo.set($event)" />
      </div>
      <div class="mb-3">
        <label class="mb-1 block text-sm font-medium">{{ 'Sender IDs (comma separated)' | transloco }}</label>
        <input class="w-full rounded-card border border-border px-3 py-2 text-sm" [ngModel]="senderIds()" (ngModelChange)="senderIds.set($event)" />
      </div>
      <div class="mb-4">
        <label class="mb-2 flex items-center gap-2 text-sm">
          <input type="checkbox" [ngModel]="allowFreeSenderId()" (ngModelChange)="allowFreeSenderId.set($event)" />
          {{ 'Allow the account to type its own Sender ID' | transloco }}
        </label>
      </div>

      <div class="flex justify-end gap-2">
        <button type="button" class="rounded-card border border-border px-4 py-2 text-sm hover:bg-surface-muted" (click)="modalOpen.set(false)">
          {{ 'Cancel' | transloco }}
        </button>
        <button
          type="button"
          class="rounded-card bg-primary-500 px-4 py-2 text-sm font-medium text-white hover:bg-primary-600 disabled:opacity-60"
          [disabled]="saving()"
          (click)="save()"
        >
          {{ 'Save' | transloco }}
        </button>
      </div>
    </app-modal>
  `,
})
export class AccountsComponent {
  private readonly accounts = inject(AccountsService);
  private readonly flash = inject(FlashService);
  private readonly transloco = inject(TranslocoService);

  protected readonly PAGE_SIZE = PAGE_SIZE;
  protected readonly items = signal<AccountListItem[]>([]);
  protected readonly page = signal(1);
  protected readonly totalCount = signal(0);
  protected readonly totalPages = signal(0);

  protected readonly modalOpen = signal(false);
  protected readonly editingId = signal<number | null>(null);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly username = signal('');
  protected readonly email = signal('');
  protected readonly password = signal('');
  protected readonly initialBalance = signal<number | null>(0);
  protected readonly fullName = signal('');
  protected readonly mobileNo = signal('');
  protected readonly senderIds = signal('');
  protected readonly allowFreeSenderId = signal(false);
  protected readonly isActive = signal(true);

  constructor() {
    void this.load(1);
  }

  protected async load(page: number): Promise<void> {
    const result = await this.accounts.list(page, PAGE_SIZE);
    this.items.set(result.items ?? []);
    this.page.set(result.pageNumber ?? page);
    this.totalCount.set(result.totalCount ?? 0);
    this.totalPages.set(result.totalPages ?? 0);
  }

  protected openCreate(): void {
    this.editingId.set(null);
    this.errorMessage.set(null);
    this.username.set('');
    this.email.set('');
    this.password.set('');
    this.initialBalance.set(0);
    this.fullName.set('');
    this.mobileNo.set('');
    this.senderIds.set('');
    this.allowFreeSenderId.set(false);
    this.isActive.set(true);
    this.modalOpen.set(true);
  }

  protected async openEdit(account: AccountListItem): Promise<void> {
    this.errorMessage.set(null);
    const detail = await this.accounts.getById(account.id!);
    this.editingId.set(detail.id ?? null);
    this.fullName.set(detail.fullName ?? '');
    this.mobileNo.set(detail.mobileNo ?? '');
    this.senderIds.set(detail.senderIds ?? '');
    this.allowFreeSenderId.set(!!detail.allowFreeSenderId);
    this.isActive.set(!!detail.isActive);
    this.modalOpen.set(true);
  }

  protected async save(): Promise<void> {
    this.errorMessage.set(null);
    this.saving.set(true);
    try {
      const id = this.editingId();
      if (id) {
        await this.accounts.update(id, {
          fullName: this.fullName().trim(),
          mobileNo: this.mobileNo() || null,
          isActive: this.isActive(),
          senderIds: this.senderIds() || null,
          allowFreeSenderId: this.allowFreeSenderId(),
        });
        this.flash.success('Account updated.');
      } else {
        await this.accounts.create({
          username: this.username().trim(),
          email: this.email().trim(),
          fullName: this.fullName().trim(),
          mobileNo: this.mobileNo() || null,
          password: this.password(),
          initialBalance: this.initialBalance() ?? 0,
          senderIds: this.senderIds() || null,
          allowFreeSenderId: this.allowFreeSenderId(),
        });
        this.flash.success('Account created.');
      }
      this.modalOpen.set(false);
      await this.load(this.page());
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.errorMessage.set((error.error as ApiErrorResponse)?.message ?? 'Unable to save this account.');
      }
    } finally {
      this.saving.set(false);
    }
  }

  protected async credit(account: AccountListItem): Promise<void> {
    const amount = this.promptAmount('Amount to credit');
    if (amount === null) {
      return;
    }
    try {
      const result = await this.accounts.credit(account.id!, amount);
      this.flash.success(`Credited. New balance: ${result.balance}.`);
      await this.load(this.page());
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.flash.error((error.error as ApiErrorResponse)?.message ?? 'Unable to credit this account.');
      }
    }
  }

  protected async debit(account: AccountListItem): Promise<void> {
    const amount = this.promptAmount('Amount to debit');
    if (amount === null) {
      return;
    }
    try {
      const result = await this.accounts.debit(account.id!, amount);
      this.flash.success(`Debited. New balance: ${result.balance}.`);
      await this.load(this.page());
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.flash.error((error.error as ApiErrorResponse)?.message ?? 'Unable to debit this account.');
      }
    }
  }

  protected async regenerateCredentials(account: AccountListItem): Promise<void> {
    if (!confirm(this.transloco.translate('This replaces the account\'s API token/secret. Continue?'))) {
      return;
    }
    try {
      const result = await this.accounts.regenerateApiCredentials(account.id!);
      this.flash.success(`New API token: ${result.apiToken}`);
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.flash.error((error.error as ApiErrorResponse)?.message ?? 'Unable to regenerate API credentials.');
      }
    }
  }

  private promptAmount(labelKey: string): number | null {
    const input = prompt(this.transloco.translate(labelKey));
    if (input === null) {
      return null;
    }
    const amount = Number(input);
    return Number.isFinite(amount) && amount > 0 ? amount : null;
  }
}
