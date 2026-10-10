import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ApiResponse } from '../interfaces/common/ApiResponse';
import type {
  CreateUserInput,
  ListUsersParams,
  UpdateUserInput,
  User,
} from '../interfaces/User/Users';
import { usersApi } from '../UsersApi';
import { queryKeys } from '../queryKeys';

export function useUsers(params: ListUsersParams, enabled = true) {
  return useQuery({
    queryKey: queryKeys.users(params.locationId, params),
    queryFn: () => usersApi.list(params),
    enabled,
    placeholderData: keepPreviousData,
  });
}

export function useCreateUser(locationId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: CreateUserInput) => usersApi.create(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.usersScope(locationId) }),
  });
}

export function useUpdateUser(locationId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: UpdateUserInput) => usersApi.update(input),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.usersScope(locationId) }),
  });
}

export function useRemoveUser(locationId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => usersApi.remove(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.usersScope(locationId) }),
  });
}

export function useSetUserEnabled(locationId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (variables: { id: string; isEnabled: boolean }) =>
      usersApi.setEnabled(variables.id, variables.isEnabled),
    onMutate: async (variables) => {
      await queryClient.cancelQueries({ queryKey: queryKeys.usersScope(locationId) });

      const previous = queryClient.getQueriesData<ApiResponse<User[]>>({
        queryKey: queryKeys.usersScope(locationId),
      });

      queryClient.setQueriesData<ApiResponse<User[]>>(
        { queryKey: queryKeys.usersScope(locationId) },
        (current) =>
          current
            ? {
                ...current,
                data: current.data.map((user) =>
                  user.id === variables.id ? { ...user, enabled: variables.isEnabled } : user
                ),
              }
            : current
      );

      return { previous };
    },
    onError: (_error, _variables, context) => {
      context?.previous?.forEach(([key, data]) => {
        queryClient.setQueryData(key, data);
      });
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: queryKeys.usersScope(locationId) }),
  });
}

const ALL_USERS_PAGE_LIMIT = 100;
const ALL_USERS_MAX_PAGES = 100;

async function fetchAllUsers(locationId: string): Promise<User[]> {
  const all: User[] = [];
  let page = 1;

  for (;;) {
    const response = await usersApi.list({
      locationId,
      page,
      limit: ALL_USERS_PAGE_LIMIT,
      sortOrder: 'asc',
    });

    all.push(...response.data);

    const totalPages = response.pagination?.totalPages ?? 1;
    if (
      response.data.length < ALL_USERS_PAGE_LIMIT ||
      page >= totalPages ||
      page >= ALL_USERS_MAX_PAGES
    ) {
      break;
    }

    page += 1;
  }

  return all;
}

export function useAllUsers(locationId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.allUsers(locationId ?? ''),
    queryFn: () => fetchAllUsers(locationId as string),
    enabled: locationId !== undefined,
  });
}
