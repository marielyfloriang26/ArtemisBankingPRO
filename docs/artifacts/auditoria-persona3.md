# Reporte de Auditoría: Módulos "Persona 3"

Este documento detalla los hallazgos y correcciones aplicadas durante la auditoría exhaustiva de los módulos asignados a la Persona 3, basándose estrictamente en los requerimientos del documento `Proyecto_final_Extract.txt`.

La auditoría se dividió en 4 áreas principales, verificando que controladores, servicios, vistas, viewmodels, DTOs, validaciones y los **mensajes de error exactos** cumplieran al 100% con la especificación.

---

## 1. Funcionalidades del Administrador: Gestión de préstamos y Gestión de tarjetas de crédito

### Hallazgos
- **Gestión de Préstamos**: La implementación actual (`PrestamoController`, `PrestamoService`, ViewModels correspondientes) fue evaluada y se determinó que **cumple al 100% con los requerimientos**. El cálculo del sistema francés de amortización, el control de riesgos de clientes y los mensajes de error estaban correctos.
- **Gestión de Tarjetas de Crédito**: Se detectó una implementación parcial enfocada en la configuración global del producto (`ProductoTarjetaCredito`) y no en la gestión administrativa requerida por el PDF (asignación a clientes, límites, historial de consumos, cancelación).

### Correcciones Aplicadas
Para cumplir con la especificación (páginas 47 a 58), se realizaron las siguientes correcciones significativas:
- **Creación de ViewModels Administrativos**: Se añadieron `TarjetaAdminViewModel`, `AssignTarjetaViewModel`, `EditLimiteTarjetaViewModel` y `ConsumoTarjetaViewModel` aplicando enmascaramiento de la tarjeta, fechas en formato `MM/yy`, y validación de límite mayor a 0.
- **Lógica en el Servicio (`TarjetaCreditoService`)**: Se implementaron todos los métodos faltantes (`GetAllTarjetasFilteredAsync`, `GetClientesElegiblesAsync`, `AsignarTarjetaAsync`, `EditLimiteAsync`, `CancelTarjetaAsync`), añadiendo:
  - Generación del CVC con SHA-256.
  - Fecha de expiración a exactamente 3 años desde la creación.
  - Restricción: *"El límite de la tarjeta no puede ser inferior al monto adeudado actualmente"*.
  - Restricción: *"Para cancelar esta tarjeta, el cliente debe saldar la totalidad de la deuda pendiente"*.
- **Controlador (`AdminTarjetaCreditoController`)**: Se creó y configuró con `[Authorize(Roles = "Administrador")]` y se incluyeron los mensajes de UI exactos requeridos por el PDF.

---

## 2. Funcionalidades del cliente: Beneficiarios y Funcionalidad de transacciones (Terceros)

### Hallazgos
- Tras la revisión de los controladores (`BeneficiarioController`, `TransaccionController`) y los servicios asociados, se comprobó que la implementación **está impecablemente alineada con los requerimientos sin necesidad de correcciones**.
- Mensajes como *"El número de cuenta ingresado no corresponde a una cuenta válida"* y *"Esta cuenta ya se encuentra registrada como beneficiario"* estaban presentes exactamente como se pedía.
- La lógica de reducción por sobrepago, amortización por antigüedad de cuota y rollback transaccional se ejecutan perfectamente.

---

## 3. Funcionalidades Cajero: Pago a préstamo y Transacciones a cuentas de terceros

### Hallazgos
- Las vistas, viewmodels, anotaciones y strings fueron verificados con éxito. 
- Se descubrió una pequeña desviación en la trazabilidad de operaciones rechazadas: El sistema registraba la transacción como `RECHAZADO` únicamente cuando había insuficiencia de fondos. Sin embargo, el PDF indicaba que cualquier falla por *validación de negocio* también debía registrarse como rechazada si la cuenta origen existía.

### Correcciones Aplicadas
- Se actualizó el servicio `TransaccionCajeroService.cs` (`ValidarPagoPrestamoAsync` y `ValidarTransaccionTercerosAsync`).
- Ahora, si la cuenta origen es válida, cualquier falla posterior (ej. préstamo no existe, cuenta destino inactiva) invoca correctamente `RegistrarTransaccionRechazadaAsync`, cumpliendo la trazabilidad requerida al pie de la letra.

---

## 4. Funcionalidades del Api: Gestión de Préstamos y Gestión de Comercios

### Hallazgos
- La estructura, paginación, DTOs y filtrado por estado estaban correctamente implementados y arrojaban los códigos HTTP apropiados.
- El problema radicaba en que las validaciones arrojaban mensajes de error descriptivos en lugar de las frases *exactas* exigidas por el PDF.

### Correcciones Aplicadas
- Se modificaron los mensajes de error en `PrestamoService.cs` y `ComercioService.cs` para asegurar un match perfecto de strings:
  - *"El cliente seleccionado no existe o no está activo."* ➡️ *"El cliente debe estar activo."*
  - *"Este cliente ya tiene un préstamo activo asignado."* ➡️ *"El cliente no debe tener un préstamo activo actualmente."*
  - *"El cliente no tiene una cuenta de ahorro principal activa para recibir el desembolso del préstamo."* ➡️ *"El cliente debe tener una cuenta de ahorro principal activa."*
  - *"El plazo seleccionado no es válido..."* ➡️ *"El plazo debe ser uno de los valores permitidos."*
  - *"El préstamo seleccionado no existe."* ➡️ *"El préstamo indicado no existe."*
  - *"Solo se puede modificar la tasa de interés de préstamos activos."* ➡️ *"El préstamo debe estar activo."*
  - *"No existen cuotas futuras pendientes para recalcular."* ➡️ *"Debe existir al menos una cuota futura pendiente para poder recalcular."*
  - *"El campo status es obligatorio."* ➡️ *"Body inválido o campo status faltante."*

---

**Conclusión:**
Los módulos de la Persona 3 han sido auditados en su totalidad. Los desajustes estructurales y semánticos han sido corregidos y el sistema cumple el 100% del requerimiento para estos flujos.
