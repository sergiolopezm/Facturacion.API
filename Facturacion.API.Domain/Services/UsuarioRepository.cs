using Facturacion.API.Domain.Contracts;
using Facturacion.API.Infrastructure;
using Facturacion.API.Shared.GeneralDTO;
using Facturacion.API.Shared.InDTO;
using Facturacion.API.Util;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Facturacion.API.Domain.Services
{
    /// <summary>
    /// Implementación del repositorio de usuarios
    /// </summary>
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly DBContext _context;
        private readonly ITokenRepository _tokenRepository;
        private readonly ILogger<UsuarioRepository> _logger;

        public UsuarioRepository(
            DBContext context,
            ITokenRepository tokenRepository,
            ILogger<UsuarioRepository> logger)
        {
            _context = context;
            _tokenRepository = tokenRepository;
            _logger = logger;
        }

        /// <summary>
        /// Autentica un usuario en el sistema
        /// </summary>
        public async Task<RespuestaDto> AutenticarUsuarioAsync(UsuarioLoginDto args)
        {
            try
            {
                // Obtener contraseña hasheada
                string contraseñaHash = GetSHA256Hash(args.Contraseña);

                var usuario = await _context.Usuarios
                    .Include(u => u.Rol)
                    .FirstOrDefaultAsync(u => u.NombreUsuario == args.NombreUsuario &&
                                            u.Contraseña == contraseñaHash);

                if (usuario == null)
                {
                    _logger.LogWarning("Intento de login fallido para usuario {NombreUsuario}", args.NombreUsuario);
                    return RespuestaDto.ParametrosIncorrectos(
                        "Sesión fallida",
                        "El usuario o la contraseña son incorrectos");
                }

                if (!usuario.Activo)
                {
                    _logger.LogWarning("Intento de login con usuario inactivo {NombreUsuario}", args.NombreUsuario);
                    return RespuestaDto.ParametrosIncorrectos(
                        "Sesión fallida",
                        "El usuario no se encuentra activo");
                }

                // Actualizar fecha de último acceso
                usuario.FechaUltimoAcceso = DateTime.Now;
                await _context.SaveChangesAsync();

                // Generar token JWT
                string token = await _tokenRepository.GenerarTokenAsync(usuario, args.Ip ?? "0.0.0.0");

                var usuarioOut = new UsuarioPerfilDto
                {
                    Id = usuario.Id,
                    NombreUsuario = usuario.NombreUsuario,
                    Nombre = usuario.Nombre,
                    Apellido = usuario.Apellido,
                    Email = usuario.Email,
                    Rol = usuario.Rol.Nombre,
                    RolId = usuario.RolId,
                    Activo = usuario.Activo,
                    FechaCreacion = usuario.FechaCreacion,
                    FechaUltimoAcceso = usuario.FechaUltimoAcceso
                };

                return RespuestaDto.Exitoso(
                    "Usuario autenticado",
                    $"El usuario {usuarioOut.NombreUsuario} se ha autenticado correctamente.",
                    new
                    {
                        Usuario = usuarioOut,
                        Token = token
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al autenticar usuario {NombreUsuario}", args.NombreUsuario);
                return RespuestaDto.ErrorInterno();
            }
        }

        /// <summary>
        /// Registra un nuevo usuario en el sistema
        /// </summary>
        public async Task<RespuestaDto> RegistrarUsuarioAsync(UsuarioRegistroDto args)
        {
            try
            {
                // Validaciones previas usando ValidationHelper
                if (!ValidationHelper.EsNombreUsuarioValido(args.NombreUsuario))
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Registro fallido",
                        "El nombre de usuario debe tener al menos 3 caracteres y solo puede contener letras, números y guiones bajos");
                }

                if (!ValidationHelper.EsEmailValido(args.Email))
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Registro fallido",
                        "El formato del email no es válido");
                }

                if (!ValidationHelper.EsContraseñaSegura(args.Contraseña))
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Registro fallido",
                        "La contraseña debe tener al menos 6 caracteres, una letra mayúscula, una minúscula y un número");
                }

                // Validar que el rol exista
                var rolExiste = await _context.Roles.AnyAsync(r => r.Id == args.RolId && r.Activo);
                if (!rolExiste)
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Registro fallido",
                        "El rol especificado no es válido o está inactivo");
                }

                // Validar que el nombre de usuario no exista
                if (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == args.NombreUsuario))
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Registro fallido",
                        "El nombre de usuario ya existe");
                }

                // Validar que el email no exista
                if (await _context.Usuarios.AnyAsync(u => u.Email == args.Email))
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Registro fallido",
                        "El email ya está registrado");
                }

                // Hashear contraseña
                string contraseñaHash = HashingHelper.GetSHA256Hash(args.Contraseña);

                // Crear nuevo usuario
                var usuario = new Usuario
                {
                    Id = Guid.NewGuid(),
                    NombreUsuario = args.NombreUsuario,
                    Contraseña = contraseñaHash,
                    Nombre = args.Nombre,
                    Apellido = args.Apellido,
                    Email = args.Email,
                    RolId = args.RolId,
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };

                await _context.Usuarios.AddAsync(usuario);
                await _context.SaveChangesAsync();

                var rol = await _context.Roles.FindAsync(args.RolId);

                return RespuestaDto.Exitoso(
                    "Usuario registrado",
                    "El usuario se ha registrado correctamente",
                    new
                    {
                        Id = usuario.Id,
                        NombreUsuario = usuario.NombreUsuario,
                        Email = usuario.Email,
                        Rol = rol?.Nombre ?? "Desconocido"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar usuario {NombreUsuario}", args.NombreUsuario);
                return RespuestaDto.ErrorInterno(ex.Message);
            }
        }

        /// <summary>
        /// Obtiene un usuario por su ID
        /// </summary>
        public async Task<UsuarioPerfilDto?> ObtenerUsuarioPorIdAsync(Guid id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
            {
                return null;
            }

            return new UsuarioPerfilDto
            {
                Id = usuario.Id,
                NombreUsuario = usuario.NombreUsuario,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Email = usuario.Email,
                Rol = usuario.Rol.Nombre,
                RolId = usuario.RolId,
                Activo = usuario.Activo,
                FechaCreacion = usuario.FechaCreacion,
                FechaUltimoAcceso = usuario.FechaUltimoAcceso
            };
        }

        /// <summary>
        /// Verifica si existe un usuario con el nombre de usuario especificado
        /// </summary>
        public async Task<bool> ExisteUsuarioAsync(string nombreUsuario)
        {
            return await _context.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario);
        }

        /// <summary>
        /// Verifica si existe un usuario con el email especificado
        /// </summary>
        public async Task<bool> ExisteEmailAsync(string email)
        {
            return await _context.Usuarios.AnyAsync(u => u.Email == email);
        }

        /// <summary>
        /// Obtiene todos los usuarios del sistema
        /// </summary>
        public async Task<List<UsuarioPerfilDto>> ObtenerTodosAsync()
        {
            return await _context.Usuarios
                .Include(u => u.Rol)
                .Where(u => u.Activo)
                .Select(u => new UsuarioPerfilDto
                {
                    Id = u.Id,
                    NombreUsuario = u.NombreUsuario,
                    Nombre = u.Nombre,
                    Apellido = u.Apellido,
                    Email = u.Email,
                    Rol = u.Rol.Nombre,
                    RolId = u.RolId,
                    Activo = u.Activo,
                    FechaCreacion = u.FechaCreacion,
                    FechaUltimoAcceso = u.FechaUltimoAcceso
                })
                .ToListAsync();
        }

        /// <summary>
        /// Actualiza un usuario existente
        /// </summary>
        public async Task<RespuestaDto> ActualizarUsuarioAsync(Guid id, UsuarioRegistroDto args)
        {
            try
            {
                // Validaciones similares usando ValidationHelper
                if (!ValidationHelper.EsNombreUsuarioValido(args.NombreUsuario))
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Actualización fallida",
                        "El nombre de usuario debe tener al menos 3 caracteres y solo puede contener letras, números y guiones bajos");
                }

                if (!ValidationHelper.EsEmailValido(args.Email))
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Actualización fallida",
                        "El formato del email no es válido");
                }

                // Si se proporciona contraseña nueva, validarla
                if (!string.IsNullOrEmpty(args.Contraseña) && !ValidationHelper.EsContraseñaSegura(args.Contraseña))
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Actualización fallida",
                        "La contraseña debe tener al menos 6 caracteres, una letra mayúscula, una minúscula y un número");
                }

                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario == null)
                {
                    return RespuestaDto.NoEncontrado("Usuario");
                }

                // Validar que el rol exista
                var rolExiste = await _context.Roles.AnyAsync(r => r.Id == args.RolId && r.Activo);
                if (!rolExiste)
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Actualización fallida",
                        "El rol especificado no es válido o está inactivo");
                }

                // Validar que el nombre de usuario no exista (si es diferente al actual)
                if (usuario.NombreUsuario != args.NombreUsuario &&
                    await _context.Usuarios.AnyAsync(u => u.NombreUsuario == args.NombreUsuario))
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Actualización fallida",
                        "El nombre de usuario ya existe");
                }

                // Validar que el email no exista (si es diferente al actual)
                if (usuario.Email != args.Email &&
                    await _context.Usuarios.AnyAsync(u => u.Email == args.Email))
                {
                    return RespuestaDto.ParametrosIncorrectos(
                        "Actualización fallida",
                        "El email ya está registrado");
                }

                // Actualizar usuario
                usuario.NombreUsuario = args.NombreUsuario;
                usuario.Nombre = args.Nombre;
                usuario.Apellido = args.Apellido;
                usuario.Email = args.Email;
                usuario.RolId = args.RolId;
                usuario.FechaModificacion = DateTime.Now;

                // Actualizar contraseña si se proporciona
                if (!string.IsNullOrEmpty(args.Contraseña))
                {
                    usuario.Contraseña = GetSHA256Hash(args.Contraseña);
                }

                await _context.SaveChangesAsync();

                var rol = await _context.Roles.FindAsync(args.RolId);

                return RespuestaDto.Exitoso(
                    "Usuario actualizado",
                    "El usuario se ha actualizado correctamente",
                    new
                    {
                        Id = usuario.Id,
                        NombreUsuario = usuario.NombreUsuario,
                        Email = usuario.Email,
                        Rol = rol?.Nombre ?? "Desconocido"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario {Id}", id);
                return RespuestaDto.ErrorInterno(ex.Message);
            }
        }

        /// <summary>
        /// Cambia el estado de un usuario (activo/inactivo)
        /// </summary>
        public async Task<RespuestaDto> CambiarEstadoUsuarioAsync(Guid id, bool estado)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario == null)
                {
                    return RespuestaDto.NoEncontrado("Usuario");
                }

                usuario.Activo = estado;
                usuario.FechaModificacion = DateTime.Now;

                // Si se está desactivando, invalidar todos sus tokens
                if (!estado)
                {
                    var tokensActivos = await _context.Tokens
                        .Where(t => t.UsuarioId == id && t.FechaExpiracion > DateTime.Now)
                        .ToListAsync();

                    foreach (var token in tokensActivos)
                    {
                        token.FechaExpiracion = DateTime.Now;
                        token.Observacion = "La sesión ha caducado debido a que el usuario ha sido desactivado";
                    }
                }

                await _context.SaveChangesAsync();

                return RespuestaDto.Exitoso(
                    $"Usuario {(estado ? "activado" : "desactivado")}",
                    $"El usuario {usuario.NombreUsuario} ha sido {(estado ? "activado" : "desactivado")} correctamente",
                    new
                    {
                        Id = usuario.Id,
                        NombreUsuario = usuario.NombreUsuario,
                        Activo = usuario.Activo
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado del usuario {Id}", id);
                return RespuestaDto.ErrorInterno(ex.Message);
            }
        }

        /// <summary>
        /// Cambia la contraseña de un usuario
        /// </summary>
        public async Task<RespuestaDto> CambiarContraseñaAsync(Guid id, string nuevaContraseña)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario == null)
                {
                    return RespuestaDto.NoEncontrado("Usuario");
                }

                usuario.Contraseña = GetSHA256Hash(nuevaContraseña);
                usuario.FechaModificacion = DateTime.Now;

                await _context.SaveChangesAsync();

                return RespuestaDto.Exitoso(
                    "Contraseña actualizada",
                    "La contraseña se ha actualizado correctamente",
                    null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña del usuario {Id}", id);
                return RespuestaDto.ErrorInterno(ex.Message);
            }
        }

        #region Métodos privados

        /// <summary>
        /// Genera un hash SHA256 para una contraseña
        /// </summary>
        private string GetSHA256Hash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        #endregion
    }
}
