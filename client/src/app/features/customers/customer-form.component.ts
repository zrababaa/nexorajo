import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import type { ApiErrorResponse } from '../../core/api/api.types';
import { FlashService } from '../../shared/flash/flash.service';
import { CustomersService, type CustomerStatus } from './customers.service';

@Component({
  selector: 'app-customer-form',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslocoPipe],
  template: `
    <h1 class="mb-4 text-xl font-semibold">{{ id() ? ('Edit customer' | transloco) : ('New customer' | transloco) }}</h1>

    <div class="max-w-xl rounded-card border border-border bg-surface p-4 shadow-card">
      @if (errorMessage()) {
        <p class="mb-3 text-sm text-danger" role="alert">{{ errorMessage() }}</p>
      }

      <div class="mb-3">
        <label for="name" class="mb-1 block text-sm font-medium">{{ 'Name' | transloco }}</label>
        <input
          id="name"
          class="w-full rounded-card border border-border px-3 py-2 text-sm"
          [ngModel]="name()"
          (ngModelChange)="name.set($event)"
        />
      </div>

      <div class="mb-3">
        <label for="companyName" class="mb-1 block text-sm font-medium">{{ 'Company' | transloco }}</label>
        <input
          id="companyName"
          class="w-full rounded-card border border-border px-3 py-2 text-sm"
          [ngModel]="companyName()"
          (ngModelChange)="companyName.set($event)"
        />
      </div>

      <div class="mb-3 grid grid-cols-2 gap-3">
        <div>
          <label for="email" class="mb-1 block text-sm font-medium">{{ 'Email' | transloco }}</label>
          <input
            id="email"
            class="w-full rounded-card border border-border px-3 py-2 text-sm"
            [ngModel]="email()"
            (ngModelChange)="email.set($event)"
          />
        </div>
        <div>
          <label for="phone" class="mb-1 block text-sm font-medium">{{ 'Phone' | transloco }}</label>
          <input
            id="phone"
            class="w-full rounded-card border border-border px-3 py-2 text-sm"
            [ngModel]="phone()"
            (ngModelChange)="phone.set($event)"
          />
        </div>
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

      <div class="mb-3">
        <label for="status" class="mb-1 block text-sm font-medium">{{ 'Status' | transloco }}</label>
        <select
          id="status"
          class="w-full rounded-card border border-border px-3 py-2 text-sm"
          [ngModel]="status()"
          (ngModelChange)="status.set($event)"
        >
          <option value="Lead">{{ 'Lead' | transloco }}</option>
          <option value="Active">{{ 'Active' | transloco }}</option>
          <option value="Inactive">{{ 'Inactive' | transloco }}</option>
        </select>
      </div>

      <div class="mb-4">
        <label for="notes" class="mb-1 block text-sm font-medium">{{ 'Notes' | transloco }}</label>
        <textarea
          id="notes"
          rows="4"
          class="w-full rounded-card border border-border px-3 py-2 text-sm"
          [ngModel]="notes()"
          (ngModelChange)="notes.set($event)"
        ></textarea>
      </div>

      <div class="flex gap-2">
        <button
          type="button"
          class="rounded-card bg-primary-500 px-4 py-2 text-sm font-medium text-white hover:bg-primary-600 disabled:opacity-60"
          [disabled]="saving()"
          (click)="save()"
        >
          {{ 'Save' | transloco }}
        </button>
        <a routerLink="/customers" class="rounded-card border border-border px-4 py-2 text-sm hover:bg-surface-muted">
          {{ 'Cancel' | transloco }}
        </a>
      </div>
    </div>
  `,
})
export class CustomerFormComponent {
  readonly id = input<string>();

  private readonly customers = inject(CustomersService);
  private readonly flash = inject(FlashService);
  private readonly router = inject(Router);

  protected readonly name = signal('');
  protected readonly companyName = signal('');
  protected readonly email = signal('');
  protected readonly phone = signal('');
  protected readonly address = signal('');
  protected readonly notes = signal('');
  protected readonly status = signal<CustomerStatus>('Lead');
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  constructor() {
    const id = this.id();
    if (id) {
      void this.customers.getById(Number(id)).then((c) => {
        this.name.set(c.name ?? '');
        this.companyName.set(c.companyName ?? '');
        this.email.set(c.email ?? '');
        this.phone.set(c.phone ?? '');
        this.address.set(c.address ?? '');
        this.notes.set(c.notes ?? '');
        this.status.set(c.status ?? 'Lead');
      });
    }
  }

  protected async save(): Promise<void> {
    this.errorMessage.set(null);

    if (!this.name().trim()) {
      this.errorMessage.set('Enter a name for the customer.');
      return;
    }

    this.saving.set(true);
    try {
      const fields = {
        name: this.name().trim(),
        companyName: this.companyName().trim() || null,
        email: this.email().trim() || null,
        phone: this.phone().trim() || null,
        address: this.address().trim() || null,
        notes: this.notes().trim() || null,
        status: this.status(),
      };

      const id = this.id();
      if (id) {
        await this.customers.update(Number(id), fields);
      } else {
        await this.customers.create(fields);
      }

      this.flash.success(id ? 'Customer updated successfully.' : 'Customer added successfully.');
      await this.router.navigateByUrl('/customers');
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.errorMessage.set((error.error as ApiErrorResponse)?.message ?? 'Unable to save this customer.');
      }
    } finally {
      this.saving.set(false);
    }
  }
}
