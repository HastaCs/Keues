import { request } from './httpClient';

import type {
  AuthInput,
  ForgotPasswordInput,
  HasAdminResponse,
  LoginInput,
  MeResponse,
  ResetPasswordInput,
} from './interfaces/User/Users';

const endpoint = '/users';

export const usersApi = {
  hasAdmin() {
    return request<HasAdminResponse>(`${endpoint}/has-admin`);
  },

  me() {
    return request<MeResponse>(`${endpoint}/me`);
  },

  createAdmin(input: AuthInput) {
    return request<MeResponse>(`${endpoint}/create-admin`, {
      method: 'POST',
      body: input,
    });
  },

  login(input: LoginInput) {
    return request<void>(`${endpoint}/login`, {
      method: 'POST',
      body: input,
    });
  },

  forgotPassword(input: ForgotPasswordInput) {
    return request<void>(`${endpoint}/forgot-password`, {
      method: 'POST',
      body: input,
    });
  },

  resetPassword(input: ResetPasswordInput) {
    return request<void>(`${endpoint}/reset-password`, {
      method: 'POST',
      body: input,
    });
  },

  logout() {
    return request<void>(`${endpoint}/logout`, {
      method: 'POST',
    });
  },
};
