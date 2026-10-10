import { ApiError } from '@/api/httpClient';
import { QueryClient, type QueryClientConfig } from '@tanstack/react-query';

function shouldRetryQuery(failureCount: number, error: unknown): boolean {
  if (error instanceof ApiError && error.status >= 400 && error.status < 500) {
    return false;
  }

  return failureCount < 1;
}

export function createQueryClient(config: QueryClientConfig = {}) {
  return new QueryClient({
    ...config,
    defaultOptions: {
      ...config.defaultOptions,
      queries: {
        staleTime: 30_000,
        retry: shouldRetryQuery,
        refetchOnWindowFocus: true,
        ...config.defaultOptions?.queries,
      },
    },
  });
}
