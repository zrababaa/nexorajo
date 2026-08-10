import { Component, input } from '@angular/core';

@Component({
  selector: 'app-nav-icon',
  standalone: true,
  template: `
    <svg
      viewBox="0 0 24 24"
      width="18"
      height="18"
      fill="none"
      stroke="currentColor"
      stroke-width="1.75"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
    >
      @switch (name()) {
        @case ('dashboard') {
          <rect x="3" y="3" width="7" height="7" rx="1.5" />
          <rect x="14" y="3" width="7" height="7" rx="1.5" />
          <rect x="3" y="14" width="7" height="7" rx="1.5" />
          <rect x="14" y="14" width="7" height="7" rx="1.5" />
        }
        @case ('quickSend') {
          <path d="M22 2 11 13" />
          <path d="M22 2 15 22l-4-9-9-4 20-7Z" />
        }
        @case ('bulkSend') {
          <path d="M12 2 2 7l10 5 10-5-10-5Z" />
          <path d="M2 12l10 5 10-5" />
          <path d="M2 17l10 5 10-5" />
        }
        @case ('campaigns') {
          <path d="M3 10v4a1 1 0 0 0 1 1h2l9 4V5L6 9H4a1 1 0 0 0-1 1Z" />
          <path d="M17 9a4 4 0 0 1 0 6" />
        }
        @case ('history') {
          <circle cx="12" cy="12" r="9" />
          <path d="M12 7v5l3.5 2" />
        }
        @case ('reports') {
          <path d="M4 20V10" />
          <path d="M12 20V4" />
          <path d="M20 20v-7" />
        }
        @case ('credits') {
          <rect x="2" y="5" width="20" height="14" rx="2" />
          <path d="M2 10h20" />
        }
        @case ('accounts') {
          <circle cx="9" cy="8" r="3.25" />
          <path d="M3.5 20a5.5 5.5 0 0 1 11 0" />
          <circle cx="18" cy="9" r="2.5" />
          <path d="M15.5 20a4.5 4.5 0 0 1 6.9-3.8" />
        }
        @case ('inbox') {
          <path d="M4 12h4l2 3h4l2-3h4" />
          <path d="M4 12 6 5h12l2 7" />
          <path d="M4 12v6a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1v-6" />
        }
        @case ('shield') {
          <path d="M12 3l7 3v6c0 4.5-3 7.5-7 9-4-1.5-7-4.5-7-9V6l7-3Z" />
        }
        @case ('wallet') {
          <path d="M3 6a2 2 0 0 1 2-2h12a1 1 0 0 1 1 1v3" />
          <rect x="3" y="6" width="18" height="13" rx="2" />
          <circle cx="16" cy="13" r="1.25" />
        }
        @case ('logs') {
          <path d="M6 2h9l4 4v16H6Z" />
          <path d="M15 2v4h4" />
          <path d="M9 13h6M9 17h6M9 9h2" />
        }
        @case ('customers') {
          <circle cx="9" cy="8" r="3.25" />
          <path d="M3.5 20a5.5 5.5 0 0 1 11 0" />
          <path d="M16 4.5a3.25 3.25 0 0 1 0 6.3" />
          <path d="M17.5 14.5a4.5 4.5 0 0 1 3 4.2" />
        }
        @case ('company') {
          <rect x="4" y="9" width="16" height="12" rx="1" />
          <path d="M9 3h6v6H9z" />
          <path d="M10 21v-4h4v4" />
        }
      }
    </svg>
  `,
})
export class NavIconComponent {
  readonly name = input.required<string>();
}
