 
# Keues

Keues es un sistema de gestión de colas y tickets diseñado para negocios, clínicas, administraciones públicas y cualquier organización que necesite organizar la atención de clientes.

El objetivo del proyecto es ofrecer una solución moderna, rápida y sencilla, completamente desarrollada con .NET y React.

Web oficial y documentación: **[https://www.keues.dev](https://www.keues.dev)**

> ⚠️ Proyecto en desarrollo (MVP)

---

## Características

- Gestión de tipos de ticket.
- Emisión de tickets.
- Gestión de mostradores.
- Llamada al siguiente ticket.
- Dashboard de administración.
- Pantalla pública para mostrar el turno actual.
- Máquina de expedición de tickets.
- API REST documentada con Swagger.

---

## Arquitectura

El proyecto sigue una arquitectura basada en **Clean Architecture**.

```
API
│
├── Application
│
├── Domain
│
└── Infrastructure
```

### Capas

- **API** → Endpoints HTTP.
- **Application** → Casos de uso.
- **Domain** → Reglas de negocio y entidades.
- **Infrastructure** → Entity Framework Core, SQLite y servicios externos.

---

## Tecnologías

### Backend

- ASP.NET Core
- Entity Framework Core
- SQLite
- Clean Architecture

### Frontend (pendiente)

- React
- TypeScript

---

## Estado del proyecto

### Backend

- [x] Arquitectura inicial
- [x] Entity Framework
- [x] Migraciones
- [x] CRUD Ticket Types
- [ ] CRUD Tickets
- [ ] CRUD Counters
- [ ] CRUD Queues
- [ ] Sistema de llamada de tickets
- [ ] Dashboard

### Frontend

- [ ] Panel de administración
- [ ] Pantalla pública
- [ ] Máquina de tickets

---

## Roadmap

### MVP

- Gestión de tipos de ticket
- Gestión de tickets
- Gestión de mostradores
- Llamada al siguiente ticket
- Dashboard básico
- Pantalla pública

### V2

- Usuarios y autenticación
- Roles y permisos
- Estadísticas
- API Keys
- Temas personalizables
- Notificaciones
- Multiempresa

---

## Instalación

```bash
git clone https://github.com/tuusuario/Keues.git

cd Keues
```

Crear la base de datos:

```bash
dotnet ef database update \
  --project Keues.Infrastructure \
  --startup-project Keues.API
```

Ejecutar la API:

```bash
dotnet run --project Keues.API
```

Swagger:

```
https://localhost:xxxx/swagger
```

---

## Estructura del proyecto

```
Keues.API
Keues.Application
Keues.Domain
Keues.Infrastructure
```

---

## Licencia

Pendiente.