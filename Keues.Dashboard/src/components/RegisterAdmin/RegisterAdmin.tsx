import {
  Alert,
  Box,
  Button,
  Center,
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
import { useAuth } from "@/auth/AuthContext";

export default function RegisterAdmin() {
  const { t } = useTranslation();
  const { registerAdmin } = useAuth();
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [repeatPassword, setRepeatPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
      setError(t("register.invalidEmail"));
      return;
    }

    if (password !== repeatPassword) {
      setError(t("register.passwordsMismatch"));
      return;
    }

    setLoading(true);

    try {
      await registerAdmin(name, email, password);
      navigate("/locations", { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t("register.failed"));
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
                <Stack maw={420}>
                  <Title order={1} size="3rem">
                    {t("register.brandTitle")}
                  </Title>

                  <Text size="xl" c="dimmed">
                    {t("register.initialSetup")}
                  </Text>

                  <Text c="dimmed">
                    {t("register.noAdmin")}
                  </Text>

                  <Text c="dimmed">
                    {t("register.createFirstAccount")}
                  </Text>
                </Stack>
              </Center>

              {/* Formulario */}
              <Center>
                <Paper
                  shadow="md"
                  radius="lg"
                  p="xl"
                  w={460}
                  withBorder
                >
                  <form onSubmit={handleSubmit}>
                    <Stack>
                      <Title order={2}>
                        {t("register.title")}
                      </Title>

                      <Text c="dimmed" size="sm">
                        {t("register.subtitle")}
                      </Text>

                      {error && <Alert color="red">{error}</Alert>}

                      <TextInput
                        required
                        label={t("register.name")}
                        placeholder={t("register.namePlaceholder")}
                        value={name}
                        onChange={(e) => setName(e.currentTarget.value)}
                      />

                      <TextInput
                        required
                        type="email"
                        label={t("register.email")}
                        placeholder={t("register.emailPlaceholder")}
                        value={email}
                        onChange={(e) => setEmail(e.currentTarget.value)}
                      />

                      <PasswordInput
                        required
                        label={t("register.password")}
                        placeholder="********"
                        value={password}
                        onChange={(e) => setPassword(e.currentTarget.value)}
                      />

                      <PasswordInput
                        required
                        label={t("register.repeatPassword")}
                        placeholder="********"
                        value={repeatPassword}
                        onChange={(e) =>
                          setRepeatPassword(e.currentTarget.value)
                        }
                      />

                      <Button
                        type="submit"
                        fullWidth
                        size="md"
                        loading={loading}
                      >
                        {t("register.submit")}
                      </Button>
                    </Stack>
                  </form>
                </Paper>
              </Center>
            </Group>
          </Center>
        </Container>
      </Box>
    </MantineProvider>
  );
}
