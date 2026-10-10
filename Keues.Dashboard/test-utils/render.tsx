import { MantineProvider } from '@mantine/core';
import { QueryClientProvider } from '@tanstack/react-query';
import { render as testingLibraryRender } from '@testing-library/react';
import { I18nextProvider } from 'react-i18next';
import { i18n } from '../src/i18n';
import { createQueryClient } from '../src/query/queryClient';
import { theme } from '../src/theme';

export function render(ui: React.ReactNode) {
  const queryClient = createQueryClient({
    defaultOptions: {
      queries: {
        gcTime: 0,
        refetchOnWindowFocus: false,
        retry: false,
        staleTime: 0,
      },
    },
  });

  return testingLibraryRender(ui, {
    wrapper: ({ children }: { children: React.ReactNode }) => (
      <I18nextProvider i18n={i18n}>
        <MantineProvider theme={theme} env="test">
          <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
        </MantineProvider>
      </I18nextProvider>
    ),
  });
}
