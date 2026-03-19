using Microsoft.AspNetCore.Http;
using System;

namespace ShopBackend.API.Middleware
{


    // Statt der Middleware hätte ich auch wie ganz am Anfang kurz angedacht auch Exceptions User-Spezifisch über Response-DTOs ausgeben können,
    // Allerdings wäre das einfach viel zu viel geworden (und würde die Services NOCH weiter aufblähen),
    // Daher jetzt knackig über einen Global-Exception-Handler:

    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        { 
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (KeyNotFoundException ex)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new {error = ex.Message});
            }
            catch (UnauthorizedAccessException ex)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
            catch (Exception) 
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { error = "Es ist ein wildes Relaxo aufgetreten." });
            }
        }
    }
}
