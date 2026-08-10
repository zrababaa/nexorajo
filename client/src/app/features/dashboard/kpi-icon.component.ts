import { Component, input } from '@angular/core';

@Component({
  selector: 'app-kpi-icon',
  standalone: true,
  template: `
    <svg
      viewBox="0 0 24 24"
      width="19"
      height="19"
      fill="none"
      stroke="currentColor"
      stroke-width="1.75"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
    >
      @switch (name()) {
        @case ('wallet') {
          <rect x="2" y="5" width="20" height="14" rx="2" />
          <path d="M2 10h20" />
        }
        @case ('send') {
          <path d="M22 2 11 13" />
          <path d="M22 2 15 22l-4-9-9-4 20-7Z" />
        }
        @case ('check') {
          <circle cx="12" cy="12" r="9" />
          <path d="m8.5 12.5 2.5 2.5 4.5-5" />
        }
        @case ('inbox') {
          <path d="M4 12h4l2 3h4l2-3h4" />
          <path d="M4 12 6 5h12l2 7" />
          <path d="M4 12v6a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1v-6" />
        }
        @case ('users') {
          <circle cx="9" cy="8" r="3.25" />
          <path d="M3.5 20a5.5 5.5 0 0 1 11 0" />
          <circle cx="18" cy="9" r="2.5" />
          <path d="M15.5 20a4.5 4.5 0 0 1 6.9-3.8" />
        }
      }
    </svg>
  `,
})
export class KpiIconComponent {
  readonly name = input.required<string>();
}
