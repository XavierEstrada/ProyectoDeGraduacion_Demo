using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoSGIOCore.Data;
using System.Security.Claims;

namespace ProyectoSGIOCore.Controllers
{
    [AllowAnonymous]
    public class BusquedaController : Controller
    {
        private readonly AppDBContext _dbContext;
        private const int LimitePorCategoria = 5;

        public BusquedaController(AppDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Global(string q)
        {
            q = (q ?? "").Trim();
            if (q.Length < 2)
            {
                return Json(new { proyectos = Array.Empty<object>(), facturas = Array.Empty<object>(), proveedores = Array.Empty<object>() });
            }

            var qLower = q.ToLowerInvariant();

            // Mismas reglas que el resto del sistema: un cliente (Usuario) o un Empleado
            // no deben poder usar la búsqueda para llegar a módulos o proyectos que
            // tienen bloqueados en el menú y en el backend.
            bool esUsuarioCliente = User.Identity?.IsAuthenticated == true && User.IsInRole("Usuario");
            bool esEmpleado = User.Identity?.IsAuthenticated == true && User.IsInRole("Empleado");
            bool puedeVerFacturasYProveedores = !esUsuarioCliente && !esEmpleado;

            var proyectosQuery = _dbContext.Proyectos
                .Where(p => p.Nombre.ToLower().Contains(qLower));

            if (esUsuarioCliente)
            {
                var idUsuarioActual = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                proyectosQuery = proyectosQuery.Where(p => p.IdUsuario == idUsuarioActual);
            }

            var proyectosRaw = await proyectosQuery
                .OrderBy(p => p.Nombre)
                .Take(LimitePorCategoria)
                .Select(p => new { p.Id, p.Nombre })
                .ToListAsync();

            var proyectos = proyectosRaw.Select(p => new
            {
                texto = p.Nombre,
                subtexto = "Proyecto",
                tipo = "Proyecto",
                url = Url.Action("Dashboard", "Proyecto", new { id = p.Id })
            });

            if (!puedeVerFacturasYProveedores)
            {
                return Json(new { proyectos, facturas = Array.Empty<object>(), proveedores = Array.Empty<object>() });
            }

            var facturasRaw = await _dbContext.Facturas
                .Include(f => f.Proveedor)
                .Where(f => f.NumeroFactura.ToLower().Contains(qLower)
                    || (f.Proveedor != null && f.Proveedor.Nombre.ToLower().Contains(qLower)))
                .OrderByDescending(f => f.FechaEmision)
                .Take(LimitePorCategoria)
                .Select(f => new { f.IdFactura, f.NumeroFactura, ProveedorNombre = f.Proveedor != null ? f.Proveedor.Nombre : null })
                .ToListAsync();

            var proveedoresRaw = await _dbContext.Proveedores
                .Where(p => p.Nombre.ToLower().Contains(qLower) || p.Correo.ToLower().Contains(qLower))
                .OrderBy(p => p.Nombre)
                .Take(LimitePorCategoria)
                .Select(p => new { p.IdProveedor, p.Nombre, p.Correo })
                .ToListAsync();

            var facturas = facturasRaw.Select(f => new
            {
                texto = f.NumeroFactura,
                subtexto = f.ProveedorNombre ?? "Sin proveedor",
                tipo = "Factura",
                url = Url.Action("VisualizarFacturas", "Facturas")
            });

            var proveedores = proveedoresRaw.Select(p => new
            {
                texto = p.Nombre,
                subtexto = p.Correo,
                tipo = "Proveedor",
                url = Url.Action("VisualizarProveedores", "Proveedores")
            });

            return Json(new { proyectos, facturas, proveedores });
        }
    }
}
