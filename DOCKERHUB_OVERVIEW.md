# Keues

Keues is a queue & ticket management system for businesses, clinics, public administrations and any organization that needs to organize customer service. This image bundles the **Dashboard (React SPA)** and the **REST API (.NET)** in a single container.

Official website & documentation: **[https://www.keues.dev](https://www.keues.dev)**

## Quick start

```bash
docker run -d \
  --name keues \
  -p 8080:8080 \
  -v "$(pwd)/data:/app/data" \
  gorerecord/keues:latest
```

Open the dashboard at `http://localhost:8080`.

### First run

On first start the container creates a persistent data folder (`config.json` with a randomly generated JWT key, and the SQLite database `keues.db`). Open `http://localhost:8080` in your browser and the dashboard will guide you through creating the first admin account. After that you can configure the queue system from the dashboard.

## Docker Compose

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
      KEUES_EMAIL_PROVIDER: "smtp"
      KEUES_SMTP_HOST: ""
      KEUES_SMTP_PORT: "587"
      KEUES_SMTP_USER: ""
      KEUES_SMTP_PASSWORD: ""
      KEUES_SMTP_FROM: ""
      KEUES_SMTP_USE_TLS: "true"
    restart: unless-stopped
```

## Ports

| Port | Description                       |
|------|-----------------------------------|
| 8080 | Dashboard (SPA) and REST API      |

## Volumes

| Path          | Description                                        |
|---------------|----------------------------------------------------|
| `/app/data`   | Persistent data: `config.json` + SQLite `keues.db` |

> Mount this volume to keep your configuration and database between container restarts.

## Environment variables

All variables are optional. Empty variables fall back to the value stored in `config.json`.

| Variable                   | Description                                   | Default      |
|----------------------------|-----------------------------------------------|--------------|
| `KEUES_DASHBOARD_URL`      | Public URL of the dashboard (used in reset-password emails) | from `config.json` |
| `KEUES_JWT_KEY`            | Secret key for JWT signing                    | generated at first run |
| `KEUES_EMAIL_PROVIDER`     | Email provider (`smtp`)                       | `smtp`       |
| `KEUES_SMTP_HOST`          | SMTP server host                              | from `config.json` |
| `KEUES_SMTP_PORT`          | SMTP server port                              | from `config.json` |
| `KEUES_SMTP_USER`          | SMTP username                                 | from `config.json` |
| `KEUES_SMTP_PASSWORD`      | SMTP password                                 | from `config.json` |
| `KEUES_SMTP_FROM`          | Sender email address                          | from `config.json` |
| `KEUES_SMTP_USE_TLS`       | Use TLS for SMTP (`true`/`false`)             | from `config.json` |

## Features

- Ticket type management
- Ticket issuing
- Counter (desk) management
- Next-ticket calling
- Public display screen
- Ticket machine
- Admin dashboard
- REST API + SignalR

## Tech stack

- **Backend:** ASP.NET Core, Entity Framework Core, SQLite, SignalR
- **Frontend:** React, TypeScript, Vite

## Website & documentation

Official website and documentation: **[https://www.keues.dev](https://www.keues.dev)**

Full API reference and endpoint documentation are available on the website.

## License

Pending.
