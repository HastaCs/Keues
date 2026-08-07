import { createBrowserRouter, Navigate, RouterProvider } from 'react-router-dom';
import { AuthGuard, LoginGuard, RegisterAdminGuard } from './auth/Guards';
import { CountersPanel } from './features/counters/CountersPanel';
import { FlowsPanel } from './features/flows/FlowsPanel';
import { LocationDashboard } from './features/locations/LocationDashboard';
import { LocationListPage } from './features/locations/LocationListPage';
import { DeviceTicketMachinesPanel } from './features/devices/DeviceTicketMachinesPanel';
import { CounterDevicesPanel } from './features/devices/CounterDevicesPanel';
import { MonitorDevicesPanel } from './features/devices/MonitorDevicesPanel';
import { TicketsPanel } from './features/tickets/TicketsPanel';
import { QueuesPanel } from './features/queues/QueuePanel';
import { AppShellLayout } from './layout/AppShellLayout';
import { HomePage } from './pages/Home.page';
import Login from './components/Login/Login';
import RegisterAdmin from './components/RegisterAdmin/RegisterAdmin';
import ResetPassword from './components/ResetPassword/ResetPassword';

const router = createBrowserRouter([
  {
    path: '/',
    element: (
      <AuthGuard>
        <HomePage />
      </AuthGuard>
    ),
  },
  {
    path: '/locations',
    element: (
      <AuthGuard>
        <AppShellLayout />
      </AuthGuard>
    ),
    children: [
      {
        index: true,
        element: <LocationListPage />,
      },
      {
        path: ':locationId',
        children: [
          {
            index: true,
            element: <LocationDashboard />,
          },
          {
            path: 'tickets',
            element: <TicketsPanel />,
          },
          {
            path: 'counters',
            element: <CountersPanel />,
          },
          {
            path: 'ticketTypes',
            element: <QueuesPanel />,
          },
          {
            path: 'flows',
            element: <FlowsPanel />,
          },

          {
            path: 'deviceTicketMachines',
            element: <DeviceTicketMachinesPanel />,
          },
          {
            path: 'deviceCounters',
            element: <CounterDevicesPanel />,
          },
          {
            path: 'deviceMonitors',
            element: <MonitorDevicesPanel />,
          },
        ],
      },
    ],
  },
  {
    path: '/login',
    element: (
      <LoginGuard>
        <Login />
      </LoginGuard>
    ),
  },
  {
    path: '/register-admin',
    element: (
      <RegisterAdminGuard>
        <RegisterAdmin />
      </RegisterAdminGuard>
    ),
  },
  {
    path: '/reset-password',
    element: <ResetPassword />,
  },
  {
    path: '*',
    element: <Navigate to="/locations" replace />,
  },
]);

export function Router() {
  return <RouterProvider router={router} />;
}
