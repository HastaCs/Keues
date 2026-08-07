import { MantineProvider } from '@mantine/core';
import { render as testingLibraryRender } from '@testing-library/react';
import { I18nextProvider } from 'react-i18next';
import { i18n } from '../src/i18n';
import { theme } from '../src/theme';

export function render(ui: React.ReactNode) {
  return testingLibraryRender(ui, {
    wrapper: ({ children }: { children: React.ReactNode }) => (
      <I18nextProvider i18n={i18n}>
        <MantineProvider theme={theme} env="test">
          {children}
        </MantineProvider>
      </I18nextProvider>
    ),
  });
}
