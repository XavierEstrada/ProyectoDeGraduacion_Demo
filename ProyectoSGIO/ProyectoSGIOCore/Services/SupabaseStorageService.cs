using System.Net.Http.Headers;

namespace ProyectoSGIOCore.Services
{
    public class SupabaseStorageService : ISupabaseStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _serviceRoleKey;

        public SupabaseStorageService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _supabaseUrl = configuration["settings:SupabaseUrl"]?.TrimEnd('/');
            _serviceRoleKey = configuration["settings:SupabaseServiceRoleKey"];
        }

        public async Task<string> SubirArchivoAsync(string bucket, string path, byte[] contenido, string contentType)
        {
            var url = $"{_supabaseUrl}/storage/v1/object/{bucket}/{path}";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {_serviceRoleKey}");
            request.Headers.Add("apikey", _serviceRoleKey);
            request.Headers.Add("x-upsert", "true");
            request.Content = new ByteArrayContent(contenido);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return path;
        }

        public async Task EliminarArchivoAsync(string bucket, string path)
        {
            var url = $"{_supabaseUrl}/storage/v1/object/{bucket}/{path}";
            using var request = new HttpRequestMessage(HttpMethod.Delete, url);
            request.Headers.Add("Authorization", $"Bearer {_serviceRoleKey}");
            request.Headers.Add("apikey", _serviceRoleKey);
            await _httpClient.SendAsync(request);
        }

        public string ObtenerUrlPublica(string bucket, string path)
        {
            return $"{_supabaseUrl}/storage/v1/object/public/{bucket}/{path}";
        }
    }
}
