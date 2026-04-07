# SGIO — Sistema de Gestión Integral de Obras

Sistema web para la administración de proyectos de construcción. Permite gestionar proyectos, fases, tareas, hitos, inventario, proveedores, empleados y facturas desde una sola plataforma. Desarrollado como proyecto de graduación y adaptado como demo de portafolio.

**Demo público:** acceso de solo lectura sin autenticación. Credenciales de administrador disponibles en la pantalla de inicio.

---

## Tabla de Contenidos

- [Características](#características)
- [Stack Tecnológico](#stack-tecnológico)
- [Arquitectura](#arquitectura)
- [Módulos del Sistema](#módulos-del-sistema)
- [Roles y Permisos](#roles-y-permisos)
- [Modelos de Datos](#modelos-de-datos)
- [Autenticación y Seguridad](#autenticación-y-seguridad)
- [Configuración Local](#configuración-local)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Variables de Entorno](#variables-de-entorno)

---

## Características

### Acceso Público
- Todas las vistas de consulta son accesibles sin iniciar sesión (`[AllowAnonymous]`)
- Los controles de creación, edición y eliminación solo aparecen para usuarios autenticados
- Página de inicio con descripción del sistema y credenciales de demo

### Gestión de Proyectos
- Crear proyectos con fases y tareas definidas desde el formulario inicial
- Validación completa en cliente (JS) antes de enviar el formulario — los datos no se pierden ante errores
- Gestionar fases y tareas desde la vista de administración del proyecto
- Marcar tareas como completadas con guardado automático vía AJAX (sin recarga de página)
- Asignar cliente al proyecto con buscador en tiempo real por nombre o correo
- Controlar hitos: crear, cambiar estado (Completo / Pendiente / En Progreso / Aprobado / Rechazado) y eliminar
- Dashboard por proyecto con gráficos Chart.js: progreso general, distribución de tareas, avance por fase

### Módulos de Gestión
- **Inventario:** productos con categoría, cantidad, precio unitario y cálculo automático de precio total
- **Proveedores:** directorio con estado activo/inactivo y datos de contacto
- **Empleados:** registro con nombre, apellido y correo
- **Facturas:** registro de facturas con impuestos, totales y estados
- **Cierres Financieros:** resúmenes anuales y mensuales (acceso solo desde URL directa, no en menú)

### Administración de Usuarios
- Crear usuarios con rol asignado desde el panel administrativo
- Cambiar rol de cualquier usuario
- Activar o bloquear el acceso al sistema
- Vista con estadísticas por rol y estado de cuenta

### Diseño
- Layout fijo: sidebar fijo (`position: fixed`) + top navbar sticky
- Sistema de diseño coherente: `page-header`, `sgio-table`, `stat-mini`, `badge-status`, `btn-action`
- Tablas con DataTables (búsqueda, paginación, ordenamiento) en todos los módulos
- Totalmente responsivo con Bootstrap 5.3

---

## Stack Tecnológico

| Capa | Tecnología | Versión |
|---|---|---|
| Framework | ASP.NET Core MVC | .NET 8 |
| ORM | Entity Framework Core | 8.x |
| Base de datos | SQL Server | Express / Azure SQL |
| Autenticación | Cookie Authentication | ASP.NET Core Identity |
| UI | Bootstrap | 5.3 |
| Tablas | DataTables | 2.1.8 |
| Gráficos | Chart.js | 4.4.0 |
| JavaScript | jQuery + Vanilla JS | 3.7.0 |
| Iconos | Bootstrap Icons | 1.x |
| Fuente | Poppins | Google Fonts |
| JSON | Newtonsoft.Json | 13.x |

---

## Arquitectura

```
ProyectoSGIOCore/
├── Controllers/          # Lógica de negocio y enrutamiento
├── Models/               # Entidades de la base de datos
├── ViewModels/           # DTOs para vistas y formularios
├── Views/                # Razor Views (.cshtml) por módulo
│   └── Shared/           # Layout principal y partials
├── Data/
│   └── AppDBContext.cs   # DbContext de Entity Framework
├── Services/             # Servicios auxiliares (utilidades, SMTP)
├── Migrations/           # Historial de migraciones EF Core
└── wwwroot/
    ├── css/styles.css    # Sistema de diseño global
    ├── js/menu.js        # Toggle sidebar + submenús
    └── lib/              # Bootstrap, jQuery, validaciones
```

El proyecto sigue el patrón **MVC estricto**: los controladores consultan directamente el `AppDBContext` mediante EF Core con carga ansiosa (`Include` / `ThenInclude`). No hay capa de repositorio intermedia.

---

## Módulos del Sistema

### Proyectos (`/Proyecto`)
| Acción | Ruta | Autenticación |
|---|---|---|
| Listar proyectos | `GET /Proyecto/Proyectos` | Público |
| Crear proyecto | `GET/POST /Proyecto/CrearProyecto` | Requerida |
| Editar nombre/estado | `GET/POST /Proyecto/EditarProyecto/{id}` | Requerida |
| Eliminar proyecto | `POST /Proyecto/EliminarProyecto` | Requerida |
| Gestionar fases/tareas/hitos | `GET /Proyecto/GestionarProyecto/{id}` | Público (lectura) |
| Dashboard analítico | `GET /Proyecto/Dashboard/{id}` | Público |
| Asignar cliente | `POST /Proyecto/AsignarCliente` | Requerida |
| Agregar fase | `POST /Proyecto/AgregarFase` | Requerida |
| Eliminar fase | `POST /Proyecto/EliminarFase` | Requerida |
| Agregar tareas | `POST /Proyecto/AgregarTareasModal` | Requerida |
| Actualizar estado tareas | `POST /Proyecto/ActualizarTareas` | Requerida |
| Crear hito | `POST /Proyecto/AsignarHito` | Requerida |
| Cambiar estado hito | `POST /Proyecto/ActualizarEstadoHito` | Requerida |
| Eliminar hito | `POST /Proyecto/EliminarHito` | Requerida |

**Jerarquía de datos:**
```
Proyecto
├── Estado: Planificacion | Progreso | Completado | Pendiente
├── Cliente (Usuario con rol "Usuario")
├── Fases[]
│   └── Tareas[]
│       ├── FechaInicio / FechaFin
│       ├── Costo (decimal?)
│       └── Completada (bool)
└── Hitos[]
    ├── Descripcion
    ├── Responsable (Usuario con rol "Empleado")
    ├── Fecha
    └── Estado: 1=Completo 2=Pendiente 3=En Progreso 4=Aprobado 5=Rechazado
```

### Inventario (`/Inventario`)
CRUD completo de productos. El campo `PrecioTotal` es calculado (`Cantidad × PrecioUnidad`). El campo `Stock` es una propiedad calculada en el modelo (`Cantidad > 0`). Acceso de lectura público.

### Proveedores (`/Proveedores`)
Directorio de empresas proveedoras. Estado booleano (`Activo`). Toggle de estado sin eliminación física.

### Empleados (`/Empleado`)
Registro simple: nombre, apellido, correo. Se usan como responsables de hitos en proyectos.

### Facturas (`/Facturas`)
Registro de facturas con número, proveedor, monto, impuestos y estado. Incluye módulo de Cierres Financieros anuales y mensuales (accesibles por URL, no visibles en el menú lateral).

### Usuarios (`/Administrativo`)
Solo accesible para el rol **Administrador**.

| Acción | Ruta |
|---|---|
| Listar usuarios | `GET /Administrativo/VisualizarUsuarios` |
| Crear usuario | `GET/POST /Administrativo/CrearUsuario` |
| Cambiar rol | `GET/POST /Administrativo/CambiarRol/{id}` |
| Activar / Bloquear | `POST /Administrativo/CambiarEstado` |

---

## Roles y Permisos

| Rol | ID | Descripción |
|---|---|---|
| Administrador | 1 | Acceso total: usuarios, proyectos, inventario, facturas |
| Supervisor | 2 | Proyectos, facturas, inventario, empleados, proveedores |
| Empleado | 3 | Lectura general, puede ser asignado como responsable de hitos |
| Usuario | 4 | Cliente externo, puede ser asignado a proyectos |

El control de acceso se implementa con `[Authorize(Roles = "...")]` por acción. Las vistas de consulta usan `[AllowAnonymous]`. Los botones de acción en las vistas están condicionados con `@if (User.Identity.IsAuthenticated)` y `@if (User.IsInRole("..."))`.

---

## Modelos de Datos

```csharp
// Usuarios y autenticación
Usuario        { IdUsuario, Nombre, Apellido, Correo, Clave*, Activo, Temporal, TwoFA, intentos, IdRol }
Rol            { IdRol, Nombre }

// Proyectos
Proyecto       { Id, Nombre, FechaCreacion, Estado (enum), IdUsuario? }
Fase           { Id, Nombre, ProyectoId }
Tarea          { Id, Nombre, FechaInicio, FechaFin, Completada, Costo?, FaseId }
Hito           { ID, Descripcion, Fecha, estado (int), IdUsuario?, ProyectoId }

// Operaciones
Inventario     { ID, Nombre, Categoria, Cantidad, PrecioUnidad, PrecioTotal, InformacionAdicional? }
Proveedor      { IdProveedor, Nombre, Correo, Telefono, Direccion, Estado }
Empleado       { IdEmpleado, Nombre, Apellido, Correo }
FacturaProveedor { Id, NumeroFactura, ... }
CierreFinanciero { Id, ... }
```

`*` La contraseña se almacena cifrada con **AES-256** usando la clave configurada en `settings:SecretKey`.

---

## Autenticación y Seguridad

- **Sesiones:** Cookie-based con expiración de 30 minutos (`ExpireTimeSpan`)
- **Cifrado de contraseñas:** AES-256 simétrico (no hashing — permite recuperación de cuenta)
- **Recuperación de cuenta:** envío de contraseña temporal por correo SMTP
- **Bloqueo de cuenta:** se bloquea tras intentos fallidos (`intentos`) o manualmente desde el panel administrativo
- **2FA:** soporte implementado en base de datos (`TwoFA bool`), deshabilitado en el menú de usuario (lógica preservada)
- **Acceso público:** rutas de solo lectura decoradas con `[AllowAnonymous]`, con guardias en vistas para ocultar acciones

---

## Configuración Local

### Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- SQL Server Express (o SQL Server Developer)
- Visual Studio 2022 o VS Code con extensión C#

### Pasos

**1. Clonar el repositorio**
```bash
git clone https://github.com/tu-usuario/ProyectoDeGraduacion_Demo.git
cd ProyectoDeGraduacion_Demo
```

**2. Configurar la conexión a la base de datos**

Edita `ProyectoSGIO/ProyectoSGIOCore/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=TU_SERVIDOR\\SQLEXPRESS;Database=SGIO_Dev;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;"
  }
}
```
Reemplaza `TU_SERVIDOR` con el nombre de tu máquina o instancia SQL Server.

**3. Aplicar migraciones**

Desde la Consola del Administrador de Paquetes en Visual Studio:
```powershell
Update-Database
```

O desde terminal:
```bash
cd ProyectoSGIO/ProyectoSGIOCore
dotnet ef database update
```

**4. Ejecutar el proyecto**
```bash
dotnet run
```

O presiona `F5` en Visual Studio. La aplicación abre en `https://localhost:{puerto}`.

**5. Primer acceso**

Crea el primer usuario administrador directamente en la base de datos o mediante el registro si está habilitado. El sistema crea automáticamente las tablas con la migración.

### Datos de prueba

En la carpeta raíz del repositorio (o en este README) se encuentran scripts SQL con `INSERT` de ejemplo para:
- `Inventario` — 23 registros de materiales de construcción
- `Proveedores` — 22 registros de empresas proveedoras
- `Empleados` — 23 registros de personal
- `Usuario` — 30 registros distribuidos en los 4 roles

> Para los registros de `Usuario`, el campo `Clave` debe usar el mismo cifrado AES-256 de la aplicación. Se recomienda crear usuarios desde la interfaz web (`/Administrativo/CrearUsuario`) o copiar el valor cifrado de un usuario ya existente.

---

## Estructura del Proyecto

```
ProyectoDeGraduacion_Demo/
├── README.md
└── ProyectoSGIO/
    └── ProyectoSGIOCore/
        ├── Controllers/
        │   ├── AccesoController.cs          # Login, registro, perfil, recuperación
        │   ├── AdministrativoController.cs  # Gestión de usuarios y roles
        │   ├── ProyectoController.cs        # Proyectos, fases, tareas, hitos
        │   ├── FacturasController.cs        # Facturas y cierres financieros
        │   ├── CierresFinancierosController.cs
        │   ├── InventarioController.cs
        │   ├── ProveedoresController.cs
        │   ├── EmpleadoController.cs
        │   └── HomeController.cs
        │
        ├── Models/
        │   ├── Usuario.cs / Rol.cs
        │   ├── Proyecto.cs / Fase.cs / Tarea.cs / Hito.cs
        │   ├── Inventario.cs / Proveedor.cs / Empleado.cs
        │   ├── FacturaProveedor.cs / CierreFinanciero.cs
        │   └── UtilitariosModel.cs          # Cifrado AES, envío SMTP
        │
        ├── ViewModels/
        │   ├── IniciarSesionVM.cs
        │   ├── CrearUsuarioVM.cs
        │   ├── UsuarioVM.cs
        │   └── EstadoHitoVM.cs
        │
        ├── Data/
        │   └── AppDBContext.cs
        │
        ├── Services/
        │
        ├── Migrations/                      # 20+ migraciones EF Core
        │
        ├── Views/
        │   ├── Acceso/                      # Login, registro, perfil, 2FA
        │   ├── Administrativo/              # Usuarios, roles, actividades
        │   ├── Proyecto/                    # Proyectos, gestión, dashboard
        │   ├── Facturas/                    # Facturas y cierres
        │   ├── Inventario/
        │   ├── Proveedores/
        │   ├── Empleado/
        │   ├── Home/                        # Landing page del demo
        │   └── Shared/
        │       ├── _Layout.cshtml           # Sidebar + top navbar
        │       └── _LayoutExterno.cshtml    # Layout para login/registro
        │
        ├── wwwroot/
        │   ├── css/
        │   │   └── styles.css               # Sistema de diseño completo
        │   ├── js/
        │   │   ├── menu.js                  # Sidebar toggle + submenús
        │   │   └── funciones.js
        │   └── lib/                         # Bootstrap, jQuery, validaciones
        │
        ├── appsettings.json
        ├── appsettings.Development.json     # Conexión local (no commitear)
        └── Program.cs
```

---

## Variables de Entorno

Configuradas en `appsettings.json` y sobreescritas en `appsettings.Development.json`:

| Clave | Descripción |
|---|---|
| `ConnectionStrings:DefaultConnection` | Cadena de conexión a SQL Server |
| `settings:SecretKey` | Clave AES-256 para cifrado de contraseñas (32 caracteres) |
| `settings:correoSMTP` | Correo origen para envío de recuperación de cuenta |
| `settings:claveSMTP` | Contraseña del correo SMTP |

> `appsettings.Development.json` no debe commitearse a repositorios públicos ya que puede contener credenciales locales. Asegúrate de que esté en `.gitignore`.

---

## Decisiones de Diseño

**¿Por qué AES-256 en lugar de hashing?**
El sistema incluye recuperación de cuenta por correo enviando la contraseña original. Esto requiere cifrado reversible. En un entorno de producción real se recomendaría migrar a hashing con bcrypt/Argon2 más un flujo de restablecimiento de contraseña.

**¿Por qué sin repositorio pattern?**
El proyecto es un demo académico de alcance acotado. Los controladores acceden directamente al `AppDBContext`. Para escalar a producción se recomendaría introducir una capa de servicios o repositorios.

**¿Por qué acceso anónimo en las vistas de consulta?**
El objetivo del demo es que reclutadores y visitantes puedan explorar el sistema sin necesidad de credenciales. Las acciones de escritura siguen protegidas con `[Authorize]`.

---

## Licencia

Proyecto académico — uso libre para fines educativos y de portafolio.
