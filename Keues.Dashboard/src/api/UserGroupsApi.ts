import { request } from './httpClient';

import type { ApiResponse } from './interfaces/common/ApiResponse';
import type {
  CreateUserGroupInput,
  UpdateUserGroupInput,
  UserGroup,
  UserGroupId,
} from './interfaces/UserGroup/UserGroups';

const endpoint = '/UserGroup';

export const userGroupsApi = {
  list(locationId: string) {
    return request<ApiResponse<UserGroup[]>>(`${endpoint}?locationId=${locationId}`);
  },

  get(id: UserGroupId) {
    return request<UserGroup>(`${endpoint}/${id}`);
  },

  create(input: CreateUserGroupInput) {
    return request<UserGroup>(endpoint, {
      method: 'POST',
      body: input,
    });
  },

  update(input: UpdateUserGroupInput) {
    return request<UserGroup>(`${endpoint}/${input.id}`, {
      method: 'PUT',
      body: {
        name: input.name,
        color: input.color,
        locationId: input.locationId,
      },
    });
  },

  remove(id: UserGroupId) {
    return request<void>(`${endpoint}/${id}`, {
      method: 'DELETE',
    });
  },
};
