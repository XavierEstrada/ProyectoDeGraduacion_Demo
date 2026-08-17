using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProyectoSGIOCore.Data;
using ProyectoSGIOCore.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using ProyectoSGIOCore.Services;

namespace ProyectoSGIOCore.Controllers
{
    [Authorize(Roles = "Administrador , Supervisor")]
    public class FacturasController : Controller
    {
        private readonly AppDBContext _dbContext;
        private readonly ISupabaseStorageService _storageService;
        private readonly IActividadService _actividadService;
        private const string BucketAdjuntos = "adjuntos";
        private static readonly string[] ExtensionesPermitidas = { ".pdf", ".png", ".jpg", ".jpeg", ".webp" };
        private const long TamanoMaximoBytes = 10 * 1024 * 1024; // 10 MB

        public FacturasController(AppDBContext context, ISupabaseStorageService storageService, IActividadService actividadService)
        {
            _dbContext = context;
            _storageService = storageService;
            _actividadService = actividadService;
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

                await _actividadService.RegistrarAsync(User, "creó", "Factura", $"Factura '{factura.NumeroFactura}' de {proveedor.Nombre}");

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

            var facturaIds = facturas.Select(f => f.IdFactura).ToList();
            ViewBag.Adjuntos = _dbContext.Adjuntos
                .Where(a => a.EntidadTipo == "Factura" && facturaIds.Contains(a.EntidadId))
                .OrderByDescending(a => a.FechaSubida)
                .ToList();

            return View(facturas);
        }

        [HttpPost]
        [RequestSizeLimit(TamanoMaximoBytes)]
        public async Task<IActionResult> SubirAdjuntoFactura(int facturaId, IFormFile archivo)
        {
            var factura = await _dbContext.Facturas.FindAsync(facturaId);
            if (factura == null)
            {
                TempData["MensajeError"] = "Factura no encontrada.";
                return RedirectToAction("VisualizarFacturas");
            }

            if (archivo == null || archivo.Length == 0)
            {
                TempData["MensajeError"] = "Selecciona un archivo para subir.";
                return RedirectToAction("VisualizarFacturas");
            }

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(extension))
            {
                TempData["MensajeError"] = "Solo se permiten archivos PDF, PNG, JPG o WEBP.";
                return RedirectToAction("VisualizarFacturas");
            }

            if (archivo.Length > TamanoMaximoBytes)
            {
                TempData["MensajeError"] = "El archivo no puede superar los 10 MB.";
                return RedirectToAction("VisualizarFacturas");
            }

            using var ms = new MemoryStream();
            await archivo.CopyToAsync(ms);
            var path = $"facturas/{facturaId}/{Guid.NewGuid()}{extension}";

            try
            {
                await _storageService.SubirArchivoAsync(BucketAdjuntos, path, ms.ToArray(), archivo.ContentType);

                _dbContext.Adjuntos.Add(new Adjunto
                {
                    EntidadTipo = "Factura",
                    EntidadId = facturaId,
                    NombreArchivo = archivo.FileName,
                    RutaStorage = path,
                    UrlPublica = _storageService.ObtenerUrlPublica(BucketAdjuntos, path),
                    FechaSubida = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync();

                TempData["MensajeExito"] = "Comprobante subido correctamente.";
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error al subir el archivo. Intenta de nuevo.";
            }

            return RedirectToAction("VisualizarFacturas");
        }

        [HttpPost]
        public async Task<IActionResult> EliminarAdjuntoFactura(int id)
        {
            var adjunto = await _dbContext.Adjuntos.FindAsync(id);
            if (adjunto == null)
            {
                TempData["MensajeError"] = "El adjunto no fue encontrado.";
                return RedirectToAction("VisualizarFacturas");
            }

            try
            {
                await _storageService.EliminarArchivoAsync(BucketAdjuntos, adjunto.RutaStorage);
                _dbContext.Adjuntos.Remove(adjunto);
                await _dbContext.SaveChangesAsync();
                TempData["MensajeExito"] = "Adjunto eliminado correctamente.";
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error al eliminar el adjunto.";
            }

            return RedirectToAction("VisualizarFacturas");
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

            var proveedor = await _dbContext.Proveedores.FindAsync(entidad.IdProveedor);
            if (proveedor == null)
            {
                TempData["MensajeError"] = "El proveedor seleccionado no existe.";
                return RedirectToAction("VisualizarFacturas");
            }

            factura.IdProveedor = entidad.IdProveedor;
            factura.Proveedor = proveedor;
            factura.FechaEmision = entidad.FechaEmision;
            factura.MontoTotal = entidad.MontoTotal;
            factura.Descripcion = entidad.Descripcion;

            await _dbContext.SaveChangesAsync();
            await _actividadService.RegistrarAsync(User, "editó", "Factura", $"Factura '{factura.NumeroFactura}'");
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
            await _actividadService.RegistrarAsync(User, "eliminó", "Factura", $"Factura '{factura.NumeroFactura}'");
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
