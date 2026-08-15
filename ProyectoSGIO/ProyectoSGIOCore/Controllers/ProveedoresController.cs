using DinkToPdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoSGIOCore.Data;
using ProyectoSGIOCore.Models;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using OfficeOpenXml;
using System.IO;
using DinkToPdf.Contracts;
using System.Runtime.Loader;
using System.Text;

namespace ProyectoSGIOCore.Controllers
{
    [Authorize(Roles = "Administrador , Supervisor")]
    [ResponseCache(Duration = 0, NoStore = true)]
    public class ProveedoresController : Controller
    {
        private readonly AppDBContext _dbContext;

        public ProveedoresController(AppDBContext appDBContext)
        {
            _dbContext = appDBContext;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult VisualizarProveedores()
        {
            var proveedores = _dbContext.Proveedores.ToList();
            return View(proveedores);
        }

        [HttpGet]
        public IActionResult RegistroProveedor()
        {
            return View();
        }

        [HttpPost]
        public IActionResult RegistroProveedor(Proveedor proveedor)
        {
            if (ModelState.IsValid)
            {
                proveedor.Estado = true;
                _dbContext.Proveedores.Add(proveedor);
                _dbContext.SaveChanges();

                TempData["MensajeExito"] = $"El proveedor '{proveedor.Nombre}' se ha registrado correctamente.";
                return RedirectToAction("VisualizarProveedores");
            }

            TempData["MensajeError"] = "Error al registrar el proveedor. Verifique los datos ingresados.";
            return View(proveedor);
        }

        [HttpGet]
        public IActionResult EditarProveedor(int id)
        {
            var proveedor = _dbContext.Proveedores.Find(id);
            if (proveedor == null)
            {
                return NotFound();
            }
            return View(proveedor);
        }

        [HttpPost]
        public IActionResult EditarProveedor(Proveedor proveedor)
        {
            if (ModelState.IsValid)
            {
                _dbContext.Proveedores.Update(proveedor);
                _dbContext.SaveChanges();

                TempData["MensajeExito"] = $"El proveedor '{proveedor.Nombre}' ha sido actualizado correctamente.";
                return RedirectToAction("VisualizarProveedores");
            }

            TempData["MensajeError"] = "Error al actualizar el proveedor. Verifique los datos ingresados.";
            return View(proveedor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EliminarProveedorConfirmado(int id)
        {
            var proveedor = _dbContext.Proveedores.Find(id);
            if (proveedor == null)
            {
                TempData["MensajeError"] = "El proveedor no existe.";
                return RedirectToAction("VisualizarProveedores");
            }

            try
            {
                _dbContext.Proveedores.Remove(proveedor);
                _dbContext.SaveChanges();
                TempData["MensajeExito"] = $"El proveedor '{proveedor.Nombre}' fue eliminado correctamente.";
            }
            catch (Exception)
            {
                TempData["MensajeError"] = "Ocurrió un error al eliminar el proveedor.";
            }

            return RedirectToAction("VisualizarProveedores");
        }

        [HttpPost]
        public IActionResult CambiarEstado(int id)
        {
            var proveedor = _dbContext.Proveedores.Find(id);
            if (proveedor == null)
            {
                return NotFound();
            }

            // Cambiar el estado de Activo a Inactivo o viceversa
            proveedor.Estado = !proveedor.Estado;

            _dbContext.Proveedores.Update(proveedor);
            _dbContext.SaveChanges();

            return RedirectToAction("VisualizarProveedores");
        }

        public IActionResult DescargarProveedoresHTML()
        {
            var proveedores = _dbContext.Proveedores.ToList();

            var html = new StringBuilder();
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='UTF-8'>");
            html.AppendLine("<title>Reporte de Proveedores</title>");
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
            html.AppendLine("<h1>Reporte de Proveedores</h1>");
            html.AppendLine("<table>");
            html.AppendLine("<thead>");
            html.AppendLine("<tr>");
            html.AppendLine("<th>ID</th>");
            html.AppendLine("<th>Nombre</th>");
            html.AppendLine("<th>Correo</th>");
            html.AppendLine("<th>Teléfono</th>");
            html.AppendLine("<th>Dirección</th>");
            html.AppendLine("<th>Estado</th>");
            html.AppendLine("</tr>");
            html.AppendLine("</thead>");
            html.AppendLine("<tbody>");

            foreach (var proveedor in proveedores)
            {
                html.AppendLine("<tr>");
                html.AppendLine($"<td>{proveedor.IdProveedor}</td>");
                html.AppendLine($"<td>{proveedor.Nombre}</td>");
                html.AppendLine($"<td>{proveedor.Correo}</td>");
                html.AppendLine($"<td>{proveedor.Telefono}</td>");
                html.AppendLine($"<td>{proveedor.Direccion}</td>");
                html.AppendLine($"<td>{(proveedor.Estado ? "Activo" : "Inactivo")}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            var bytes = Encoding.UTF8.GetBytes(html.ToString());

            return File(bytes, "text/html", "Proveedores.html");
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

        public IActionResult DescargarProveedoresCSV()
        {
            var proveedores = _dbContext.Proveedores.ToList();

            using (var stream = new MemoryStream())
            {
                // UTF8Encoding(true) agrega el BOM para que Excel detecte la codificación correctamente
                using (var writer = new StreamWriter(stream, new UTF8Encoding(true)))
                {
                    writer.WriteLine("ID,Nombre,Correo,Teléfono,Dirección,Estado");

                    foreach (var proveedor in proveedores)
                    {
                        string estado = proveedor.Estado ? "Activo" : "Inactivo";
                        var campos = new[]
                        {
                            proveedor.IdProveedor.ToString(),
                            EscaparCampoCsv(proveedor.Nombre),
                            EscaparCampoCsv(proveedor.Correo),
                            EscaparCampoCsv(proveedor.Telefono),
                            EscaparCampoCsv(proveedor.Direccion),
                            estado
                        };
                        writer.WriteLine(string.Join(",", campos));
                    }

                    writer.Flush();
                    stream.Position = 0;
                }

                return File(stream.ToArray(), "text/csv", "Proveedores.csv");
            }
        }

    }
}
