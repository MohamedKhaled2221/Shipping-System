import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthTokensDto,
  LoginRequest,
  RegisterCustomerRequest,
} from '../models/auth.models';
import { UserRole } from '../models/enums';
import { TokenStorageService } from './token-storage.service';

export interface CurrentUser {
  userId: string;
  role: UserRole;
}

const ROLE_BY_NAME: Record<string, UserRole> = {
  Customer: UserRole.Customer,
  Admin: UserRole.Admin,
  DeliveryAgent: UserRole.DeliveryAgent,
};

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly baseUrl = `${environment.apiBaseUrl}/auth`;

  /**
   * Start with null because dependencies used to rebuild the user
   * from the stored JWT are not initialized until the constructor runs.
   */
  private readonly currentUserSignal =
    signal<CurrentUser | null>(null);

  readonly currentUser =
    this.currentUserSignal.asReadonly();

  readonly isLoggedIn = computed(
    () => this.currentUserSignal() !== null
  );

  readonly role = computed(
    () => this.currentUserSignal()?.role ?? null
  );

  constructor(
    private readonly http: HttpClient,
    private readonly tokenStorage: TokenStorageService
  ) {
    // Dependencies are initialized here, so it is now safe
    // to read the stored access token.
    this.currentUserSignal.set(this.readUserFromToken());
  }

  loginCustomer(
    request: LoginRequest
  ): Observable<AuthTokensDto> {
    return this.login('customers/login', request);
  }

  loginAdmin(
    request: LoginRequest
  ): Observable<AuthTokensDto> {
    return this.login('admins/login', request);
  }

  loginAgent(
    request: LoginRequest
  ): Observable<AuthTokensDto> {
    return this.login('agents/login', request);
  }

  registerCustomer(
    request: RegisterCustomerRequest
  ): Observable<AuthTokensDto> {
    return this.http
      .post<AuthTokensDto>(
        `${this.baseUrl}/customers/register`,
        request
      )
      .pipe(
        tap((tokens) => this.storeSession(tokens))
      );
  }

  /**
   * Used only by the auth interceptor for a silent
   * refresh-and-retry on 401.
   */
  refresh(
    refreshToken: string
  ): Observable<AuthTokensDto> {
    return this.http
      .post<AuthTokensDto>(
        `${this.baseUrl}/refresh`,
        { refreshToken }
      )
      .pipe(
        tap((tokens) => this.storeSession(tokens))
      );
  }

  logout(): void {
    this.tokenStorage.clear();
    this.currentUserSignal.set(null);
  }

  private login(
    path: string,
    request: LoginRequest
  ): Observable<AuthTokensDto> {
    return this.http
      .post<AuthTokensDto>(
        `${this.baseUrl}/${path}`,
        request
      )
      .pipe(
        tap((tokens) => this.storeSession(tokens))
      );
  }

  private storeSession(
    tokens: AuthTokensDto
  ): void {
    this.tokenStorage.setTokens(
      tokens.accessToken,
      tokens.refreshToken
    );

    const role = ROLE_BY_NAME[tokens.role];

    if (!role) {
      this.currentUserSignal.set(null);
      return;
    }

    this.currentUserSignal.set({
      userId: tokens.userId,
      role,
    });
  }

  private readUserFromToken(): CurrentUser | null {
    const claims =
      this.tokenStorage.decodeAccessToken();

    if (
      !claims ||
      this.tokenStorage.isAccessTokenExpired()
    ) {
      return null;
    }

    const userId =
      claims['sub'] ??
      (claims[
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
      ] as string | undefined);

    const roleName =
      claims['role'] ??
      (claims[
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
      ] as string | undefined);

    if (
      !userId ||
      !roleName ||
      !(roleName in ROLE_BY_NAME)
    ) {
      return null;
    }

    return {
      userId,
      role: ROLE_BY_NAME[roleName],
    };
  }
}