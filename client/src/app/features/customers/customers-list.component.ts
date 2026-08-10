import { SlicePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import type { ApiErrorResponse } from '../../core/api/api.types';
import { FlashService } from '../../shared/flash/flash.service';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { CustomersService, type CustomerListItem, type CustomerStatus } from './customers.service';

const PAGE_SIZE = 10;

@Component({
  selector: 'app-customers-list',
  standalone: true,
  imports: [RouterLink, SlicePipe, FormsModule, TranslocoPipe, PaginationComponent],
  template: `
    <div class="mb-4 flex items-center justify-between">
      <h1 class="text-xl font-semibold">{{ 'Customers' | transloco }}</h1>
      <a routerLink="/customers/new" class="rounded-card bg-primary-500 px-3 py-1.5 text-sm font-medium text-white hover:bg-primary-600">
        + {{ 'New customer' | transloco }}
      </a>
    </div>

    <div class="mb-3 flex flex-wrap gap-2">
      <input
        type="text"
        class="rounded-card border border-border px-3 py-1.5 text-sm"
        [placeholder]="'Search customers' | transloco"
        [ngModel]="search()"
        (ngModelChange)="search.set($event)"
        (keyup.enter)="load(1)"
      />
      <select
        class="rounded-card border border-border px-3 py-1.5 text-sm"
        [ngModel]="status()"
        (ngModelChange)="status.set($event); load(1)"
      >
        <option value="">{{ 'All statuses' | transloco }}</option>
        <option value="Lead">{{ 'Lead' | transloco }}</option>
        <option value="Active">{{ 'Active' | transloco }}</option>
        <option value="Inactive">{{ 'Inactive' | transloco }}</option>
      </select>
      <button type="button" class="rounded-card border border-border px-3 py-1.5 text-sm hover:bg-surface-muted" (click)="load(1)">
        {{ 'Search' | transloco }}
      </button>
    </div>

    <div class="rounded-card border border-border bg-surface shadow-card">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead class="text-left text-xs uppercase tracking-wide text-text-muted">
            <tr>
              <th class="px-4 py-2">{{ 'Name' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Company' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Email' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Phone' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Status' | transloco }}</th>
              <th class="px-4 py-2">{{ 'Created' | transloco }}</th>
              <th class="px-4 py-2 text-right">{{ 'Actions' | transloco }}</th>
            </tr>
          </thead>
          <tbody>
            @if (items().length === 0) {
              <tr><td colspan="7" class="px-4 py-6 text-center text-text-muted">{{ 'No customers yet.' | transloco }}</td></tr>
            }
            @for (c of items(); track c.id) {
              <tr class="border-t border-border">
                <td class="px-4 py-2">{{ c.name }}</td>
                <td class="px-4 py-2">{{ c.companyName }}</td>
                <td class="px-4 py-2">{{ c.email }}</td>
                <td class="px-4 py-2">{{ c.phone }}</td>
                <td class="px-4 py-2">{{ c.status }}</td>
                <td class="px-4 py-2">{{ c.createdAt | slice: 0 : 16 }}</td>
                <td class="px-4 py-2 text-right">
                  <a [routerLink]="['/customers', c.id, 'edit']" class="text-primary-600 hover:underline">{{ 'Edit' | transloco }}</a>
                  <button type="button" class="ml-3 text-danger hover:underline" (click)="remove(c)">{{ 'Delete' | transloco }}</button>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
      <div class="border-t border-border px-4">
        <app-pagination
          [pageNumber]="page()"
          [pageSize]="PAGE_SIZE"
          [totalCount]="totalCount()"
          [totalPages]="totalPages()"
          (pageChange)="load($event)"
        />
      </div>
    </div>
  `,
})
export class CustomersListComponent {
  private readonly customers = inject(CustomersService);
  private readonly flash = inject(FlashService);
  private readonly transloco = inject(TranslocoService);

  protected readonly PAGE_SIZE = PAGE_SIZE;
  protected readonly items = signal<CustomerListItem[]>([]);
  protected readonly page = signal(1);
  protected readonly totalCount = signal(0);
  protected readonly totalPages = signal(0);
  protected readonly search = signal('');
  protected readonly status = signal<CustomerStatus | ''>('');

  constructor() {
    void this.load(1);
  }

  protected async load(page: number): Promise<void> {
    const result = await this.customers.list(this.search().trim(), this.status(), page, PAGE_SIZE);
    this.items.set(result.items ?? []);
    this.page.set(result.pageNumber ?? page);
    this.totalCount.set(result.totalCount ?? 0);
    this.totalPages.set(result.totalPages ?? 0);
  }

  protected async remove(customer: CustomerListItem): Promise<void> {
    if (!confirm(this.transloco.translate('Delete this customer?'))) {
      return;
    }
    try {
      await this.customers.delete(customer.id!);
      this.flash.success('Customer deleted successfully.');
      await this.load(this.page());
    } catch (error) {
      if (error instanceof HttpErrorResponse) {
        this.flash.error((error.error as ApiErrorResponse)?.message ?? 'Unable to delete this customer.');
      }
    }
  }
}
