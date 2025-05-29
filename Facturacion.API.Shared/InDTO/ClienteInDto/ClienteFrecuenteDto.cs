namespace Facturacion.API.Shared.InDTO.ClienteInDto
{
    public class ClienteFrecuenteDto
    {
        public int ClienteId { get; set; }
        public string? NombreCompleto { get; set; }
        public string NumeroDocumento { get; set; } = null!;
        public int TotalFacturas { get; set; }
        public decimal MontoTotalCompras { get; set; }
        public string? MontoTotalComprasFormateado { get; set; }
        public DateTime? UltimaCompra { get; set; }
    }
}
