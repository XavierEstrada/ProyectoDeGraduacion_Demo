# SGIO — Sistema de Gestión Integral de Obras

Sistema web para la administración de proyectos de construcción. Permite gestionar proyectos, fases, tareas, hitos, inventario, proveedores, empleados y facturas desde una sola plataforma, con tablero Kanban, dashboard ejecutivo, adjuntos de comprobantes, historial de actividad y búsqueda global. Desarrollado como proyecto de graduación y adaptado como demo de portafolio, con control de acceso granular por rol y diseño totalmente responsivo.

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
- [Despliegue](#despliegue)
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
- Marcar tareas como completadas / en progreso con guardado automático vía AJAX (sin recarga de página)
- Asignar cliente al proyecto con buscador en tiempo real por nombre o correo
- Controlar hitos: crear, cambiar estado (Completo / Pendiente / En Progreso / Aprobado / Rechazado) y eliminar
- Dashboard por proyecto con gráficos Chart.js: progreso general, distribución de tareas, avance por fase, Gantt de tareas y costo por fase

### Dashboard Ejecutivo
- Vista consolidada de todos los proyectos en un solo lugar (`/Proyecto/DashboardGeneral`)
- Total invertido, distribución de proyectos por estado y costo por proyecto
- Próximos hitos por vencer entre todos los proyectos activos

### Tablero Kanban
- Tablero por proyecto (`/Proyecto/Kanban/{id}`) con columnas Pendiente / En Progreso / Completada
- Arrastrar y soltar (HTML5 Drag & Drop nativo, sin librerías externas) para mover tareas entre columnas
- Cambio de estado también disponible como menú desplegable sobre la tarjeta, con guardado AJAX

### Módulos de Gestión
- **Inventario:** productos con categoría, cantidad, precio unitario y cálculo automático de precio total
- **Proveedores:** directorio con estado activo/inactivo y datos de contacto
- **Empleados:** registro con nombre, apellido y correo
- **Facturas:** registro de facturas con impuestos, totales, estados y comprobantes adjuntos (PDF/imagen)
- **Cierres Financieros:** resúmenes anuales y mensuales (acceso solo desde URL directa, no en menú)

### Archivos Adjuntos
- Comprobantes (PDF, PNG, JPG, WEBP — máx. 10 MB) subidos a **Supabase Storage** y vinculados a cada factura
- Subida y borrado desde un modal en la vista de Facturas, sin salir de la página
- Solo Administrador/Supervisor pueden subir o eliminar adjuntos

### Historial de Actividad (Auditoría)
- Registro automático de las últimas 200 acciones del sistema (crear, editar, eliminar) con usuario, entidad y fecha
- Vista exclusiva para el rol Administrador (`/Administrativo/HistorialActividad`)

### Búsqueda Global
- Barra de búsqueda en el navbar que consulta proyectos, facturas y proveedores en tiempo real (`/Busqueda/Global`)
- Respeta las mismas restricciones de rol que el resto del sistema: un cliente (Usuario) solo encuentra sus propios proyectos, y un Empleado no puede llegar a Facturas/Proveedores desde ahí

### Administración de Usuarios
- Crear usuarios con rol asignado desde el panel administrativo (contraseña cifrada con AES-256 desde la creación)
- Cambiar rol de cualquier usuario desde un modal en la propia tabla (sin navegar a otra página)
- Activar o bloquear el acceso al sistema
- Vista con estadísticas por rol y estado de cuenta

### Control de Acceso por Rol
- Además de `[Authorize(Roles = "...")]` por acción, un `IAsyncActionFilter` global (`RestriccionUsuarioClienteFilter`) restringe la navegación de los roles "de campo":
  - **Usuario** (cliente externo): solo ve sus propios proyectos, en modo lectura total
  - **Empleado** (personal de campo): ve todos los proyectos y puede actualizar el estado de tareas/hitos, pero no tiene acceso a Facturas, Inventario, Proveedores ni Empleados, y no puede crear/editar/eliminar nada
- La restricción aplica incluso en rutas `[AllowAnonymous]` pensadas para visitantes del demo, y también en la búsqueda global

### Diseño
- Layout fijo: sidebar fijo (`position: fixed`) + top navbar sticky, con sidebar off-canvas en móvil/tablet
- Sistema de diseño coherente: `page-header`, `sgio-table`, `stat-mini`, `badge-status`, `btn-action`
- Tablas con DataTables (búsqueda, paginación, ordenamiento) en todos los módulos
- Totalmente responsivo con Bootstrap 5.3: en pantallas ≤768px las tablas se transforman en tarjetas apiladas (una fila = una tarjeta con etiqueta + valor) en vez de requerir scroll horizontal

---

## Stack Tecnológico

| Capa | Tecnología | Versión |
|---|---|---|
| Framework | ASP.NET Core MVC | .NET 8 |
| ORM | Entity Framework Core | 8.x |
| Base de datos | PostgreSQL | 17.x |
| Driver BD | Npgsql.EntityFrameworkCore.PostgreSQL | 8.x |
| Autenticación | Cookie Authentication | ASP.NET Core |
| UI | Bootstrap | 5.3 |
| Tablas | DataTables | 2.1.8 |
| Gráficos | Chart.js | 4.4.0 |
| JavaScript | jQuery + Vanilla JS | 3.7.0 |
| Iconos | Bootstrap Icons | 1.x |
| Fuente | Poppins | Google Fonts |
| JSON | Newtonsoft.Json | 13.x |
| Almacenamiento de archivos | Supabase Storage (REST API) | — |
| Contenedor | Docker | — |

---

## Arquitectura

```
ProyectoSGIOCore/
├── Controllers/          # Lógica de negocio y enrutamiento
├── Filters/              # RestriccionUsuarioClienteFilter (IAsyncActionFilter global)
├── Models/               # Entidades de la base de datos
├── ViewModels/           # DTOs para vistas y formularios
├── Views/                # Razor Views (.cshtml) por módulo
│   └── Shared/           # Layout principal y partials
├── Data/
│   └── AppDBContext.cs   # DbContext de Entity Framework
├── Services/             # Servicios auxiliares (utilidades, SMTP, actividad, Supabase Storage)
├── Migrations/           # InitialCreate + migraciones incrementales para PostgreSQL
├── Dockerfile            # Imagen Docker para despliegue
└── wwwroot/
    ├── css/styles.css    # Sistema de diseño global (incluye breakpoints responsive)
    ├── js/Menu.js        # Toggle sidebar + submenús + off-canvas en móvil
    └── lib/              # Bootstrap, jQuery, validaciones
```

El proyecto sigue el patrón **MVC estricto**: los controladores consultan directamente el `AppDBContext` mediante EF Core con carga ansiosa (`Include` / `ThenInclude`). No hay capa de repositorio intermedia. El control de acceso combina `[Authorize(Roles = "...")]` por acción con un filtro global (`Filters/RestriccionUsuarioClienteFilter.cs`) para restricciones de navegación que no se pueden expresar solo con atributos.

Las migraciones se aplican automáticamente al arrancar la aplicación mediante `db.Database.Migrate()` en `Program.cs`.

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
| Dashboard por proyecto | `GET /Proyecto/Dashboard/{id}` | Público |
| Dashboard ejecutivo (todos los proyectos) | `GET /Proyecto/DashboardGeneral` | Público |
| Tablero Kanban | `GET /Proyecto/Kanban/{id}` | Público (lectura) |
| Mover tarea entre columnas del Kanban | `POST /Proyecto/ActualizarColumnaKanban` | Admin, Supervisor, Empleado |
| Cambiar estado de un hito (vista simplificada) | `POST /Proyecto/ActualizarEstadoHito` | Admin, Supervisor, Empleado |
| Asignar cliente | `POST /Proyecto/AsignarCliente` | Admin, Supervisor |
| Agregar fase | `POST /Proyecto/AgregarFase` | Admin, Supervisor |
| Eliminar fase | `POST /Proyecto/EliminarFase` | Admin, Supervisor |
| Agregar tareas | `POST /Proyecto/AgregarTareasModal` | Admin, Supervisor |
| Actualizar estado tareas | `POST /Proyecto/ActualizarTareas` | Admin, Supervisor |
| Crear hito | `POST /Proyecto/AsignarHito` | Admin, Supervisor |
| Editar hito (descripción, responsable, estado, fecha) | `POST /Proyecto/EditarHito` | Admin, Supervisor |
| Eliminar hito | `POST /Proyecto/EliminarHito` | Admin, Supervisor |

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
CRUD completo de productos. El campo `PrecioTotal` es calculado (`Cantidad × PrecioUnidad`). El campo `Stock` es una propiedad calculada en el modelo (`Cantidad > 0`). Exportación a HTML y CSV. Acceso de lectura público.

### Proveedores (`/Proveedores`)
Directorio de empresas proveedoras. Estado booleano (`Activo`). Toggle de estado sin eliminación física. Exportación a HTML y CSV (codificación UTF-8 con BOM y campos escapados para compatibilidad con Excel).

### Empleados (`/Empleado`)
Registro simple: nombre, apellido, correo. Se usan como responsables de hitos en proyectos.

### Facturas (`/Facturas`)
CRUD completo: registrar, editar y eliminar facturas por proveedor, con numeración correlativa por año y cálculo automático de impuestos (16%). Exportación a HTML. Cada factura puede tener comprobantes adjuntos (PDF/imagen) almacenados en Supabase Storage, gestionados desde un modal (`SubirAdjuntoFactura` / `EliminarAdjuntoFactura`, solo Admin/Supervisor). Incluye módulo de Cierres Financieros anuales y mensuales (accesibles por URL, no visibles en el menú lateral).

### Usuarios (`/Administrativo`)
Solo accesible para el rol **Administrador**.

| Acción | Ruta |
|---|---|
| Listar usuarios | `GET /Administrativo/VisualizarUsuarios` |
| Crear usuario | `GET/POST /Administrativo/CrearUsuario` |
| Cambiar rol | `POST /Administrativo/CambiarRol` (modal en la propia tabla) |
| Activar / Bloquear | `POST /Administrativo/CambiarEstado` |
| Historial de actividad | `GET /Administrativo/HistorialActividad` |

### Búsqueda Global (`/Busqueda`)
| Acción | Ruta | Autenticación |
|---|---|---|
| Buscar proyectos, facturas y proveedores | `GET /Busqueda/Global?q=...` | Público, filtrado por rol |

---

## Roles y Permisos

| Rol | ID | Descripción |
|---|---|---|
| Administrador | 1 | Acceso total: usuarios, proyectos, inventario, facturas, historial de actividad |
| Supervisor | 2 | Proyectos, facturas, inventario, empleados, proveedores — sin gestión de usuarios |
| Empleado | 3 | Personal de campo: ve **todos** los proyectos (tabla, gestión, dashboard, Kanban) y puede actualizar el estado de tareas e hitos, pero no crea/edita/elimina nada y no tiene acceso a Facturas, Inventario, Proveedores ni Empleados |
| Usuario | 4 | Cliente externo: solo ve **sus propios** proyectos asignados, en modo 100% lectura (dashboard y detalle, sin Kanban ni ninguna acción) |

El control de acceso combina dos capas:

1. **`[Authorize(Roles = "...")]` por acción.** Nota importante: cuando se combina un atributo a nivel de clase con uno a nivel de método, ASP.NET Core los evalúa con lógica **AND** (deben cumplirse ambos), no de sobrescritura. Por eso el atributo de clase en `ProyectoController` usa el conjunto de roles más amplio (`Administrador, Supervisor, Empleado`) y cada acción que Empleado no debe tocar se restringe explícitamente a nivel de método (`Administrador, Supervisor`).
2. **`RestriccionUsuarioClienteFilter` (filtro global).** Cubre lo que `[Authorize]` no puede expresar por sí solo: varias rutas de consulta son `[AllowAnonymous]` para que un visitante del demo pueda navegar libremente, pero un **Usuario** o **Empleado** autenticado sí debe quedar limitado a su subconjunto de pantallas. El filtro redirige a `Proyecto/Proyectos` si el controlador/acción actual no está en la lista permitida para el rol de la sesión.

Los botones de acción en las vistas están además condicionados con `@if (User.Identity.IsAuthenticated)` y `@if (User.IsInRole("..."))` para ocultar controles que el backend igual rechazaría.

---

## Modelos de Datos

```csharp
// Usuarios y autenticación
Usuario        { IdUsuario, Nombre, Apellido, Correo, Clave*, Activo, Temporal, TwoFA, intentos, IdRol }
Rol            { IdRol, Nombre }

// Proyectos
Proyecto       { Id, Nombre, FechaCreacion, Estado (enum), IdUsuario? }
Fase           { Id, Nombre, ProyectoId }
Tarea          { Id, Nombre, FechaInicio, FechaFin, Completada, EnProgreso, Costo?, FaseId }
Hito           { ID, Descripcion, Fecha, estado (int), IdUsuario?, ProyectoId }

// Operaciones
Inventario     { ID, Nombre, Categoria, Cantidad, PrecioUnidad, PrecioTotal, InformacionAdicional? }
Proveedor      { IdProveedor, Nombre, Correo, Telefono, Direccion, Estado }
Empleado       { IdEmpleado, Nombre, Apellido, Correo }
FacturaProveedor { Id, NumeroFactura, ... }
CierreFinanciero { Id, ... }

// Auditoría y archivos
Adjunto          { Id, EntidadTipo, EntidadId, NombreArchivo, RutaStorage, UrlPublica, FechaSubida }
RegistroActividad { Id, UsuarioNombre, Accion, Entidad, Detalle, Fecha }
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
- **Restricción por rol en navegación:** `Filters/RestriccionUsuarioClienteFilter.cs` limita lo que un Usuario o Empleado autenticado puede visitar, incluso en rutas anónimas (ver [Roles y Permisos](#roles-y-permisos))

---

## Despliegue

La aplicación está contenerizada con Docker y desplegada en **Render** (Web Service, tier gratuito) con **Supabase PostgreSQL** como base de datos.

### Infraestructura
| Componente | Servicio |
|---|---|
| Aplicación | Render Web Service (Docker, free tier) |
| Base de datos | Supabase PostgreSQL (free tier) |
| Mantenimiento de BD | GitHub Actions (`supabase-keep-alive.yml`) |

> El free tier de Render "duerme" el servicio tras 15 minutos sin tráfico (la primera visita tras eso tarda ~30-50s en responder). El free tier de Supabase pausa el proyecto tras 7 días sin actividad — por eso existe el workflow de mantenimiento (ver más abajo).

### Dockerfile
La imagen usa un build multi-stage:
1. **Build stage** (`mcr.microsoft.com/dotnet/sdk:8.0`): compila y publica la app
2. **Runtime stage** (`mcr.microsoft.com/dotnet/aspnet:8.0`): imagen final ligera

La app corre en el puerto indicado por la variable `PORT` que inyecta Render (`ASPNETCORE_URLS=http://+:8080` en el Dockerfile, con `PORT=8080` configurado en Render).

### Migraciones automáticas
Al arrancar, `Program.cs` ejecuta `db.Database.Migrate()` que aplica cualquier migración pendiente. No se requiere intervención manual al desplegar.

### Variables requeridas en Render
| Variable | Descripción |
|---|---|
| `PORT` | Puerto en el que Render enruta el tráfico hacia el contenedor (`8080`) |
| `ConnectionStrings__DefaultConnection` | Cadena de conexión al *connection pooler* de Supabase (modo *session*, puerto 5432) |
| `settings__SecretKey` | Clave AES-256 de 32 caracteres para cifrado de contraseñas |
| `settings__SupabaseUrl` | URL del proyecto de Supabase (para el bucket de Storage) |
| `settings__SupabaseServiceRoleKey` | `service_role` key de Supabase — solo se usa server-side para subir/eliminar adjuntos, nunca se expone al cliente |

> La recuperación de cuenta por correo (`settings__correoSMTP` / `settings__claveSMTP`) no está activa en el demo desplegado — esas variables se dejan vacías y esa funcionalidad queda deshabilitada en producción.

> Los comprobantes de facturas se guardan en un bucket de **Supabase Storage** llamado `adjuntos` (debe existir en el proyecto de Supabase antes de usar la función de adjuntos).

### Mantenimiento automático de la base de datos
El workflow [`.github/workflows/supabase-keep-alive.yml`](.github/workflows/supabase-keep-alive.yml) corre cada 5 días vía GitHub Actions (`schedule` + `workflow_dispatch` para ejecutarlo manualmente) y hace una lectura mínima a la API REST de Supabase, sin modificar datos, solo para mantener el proyecto activo. Requiere los secrets de repositorio `SUPABASE_URL` y `SUPABASE_ANON_KEY`.

---

## Configuración Local

### Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- PostgreSQL 14+ (local o instancia cloud)
- Visual Studio 2022 o VS Code con extensión C#

### Pasos

**1. Clonar el repositorio**
```bash
git clone https://github.com/tu-usuario/ProyectoDeGraduacion_Demo.git
cd ProyectoDeGraduacion_Demo
```

**2. Configurar la conexión a la base de datos**

El proyecto usa [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) para las credenciales locales (no se commitean, se guardan fuera del repositorio):
```bash
cd ProyectoSGIO/ProyectoSGIOCore
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=sgio_dev;Username=postgres;Password=tu_password"
dotnet user-secrets set "settings:SecretKey" "una_clave_de_32_caracteres_aqui"

# Opcional: solo necesario para probar la subida de adjuntos en Facturas
dotnet user-secrets set "settings:SupabaseUrl" "https://tu-proyecto.supabase.co"
dotnet user-secrets set "settings:SupabaseServiceRoleKey" "tu_service_role_key"
```

`appsettings.Development.json` y `appsettings.json` se mantienen en el repo con los valores de conexión y clave **vacíos** a propósito — en desarrollo se completan vía user secrets, y en producción vía variables de entorno (ver [Despliegue](#despliegue)).

**3. Ejecutar el proyecto**
```bash
cd ProyectoSGIO/ProyectoSGIOCore
dotnet run
```

Las migraciones se aplican automáticamente al arrancar. La aplicación abre en `https://localhost:{puerto}`.

**4. Primer acceso**

El sistema crea automáticamente los roles y un usuario administrador inicial al aplicar la migración. Puedes crear usuarios adicionales desde `/Administrativo/CrearUsuario`.

### Ejecutar con Docker localmente
```bash
cd ProyectoSGIO/ProyectoSGIOCore
docker build -t sgio-demo .
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=sgio_dev;Username=postgres;Password=tu_password" \
  -e settings__SecretKey="tu_clave_de_32_caracteres_aqui__" \
  sgio-demo
```

---

## Estructura del Proyecto

```
ProyectoDeGraduacion_Demo/
├── README.md
└── ProyectoSGIO/
    └── ProyectoSGIOCore/
        ├── Dockerfile
        ├── .dockerignore
        ├── Controllers/
        │   ├── AccesoController.cs          # Login, registro, perfil, recuperación
        │   ├── AdministrativoController.cs  # Gestión de usuarios, roles, historial de actividad
        │   ├── ProyectoController.cs        # Proyectos, fases, tareas, hitos, Kanban, dashboards
        │   ├── FacturasController.cs        # Facturas, cierres financieros, adjuntos
        │   ├── CierresFinancierosController.cs
        │   ├── InventarioController.cs
        │   ├── ProveedoresController.cs
        │   ├── EmpleadoController.cs
        │   ├── BusquedaController.cs        # Búsqueda global (navbar), filtrada por rol
        │   └── HomeController.cs
        │
        ├── Filters/
        │   └── RestriccionUsuarioClienteFilter.cs  # Restricción de navegación por rol (Usuario/Empleado)
        │
        ├── Models/
        │   ├── Usuario.cs / Rol.cs
        │   ├── Proyecto.cs / Fase.cs / Tarea.cs / Hito.cs
        │   ├── Inventario.cs / Proveedor.cs / Empleado.cs
        │   ├── FacturaProveedor.cs / CierreFinanciero.cs
        │   ├── Adjunto.cs                   # Comprobantes en Supabase Storage
        │   ├── RegistroActividad.cs         # Entrada del historial de actividad
        │   └── UtilitariosModel.cs          # Cifrado AES, envío SMTP
        │
        ├── ViewModels/
        │   ├── IniciarSesionVM.cs
        │   ├── CrearUsuarioVM.cs
        │   ├── UsuarioVM.cs
        │   ├── EstadoHitoVM.cs
        │   └── HitoResumenVM.cs             # Próximos hitos en el dashboard ejecutivo
        │
        ├── Data/
        │   └── AppDBContext.cs
        │
        ├── Services/
        │   ├── UtilitariosModel.cs / IUtilitariosModel.cs
        │   ├── ActividadService.cs / IActividadService.cs   # Registro del historial de actividad
        │   └── SupabaseStorageService.cs / ISupabaseStorageService.cs  # Subida/borrado en Supabase Storage
        │
        ├── Migrations/                      # InitialCreate + migraciones incrementales (PostgreSQL)
        │
        ├── Views/
        │   ├── Acceso/                      # Login, registro, perfil, 2FA
        │   ├── Administrativo/              # Usuarios, roles, historial de actividad
        │   ├── Proyecto/                    # Proyectos, gestión, dashboards, Kanban
        │   ├── Facturas/                    # Facturas, cierres, adjuntos
        │   ├── Inventario/
        │   ├── Proveedores/
        │   ├── Empleado/
        │   ├── Home/                        # Landing page del demo
        │   └── Shared/
        │       ├── _Layout.cshtml           # Sidebar + top navbar (responsive)
        │       └── _LayoutExterno.cshtml    # Layout para login/registro
        │
        ├── wwwroot/
        │   ├── css/
        │   │   ├── styles.css               # Sistema de diseño completo
        │   │   └── Layout_Externo.css       # Estilos para login/registro
        │   ├── js/
        │   │   ├── Menu.js                  # Sidebar toggle + submenús
        │   │   └── funciones.js
        │   └── lib/                         # Bootstrap, jQuery, validaciones
        │
        ├── appsettings.json                 # Configuración base (sin credenciales)
        └── Program.cs
```

---

## Variables de Entorno

| Clave | Descripción |
|---|---|
| `ConnectionStrings__DefaultConnection` | Cadena de conexión PostgreSQL (formato Npgsql) |
| `settings__SecretKey` | Clave AES-256 para cifrado de contraseñas (32 caracteres) |
| `settings__correoSMTP` | Correo origen para envío de recuperación de cuenta |
| `settings__claveSMTP` | Contraseña del correo SMTP |
| `settings__SupabaseUrl` | URL del proyecto de Supabase (Storage) |
| `settings__SupabaseServiceRoleKey` | `service_role` key de Supabase, solo para uso server-side |

> En ASP.NET Core, el separador `__` en variables de entorno equivale a `:` en `appsettings.json`. Así `settings__SecretKey` mapea a `settings:SecretKey`.

> `appsettings.Development.json` está en `.gitignore` y nunca se commitea — contiene credenciales locales.

---

## Decisiones de Diseño

**¿Por qué AES-256 en lugar de hashing?**
El sistema incluye recuperación de cuenta por correo enviando la contraseña original. Esto requiere cifrado reversible. En un entorno de producción real se recomendaría migrar a hashing con bcrypt/Argon2 más un flujo de restablecimiento de contraseña.

**¿Por qué sin repositorio pattern?**
El proyecto es un demo académico de alcance acotado. Los controladores acceden directamente al `AppDBContext`. Para escalar a producción se recomendaría introducir una capa de servicios o repositorios.

**¿Por qué acceso anónimo en las vistas de consulta?**
El objetivo del demo es que reclutadores y visitantes puedan explorar el sistema sin necesidad de credenciales. Las acciones de escritura siguen protegidas con `[Authorize]`.

**¿Por qué PostgreSQL en lugar de SQL Server?**
El proyecto originalmente usaba SQL Server (Azure SQL). Para el demo de portafolio se migró a PostgreSQL por compatibilidad con plataformas de hosting gratuitas (Render, Supabase) que no ofrecen SQL Server en tier gratuito.

**¿Por qué Render + Supabase y no un solo proveedor?**
Se probó Railway primero, pero su tier gratuito resultó demasiado limitado para mantener el demo activo. Render (hosting) + Supabase (base de datos) es la combinación gratuita más simple de configurar sin tarjeta de crédito, a cambio de aceptar que ambos servicios "duerman" por inactividad — mitigado con el workflow de mantenimiento automático.

---

## Licencia

Proyecto académico — uso libre para fines educativos y de portafolio.
