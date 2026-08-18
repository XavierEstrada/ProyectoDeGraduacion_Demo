using Microsoft.Extensions.Configuration;
using ProyectoSGIOCore.Models;
using Xunit;

namespace ProyectoSGIOCore.Tests
{
    public class UtilitariosModelTests
    {
        private static UtilitariosModel CrearUtilitarios(string secretKey = "12345678901234567890123456789012")
        {
            var configuracion = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["settings:SecretKey"] = secretKey
                })
                .Build();

            return new UtilitariosModel(configuracion);
        }

        [Fact]
        public void Encrypt_Decrypt_RoundTrip_ReturnsOriginalText()
        {
            var utilitarios = CrearUtilitarios();
            var original = "P@ssw0rd-Secreta!";

            var cifrado = utilitarios.Encrypt(original);
            var descifrado = utilitarios.Decrypt(cifrado);

            Assert.NotEqual(original, cifrado);
            Assert.Equal(original, descifrado);
        }

        [Fact]
        public void Encrypt_SameInput_ProducesSameOutput_GivenFixedIV()
        {
            // Encrypt usa un IV fijo (todo ceros), así que el mismo texto con la misma
            // clave siempre produce el mismo cifrado — documentando este comportamiento.
            var utilitarios = CrearUtilitarios();

            var cifrado1 = utilitarios.Encrypt("mismo-texto");
            var cifrado2 = utilitarios.Encrypt("mismo-texto");

            Assert.Equal(cifrado1, cifrado2);
        }
    }
}
