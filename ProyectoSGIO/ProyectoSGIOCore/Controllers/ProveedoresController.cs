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
            html.AppendLine("<head><meta charset='UTF-8'><title>Lista de Proveedores</title></head>");
            html.AppendLine("<body>");
            html.AppendLine("<h1 style='text-align:center;'>Lista de Proveedores</h1>");
            html.AppendLine("<table border='1' width='100%' style='border-collapse:collapse;'>");
            html.AppendLine("<thead><tr><th>ID</th><th>Nombre</th><th>Correo</th><th>Teléfono</th><th>Dirección</th><th>Estado</th></tr></thead>");
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

            html.AppendLine("</tbody></table>");
            html.AppendLine("</body></html>");

            var bytes = Encoding.UTF8.GetBytes(html.ToString());

            return File(bytes, "text/html", "Proveedores.html");
        }

        public IActionResult DescargarProveedoresCSV()
        {
            var proveedores = _dbContext.Proveedores.ToList();

            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.WriteLine("ID,Nombre,Correo,Teléfono,Dirección,Estado");

                    foreach (var proveedor in proveedores)
                    {
                        string estado = proveedor.Estado ? "Activo" : "Inactivo";
                        writer.WriteLine($"{proveedor.IdProveedor},{proveedor.Nombre},{proveedor.Correo},{proveedor.Telefono},{proveedor.Direccion},{estado}");
                    }

                    writer.Flush();
                    stream.Position = 0;
                }

                return File(stream.ToArray(), "text/csv", "Proveedores.csv");
            }
        }

    }
}
