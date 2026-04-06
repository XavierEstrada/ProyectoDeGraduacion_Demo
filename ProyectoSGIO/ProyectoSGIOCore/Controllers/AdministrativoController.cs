using Microsoft.AspNetCore.Mvc;
using ProyectoSGIOCore.Data;
using ProyectoSGIOCore.Models;
using Microsoft.EntityFrameworkCore;
using ProyectoSGIOCore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ProyectoSGIOCore.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdministrativoController : Controller
    {
        private readonly AppDBContext _dbContext;

        public AdministrativoController(AppDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> VisualizarUsuarios()
        {
            // Obtener los usuarios de la base de datos, incluyendo los roles
            var usuarios = await _dbContext.Usuarios
                                           .Include(u => u.Rol)
                                           .ToListAsync();
            return View(usuarios);
        }

        [HttpGet]
        public IActionResult CrearUsuario()
        {

            var roles = _dbContext.Roles.ToList();

            // Validación roles
            if (roles == null || !roles.Any())
            {
                ViewData["Mensaje"] = "No hay roles disponibles. Por favor, asegúrese de que los roles estén configurados.";
                return View();
            }

            ViewBag.Roles = roles;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CrearUsuario(CrearUsuarioVM modelo)
        {
            // Cargar los roles
            var roles = await _dbContext.Roles.ToListAsync();
            ViewBag.Roles = roles;

            // Verifica si el correo ya está en uso
            var usuarioExistente = await _dbContext.Usuarios
                                                   .FirstOrDefaultAsync(u => u.Correo == modelo.Correo);

            if (usuarioExistente != null)
            {
                ViewData["Mensaje"] = "El correo electrónico ya está en uso.";
                return View(modelo);
            }

            // Verifica si las contraseñas coinciden
            if (modelo.Clave != modelo.ConfirmarClave)
            {
                ViewData["Mensaje"] = "Las contraseñas no coinciden";
                return View(modelo);
            }

            // Validar la contraseña con una expresión regular
            var regex = new System.Text.RegularExpressions.Regex(@"^(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{6,}$");
            if (!regex.IsMatch(modelo.Clave))
            {
                ViewData["Mensaje"] = "La contraseña debe tener al menos 6 caracteres, una letra minúscula, un número y un carácter especial.";
                return View(modelo);
            }

            // Buscar el rol seleccionado
            var rolDefault = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Nombre == modelo.RolSeleccionado);

            if (rolDefault == null)
            {
                ViewData["Mensaje"] = "Debe seleccionar un Rol";
                return View(modelo);
            }

            // Crea el usuario
            Usuario usuario = new Usuario()
            {
                Nombre = modelo.Nombre,
                Apellido = modelo.Apellido,
                Correo = modelo.Correo,
                Clave = modelo.Clave,
                IdRol = rolDefault.IdRol,
                Activo = true,
                intentos = 0,
            };

            // Guarda el usuario en la base de datos
            await _dbContext.Usuarios.AddAsync(usuario);
            await _dbContext.SaveChangesAsync();

            // Redirige si se creó correctamente
            if (usuario.IdUsuario != 0)
            {
                TempData["MensajeExito"] = "Usuario creado correctamente";
                return RedirectToAction("CrearUsuario", "Administrativo");
            }
            else
            {
                ViewData["Mensaje"] = "No se pudo crear el usuario";
            }
            return View(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> CambiarRol(int id)
        {
            var usuario = await _dbContext.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.IdUsuario == id);
            if (usuario == null)
            {
                return NotFound();
            }

            // Cargar todos los roles
            var roles = await _dbContext.Roles.ToListAsync();
            ViewBag.Roles = roles;

            return View(usuario);
        }

        [HttpPost]
        public async Task<IActionResult> CambiarRol(int id, int nuevoRolId)
        {
            var usuario = await _dbContext.Usuarios.FindAsync(id);
            var rolNuevo = await _dbContext.Roles.FindAsync(nuevoRolId);

            // Verificar si el usuario existe y si el rol existe
            if (usuario == null || rolNuevo == null)
            {
                return NotFound();
            }

            // Obtener el ID del usuario logueado
            var loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Verificar si el administrador intenta cambiar su propio rol
            if (id == int.Parse(loggedInUserId))
            {
                TempData["MensajeError"] = "No puedes cambiar tu propio rol.";
                return RedirectToAction("VisualizarUsuarios");
            }

            // Cambiar el rol del usuario
            usuario.IdRol = rolNuevo.IdRol;
            await _dbContext.SaveChangesAsync();

            TempData["MensajeExito"] = "Rol cambiado exitosamente.";
            return RedirectToAction("VisualizarUsuarios");
        }

        [HttpPost]
        public async Task<IActionResult> CambiarEstado(Usuario usuario)
        {
            usuario = await _dbContext.Usuarios.FindAsync(usuario.IdUsuario);
            // Verificar si el usuario existe
            if (usuario == null)
            {
                return NotFound();
            }

            // Obtener el ID del usuario logueado
            var loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Verificar si el administrador intenta cambiar su propio estado
            if (usuario.IdUsuario == int.Parse(loggedInUserId))
            {
                TempData["MensajeError"] = "No puedes cambiar tu propio estado.";
                return RedirectToAction("VisualizarUsuarios");
            }

            // Cambiar el rol del usuario
            usuario.Activo = !usuario.Activo;
            await _dbContext.SaveChangesAsync();
            TempData["MensajeExito"] = "Estado cambiado exitosamente.";
            return RedirectToAction("VisualizarUsuarios");
        }
    }


}