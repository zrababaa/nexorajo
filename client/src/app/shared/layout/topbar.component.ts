import { Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthService } from '../../core/auth/auth.service';
import { LocaleService } from '../../core/i18n/locale.service';
import { SidebarService } from './sidebar.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [TranslocoPipe],
  template: `
    <header class="flex items-center gap-3 border-b border-border bg-surface px-4 py-3">
      <button
        type="button"
        class="rounded-card p-2 hover:bg-surface-muted md:hidden"
        (click)="sidebar.toggle()"
        [attr.aria-label]="'Page navigation' | transloco"
      >
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
          <path d="M3 6h18M3 12h18M3 18h18" />
        </svg>
      </button>

      <div class="ms-auto flex items-center gap-3">
        <div class="flex items-center gap-1 text-sm">
          <button
            type="button"
            class="px-1"
            [class.font-semibold]="locale.activeLang === 'en'"
            [class.text-text-muted]="locale.activeLang !== 'en'"
            (click)="locale.setLang('en')"
          >
            EN
          </button>
          <span class="text-text-muted">|</span>
          <button
            type="button"
            class="px-1"
            [class.font-semibold]="locale.activeLang === 'ar'"
            [class.text-text-muted]="locale.activeLang !== 'ar'"
            (click)="locale.setLang('ar')"
          >
            AR
          </button>
        </div>

        <div class="flex items-center gap-2">
          <span
            class="flex h-8 w-8 items-center justify-center rounded-full bg-primary-500 text-sm font-semibold text-white"
          >
            {{ initials() }}
          </span>
          <span class="hidden sm:flex sm:flex-col sm:leading-tight">
            <span class="text-sm font-medium">{{ displayName() }}</span>
            <span class="text-xs text-text-muted">{{ roleLabel() | transloco }}</span>
          </span>
        </div>

        <button
          type="button"
          class="rounded-card border border-border px-3 py-1.5 text-sm hover:bg-surface-muted"
          (click)="signOut()"
        >
          {{ 'Sign out' | transloco }}
        </button>
      </div>
    </header>
  `,
})
export class TopbarComponent {
  protected readonly sidebar = inject(SidebarService);
  protected readonly locale = inject(LocaleService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly displayName = computed(() => this.auth.user()?.username ?? '');

  protected readonly roleLabel = computed(() =>
    this.auth.user()?.role === 'Superadmin' ? 'Superadmin' : 'Account',
  );

  protected readonly initials = computed(() => {
    const name = this.displayName();
    const parts = name.split(/\s+/).filter(Boolean).slice(0, 2);
    const initials = parts.map((p) => p[0]?.toUpperCase()).join('');
    return initials || '?';
  });

  protected signOut(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}
