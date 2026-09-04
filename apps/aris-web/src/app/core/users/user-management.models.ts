// Mirrors aris.IdentityService.Application.Users.{CreateUserRequestDto, CreateUserResponseDto}
// exactly — see src/Services/IdentityService/aris.IdentityService.Application/Users/.

export const SEEDED_ROLES = ['Administrator', 'Clinician', 'Coder', 'RiskAnalyst', 'Auditor', 'Researcher'] as const;

export type SeededRole = (typeof SEEDED_ROLES)[number];

export interface CreateUserRequest {
  username: string;
  email: string;
  password: string;
  displayName: string;
  roles: string[];
}

export interface CreateUserResponse {
  id: string;
  username: string;
  email: string;
  displayName: string;
  roles: string[];
  isActive: boolean;
}

export interface UserSummary {
  id: string;
  username: string;
  email: string;
  displayName: string;
  roles: string[];
  isActive: boolean;
}

export interface ListUsersResponse {
  items: UserSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
}
