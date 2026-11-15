# QR virtual

Contrato backend para los flujos de tipo `QrVirtual`.

## Configuracion del flujo

- `FlowType.QrVirtual` tiene el valor numerico `3`.
- El flujo debe tener un unico `queueId` valido dentro de `flowJson`.
- El backend toma el primer `queueId` valido de un nodo `ticket`.
- El backend exige una cola valida al crear o actualizar un flujo QR. Mientras no la tenga, no se generan tokens QR.

Ejemplo de `flowJson`:

```json
[
  {
    "nodeType": "ticket",
    "queueId": "11111111-1111-1111-1111-111111111111"
  }
]
```

## Conexion SignalR

La pantalla que muestra el QR debe conectarse al hub `/devices` como `TicketMachine` y usando el `flowId` del flujo QR:

```text
/devices?deviceId={deviceId}&name={name}&locationId={locationId}&type=TicketMachine&flowId={flowId}
```

El servidor la mete en el grupo:

```text
locationId:{locationId}:typeDevice:TicketMachine:flowId:{flowId}
```

Los monitores normales no reciben estos eventos.

## Evento `QrToken`

Al conectar, y despues cada 5 minutos, el servidor envia:

```json
{
  "flowId": "11111111-1111-1111-1111-111111111111",
  "locationId": "22222222-2222-2222-2222-222222222222",
  "token": "token-opaco-hmac",
  "rotatesAt": "2026-11-14T12:05:00+00:00",
  "expiresAt": "2026-11-14T12:10:00+00:00",
  "url": "https://keues.example/turno/11111111-1111-1111-1111-111111111111/token-opaco-hmac"
}
```

Campos:

- `flowId`: flujo QR.
- `locationId`: ubicacion del flujo.
- `token`: token opaco. No es un JWT y no se debe decodificar.
- `rotatesAt`: momento en el que la pantalla debe cambiar el QR.
- `expiresAt`: momento en el que el token deja de ser valido.
- `url`: contenido que debe codificarse dentro del QR.

La pantalla solo tiene que renderizar un QR con `url`.

## Configuracion de la URL publica

La `url` se construye con `keuesQrUrl`:

- en `data/config.json`:
  ```json
  {
    "dashboardUrl": "https://admin.example.com",
    "keuesQrUrl": "https://qr.example.com"
  }
  ```
- o con la variable de entorno:
  ```text
  KEUES_QR_URL=https://qr.example.com
  ```

Prioridad:

```text
KEUES_QR_URL
  > keuesQrUrl
  > dashboardUrl (si no es loopback)
  > IP LAN detectada + KEUES_PUBLIC_PORT
  > host de la primera TicketMachine + KEUES_PUBLIC_PORT
  > ruta relativa
```

Si no hay `keuesQrUrl` ni `KEUES_QR_URL`, se usa `dashboardUrl`.

Una `keuesQrUrl` o `KEUES_QR_URL` explicita se respeta siempre, incluso si es `localhost`, para desarrollo local.

Si no hay ninguna URL QR explicita, una `dashboardUrl` de loopback se ignora. En una ejecucion local, se detecta la IP LAN y se usa el puerto publico:

```text
http://192.168.1.50:8081/turno/{flowId}/{token}
```

En Docker, la deteccion automatica de IP no es fiable. Configura `KEUES_QR_URL` con la URL publica real. Si no se ha configurado y aun no se ha conectado ninguna TicketMachine, el payload lleva una ruta relativa `/turno/{flowId}/{token}`.

`KEUES_PUBLIC_PORT` (por defecto `8081`) es el puerto local del listener publico. No es la URL publica; sirve para que el middleware sepa que peticiones llegan por el puerto publico.

## Validez del token

- Se genera con HMAC-SHA256.
- Rota cada 5 minutos.
- El servidor acepta el token del periodo actual y el del periodo anterior.
- Un token puede seguir siendo valido hasta 10 minutos desde el inicio de su periodo.
- No se guarda en base de datos ni en memoria.
- La clave se deriva de `JwtKey` con el prefijo `keues-qr-v1:`.

## Emision del ticket

La pagina de escaneo obtiene el `queueId` del flujo y llama a:

```http
POST /api/queues/{queueId:guid}/qr-new-ticket
Content-Type: application/json

{
  "flowId": "11111111-1111-1111-1111-111111111111",
  "token": "token-opaco-hmac"
}
```

No requiere autenticacion de usuario. El token QR es la autorizacion.

Respuesta correcta:

```json
{
  "id": "33333333-3333-3333-3333-333333333333",
  "code": "C001"
}
```

Errores de validacion devuelven `400`:

```json
{
  "error": "QR token is invalid or expired"
}
```

Posibles errores:

- flujo inexistente,
- flujo no QR,
- flujo QR sin cola,
- `queueId` que no pertenece al flujo,
- token invalido o caducado.

## Despliegue con dos puertos

La misma aplicacion escucha en dos puertos:

```text
8080 -> dashboard, administracion, API completa, SignalR, TicketMachine
8081 -> pagina publica del QR y endpoints minimos
```

Los dos puertos solo son necesarios si se usa QR virtual. Si no se usa QR, basta con publicar el `8080`; el `8081` puede quedar sin publicar. El contenedor sigue escuchando en `8081` internamente, pero no es accesible desde fuera si no se publica ese puerto.

La separacion es una medida de seguridad: permite exponer solo la superficie publica del QR y mantener el dashboard, la administracion y la API completa en el `8080` privado.

El puerto `8081` solo permite:

```text
GET/HEAD  /turno
GET/HEAD  /turno/*
GET/HEAD  /assets/*
GET/HEAD  /favicon.ico
GET/HEAD  /api/flows/{flowId}
GET/HEAD  /api/tickets/{ticketId}
POST      /api/queues/{queueId}/qr-new-ticket
```

Todo lo demas devuelve `404`. El middleware usa `Connection.LocalPort` y el valor de `KEUES_PUBLIC_PORT`.

### Docker

```yaml
ports:
  - "8080:8080"   # privado
  - "8081:8081"   # publico
environment:
  KEUES_QR_URL: "https://qr.example.com"
  KEUES_PUBLIC_PORT: "8081"
```

No expongas `8080` a Internet. La TicketMachine y el dashboard deben ir por la red privada.

### LAN

```text
QR:     http://192.168.1.50:8081/turno/{flowId}/{token}
Admin:  http://192.168.1.50:8080
```

Si hay una WiFi de clientes, bloquea el `8080` en el firewall o ponlo en otra VLAN.

### Internet

Publica solo el `8081`, preferiblemente con HTTPS:

- Nginx/Traefik/Caddy: `proxy_pass http://keues:8081`.
- Cloudflare Tunnel: `service: http://keues:8081`.
- ngrok solo para pruebas.

No configures el proxy publico hacia `8080`: si el proxy traduce el puerto externo al 8080 interno, el gate de `Connection.LocalPort` no se aplica.

Se recomienda rate limiting en `/api/queues/*/qr-new-ticket`.

## Notas

- Varios clientes pueden escanear el mismo QR mientras siga vigente. Cada escaneo crea un ticket.
- El token es un bearer token: quien lo vea puede usarlo hasta que caduque.
- El endpoint no comprueba identidad ni ubicacion fisica del cliente.
- Si la API esta expuesta a Internet, el hub `/devices` deberia protegerse con una clave de dispositivo o autenticacion equivalente.
- La pagina `/turno/{flowId}/{token}` esta implementada en el dashboard y la pantalla QR esta implementada en Keues-TicketMachine.
