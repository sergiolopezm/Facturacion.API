using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Facturacion.API.Util
{
    public static class ValidationHelper
    {
        /// <summary>
        /// Valida un objeto según sus atributos de validación de datos
        /// </summary>
        public static bool ValidarObjeto<T>(T objeto, out List<ValidationResult> errores)
        {
            var context = new ValidationContext(objeto);
            errores = new List<ValidationResult>();
            return Validator.TryValidateObject(objeto, context, errores, true);
        }

        /// <summary>
        /// Obtiene los mensajes de error de validación
        /// </summary>
        public static List<string> ObtenerMensajesError(List<ValidationResult> errores)
        {
            return errores.Select(e => e.ErrorMessage).ToList();
        }

        /// <summary>
        /// Valida si una cadena es una dirección de correo electrónico válida
        /// </summary>
        public static bool EsEmailValido(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            try
            {
                // Uso de Regex para validación de correo
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Valida si una cadena es una dirección IP válida
        /// </summary>
        public static bool EsIpValida(string ip)
        {
            if (string.IsNullOrEmpty(ip))
                return false;

            // Aceptar localhost
            if (ip == "::1" || ip == "127.0.0.1")
                return true;

            // Validar formato IPv4
            var regex = new Regex(@"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
            return regex.IsMatch(ip);
        }

        /// <summary>
        /// Valida si una cadena tiene un formato de nombre de usuario válido
        /// </summary>
        public static bool EsNombreUsuarioValido(string nombreUsuario)
        {
            if (string.IsNullOrEmpty(nombreUsuario))
                return false;

            // Al menos 3 caracteres, alfanuméricos y _ 
            var regex = new Regex(@"^[a-zA-Z0-9_]{3,}$");
            return regex.IsMatch(nombreUsuario);
        }

        /// <summary>
        /// Valida si una contraseña cumple con requisitos mínimos de seguridad
        /// </summary>
        public static bool EsContraseñaSegura(string contraseña)
        {
            if (string.IsNullOrEmpty(contraseña))
                return false;

            // Al menos 6 caracteres, una letra mayúscula, una minúscula y un número
            var regex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$");
            return regex.IsMatch(contraseña);
        }
    }
}
