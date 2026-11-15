# Keues

Keues is a queue & ticket management system for businesses, clinics, public administrations and any organization that needs to organize customer service. This image bundles the **Dashboard (React SPA)** and the **REST API (.NET)** in a single container.

Official website & documentation: **[https://www.keues.dev](https://www.keues.dev)**

## Quick start

```bash
docker run -d \
  --name keues \
  -p 8080:8080 \
  -p 8081:8081 \
  -e KEUES_QR_URL="http://YOUR_HOST_IP:8081" \
  -v "$(pwd)/data:/app/data" \
  gorerecord/keues:latest
```

- Dashboard: `http://localhost:8080`
- Public QR page: `http://YOUR_HOST_IP:8081/turno/{flowId}/{token}`

> `8080` is the administration/full API port. Keep it private.
>
> `8081` is the public QR port. It is the only port customers should be able to reach.

### First run

On first start the container creates a persistent data folder (`config.json` with a randomly generated JWT key, and the SQLite database `keues.db`). Open `http://localhost:8080` in your browser and the dashboard will guide you through creating the first admin account.

## Ports

| Port | What it serves | Exposure |
|------|----------------|----------|
| `8080` | Dashboard, admin API, SignalR hub, TicketMachine and internal operations | **Private** |
| `8081` | Public QR page and public QR ticket endpoints | **Public** |

Both ports belong to the same container and the same process. They are not two separate applications.

### Do I need both ports?

Only if you use the **QR virtual queue**. If you do not use QR, you can publish only `8080` and leave `8081` unpublished. The container still listens internally on both ports, but `8081` is not reachable unless you publish it and is not needed for the normal dashboard/API/TicketMachine installation.

The two-port split is a security boundary for QR installations:

```text
8080 -> full admin/API surface, private
8081 -> only the public QR surface, public
```

Customers need to reach the QR page, but they must not reach the admin dashboard, counters, users or devices. Publishing only `8081` keeps the full API private.

### What the public port allows

Only:

```text
/turno/*
/assets/*
/api/flows/{flowId}
/api/queues/{queueId}/qr-new-ticket
/api/tickets/{ticketId}
```

Everything else returns `404`.

The admin dashboard, `/api/locations`, `/api/counters/*`, `/api/users/*`, `/devices` and all other internal endpoints are only available on `8080`.

## Virtual queue QR

A QR flow has exactly one associated queue. The TicketMachine receives a rotating token and displays a QR with a URL like:

```text
https://qr.example.com/turno/{flowId}/{token}
```

The token rotates every 5 minutes. The server accepts the current token and the previous one, so a QR remains valid for up to 10 minutes. The token is an opaque HMAC token, not a JWT.

Set the public QR URL explicitly:

```yaml
KEUES_QR_URL: "https://qr.example.com"
```

or in `data/config.json`:

```json
{
  "keuesQrUrl": "https://qr.example.com"
}
```

If `KEUES_QR_URL` is empty, the application tries to auto-detect the LAN address on a host deployment. In Docker, set it explicitly.

### LAN

For a local network installation:

```text
QR page:   http://192.168.1.133:8081
Admin:     http://192.168.1.133:8080
```

- Customers only need `8081`.
- Keep `8080` on the internal network, a VPN, or behind a firewall rule.
- Do not expose `8080` on a guest/customer WiFi.
- The TicketMachine must reach `8080` on the trusted network.

### Internet

Do **not** publish `8080` to the Internet. Publish only `8081`, preferably over HTTPS, using one of these:

- a reverse proxy (Nginx, Traefik, Caddy),
- Cloudflare Tunnel,
- ngrok for testing only.

#### Nginx example

```nginx
server {
    listen 443 ssl;
    server_name qr.example.com;

    # ... TLS configuration ...

    location / {
        proxy_pass http://keues:8081;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }
}
```

Point the proxy to `8081`, not `8080`. The app applies its own public-path restrictions on `8081`.

#### Cloudflare Tunnel example

```yaml
ingress:
  - hostname: qr.example.com
    service: http://keues:8081
  - service: http_status:404
```

Do not route the public hostname to `8080`. Use VPN, Cloudflare Access or a private hostname for the dashboard.

### Security notes

- The QR token is a bearer token: anyone who sees it can use it until it expires.
- Add rate limiting at the proxy for `/api/queues/*/qr-new-ticket`.
- The public QR page does not require login; the admin dashboard does.
- Keep `8080` private even if you use the two-port image. The port separation only protects the API if the admin port is not reachable from customer networks.

## Docker Compose

```yaml
services:
  keues:
    image: gorerecord/keues:latest
    container_name: keues
    ports:
      # Admin/API. Keep private or firewall it.
      - "8080:8080"
      # Public QR page. This is the only port customers need.
      - "8081:8081"
    volumes:
      - ./data:/app/data
    environment:
      KEUES_DASHBOARD_URL: "http://YOUR_HOST_IP:8080"
      KEUES_QR_URL: "http://YOUR_HOST_IP:8081"
      KEUES_PUBLIC_PORT: "8081"
      KEUES_EMAIL_PROVIDER: "smtp"
      KEUES_EMAIL_FROM: ""
      KEUES_EMAIL_FROM_NAME: "Keues"
      KEUES_SMTP_HOST: ""
      KEUES_SMTP_PORT: "587"
      KEUES_SMTP_USER: ""
      KEUES_SMTP_PASSWORD: ""
      KEUES_SMTP_FROM: ""
      KEUES_SMTP_USE_TLS: "true"
      KEUES_MAILJET_API_KEY: ""
      KEUES_MAILJET_SECRET_KEY: ""
    restart: unless-stopped
```

## Volumes

| Path | Description |
|------|-------------|
| `/app/data` | Persistent data: `config.json` + SQLite `keues.db` |

> Mount this volume to keep your configuration and database between container restarts.

## Environment variables

All variables are optional. Empty variables fall back to the value stored in `config.json`.

| Variable | Description | Default |
|----------|-------------|---------|
| `KEUES_DASHBOARD_URL` | Public URL of the admin dashboard (used in password-reset emails) | from `config.json` |
| `KEUES_QR_URL` | Public URL encoded in the QR page | auto-detected on host; set explicitly in Docker |
| `KEUES_PUBLIC_PORT` | Local port used for the public QR listener/gate | `8081` |
| `KEUES_JWT_KEY` | Secret key for JWT signing | generated at first run |
| `KEUES_EMAIL_PROVIDER` | Email provider (`smtp` or `mailjet`) | `smtp` |
| `KEUES_EMAIL_FROM` | Sender address shared by every provider | from `config.json` |
| `KEUES_EMAIL_FROM_NAME` | Sender display name shared by every provider | `Keues` |
| `KEUES_SMTP_HOST` | SMTP server host | from `config.json` |
| `KEUES_SMTP_PORT` | SMTP server port | from `config.json` |
| `KEUES_SMTP_USER` | SMTP username | from `config.json` |
| `KEUES_SMTP_PASSWORD` | SMTP password | from `config.json` |
| `KEUES_SMTP_FROM` | SMTP-only sender override (backwards compatibility) | from `config.json` |
| `KEUES_SMTP_USE_TLS` | Use TLS for SMTP (`true`/`false`) | from `config.json` |
| `KEUES_MAILJET_API_KEY` | Mailjet API key (Basic auth user) | from `config.json` |
| `KEUES_MAILJET_SECRET_KEY` | Mailjet secret key (Basic auth password) | from `config.json` |

`KEUES_PUBLIC_PORT` is not the public URL. It is the local port the app uses to identify public traffic. Change it only if you also change the Kestrel/Docker port mapping.

## Features

- Ticket type management
- Ticket issuing
- Counter (desk) management
- Next-ticket calling
- Public display screen
- Ticket machine
- Rotating QR virtual queue
- Admin dashboard
- REST API + SignalR

## Tech stack

- **Backend:** ASP.NET Core, Entity Framework Core, SQLite, SignalR
- **Frontend:** React, TypeScript, Vite

## Website & documentation

Official website and documentation: **[https://www.keues.dev](https://www.keues.dev)**

Full API reference and endpoint documentation are available on the website.

## License

MIT
