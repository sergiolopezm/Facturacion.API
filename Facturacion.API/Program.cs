﻿using Facturacion.API.Attributes;
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
// CONFIGURACIÓN DE SERVICIOS
// ==============================================================================

// Configuración de Entity Framework
builder.Services.AddDbContext<DBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuración de controllers
builder.Services.AddControllers();

// Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3001") // Ajustar según necesidad
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configuración de JWT
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

// Configuración de Swagger
builder.Services.AddCustomSwagger();

// Registrar el sistema de logging
builder.Services.AddScoped<IFileLogger, FileLoggerService>();
builder.Services.AddScoped<Facturacion.API.Domain.Contracts.ILoggerFactory, Facturacion.API.Domain.Services.LoggerFactory>();

// Registrar filtros y atributos
builder.Services.AddScoped<AccesoAttribute>();
builder.Services.AddScoped<ExceptionAttribute>();
builder.Services.AddScoped<LogAttribute>();
builder.Services.AddScoped<ValidarModeloAttribute>();
builder.Services.AddScoped<JwtAuthorizationAttribute>();

// Registrar repositorios
builder.Services.AddScoped<IAccesoRepository, AccesoRepository>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<IRolRepository, RolRepository>();
builder.Services.AddScoped<ITokenRepository, TokenRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

// Repositorios de facturación
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<ICategoriaArticuloRepository, CategoriaArticuloRepository>();
builder.Services.AddScoped<IArticuloRepository, ArticuloRepository>();
builder.Services.AddScoped<IFacturaRepository, FacturaRepository>();
builder.Services.AddScoped<IFacturaDetalleRepository, FacturaDetalleRepository>();
builder.Services.AddScoped<IReporteRepository, ReporteRepository>();
builder.Services.AddScoped<ICalculoFacturacionRepository, CalculoFacturacionRepository>();
builder.Services.AddScoped<IValidacionNegocioRepository, ValidacionNegocioRepository>();

// Configurar filtros globales
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ExceptionAttribute>();
})
.ConfigureApiBehaviorOptions(options =>
{
    options.SuppressModelStateInvalidFilter = true; // Usar nuestras validaciones personalizadas
});

// Configuración de Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<DBContext>();

// Configuración para JSON
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
});

// Configuración de Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

// ==============================================================================
// BUILD DE LA APLICACIÓN
// ==============================================================================

var app = builder.Build();

// ==============================================================================
// CONFIGURACIÓN DEL PIPELINE DE MIDDLEWARE
// ==============================================================================

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseCustomSwagger();
}
else
{
    // Usar middleware personalizado para errores y logging cuando NO es desarrollo
    app.Use(async (context, next) =>
    {
        var errorHandler = new ErrorHandlingMiddleware(
            next,
            context.RequestServices.GetRequiredService<ILogger<ErrorHandlingMiddleware>>(),
            context.RequestServices.GetRequiredService<Facturacion.API.Domain.Contracts.ILoggerFactory>()
        );
        await errorHandler.Invoke(context);
    });
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCustomCors();

app.UseRouting();

// Middleware de logging
app.Use(async (context, next) =>
{
    var loggingHandler = new LoggingMiddleware(
        next,
        context.RequestServices.GetRequiredService<ILogger<LoggingMiddleware>>(),
        context.RequestServices.GetRequiredService<Facturacion.API.Domain.Contracts.ILoggerFactory>()
    );
    await loggingHandler.Invoke(context);
});

// Autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// Usar endpoints
app.UseCustomEndpoints();

// ==============================================================================
// INICIALIZACIÓN DE BASE DE DATOS
// ==============================================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DBContext>();

        // Verificar conexión a la base de datos
        if (context.Database.CanConnect())
        {
            app.Logger.LogInformation("✅ Conexión a base de datos establecida correctamente");

            // Opcionalmente aplicar migraciones pendientes
            // context.Database.Migrate();
        }
        else
        {
            app.Logger.LogError("❌ No se pudo conectar a la base de datos");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "❌ Error al verificar la conexión a la base de datos");
    }
}

// ==============================================================================
// INICIAR LA APLICACIÓN
// ==============================================================================

app.Logger.LogInformation("🚀 Aplicación Facturación API iniciada");
app.Logger.LogInformation("🌍 Entorno: {Environment}", app.Environment.EnvironmentName);
app.Logger.LogInformation("📝 Swagger disponible en: /swagger");
app.Logger.LogInformation("💚 Health check disponible en: /health");

try
{
    app.Run();
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "💥 La aplicación terminó inesperadamente");
    throw;
}