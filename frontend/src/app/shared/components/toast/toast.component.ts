import { Component, inject } from '@angular/core';
import { NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-toast-stack',
  standalone: true,
  template: `
    <div class="toast-stack">
      @for (toast of notifications.toasts(); track toast.id) {
        <div class="toast" [class]="'toast--' + toast.kind" (click)="notifications.dismiss(toast.id)">
          {{ toast.message }}
        </div>
      }
    </div>
  `,
  styles: [
    `
      .toast-stack {
        position: fixed;
        right: 1.25rem;
        bottom: 1.25rem;
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        z-index: 1000;
        max-width: 340px;
      }
      .toast {
        padding: 0.75em 1em;
        border-radius: var(--radius-sm);
        font-size: 0.85rem;
        color: #fff;
        box-shadow: 0 4px 14px rgba(18, 35, 61, 0.25);
        cursor: pointer;
      }
      .toast--info { background: var(--color-ink); }
      .toast--success { background: var(--color-teal); }
      .toast--error { background: var(--color-red); }
    `,
  ],
})
export class ToastStackComponent {
  readonly notifications = inject(NotificationService);
}
