using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Facturacion.API.Util
{
    public static class JwtHelper
    {
        /// <summary>
        /// Genera un token JWT con los claims proporcionados y configuración
        /// </summary>
        public static string GenerateJwtToken(
            IEnumerable<Claim> claims,
            string key,
            string issuer,
            string audience,
            DateTime expiration)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Valida un token JWT y devuelve los claims si es válido
        /// </summary>
        public static ClaimsPrincipal ValidateToken(string token, string key, string issuer, string audience)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ValidateIssuer = !string.IsNullOrEmpty(issuer),
                ValidateAudience = !string.IsNullOrEmpty(audience),
                ValidIssuer = issuer,
                ValidAudience = audience,
                ClockSkew = TimeSpan.Zero
            };

            return tokenHandler.ValidateToken(token, validationParameters, out _);
        }

        /// <summary>
        /// Extrae el valor de un claim específico del token
        /// </summary>
        public static string? GetClaimValue(string token, string claimType)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (tokenHandler.CanReadToken(token))
            {
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var claim = jwtToken.Claims.FirstOrDefault(c => c.Type == claimType);
                return claim?.Value;
            }
            return null;
        }

        /// <summary>
        /// Extrae todos los claims de un token como un diccionario
        /// </summary>
        public static Dictionary<string, string> GetAllClaims(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (tokenHandler.CanReadToken(token))
            {
                var jwtToken = tokenHandler.ReadJwtToken(token);
                return jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);
            }
            return new Dictionary<string, string>();
        }
    }
}
