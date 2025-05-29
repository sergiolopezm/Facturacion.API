namespace Facturacion.API.Shared.InDTO.ArticulosInDto
{
    public class ArticuloVendidoDto
    {
        public int ArticuloId { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public int CantidadVendida { get; set; }
        public decimal MontoVendido { get; set; }
        public string? MontoVendidoFormateado { get; set; }
        public int VecesVendido { get; set; }
    }
}
