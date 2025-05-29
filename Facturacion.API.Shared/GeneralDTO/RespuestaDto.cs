namespace Facturacion.API.Shared.GeneralDTO
{
    /// <summary>
    /// Clase para estandarizar las respuestas de la API
    /// </summary>
    public class RespuestaDto
    {
        public bool Exito { get; set; }
        public string? Mensaje { get; set; }
        public string? Detalle { get; set; }
        public object? Resultado { get; set; }

        public static RespuestaDto ErrorInterno(string? detalle = null)
        {
            return new RespuestaDto
            {
                Exito = false,
                Mensaje = "Error de servidor",
                Detalle = detalle ?? "No se pudo cumplir con la solicitud, se ha presentado un error.",
                Resultado = null
            };
        }

        public static RespuestaDto ParametrosIncorrectos(string mensaje, string detalle)
        {
            return new RespuestaDto
            {
                Exito = false,
                Mensaje = mensaje,
                Detalle = detalle,
                Resultado = null
            };
        }

        public static RespuestaDto Exitoso(string mensaje, string detalle, object? resultado = null)
        {
            return new RespuestaDto
            {
                Exito = true,
                Mensaje = mensaje,
                Detalle = detalle,
                Resultado = resultado
            };
        }

        public static RespuestaDto NoEncontrado(string entidad)
        {
            return new RespuestaDto
            {
                Exito = false,
                Mensaje = $"{entidad} no encontrado",
                Detalle = $"No se encontró el {entidad} solicitado.",
                Resultado = null
            };
        }
    }
}
