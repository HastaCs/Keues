import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';

import { usersApi } from '@/api/UsersApi';
import type { MeResponse } from '@/api/interfaces/User/Users';

type AuthStatus = 'loading' | 'no-admin' | 'unauthenticated' | 'authenticated';

interface AuthContextValue {
  status: AuthStatus;
  user: MeResponse | null;
  login: (email: string, password: string) => Promise<void>;
  registerAdmin: (name: string, email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [status, setStatus] = useState<AuthStatus>('loading');
  const [user, setUser] = useState<MeResponse | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function bootstrap() {
      try {
        const { hasAdmin } = await usersApi.hasAdmin();

        if (!hasAdmin) {
          if (!cancelled) {
            setStatus('no-admin');
          }
          return;
        }

        try {
          const me = await usersApi.me();

          if (!cancelled) {
            setUser(me);
            setStatus('authenticated');
          }
        } catch {
          if (!cancelled) {
            setStatus('unauthenticated');
          }
        }
      } catch {
        if (!cancelled) {
          setStatus('unauthenticated');
        }
      }
    }

    void bootstrap();

    return () => {
      cancelled = true;
    };
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    await usersApi.login({ email, password });

    const me = await usersApi.me();
    setUser(me);
    setStatus('authenticated');
  }, []);

  const registerAdmin = useCallback(async (name: string, email: string, password: string) => {
    await usersApi.createAdmin({ name, email, password });

    const me = await usersApi.me();
    setUser(me);
    setStatus('authenticated');
  }, []);

  const logout = useCallback(async () => {
    try {
      await usersApi.logout();
    } catch {
      // Ignoramos errores: aunque falle el endpoint, cerramos sesión localmente.
    }

    setUser(null);
    setStatus('unauthenticated');
  }, []);

  const value = useMemo(
    () => ({ status, user, login, registerAdmin, logout }),
    [status, user, login, registerAdmin, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);

  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }

  return context;
}
