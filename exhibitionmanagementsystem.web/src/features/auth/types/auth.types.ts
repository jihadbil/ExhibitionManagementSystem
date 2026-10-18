export interface LoginRequestDto {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface LoginResponseDto {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  userId: string;
  fullName: string;
  email: string;
  tenantID: number;
  tenantName: string;
  baseCurrency: string;
  roles: string[];
}

export interface CompanyRegistrationRequestDto {
  companyName: string;
  subdomain: string;
  adminFullName: string;
  adminEmail: string;
  adminPhoneNumber?: string;
  password: string;
  confirmPassword: string;
  baseCurrency: string;
  acceptTerms: boolean;
}

export interface CompanyRegistrationResponseDto {
  tenantID: number;
  companyName: string;
  subdomain: string;
  adminEmail: string;
  trialEndsAt: string;
  message: string;
}

export interface RefreshTokenRequestDto {
  accessToken: string;
  refreshToken: string;
}

export interface RefreshTokenResponseDto {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}
