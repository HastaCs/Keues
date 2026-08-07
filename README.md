[![Español](https://img.shields.io/badge/Español-README.md-2ea44f)](README.md) · [![English](https://img.shields.io/badge/English-README_EN.md-blue)](README_EN.md)

<p align="center">
  <img src="Keues.Dashboard/src/assets/logos/horizontal.png" alt="Keues" width="280">
</p>

<p align="center">
  <b>Sistema de gestión de colas y tickets</b> para negocios, clínicas, administraciones públicas y cualquier organización que necesite organizar la atención al cliente.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Version-1.0-green" alt="Version">
  <img src="https://img.shields.io/badge/Status-Stable-green" alt="Status">
  <img src="https://img.shields.io/badge/Web-www.keues.dev-1da1f2" alt="Website">
  <br>
  <img src="https://img.shields.io/badge/.NET-10-512BD4" alt=".NET">
  <img src="https://img.shields.io/badge/React-19-61DAFB" alt="React">
  <img src="https://img.shields.io/badge/TypeScript-7-3178C6" alt="TypeScript">
  <img src="https://img.shields.io/badge/Vite-8-646CFF" alt="Vite">
  <img src="https://img.shields.io/badge/Mantine-9-339AF0" alt="Mantine">
  <img src="https://img.shields.io/badge/License-MIT-green" alt="License: MIT">
</p>

<p align="center">
  Web oficial y documentación: <b><a href="https://www.keues.dev">https://www.keues.dev</a></b>
</p>

> ✅ **Versión 1.0.** API, dashboard y flujos de cola/tickets funcionales, con suite de tests automatizada (xUnit) y endpoints de administración protegidos por JWT. La impresión física de tickets en TicketMachine sigue en desarrollo.

---

## Tabla de contenidos

- [Cómo funciona](#cómo-funciona)
- [Ecosistema de repositorios](#ecosistema-de-repositorios)
- [Tipos de flujo](#tipos-de-flujo)
- [Características](#características)
- [Arquitectura](#arquitectura)
- [Tecnologías](#tecnologías)
- [Estructura del proyecto](#estructura-del-proyecto)
- [Instalación](#instalación)
- [Documentación de la API](#documentación-de-la-api)
- [Estado del proyecto](#estado-del-proyecto)
- [Licencia](#licencia)

---

## Cómo funciona

**Keues** se compone de una **API central (.NET)** que guarda todo el estado (localizaciones, colas, mostradores, tickets y dispositivos) y notifica los cambios **en tiempo real** a las pantallas mediante **SignalR**. A su alrededor hay cuatro aplicaciones que se conectan a ella:

```
                   ┌───────────────────────────────┐
                   │  Dashboard · React (admin)     │
                   └───────────────┬───────────────┘
                                   │ REST + JWT
                   ┌───────────────▼───────────────┐
                   │        Keues API · .NET 10     │
                   │    REST  +  SignalR /devices   │
                   └──────┬──────────────────┬──────┘
                          │                  │
             ┌────────────▼─────┐   ┌────────▼──────────┐
             │  TicketMachine   │   │  Monitors · TV     │
             │  (autoservicio)  │   │  (solo lectura)    │
             └──────────────────┘   └───────────────────┘
             ┌──────────────┐
             │  Counter     │
             │  (mostrador) │
             └──────────────┘
```

El ciclo de atención típico:

1. El cliente pulsa un servicio en la **máquina de tickets** y recibe su turno (p. ej. `P-001`).
2. El operador, en su **mostrador**, pulsa "Llamar siguiente turno".
3. La API elige el siguiente ticket según **prioridad, peso y envejecimiento** de las colas y lo emite por SignalR.
4. Los **monitores** (pantallas de TV) muestran el turno al instante, sin recargar la página.
5. Al terminar, el operador marca el ticket como atendido (o el puesto como libre) y los monitores se actualizan.

---

## Ecosistema de repositorios

| Repositorio | Qué es | Rol |
|---|---|---|
| [**Keues**](https://github.com/HastaCs/Keues) *(este)* | API REST + SignalR y Dashboard de administración | Corazón del sistema: guarda el estado y coordina todo |
| [**Keues-Counter**](https://github.com/HastaCs/Keues-Counter) | Aplicación de escritorio (Electron) para cada mostrador | El operador llama al siguiente turno, atiende, marca libre o hace llamadas manuales |
| [**Keues-Monitors**](https://github.com/HastaCs/Keues-Monitors) | Aplicación para pantallas/TV (Electron) | Muestra el turno actual, el último puesto libre y las llamadas manuales en tiempo real (solo lectura) |
| [**Keues-TicketMachine**](https://github.com/HastaCs/Keues-TicketMachine) | Máquina de autoservicio (Electron) | El cliente elige un servicio y recibe su número de ticket |

---

## Tipos de flujo

Cada localización define **flujos** (`Flow`) que determinan cómo se comportan las máquinas, mostradores y monitores asociados:

| Tipo | Uso | Comportamiento |
|---|---|---|
| `TicketMachine` | Carnicería, pescadería, frutería, bancos, clínicas… | El cliente saca un ticket en el terminal y el mostrador va llamando el siguiente turno |
| `SetFree` | Puestos sin tickets (estilo Carrefour) | El puesto indica que está libre y el monitor lo muestra |
| `ManualCall` | Puestos con números manuales | El operador sube/baja el número (+1 / −1 / +10 / −10) y el monitor lo muestra |

---

## Características

- **Multi-establecimiento (localizaciones):** cada local tiene sus propias colas, mostradores, flujos y dispositivos.
- **Colas inteligentes:** prioridad, **peso** (ratio de atención entre colas, p. ej. 1 ticket de A por cada 3 de B) y **envejecimiento** (aging) que sube automáticamente la prioridad de los tickets que llevan mucho esperando.
- **Mostradores:** cada puesto con nombre, código y color, asociado a una o varias colas.
- **Tickets:** emisión, llamada al siguiente, atender y cancelar, con control de numeración (`NextNumber` / `MaxValue`).
- **Tiempo real (SignalR):** los dispositivos se registran en el hub `/devices` y los monitores se actualizan al instante (`TicketCalled`, `TicketAttended`, `CounterFree`, `ReloadFlow`).
- **Dashboard de administración:** SPA en React + Mantine con gestión completa de localizaciones, colas, mostradores, flujos, tickets y dispositivos.
- **Seguridad:** autenticación JWT en cookie `HttpOnly`, creación del primer admin, login y recuperación de contraseña por **email (SMTP)**.
- **API documentada:** OpenAPI/Swagger generado automáticamente.
- **Despliegue sencillo:** una sola imagen Docker con el dashboard y la API, datos persistentes en `/app/data`.

---

## Arquitectura

El backend sigue una arquitectura **Clean Architecture** separada en capas con dependencias hacia dentro:

```
┌──────────────────────────────────────────────────────────┐
│  Keues.Dashboard   SPA React (administración web)          │
├──────────────────────────────────────────────────────────┤
│  Keues.API        Endpoints HTTP + hub SignalR + SPA (prod)│
├──────────────────────────────────────────────────────────┤
│  Keues.Application  Casos de uso (use cases)              │
├──────────────────────────────────────────────────────────┤
│  Keues.Domain       Entidades y reglas de negocio         │
├──────────────────────────────────────────────────────────┤
│  Keues.Infrastructure  EF Core · SQLite · JWT · SMTP      │
└──────────────────────────────────────────────────────────┘
```

---

## Tecnologías

### Backend

- ASP.NET Core (.NET 10)
- Entity Framework Core + SQLite
- SignalR (tiempo real)
- Autenticación JWT (cookie HttpOnly)
- OpenAPI / Swagger
- Pruebas: xUnit (use cases + integración HTTP con SQLite en memoria)

### Frontend

- React 19 + TypeScript
- Vite 8
- Mantine 9 + Tabler Icons
- React Router · i18next

### Despliegue

- Docker · Docker Compose

---

## Estructura del proyecto

```
Keues.sln
├── Keues.API/              # API REST + hub SignalR + SPA (producción)
├── Keues.Application/      # Casos de uso
├── Keues.Domain/           # Entidades y reglas de negocio
├── Keues.Infrastructure/   # EF Core, SQLite, JWT, email SMTP
├── Keues.Tests/            # Suite de tests (xUnit): use cases + API
├── Keues.Dashboard/        # SPA React (dashboard de administración)
├── scripts/                # utilidades (export-openapi.sh, etc.)
└── docs/                   # documentación generada (openapi.json)
```

---

## Instalación

### Docker (recomendado)

```bash
docker run -d \
  --name keues \
  -p 8080:8080 \
  -v "$(pwd)/data:/app/data" \
  gorerecord/keues:latest
```

Abre **http://localhost:8080**: en el primer arranque se crea `config.json` (con una clave JWT aleatoria) y la base de datos SQLite, y el dashboard te guiará para crear el **primer administrador**.

O con Docker Compose:

```yaml
services:
  keues:
    image: gorerecord/keues:latest
    container_name: keues
    ports:
      - "8080:8080"
    volumes:
      - ./data:/app/data
    environment:
      KEUES_DASHBOARD_URL: "http://localhost:8080"
    restart: unless-stopped
```

### Desarrollo local

Requisitos: **.NET 10 SDK** y **Node.js + pnpm**.

```bash
# 1) API (.NET 10)
dotnet run --project Keues.API
# → http://localhost:5125 · OpenAPI en http://localhost:5125/openapi/v1.json

# 2) Dashboard (React)
cd Keues.Dashboard
pnpm install
pnpm dev
```

> Nota: el proxy de Vite apunta a `http://localhost:8080` (configurado en `Keues.Dashboard/vite.config.mjs`). Si lanzas la API en otro puerto, ajusta esa línea.

La base de datos se crea y migra automáticamente en el arranque (`db.Database.Migrate()`), en `Keues.API/data/keues.db`.

---

## Documentación de la API

- En desarrollo, el documento OpenAPI está disponible en `http://localhost:5125/openapi/v1.json`.
- Puedes regenerarlo a `docs/openapi.json` con:

```bash
./scripts/export-openapi.sh
```

---

## Pruebas

La suite cubre los **use cases** (reglas de negocio: numeración de tickets, prioridad/aging/peso en la llamada, auth, etc.) y la **API** (integración HTTP real con `WebApplicationFactory`, incluyendo seguridad y entradas malformadas). Usa una base de datos **SQLite en memoria** con `dotnet test`:

```bash
dotnet test Keues.Tests
```

---

## Estado del proyecto

### Backend (API)

- [x] Arquitectura Clean Architecture
- [x] Entity Framework Core + SQLite + migraciones automáticas
- [x] CRUD de localizaciones, colas, mostradores, flujos, tickets y dispositivos
- [x] Emisión de tickets (`POST /api/queues/{id}/new-ticket`)
- [x] Llamada al siguiente ticket (prioridad, peso y envejecimiento)
- [x] Atender / cancelar / llamada manual / puesto libre
- [x] Notificaciones en tiempo real (SignalR)
- [x] Autenticación JWT + recuperación de contraseña por SMTP
- [x] Endpoints de administración protegidos (`[Authorize]`)
- [x] OpenAPI / Swagger
- [x] Suite de tests automatizada (xUnit): 130+ tests de use cases y API

### Dashboard

- [x] Creación del primer administrador, login y reset de contraseña
- [x] Gestión de localizaciones y dashboard por localización
- [x] Gestión de colas, mostradores, flujos y tickets
- [x] Gestión de dispositivos (máquinas, mostradores, monitores)
- [x] Multi-idioma (ES/EN) y tema claro/oscuro

### Clientes (Electron)

- [x] Keues-Counter: llamar siguiente, atender, libre, llamada manual
- [x] Keues-Monitors: turno actual, puesto libre, llamadas manuales
- [x] Keues-TicketMachine: menú de servicios y emisión de tickets
- [ ] Impresión física de tickets (impresora POS) en TicketMachine

---

## Licencia

Este proyecto se distribuye bajo la licencia **MIT**. Consulta el archivo [LICENSE](LICENSE).

---

Hecho con 💙 por [HastaCs](https://github.com/HastaCs).
