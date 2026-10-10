import type { ListTicketsParams } from './interfaces/Tickets/Tickets';
import type { ListUsersParams } from './interfaces/User/Users';

export const queryKeys = {
  locations: () => ['locations'] as const,
  location: (locationId: string) => ['locations', 'detail', locationId] as const,
  dashboard: (locationId: string, date?: string) =>
    ['dashboard', locationId, date ?? null] as const,
  queues: (locationId: string) => ['queues', locationId] as const,
  counters: (locationId: string) => ['counters', locationId] as const,
  users: (locationId: string, filters: ListUsersParams) => ['users', locationId, filters] as const,
  usersScope: (locationId: string) => ['users', locationId] as const,
  allUsers: (locationId: string) => ['users', locationId, 'all'] as const,
  userGroups: (locationId: string) => ['userGroups', locationId] as const,
  userGroup: (userGroupId: string) => ['userGroups', 'detail', userGroupId] as const,
  flows: (locationId: string) => ['flows', locationId] as const,
  tickets: (locationId: string, filters: ListTicketsParams) =>
    ['tickets', locationId, filters] as const,
  ticketHistory: (ticketId: string) => ['tickets', 'history', ticketId] as const,
  devices: (deviceType: number, locationId: string) => ['devices', deviceType, locationId] as const,
  webhooks: () => ['webhooks', 'config'] as const,
  webhookEvents: () => ['webhooks', 'events'] as const,
};
