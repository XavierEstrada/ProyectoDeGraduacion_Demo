using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using ProyectoSGIOCore.Filters;
using Xunit;

namespace ProyectoSGIOCore.Tests
{
    public class RestriccionUsuarioClienteFilterTests
    {
        private static ClaimsPrincipal UsuarioConRol(string rol)
        {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, rol) }, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        private static ClaimsPrincipal UsuarioAnonimo() => new ClaimsPrincipal(new ClaimsIdentity());

        private static async Task<bool> FueRedirigidoAsync(ClaimsPrincipal usuario, string controller, string action)
        {
            var httpContext = new DefaultHttpContext { User = usuario };
            var routeData = new RouteData();
            routeData.Values["controller"] = controller;
            routeData.Values["action"] = action;

            var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(httpContext, routeData, new ActionDescriptor());
            var filters = new List<IFilterMetadata>();
            var controllerInstance = new object();

            var executingContext = new ActionExecutingContext(
                actionContext, filters, new Dictionary<string, object>(), controllerInstance);

            ActionExecutionDelegate next = () =>
                Task.FromResult(new ActionExecutedContext(actionContext, filters, controllerInstance));

            var filtro = new RestriccionUsuarioClienteFilter();
            await filtro.OnActionExecutionAsync(executingContext, next);

            return executingContext.Result != null;
        }

        [Fact]
        public async Task Usuario_AccessingDashboard_IsAllowed()
        {
            Assert.False(await FueRedirigidoAsync(UsuarioConRol("Usuario"), "Proyecto", "Dashboard"));
        }

        [Fact]
        public async Task Usuario_AccessingGestionarProyecto_IsRedirected()
        {
            Assert.True(await FueRedirigidoAsync(UsuarioConRol("Usuario"), "Proyecto", "GestionarProyecto"));
        }

        [Fact]
        public async Task Usuario_AccessingFacturasController_IsRedirected()
        {
            Assert.True(await FueRedirigidoAsync(UsuarioConRol("Usuario"), "Facturas", "VisualizarFacturas"));
        }

        [Fact]
        public async Task Empleado_AccessingKanban_IsAllowed()
        {
            Assert.False(await FueRedirigidoAsync(UsuarioConRol("Empleado"), "Proyecto", "Kanban"));
        }

        [Fact]
        public async Task Empleado_AccessingAgregarFase_IsRedirected()
        {
            Assert.True(await FueRedirigidoAsync(UsuarioConRol("Empleado"), "Proyecto", "AgregarFase"));
        }

        [Fact]
        public async Task Administrador_AccessingAnyController_IsAllowed()
        {
            Assert.False(await FueRedirigidoAsync(UsuarioConRol("Administrador"), "Facturas", "VisualizarFacturas"));
        }

        [Fact]
        public async Task UnauthenticatedUser_IsAllowed()
        {
            Assert.False(await FueRedirigidoAsync(UsuarioAnonimo(), "Proyecto", "GestionarProyecto"));
        }
    }
}
