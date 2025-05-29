using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Facturacion.API.Extensions
{
    public static class SwaggerExtensions
    {
        /// <summary>
        /// Configura Swagger para la aplicación
        /// </summary>
        public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Facturacion API",
                    Version = "v1",
                    Description = "API para gestión de Facturas",
                    Contact = new OpenApiContact
                    {
                        Name = "Facturacion",
                        Email = "soporte@Facturacion.com",
                        Url = new Uri("https://Facturacion.com")
                    }
                });

                // Configurar JWT para Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });

                // Comentarios XML para documentación de API
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            return services;
        }

        /// <summary>
        /// Configura el middleware de Swagger para la aplicación
        /// </summary>
        public static IApplicationBuilder UseCustomSwagger(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Facturacion API v1");
                c.RoutePrefix = "swagger";
                c.DocumentTitle = "Facturacion API";
                c.DefaultModelsExpandDepth(-1); // Ocultar esquemas
            });

            return app;
        }
    }
}