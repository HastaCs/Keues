import '@mantine/core/styles.css';
import '@mantine/notifications/styles.css';
import { localStorageColorSchemeManager, MantineProvider } from '@mantine/core';
import { Notifications } from '@mantine/notifications';
import { AuthProvider } from './auth/AuthContext';
import { Router } from './Router';
import { theme } from './theme';

const colorSchemeManager = localStorageColorSchemeManager({
  key: 'keues-color-scheme',
});

export default function App() {
  return (
    <MantineProvider
      theme={theme}
      colorSchemeManager={colorSchemeManager}
      defaultColorScheme="light"
    >
      <Notifications />
      <AuthProvider>
        <Router />
      </AuthProvider>
    </MantineProvider>
  );
}
