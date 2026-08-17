# Pago a préstamo (Funcionalidades Cajero)

Al ingresar a la opción **Pago a préstamo** desde el menú principal del cajero, el sistema debe enviar al usuario a la pantalla para registrar pagos a préstamos.

Esta funcionalidad permitirá que el cajero aplique un pago a un préstamo activo, utilizando fondos disponibles en una cuenta de ahorro activa registrada en el sistema.

Solo los usuarios con rol **Cajero** podrán acceder a esta funcionalidad. Si un usuario con rol Administrador, Cliente o Comercio intenta acceder directamente a esta pantalla mediante la URL, el sistema debe redirigirlo a la pantalla de **Acceso denegado**.

## Formulario de pago a préstamo

La pantalla debe mostrar un formulario para que el cajero pueda ingresar la cuenta de origen, el préstamo destino y el monto que desea pagar.

El formulario debe contener los siguientes campos:

| Campo | Tipo de dato | Requerido | Descripción |
|---|---|---|---|
| Número de cuenta origen | Texto / string | Sí | Número de cuenta de ahorro desde la cual se tomará el dinero para realizar el pago. |
| Número del préstamo | Texto / string | Sí | Número identificador de 9 dígitos del préstamo al que se aplicará el pago. |
| Monto a pagar | Decimal / number | Sí | Monto que se desea abonar al préstamo. |

Debajo del formulario debe existir un botón con el texto **Realizar pago**.

### Descripción de campos

**Número de cuenta origen**
Representa la cuenta de ahorro desde la cual se descontará el dinero para realizar el pago. La cuenta debe existir, estar activa y tener fondos suficientes para cubrir el monto efectivo que será aplicado al préstamo.

**Número del préstamo**
Representa el préstamo al que se aplicará el pago. El préstamo debe existir y debe encontrarse activo. No se deben permitir pagos a préstamos completados.

**Monto a pagar**
Representa el monto que el cajero desea aplicar como pago al préstamo. Este monto debe ser mayor que cero. Si el monto ingresado supera el monto total pendiente del préstamo, el sistema solo debe utilizar como monto efectivo de pago el valor exacto de la deuda pendiente.

## Validaciones del pago a préstamo

El formulario de pago a préstamo debe cumplir las siguientes validaciones:

- El número de cuenta origen es requerido.
- La cuenta origen debe existir en el sistema.
- La cuenta origen debe estar activa.
- El número del préstamo es requerido.
- El número del préstamo debe contener 9 dígitos.
- El préstamo debe existir en el sistema.
- El préstamo debe estar activo.
- El préstamo debe tener cuotas pendientes de pago.
- El monto a pagar es requerido.
- El monto a pagar debe ser mayor que cero.
- La cuenta origen debe tener fondos suficientes para cubrir el monto efectivo que será aplicado al préstamo.

Mensajes de error exactos:

- Cuenta origen no existe o inactiva: **"El número de cuenta ingresado no corresponde a una cuenta válida."**
- Préstamo no existe o completado: **"El número de préstamo ingresado no corresponde a un préstamo válido."**
- Monto ≤ 0: **"El monto a pagar debe ser mayor que cero."**
- Préstamo sin cuotas pendientes: **"El préstamo seleccionado no tiene cuotas pendientes de pago."**
- Fondos insuficientes: **"El monto ingresado excede el saldo disponible de la cuenta."**

## Regla para evitar pagos superiores a la deuda del préstamo

El sistema no debe descontar de la cuenta de ahorro un monto mayor al total pendiente real del préstamo. Si el monto ingresado por el cajero excede el monto pendiente del préstamo, solo se debe debitar de la cuenta origen el monto correspondiente a la deuda real pendiente. El excedente no debe utilizarse ni descontarse de la cuenta del cliente.

Ejemplo: si el préstamo tiene un monto pendiente de RD$2,000.00 y el cajero intenta registrar un pago de RD$3,000.00, el sistema solo debe debitar RD$2,000.00 de la cuenta origen y aplicar RD$2,000.00 al préstamo. El excedente de RD$1,000.00 no debe descontarse ni registrarse como pago.

## Confirmación del pago

Si todas las validaciones son correctas, el sistema debe enviar al cajero a una pantalla de confirmación antes de ejecutar el pago.

En esta pantalla se debe mostrar:

| Campo | Descripción |
|---|---|
| Titular de la cuenta origen | Nombre y apellido del cliente propietario de la cuenta desde la cual se tomará el dinero. |
| Número de cuenta origen | Número de cuenta desde la cual se realizará el débito. |
| Titular del préstamo | Nombre y apellido del cliente propietario del préstamo. |
| Número del préstamo | Número identificador del préstamo al que se aplicará el pago. |
| Monto ingresado | Monto digitado por el cajero. |
| Monto efectivo a pagar | Monto que realmente será debitado y aplicado al préstamo. |

La pantalla debe mostrar el mensaje: **"¿Está seguro que desea realizar este pago?"**

Botones:

| Botón | Descripción |
|---|---|
| Cancelar | Cancela la operación y redirige al cajero al Home del cajero. |
| Confirmar | Ejecuta el pago del préstamo. |

Si el cajero cancela la operación, el pago no debe ejecutarse y el sistema debe redirigirlo al Home del cajero.

## Aplicación del pago al préstamo

Si el cajero confirma la operación, el sistema debe aplicar el pago siguiendo la tabla de amortización del préstamo.

El sistema debe buscar la primera cuota pendiente, es decir, la cuota más cercana en fecha que no haya sido pagada completamente o que tenga saldo pendiente, aunque sea parcial.

El monto efectivo pagado debe aplicarse de la siguiente manera:

- Si el monto alcanza para completar la primera cuota pendiente, esa cuota debe marcarse como pagada.
- Si el monto no alcanza para completar la primera cuota pendiente, esa cuota debe quedar parcialmente pagada.
- Si después de pagar una cuota queda saldo disponible del monto efectivo pagado, el sistema debe continuar aplicando el excedente a la siguiente cuota pendiente.
- Este proceso debe repetirse hasta que el monto efectivo pagado se agote o hasta que no existan más cuotas pendientes.

Si todas las cuotas del préstamo quedan pagadas, el préstamo debe actualizarse al estado **Completado**.

Si una cuota atrasada es pagada completamente, el sistema debe actualizar su indicador de atraso para que ya no aparezca como atrasada.

## Procesamiento del pago

Cuando el pago sea aprobado, el sistema debe realizar las siguientes acciones:

- Debitar el monto efectivo de pago desde la cuenta de ahorro origen.
- Aplicar el pago al préstamo siguiendo el orden de la tabla de amortización.
- Actualizar el estado de las cuotas afectadas.
- Actualizar el monto pendiente del préstamo.
- Marcar el préstamo como Completado si todas sus cuotas quedan pagadas.
- Registrar la transacción en el historial de la cuenta origen como DÉBITO.
- Asociar la operación al cajero autenticado que realizó el pago.
- Registrar la fecha y hora exacta de la operación.

La transacción debe quedar registrada con la siguiente información:

| Campo | Valor |
|---|---|
| Tipo de transacción | DÉBITO |
| Monto | Monto efectivo pagado |
| Origen | Número de cuenta origen |
| Beneficiario | Número identificador del préstamo |
| Estado | APROBADA |
| Usuario responsable | Cajero autenticado |
| Fecha | Fecha y hora en que se realizó la operación |

Si la operación es rechazada por fondos insuficientes o por alguna validación de negocio, el sistema debe registrar el intento como RECHAZADO en la cuenta origen, sin afectar el balance de la cuenta ni las cuotas del préstamo.

## Correo de pago a préstamo

Una vez procesado correctamente el pago, el sistema debe enviar automáticamente un correo electrónico notificando la operación.

El correo debe enviarse al cliente propietario del préstamo.

Si el propietario de la cuenta origen es diferente al propietario del préstamo, también debe enviarse una notificación al propietario de la cuenta origen, indicando que se debitó dinero de su cuenta para realizar el pago.

Asunto del correo para el propietario del préstamo: **"Pago realizado al préstamo [XXXXXXXXX]"** (donde [XXXXXXXXX] es el número identificador de 9 dígitos del préstamo).

El cuerpo del correo debe incluir:
- Monto pagado.
- Número del préstamo.
- Últimos cuatro dígitos de la cuenta desde la cual se realizó el pago.
- Fecha de la transacción.
- Hora exacta de la transacción.

Contenido sugerido:

```
Asunto: Pago realizado al préstamo [XXXXXXXXX]

Hola [Nombre del cliente],

Se ha realizado un pago a su préstamo [XXXXXXXXX].

Monto pagado: RD$[Monto]
Cuenta origen terminada en: [XXXX]
Fecha y hora: [Fecha y hora]

Si usted no reconoce esta operación, comuníquese con la entidad bancaria.
```

Si ocurre un error al enviar el correo, el pago no debe revertirse. El sistema debe registrar el error y mostrar un mensaje informativo al cajero.

Mensaje sugerido: **"El pago fue realizado correctamente, pero no fue posible enviar el correo de notificación."**

Finalmente, una vez completada la operación, el sistema debe redirigir al cajero a su pantalla principal, es decir, al Home del cajero.

## Reglas adicionales del módulo

1. Solo los usuarios con rol Cajero pueden acceder a la funcionalidad de Pago a préstamo.
2. Solo se pueden realizar pagos desde cuentas de ahorro activas.
3. Solo se pueden aplicar pagos a préstamos activos.
4. El monto a pagar debe ser mayor que cero.
5. El préstamo debe tener cuotas pendientes para poder recibir un pago.
6. El sistema no debe descontar montos superiores al total pendiente real del préstamo.
7. Si el monto ingresado excede la deuda pendiente, solo se debe debitar el monto correspondiente a la deuda real.
8. El pago debe aplicarse desde la cuota pendiente más antigua hacia las cuotas siguientes.
9. Una cuota puede quedar parcialmente pagada si el monto aplicado no alcanza para saldar completa.
10. El pago debe registrarse como una transacción de tipo DÉBITO en la cuenta origen.
11. El beneficiario de la transacción debe ser el número identificador del préstamo.
12. Todo pago aprobado debe actualizar el balance de la cuenta origen, las cuotas afectadas y el monto pendiente del préstamo.
13. Si todas las cuotas quedan pagadas, el préstamo debe pasar al estado completo.
14. Las operaciones rechazadas no deben modificar balances ni cuotas.
15. La operación debe quedar asociada al cajero autenticado que la realizó.
16. El pago no debe revertirse si falla el envío del correo electrónico.
17. Al finalizar la operación, el sistema debe redirigir al cajero al Home del cajero.
