import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-forbidden',
  standalone: true,
  imports: [RouterLink, TranslocoPipe],
  template: `
    <div class="flex min-h-screen flex-col items-center justify-center gap-3 bg-body-bg px-4 text-center">
      <h1 class="text-xl font-semibold">{{ 'Access denied' | transloco }}</h1>
      <p class="text-sm text-text-muted">{{ "You don't have permission to view that page." | transloco }}</p>
      <a routerLink="/dashboard" class="text-sm text-primary-600 hover:underline">
        {{ 'Back to dashboard' | transloco }}
      </a>
    </div>
  `,
})
export class ForbiddenComponent {}
