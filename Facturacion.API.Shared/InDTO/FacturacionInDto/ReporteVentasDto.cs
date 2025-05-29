using Facturacion.API.Shared.InDTO.ArticulosInDto;
using Facturacion.API.Shared.InDTO.ClienteInDto;

namespace Facturacion.API.Shared.InDTO.FacturacionInDto
{
    public class ReporteVentasDto
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int TotalFacturas { get; set; }
        public decimal TotalVentas { get; set; }
        public string? TotalVentasFormateado { get; set; }
        public decimal TotalIVA { get; set; }
        public string? TotalIVAFormateado { get; set; }
        public decimal TotalDescuentos { get; set; }
        public string? TotalDescuentosFormateado { get; set; }
        public List<ArticuloVendidoDto>? ArticulosMasVendidos { get; set; }
        public List<ClienteFrecuenteDto>? ClientesFrecuentes { get; set; }
    }
}
