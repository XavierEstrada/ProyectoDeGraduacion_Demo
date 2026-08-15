using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoSGIOCore.Data;
using ProyectoSGIOCore.Models;
using System.Text;

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

        [AllowAnonymous]
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

        public async Task<IActionResult> DescargarInventarioHTML()
        {
            var inventarios = await _dbContext.Inventarios.ToListAsync();

            var html = new StringBuilder();
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='UTF-8'>");
            html.AppendLine("<title>Reporte de Inventario</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine("h1 { text-align: center; color: #333; }");
            html.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
            html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: center; }");
            html.AppendLine("th { background-color: #4CAF50; color: white; }");
            html.AppendLine("tr:nth-child(even) { background-color: #f2f2f2; }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<h1>Reporte de Inventario</h1>");
            html.AppendLine("<table>");
            html.AppendLine("<thead>");
            html.AppendLine("<tr>");
            html.AppendLine("<th>ID</th>");
            html.AppendLine("<th>Nombre</th>");
            html.AppendLine("<th>Categoría</th>");
            html.AppendLine("<th>Cantidad</th>");
            html.AppendLine("<th>Precio Unitario</th>");
            html.AppendLine("<th>Precio Total</th>");
            html.AppendLine("<th>Stock</th>");
            html.AppendLine("<th>Info Adicional</th>");
            html.AppendLine("</tr>");
            html.AppendLine("</thead>");
            html.AppendLine("<tbody>");

            foreach (var producto in inventarios)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{producto.ID}</td>");
                html.AppendLine($"<td>{producto.Nombre}</td>");
                html.AppendLine($"<td>{producto.Categoria}</td>");
                html.AppendLine($"<td>{producto.Cantidad}</td>");
                html.AppendLine($"<td>{producto.PrecioUnidad:C}</td>");
                html.AppendLine($"<td>{producto.PrecioTotal:C}</td>");
                html.AppendLine($"<td>{(producto.Stock ? "En stock" : "Sin stock")}</td>");
                html.AppendLine($"<td>{producto.InformacionAdicional}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            var bytes = Encoding.UTF8.GetBytes(html.ToString());

            return File(bytes, "text/html", "Inventario.html");
        }

        private static string EscaparCampoCsv(string valor)
        {
            valor ??= string.Empty;
            if (valor.Contains(',') || valor.Contains('"') || valor.Contains('\n'))
            {
                return "\"" + valor.Replace("\"", "\"\"") + "\"";
            }
            return valor;
        }

        public async Task<IActionResult> DescargarInventarioCSV()
        {
            var inventarios = await _dbContext.Inventarios.ToListAsync();

            using (var stream = new MemoryStream())
            {
                // UTF8Encoding(true) agrega el BOM para que Excel detecte la codificación correctamente
                using (var writer = new StreamWriter(stream, new UTF8Encoding(true)))
                {
                    writer.WriteLine("ID,Nombre,Categoría,Cantidad,Precio Unitario,Precio Total,Stock,Info Adicional");

                    foreach (var producto in inventarios)
                    {
                        var campos = new[]
                        {
                            producto.ID.ToString(),
                            EscaparCampoCsv(producto.Nombre),
                            EscaparCampoCsv(producto.Categoria),
                            producto.Cantidad.ToString(),
                            producto.PrecioUnidad.ToString("0.00"),
                            producto.PrecioTotal.ToString("0.00"),
                            producto.Stock ? "En stock" : "Sin stock",
                            EscaparCampoCsv(producto.InformacionAdicional)
                        };
                        writer.WriteLine(string.Join(",", campos));
                    }

                    writer.Flush();
                    stream.Position = 0;
                }

                return File(stream.ToArray(), "text/csv", "Inventario.csv");
            }
        }
    }
}