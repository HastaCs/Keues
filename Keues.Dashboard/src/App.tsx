import '@mantine/core/styles.css';
import '@mantine/notifications/styles.css';
import { localStorageColorSchemeManager, MantineProvider } from '@mantine/core';
import { Notifications } from '@mantine/notifications';
import { QueryClientProvider } from '@tanstack/react-query';
import { useState } from 'react';
import { AuthProvider } from './auth/AuthContext';
import { createQueryClient } from './query/queryClient';
import { Router } from './Router';
import { theme } from './theme';

const colorSchemeManager = localStorageColorSchemeManager({
  key: 'keues-color-scheme',
});

export default function App() {
  const [queryClient] = useState(createQueryClient);

  return (
    <MantineProvider
      theme={theme}
      colorSchemeManager={colorSchemeManager}
      defaultColorScheme="light"
    >
      <Notifications />
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <Router />
        </AuthProvider>
      </QueryClientProvider>
    </MantineProvider>
  );
}
