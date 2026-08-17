namespace ProyectoSGIOCore.Services
{
    public interface ISupabaseStorageService
    {
        Task<string> SubirArchivoAsync(string bucket, string path, byte[] contenido, string contentType);
        Task EliminarArchivoAsync(string bucket, string path);
        string ObtenerUrlPublica(string bucket, string path);
    }
}
