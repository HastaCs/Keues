import { Center, Loader } from '@mantine/core';
import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';

import { useAuth } from './AuthContext';

function FullScreenLoader() {
  return (
    <Center h="100vh">
      <Loader />
    </Center>
  );
}

export function AuthGuard({ children }: { children: ReactNode }) {
  const { status } = useAuth();

  if (status === 'loading') {
    return <FullScreenLoader />;
  }
  if (status === 'no-admin') {
    return <Navigate to="/register-admin" replace />;
  }
  if (status === 'unauthenticated') {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}

export function RegisterAdminGuard({ children }: { children: ReactNode }) {
  const { status } = useAuth();

  if (status === 'loading') {
    return <FullScreenLoader />;
  }
  if (status === 'authenticated') {
    return <Navigate to="/locations" replace />;
  }
  if (status === 'unauthenticated') {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}

export function LoginGuard({ children }: { children: ReactNode }) {
  const { status } = useAuth();

  if (status === 'loading') {
    return <FullScreenLoader />;
  }
  if (status === 'authenticated') {
    return <Navigate to="/locations" replace />;
  }
  if (status === 'no-admin') {
    return <Navigate to="/register-admin" replace />;
  }

  return <>{children}</>;
}
