using Facturacion.API.Attributes;

namespace Facturacion.API.Util.Extensions
{
    public static class AppExtensions
    {
        /// <summary>
        /// Configura el middleware personalizado para la aplicación
        /// </summary>
        public static IApplicationBuilder UseCustomMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseMiddleware<LoggingMiddleware>();

            return app;
        }

        /// <summary>
        /// Configura los endpoints predeterminados para la aplicación
        /// </summary>
        public static IApplicationBuilder UseCustomEndpoints(this IApplicationBuilder app)
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGet("/", async context =>
                {
                    context.Response.Redirect("/swagger");
                    await Task.CompletedTask;
                });
            });

            return app;
        }

        /// <summary>
        /// Configura las políticas CORS para la aplicación
        /// </summary>
        public static IApplicationBuilder UseCustomCors(this IApplicationBuilder app)
        {
            app.UseCors("AllowSpecificOrigins");
            return app;
        }
    }
}
