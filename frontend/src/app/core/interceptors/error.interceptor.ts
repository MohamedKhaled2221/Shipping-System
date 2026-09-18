import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../../shared/services/notification.service';

/** Surfaces API errors as a toast, then re-throws so individual components can still react
 *  (e.g. keep a form open, highlight a field) instead of only ever seeing a silent failure. */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notifications = inject(NotificationService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        notifications.error(extractMessage(error));
      }
      return throwError(() => error);
    })
  );
};

interface ProblemDetailsBody {
  title?: string;
  detail?: string;
  message?: string;
}

function extractMessage(error: HttpErrorResponse): string {
  const raw = error.error as ProblemDetailsBody | string | null;

  if (typeof raw === 'string') {
    if (raw.trim()) return raw;
  } else if (raw) {
    if (raw.detail) return raw.detail;
    if (raw.title) return raw.title;
    if (raw.message) return raw.message;
  }

  switch (error.status) {
    case 0:
      return 'Could not reach the server. Check your connection and the API base URL.';
    case 401:
      return 'Your session has expired. Please sign in again.';
    case 403:
      return "You don't have permission to do that.";
    case 404:
      return 'Not found.';
    default:
      return `Something went wrong (HTTP ${error.status}).`;
  }
}
