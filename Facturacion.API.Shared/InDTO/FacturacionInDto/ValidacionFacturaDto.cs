using Facturacion.API.Util;

namespace Facturacion.API.Shared.InDTO.FacturacionInDto
{
    public class ValidacionFacturaDto
    {
        public bool EsValida { get; set; }
        public List<string> Errores { get; set; } = new List<string>();
        public List<string> Advertencias { get; set; } = new List<string>();
        public FacturaTotalesDto? TotalesCalculados { get; set; }
    }
}
