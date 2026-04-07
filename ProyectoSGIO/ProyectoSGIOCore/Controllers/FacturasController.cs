using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProyectoSGIOCore.Data;
using ProyectoSGIOCore.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace ProyectoSGIOCore.Controllers
{
    [Authorize(Roles = "Administrador , Supervisor")]
    public class FacturasController : Controller
    {
        private readonly AppDBContext _dbContext;

        public FacturasController(AppDBContext context)
        {
            _dbContext = context;
        }

        [HttpGet]
        public IActionResult RegistroFactura()
        {
            ViewBag.Proveedores = new SelectList(_dbContext.Proveedores, "IdProveedor", "Nombre");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegistroFactura(FacturaProveedor factura)
        {
            int anioActual = DateTime.Now.Year;

            var ultimaFacturaDelAnio = await _dbContext.Facturas
                .Where(f => f.NumeroFactura.StartsWith(anioActual.ToString()))
                .OrderByDescending(f => f.IdFactura)
                .FirstOrDefaultAsync();

            int siguienteNumero = 1;

            if (ultimaFacturaDelAnio != null)
            {
                // Extraer el número consecutivo actual
                string[] partes = ultimaFacturaDelAnio.NumeroFactura.Split('-');
                if (partes.Length == 2 && int.TryParse(partes[1], out int ultimoNumero))
                {
                    siguienteNumero = ultimoNumero + 1;
                }
            }

            factura.NumeroFactura = $"{anioActual}-{siguienteNumero.ToString("D6")}";

            if (ModelState.IsValid)
            {
                var proveedor = await _dbContext.Proveedores.FindAsync(factura.IdProveedor);
                if (proveedor == null)
                {
                    ModelState.AddModelError("", "El proveedor seleccionado no existe.");
                    ViewBag.Proveedores = new SelectList(_dbContext.Proveedores, "IdProveedor", "Nombre");
                    return View(factura);
                }

                factura.Proveedor = proveedor;

                _dbContext.Facturas.Add(factura);
                await _dbContext.SaveChangesAsync();

                return RedirectToAction("VisualizarFacturas");
            }

            ViewBag.Proveedores = new SelectList(_dbContext.Proveedores, "IdProveedor", "Nombre", factura.IdProveedor);
            return View(factura);
        }


        [AllowAnonymous]
        [HttpGet]
        public IActionResult VisualizarFacturas()
        {
            var facturas = _dbContext.Facturas
                .Include(f => f.Proveedor)
                .ToList();

            // Calcular métricas personalizadas
            var totalFacturas = facturas.Sum(f => f.MontoTotal);
            var totalImpuestos = facturas.Sum(f => f.MontoTotal * 0.16m);
            var promedioFactura = facturas.Count > 0 ? facturas.Average(f => f.MontoTotal) : 0;

            // Pasar las métricas a la vista
            ViewBag.TotalFacturas = totalFacturas;
            ViewBag.TotalImpuestos = totalImpuestos;
            ViewBag.PromedioFactura = promedioFactura;

            return View(facturas);
        }

        [HttpGet]
        public async Task<IActionResult> EditarFactura(int id)
        {
            var factura = await _dbContext.Facturas.FindAsync(id);
            if (factura == null)
            {
                TempData["MensajeError"] = "Factura no encontrada.";
                return RedirectToAction("VisualizarFacturas");
            }
            ViewBag.Proveedores = new SelectList(_dbContext.Proveedores, "IdProveedor", "Nombre", factura.IdProveedor);
            return View(factura);
        }

        [HttpPost]
        public async Task<IActionResult> EditarFactura(FacturaProveedor entidad)
        {
            var factura = await _dbContext.Facturas.FindAsync(entidad.IdFactura);
            if (factura == null)
            {
                TempData["MensajeError"] = "Factura no encontrada.";
                return RedirectToAction("VisualizarFacturas");
            }

            factura.IdProveedor = entidad.IdProveedor;
            factura.FechaEmision = entidad.FechaEmision;
            factura.MontoTotal = entidad.MontoTotal;
            factura.Descripcion = entidad.Descripcion;

            await _dbContext.SaveChangesAsync();
            TempData["MensajeExito"] = $"Factura '{factura.NumeroFactura}' editada correctamente.";
            return RedirectToAction("VisualizarFacturas");
        }

        [HttpPost]
        public async Task<IActionResult> EliminarFactura(int id)
        {
            var factura = await _dbContext.Facturas.FindAsync(id);
            if (factura == null)
            {
                TempData["MensajeError"] = "Factura no encontrada.";
                return RedirectToAction("VisualizarFacturas");
            }

            _dbContext.Facturas.Remove(factura);
            await _dbContext.SaveChangesAsync();
            TempData["MensajeExito"] = $"Factura '{factura.NumeroFactura}' eliminada correctamente.";
            return RedirectToAction("VisualizarFacturas");
        }

        [HttpGet]
        public IActionResult DescargarFacturasHTML()
        {
            // Obtener la lista de facturas con su proveedor
            var facturas = _dbContext.Facturas.Include(f => f.Proveedor).ToList();

            // Crear el contenido HTML
            var html = new StringBuilder();
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='UTF-8'>");
            html.AppendLine("<title>Reporte de Facturas</title>");
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
            html.AppendLine("<h1>Reporte de Facturas</h1>");
            html.AppendLine("<table>");
            html.AppendLine("<thead>");
            html.AppendLine("<tr>");
            html.AppendLine("<th>ID</th>");
            html.AppendLine("<th>Proveedor</th>");
            html.AppendLine("<th>Número Factura</th>");
            html.AppendLine("<th>Fecha Emisión</th>");
            html.AppendLine("<th>Monto Total</th>");
            html.AppendLine("<th>Descripción</th>");
            html.AppendLine("</tr>");
            html.AppendLine("</thead>");
            html.AppendLine("<tbody>");

            // Llenar el cuerpo de la tabla con las facturas
            foreach (var factura in facturas)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{factura.IdFactura}</td>");
                html.AppendLine($"<td>{factura.Proveedor?.Nombre ?? "Sin Proveedor"}</td>");
                html.AppendLine($"<td>{factura.NumeroFactura}</td>");
                html.AppendLine($"<td>{factura.FechaEmision.ToShortDateString()}</td>");
                html.AppendLine($"<td>{factura.MontoTotal:C}</td>");
                html.AppendLine($"<td>{factura.Descripcion}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            // Convertir el contenido HTML en un array de bytes
            var bytes = Encoding.UTF8.GetBytes(html.ToString());

            // Devolver el archivo HTML para descarga
            return File(bytes, "text/html", "ReporteFacturas.html");
        }

    }
}
