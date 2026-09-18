import { Injectable } from '@angular/core';

const ACCESS_TOKEN_KEY = 'shipping.accessToken';
const REFRESH_TOKEN_KEY = 'shipping.refreshToken';

export interface JwtClaims {
  sub?: string;
  role?: string;
  // ASP.NET Core's default claim types come through with these long XML-namespace URIs
  // unless the API explicitly maps them — handle both shapes defensively.
  ['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']?: string;
  ['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']?: string;
  exp?: number;
  [key: string]: unknown;
}

/**
 * Plain localStorage token storage. This is the simplest option for a template; a
 * production hardening pass would weigh this against XSS exposure and consider moving the
 * refresh token into an httpOnly cookie set by the API instead (AuthTokensDto's own doc
 * comment on the backend calls this out as a client-specific choice).
 */
@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  setTokens(accessToken: string, refreshToken: string): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  }

  clear(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
  }

  /** Decodes the access token payload without verifying the signature — the API is the
   *  only party that needs to trust it; the client only reads it to drive UI (nav, guards). */
  decodeAccessToken(): JwtClaims | null {
    const token = this.getAccessToken();
    if (!token) return null;
    try {
      const payload = token.split('.')[1];
      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
      const json = decodeURIComponent(
        atob(normalized)
          .split('')
          .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
          .join('')
      );
      return JSON.parse(json) as JwtClaims;
    } catch {
      return null;
    }
  }

  isAccessTokenExpired(): boolean {
    const claims = this.decodeAccessToken();
    const exp = claims?.exp;
    if (!exp) return true;
    return Date.now() >= exp * 1000;
  }
}
