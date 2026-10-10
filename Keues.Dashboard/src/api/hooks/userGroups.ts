import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type {
  CreateUserGroupInput,
  UpdateUserGroupInput,
  UserGroupId,
} from '../interfaces/UserGroup/UserGroups';
import { userGroupsApi } from '../UserGroupsApi';
import { queryKeys } from '../queryKeys';

export function useUserGroups(locationId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.userGroups(locationId ?? ''),
    queryFn: () => userGroupsApi.list(locationId as string),
    enabled: locationId !== undefined,
    select: (response) => response.data,
  });
}

export function useUserGroup(userGroupId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.userGroup(userGroupId ?? ''),
    queryFn: () => userGroupsApi.get(userGroupId as UserGroupId),
    enabled: userGroupId !== undefined,
  });
}

export function useCreateUserGroup(locationId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: CreateUserGroupInput) => userGroupsApi.create(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.userGroups(locationId) }),
  });
}

export function useUpdateUserGroup(locationId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: UpdateUserGroupInput) => userGroupsApi.update(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.userGroups(locationId) }),
  });
}

export function useRemoveUserGroup(locationId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: UserGroupId) => userGroupsApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.userGroups(locationId) }),
  });
}
