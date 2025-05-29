using System.Security.Cryptography;
using System.Text;

namespace Facturacion.API.Util
{
    public static class HashingHelper
    {
        /// <summary>
        /// Genera un hash SHA256 para la contraseña proporcionada
        /// </summary>
        public static string GetSHA256Hash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Convertir la entrada en bytes
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Convertir bytes a cadena hexadecimal
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Verifica si una contraseña coincide con su hash
        /// </summary>
        public static bool VerifyHash(string input, string hashToCompare)
        {
            string hashOfInput = GetSHA256Hash(input);
            return string.Equals(hashOfInput, hashToCompare, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Genera una sal aleatoria para reforzar el hash
        /// </summary>
        public static string GenerateSalt(int size = 16)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var buff = new byte[size];
                rng.GetBytes(buff);
                return Convert.ToBase64String(buff);
            }
        }

        /// <summary>
        /// Genera un hash con sal para mayor seguridad
        /// </summary>
        public static string HashWithSalt(string input, string salt)
        {
            return GetSHA256Hash(input + salt);
        }
    }
}
