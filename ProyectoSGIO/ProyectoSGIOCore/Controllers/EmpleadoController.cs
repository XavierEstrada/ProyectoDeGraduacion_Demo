using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoSGIOCore.Data;
using ProyectoSGIOCore.Models;
using ProyectoSGIOCore.ViewModels;
using System.Security.Claims;

namespace ProyectoSGIOCore.Controllers
{
    [Authorize(Roles = "Administrador, Supervisor")]
    public class EmpleadoController : Controller
    {
        private readonly AppDBContext _dbContext;

        public EmpleadoController(AppDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> VisualizarEmpleados()
        {
            var empleados = await _dbContext.Empleados.ToListAsync();
            return View(empleados);
        }

        [HttpGet]
        public IActionResult CrearEmpleado()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CrearEmpleado(Empleado modelo)
        {
            if (await _dbContext.Empleados.AnyAsync(e => e.Correo == modelo.Correo))
            {
                TempData["MensajeError"] = "El correo electrónico ya está en uso";
                return View(modelo);
            }

            var empleado = new Empleado
            {
                Nombre = modelo.Nombre,
                Apellido = modelo.Apellido,
                Correo = modelo.Correo,
            };

            await _dbContext.Empleados.AddAsync(empleado);
            await _dbContext.SaveChangesAsync();

            TempData["MensajeExito"] = $"El empleado '{empleado.Nombre + " " + empleado.Apellido}' se ha registrado correctamente";
            return RedirectToAction("VisualizarEmpleados");
        }

        [HttpGet]
        public async Task<IActionResult> EditarEmpleado(int id)
        {
            var empleado = await _dbContext.Empleados.FindAsync(id);
            if (empleado == null)
            {
                TempData["MensajeError"] = "Empleado no encontrado.";
                return RedirectToAction("VisualizarEmpleados");
            }
            return View(empleado);
        }

        [HttpPost]
        public async Task<IActionResult> EditarEmpleado(Empleado entidad)
        {
            var empleado = await _dbContext.Empleados.FindAsync(entidad.IdEmpleado);
            if (empleado == null)
            {
                TempData["MensajeError"] = "Empleado no encontrado.";
                return RedirectToAction("VisualizarEmpleados");
            }

            empleado.Nombre = entidad.Nombre;
            empleado.Apellido = entidad.Apellido;
            empleado.Correo = entidad.Correo;
            await _dbContext.SaveChangesAsync();

            TempData["MensajeExito"] = $"Empleado: '{empleado.Nombre + " " + empleado.Apellido}' editado correctamente";
            return RedirectToAction("VisualizarEmpleados");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarEmpleadoConfirmado(int id)
        {
            var empleado = await _dbContext.Empleados.FindAsync(id);
            if (empleado == null)
            {
                TempData["MensajeError"] = "Empleado no encontrado.";
                return RedirectToAction("VisualizarEmpleados");
            }

            _dbContext.Empleados.Remove(empleado);
            await _dbContext.SaveChangesAsync();
            TempData["MensajeExito"] = $"Empleado: '{empleado.Nombre + " " + empleado.Apellido}' eliminado exitosamente";
            return RedirectToAction("VisualizarEmpleados");
        }
    }
}