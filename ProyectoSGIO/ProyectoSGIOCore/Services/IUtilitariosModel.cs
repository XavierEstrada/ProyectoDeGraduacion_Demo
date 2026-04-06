namespace ProyectoSGIOCore.Services
{
    public interface IUtilitariosModel
    {
        public string GenerarToken(long codigo_usuario);
        public string Encrypt(string texto);
        public string Decrypt(string texto);
        public string GenerarCodigo();
        public string GenerarPin();
        public void EnviarCorreo(string Destinatario, string Asunto, string Mensaje);
    }
}
