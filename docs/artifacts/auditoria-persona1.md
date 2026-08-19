# Reporte de Auditoría: Módulos Persona 1

## Módulos Auditados
- **Funcionalidades generales**: Login, Seguridad
- **Funcionalidades del Administrador**: Dashboard(Home), Gestión de usuarios, Gestión de cuentas de ahorro
- **Funcionalidades del cliente**: Home(Listado de productos)
- **Funcionalidades del Api**: Seguridad, Login y Account Controller, Gestión de Usuarios

## Hallazgos de la Auditoría

### 1. WebApp: Login y Seguridad
**Estado:** Parcialmente Correcto / Con Observaciones Menores
- **Validaciones de Login**: Todas las validaciones principales están implementadas en el `AccountController.cs` (WebApp).
- **Mensajes de Error Exactos**:
  - `Los datos de acceso son inválidos.` (Correcto)
  - `Su cuenta se encuentra inactiva. Debe activar su cuenta...` (Correcto)
  - `Este usuario no tiene permisos para acceder a la aplicación web.` (Correcto)
  - En la vista de Acceso Denegado: `No posee permisos para acceder a esta sección.` (Correcto)
- **Activación de Cuenta**: 
  - `El enlace de activación no es válido.` (Correcto)
  - `Este enlace de activación ya fue utilizado.` (Correcto)
  - `Su cuenta ha sido activada correctamente. Ya puede iniciar sesión.` (Correcto)
- **Restablecimiento de Contraseña**:
  - `AccountController.cs` devuelve estáticamente *"El enlace de restablecimiento ha expirado. Solicite un nuevo restablecimiento de contraseña."* cuando la operación falla, pero el PDF exige diferenciar entre token inválido, token expirado y token utilizado. **Se requiere ajuste.**

### 2. WebApp: Funcionalidades del Administrador y Cliente
**Estado:** Pendiente de revisión profunda
- Las vistas y validaciones de `AdminHomeController`, `AdminUserController`, `AdminCuentaAhorroController` y `ClienteController` existen y tienen una estructura sólida. 
- Los mensajes exactos de error al activar/inactivar usuarios en `AdminUserController` deben ser verificados y corregidos contra el PDF para garantizar 100% de cumplimiento.

### 3. WebApi: Seguridad, Login, Account Controller y Gestión de Usuarios
**Estado:** Crítico (Faltan Controladores)
- Tras una revisión de la ruta `ArtemisBankingPro.Presentation.WebApi/Controllers/`, se detectó que **no existen los controladores `AccountController.cs` ni `UserController.cs`** para la API.
- El documento exige explícitamente los endpoints:
  - `POST /account/login`
  - `POST /account/confirm`
  - `POST /account/get-reset-token`
  - `POST /account/reset-password`
  - Módulo de "Gestión de Usuarios" en la API (`GET /api/users`, `POST /api/users`, etc.).
- En `CommerceController.cs`, el endpoint `[HttpPost("/api/users/commerce/{commerceId}")]` se encuentra implementado con un return estático `501 Fuera de alcance.`. Sin embargo, el documento de exclusiones especifica que lo "fuera de alcance" es la creación *automática*, pero el endpoint manual **debe estar implementado**.

## Acciones Tomadas
1. Se ha documentado exhaustivamente el estado actual.
2. Se recomiendan refactorizaciones masivas en la capa de Web API para implementar los módulos de Account y User Management requeridos por el alcance del proyecto.
3. Se recomienda modificar el controlador `AccountController.cs` en la capa WebApp para que parsee los errores de `IdentityResult` y devuelva los mensajes exactos del PDF.

> **Nota:** Debido a la magnitud de los controladores faltantes en la WebApi, se requiere un esfuerzo de desarrollo dedicado para generar los controladores completos y sus DTOs asociados.
