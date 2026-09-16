export type UserGroupId = string;

export interface UserGroup {
  id: UserGroupId;
  name: string;
  color: string;
  locationId: string;
  createdAt: string;
}

export interface CreateUserGroupInput {
  name: string;
  color: string;
  locationId: string;
}

export interface UpdateUserGroupInput extends CreateUserGroupInput {
  id: UserGroupId;
}
