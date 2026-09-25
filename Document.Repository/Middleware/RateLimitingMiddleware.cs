using System.Collections.Concurrent;

namespace Document.Repository.Middleware
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly ConcurrentDictionary<string, (DateTime, int)> _requestCounts = new();
        private const int MaxRequests = 100; // Max requests per time window
        private static readonly TimeSpan TimeWindow = TimeSpan.FromMinutes(1);

        public RateLimitingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            // Clean old entries periodically
            CleanOldEntries();

            if (_requestCounts.TryGetValue(ipAddress, out var requestData))
            {
                var (firstRequestTime, count) = requestData;

                if (DateTime.UtcNow - firstRequestTime < TimeWindow)
                {
                    if (count >= MaxRequests)
                    {
                        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                        await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                        return;
                    }

                    _requestCounts[ipAddress] = (firstRequestTime, count + 1);
                }
                else
                {
                    _requestCounts[ipAddress] = (DateTime.UtcNow, 1);
                }
            }
            else
            {
                _requestCounts[ipAddress] = (DateTime.UtcNow, 1);
            }

            await _next(context);
        }

        private static void CleanOldEntries()
        {
            var keysToRemove = _requestCounts
                .Where(kvp => DateTime.UtcNow - kvp.Value.Item1 > TimeWindow)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _requestCounts.TryRemove(key, out _);
            }
        }
    }
}
