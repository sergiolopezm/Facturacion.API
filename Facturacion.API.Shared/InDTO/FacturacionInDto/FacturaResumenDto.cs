namespace Facturacion.API.Shared.InDTO.FacturacionInDto
{
    public class FacturaResumenDto
    {
        public int Id { get; set; }
        public string? NumeroFactura { get; set; }
        public DateTime Fecha { get; set; }
        public string? ClienteNombreCompleto { get; set; }
        public string? ClienteNumeroDocumento { get; set; }
        public decimal Total { get; set; }
        public string? TotalFormateado { get; set; }
        public string Estado { get; set; } = null!;
        public int TotalArticulos { get; set; }
        public string? CreadoPor { get; set; }
        public string? FechaFormateada { get; set; }
    }
}
