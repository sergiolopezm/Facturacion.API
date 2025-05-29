namespace Facturacion.API.Shared.GeneralDTO
{
    /// <summary>
    /// Clase para validaciones generales
    /// </summary>
    public class ValidoDto
    {
        public bool EsValido { get; set; }
        public string? Detalle { get; set; }

        public static ValidoDto Invalido(string detalle)
        {
            return new ValidoDto
            {
                EsValido = false,
                Detalle = detalle
            };
        }

        public static ValidoDto Invalido()
        {
            return new ValidoDto
            {
                EsValido = false,
            };
        }

        public static ValidoDto Valido()
        {
            return new ValidoDto
            {
                EsValido = true,
            };
        }

        public static ValidoDto Valido(string detalle)
        {
            return new ValidoDto
            {
                EsValido = true,
                Detalle = detalle
            };
        }
    }
}
