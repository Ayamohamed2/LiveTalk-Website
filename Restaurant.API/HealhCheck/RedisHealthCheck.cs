using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Restaurant.API.HealhCheck
{
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisHealthCheck> _logger;

        public RedisHealthCheck(
            IConnectionMultiplexer redis,
            ILogger<RedisHealthCheck> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var db = _redis.GetDatabase();
                var pingResult = await db.PingAsync();

                var data = new Dictionary<string, object>
            {
                { "latency_ms", pingResult.TotalMilliseconds },
                { "connected_clients", _redis.GetEndPoints().Length }
            };

            
                if (pingResult.TotalMilliseconds > 100)
                    return HealthCheckResult.Degraded(
                        $"Redis slow: {pingResult.TotalMilliseconds}ms ⚠️",
                        data: data);

                return HealthCheckResult.Healthy(
                    $"Redis connected — {pingResult.TotalMilliseconds}ms ✅",
                    data: data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis health check failed");
                return HealthCheckResult.Unhealthy("Redis error ❌", ex);
            }
        }
    }
}
