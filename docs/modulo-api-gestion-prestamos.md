# Módulo: Gestión de Préstamos (Funcionalidades del API)

> Transcripción literal del PDF `Proyecto final_ Artemis Banking Pro (ABP) .pdf`, páginas 165–176 (sección "Funcionalidades del API").

Este módulo permite administrar los préstamos desde la Web API.

Desde estos endpoints, el usuario administrador podrá consultar préstamos, asignar
nuevos préstamos a clientes, visualizar el detalle de un préstamo con su tabla de
amortización y modificar la tasa de interés anual de un préstamo activo.

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

## Obtener listado de préstamos

### Endpoint

`GET /api/loan`

### Descripción

Obtiene un listado paginado de los préstamos registrados en el sistema.
Por defecto, el listado debe mostrar los préstamos activos, ordenados desde el más
reciente hasta el más antiguo.

El endpoint también debe permitir filtrar por estado y buscar préstamos asociados a
un cliente mediante su cédula.

### Query Params

| Parámetro | Tipo de dato | Requerido | Valor por defecto | Descripción |
|---|---|---|---|---|
| page | int | No | 1 | Número de página que se desea consultar. |
| pageSize | int | No | 20 | Cantidad de registros por página. |
| status | string | No | activos | Estado de los préstamos a consultar. Valores permitidos: activos, completados, todos. |
| identification | string | No | null | Cédula del cliente para buscar sus préstamos. |

### Reglas

- El parámetro `page` debe ser mayor que cero.
- El parámetro `pageSize` debe ser mayor que cero.
- El valor máximo permitido para `pageSize` debe ser 20.
- El parámetro `status` solo puede tener los valores `activos`, `completados` o `todos`.
- Si se envía `identification`, el sistema debe buscar los préstamos asociados al cliente correspondiente.
- Si se busca por cédula y no se especifica `status`, deben mostrarse primero los préstamos activos y luego los completados.
- Dentro de cada grupo, los préstamos deben mostrarse del más reciente al más antiguo.

### Respuestas

| Código HTTP | Resultado | Descripción |
|---|---|---|
| 200 OK | Listado retornado | Retorna el listado paginado de préstamos. |
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
   "id": "1",
   "loanNumber": "987654321",
   "clientId": "20",
   "clientFullName": "María Gómez",
   "capitalAmount": 100000.00,
   "totalInstallments": 12,
   "paidInstallments": 3,
   "pendingAmount": 76250.00,
   "annualInterestRate": 12.00,
   "termInMonths": 12,
   "status": "Activo",
   "clientPaymentStatus": "Al día",
   "createdAt": "2026-07-01T10:30:00"
  }
]
}
```

---

## Asignar préstamo a cliente

### Endpoint

`POST /api/loan`

### Descripción

Crea un nuevo préstamo para un cliente activo, genera automáticamente su tabla de
amortización, acredita el monto aprobado en la cuenta de ahorro principal del
cliente y registra la transacción correspondiente.

Un cliente solo puede tener un préstamo activo a la vez.

### Request Body

```json
{
"clientId": "20",
"capitalAmount": 100000.00,
"termInMonths": 12,
"annualInterestRate": 12.00,
"confirmHighRisk": false
}
```

### Campos del body

| Campo | Tipo de dato | Requerido | Descripción |
|---|---|---|---|
| clientId | string | Sí | Identificador del cliente al que se asignará el préstamo. |
| capitalAmount | decimal | Sí | Monto de capital aprobado para el préstamo. |
| termInMonths | int | Sí | Plazo del préstamo expresado en meses. |
| annualInterestRate | decimal | Sí | Tasa de interés anual aplicada al préstamo. |
| confirmHighRisk | boolean | No | Indica si el administrador confirma la asignación aunque el cliente sea de alto riesgo. |

### Reglas

- Todos los campos son obligatorios, excepto `confirmHighRisk`.
- El cliente debe existir.
- El cliente debe estar activo.
- El cliente no debe tener un préstamo activo actualmente.
- El cliente debe tener una cuenta de ahorro principal activa.
- El monto del préstamo debe ser mayor que cero.
- La tasa de interés anual no puede ser negativa.
- El plazo debe ser uno de los valores permitidos.
- Antes de crear el préstamo, el sistema debe validar si el cliente es o se convierte en cliente de alto riesgo.

El campo `termInMonths` solo puede recibir los siguientes valores:

- 6
- 12
- 18
- 24
- 30
- 36
- 42
- 48
- 54
- 60

### Validación de cliente de alto riesgo

El sistema debe calcular la deuda promedio de los clientes activos.

La deuda promedio debe tomar en cuenta préstamos activos y deudas de tarjetas
de crédito activas.

El sistema debe considerar al cliente como de alto riesgo si ocurre cualquiera de
estos casos:

- La deuda actual del cliente supera la deuda promedio del sistema.
- La deuda proyectada del cliente, incluyendo el nuevo préstamo, supera la deuda promedio del sistema.

La deuda proyectada debe calcularse sumando:

`Deuda actual del cliente + Total a pagar del nuevo préstamo`

El total a pagar del nuevo préstamo corresponde a la suma de todas las cuotas
generadas en la tabla de amortización.

Si el cliente es o se convierte en cliente de alto riesgo y el campo `confirmHighRisk`
no fue enviado en `true`, la API debe responder con:

`409 Conflict`

Esta respuesta debe permitir que el consumidor de la API conozca la razón del
conflicto y, si desea continuar, vuelva a enviar la solicitud con `confirmHighRisk` en
`true`.

### Respuesta 409 Conflict

```json
{
"message": "Asignar este préstamo convertirá al cliente en un cliente de alto riesgo, ya que su deuda superará el umbral promedio del sistema.",
"riskType": "ProjectedHighRisk",
"currentDebt": 25000.00,
"projectedDebt": 132500.00,
"averageDebt": 80000.00
}
```

Si el administrador envía `confirmHighRisk` en `true`, el sistema debe permitir la
creación del préstamo aunque el cliente sea considerado de alto riesgo.

### Procesamiento del préstamo

Si todas las validaciones son correctas, el sistema debe:

- Crear el préstamo en estado Activo.
- Generar un número de préstamo único de 9 dígitos.
- Generar automáticamente la tabla de amortización.
- Acreditar el monto aprobado a la cuenta de ahorro principal del cliente.
- Registrar una transacción de tipo CRÉDITO en la cuenta principal del cliente.
- Asociar el préstamo al usuario administrador autenticado que realizó la asignación.
- Enviar un correo electrónico al cliente notificando la aprobación del préstamo.

El número de préstamo debe cumplir las siguientes reglas:

- Debe tener exactamente 9 dígitos.
- Debe ser único en el sistema.
- No debe repetirse como número de préstamo.
- No debe repetirse como número de cuenta de ahorro.
- Debe almacenarse como texto para evitar pérdida de ceros iniciales.

### Respuestas

| Código HTTP | Resultado | Descripción |
|---|---|---|
| 201 Created | Préstamo creado | El préstamo fue creado y la tabla de amortización fue generada. |
| 400 Bad Request | Solicitud inválida | Datos incompletos, inválidos o cliente ya tiene un préstamo activo. |
| 401 Unauthorized | No autenticado | Token ausente, inválido o expirado. |
| 403 Forbidden | Acceso denegado | El usuario autenticado no tiene rol de administrador. |
| 404 Not Found | No encontrado | El cliente indicado no existe. |
| 409 Conflict | Alto riesgo | El cliente es o se convierte en cliente de alto riesgo y no se confirmó la asignación. |

### Respuesta 201 Created

```json
{
"id": "1",
"loanNumber": "987654321",
"clientId": "20",
"clientFullName": "María Gómez",
"capitalAmount": 100000.00,
"termInMonths": 12,
"annualInterestRate": 12.00,
"monthlyInstallment": 8884.88,
"totalAmountToPay": 106618.56,
"status": "Activo",
"createdAt": "2026-07-01T10:30:00"
}
```

---

## Obtener detalle de préstamo y tabla de amortización

### Endpoint

`GET /api/loan/{id}`

### Descripción

Obtiene el detalle de un préstamo específico y su tabla de amortización.

### Route Params

| Parámetro | Tipo de dato | Requerido | Descripción |
|---|---|---|---|
| id | string | Sí | Identificador del préstamo que se desea consultar. |

### Respuestas

| Código HTTP | Resultado | Descripción |
|---|---|---|
| 200 OK | Detalle retornado | Retorna la información del préstamo y su tabla de amortización. |
| 401 Unauthorized | No autenticado | Token ausente, inválido o expirado. |
| 403 Forbidden | Acceso denegado | El usuario autenticado no tiene rol Administrador. |
| 404 Not Found | No encontrado | El préstamo indicado no existe. |

### Respuesta 200 OK

```json
{
"id": "1",
"loanNumber": "987654321",
"clientId": "20",
"clientFullName": "María Gómez",
"capitalAmount": 100000.00,
"annualInterestRate": 12.00,
"termInMonths": 12,
"monthlyInstallment": 8884.88,
"pendingAmount": 76250.00,
"status": "Activo",
"clientPaymentStatus": "Al día",
"createdAt": "2026-07-01T10:30:00",
"amortization": [
  {
   "installmentNumber": 1,
   "dueDate": "2026-08-01",
   "installmentAmount": 8884.88,
   "interestAmount": 1000.00,
   "capitalAmount": 7884.88,
   "pendingInstallmentAmount": 0.00,
   "paymentStatus": "Pagada",
   "isLate": false
  },
  {
   "installmentNumber": 2,
   "dueDate": "2026-09-01",
   "installmentAmount": 8884.88,
   "interestAmount": 921.15,
   "capitalAmount": 7963.73,
   "pendingInstallmentAmount": 8884.88,
   "paymentStatus": "Pendiente",
   "isLate": false
  }
]
}
```

---

## Editar tasa de interés de préstamo

### Endpoint

`PATCH /api/loan/{id}/rate`

### Descripción

Permite modificar la tasa de interés anual de un préstamo activo.

Al actualizar la tasa, el sistema debe recalcular únicamente las cuotas futuras
pendientes. Las cuotas pagadas, parcialmente pagadas, vencidas o con fecha de
vencimiento igual o anterior a la fecha actual no deben modificarse.

### Route Params

| Parámetro | Tipo de dato | Requerido | Descripción |
|---|---|---|---|
| id | string | Sí | Identificador del préstamo que se desea modificar. |

### Request Body

```json
{
"annualInterestRate": 10.50
}
```

### Campos del body

| Campo | Tipo de dato | Requerido | Descripción |
|---|---|---|---|
| annualInterestRate | decimal | Sí | Nueva tasa de interés anual que será aplicada al préstamo. |

### Reglas

- El préstamo debe existir.
- El préstamo debe estar activo.
- La tasa de interés anual es obligatoria.
- La tasa de interés anual no puede ser negativa.
- Debe existir al menos una cuota futura pendiente para poder recalcular.
- Solo se deben recalcular cuotas futuras pendientes.
- Las cuotas pagadas no deben modificarse.
- Las cuotas vencidas no deben modificarse.
- Las cuotas parcialmente pagadas no deben modificarse.
- Las cuotas con fecha de vencimiento igual o anterior a la fecha actual no deben modificarse.
- Después de actualizar la tasa, el sistema debe enviar un correo electrónico al cliente notificando el cambio.

### Respuestas

| Código HTTP | Resultado | Descripción |
|---|---|---|
| 204 No Content | Tasa actualizada | La tasa fue actualizada y las cuotas futuras fueron recalculadas. |
| 400 Bad Request | Solicitud inválida | Tasa no proporcionada, tasa inválida o no existen cuotas futuras pendientes. |
| 401 Unauthorized | No autenticado | Token ausente, inválido o expirado. |
| 403 Forbidden | Acceso denegado | El usuario autenticado no tiene rol de administrador. |
| 404 Not Found | No encontrado | El préstamo indicado no existe. |

---

## Reglas adicionales del módulo

- Todos los endpoints de este módulo requieren JWT.
- Solo los usuarios con rol Administrador pueden consumir estos endpoints.
- El listado debe estar paginado y ordenado del más reciente al más antiguo.
- Por defecto, el listado debe mostrar préstamos activos.
- El endpoint debe permitir filtrar por estado y buscar por cédula del cliente.
- Un cliente solo puede tener un préstamo activo a la vez.
- Solo se pueden asignar préstamos a clientes activos.
- El préstamo debe crearse en estado Activo.
- El número de préstamo debe tener 9 dígitos y ser único en el sistema.
- El número de préstamo no puede repetirse como número de cuenta de ahorro.
- Al crear un préstamo, el monto aprobado debe acreditarse a la cuenta principal del cliente.
- El desembolso del préstamo debe registrarse como una transacción de tipo CRÉDITO.
- La tabla de amortización debe generarse automáticamente al crear el préstamo.
- La API debe responder 409 Conflict cuando el cliente sea o se convierta en cliente de alto riesgo y no se haya confirmado la asignación.
- Al modificar la tasa de interés, solo deben recalcularse cuotas futuras pendientes.
- El sistema debe enviar notificaciones por correo cuando se cree un préstamo o se modifique su tasa de interés.
