namespace Restaurant.API.Custom_Middleware
{
    public class ConcurrentRequestsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SemaphoreSlim _semaphore;
        private readonly ILogger<ConcurrentRequestsMiddleware> _logger;

   
        public ConcurrentRequestsMiddleware(
            RequestDelegate next,
            ILogger<ConcurrentRequestsMiddleware> logger,
            int maxConcurrentRequests = 50)
        {
            _next = next;
            _logger = logger;
            _semaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
        }

        public async Task InvokeAsync(HttpContext context)
        {
          
            if (!await _semaphore.WaitAsync(TimeSpan.Zero))
            {
                _logger.LogWarning("Concurrent request limit reached for {Path}",
                    context.Request.Path);

                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = "5";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Too many concurrent requests. Please try again shortly.",
                    retryAfterSeconds = 5
                });
                return;
            }

            try
            {
                await _next(context);
            }
            finally
            {
    
                _semaphore.Release();
            }
        }
    }
}
