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
