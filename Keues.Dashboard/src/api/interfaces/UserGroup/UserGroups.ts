export type UserGroupId = string;

export interface UserGroupUser {
  id: string;
  name: string;
}

export interface UserGroup {
  id: UserGroupId;
  name: string;
  color: string;
  locationId: string;
  createdAt: string;
  userIds: UserGroupUser[];
}

export interface CreateUserGroupInput {
  name: string;
  color: string;
  locationId: string;
  userIds: string[];
}

export interface UpdateUserGroupInput extends CreateUserGroupInput {
  id: UserGroupId;
}
