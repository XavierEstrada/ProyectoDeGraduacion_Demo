using System.Security.Claims;

namespace ProyectoSGIOCore.Services
{
    public interface IActividadService
    {
        Task RegistrarAsync(ClaimsPrincipal usuario, string accion, string entidad, string detalle);
    }
}
