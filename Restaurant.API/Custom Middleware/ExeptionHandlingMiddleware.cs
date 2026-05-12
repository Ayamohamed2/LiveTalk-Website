using System.Net;
using System.Text.Json;

namespace Villa_API_Project.Custom_Middleware
{
    public class ExeptionHandlingMiddleware
    {
        private readonly RequestDelegate next;
        public ExeptionHandlingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context); 
            }
            catch (Exception ex)
            {
              
                await ExeptionHandlingAsync(context, ex);
            }
        }

        private static Task ExeptionHandlingAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                success = false,
                message = exception.Message,
                details = exception.InnerException?.Message,
                statusCode = statusCode
            };

            context.Response.StatusCode = statusCode;

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    
}
}
