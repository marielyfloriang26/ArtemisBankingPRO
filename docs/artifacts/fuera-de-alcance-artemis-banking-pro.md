# Artemis Banking Pro (ABP) — Especificación de Funcionalidades Fuera de Alcance (Out of Scope)

> **Documento de Control y Alcance del Proyecto**  
> **Proyecto:** Artemis Banking Pro (ABP)  
> **Referencia:** Especificaciones del PDF Oficial (`Proyecto final_ Artemis Banking Pro (ABP) .pdf`) y Estructura de Módulos.

---

## 1. Resumen Ejecutivo

Este documento consolida todas las **exclusiones de alcance**, **límites operativos**, **reglas de restricción** y **funcionalidades no contempladas** en el sistema **Artemis Banking Pro (ABP)**. 

El objetivo es establecer de forma transparente las fronteras del sistema entre las funcionalidades requeridas para la aplicación web/API y aquellas que han sido explícitamente **dejadas fuera de alcance (Out of Scope)** para evitar sobre-ingeniería, inconsistencias de datos o desviaciones de los requisitos originales.

---

## 2. Exclusiones por Módulo Funcional

### 2.1. Autenticación, Seguridad y Gestión de Usuarios

| Funcionalidad / Caso de Uso | Estado | Descripción / Justificación |
|---|---|---|
| **Acceso Web para Usuarios Comercio** | ❌ Fuera de Alcance | Los usuarios con rol `Comercio` **no pueden iniciar sesión** ni acceder a ninguna pantalla de la aplicación Web MVC. Su rol se utiliza exclusivamente para autenticarse en la Web API (Hermes Pay). |
| **Autogestión / Desactivación del propio Administrador** | ❌ Fuera de Alcance | El Administrador en sesión **no puede inactivar su propio usuario** ni modificar su perfil desde el listado de gestión de usuarios para evitar bloqueos del sistema. |
| **Cambio de Rol (`TipoUsuario`) post-creación** | ❌ Fuera de Alcance | Una vez creado un usuario (Administrador, Cajero, Cliente, Comercio), **su rol no se puede modificar** mediante la edición de usuario. |
| **Asignación de Monto Inicial a Admins / Cajeros** | ❌ Fuera de Alcance | El campo "Monto inicial" en el alta de usuarios aplica únicamente al rol `Cliente`. Admins, Cajeros y Comercios **no poseen balance inicial**. |
| **Eliminación Física de Usuarios (*Hard Delete*)** | ❌ Fuera de Alcance | El sistema no elimina registros de usuarios físicamente de la base de datos. Se utiliza desactivación lógica (`EsActivo = false`) para preservar la trazabilidad. |
| **Recuperación Automática de Sesión tras Inactivar Comercio** | ❌ Fuera de Alcance | Si un comercio se reactiva, sus usuarios asociados **no se reactivan automáticamente**; deben completar un proceso de restablecimiento de contraseña. |

---

### 2.2. Módulo de Cuentas de Ahorro

| Funcionalidad / Caso de Uso | Estado | Descripción / Justificación |
|---|---|---|
| **Cancelación de Cuenta Principal** | ❌ Fuera de Alcance | La cuenta de ahorro **Principal** asignada durante el registro del cliente **no puede ser cancelada** bajo ninguna circunstancia. |
| **Cancelación de Cuentas Secundarias con Balance** | ❌ Fuera de Alcance | No se permite cancelar una cuenta secundaria que posea un balance superior a `RD$ 0.00`. El cliente debe transferir el saldo antes de cancelar. |
| **Creación de Cuentas Principales por el Cliente** | ❌ Fuera de Alcance | El cliente solo puede crear **cuentas secundarias**. Las cuentas principales solo se originan automáticamente al dar de alta al cliente. |
| **Modificación de Número de Cuenta** | ❌ Fuera de Alcance | Los números de cuenta de 9 dígitos son numéricos, únicos e inmutables. No se permite su edición posterior. |
| **Cuentas en Moneda Extranjera (USD / EUR)** | ❌ Fuera de Alcance | Todas las cuentas de ahorro operan exclusivamente en pesos dominicanos (`DOP / RD$`). No se incluye soporte multimoneda ni conversión cambiaria. |

---

### 2.3. Módulo de Préstamos (Web App y Web API)

| Funcionalidad / Caso de Uso | Estado | Descripción / Justificación |
|---|---|---|
| **Múltiples Préstamos Activos Simultáneos** | ❌ Fuera de Alcance | Un cliente **solo puede tener un (1) préstamo activo a la vez**. No se pueden aprobar préstamos adicionales hasta que el actual esté `Completado`. |
| **Recálculo Retroactivo de Cuotas Pasadas o Vencidas** | ❌ Fuera de Alcance | Al modificar la tasa de interés anual (`PATCH /api/loan/{id}/rate`), **no se modifican** cuotas pagadas, parcialmente pagadas, vencidas o con vencimiento presente. Solo aplica a cuotas futuras pendientes. |
| **Sobrepago de Préstamo por Ventanilla (Excedentes)** | ❌ Fuera de Alcance | Si el monto digitado por el cajero excede la deuda pendiente del préstamo, el sistema **solo debita el monto exacto de la deuda real**. El excedente no se cobra ni se guarda como saldo a favor. |
| **Aprobación Automática a Clientes de Alto Riesgo** | ❌ Fuera de Alcance | Si la deuda proyectada supera la deuda promedio del sistema, la API responde `409 Conflict` y no aprueba el préstamo salvo que se envíe `confirmHighRisk = true`. |
| **Plazos Arbitrarios de Préstamo** | ❌ Fuera de Alcance | Los plazos de préstamo están restringidos a múltiplos de 6 meses (6, 12, 18, 24, 30, 36, 42, 48, 54, 60 meses). No se permiten plazos fuera de esta lista. |
| **Pagos a Préstamos en Estado Completado** | ❌ Fuera de Alcance | No se admiten transacciones de pago ni débitos a préstamos que ya hayan saldado todas sus cuotas. |

---

### 2.4. Módulo de Tarjetas de Crédito y Avances de Efectivo

| Funcionalidad / Caso de Uso | Estado | Descripción / Justificación |
|---|---|---|
| **Emisión de Tarjeta con Deuda Inicial** | ❌ Fuera de Alcance | El límite de crédito aprobado **no constituye deuda inicial**. Todas las tarjetas de crédito inician obligatoriamente con `MontoAdeudado = 0.00`. |
| **Cancelación de Tarjetas con Deuda Pendiente** | ❌ Fuera de Alcance | No se permite cancelar una tarjeta de crédito si su monto adeudado es superior a `RD$ 0.00`. Debe ser saldada previamente. |
| **Almacenamiento de CVC en Texto Plano** | ❌ Fuera de Alcance | El CVC de 3 dígitos **nunca se almacena en texto plano**. Debe guardarse con hash de seguridad `SHA-256`. |
| **Visualización Completa del Plástico / PAN** | ❌ Fuera de Alcance | Las pantallas y correos notificadores **nunca muestran los 16 dígitos completos** ni el CVC. Solo se exhiben enmascarados (ej: `XXXX-XXXX-XXXX-1234`). |
| **Avance de Efectivo a Cuentas Secundarias** | ❌ Fuera de Alcance | El avance de efectivo retira fondos de la línea de crédito y los deposita **exclusivamente en la cuenta de ahorro principal** del cliente. |

---

### 2.5. Operaciones de Cajero y Transacciones a Terceros

| Funcionalidad / Caso de Uso | Estado | Descripción / Justificación |
|---|---|---|
| **Operaciones por Ventanilla Ejecutadas por Admins o Clientes** | ❌ Fuera de Alcance | Las funciones de caja (Depósito, Retiro, Pago Préstamo/Tarjeta por Caja, Terceros) son **exclusivas del rol Cajero**. Otros roles que intenten acceder por URL son redirigidos a *Acceso Denegado*. |
| **Transacciones a la Misma Cuenta Origen-Destino** | ❌ Fuera de Alcance | En transacciones a terceros o entre cuentas, la cuenta de origen y destino **no pueden ser idénticas**. |
| **Rollback de Transacción por Fallo de Notificación SMTP** | ❌ Fuera de Alcance | Si ocurre un fallo en el servidor de correos al enviar la notificación de una transacción o pago, **la operación NO se revierte**. Se registra el pago y se notifica al usuario con un mensaje informativo. |
| **Transacciones de Depósito/Retiro sin Registro de Cajero** | ❌ Fuera de Alcance | Todo movimiento en caja exige registrar explícitamente el `UsuarioResponsableId` del cajero en sesión. No se permiten transacciones anónimas. |

---

### 2.6. Gestión de Comercios y API Hermes Pay

| Funcionalidad / Caso de Uso | Estado | Descripción / Justificación |
|---|---|---|
| **Creación Automática del Usuario de API al Crear Comercio** | ❌ Fuera de Alcance | El endpoint `POST /api/commerce` **únicamente crea los datos comerciales**. El usuario con rol Comercio debe crearse en un paso independiente mediante `POST /api/users/commerce/{commerceId}`. |
| **Múltiples Usuarios de API por Comercio** | ❌ Fuera de Alcance | Un comercio solo puede tener **un (1) usuario de API asociado** a su cuenta comercial. |
| **Procesamiento de Pagos para Comercios Inactivos** | ❌ Fuera de Alcance | Los comercios desactivados (`isActive = false`) **tienen bloqueado el procesamiento de pagos** en Hermes Pay. |
| **Cuerpos de Respuesta JSON Estándar para Errores de Comercio** | ❌ Fuera de Alcance | El PDF de requerimientos no especifica schemas JSON de respuesta para errores de validación en Comercios (solo para casos de éxito `200/201` y conflicto `409` en préstamos). |

---

## 3. Exclusiones Técnicas, de Arquitectura e Infraestructura

1. **Integración con Redes Bancarias Externas / ACH Interbancario:**  
   El sistema opera dentro de una entidad simulada en un entorno cerrado. No se contempla comunicación con el Banco Central, ACH ni redes de procesamiento internacionales (Visa/Mastercard reales).
2. **Persistencia Física de Tarjetas de Crédito / Chips EMV:**  
   No se gestiona logística ni emisión física de plásticos bancarios.
3. **Modificación de Historial Transaccional:**  
   La bitácora de transacciones es de **solo lectura e inmutable**. No existen endpoints ni pantallas para editar o eliminar transacciones registradas.
4. **Autenticación Multifactor (MFA / 2FA Biométrico):**  
   El alcance de seguridad contempla autenticación estándar por JWT / ASP.NET Core Identity y tokens de confirmación por correo electrónico.

---

> **Última Actualización:** 2026-08-19  
> **Elaborado por:** Equipo de Desarrollo Antigravity AI  
> **Estado:** Documento de Referencia Oficial de Alcance
