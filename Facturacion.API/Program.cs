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

var builder = WebApplication.CreateBuilder(args);

// ==============================================================================
// CONFIGURACI�N DE SERVICIOS
// ==============================================================================

// Configuraci�n de Entity Framework
builder.Services.AddDbContext<DBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuraci�n de controllers
builder.Services.AddControllers();

// Configuraci�n de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3001") // Ajustar seg�n necesidad
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configuraci�n de JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key no configurada"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Solo para desarrollo
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = !string.IsNullOrEmpty(jwtSettings["Issuer"]),
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = !string.IsNullOrEmpty(jwtSettings["Audience"]),
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Configuraci�n de Swagger
builder.Services.AddCustomSwagger();

// ==============================================================================
// INYECCI�N DE DEPENDENCIAS - REPOSITORIOS
// ==============================================================================

// Repositorios base
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IRolRepository, RolRepository>();
builder.Services.AddScoped<ITokenRepository, TokenRepository>();
builder.Services.AddScoped<IAccesoRepository, AccesoRepository>();

// Repositorios de facturaci�n
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<ICategoriaArticuloRepository, CategoriaArticuloRepository>();
builder.Services.AddScoped<IArticuloRepository, ArticuloRepository>();
builder.Services.AddScoped<IFacturaRepository, FacturaRepository>();
builder.Services.AddScoped<IFacturaDetalleRepository, FacturaDetalleRepository>();
builder.Services.AddScoped<IReporteRepository, ReporteRepository>();
builder.Services.AddScoped<ICalculoFacturacionRepository, CalculoFacturacionRepository>();
builder.Services.AddScoped<IValidacionNegocioRepository, ValidacionNegocioRepository>();

// ==============================================================================
// INYECCI�N DE DEPENDENCIAS - SERVICIOS
// ==============================================================================

// Servicios de logging
builder.Services.AddScoped<IFileLogger, FileLoggerService>();
builder.Services.AddScoped<Facturacion.API.Domain.Contracts.ILoggerFactory, Facturacion.API.Domain.Services.LoggerFactory>();

// ==============================================================================
// INYECCI�N DE DEPENDENCIAS - ATTRIBUTES
// ==============================================================================

builder.Services.AddScoped<LogAttribute>();
builder.Services.AddScoped<ExceptionAttribute>();
builder.Services.AddScoped<ValidarModeloAttribute>();
builder.Services.AddScoped<AccesoAttribute>();

// ==============================================================================
// CONFIGURACI�N ADICIONAL
// ==============================================================================

// Configuraci�n para JSON
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null; // Mantener nombres originales
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = true; // Usar nuestras validaciones personalizadas
    });

// Configuraci�n de Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DBContext>();

// ==============================================================================
// CONFIGURACI�N DE LOGGING
// ==============================================================================

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configurar niveles de log seg�n el entorno
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

// ==============================================================================
// BUILD DE LA APLICACI�N
// ==============================================================================

var app = builder.Build();

// ==============================================================================
// CONFIGURACI�N DEL PIPELINE DE MIDDLEWARE
// ==============================================================================

// Middleware de manejo de errores (debe ir primero)
app.UseCustomMiddleware();

// Configuraci�n para Development
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

// Middleware de seguridad
app.UseHttpsRedirection();

// CORS (debe ir antes de Authentication y Authorization)
app.UseCustomCors();

// Autenticaci�n y autorizaci�n
app.UseAuthentication();
app.UseAuthorization();

// Health checks
app.UseHealthChecks("/health");

// Configuraci�n de endpoints
app.UseCustomEndpoints();

// ==============================================================================
// INICIALIZACI�N DE BASE DE DATOS
// ==============================================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DBContext>();

        // Verificar conexi�n a la base de datos
        if (context.Database.CanConnect())
        {
            app.Logger.LogInformation("? Conexi�n a base de datos establecida correctamente");

            // Aplicar migraciones pendientes (opcional)
            // context.Database.Migrate();
        }
        else
        {
            app.Logger.LogError("? No se pudo conectar a la base de datos");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "? Error al verificar la conexi�n a la base de datos");
    }
}

// ==============================================================================
// CONFIGURACI�N DE RUTAS PREDETERMINADAS
// ==============================================================================

// Redireccionar la ra�z a Swagger en desarrollo
if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

// ==============================================================================
// INFORMACI�N DE INICIO
// ==============================================================================

app.Logger.LogInformation("?? Aplicaci�n Facturaci�n API iniciada");
app.Logger.LogInformation("?? Entorno: {Environment}", app.Environment.EnvironmentName);
app.Logger.LogInformation("?? Swagger disponible en: /swagger");
app.Logger.LogInformation("?? Health check disponible en: /health");

// ==============================================================================
// EJECUTAR LA APLICACI�N
// ==============================================================================

try
{
    app.Run();
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "?? La aplicaci�n termin� inesperadamente");
    throw;
}