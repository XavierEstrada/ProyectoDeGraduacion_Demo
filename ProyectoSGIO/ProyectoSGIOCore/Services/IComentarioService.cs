using System.Security.Claims;
using ProyectoSGIOCore.Models;

namespace ProyectoSGIOCore.Services
{
    public interface IComentarioService
    {
        Task<Comentario> AgregarAsync(ClaimsPrincipal usuario, string entidadTipo, int entidadId, string texto);
        Task<List<Comentario>> ListarAsync(string entidadTipo, int entidadId);
        Task<bool> EliminarAsync(int comentarioId, ClaimsPrincipal usuario);
    }
}
