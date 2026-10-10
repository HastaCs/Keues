# AGENTS.md — Keues.Dashboard

Guidance for AI coding agents working in this directory.

## Project overview

Keues is a queue/turn management system for in-person customer service (shops, clinics, banks, public administration). This directory is the **admin web SPA** (`Keues.Dashboard`). The backend lives in the parent monorepo: .NET 10 layered projects `Keues.API` (HTTP endpoints + real-time hub), `Keues.Application` (use cases), `Keues.Domain` (entities/rules), `Keues.Infrastructure` (EF Core + SQLite + JWT + SMTP), and `Keues.Tests` (xUnit).

Companion apps (separate repos): Keues-Counter (operator), Keues-Monitors (customer-facing screens), Keues-TicketMachine (kiosk).

## Stack

- React 19, TypeScript 7 (strict), Vite 8
- Mantine 9 (`@mantine/core`, `@mantine/hooks`, `@mantine/notifications`)
- `@xyflow/react` for the system map (`src/features/map/`)
- `react-router-dom` 7, `i18next` + `react-i18next` (es/en)
- `@tanstack/react-query` v5 (server state / caching)
- Vitest + React Testing Library, jsdom, `globals: true`
- Oxlint + Stylelint + Oxfmt (formatting/linting — NOT eslint/prettier)
- Storybook 10

## Commands

Package manager is **pnpm**. Always use `pnpm <script>` (never npm/yarn).

| Command | What it does |
|---|---|
| `pnpm dev` | Vite dev server (proxies `/api` to backend) |
| `pnpm build` | `tsc && vite build` |
| `pnpm typecheck` | `tsc --noEmit` |
| `pnpm lint` | oxlint + stylelint |
| `pnpm format:test` / `pnpm format:write` | oxfmt check / fix |
| `pnpm vitest` / `pnpm vitest:watch` | run tests / watch mode |
| `pnpm test` | FULL gate: typecheck → format:test → lint → vitest → build (slow) |
| `pnpm storybook` | Storybook dev server |

For fast iteration, run `pnpm typecheck`, `pnpm lint`, and `pnpm vitest` separately instead of the full `pnpm test` (it includes a production build).

## Git

Never create commits, amend, or push — the user commits manually. Do not run `git commit`/`git push` under any circumstance.

## Architecture & conventions

- **Routing**: all routes centralized in `src/Router.tsx`. Auth guards (`AuthGuard`, `LoginGuard`, `RegisterAdminGuard`) from `src/auth/Guards.tsx`.
- **Features**: one folder per domain under `src/features/<name>/` with `*Panel.tsx` (page panel) and `*FormModal.tsx` (create/edit modal). Shared UI in `src/components/`.
- **API layer**: `src/api/*Api.ts` — one object per entity exposing `list/get/create/update/remove`, built on `request<T>()` from `src/api/httpClient.ts`. Typed contracts in `src/api/interfaces/<Entity>/`. API calls throw `ApiError` (from `httpClient.ts`); extract messages with `getErrorMessage(error, fallback)` from `src/api/getErrorMessage.ts` (never raw `error.message` of unknown errors).
- **Data fetching (TanStack Query)**: `QueryClientProvider` is mounted in `src/App.tsx`; the client factory is `createQueryClient()` in `src/query/queryClient.ts` (`staleTime: 30s`, 1 retry except on 4xx, `refetchOnWindowFocus`). Query/mutation hooks live in `src/api/hooks/<entity>.ts` and wrap the `*Api` objects; centralized keys in `src/api/queryKeys.ts`. Mutations invalidate via `invalidateQueries` — do NOT add manual `reloadKey`/`refreshX()` callbacks. Paginated lists use `placeholderData: keepPreviousData`.
- **Import aliases**: `@/*` → `src/*`, `@test-utils` → `test-utils` (use `render` from there in tests).
- **i18n**: no hardcoded UI strings. All copy lives in `src/i18n/index.ts` under `es` and `en` resources (keep both in sync) and is consumed with `useTranslation()`.
- **Auth**: JWT in HttpOnly cookie. State via `useAuth()` from `src/auth/AuthContext.tsx` (statuses: `loading` / `no-admin` / `unauthenticated` / `authenticated`).
- **Active location**: most panels scope to the location selected via `useActiveLocation()` (`src/features/locations/LocationContext.tsx`).
- **Tests**: colocated with source as `*.test.ts(x)` (see `src/features/map/graphBuilder.test.ts`). Setup in `vitest.setup.mjs`; CSS modules are mocked with `identity-obj-proxy`.
- **Code style**: no comments in code; follow the existing patterns in neighboring files. TypeScript strict mode.

## Quirks (don't get confused by these)

- The dashboard `README.md` is the generic Mantine Vite template readme — it does NOT describe Keues. The real project docs are in the monorepo root `README.md` / `README_EN.md`.
- Vite dev proxy targets `http://localhost:5125` (see `vite.config.mjs`), which is the actual local API port. The root README mentioning 8080 refers to the Docker container setup.
- CI workflow (`.github/workflows/npm_test.yml`) uses yarn — outdated. The real package manager is pnpm (`pnpm-workspace.yaml`, `packageManager` field).
- `src/styles/` uses Mantine CSS modules + PostCSS preset (`postcss.config.cjs`); stylelint runs on `**/*.css`.
- Mantine color scheme is persisted under the localStorage key `keues-color-scheme`.
