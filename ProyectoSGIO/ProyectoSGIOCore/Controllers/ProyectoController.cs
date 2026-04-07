using Microsoft.AspNetCore.Mvc;
using ProyectoSGIOCore.Data;
using Microsoft.EntityFrameworkCore;
using ProyectoSGIOCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProyectoSGIOCore.ViewModels;
using Newtonsoft.Json;

namespace ProyectoSGIOCore.Controllers
{
    [Authorize(Roles = "Administrador, Supervisor, Usuario")]
    public class ProyectoController : Controller
    {
        private readonly AppDBContext _dbContext;

        public ProyectoController(AppDBContext context)
        {
            _dbContext = context;
        }

        //Proyectos
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Proyectos()
        {
            var proyectos = await _dbContext.Proyectos
                .Include(p => p.Fases)
                .ThenInclude(f => f.Tareas)
                .Include(p => p.Usuario)
                .ToListAsync();

            return View(proyectos);
        }

        [HttpGet]
        public IActionResult CrearProyecto()
        {
            return View();
        }

        [HttpPost]
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

            TempData["MensajeExito"] = $"Proyecto '{proyecto.Nombre}' editado correctamente.";
            return RedirectToAction("Proyectos");
        }

        [HttpPost]
        public IActionResult EliminarProyecto(int proyectoId)
        {
            try
            {
                var proyecto = _dbContext.Proyectos
                    .Include(p => p.Fases)
                    .ThenInclude(f => f.Tareas)
                    .FirstOrDefault(p => p.Id == proyectoId);

                if (proyecto != null)
                {
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
                .Include(h => h.Hitos)
                .ThenInclude(u => u.Usuario)

                .FirstOrDefaultAsync(p => p.Id == id);
            var usuarios = await _dbContext.Usuarios
                .Where(u => u.Rol.Nombre == "Empleado")
                .ToListAsync();


            var estadosHitos = new List<EstadoHitoVM>
            {
                new EstadoHitoVM { Id = 1, Nombre = "Completo" },
                new EstadoHitoVM { Id = 2, Nombre = "Pendiente" },
                new EstadoHitoVM { Id = 3, Nombre = "En Progreso" },
                new EstadoHitoVM { Id = 4, Nombre = "Aprobado" },
                new EstadoHitoVM { Id = 5, Nombre = "Rechazado" }};

            ViewBag.Usuarios = new SelectList(usuarios, "IdUsuario", "Correo");
            ViewBag.ProyectoId = id;
            ViewBag.EstadosHito = new SelectList(estadosHitos, "Id", "Nombre");


            if (proyecto == null)
            {
                TempData["MensajeError"] = "Proyecto no encontrado.";
                return RedirectToAction("Proyectos");
            }

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
                .GroupBy(t => t.Completada)
                .Select(g => new
                {
                    Completada = g.Key,
                    Nombre = g.Key ? "Completadas" : "No Completadas",
                    Count = g.Count(),
                    CountP = g.Count() * 10
                })
                .ToList();

            var faseData = proyecto.Fases
                .Select(f => new
                {
                    f.Nombre,
                    PorcentajeCompletadas = f.Tareas.Count == 0
                        ? 0
                        : (f.Tareas.Count(t => t.Completada) * 100 / f.Tareas.Count())
                })
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
            ViewBag.CostoTotal = costoTotal;
            ViewBag.ProgresoGeneral = progresoGeneral; // Pasar el progreso general a la vista

            return View();
        }

        //Clientes
        [HttpGet]
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

            TempData["MensajeExito"] = "Cliente asignado correctamente.";
            return RedirectToAction("Proyectos");
        }

        //Fases
        [HttpPost]
        public IActionResult AgregarFase(int proyectoId, string Nombre)
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

            TempData["MensajeExito"] = "Fase agregada correctamente.";
            return RedirectToAction("GestionarProyecto", new { id = proyectoId });
        }

        [HttpPost]
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

            TempData["MensajeExito"] = "Hito asignado correctamente.";
            return RedirectToAction("Proyectos");
        }

        [HttpPost]
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
                TempData["MensajeExito"] = "Hito eliminado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = $"Error al eliminar el hito: {ex.Message}";
            }

            return RedirectToAction("GestionarProyecto", new { id = hito.ProyectoId });
        }

        [HttpPost]
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

        //Guardar Cambios
        [HttpPost]
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
