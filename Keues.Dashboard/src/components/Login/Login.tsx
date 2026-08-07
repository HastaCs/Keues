import {
  Alert,
  Anchor,
  Box,
  Button,
  Center,
  Checkbox,
  Container,
  Group,
  MantineProvider,
  Paper,
  PasswordInput,
  Stack,
  Text,
  TextInput,
  Title,
} from "@mantine/core";
import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { ApiError } from "@/api/httpClient";
import { usersApi } from "@/api/UsersApi";
import { useAuth } from "@/auth/AuthContext";

export default function Login() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [remember, setRemember] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [mode, setMode] = useState<"login" | "forgot">("login");
  const [forgotEmail, setForgotEmail] = useState("");
  const [forgotSent, setForgotSent] = useState(false);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      await login(email, password);
      navigate("/locations", { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t("login.failed"));
    } finally {
      setLoading(false);
    }
  };

  const handleForgotSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setForgotSent(false);

    try {
      await usersApi.forgotPassword({ email: forgotEmail });
      setForgotSent(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t("login.failed"));
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
            <Group grow w="100%" align="stretch">
              {/* Lado izquierdo */}
              <Center visibleFrom="md">
                <Stack maw={400}>
                  <Title order={1} size="3rem">
                    Keues
                  </Title>

                  <Text size="xl" c="dimmed">
                    {t("login.brandTagline")}
                  </Text>

                  <Text c="dimmed">{t("login.leftDescription")}</Text>
                </Stack>
              </Center>

              {/* Lado derecho */}
              <Center>
                <Paper shadow="md" radius="lg" p="xl" w={420} withBorder>
                  {mode === "login" ? (
                    <form onSubmit={handleSubmit}>
                      <Stack>
                        <Title order={2}>{t("login.title")}</Title>

                        <Text c="dimmed" size="sm">
                          {t("login.subtitle")}
                        </Text>

                        {error && <Alert color="red">{error}</Alert>}

                        <TextInput
                          required
                          type="email"
                          label={t("login.email")}
                          placeholder={t("login.emailPlaceholder")}
                          value={email}
                          onChange={(e) => setEmail(e.currentTarget.value)}
                        />

                        <PasswordInput
                          required
                          label={t("login.password")}
                          placeholder="********"
                          value={password}
                          onChange={(e) => setPassword(e.currentTarget.value)}
                        />

                        <Checkbox
                          label={t("login.remember")}
                          checked={remember}
                          onChange={(e) => setRemember(e.currentTarget.checked)}
                        />

                        <Button type="submit" fullWidth size="md" loading={loading}>
                          {t("login.submit")}
                        </Button>

                        <Anchor
                          ta="center"
                          size="sm"
                          href="#"
                          onClick={(e) => {
                            e.preventDefault();
                            setMode("forgot");
                            setError(null);
                            setForgotSent(false);
                          }}
                        >
                          {t("login.forgotPassword")}
                        </Anchor>
                      </Stack>
                    </form>
                  ) : (
                    <form onSubmit={handleForgotSubmit}>
                      <Stack>
                        <Title order={2}>{t("login.forgotTitle")}</Title>

                        <Text c="dimmed" size="sm">
                          {t("login.forgotDescription")}
                        </Text>

                        {forgotSent && <Alert color="green">{t("login.forgotSent")}</Alert>}
                        {error && <Alert color="red">{error}</Alert>}

                        <TextInput
                          required
                          type="email"
                          label={t("login.email")}
                          placeholder={t("login.emailPlaceholder")}
                          value={forgotEmail}
                          onChange={(e) => setForgotEmail(e.currentTarget.value)}
                        />

                        <Button type="submit" fullWidth size="md" loading={loading}>
                          {t("login.forgotSubmit")}
                        </Button>

                        <Anchor
                          ta="center"
                          size="sm"
                          href="#"
                          onClick={(e) => {
                            e.preventDefault();
                            setMode("login");
                            setError(null);
                          }}
                        >
                          {t("login.forgotBack")}
                        </Anchor>
                      </Stack>
                    </form>
                  )}
                </Paper>
              </Center>
            </Group>
          </Center>
        </Container>
      </Box>
    </MantineProvider>
  );
}