using Microsoft.AspNetCore.Authorization;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;

namespace Villa_API_Project.Custom_Middleware
{
    public class BlackListTokensMiddleware
    {
        private readonly RequestDelegate next;
       

        public BlackListTokensMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
        {
            var endpoint = context.GetEndpoint();

            var isAuthentecated = endpoint?.Metadata.GetMetadata<AuthorizeAttribute>() != null;

            if (!isAuthentecated)
            {
                await next(context);
                return;
            }
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                using (var scope = scopeFactory.CreateScope())
                {
                    var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    var isRevoked = await unit.RevokedTokens.IsTokenRevokedAsync(token);
                    if (isRevoked)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("This token has been revoked.");
                        return;
                    }
                }
            }
            await next(context);
        }
    }
}
