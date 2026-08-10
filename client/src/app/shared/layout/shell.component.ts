import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { FlashContainerComponent } from '../flash/flash-container.component';
import { SidebarComponent } from './sidebar.component';
import { TopbarComponent } from './topbar.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, TopbarComponent, FlashContainerComponent],
  template: `
    <div class="flex min-h-screen bg-body-bg">
      <app-sidebar />
      <div class="flex min-w-0 flex-1 flex-col">
        <app-topbar />
        <main class="flex-1 p-4 md:p-6">
          <router-outlet />
        </main>
      </div>
    </div>
    <app-flash-container />
  `,
})
export class ShellComponent {}
