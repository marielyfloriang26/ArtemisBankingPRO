# Módulo: Gestión de Comercios (Funcionalidades del API)

> Transcripción literal del PDF `Proyecto final_ Artemis Banking Pro (ABP) .pdf`, páginas 195–205 (sección "Funcionalidades del API").

Este módulo permite administrar los comercios registrados en el sistema desde la
Web API.

Desde estos endpoints, el usuario administrador podrá consultar comercios, obtener
el detalle de un comercio específico, crear nuevos comercios, actualizar sus datos y
activar o desactivar comercios existentes.

Los comercios serán utilizados por el procesador de pagos Hermes Pay y podrán
tener un usuario asociado con rol Comercio, creado desde el módulo de Gestión de
Usuarios.

## Seguridad

Todos los endpoints de este módulo requieren autenticación mediante JWT.

En cada solicitud debe enviarse el siguiente encabezado:

```
Authorization: Bearer {token_jwt}
```

Acceso restringido:

Solo los usuarios con rol Administrador pueden consumir los endpoints de este
módulo.

Si la solicitud no contiene un token JWT válido, la API debe responder con:

`401 Unauthorized`

Si el usuario autenticado no tiene rol Administrador, la API debe responder con:

`403 Forbidden`

---

## Obtener todos los comercios

### Endpoint

`GET /api/commerce`

### Descripción

Devuelve un listado paginado de comercios registrados en el sistema.

Por defecto, el listado debe mostrar los comercios activos, ordenados desde el más
reciente hasta el más antiguo.

Para mantener consistencia con los demás endpoints de listado, si no se envían
parámetros de paginación, el sistema debe utilizar page = 1 y pageSize = 20.

### Query Params

| Parámetro | Tipo de dato | Requerido | Valor por defecto | Descripción |
|---|---|---|---|---|
| page | int | No | 1 | Número de página que se desea consultar. |
| pageSize | int | No | 20 | Cantidad de registros por página. |
| status | string | No | activo | Estado de los comercios a consultar. Valores permitidos: activo, inactivo, todos. |

### Reglas

- El parámetro `page` debe ser mayor que cero.
- El parámetro `pageSize` debe ser mayor que cero.
- El valor máximo permitido para `pageSize` debe ser 20.
- El parámetro `status` solo puede tener los valores `activo`, `inactivo` o `todos`.
- Si no se envía `status`, deben retornar únicamente comercios activos.
- Los comercios deben ordenarse desde el más reciente hasta el más antiguo.

### Respuestas

| Código HTTP | Resultado | Descripción |
|---|---|---|
| 200 OK | Listado retornado | Retorna el listado paginado de comercios. |
| 400 Bad Request | Parámetros inválidos | Algún parámetro de consulta tiene un valor incorrecto. |
| 401 Unauthorized | No autenticado | Token ausente, inválido o expirado. |
| 403 Forbidden | Acceso denegado | El usuario autenticado no tiene rol Administrador. |

### Respuesta 200 OK

```json
{
"page": 1,
"pageSize": 20,
"totalRecords": 1,
"totalPages": 1,
"data": [
  {
   "id": 5,
   "name": "Tienda Demo",
   "description": "Comercio de prueba para pagos Hermes Pay",
   "email": "contacto@tiendademo.com",
   "phoneNumber": "8095551234",
   "rnc": "101999999",
   "isActive": true,
   "hasAssociatedUser": true,
   "createdAt": "2026-07-01T10:30:00"
  }
]
}
```

---

## Obtener comercio por ID

### Endpoint

`GET /api/commerce/{id}`

### Descripción

Devuelve la información detallada de un comercio específico según su identificador.

### Route Params

| Parámetro | Tipo de dato | Requerido | Descripción |
|---|---|---|---|
| id | int | Sí | Identificador del comercio que se desea consultar. |

### Respuestas

| Código HTTP | Resultado | Descripción |
|---|---|---|
| 200 OK | Detalle retornado | Retorna la información detallada del comercio. |
| 401 Unauthorized | No autenticado | Token ausente, inválido o expirado. |
| 403 Forbidden | Acceso denegado | El usuario autenticado no tiene rol de administrador. |
| 404 Not Found | No encontrado | El comercio indicado no existe. |

### Respuesta 200 OK

```json
{
"id": 5,
"name": "Tienda Demo",
"description": "Comercio de prueba para pagos Hermes Pay",
"email": "contacto@tiendademo.com",
"phoneNumber": "8095551234",
"rnc": "101999999",
"isActive": true,
"createdAt": "2026-07-01T10:30:00",
"associatedUser": {
  "id": "10",
  "userName": "commerce01",
  "email": "commerce01@artemis.com",
  "isActive": true
}
}
```

---

## Crear nuevo comercio

### Endpoint

`POST /api/commerce`

### Descripción

Crea un nuevo comercio en el sistema.

Este endpoint solo registra la información del comercio. El usuario con rol Comercio
debe crearse posteriormente desde el endpoint correspondiente del módulo de
Gestión de Usuarios.

### Request Body

```json
{
"name": "Tienda Demo",
"description": "Comercio de prueba para pagos Hermes Pay",
"email": "contacto@tiendademo.com",
"phoneNumber": "8095551234",
"rnc": "101999999"
}
```

### Campos del body

| Campo | Tipo de dato | Requerido | Descripción |
|---|---|---|---|
| name | string | Sí | Nombre comercial del comercio. |
| description | string | No | Descripción general del comercio. |
| email | string | Sí | Correo electrónico de contacto del comercio. |
| phoneNumber | string | Sí | Número telefónico del comercio. |
| rnc | string | Sí | Identificador fiscal o RNC del comercio. |

### Reglas

- El nombre del comercio es obligatorio.
- El correo electrónico es obligatorio.
- El correo electrónico debe tener un formato válido.
- El teléfono es obligatorio.
- El RNC es obligatorio.
- No debe existir otro comercio con el mismo RNC.
- No debe existir otro comercio con el mismo correo electrónico.
- El comercio debe crearse en estado Activo.
- El usuario administrador autenticado debe quedar registrado como responsable de la creación.

### Respuestas

| Código HTTP | Resultado | Descripción |
|---|---|---|
| 201 Created | Comercio creado | El comercio fue creado correctamente. |
| 400 Bad Request | Solicitud inválida | Datos faltantes o inválidos. |
| 401 Unauthorized | No autenticado | Token ausente, inválido o expirado. |
| 403 Forbidden | Acceso denegado | El usuario autenticado no tiene rol de administrador. |
| 409 Conflict | Conflicto | Ya existe un comercio con el mismo RNC o correo electrónico. |

### Respuesta 201 Created

```json
{
"id": 5,
"name": "Tienda Demo",
"description": "Comercio de prueba para pagos Hermes Pay",
"email": "contacto@tiendademo.com",
"phoneNumber": "8095551234",
"rnc": "101999999",
"isActive": true,
"createdAt": "2026-07-01T10:30:00"
}
```

---

## Actualizar comercio existente

### Endpoint

`PUT /api/commerce/{id}`

### Descripción

Actualiza los datos de un comercio existente.

Este endpoint no debe modificar el estado del comercio. Para activar o desactivar un
comercio debe utilizarse el endpoint de cambio de estado.

### Route Params

| Parámetro | Tipo de dato | Requerido | Descripción |
|---|---|---|---|
| id | int | Sí | Identificador del comercio que se desea actualizar. |

### Request Body

```json
{
"name": "Tienda Demo Actualizada",
"description": "Comercio actualizado para pagos Hermes Pay",
"email": "contacto.actualizado@tiendademo.com",
"phoneNumber": "8095555678",
"rnc": "101999999"
}
```

### Campos del body

| Campo | Tipo de dato | Requerido | Descripción |
|---|---|---|---|
| name | string | Sí | Nombre comercial del comercio. |
| description | string | No | Descripción general del comercio. |
| email | string | Sí | Correo electrónico de contacto del comercio. |
| phoneNumber | string | Sí | Número telefónico del comercio. |
| rnc | string | Sí | Identificador fiscal o RNC del comercio. |

### Reglas

- El comercio indicado debe existir.
- El nombre del comercio es obligatorio.
- El correo electrónico es obligatorio.
- El correo electrónico debe tener un formato válido.
- El teléfono es obligatorio.
- El RNC es obligatorio.
- El RNC no puede pertenecer a otro comercio.
- El correo electrónico no puede pertenecer a otro comercio.
- El estado del comercio no debe modificarse desde este endpoint.

### Respuestas

| Código HTTP | Resultado | Descripción |
|---|---|---|
| 204 No Content | Comercio actualizado | Los datos del comercio fueron actualizados correctamente. |
| 400 Bad Request | Solicitud inválida | Datos faltantes o inválidos. |
| 401 Unauthorized | No autenticado | Token ausente, inválido o expirado. |
| 403 Forbidden | Acceso denegado | El usuario autenticado no tiene rol de administrador. |
| 404 Not Found | No encontrado | El comercio indicado no existe. |
| 409 Conflict | Conflicto | El RNC o correo electrónico pertenece a otro comercio. |

---

## Cambiar estado de un comercio

### Endpoint

`PATCH /api/commerce/{id}/status`

### Descripción

Activa o desactiva un comercio según el valor enviado en el body.

Cuando un comercio se desactiva, todos los usuarios asociados a ese comercio
deben quedar inactivos.

Si posteriormente el comercio se reactiva, los usuarios asociados deben permanecer
inactivos. Para volver a utilizarlos, deberán completar el proceso de
restablecimiento de contraseña o activación definido por el sistema.

### Route Params

| Parámetro | Tipo de dato | Requerido | Descripción |
|---|---|---|---|
| id | int | Sí | Identificador del comercio al que se le cambiará el estado. |

### Request Body

```json
{
"status": true
}
```

### Campos del body

| Campo | Tipo de dato | Requerido | Descripción |
|---|---|---|---|
| status | boolean | Sí | Nuevo estado del comercio. true para activo, false para inactivo. |

### Reglas

- El comercio indicado debe existir.
- El campo `status` es obligatorio.
- Si `status` es `false`, el comercio debe quedar inactivo.
- Al desactivar un comercio, todos los usuarios asociados a ese comercio deben quedar inactivos.
- Si `status` es `true`, el comercio debe quedar activo.
- Al reactivar un comercio, los usuarios asociados no deben activarse automáticamente.
- Los usuarios asociados deben realizar el proceso de restablecimiento de contraseña para volver a quedar activos.
- Un comercio inactivo no debe poder procesar pagos mediante Hermes Pay.
- Cambiar el estado de un comercio no debe eliminar su historial ni sus transacciones.

### Respuestas

| Código HTTP | Resultado | Descripción |
|---|---|---|
| 204 No Content | Estado cambiado | El estado del comercio fue actualizado correctamente. |
| 400 Bad Request | Solicitud inválida | Body inválido o campo status faltante. |
| 401 Unauthorized | No autenticado | Token ausente, inválido o expirado. |
| 403 Forbidden | Acceso denegado | El usuario autenticado no tiene rol de administrador. |
| 404 Not Found | No encontrado | El comercio indicado no existe. |

---

## Reglas adicionales del módulo

- Todos los endpoints de este módulo requieren JWT.
- Solo los usuarios con rol Administrador pueden consumir estos endpoints.
- Los comercios deben listarse ordenados desde el más reciente hasta el más antiguo.
- Por defecto, el listado debe mostrar comercios activos.
- El listado debe estar paginado con `page = 1` y `pageSize = 20` por defecto.
- El tamaño máximo de página debe ser 20 registros.
- No debe existir más de un comercio con el mismo RNC.
- No debe existir más de un comercio con el mismo correo electrónico.
- Crear un comercio no crea automáticamente un usuario de comercio.
- El usuario de comercio debe crearse desde el endpoint `POST /api/users/commerce/{commerceId}`.
- Un comercio solo puede tener un usuario asociado.
- Un comercio inactivo no puede procesar pagos mediante Hermes Pay.
- Al desactivar un comercio, sus usuarios asociados deben inactivarse.
- Al reactivar un comercio, sus usuarios asociados deben permanecer inactivos hasta completar el proceso de restablecimiento de contraseña.
- Cambiar el estado de un comercio no debe eliminar su historial ni sus registros asociados.

---

## Nota de transcripción

A diferencia del módulo hermano "Gestión de Cuentas de Ahorro" (que sí incluye un
ejemplo literal `{"message": "Las cuentas principales no pueden ser canceladas."}`),
**este módulo no especifica ningún cuerpo de respuesta de error literal**. Las únicas
respuestas con forma JSON dada en el PDF son las de éxito: el listado 200 OK, el
detalle 200 OK y el 201 Created. Los textos de error solo aparecen como
descripciones en las tablas de respuestas (p. ej. "Ya existe un comercio con el
mismo RNC o correo electrónico."), que son la fuente más literal disponible para
derivar los mensajes de la implementación.
