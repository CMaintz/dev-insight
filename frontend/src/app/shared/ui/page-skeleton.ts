import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-page-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="skeleton"
      [class.skeleton--hero]="hero()"
      aria-busy="true"
      [attr.aria-label]="label()"
    ></div>
    <div class="grid grid--2" aria-hidden="true">
      <div class="skeleton skeleton--tall"></div>
      <div class="skeleton skeleton--tall"></div>
    </div>
  `,
  styles: `
    :host {
      display: grid;
      gap: 1.5rem;
    }
    .skeleton--hero {
      min-height: 14rem;
    }
    .skeleton--tall {
      min-height: 16rem;
    }
  `,
})
export class PageSkeleton {
  readonly label = input.required<string>();
  readonly hero = input(false);
}
