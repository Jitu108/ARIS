// Mirrors aris.IdentityService.Application.Authentication.{LoginRequestDto, LoginResponseDto, LoginUserDto}
// exactly — see src/Services/IdentityService/aris.IdentityService.Application/Authentication/.

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LogoutRequest {
  refreshToken: string;
}

export interface RefreshRequest {
  refreshToken: string;
}

export interface LoginUser {
  id: string;
  displayName: string;
  roles: string[];
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresInSeconds: number;
  user: LoginUser;
  mustChangePassword: boolean;
}

export interface ProblemDetails {
  type: string;
  title: string;
  status: number;
  detail?: string;
  traceId?: string;
}
