import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  standalone: true,
  template: `
    <div class="pagination">
      <button class="btn-outline" [disabled]="pageNumber() <= 1" (click)="pageChange.emit(pageNumber() - 1)">
        ← Prev
      </button>
      <span class="pagination__label">Page {{ pageNumber() }} of {{ totalPages() || 1 }}</span>
      <button
        class="btn-outline"
        [disabled]="pageNumber() >= totalPages()"
        (click)="pageChange.emit(pageNumber() + 1)"
      >
        Next →
      </button>
    </div>
  `,
  styles: [
    `
      .pagination { display: flex; align-items: center; gap: 0.9rem; margin-top: 1rem; }
      .pagination__label { font-size: 0.82rem; color: var(--color-muted); font-family: var(--font-mono); }
    `,
  ],
})
export class PaginationComponent {
  readonly pageNumber = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly pageChange = output<number>();
}
