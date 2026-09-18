import { HttpContextToken, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { TokenStorageService } from '../services/token-storage.service';

/** Set on requests that must never trigger a refresh-and-retry (e.g. the refresh call itself,
 *  and the three login endpoints — a 401 from those is a real "bad credentials", not an
 *  expired-session situation). */
export const SKIP_AUTH_REFRESH = new HttpContextToken<boolean>(() => false);

const AUTH_FREE_PATHS = ['/auth/customers/login', '/auth/admins/login', '/auth/agents/login', '/auth/refresh', '/auth/customers/register'];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenStorage = inject(TokenStorageService);
  const authService = inject(AuthService);

  const isAuthFree = AUTH_FREE_PATHS.some((p) => req.url.includes(p));
  const accessToken = tokenStorage.getAccessToken();

  const authorizedReq =
    accessToken && !isAuthFree
      ? req.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } })
      : req;

  return next(authorizedReq).pipe(
    catchError((error: unknown) => {
      const shouldAttemptRefresh =
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !isAuthFree &&
        !req.context.get(SKIP_AUTH_REFRESH);

      if (!shouldAttemptRefresh) {
        return throwError(() => error);
      }

      const refreshToken = tokenStorage.getRefreshToken();
      if (!refreshToken) {
        authService.logout();
        return throwError(() => error);
      }

      return authService.refresh(refreshToken).pipe(
        switchMap((tokens) =>
          next(authorizedReq.clone({ setHeaders: { Authorization: `Bearer ${tokens.accessToken}` } }))
        ),
        catchError((refreshError) => {
          authService.logout();
          return throwError(() => refreshError);
        })
      );
    })
  );
};
