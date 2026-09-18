// Mirrors Api/Contracts/AuthRequests.cs and Application/Common/Models/AuthDtos.cs

export interface RegisterCustomerRequest {
  name: string;
  email: string;
  phone: string;
  password: string;
}

/** Identifier is an email for customer/admin login, a phone number for agent login (see AuthController). */
export interface LoginRequest {
  identifier: string;
  password: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface AuthTokensDto {
  userId: string;
  role: string;
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
}
