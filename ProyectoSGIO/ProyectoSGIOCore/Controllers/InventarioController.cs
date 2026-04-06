using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoSGIOCore.Data;
using ProyectoSGIOCore.Models;

namespace ProyectoSGIOCore.Controllers
{
    [Authorize(Roles = "Administrador , Supervisor")]
    public class InventarioController : Controller
    {
        private readonly AppDBContext _dbContext;

        public InventarioController(AppDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> VisualizarInventario()
        {
            var inventarios = await _dbContext.Inventarios.ToListAsync();
            return View(inventarios);
        }

        [HttpGet]
        public IActionResult CrearInventario() => View();

        [HttpPost]
        public async Task<IActionResult> CrearInventario(Inventario inventario)
        {
            if (!ModelState.IsValid)
            {
                TempData["MensajeError"] = "Por favor, complete todos los campos requeridos.";
                return View(inventario);
            }

            try
            {
                inventario.PrecioTotal = inventario.Cantidad * inventario.PrecioUnidad;
                _dbContext.Inventarios.Add(inventario);
                await _dbContext.SaveChangesAsync();
                TempData["MensajeExito"] = "Producto registrado exitosamente en el inventario.";
            }
            catch (Exception ex)
            {
                TempData["MensajeError"] = "Ocurrió un error al registrar el producto. Intente nuevamente.";
            }

            return RedirectToAction("VisualizarInventario");
        }

        [HttpGet]
        public async Task<IActionResult> EditarInventario(int id)
        {
            var inventario = await _dbContext.Inventarios.FindAsync(id);
            return inventario == null ? NotFound() : View(inventario);
        }

        [HttpPost]
        public async Task<IActionResult> EditarInventario(Inventario inventario)
        {
            if (!ModelState.IsValid)
            {
                TempData["MensajeError"] = "Hubo un error al modificar el producto. Verifica los datos ingresados.";
                return View(inventario);
            }

            try
            {
                inventario.PrecioTotal = inventario.Cantidad * inventario.PrecioUnidad;
                _dbContext.Update(inventario);
                await _dbContext.SaveChangesAsync();
                TempData["MensajeExito"] = "Producto modificado exitosamente.";
            }
            catch
            {
                TempData["MensajeError"] = "Ocurrió un error al intentar modificar el producto.";
            }

            return RedirectToAction("VisualizarInventario");
        }

        [HttpPost]
        public async Task<IActionResult> EliminarInventario(int id)
        {
            var inventario = await _dbContext.Inventarios.FindAsync(id);
            if (inventario == null)
            {
                TempData["MensajeError"] = "El producto que intentas eliminar no existe.";
                return RedirectToAction("VisualizarInventario");
            }

            try
            {
                _dbContext.Inventarios.Remove(inventario);
                await _dbContext.SaveChangesAsync();
                TempData["MensajeExito"] = "Producto eliminado exitosamente.";
            }
            catch
            {
                TempData["MensajeError"] = "Ocurrió un error al intentar eliminar el producto.";
            }

            return RedirectToAction("VisualizarInventario");
        }
    }
}