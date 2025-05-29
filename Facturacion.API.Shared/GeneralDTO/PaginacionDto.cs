namespace Facturacion.API.Shared.GeneralDTO
{
    /// <summary>
    /// Clase genérica para paginación de resultados
    /// </summary>
    public class PaginacionDto<T> where T : class
    {
        public int Pagina { get; set; }
        public int TotalPaginas { get; set; }
        public int TotalRegistros { get; set; }
        public int ElementosPorPagina { get; set; }
        public List<T>? Lista { get; set; }
    }
}
