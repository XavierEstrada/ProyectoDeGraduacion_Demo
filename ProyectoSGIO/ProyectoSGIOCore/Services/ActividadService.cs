using System.Security.Claims;
using ProyectoSGIOCore.Data;
using ProyectoSGIOCore.Models;

namespace ProyectoSGIOCore.Services
{
    public class ActividadService : IActividadService
    {
        private readonly AppDBContext _dbContext;

        public ActividadService(AppDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task RegistrarAsync(ClaimsPrincipal usuario, string accion, string entidad, string detalle)
        {
            var nombre = usuario?.Identity?.IsAuthenticated == true
                ? (usuario.FindFirst(ClaimTypes.Name)?.Value ?? "Usuario")
                : "Sistema";

            _dbContext.RegistrosActividad.Add(new RegistroActividad
            {
                UsuarioNombre = nombre,
                Accion = accion,
                Entidad = entidad,
                Detalle = detalle,
                Fecha = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();
        }
    }
}
