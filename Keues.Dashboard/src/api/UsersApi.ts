import { request } from './httpClient';

import type { ApiResponse } from './interfaces/common/ApiResponse';
import type {
  AuthInput,
  CreateUserInput,
  ForgotPasswordInput,
  HasAdminResponse,
  ListUsersParams,
  LoginInput,
  MeResponse,
  ResetPasswordInput,
  UpdateUserInput,
  User,
  UserMutationResult,
} from './interfaces/User/Users';

const endpoint = '/users';

function buildListQuery(params: ListUsersParams): string {
  const query = new URLSearchParams();
  query.set('locationId', params.locationId);

  // El backend marca Name como requerido (string no-nullable); enviarlo vacío evita el 400.
  query.set('name', params.name ?? '');

  if (params.isActive !== undefined) {
    query.set('isActive', String(params.isActive));
  }

  if (params.page !== undefined) {
    query.set('page', String(params.page));
  }

  if (params.limit !== undefined) {
    query.set('limit', String(params.limit));
  }

  if (params.sortOrder !== undefined) {
    query.set('sortOrder', params.sortOrder);
  }

  return query.toString();
}

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

  list(params: ListUsersParams) {
    return request<ApiResponse<User[]>>(`${endpoint}?${buildListQuery(params)}`);
  },

  get(id: string) {
    return request<User>(`${endpoint}/${id}`);
  },

  create(input: CreateUserInput) {
    return request<UserMutationResult>(endpoint, {
      method: 'POST',
      body: input,
    });
  },

  update(input: UpdateUserInput) {
    return request<UserMutationResult>(`${endpoint}/${input.id}`, {
      method: 'PUT',
      body: {
        name: input.name,
        email: input.email,
        password: input.password,
        locationId: input.locationId,
      },
    });
  },

  setEnabled(id: string, isEnabled: boolean) {
    return request<void>(`${endpoint}/${id}/enable`, {
      method: 'POST',
      body: { isEnabled },
    });
  },

  remove(id: string) {
    return request<void>(`${endpoint}/${id}`, {
      method: 'DELETE',
    });
  },
};
