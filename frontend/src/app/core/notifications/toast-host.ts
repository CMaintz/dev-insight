import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from './toast.service';

@Component({
  selector: 'app-toast-host',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="toasts" aria-live="polite" aria-relevant="additions">
      @for (toast of toasts(); track toast.id) {
        <div [class]="'toast toast--' + toast.kind" role="status">
          <div class="toast__body">
            <strong>{{ toast.title }}</strong>
            @if (toast.detail) {
              <span>{{ toast.detail }}</span>
            }
          </div>
          <button
            type="button"
            class="toast__close"
            (click)="dismiss(toast.id)"
            aria-label="Dismiss notification"
          >
            &times;
          </button>
        </div>
      }
    </div>
  `,
  styleUrl: './toast-host.scss',
})
export class ToastHost {
  private readonly service = inject(ToastService);
  protected readonly toasts = this.service.toasts;

  protected dismiss(id: number): void {
    this.service.dismiss(id);
  }
}
