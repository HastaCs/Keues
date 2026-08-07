import {
  Alert,
  Anchor,
  Box,
  Button,
  Center,
  Container,
  MantineProvider,
  Paper,
  PasswordInput,
  Stack,
  Text,
  Title,
} from "@mantine/core";
import { useState, type FormEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { ApiError } from "@/api/httpClient";
import { usersApi } from "@/api/UsersApi";

export default function ResetPassword() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token") ?? "";
  const email = searchParams.get("email") ?? "";

  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    if (password !== confirm) {
      setError(t("reset.passwordsMismatch"));
      setLoading(false);
      return;
    }

    try {
      await usersApi.resetPassword({ token, email, password });
      setDone(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t("reset.failed"));
    } finally {
      setLoading(false);
    }
  };

  return (
    <MantineProvider forceColorScheme="light">
      <Box
        style={{
          minHeight: "100vh",
          background: "var(--mantine-color-gray-0)",
        }}
      >
        <Container size="lg" h="100vh">
          <Center h="100%">
            <Paper shadow="md" radius="lg" p="xl" w={420} withBorder>
              {done ? (
                <Stack>
                  <Title order={2}>{t("reset.successTitle")}</Title>
                  <Text c="dimmed" size="sm">
                    {t("reset.successMessage")}
                  </Text>
                  <Button component={Link} to="/login" fullWidth size="md">
                    {t("reset.backToLogin")}
                  </Button>
                </Stack>
              ) : (
                <form onSubmit={handleSubmit}>
                  <Stack>
                    <Title order={2}>{t("reset.title")}</Title>

                    <Text c="dimmed" size="sm">
                      {t("reset.subtitle")}
                    </Text>

                    {!token && <Alert color="red">{t("reset.invalidToken")}</Alert>}

                    {error && <Alert color="red">{error}</Alert>}

                    <PasswordInput
                      required
                      label={t("reset.newPassword")}
                      placeholder="********"
                      value={password}
                      onChange={(e) => setPassword(e.currentTarget.value)}
                    />

                    <PasswordInput
                      required
                      label={t("reset.confirmPassword")}
                      placeholder="********"
                      value={confirm}
                      onChange={(e) => setConfirm(e.currentTarget.value)}
                    />

                    <Button type="submit" fullWidth size="md" loading={loading}>
                      {t("reset.submit")}
                    </Button>

                    <Anchor ta="center" size="sm" component={Link} to="/login">
                      {t("reset.backToLogin")}
                    </Anchor>
                  </Stack>
                </form>
              )}
            </Paper>
          </Center>
        </Container>
      </Box>
    </MantineProvider>
  );
}