namespace Facturacion.API.Domain.Contracts
{
    /// <summary>
    /// Interfaz para la validación de acceso a la API
    /// </summary>
    public interface IAccesoRepository
    {
        /// <summary>
        /// Valida si la combinación de sitio y contraseña es válida
        /// </summary>
        Task<bool> ValidarAccesoAsync(string sitio, string contraseña);
    }
}
