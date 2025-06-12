using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLogicLayer.Filters
{
    public class CacheResponseAttribute : ActionFilterAttribute
    {
        private readonly int _durationInMinutes;
        private readonly string _cacheKeyPrefix;

        public CacheResponseAttribute(int durationInMinutes = 5, string cacheKeyPrefix = "")
        {
            _durationInMinutes = durationInMinutes;
            _cacheKeyPrefix = cacheKeyPrefix;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
            var request = context.HttpContext.Request;

            var cacheKey = GenerateCacheKey(context, _cacheKeyPrefix);

            if (cache.TryGetValue(cacheKey, out var cachedResult))
            {
                context.Result = (IActionResult)cachedResult!;
                return;
            }

            var executedContext = await next();

            if (executedContext.Result is ViewResult viewResult && executedContext.Exception == null)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_durationInMinutes),
                    Priority = CacheItemPriority.Normal
                };

                cache.Set(cacheKey, viewResult, cacheOptions);
            }
        }

        private static string GenerateCacheKey(ActionExecutingContext context, string prefix)
        {
            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();
            var userId = context.HttpContext.User.FindFirst("UserId")?.Value ?? "anonymous";

            var parameters = string.Join("_", context.ActionArguments.Values.Select(v => v?.ToString() ?? "null"));

            return $"{prefix}_{controller}_{action}_{userId}_{parameters}";
        }
    }
}






