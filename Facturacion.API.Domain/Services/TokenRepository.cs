using Facturacion.API.Domain.Contracts;
using Facturacion.API.Infrastructure;
using Facturacion.API.Shared.GeneralDTO;
using Facturacion.API.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Facturacion.API.Domain.Services
{
    /// <summary>
    /// Implementación del repositorio de tokens para autenticación
    /// </summary>
    public class TokenRepository : ITokenRepository
    {
        private readonly DBContext _context;
        private readonly ILogger<TokenRepository> _logger;
        private readonly IConfiguration _configuration;
        private readonly byte[] _keyBytes;
        private readonly int _tiempoExpiracionMinutos;
        private readonly int _tiempoExpiracionBDMinutos;

        public TokenRepository(
            DBContext context,
            ILogger<TokenRepository> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;

            string keyJwt = _configuration["JwtSettings:Key"] ?? throw new InvalidOperationException("JWT Key no configurada");
            _keyBytes = Encoding.UTF8.GetBytes(keyJwt);
            _tiempoExpiracionMinutos = int.Parse(_configuration["JwtSettings:TiempoExpiracionMinutos"] ?? "30");
            _tiempoExpiracionBDMinutos = int.Parse(_configuration["JwtSettings:TiempoExpiracionBDMinutos"] ?? "60");
        }

        /// <summary>
        /// Genera un nuevo token JWT para un usuario
        /// </summary>
        public async Task<string> GenerarTokenAsync(Usuario usuario, string ip)
        {
            await RemoverTokensExpiradosAsync(usuario.Id);
            string token = CrearTokenUsuario(usuario, ip);
            await GuardarTokenBDAsync(token, usuario, ip);
            return token;
        }

        /// <summary>
        /// Cancela un token existente (logout)
        /// </summary>
        public async Task<bool> CancelarTokenAsync(string token)
        {
            try
            {
                var tokenActual = await _context.Tokens.FirstOrDefaultAsync(t => t.Token1 == token);
                if (tokenActual == null)
                {
                    return false;
                }

                // Mover a tokens expirados
                var tokenExpirado = new TokensExpirado
                {
                    Token = tokenActual.Token1,
                    UsuarioId = tokenActual.UsuarioId,
                    Ip = tokenActual.Ip,
                    FechaCreacion = tokenActual.FechaCreacion,
                    FechaExpiracion = DateTime.Now,
                    Observacion = "Token cancelado por el usuario (logout)"
                };

                await _context.TokensExpirados.AddAsync(tokenExpirado);
                _context.Tokens.Remove(tokenActual);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar token");
                return false;
            }
        }

        /// <summary>
        /// Obtiene información de un token
        /// </summary>
        public async Task<object> ObtenerInformacionTokenAsync(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            if (!tokenHandler.CanReadToken(token))
            {
                return new { Valido = false, Mensaje = "Token no válido" };
            }

            var tokenJwt = tokenHandler.ReadJwtToken(token);
            var tokenBD = await _context.Tokens.FirstOrDefaultAsync(t => t.Token1 == token);

            if (tokenBD == null)
            {
                return new { Valido = false, Mensaje = "El token no existe en la base de datos" };
            }

            return new
            {
                Valido = tokenBD.FechaExpiracion > DateTime.Now,
                Claims = tokenJwt.Claims.Select(c => new { c.Type, c.Value }).ToList(),
                FechaCreacion = tokenBD.FechaCreacion,
                FechaExpiracion = tokenBD.FechaExpiracion,
                Usuario = await _context.Usuarios
                    .Include(u => u.Rol)
                    .Where(u => u.Id == tokenBD.UsuarioId)
                    .Select(u => new { u.NombreUsuario, u.Nombre, u.Apellido, Rol = u.Rol.Nombre })
                    .FirstOrDefaultAsync()
            };
        }

        /// <summary>
        /// Verifica si un token es válido
        /// </summary>
        public async Task<ValidoDto> EsValidoAsync(string idToken, Guid idUsuario, string ip)
        {
            var validarToken = await ValidarTokenEnSistemaAsync(idToken, idUsuario, ip);
            if (!validarToken.EsValido)
            {
                return validarToken;
            }
            return await ValidarTokenEnBDAsync(idToken);
        }

        /// <summary>
        /// Aumenta el tiempo de expiración de un token
        /// </summary>
        public async Task AumentarTiempoExpiracionAsync(string token)
        {
            var tokenBD = await _context.Tokens.FirstOrDefaultAsync(t => t.Token1 == token);
            if (tokenBD != null)
            {
                tokenBD.FechaExpiracion = DateTime.Now.AddMinutes(_tiempoExpiracionBDMinutos);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Tiempo de expiración aumentado para el token del usuario {UsuarioId}", tokenBD.UsuarioId);
            }
        }

        #region Métodos privados

        /// <summary>
        /// Crea un token JWT para un usuario
        /// </summary>
        private string CrearTokenUsuario(Usuario usuario, string ip)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.NombreUsuario),
                new Claim("Nombre", usuario.Nombre),
                new Claim("Apellido", usuario.Apellido),
                new Claim(ClaimTypes.Role, usuario.Rol.Nombre),
                new Claim("Ip", ip),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            //var credencialesToken = new SigningCredentials(
            //    new SymmetricSecurityKey(_keyBytes),
            //    SecurityAlgorithms.HmacSha256Signature);

            //var tokenDescriptor = new SecurityTokenDescriptor
            //{
            //    Subject = claims,
            //    Expires = DateTime.UtcNow.AddMinutes(_tiempoExpiracionMinutos),
            //    SigningCredentials = credencialesToken,
            //    Issuer = _configuration["JwtSettings:Issuer"],
            //    Audience = _configuration["JwtSettings:Audience"]
            //};

            //var tokenHandler = new JwtSecurityTokenHandler();
            //var tokenConfig = tokenHandler.CreateToken(tokenDescriptor);

            //return tokenHandler.WriteToken(tokenConfig);

            return JwtHelper.GenerateJwtToken(
                  claims,
                  _configuration["JwtSettings:Key"],
                  _configuration["JwtSettings:Issuer"],
                  _configuration["JwtSettings:Audience"],
                  DateTime.UtcNow.AddMinutes(_tiempoExpiracionMinutos)
              );
        }

        /// <summary>
        /// Guarda un token en la base de datos
        /// </summary>
        private async Task GuardarTokenBDAsync(string token, Usuario usuario, string ip)
        {
            var tokenEntity = new Token
            {
                Token1 = token,
                UsuarioId = usuario.Id,
                Ip = ip,
                FechaCreacion = DateTime.Now,
                FechaExpiracion = DateTime.Now.AddMinutes(_tiempoExpiracionBDMinutos)
            };

            await _context.Tokens.AddAsync(tokenEntity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Elimina tokens expirados para un usuario
        /// </summary>
        private async Task RemoverTokensExpiradosAsync(Guid usuarioId)
        {
            // Buscar token activo del usuario
            var tokenUsuarioActivo = await _context.Tokens
                .FirstOrDefaultAsync(t => t.UsuarioId == usuarioId && t.FechaExpiracion > DateTime.Now);

            // Buscar tokens expirados del usuario
            var tokensUsuarioExpirados = await _context.Tokens
                .Where(u => u.UsuarioId == usuarioId && u.FechaExpiracion < DateTime.Now)
                .ToListAsync();

            // Mover tokens expirados a la tabla de históricos
            if (tokensUsuarioExpirados.Any())
            {
                var tokensExpirados = tokensUsuarioExpirados.Select(t => new TokensExpirado
                {
                    Token = t.Token1,
                    UsuarioId = t.UsuarioId,
                    Ip = t.Ip,
                    FechaCreacion = t.FechaCreacion,
                    FechaExpiracion = t.FechaExpiracion,
                    Observacion = t.Observacion
                }).ToList();

                await _context.TokensExpirados.AddRangeAsync(tokensExpirados);
                _context.Tokens.RemoveRange(tokensUsuarioExpirados);
            }

            // Si hay un token activo, invalidarlo porque el usuario está iniciando sesión desde otro dispositivo
            if (tokenUsuarioActivo != null)
            {
                tokenUsuarioActivo.Observacion = "La sesión ha caducado debido a que el usuario ha ingresado desde otro equipo";
                tokenUsuarioActivo.FechaExpiracion = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Valida un token en el sistema (JWT)
        /// </summary>
        private async Task<ValidoDto> ValidarTokenEnSistemaAsync(string idToken, Guid idUsuario, string ip)
        {
            if (string.IsNullOrEmpty(idToken) || idUsuario == Guid.Empty || string.IsNullOrEmpty(ip))
            {
                return ValidoDto.Invalido("La información referente a la sesión de usuario está incompleta");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(_keyBytes),
                ValidateIssuer = !string.IsNullOrEmpty(_configuration["JwtSettings:Issuer"]),
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidateAudience = !string.IsNullOrEmpty(_configuration["JwtSettings:Audience"]),
                ValidAudience = _configuration["JwtSettings:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // Sin margen de tiempo para la expiración
            };

            try
            {
                ClaimsPrincipal principal = tokenHandler.ValidateToken(idToken, validationParameters, out _);

                Guid claimIdUsuario = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
                string claimIp = principal.FindFirst("Ip")?.Value ?? string.Empty;

                if (idUsuario != claimIdUsuario || ip != claimIp)
                {
                    return ValidoDto.Invalido("La información referente a la sesión de usuario es incorrecta");
                }

                return ValidoDto.Valido();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validando token JWT para usuario {UsuarioId}", idUsuario);
                return ValidoDto.Invalido("La sesión de usuario no se encuentra activa o no existe en el sistema. Por favor, inicie sesión");
            }
        }

        /// <summary>
        /// Valida un token en la base de datos
        /// </summary>
        private async Task<ValidoDto> ValidarTokenEnBDAsync(string idToken)
        {
            var token = await _context.Tokens.FirstOrDefaultAsync(t => t.Token1 == idToken);

            if (token == null)
            {
                return ValidoDto.Invalido("La sesión de usuario no se encuentra activa. Por favor, inicie sesión");
            }

            if (token.FechaExpiracion < DateTime.Now)
            {
                if (string.IsNullOrEmpty(token.Observacion))
                {
                    return ValidoDto.Invalido("La sesión de usuario ha caducado por tiempo de inactividad. Por favor, inicie sesión nuevamente");
                }
                return ValidoDto.Invalido(token.Observacion);
            }

            return ValidoDto.Valido();
        }

        #endregion
    }
}
