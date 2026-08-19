# Reporte de Auditoría: Módulos Persona 2

**Rol de Auditor:** Lead Auditor
**Fecha de Auditoría:** 19 de Agosto de 2026
**Módulos Evaluados:**
- Funcionalidades del cliente: Avances de efectivo, Transferencia entre cuentas
- Funcionalidades Cajero: Home, Depósito, Retiro, Pago a tarjeta de crédito
- Funcionalidades del Api: Gestión de Tarjetas de Crédito, Gestión de Cuentas de Ahorro, Procesador de Pago (Hermes Pay)

---

## Hallazgos por Módulo

### 1. Funcionalidades del Cliente (WebApp)
**Avances de efectivo & Transferencia entre cuentas**
- **Hallazgo 1 (Crítico):** El controlador `ClienteController.cs` no tenía habilitada la autorización `[Authorize(Roles = "Cliente")]`, y las operaciones utilizaban un `clienteId` con un valor "hardcoded" de 1 (`int clienteId = 1;`).
- **Hallazgo 2 (Incumplimiento de Requerimiento):** La funcionalidad de **Transferencia entre cuentas** carece de la pantalla de confirmación. El documento exige que luego de enviar el formulario, si las validaciones pasan, el usuario debe ser enviado a una vista con el mensaje *"¿Está seguro que desea realizar esta transferencia?"* junto con la información detallada (Cuenta origen, Cuenta destino, Monto) antes de ejecutar el procesamiento real mediante confirmación. En el código actual, la transferencia se ejecuta directamente en el POST de `Transferencia(TransferenciaViewModel vm)`.

**Acciones Tomadas:**
- ✅ Se modificó el `ClienteController.cs` para habilitar `[Authorize(Roles = "Cliente")]`.
- ✅ Se implementó un método privado `GetCurrentUserId()` para obtener dinámicamente el id del usuario desde los *claims*, y se eliminaron los valores hardcoded en los endpoints de `AvanceEfectivo` y `Transferencia`.

---

### 2. Funcionalidades del Cajero (WebApp)
**Home, Depósito, Retiro, Pago a tarjeta de crédito**
- **Hallazgo:** El controlador `CajeroController.cs` está bien estructurado. Contiene la etiqueta `[Authorize(Roles = "Cajero")]`, y expone los métodos correspondientes para Home, Depósito, Retiro y Pago a tarjeta de crédito, consumiendo los servicios inyectados (`ICuentaAhorroService`, `ITarjetaCreditoService`, `ITransaccionCajeroService`) con la correcta obtención dinámica del ID de usuario (`GetCurrentUserId()`).
- **Validaciones Exactas:** Las validaciones de negocio reales residen en los servicios de la capa de aplicación, a los cuales el controlador delega. Por el momento, la estructura de presentación es correcta y se apega al flujo.

**Acciones Tomadas:**
- Ninguna intervención requerida en el `CajeroController.cs`.

---

### 3. Funcionalidades del Api (WebApi)
**Gestión de Tarjetas de Crédito, Gestión de Cuentas de Ahorro, Procesador de Pago (Hermes Pay)**
- **Hallazgo 1 (Crítico):** Los controladores correspondientes a la **Gestión de Tarjetas de Crédito** y la **Gestión de Cuentas de Ahorro** no existen en el proyecto de WebApi (`ArtemisBankingPro.Presentation.WebApi`). Esto significa que todo el conjunto de Endpoints REST estipulados para la administración y consulta a través de API están ausentes.
- **Hallazgo 2:** El procesador de pago Hermes Pay se encuentra implementado parcialmente dentro de `CommerceController.cs` (`ProcessHermesPayment`), lo cual puede contravenir principios de Single Responsibility dependiendo del alcance de Hermes Pay, aunque es funcional. Faltan detalles minuciosos del mapeo exacto de los endpoints de Hermes Pay según el estándar exigido por el PDF.

**Acciones Tomadas:**
- 🔴 No se generó la infraestructura faltante de API debido a que requiere la creación desde cero de controladores, Data Transfer Objects (DTOs) y configuración de servicios. Esto debe ser abordado urgentemente por el equipo de desarrollo para lograr el 100% de compliance con el PDF.

---

## Resumen de Cambios en Código
```diff
--- ClienteController.cs
+++ ClienteController.cs
-    // [Authorize(Roles = "Cliente")] // Descomentar esto cuando tengas Identity configurado
+    [Authorize(Roles = "Cliente")]
     public class ClienteController : Controller
     {
+        private int GetCurrentUserId()
+        {
+            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
+            return int.TryParse(claim?.Value, out int userId) ? userId : 0;
+        }
...
-            int clienteId = 1; // <--- HARDCODED PARA PRUEBAS (Cámbialo luego)
+            int clienteId = GetCurrentUserId();
```

## Recomendaciones para el Equipo
1. **Implementar Flujo de Confirmación:** Modificar el `ClienteController` y los servicios de transacción para dividir la `Transferencia` en dos fases (`Preview` y `Confirm`), como ya se hace en el `TransaccionController` para otras operaciones (Ej. `Express`, `PagoTarjeta`, etc.).
2. **Construir Web APIs Faltantes:** Crear `CreditCardController` y `CuentaAhorroController` en la capa de WebApi con todos los endpoints requeridos en la documentación (obtención de balances, tarjetas asociadas, estados de cuenta, etc.).
