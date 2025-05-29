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
// CONFIGURACI�N DE SERVICIOS
// ==============================================================================

// Configuraci�n de base de datos
builder.Services.AddDbContext<DBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuraci�n de controladores
builder.Services.AddControllers();

// Configuraci�n de CORS
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

// Configuraci�n de autenticaci�n JWT
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

// Configuraci�n de Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCustomSwagger();

// ==============================================================================
// INYECCI�N DE DEPENDENCIAS - SERVICIOS CORE
// ==============================================================================

// Servicios de logging
builder.Services.AddScoped<IFileLogger, FileLoggerService>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<ILoggerFactory, LoggerFactory>();

// Servicios de autenticaci�n y acceso
builder.Services.AddScoped<ITokenRepository, TokenRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IRolRepository, RolRepository>();
builder.Services.AddScoped<IAccesoRepository, AccesoRepository>();

// ==============================================================================
// INYECCI�N DE DEPENDENCIAS - SERVICIOS DE FACTURACI�N
// ==============================================================================

// Servicios de clientes
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();

// Servicios de art�culos
builder.Services.AddScoped<IArticuloRepository, ArticuloRepository>();
builder.Services.AddScoped<ICategoriaArticuloRepository, CategoriaArticuloRepository>();

// Servicios de facturaci�n
builder.Services.AddScoped<IFacturaRepository, FacturaRepository>();
builder.Services.AddScoped<IFacturaDetalleRepository, FacturaDetalleRepository>();
builder.Services.AddScoped<ICalculoFacturacionRepository, CalculoFacturacionRepository>();
builder.Services.AddScoped<IValidacionNegocioRepository, ValidacionNegocioRepository>();

// Servicios de reportes
builder.Services.AddScoped<IReporteRepository, ReporteRepository>();

// ==============================================================================
// CONFIGURACI�N DE ATRIBUTOS Y MIDDLEWARES
// ==============================================================================

// Registrar atributos como servicios
builder.Services.AddScoped<ExceptionAttribute>();
builder.Services.AddScoped<LogAttribute>();
builder.Services.AddScoped<AccesoAttribute>();

// Configuraci�n de logging de aplicaci�n
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
// CONFIGURACI�N DEL PIPELINE DE MIDDLEWARE
// ==============================================================================

// Configuraci�n del entorno de desarrollo
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

// Middlewares de seguridad y redirecci�n
app.UseHttpsRedirection();

// Middleware de CORS
app.UseCustomCors();

// Middleware de localizaci�n
app.UseRequestLocalization();

// Middlewares de autenticaci�n y autorizaci�n
app.UseAuthentication();
app.UseAuthorization();

// Middlewares personalizados (logging y manejo de errores)
app.UseCustomMiddleware();

// Configuraci�n de rutas
app.UseCustomEndpoints();

// ==============================================================================
// INICIALIZACI�N DE LA BASE DE DATOS
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

        // Verificar la conexi�n a la base de datos
        logger.LogInformation("Verificando conexi�n a la base de datos...");

        if (context.Database.CanConnect())
        {
            logger.LogInformation("? Conexi�n a la base de datos exitosa");

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
        logger.LogError(ex, "? Error durante la inicializaci�n de la base de datos");
    }
}

// ==============================================================================
// CONFIGURACI�N DE URLs Y PUERTO
// ==============================================================================

// Configurar URLs si no est�n definidas
if (!app.Environment.IsDevelopment())
{
    app.Urls.Add("http://0.0.0.0:5000");
    app.Urls.Add("https://0.0.0.0:5001");
}

// Mensaje de inicio
app.Logger.LogInformation("?? Facturaci�n API inici�ndose...");
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

app.Logger.LogInformation("? Facturaci�n API iniciada correctamente");

// Ejecutar la aplicaci�n
app.Run();