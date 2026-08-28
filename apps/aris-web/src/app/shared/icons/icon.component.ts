import { Component, Input } from '@angular/core';

// Hand-drawn inline SVG, stroke-based, 1.6px stroke weight, 24x24 viewBox, stroke="currentColor" —
// UI Guidelines §5. Built once here rather than copy-pasted inline per screen (§8).
export type IconName = 'grid' | 'eye' | 'eye-off' | 'alert-triangle' | 'chevron-down';

@Component({
  selector: 'app-icon',
  template: `
    <svg [attr.width]="size" [attr.height]="size" viewBox="0 0 24 24" fill="none" aria-hidden="true">
      @switch (name) {
        @case ('grid') {
          <rect x="3" y="3" width="7" height="7" rx="1.5" stroke="currentColor" stroke-width="1.6" />
          <rect x="14" y="3" width="7" height="7" rx="1.5" stroke="currentColor" stroke-width="1.6" />
          <rect x="3" y="14" width="7" height="7" rx="1.5" stroke="currentColor" stroke-width="1.6" />
          <rect x="14" y="14" width="7" height="7" rx="1.5" stroke="currentColor" stroke-width="1.6" />
        }
        @case ('eye') {
          <path d="M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7-10-7-10-7z" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round" />
          <circle cx="12" cy="12" r="3" stroke="currentColor" stroke-width="1.6" />
        }
        @case ('eye-off') {
          <path d="M3 3l18 18" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" />
          <path d="M10.6 5.2A10.6 10.6 0 0 1 12 5c6.4 0 10 7 10 7a17.9 17.9 0 0 1-3.5 4.6" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" />
          <path d="M6.1 6.1C3.4 7.9 2 11 2 12c0 0 1.6 3 4.6 5" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" />
          <path d="M9.9 9.9a3 3 0 0 0 4.2 4.2" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" />
        }
        @case ('alert-triangle') {
          <path d="M12 3.5l9.5 16.5H2.5L12 3.5z" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round" />
          <path d="M12 9.5v4.5" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" />
          <circle cx="12" cy="17" r="0.9" fill="currentColor" />
        }
        @case ('chevron-down') {
          <path d="M5 8l7 7 7-7" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" />
        }
      }
    </svg>
  `,
})
export class IconComponent {
  @Input({ required: true }) name!: IconName;
  @Input() size = 18;
}
