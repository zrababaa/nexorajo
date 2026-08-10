import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import type { ApiErrorResponse } from '../../core/api/api.types';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslocoPipe],
  template: `
    <div class="flex min-h-screen items-center justify-center bg-body-bg px-4">
      <div class="w-full max-w-sm rounded-card bg-surface p-6 shadow-card-md">
        <img src="/assets/images/nexora-logo.png" alt="Nexora SMS Portal" class="mx-auto mb-6 h-10" />

        <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4" novalidate>
          <div>
            <label for="identifier" class="mb-1 block text-sm font-medium">
              {{ 'Username or email' | transloco }}
            </label>
            <input
              id="identifier"
              type="text"
              formControlName="identifier"
              class="w-full rounded-card border border-border px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
              autocomplete="username"
            />
          </div>

          <div>
            <label for="password" class="mb-1 block text-sm font-medium">
              {{ 'Password' | transloco }}
            </label>
            <input
              id="password"
              type="password"
              formControlName="password"
              class="w-full rounded-card border border-border px-3 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
              autocomplete="current-password"
            />
          </div>

          @if (errorMessage()) {
            <p class="text-sm text-danger" role="alert">{{ errorMessage() }}</p>
          }

          <button
            type="submit"
            class="rounded-card bg-primary-500 px-4 py-2 text-sm font-medium text-white hover:bg-primary-600 disabled:opacity-60"
            [disabled]="form.invalid || submitting()"
          >
            {{ 'Sign in' | transloco }}
          </button>

          <a routerLink="/forgot-password" class="text-center text-sm text-primary-600 hover:underline">
            {{ 'Forgot password?' | transloco }}
          </a>
        </form>
      </div>
    </div>
  `,
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    identifier: ['', Validators.required],
    password: ['', Validators.required],
  });

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    try {
      const { identifier, password } = this.form.getRawValue();
      await this.auth.login(identifier, password);
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard';
      await this.router.navigateByUrl(returnUrl);
    } catch (error) {
      const body = error instanceof HttpErrorResponse ? (error.error as ApiErrorResponse) : undefined;
      this.errorMessage.set(body?.message ?? 'Unable to sign in. Please try again.');
    } finally {
      this.submitting.set(false);
    }
  }
}
