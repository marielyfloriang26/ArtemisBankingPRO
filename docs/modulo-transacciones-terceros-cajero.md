# Transacciones a cuentas de terceros (Funcionalidades Cajero)

Al ingresar a la opción **Transacciones a cuentas de terceros** desde el menú principal del cajero, el sistema debe enviar al usuario a la pantalla para realizar transferencias entre cuentas de ahorro registradas en el sistema.

Esta funcionalidad permitirá que el cajero debite fondos desde una cuenta de ahorro origen y los acredite en una cuenta de ahorro destino, siempre que ambas cuentas existan, estén activas y la cuenta origen tenga fondos suficientes.

Solo los usuarios con rol **Cajero** podrán acceder a esta funcionalidad. Si un usuario con rol Administrador, Cliente o Comercio intenta acceder directamente a esta pantalla mediante la URL, el sistema debe redirigirlo a la pantalla de **Acceso denegado**.

## Formulario de transacción a cuenta de terceros

La pantalla debe mostrar un formulario para que el cajero pueda ingresar la cuenta de origen, la cuenta destino y el monto de la transacción.

El formulario debe contener los siguientes campos:

| Campo | Tipo de dato | Requerido | Descripción |
|---|---|---|---|
| Número de cuenta origen | Texto / string | Sí | Número de cuenta de ahorro desde la cual se descontará el dinero. |
| Número de cuenta destino | Texto / string | Sí | Número de cuenta de ahorro donde se acreditará el dinero. |
| Monto de la transacción | Decimal / number | Sí | Monto que será transferido desde la cuenta origen hacia la cuenta destino. |

Debajo del formulario debe existir un botón con el texto **Realizar transacción**.

### Descripción de campos

**Número de cuenta origen**
Representa la cuenta de ahorro desde la cual se descontará el dinero.
La cuenta debe existir, estar activa y tener fondos suficientes para cubrir el monto indicado.

**Número de cuenta destino**
Representa la cuenta de ahorro donde será acreditado el dinero.
La cuenta debe existir y estar activa.

**Monto de la transacción**
Representa la cantidad de dinero que será transferida desde la cuenta origen hacia la cuenta destino.
Este monto debe ser mayor que cero.

## Validaciones de la transacción

El formulario de transacción a cuentas de terceros debe cumplir las siguientes validaciones:

- El número de cuenta origen es requerido.
- El número de cuenta origen debe existir en el sistema.
- La cuenta origen debe estar activa.
- El número de cuenta destino es requerido.
- El número de cuenta destino debe existir en el sistema.
- La cuenta destino debe estar activa.
- La cuenta origen y la cuenta destino no pueden ser la misma.
- El monto de la transacción es requerido.
- El monto de la transacción debe ser mayor que cero.
- La cuenta origen debe tener fondos suficientes para cubrir el monto indicado.

Mensajes de error exactos:

- Cuenta origen no existe o inactiva: **"El número de cuenta origen ingresado no corresponde a una cuenta válida."**
- Cuenta destino no existe o inactiva: **"El número de cuenta destino ingresado no corresponde a una cuenta válida."**
- Cuenta origen = cuenta destino: **"La cuenta origen y la cuenta destino no pueden ser la misma."**
- Monto ≤ 0: **"El monto de la transacción debe ser mayor que cero."**
- Fondos insuficientes: **"El monto ingresado excede el saldo disponible de la cuenta."**

## Confirmación de la transacción

Si todas las validaciones son correctas, el sistema debe enviar al cajero a una pantalla de confirmación antes de ejecutar la transacción.

En esta pantalla se debe mostrar la siguiente información:

| Campo | Descripción |
|---|---|
| Titular de la cuenta origen | Nombre y apellido del cliente propietario de la cuenta desde la cual se descontará el dinero. |
| Número de cuenta origen | Número de cuenta desde la cual se realizará el débito. |
| Titular de la cuenta destino | Nombre y apellido del cliente propietario de la cuenta que recibirá el dinero. |
| Número de cuenta destino | Número de cuenta donde se acreditará el dinero. |
| Monto de la transacción | Monto que será transferido. |

La pantalla debe mostrar el siguiente mensaje: **"¿Está seguro de que desea realizar esta transacción?"**

Debajo del mensaje deben existir dos botones:

| Botón | Descripción |
|---|---|
| Cancelar | Cancela la operación y redirige al cajero al Home del cajero. |
| Confirmar | Ejecuta la transacción entre las cuentas indicadas. |

Si el cajero cancela la operación, la transacción no debe ejecutarse y el sistema debe redirigirlo al Home del cajero.

## Procesamiento de la transacción

Si el cajero confirma la operación, el sistema debe debitar el monto indicado de la cuenta de ahorro origen y acreditar el mismo monto en la cuenta de ahorro destino.

El sistema debe realizar las siguientes acciones:

- Restar el monto de la transacción del balance de la cuenta origen.
- Sumar el mismo monto al balance de la cuenta destino.
- Registrar la transacción en la cuenta origen como DÉBITO.
- Registrar la transacción en la cuenta destino como CRÉDITO.
- Asociar la operación al cajero autenticado que realizó la transacción.
- Registrar la fecha y hora exacta de la operación.

La operación debe ejecutarse de forma transaccional. Si ocurre un error al debitar, acreditar o registrar alguno de los movimientos, el sistema no debe aplicar parcialmente la transacción.

### Registro en la cuenta origen

En la cuenta de origen, la transacción debe registrarse como un movimiento de tipo DÉBITO, ya que representa una salida de fondos.

La transacción debe quedar registrada con la siguiente información:

| Campo | Valor |
|---|---|
| Tipo de transacción | DÉBITO |
| Monto | Monto transferido |
| Origen | Número de cuenta origen |
| Beneficiario | Número de cuenta destino |
| Estado | APROBADA |
| Usuario responsable | Cajero autenticado |
| Fecha | Fecha y hora en que se realizó la operación |

### Registro en la cuenta destino

En la cuenta destino, la transacción debe registrarse como un movimiento de tipo CRÉDITO, ya que representa una entrada de fondos.

La transacción debe quedar registrada con la siguiente información:

| Campo | Valor |
|---|---|
| Tipo de transacción | CRÉDITO |
| Monto | Monto recibido |
| Origen | Número de cuenta origen |
| Beneficiario | Número de cuenta destino |
| Estado | APROBADA |
| Usuario responsable | Cajero autenticado |
| Fecha | Fecha y hora en que se realizó la operación |

Este registro cruzado garantiza la trazabilidad de la operación, permitiendo identificar desde cuál cuenta salió el dinero, hacia cuál cuenta fue enviado y qué cajero realizó la transacción.

Si la operación es rechazada por fondos insuficientes o por alguna validación de negocio, el sistema debe registrar el intento como RECHAZADO en la cuenta origen cuando ésta exista, sin afectar ningún balance.

## Correos de notificación

Una vez procesada correctamente la transacción, el sistema debe enviar automáticamente dos correos electrónicos.

El primer correo debe enviarse al cliente propietario de la cuenta origen, notificando que se ha realizado un envío de dinero hacia otra cuenta.

El asunto del correo debe ser: **"Transacción realizada a la cuenta [XXXX]"**

Donde [XXXX] corresponde a los últimos cuatro dígitos del número de cuenta destino.

El cuerpo del correo debe incluir:
- Monto transferido.
- Últimos cuatro dígitos de la cuenta origen.
- Últimos cuatro dígitos de la cuenta destino.
- Fecha de la transacción.
- Hora exacta de la transacción.

El segundo correo debe enviarse al cliente propietario de la cuenta destino, notificando que ha recibido una transacción desde otra cuenta.

El asunto del correo debe ser: **"Transacción enviada desde la cuenta [XXXX]"**

Donde [XXXX] corresponde a los últimos cuatro dígitos del número de cuenta origen.

El cuerpo del correo debe incluir:
- Monto recibido.
- Últimos cuatro dígitos de la cuenta origen.
- Últimos cuatro dígitos de la cuenta destino.
- Fecha de la transacción.
- Hora exacta de la transacción.

Si ocurre un error al enviar uno o ambos correos, la transacción no debe revertirse. El sistema debe registrar el error y mostrar un mensaje informativo al cajero.

Mensaje sugerido: **"La transacción fue realizada correctamente, pero no fue posible enviar una o más notificaciones por correo."**

Finalmente, una vez completada la operación, el sistema debe redirigir al cajero a su pantalla principal, es decir, al Home del cajero.

## Reglas adicionales del módulo

1. Solo los usuarios con rol Cajero pueden acceder a la funcionalidad de Transacciones a cuentas de terceros.
2. Solo se pueden realizar transacciones entre cuentas de ahorro activas.
3. La cuenta origen y la cuenta destino deben existir en el sistema.
4. La cuenta origen y la cuenta destino no pueden ser la misma cuenta.
5. El monto de la transacción debe ser mayor que cero.
6. La cuenta origen debe tener fondos suficientes antes de aprobar la transacción.
7. La cuenta origen debe registrar la operación como DÉBITO.
8. La cuenta destino debe registrar la operación como CRÉDITO.
9. Todo movimiento aprobado debe actualizar los balances de ambas cuentas.
10. Las operaciones rechazadas no deben modificar balances.
11. La operación debe quedar asociada al cajero autenticado que la realizó.
12. La transacción debe ejecutarse de forma transaccional para evitar inconsistencias.
13. El envío de correos no debe revertir la transacción si ocurre un error.
14. Al finalizar la operación, el sistema debe redirigir al cajero al Home del cajero.
