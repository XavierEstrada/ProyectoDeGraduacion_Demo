using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProyectoSGIOCore.Data;
using ProyectoSGIOCore.Models;

namespace ProyectoSGIOCore.Services
{
    public class ComentarioService : IComentarioService
    {
        private readonly AppDBContext _dbContext;

        public ComentarioService(AppDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Comentario> AgregarAsync(ClaimsPrincipal usuario, string entidadTipo, int entidadId, string texto)
        {
            var usuarioId = int.Parse(usuario.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var nombre = usuario.FindFirst(ClaimTypes.Name)?.Value ?? "Usuario";

            var comentario = new Comentario
            {
                EntidadTipo = entidadTipo,
                EntidadId = entidadId,
                UsuarioId = usuarioId,
                UsuarioNombre = nombre,
                Texto = texto,
                Fecha = DateTime.UtcNow
            };

            _dbContext.Comentarios.Add(comentario);
            await _dbContext.SaveChangesAsync();

            return comentario;
        }

        public async Task<List<Comentario>> ListarAsync(string entidadTipo, int entidadId)
        {
            return await _dbContext.Comentarios
                .Where(c => c.EntidadTipo == entidadTipo && c.EntidadId == entidadId)
                .OrderBy(c => c.Fecha)
                .ToListAsync();
        }

        public async Task<bool> EliminarAsync(int comentarioId, ClaimsPrincipal usuario)
        {
            var comentario = await _dbContext.Comentarios.FindAsync(comentarioId);
            if (comentario == null)
            {
                return false;
            }

            var usuarioId = int.Parse(usuario.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var esAutor = comentario.UsuarioId == usuarioId;
            var esAdministrador = usuario.IsInRole("Administrador");

            if (!esAutor && !esAdministrador)
            {
                return false;
            }

            _dbContext.Comentarios.Remove(comentario);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
