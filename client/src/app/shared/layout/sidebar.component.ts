import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { AuthService } from '../../core/auth/auth.service';
import { NAV_SECTIONS } from './nav-items';
import { NavIconComponent } from './nav-icon.component';
import { SidebarService } from './sidebar.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, TranslocoPipe, NavIconComponent],
  template: `
    @if (sidebar.open()) {
      <div class="fixed inset-0 z-30 bg-black/40 md:hidden" (click)="sidebar.close()"></div>
    }

    <nav
      class="fixed inset-y-0 z-40 flex w-64 -translate-x-full flex-col gap-1 overflow-y-auto bg-sidebar-bg px-3 py-4 text-sidebar-text transition-transform md:sticky md:top-0 md:h-screen md:translate-x-0"
      [class.translate-x-0]="sidebar.open()"
    >
      <a routerLink="/dashboard" class="mb-4 flex items-center px-2 py-2">
        <img src="/assets/images/nexora-logo.png" alt="Nexora SMS Portal" class="h-8" />
      </a>

      @for (section of visibleSections(); track section.titleKey) {
        <div class="mt-3 px-2 text-xs font-semibold uppercase tracking-wide text-sidebar-text/60">
          {{ section.titleKey | transloco }}
        </div>
        @for (item of section.items; track item.route) {
          <a
            [routerLink]="item.route"
            routerLinkActive="bg-primary-500/15 text-sidebar-text-active"
            [routerLinkActiveOptions]="{ exact: false }"
            (click)="sidebar.close()"
            class="flex items-center gap-3 rounded-card px-2 py-2 text-sm hover:bg-white/5 hover:text-sidebar-text-active"
          >
            <app-nav-icon [name]="item.icon" class="shrink-0" />
            <span>{{ item.labelKey | transloco }}</span>
          </a>
        }
      }
    </nav>
  `,
})
export class SidebarComponent {
  protected readonly sidebar = inject(SidebarService);
  private readonly auth = inject(AuthService);

  protected readonly sections = NAV_SECTIONS;

  protected visibleSections() {
    const role = this.auth.user()?.role;
    return this.sections.filter((section) => !section.onlyRole || section.onlyRole === role);
  }
}
