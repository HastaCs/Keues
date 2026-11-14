# QR virtual

Contrato backend para los flujos de tipo `QrVirtual`.

## Configuracion del flujo

- `FlowType.QrVirtual` tiene el valor numerico `3`.
- El flujo debe tener un unico `queueId` valido dentro de `flowJson`.
- El backend toma el primer `queueId` valido de un nodo `ticket`.
- El flujo se puede crear y guardar sin cola; se asigna despues desde el editor. Mientras no tenga una cola valida no se generan tokens QR.

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
  "url": "https://keues.example/qr/11111111-1111-1111-1111-111111111111?token=token-opaco-hmac"
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

La `url` se construye con `KEUES_DASHBOARD_URL`. Si esa configuracion esta vacia, el payload lleva una ruta relativa `/qr/{flowId}?token=...`.

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

## Notas

- Varios clientes pueden escanear el mismo QR mientras siga vigente. Cada escaneo crea un ticket.
- El endpoint no comprueba identidad ni ubicacion fisica del cliente.
- Si la API esta expuesta a Internet, el hub `/devices` deberia protegerse con una clave de dispositivo o autenticacion equivalente.
- La pagina `/qr/{flowId}?token=...` y la pantalla de la TicketMachine son partes de frontend pendientes.
