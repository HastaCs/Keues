import { Button, Group, Modal, Select, Stack } from '@mantine/core';
import { useTranslation } from 'react-i18next';

interface SettingsModalProps {
  opened: boolean;
  onClose: () => void;
}

export function SettingsModal({ opened, onClose }: SettingsModalProps) {
  const { t, i18n } = useTranslation();

  function handleLanguageSelect(value: string | null) {
    if (value === 'es' || value === 'en') {
      void i18n.changeLanguage(value);
    }
  }

  return (
    <Modal opened={opened} onClose={onClose} title={t('settings.title')} centered>
      <Stack gap="md">
        <Select
          label={t('common.language')}
          value={i18n.language === 'es' ? 'es' : 'en'}
          onChange={handleLanguageSelect}
          allowDeselect={false}
          data={[
            { value: 'es', label: t('settings.languageSpanish') },
            { value: 'en', label: t('settings.languageEnglish') },
          ]}
        />

        <Group justify="flex-end">
          <Button variant="default" onClick={onClose}>
            {t('common.cancel')}
          </Button>
        </Group>
      </Stack>
    </Modal>
  );
}
