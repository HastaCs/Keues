export interface HasAdminResponse {
  hasAdmin: boolean;
}

export interface MeResponse {
  id: string;
  name: string;
  email: string;
  role: 0 | 1;
}

export const UserRole = {
  Admin: 0,
  User: 1,
} as const;

export interface AuthInput {
  name: string;
  email: string;
  password: string;
}

export interface LoginInput {
  email: string;
  password: string;
}

export interface ForgotPasswordInput {
  email: string;
}

export interface ResetPasswordInput {
  token: string;
  email: string;
  password: string;
}

export type UserId = string;

export interface User {
  id: UserId;
  name: string;
  email: string;
  locationId: string | null;
  createdAt: string;
  enabled: boolean;
}

export interface CreateUserInput {
  name: string;
  email: string;
  password: string;
  locationId: string;
}

export interface UpdateUserInput extends CreateUserInput {
  id: UserId;
}

export interface UserMutationResult {
  id: UserId;
  name: string;
  email: string;
}

export interface ListUsersParams {
  locationId: string;
  name?: string;
  isActive?: boolean;
  page?: number;
  limit?: number;
  sortOrder?: 'asc' | 'desc';
}
