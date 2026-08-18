using Microsoft.AspNetCore.Mvc;
using ProyectoSGIOCore.Data;
using Microsoft.EntityFrameworkCore;
using ProyectoSGIOCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProyectoSGIOCore.ViewModels;
using ProyectoSGIOCore.Services;
using Newtonsoft.Json;
using System.Security.Claims;
using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace ProyectoSGIOCore.Controllers
{
    // Nota: [Authorize] de clase + de método se combinan con AND, no se sobrescriben.
    // Por eso la clase debe tener el conjunto MÁS AMPLIO de roles (incluye Empleado),
    // y cada acción que deba ser exclusiva de Administrador/Supervisor lleva su propio
    // [Authorize(Roles = "Administrador, Supervisor")] a nivel de método.
    [Authorize(Roles = "Administrador, Supervisor, Empleado")]
    public class ProyectoController : Controller
    {
        private readonly AppDBContext _dbContext;
        private readonly IActividadService _actividadService;
        private readonly IComentarioService _comentarioService;
        private static readonly HashSet<string> EntidadesComentario = new(StringComparer.OrdinalIgnoreCase) { "Tarea", "Hito" };

        public ProyectoController(AppDBContext context, IActividadService actividadService, IComentarioService comentarioService)
        {
            _dbContext = context;
            _actividadService = actividadService;
            _comentarioService = comentarioService;
        }

        //Proyectos
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Proyectos()
        {
            var query = _dbContext.Proyectos
                .Include(p => p.Fases)
                .ThenInclude(f => f.Tareas)
                .Include(p => p.Usuario)
                .AsQueryable();

            // Un cliente logueado (rol "Usuario") solo ve sus propios proyectos asignados
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Usuario"))
            {
                var idUsuarioActual = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                query = query.Where(p => p.IdUsuario == idUsuarioActual);
            }

            var proyectos = await query.OrderBy(p => p.Id).ToListAsync();

            return View(proyectos);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador, Supervisor")]
        public IActionResult CrearProyecto()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, Supervisor")]
        public async Task<IActionResult> CrearProyecto(Proyecto proyecto, List<Fase> fases)
        {
            if (string.IsNullOrEmpty(proyecto.Nombre))
            {
                TempData["MensajeError"] = "El nombre del proyecto no puede estar vacío.";
                return View(proyecto);
            }

            foreach (var fase in fases)
            {
                if (string.IsNullOrEmpty(fase.Nombre))
                {
                    TempData["MensajeError"] = "El nombre de la fase no puede estar vacío.";
                    return View(proyecto);
                }

                foreach (var tarea in fase.Tareas)
                {
                    if (string.IsNullOrEmpty(tarea.Nombre))
                    {
                        TempData["MensajeError"] = "El nombre de la tarea no puede estar vacío.";
                        return View(proyecto);
                    }

                    if (tarea.FechaInicio >= tarea.FechaFin)
                    {
                        TempData["MensajeError"] = "La fecha de fin de la tarea no puede ser menor o igual a la fecha de inicio.";
                        return View(proyecto);
                    }
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    proyecto.FechaCreacion = DateTime.Now;

                    // Comprobar si ya existe un proyecto con el mismo nombre (opcional)
                    var proyectoExistente = await _dbContext.Proyectos
                        .Include(p => p.Fases)
                        .ThenInclude(f => f.Tareas)
                        .FirstOrDefaultAsync(p => p.Nombre == proyecto.Nombre);

                    if (proyectoExistente != null)
                    {
                        TempData["MensajeError"] = "Ya existe un proyecto con ese nombre.";
                        return View(proyecto);
                    }

                    _dbContext.Proyectos.Add(proyecto);
                    await _dbContext.SaveChangesAsync();

                    var nombresFasesProcesadas = new HashSet<string>();

                    foreach (var fase in fases)
                    {
                        // Validar si la fase ya existe en el proyecto
                        bool faseExiste = await _dbContext.Fases
                            .AnyAsync(f => f.ProyectoId == proyecto.Id && f.Nombre == fase.Nombre);

                        if (faseExiste || nombresFasesProcesadas.Contains(fase.Nombre))
                        {
                            TempData["MensajeError"] = $"La fase '{fase.Nombre}' ya existe en este proyecto.";
                            continue;
                        }

                        nombresFasesProcesadas.Add(fase.Nombre);

                        fase.Id = 0;
                        fase.ProyectoId = proyecto.Id;
                        _dbContext.Fases.Add(fase);
                        await _dbContext.SaveChangesAsync();

                        foreach (var tarea in fase.Tareas)
                        {
                            tarea.Id = 0;
                            tarea.FaseId = fase.Id;
                            _dbContext.Tareas.Add(tarea);
                        }
                    }

                    await _dbContext.SaveChangesAsync();

                    await _actividadService.RegistrarAsync(User, "creó", "Proyecto", $"Proyecto '{proyecto.Nombre}'");

                    TempData["MensajeExito"] = $"Proyecto creado correctamente. Costo total: ${proyecto.CostoTotal:N2}";
                    return RedirectToAction("Proyectos");
                }
                catch (Exception ex)
                {
                    TempData["MensajeError"] = $"Ocurrió un error al crear el proyecto: {ex.Message}";
                    return View(proyecto);
                }
            }

            TempData["MensajeError"] = "El modelo no es válido. Por favor, verifica los datos ingresados.";
            return View(proyecto);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador, Supervisor")]
        public async Task<IActionResult> EditarProyecto(int id)
        {
            var proyecto = await _dbContext.Proyectos.FindAsync(id);
            if (proyecto == null)
            {
                TempData["MensajeError"] = "Proyecto no encontrado.";
                return RedirectToAction("Proyectos");
            }
            return View(proyecto);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, Supervisor")]
        public async Task<IActionResult> EditarProyecto(Proyecto entidad)
        {
            var proyecto = await _dbContext.Proyectos.FindAsync(entidad.Id);
            if (proyecto == null)
            {
                TempData["MensajeError"] = "Proyecto no encontrado.";
                return RedirectToAction("Proyectos");
            }

            proyecto.Nombre = entidad.Nombre;
            proyecto.Estado = entidad.Estado;
            await _dbContext.SaveChangesAsync();

            await _actividadService.RegistrarAsync(User, "editó", "Proyecto", $"Proyecto '{proyecto.Nombre}'");

            TempData["MensajeExito"] = $"Proyecto '{proyecto.Nombre}' editado correctamente.";
            return RedirectToAction("Proyectos");
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, Supervisor")]
        public async Task<IActionResult> EliminarProyecto(int proyectoId)
        {
            try
            {
                var proyecto = _dbContext.Proyectos
                    .Include(p => p.Fases)
                    .ThenInclude(f => f.Tareas)
                    .FirstOrDefault(p => p.Id == proyectoId);

                if (proyecto != null)
                {
                    var nombreProyecto = proyecto.Nombre;

                    // Eliminar tareas
                    foreach (var fase in proyecto.Fases)
                    {
                        _dbContext.Tareas.RemoveRange(fase.Tareas);
                    }

                    // Eliminar fases
                    _dbContext.Fases.RemoveRange(proyecto.Fases);

                    _dbContext.Hitos.RemoveRange(proyecto.Hitos);

                    // Eliminar proyecto
                    _dbContext.Proyectos.Remove(proyecto);
                    _dbContext.SaveChanges();

                    await _actividadService.RegistrarAsync(User, "eliminó", "Proyecto", $"Proyecto '{nombreProyecto}'");

                    TempData["MensajeExito"] = "El proyecto ha sido eliminado exitosamente.";
                }
                else
                {
                    TempData["MensajeError"] = "El proyecto no fue encontrado.";
                }
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Ocurrió un error al eliminar el proyecto. Intente nuevamente.";
                // Log del error
                Console.WriteLine(ex.Message);
            }

            return RedirectToAction("Proyectos");
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GestionarProyecto(int id)
        {
            var proyecto = await _dbContext.Proyectos
                .Include(p => p.Fases)
                .ThenInclude(f => f.Tareas)
                .Include(p => p.Hitos)
                .ThenInclude(u => u.Usuario)
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proyecto == null)
            {
                TempData["MensajeError"] = "Proyecto no encontrado.";
                return RedirectToAction("Proyectos");
            }

            var empleados = await _dbContext.Usuarios
                .Where(u => u.Rol.Nombre == "Empleado")
                .ToListAsync();

            var clientes = await _dbContext.Usuarios
                .Where(u => u.Rol.Nombre == "Usuario")
                .ToListAsync();

            var estadosHitos = new List<EstadoHitoVM>
            {
                new EstadoHitoVM { Id = 1, Nombre = "Completo" },
                new EstadoHitoVM { Id = 2, Nombre = "Pendiente" },
                new EstadoHitoVM { Id = 3, Nombre = "En Progreso" },
                new EstadoHitoVM { Id = 4, Nombre = "Aprobado" },
                new EstadoHitoVM { Id = 5, Nombre = "Rechazado" }
            };

            ViewBag.Usuarios = new SelectList(empleados, "IdUsuario", "Correo");
            ViewBag.Clientes = clientes;
            ViewBag.ProyectoId = id;
            ViewBag.EstadosHito = new SelectList(estadosHitos, "Id", "Nombre");
            ViewBag.EstadosHitoLista = estadosHitos;

            return View(proyecto);
        }

        //Dasboard
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Dashboard(int id)
        {
            var proyecto = await _dbContext.Proyectos
                .Include(p => p.Fases)
                .ThenInclude(f => f.Tareas)
                .Include(p => p.Hitos)
                .ThenInclude(h => h.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proyecto == null) return NotFound();

            // Un cliente logueado (rol "Usuario") solo puede ver el dashboard de su propio proyecto
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Usuario"))
            {
                var idUsuarioActual = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                if (proyecto.IdUsuario != idUsuarioActual)
                {
                    return RedirectToAction("Proyectos");
                }
            }

            var usuarios = await _dbContext.Usuarios
                .Where(u => u.Rol.Nombre == "Empleado")
                .ToListAsync();

            var estadosHitos = new List<EstadoHitoVM>
            {
                new EstadoHitoVM { Id = 1, Nombre = "Completo" },
                new EstadoHitoVM { Id = 2, Nombre = "Pendiente" },
                new EstadoHitoVM { Id = 3, Nombre = "En Progreso" }
            };

            var hitoData = proyecto.Hitos
                .GroupBy(h => h.estado)
                .Select(g => new
                {
                    Estado = g.Key,
                    Nombre = estadosHitos.FirstOrDefault(e => e.Id == g.Key)?.Nombre,
                    Count = g.Count(),
                    CountP = g.Count() * 10
                })
                .ToList();

            var tareaData = proyecto.Fases
                .SelectMany(f => f.Tareas)
                .GroupBy(t => t.Completada ? "Completada" : (t.EnProgreso ? "En Progreso" : "Pendiente"))
                .Select(g => new
                {
                    Nombre = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var faseData = proyecto.Fases
                .Select(f => new
                {
                    f.Nombre,
                    PorcentajeCompletadas = f.Tareas.Count == 0
                        ? 0
                        : (f.Tareas.Count(t => t.Completada) * 100 / f.Tareas.Count()),
                    CostoTotal = f.CostoTotal,
                    FechaInicio = f.Tareas.Any() ? f.Tareas.Min(t => t.FechaInicio) : (DateTime?)null,
                    FechaFin = f.Tareas.Any() ? f.Tareas.Max(t => t.FechaFin) : (DateTime?)null
                })
                .ToList();

            // Flujo de caja proyectado: presupuesto (todas las tareas) vs. gasto real (tareas completadas),
            // agrupado por el mes en que cada tarea inicia (Tarea no tiene fecha de finalización real).
            var todasTareas = proyecto.Fases.SelectMany(f => f.Tareas).ToList();
            var presupuestoPorMes = todasTareas
                .GroupBy(t => new { t.FechaInicio.Year, t.FechaInicio.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Monto = g.Sum(t => t.Costo ?? 0) })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToList();
            var gastoRealPorMes = todasTareas
                .Where(t => t.Completada)
                .GroupBy(t => new { t.FechaInicio.Year, t.FechaInicio.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Monto = g.Sum(t => t.Costo ?? 0) })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToList();

            // Obtener el costo total del proyecto
            var costoTotal = proyecto.CostoTotal;
            // Calcular el progreso total del proyecto

            var totalTareas = proyecto.Fases.SelectMany(f => f.Tareas).Count();
            var tareasCompletadas = proyecto.Fases.SelectMany(f => f.Tareas).Count(t => t.Completada);
            var progresoGeneral = totalTareas == 0 ? 0 : (tareasCompletadas * 100 / totalTareas);

            ViewBag.Usuarios = new SelectList(usuarios, "IdUsuario", "Correo");
            ViewBag.ProyectoId = id;
            ViewBag.EstadosHito = new SelectList(estadosHitos, "Id", "Nombre");
            ViewBag.HitoData = hitoData;
            ViewBag.TareaData = tareaData;
            ViewBag.FaseData = faseData;
            ViewBag.HitoDataJson = JsonConvert.SerializeObject(hitoData);
            ViewBag.TareaDataJson = JsonConvert.SerializeObject(tareaData);
            ViewBag.FaseDataJson = JsonConvert.SerializeObject(faseData);
            ViewBag.PresupuestoPorMesJson = JsonConvert.SerializeObject(presupuestoPorMes);
            ViewBag.GastoRealPorMesJson = JsonConvert.SerializeObject(gastoRealPorMes);
            ViewBag.CostoTotal = costoTotal;
            ViewBag.ProgresoGeneral = progresoGeneral;

            return View(proyecto);
        }

        // Exportar un resumen ejecutivo del dashboard del proyecto en PDF (iText7, sin dependencias nativas)
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ExportarDashboardPDF(int id)
        {
            var proyecto = await _dbContext.Proyectos
                .Include(p => p.Fases)
                .ThenInclude(f => f.Tareas)
                .Include(p => p.Hitos)
                .ThenInclude(h => h.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proyecto == null) return NotFound();

            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Usuario"))
            {
                var idUsuarioActual = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                if (proyecto.IdUsuario != idUsuarioActual)
                {
                    return RedirectToAction("Proyectos");
                }
            }

            var totalTareas = proyecto.Fases.SelectMany(f => f.Tareas).Count();
            var tareasCompletadas = proyecto.Fases.SelectMany(f => f.Tareas).Count(t => t.Completada);
            var progresoGeneral = totalTareas == 0 ? 0 : (tareasCompletadas * 100 / totalTareas);
            var costoTotal = proyecto.CostoTotal;

            var hoy = DateTime.Today;
            var proximosHitos = proyecto.Hitos
                .Where(h => h.estado != 1 && h.estado != 4 && h.Fecha >= hoy)
                .OrderBy(h => h.Fecha)
                .Take(5)
                .ToList();

            using var stream = new MemoryStream();
            using (var writer = new PdfWriter(stream))
            {
                writer.SetCloseStream(false);
                using var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                document.Add(new Paragraph(proyecto.Nombre).SetFontSize(20).SetBold());
                document.Add(new Paragraph($"Estado: {proyecto.Estado}  ·  Creado el {proyecto.FechaCreacion:dd/MM/yyyy}")
                    .SetFontSize(11).SetFontColor(ColorConstants.GRAY));
                document.Add(new Paragraph($"Reporte generado el {DateTime.Now:dd/MM/yyyy HH:mm}")
                    .SetFontSize(9).SetFontColor(ColorConstants.GRAY).SetMarginBottom(20));

                var resumen = new Table(UnitValue.CreatePercentArray(new float[] { 1, 1, 1 })).UseAllAvailableWidth();
                resumen.AddCell(CeldaResumenPdf("Progreso General", $"{progresoGeneral}%"));
                resumen.AddCell(CeldaResumenPdf("Tareas Completadas", $"{tareasCompletadas} / {totalTareas}"));
                resumen.AddCell(CeldaResumenPdf("Costo Total", $"${costoTotal:N2}"));
                document.Add(resumen);

                document.Add(new Paragraph("Fases").SetFontSize(14).SetBold().SetMarginTop(20));
                if (!proyecto.Fases.Any())
                {
                    document.Add(new Paragraph("Este proyecto no tiene fases todavía.").SetFontColor(ColorConstants.GRAY));
                }
                else
                {
                    var fasesTable = new Table(UnitValue.CreatePercentArray(new float[] { 3, 1, 1, 1 })).UseAllAvailableWidth();
                    fasesTable.AddHeaderCell(CeldaEncabezadoPdf("Fase"));
                    fasesTable.AddHeaderCell(CeldaEncabezadoPdf("Tareas"));
                    fasesTable.AddHeaderCell(CeldaEncabezadoPdf("Completadas"));
                    fasesTable.AddHeaderCell(CeldaEncabezadoPdf("Costo"));
                    foreach (var fase in proyecto.Fases)
                    {
                        fasesTable.AddCell(new Cell().Add(new Paragraph(fase.Nombre)));
                        fasesTable.AddCell(new Cell().Add(new Paragraph(fase.Tareas.Count.ToString())));
                        fasesTable.AddCell(new Cell().Add(new Paragraph(fase.Tareas.Count(t => t.Completada).ToString())));
                        fasesTable.AddCell(new Cell().Add(new Paragraph($"${fase.CostoTotal:N2}")));
                    }
                    document.Add(fasesTable);
                }

                document.Add(new Paragraph("Próximos Hitos").SetFontSize(14).SetBold().SetMarginTop(20));
                if (!proximosHitos.Any())
                {
                    document.Add(new Paragraph("No hay hitos próximos pendientes.").SetFontColor(ColorConstants.GRAY));
                }
                else
                {
                    foreach (var hito in proximosHitos)
                    {
                        var responsable = hito.Usuario != null ? $"{hito.Usuario.Nombre} {hito.Usuario.Apellido}" : "Sin asignar";
                        document.Add(new Paragraph($"• {hito.Descripcion} — {hito.Fecha:dd/MM/yyyy} ({responsable})").SetFontSize(11));
                    }
                }

                document.Close();
            }

            var nombreArchivo = $"Dashboard_{proyecto.Nombre.Replace(" ", "_")}.pdf";
            return File(stream.ToArray(), "application/pdf", nombreArchivo);
        }

        private static Cell CeldaResumenPdf(string titulo, string valor)
        {
            var cell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(6);
            cell.Add(new Paragraph(titulo).SetFontSize(9).SetFontColor(ColorConstants.GRAY).SetMarginBottom(2));
            cell.Add(new Paragraph(valor).SetFontSize(14).SetBold());
            return cell;
        }

        private static Cell CeldaEncabezadoPdf(string texto)
        {
            return new Cell()
                .Add(new Paragraph(texto).SetFontSize(10).SetBold())
                .SetBackgroundColor(new DeviceRgb(248, 250, 252));
        }

        // Dashboard agregado de todos los proyectos
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> DashboardGeneral()
        {
            var proyectos = await _dbContext.Proyectos
                .Include(p => p.Fases)
                .ThenInclude(f => f.Tareas)
                .Include(p => p.Hitos)
                .ThenInclude(h => h.Usuario)
                .Include(p => p.Usuario)
                .ToListAsync();

            var totalInvertido = proyectos.Sum(p => p.CostoTotal);
            var totalProyectos = proyectos.Count;
            var activos = proyectos.Count(p => p.Estado == EstadoProyecto.Progreso || p.Estado == EstadoProyecto.Planificacion);
            var completados = proyectos.Count(p => p.Estado == EstadoProyecto.Completado);
            var pendientes = proyectos.Count(p => p.Estado == EstadoProyecto.Pendiente);

            var totalTareas = proyectos.SelectMany(p => p.Fases).SelectMany(f => f.Tareas).Count();
            var tareasCompletadas = proyectos.SelectMany(p => p.Fases).SelectMany(f => f.Tareas).Count(t => t.Completada);
            var progresoGeneral = totalTareas == 0 ? 0 : (tareasCompletadas * 100 / totalTareas);

            var estadoNombres = new Dictionary<EstadoProyecto, string>
            {
                { EstadoProyecto.Planificacion, "Planificación" },
                { EstadoProyecto.Progreso, "En Progreso" },
                { EstadoProyecto.Completado, "Completado" },
                { EstadoProyecto.Pendiente, "Pendiente" }
            };

            var estadoData = proyectos
                .GroupBy(p => p.Estado)
                .Select(g => new { Nombre = estadoNombres[g.Key], Count = g.Count() })
                .ToList();

            var costoPorProyecto = proyectos
                .OrderByDescending(p => p.CostoTotal)
                .Take(8)
                .Select(p => new { p.Nombre, Costo = p.CostoTotal })
                .ToList();

            var hoy = DateTime.Today;
            var proximosHitos = proyectos
                .SelectMany(p => p.Hitos.Select(h => new { Hito = h, Proyecto = p }))
                .Where(x => x.Hito.estado != 1 && x.Hito.estado != 4 && x.Hito.Fecha >= hoy)
                .OrderBy(x => x.Hito.Fecha)
                .Take(8)
                .Select(x => new HitoResumenVM
                {
                    Descripcion = x.Hito.Descripcion,
                    Proyecto = x.Proyecto.Nombre,
                    ProyectoId = x.Proyecto.Id,
                    Fecha = x.Hito.Fecha,
                    DiasRestantes = (x.Hito.Fecha.Date - hoy).Days,
                    Responsable = x.Hito.Usuario != null ? $"{x.Hito.Usuario.Nombre} {x.Hito.Usuario.Apellido}" : "Sin asignar"
                })
                .ToList();

            ViewBag.TotalInvertido = totalInvertido;
            ViewBag.TotalProyectos = totalProyectos;
            ViewBag.Activos = activos;
            ViewBag.Completados = completados;
            ViewBag.Pendientes = pendientes;
            ViewBag.TotalTareas = totalTareas;
            ViewBag.TareasCompletadas = tareasCompletadas;
            ViewBag.ProgresoGeneral = progresoGeneral;
            ViewBag.EstadoDataJson = JsonConvert.SerializeObject(estadoData);
            ViewBag.CostoPorProyectoJson = JsonConvert.SerializeObject(costoPorProyecto);
            ViewBag.ProximosHitos = proximosHitos;

            return View(proyectos);
        }

        // Kanban de tareas
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Kanban(int id)
        {
            var proyecto = await _dbContext.Proyectos
                .Include(p => p.Fases)
                .ThenInclude(f => f.Tareas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proyecto == null) return NotFound();

            return View(proyecto);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, Supervisor, Empleado")]
        public async Task<IActionResult> ActualizarColumnaKanban(int tareaId, string columna)
        {
            var tarea = await _dbContext.Tareas.FindAsync(tareaId);
            if (tarea == null)
            {
                return NotFound(new { mensaje = "Tarea no encontrada." });
            }

            switch (columna)
            {
                case "pendiente":
                    tarea.Completada = false;
                    tarea.EnProgreso = false;
                    break;
                case "progreso":
                    tarea.Completada = false;
                    tarea.EnProgreso = true;
                    break;
                case "completada":
                    tarea.Completada = true;
                    tarea.EnProgreso = false;
                    break;
                default:
                    return BadRequest(new { mensaje = "Columna no reconocida." });
            }

            await _dbContext.SaveChangesAsync();
            return Ok(new { mensaje = "Tarea actualizada." });
        }

        //Clientes
        [HttpGet]
        [Authorize(Roles = "Administrador, Supervisor")]
        public async Task<IActionResult> AsignarCliente(int id)
        {
            var proyecto = await _dbContext.Proyectos.FindAsync(id);
            if (proyecto == null) return NotFound();

            var usuarios = await _dbContext.Usuarios
                .Where(u => u.Rol.Nombre == "Usuario")
                .ToListAsync();

            ViewBag.Usuarios = new SelectList(usuarios, "IdUsuario", "Correo");
            ViewBag.ProyectoId = id;

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, Supervisor")]
        public async Task<IActionResult> AsignarCliente(int id, int usuarioId)
        {
            var proyecto = await _dbContext.Proyectos.FindAsync(id);
            if (proyecto == null) return NotFound();

            // Verificar si se seleccionó un usuario
            if (usuarioId == 0)
            {
                TempData["MensajeError"] = "Debe seleccionar un usuario para asignar al proyecto.";
                ViewBag.ProyectoId = id;

                // Volver a cargar los usuarios para que se muestren en el dropdown
                var usuarios = await _dbContext.Usuarios
                    .Where(u => u.Rol.Nombre == "Usuario")
                    .ToListAsync();
                ViewBag.Usuarios = new SelectList(usuarios, "IdUsuario", "Correo");

                return View(proyecto);
            }

            proyecto.IdUsuario = usuarioId;
            await _dbContext.SaveChangesAsync();

            await _actividadService.RegistrarAsync(User, "asignó", "Cliente", $"Cliente asignado al proyecto '{proyecto.Nombre}'");

            TempData["MensajeExito"] = "Cliente asignado correctamente.";
            return RedirectToAction("GestionarProyecto", new { id });
        }

        //Fases
        [HttpPost]
        [Authorize(Roles = "Administrador, Supervisor")]
        public async Task<IActionResult> AgregarFase(int proyectoId, string Nombre)
        {
            if (string.IsNullOrWhiteSpace(Nombre))
            {
                TempData["MensajeError"] = "El nombre de la fase no puede estar vacío.";
                return RedirectToAction("GestionarProyecto", new { id = proyectoId });
            }

            // Buscar el proyecto correspondiente
            var proyecto = _dbContext.Proyectos.Include(p => p.Fases).FirstOrDefault(p => p.Id == proyectoId);
            if (proyecto == null)
            {
                TempData["MensajeError"] = "Proyecto no encontrado.";
                return RedirectToAction("GestionarProyecto", new { id = proyectoId });
            }

            // Verificar si el nombre de la fase ya existe
            if (proyecto.Fases.Any(f => f.Nombre == Nombre))
            {
                TempData["MensajeError"] = "Ya existe una fase con este nombre en el proyecto.";
                return RedirectToAction("GestionarProyecto", new { id = proyectoId });
            }

            var nuevaFase = new Fase
            {
                Nombre = Nombre,
                ProyectoId = proyectoId,
                Tareas = new List<Tarea>()
            };

            // Guardar en la base de datos
            _dbContext.Fases.Add(nuevaFase);
            _dbContext.SaveChanges();

            await _actividadService.RegistrarAsync(User, "agregó", "Fase", $"Fase '{Nombre}' al proyecto '{proyecto.Nombre}'");

            TempData["MensajeExito"] = "Fase agregada correctamente.";
            return RedirectToAction("GestionarProyecto", new { id = proyectoId });
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, Supervisor")]
        public async Task<IActionResult> EliminarFase(int faseId)
        {
            var fase = await _dbContext.Fases
                .Include(f => f.Tareas)
                .FirstOrDefaultAsync(f => f.Id == faseId);

            if (fase == null)
            {
                TempData["MensajeError"] = "La fase no fue encontrada.";
                return RedirectToAction("Proyectos");
            }

            try
            {
                // Eliminar todas las tareas asociadas a la fase
                _dbContext.Tareas.RemoveRange(fase.Tareas);

                // Eliminar la fase
                _dbContext.Fases.Remove(fase);

                await _dbContext.SaveChangesAsync();
                await _actividadService.RegistrarAsync(User, "eliminó", "Fase", $"Fase '{fase.Nombre}'");
                TempData["MensajeExito"] = "Fase eliminada correctamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = $"Error al eliminar la fase: {ex.Message}";
            }

            return RedirectToAction("GestionarProyecto", new { id = fase.ProyectoId });
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ObtenerFase(int faseId)
        {
            var fase = await _dbContext.Fases.FirstOrDefaultAsync(f => f.Id == faseId);
            if (fase == null)
            {
                return NotFound(new { mensaje = "Fase no encontrada." });
            }
            return Json(new { id = fase.Id, nombre = fase.Nombre });
        }

        //Tareas
        [HttpPost]
        [Authorize(Roles = "Administrador, Supervisor")]
        public async Task<IActionResult> AgregarTareasModal(int faseId, List<Tarea> tareas)
        {
            var fase = await _dbContext.Fases.Include(f => f.Tareas).FirstOrDefaultAsync(f => f.Id == faseId);
            if (fase == null)
            {
                return Json(new { exito = false, mensaje = "Fase no encontrada." });
            }

            foreach (var tarea in tareas)
            {
                if (string.IsNullOrEmpty(tarea.Nombre))
                {
                    return Json(new { exito = false, mensaje = "El nombre de cada tarea no puede estar vacío." });
                }

                if (tarea.FechaInicio >= tarea.FechaFin)
                {
                    return Json(new { exito = false, mensaje = "La fecha de inicio debe ser anterior a la fecha de finalización para cada tarea." });
                }

                tarea.FaseId = faseId;
                _dbContext.Tareas.Add(tarea);
            }

            await _dbContext.SaveChangesAsync();
            return Json(new { exito = true, mensaje = "Tarea agregada correctamente." });
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, Supervisor")]
        public IActionResult EliminarTarea(int tareaId)
        {
            try
            {
                var tarea = _dbContext.Tareas
                    .Include(t => t.Fase)
                    .FirstOrDefault(t => t.Id == tareaId);

                if (tarea == null)
                {
                    TempData["MensajeError"] = "No se encontró la tarea a eliminar.";
                    return RedirectToAction("GestionarProyecto");
                }

                if (tarea.Fase == null)
                {
                    TempData["MensajeError"] = "La tarea no está asociada a una fase válida.";
                    return RedirectToAction("GestionarProyecto");
                }

                int proyectoId = tarea.Fase.ProyectoId;

                // Eliminar la tarea
                _dbContext.Tareas.Remove(tarea);
                _dbContext.SaveChanges();

                TempData["MensajeExito"] = $"La tarea '{tarea.Nombre}' se eliminó correctamente.";

                // Redirigir a la vista de gestión del proyecto
                return RedirectToAction("GestionarProyecto", new { id = proyectoId });
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Ocurrió un error al intentar eliminar la tarea.";
                Console.WriteLine(ex.Message);
                return RedirectToAction("GestionarProyecto");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, Supervisor")]
        public IActionResult ActualizarTareas([FromBody] Dictionary<int, bool> tareasCompletadas)
        {
            try
            {
                foreach (var tareaId in tareasCompletadas.Keys)
                {
                    var tarea = _dbContext.Tareas.Find(tareaId);
                    if (tarea != null)
                    {
                        tarea.Completada = tareasCompletadas[tareaId];
                    }
                }
                _dbContext.SaveChanges();
                return Ok(new { message = "Cambios guardados exitosamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Error al guardar cambios: {ex.Message}" });
            }
        }

        //Hitos
        [HttpGet]
        [Authorize(Roles = "Administrador, Supervisor")]
        public async Task<IActionResult> AsignarHito(int id)
        {
            var proyecto = await _dbContext.Proyectos.FindAsync(id);
            if (proyecto == null) return NotFound();

            var usuarios = await _dbContext.Usuarios
                .Where(u => u.Rol.Nombre == "Empleado")
                .ToListAsync();
            var estadosHitos = new List<EstadoHitoVM>
            {
                new EstadoHitoVM { Id = 1, Nombre = "Completo" },
                new EstadoHitoVM { Id = 2, Nombre = "Pendiente" },
                new EstadoHitoVM { Id = 3, Nombre = "En Progreso" } };

            ViewBag.Usuarios = new SelectList(usuarios, "IdUsuario", "Correo");
            ViewBag.ProyectoId = id;
            ViewBag.EstadosHito = new SelectList(estadosHitos, "Id", "Nombre");

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, Supervisor")]
        public async Task<IActionResult> AsignarHito(int id, int usuarioId, int estadoId, string Descripcion, DateTime Fecha)
        {
            var proyecto = await _dbContext.Proyectos.FindAsync(id);
            if (proyecto == null) return NotFound();

            // Verificar si se seleccionó un usuario
            if (usuarioId == 0)
            {
                TempData["MensajeError"] = "Debe seleccionar un empleado para asignar al proyecto.";
                ViewBag.ProyectoId = id;

                // Volver a cargar los usuarios para que se muestren en el dropdown
                var usuarios = await _dbContext.Usuarios
                    .Where(u => u.Rol.Nombre == "Usuario")
                    .ToListAsync();
                ViewBag.Usuarios = new SelectList(usuarios, "IdUsuario", "Correo");

                return View(proyecto);
            }

            var hito = new Hito();
            hito.Fecha = Fecha;
            hito.IdUsuario = usuarioId;
            hito.Descripcion = Descripcion;
            hito.ProyectoId = id;
            hito.estado = estadoId;

            _dbContext.Hitos.Add(hito);
            _dbContext.SaveChanges();

            await _actividadService.RegistrarAsync(User, "creó", "Hito", $"Hito '{Descripcion}' en el proyecto '{proyecto.Nombre}'");

            TempData["MensajeExito"] = "Hito asignado correctamente.";
            return RedirectToAction("Proyectos");
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, Supervisor")]
        public async Task<IActionResult> EliminarHito(int hitoId)
        {
            var hito = await _dbContext.Hitos
                .FirstOrDefaultAsync(h => h.ID == hitoId);

            if (hito == null)
            {
                TempData["MensajeError"] = "El Hito no fue encontrado.";
                return RedirectToAction("Proyectos");
            }

            try
            {
                _dbContext.Hitos.Remove(hito);

                await _dbContext.SaveChangesAsync();
                await _actividadService.RegistrarAsync(User, "eliminó", "Hito", $"Hito '{hito.Descripcion}'");
                TempData["MensajeExito"] = "Hito eliminado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = $"Error al eliminar el hito: {ex.Message}";
            }

            return RedirectToAction("GestionarProyecto", new { id = hito.ProyectoId });
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, Supervisor")]
        public IActionResult AprobarHito(int hitoId)
        {
            var hito = _dbContext.Hitos.Find(hitoId);
            if (hito != null)
            {
                hito.estado = 4; // Estado "Aprobado"
                _dbContext.SaveChanges();
            }
            return RedirectToAction("GestionarProyecto", new { id = hito.ProyectoId });
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, Supervisor")]
        public IActionResult RechazarHito(int hitoId)
        {
            var hito = _dbContext.Hitos.Find(hitoId);
            if (hito != null)
            {
                hito.estado = 5; // Estado "Rechazado"
                _dbContext.SaveChanges();
            }
            return RedirectToAction("GestionarProyecto", new { id = hito.ProyectoId });
        }

        // Edición completa del hito (descripción, responsable, estado, fecha) — solo gestores.
        // Empleado usa la acción ActualizarEstadoHito, más abajo, que solo cambia el estado.
        [HttpPost]
        [Authorize(Roles = "Administrador, Supervisor")]
        public async Task<IActionResult> EditarHito(int hitoId, int usuarioId, int estadoId, string Descripcion, DateTime Fecha)
        {
            var hito = await _dbContext.Hitos.FindAsync(hitoId);
            if (hito == null)
            {
                TempData["MensajeError"] = "Hito no encontrado.";
                return RedirectToAction("Proyectos");
            }

            if (usuarioId == 0)
            {
                TempData["MensajeError"] = "Debe seleccionar un responsable para el hito.";
                return RedirectToAction("GestionarProyecto", new { id = hito.ProyectoId });
            }

            hito.Descripcion = Descripcion;
            hito.IdUsuario = usuarioId;
            hito.estado = estadoId;
            hito.Fecha = Fecha;
            await _dbContext.SaveChangesAsync();

            await _actividadService.RegistrarAsync(User, "editó", "Hito", $"Hito '{hito.Descripcion}'");

            TempData["MensajeExito"] = "Hito actualizado correctamente.";
            return RedirectToAction("GestionarProyecto", new { id = hito.ProyectoId });
        }

        // Cambio rápido de estado del hito (sin tocar responsable/fecha/descripción) — lo usa Empleado.
        [HttpPost]
        [Authorize(Roles = "Administrador, Supervisor, Empleado")]
        public async Task<IActionResult> ActualizarEstadoHito(int hitoId, int estadoId)
        {
            var hito = await _dbContext.Hitos.FindAsync(hitoId);
            if (hito == null)
            {
                TempData["MensajeError"] = "Hito no encontrado.";
                return RedirectToAction("Proyectos");
            }

            hito.estado = estadoId;
            await _dbContext.SaveChangesAsync();

            await _actividadService.RegistrarAsync(User, "actualizó el estado de", "Hito", $"Hito '{hito.Descripcion}'");

            TempData["MensajeExito"] = "Estado del hito actualizado correctamente.";
            return RedirectToAction("GestionarProyecto", new { id = hito.ProyectoId });
        }

        // Comentarios en tareas e hitos
        [HttpGet]
        public async Task<IActionResult> ListarComentarios(string entidadTipo, int entidadId)
        {
            if (!EntidadesComentario.Contains(entidadTipo ?? ""))
            {
                return BadRequest();
            }

            var comentarios = await _comentarioService.ListarAsync(entidadTipo, entidadId);
            var usuarioActualId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var esAdministrador = User.IsInRole("Administrador");

            return Json(comentarios.Select(c => new
            {
                c.Id,
                c.UsuarioNombre,
                c.Texto,
                Fecha = c.Fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                puedeEliminar = esAdministrador || c.UsuarioId == usuarioActualId
            }));
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, Supervisor, Empleado")]
        public async Task<IActionResult> AgregarComentario(string entidadTipo, int entidadId, string texto)
        {
            if (!EntidadesComentario.Contains(entidadTipo ?? ""))
            {
                return Json(new { exito = false, mensaje = "Tipo de entidad no válido." });
            }

            if (string.IsNullOrWhiteSpace(texto))
            {
                return Json(new { exito = false, mensaje = "El comentario no puede estar vacío." });
            }

            var comentario = await _comentarioService.AgregarAsync(User, entidadTipo, entidadId, texto.Trim());

            return Json(new
            {
                exito = true,
                comentario = new
                {
                    comentario.Id,
                    comentario.UsuarioNombre,
                    comentario.Texto,
                    Fecha = comentario.Fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    puedeEliminar = true
                }
            });
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, Supervisor, Empleado")]
        public async Task<IActionResult> EliminarComentario(int comentarioId)
        {
            var exito = await _comentarioService.EliminarAsync(comentarioId, User);
            if (!exito)
            {
                return Json(new { exito = false, mensaje = "No se pudo eliminar el comentario." });
            }

            return Json(new { exito = true });
        }

        //Guardar Cambios
        [HttpPost]
        [Authorize(Roles = "Administrador, Supervisor")]
        public async Task<IActionResult> GuardarCambios(int proyectoId, EstadoProyecto Estado, List<int> tareasCompletadas)
        {
            var proyecto = await _dbContext.Proyectos
                .Include(p => p.Fases)
                .ThenInclude(f => f.Tareas)
                .FirstOrDefaultAsync(p => p.Id == proyectoId);

            if (proyecto == null)
            {
                TempData["MensajeError"] = "Proyecto no encontrado.";
                return RedirectToAction("Proyectos");
            }

            try
            {
                proyecto.Estado = Estado;

                foreach (var fase in proyecto.Fases)
                {
                    foreach (var tarea in fase.Tareas)
                    {
                        tarea.Completada = tareasCompletadas.Contains(tarea.Id);
                    }
                }

                await _dbContext.SaveChangesAsync();
                TempData["MensajeExito"] = "Cambios guardados exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = $"Error al guardar los cambios: {ex.Message}";
            }

            return RedirectToAction("GestionarProyecto", new { id = proyectoId });
        }
    }
}
