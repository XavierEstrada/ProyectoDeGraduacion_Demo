using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProyectoSGIOCore.Data;
using ProyectoSGIOCore.Models;
using System.Linq;

namespace ProyectoSGIOCore.Controllers
{
    [Authorize(Roles = "Administrador , Supervisor")]
    public class CierresFinancierosController : Controller
    {
        private readonly AppDBContext _dbContext;

        public CierresFinancierosController(AppDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public IActionResult RegistrarCierre()
        {
            return View("~/Views/Facturas/RegistrarCierre.cshtml");
        }

        [HttpPost]
        public IActionResult RegistrarCierre(int anio, string observaciones)
        {
            var ingresos = _dbContext.Facturas
                .Where(f => f.FechaEmision.Year == anio && f.MontoTotal > 0)
                .Sum(f => f.MontoTotal);

            var egresos = _dbContext.Facturas
                .Where(f => f.FechaEmision.Year == anio && f.MontoTotal < 0)
                .Sum(f => f.MontoTotal);

            var cierre = new CierreFinanciero
            {
                Anio = anio,
                FechaCierre = DateTime.Now,
                TotalIngresos = ingresos,
                TotalEgresos = Math.Abs(egresos), // Convertir a positivo
                Utilidad = ingresos - Math.Abs(egresos),
                Observaciones = observaciones
            };

            _dbContext.CierresFinancieros.Add(cierre);
            _dbContext.SaveChanges();

            return RedirectToAction("VisualizarCierres");
        }

        [HttpGet]
        public IActionResult VisualizarCierres()
        {
            // Obtiene la lista de cierres financieros
            var cierres = _dbContext.CierresFinancieros.ToList();

            return View("~/Views/Facturas/VisualizarCierres.cshtml", cierres);
        }

        public IActionResult RegistrarCierreMensual()
        {
            return View("~/Views/Facturas/RegistrarCierreMensual.cshtml");
        }

        [HttpPost]
        public IActionResult RegistrarCierreMensual(int anio, int mes, string observaciones)
        {
            var ingresos = _dbContext.Facturas
                .Where(f => f.FechaEmision.Year == anio && f.FechaEmision.Month == mes && f.MontoTotal > 0)
                .Sum(f => f.MontoTotal);

            var egresos = _dbContext.Facturas
                .Where(f => f.FechaEmision.Year == anio && f.FechaEmision.Month == mes && f.MontoTotal < 0)
                .Sum(f => f.MontoTotal);

            var cierre = new CierreFinanciero
            {
                Anio = anio,
                Mes = mes,
                FechaCierre = DateTime.Now,
                TotalIngresos = ingresos,
                TotalEgresos = Math.Abs(egresos),
                Utilidad = ingresos - Math.Abs(egresos),
                Observaciones = observaciones
            };

            _dbContext.CierresFinancieros.Add(cierre);
            _dbContext.SaveChanges();

            return RedirectToAction("VisualizarCierresMensuales");
        }

        [HttpPost]
        public IActionResult EliminarCierre(int id)
        {
            var cierre = _dbContext.CierresFinancieros.Find(id);
            if (cierre == null)
            {
                TempData["MensajeError"] = "Cierre financiero no encontrado.";
                return RedirectToAction("VisualizarCierres");
            }

            bool esMensual = cierre.Mes > 0;
            _dbContext.CierresFinancieros.Remove(cierre);
            _dbContext.SaveChanges();
            TempData["MensajeExito"] = "Cierre financiero eliminado correctamente.";

            return esMensual
                ? RedirectToAction("VisualizarCierresMensuales")
                : RedirectToAction("VisualizarCierres");
        }

        [HttpGet]
        public IActionResult VisualizarCierresMensuales()
        {
            var cierresMensuales = _dbContext.CierresFinancieros
                .Where(c => c.Mes > 0) // Filtrar por cierres mensuales
                .ToList();

            return View("~/Views/Facturas/VisualizarCierresMensuales.cshtml", cierresMensuales);
        }
    }
}
