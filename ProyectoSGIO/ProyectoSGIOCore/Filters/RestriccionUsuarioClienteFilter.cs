using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProyectoSGIOCore.Filters
{
    /// <summary>
    /// Restringe la navegación de los roles "de campo" (Usuario y Empleado) a solo lo que
    /// les compete, incluso en rutas [AllowAnonymous] pensadas para visitantes del demo:
    ///
    /// - Usuario (cliente externo): solo su propia lista de proyectos y el dashboard
    ///   de sus proyectos asignados. Sin escritura de ningún tipo.
    /// - Empleado (personal de campo): puede ver todos los proyectos (tabla, gestión,
    ///   dashboard, kanban) y actualizar el estado de tareas e hitos, pero no entra a
    ///   Facturas, Inventario, Proveedores ni Empleados — ni puede crear/eliminar nada.
    /// </summary>
    public class RestriccionUsuarioClienteFilter : IAsyncActionFilter
    {
        private static readonly HashSet<string> ControladoresPermitidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "Acceso", "Home"
        };

        private static readonly HashSet<string> AccionesProyectoUsuario = new(StringComparer.OrdinalIgnoreCase)
        {
            "Proyectos", "Dashboard"
        };

        private static readonly HashSet<string> AccionesProyectoEmpleado = new(StringComparer.OrdinalIgnoreCase)
        {
            "Proyectos", "Dashboard", "GestionarProyecto", "Kanban", "ObtenerFase",
            "ActualizarColumnaKanban", "ActualizarEstadoHito"
        };

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                await next();
                return;
            }

            var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
            var action = context.RouteData.Values["action"]?.ToString() ?? "";
            bool esControladorProyecto = controller.Equals("Proyecto", StringComparison.OrdinalIgnoreCase);

            bool? permitido = null;

            if (user.IsInRole("Usuario"))
            {
                permitido = ControladoresPermitidos.Contains(controller)
                    || (esControladorProyecto && AccionesProyectoUsuario.Contains(action));
            }
            else if (user.IsInRole("Empleado"))
            {
                permitido = ControladoresPermitidos.Contains(controller)
                    || (esControladorProyecto && AccionesProyectoEmpleado.Contains(action));
            }

            if (permitido == false)
            {
                context.Result = new RedirectToActionResult("Proyectos", "Proyecto", null);
                return;
            }

            await next();
        }
    }
}
