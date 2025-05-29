using Facturacion.API.Attributes;
using Facturacion.API.Domain.Contracts;
using Facturacion.API.Domain.Contracts.FacturacionRepository;
using Facturacion.API.Domain.Services;
using Facturacion.API.Domain.Services.FacturacionService;
using Facturacion.API.Extensions;
using Facturacion.API.Infrastructure;
using Facturacion.API.Util.Extensions;
using Facturacion.API.Util.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ILoggerFactory = Facturacion.API.Domain.Contracts.ILoggerFactory;
using LoggerFactory = Facturacion.API.Domain.Services.LoggerFactory;

var builder = WebApplication.CreateBuilder(args);

// ==============================================================================
// CONFIGURACIÓN DE SERVICIOS
// ==============================================================================

// Configuración de base de datos
builder.Services.AddDbContext<DBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuración de controladores
builder.Services.AddControllers();

// Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000", "https://localhost:3001", "http://localhost:5000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// Configuración de autenticación JWT
var jwtKey = builder.Configuration["JwtSettings:Key"] ?? throw new InvalidOperationException("JWT Key no configurada");
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"];
var jwtAudience = builder.Configuration["JwtSettings:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = !string.IsNullOrEmpty(jwtIssuer),
            ValidIssuer = jwtIssuer,
            ValidateAudience = !string.IsNullOrEmpty(jwtAudience),
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Configuración de Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCustomSwagger();

// ==============================================================================
// INYECCIÓN DE DEPENDENCIAS - SERVICIOS CORE
// ==============================================================================

// Servicios de logging
builder.Services.AddScoped<IFileLogger, FileLoggerService>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<ILoggerFactory, LoggerFactory>();

// Servicios de autenticación y acceso
builder.Services.AddScoped<ITokenRepository, TokenRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IRolRepository, RolRepository>();
builder.Services.AddScoped<IAccesoRepository, AccesoRepository>();

// ==============================================================================
// INYECCIÓN DE DEPENDENCIAS - SERVICIOS DE FACTURACIÓN
// ==============================================================================

// Servicios de clientes
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();

// Servicios de artículos
builder.Services.AddScoped<IArticuloRepository, ArticuloRepository>();
builder.Services.AddScoped<ICategoriaArticuloRepository, CategoriaArticuloRepository>();

// Servicios de facturación
builder.Services.AddScoped<IFacturaRepository, FacturaRepository>();
builder.Services.AddScoped<IFacturaDetalleRepository, FacturaDetalleRepository>();
builder.Services.AddScoped<ICalculoFacturacionRepository, CalculoFacturacionRepository>();
builder.Services.AddScoped<IValidacionNegocioRepository, ValidacionNegocioRepository>();

// Servicios de reportes
builder.Services.AddScoped<IReporteRepository, ReporteRepository>();

// ==============================================================================
// CONFIGURACIÓN DE ATRIBUTOS Y MIDDLEWARES
// ==============================================================================

// Registrar atributos como servicios
builder.Services.AddScoped<ExceptionAttribute>();
builder.Services.AddScoped<LogAttribute>();
builder.Services.AddScoped<AccesoAttribute>();

// Configuración de logging de aplicación
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configurar formato de fecha y cultura
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "es-CO", "es-ES" };
    options.SetDefaultCulture(supportedCultures[0])
           .AddSupportedCultures(supportedCultures)
           .AddSupportedUICultures(supportedCultures);
});

var app = builder.Build();

// ==============================================================================
// CONFIGURACIÓN DEL PIPELINE DE MIDDLEWARE
// ==============================================================================

// Configuración del entorno de desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseCustomSwagger();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Middlewares de seguridad y redirección
app.UseHttpsRedirection();

// Middleware de CORS
app.UseCustomCors();

// Middleware de localización
app.UseRequestLocalization();

// Middlewares de autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// Middlewares personalizados (logging y manejo de errores)
app.UseCustomMiddleware();

// Configuración de rutas
app.UseCustomEndpoints();

// ==============================================================================
// INICIALIZACIÓN DE LA BASE DE DATOS
// ==============================================================================

// Crear scope para servicios
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        // Obtener el contexto de base de datos
        var context = services.GetRequiredService<DBContext>();

        // Verificar la conexión a la base de datos
        logger.LogInformation("Verificando conexión a la base de datos...");

        if (context.Database.CanConnect())
        {
            logger.LogInformation("? Conexión a la base de datos exitosa");

            // Opcional: aplicar migraciones pendientes
            // context.Database.Migrate();
        }
        else
        {
            logger.LogError("? No se pudo conectar a la base de datos");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "? Error durante la inicialización de la base de datos");
    }
}

// ==============================================================================
// CONFIGURACIÓN DE URLs Y PUERTO
// ==============================================================================

// Configurar URLs si no están definidas
if (!app.Environment.IsDevelopment())
{
    app.Urls.Add("http://0.0.0.0:5000");
    app.Urls.Add("https://0.0.0.0:5001");
}

// Mensaje de inicio
app.Logger.LogInformation("?? Facturación API iniciándose...");
app.Logger.LogInformation("?? Ambiente: {Environment}", app.Environment.EnvironmentName);
app.Logger.LogInformation("?? URLs disponibles:");

foreach (var url in app.Urls)
{
    app.Logger.LogInformation("   - {Url}", url);
}

if (app.Environment.IsDevelopment())
{
    app.Logger.LogInformation("?? Swagger UI disponible en: /swagger");
}

app.Logger.LogInformation("? Facturación API iniciada correctamente");

// Ejecutar la aplicación
app.Run();