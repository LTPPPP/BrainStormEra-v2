using BusinessLogicLayer.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLogicLayer.Extensions
{
    /// <summary>
    /// Extension methods for route handling with hashed IDs
    /// </summary>
    public static class RouteHashExtensions
    {
        /// <summary>
        /// Get real ID from route data if it contains a hash
        /// </summary>
        /// <param name="routeData">Route data</param>
        /// <param name="httpContext">HTTP context for service access</param>
        /// <param name="key">Route key (default: "id")</param>
        /// <returns>Real ID or original value</returns>
        public static string GetRealIdFromRoute(this RouteData routeData, HttpContext httpContext, string key = "id")
        {
            var hashOrId = routeData.Values[key]?.ToString();
            if (string.IsNullOrEmpty(hashOrId))
                return string.Empty;

            var urlHashService = httpContext.RequestServices.GetService<IUrlHashService>();
            return urlHashService?.GetRealId(hashOrId) ?? hashOrId;
        }

        /// <summary>
        /// Get hash ID from route data if it contains a real ID
        /// </summary>
        /// <param name="routeData">Route data</param>
        /// <param name="httpContext">HTTP context for service access</param>
        /// <param name="key">Route key (default: "id")</param>
        /// <returns>Hash ID or original value</returns>
        public static string GetHashFromRoute(this RouteData routeData, HttpContext httpContext, string key = "id")
        {
            var idOrHash = routeData.Values[key]?.ToString();
            if (string.IsNullOrEmpty(idOrHash))
                return string.Empty;

            var urlHashService = httpContext.RequestServices.GetService<IUrlHashService>();
            return urlHashService?.GetHash(idOrHash) ?? idOrHash;
        }

        /// <summary>
        /// Check if route contains a hashed ID
        /// </summary>
        /// <param name="routeData">Route data</param>
        /// <param name="httpContext">HTTP context for service access</param>
        /// <param name="key">Route key (default: "id")</param>
        /// <returns>True if the route value is a hash</returns>
        public static bool RouteContainsHash(this RouteData routeData, HttpContext httpContext, string key = "id")
        {
            var value = routeData.Values[key]?.ToString();
            if (string.IsNullOrEmpty(value))
                return false;

            var urlHashService = httpContext.RequestServices.GetService<IUrlHashService>();
            return urlHashService?.IsHash(value) ?? false;
        }
    }

    /// <summary>
    /// Extension methods for HttpContext with hash handling
    /// </summary>
    public static class HttpContextHashExtensions
    {
        /// <summary>
        /// Get URL hash service from HttpContext
        /// </summary>
        /// <param name="httpContext">HTTP context</param>
        /// <returns>URL hash service instance</returns>
        public static IUrlHashService? GetUrlHashService(this HttpContext httpContext)
        {
            return httpContext.RequestServices.GetService<IUrlHashService>();
        }

        /// <summary>
        /// Encode ID to hash using HttpContext services
        /// </summary>
        /// <param name="httpContext">HTTP context</param>
        /// <param name="realId">Real ID to encode</param>
        /// <returns>Hashed ID</returns>
        public static string EncodeId(this HttpContext httpContext, string realId)
        {
            var service = httpContext.GetUrlHashService();
            return service?.GetHash(realId) ?? realId;
        }

        /// <summary>
        /// Decode hash to real ID using HttpContext services
        /// </summary>
        /// <param name="httpContext">HTTP context</param>
        /// <param name="hashId">Hash ID to decode</param>
        /// <returns>Real ID</returns>
        public static string DecodeId(this HttpContext httpContext, string hashId)
        {
            var service = httpContext.GetUrlHashService();
            return service?.GetRealId(hashId) ?? hashId;
        }
    }

    /// <summary>
    /// Extension methods for working with collections of IDs
    /// </summary>
    public static class CollectionHashExtensions
    {
        /// <summary>
        /// Encode a collection of real IDs to hashes
        /// </summary>
        /// <param name="realIds">Collection of real IDs</param>
        /// <param name="urlHashService">URL hash service</param>
        /// <returns>Dictionary mapping real ID to hash</returns>
        public static Dictionary<string, string> ToHashDictionary(this IEnumerable<string> realIds, IUrlHashService urlHashService)
        {
            return urlHashService.EncodeIds(realIds);
        }

        /// <summary>
        /// Decode a collection of hashes to real IDs
        /// </summary>
        /// <param name="hashes">Collection of hashes</param>
        /// <param name="urlHashService">URL hash service</param>
        /// <returns>Dictionary mapping hash to real ID</returns>
        public static Dictionary<string, string> ToRealIdDictionary(this IEnumerable<string> hashes, IUrlHashService urlHashService)
        {
            return urlHashService.DecodeIds(hashes);
        }

        /// <summary>
        /// Convert collection of real IDs to hashed IDs
        /// </summary>
        /// <param name="realIds">Collection of real IDs</param>
        /// <param name="urlHashService">URL hash service</param>
        /// <returns>Collection of hashed IDs</returns>
        public static IEnumerable<string> ToHashes(this IEnumerable<string> realIds, IUrlHashService urlHashService)
        {
            return realIds.Select(id => urlHashService.GetHash(id));
        }

        /// <summary>
        /// Convert collection of hashes to real IDs
        /// </summary>
        /// <param name="hashes">Collection of hashes</param>
        /// <param name="urlHashService">URL hash service</param>
        /// <returns>Collection of real IDs</returns>
        public static IEnumerable<string> ToRealIds(this IEnumerable<string> hashes, IUrlHashService urlHashService)
        {
            return hashes.Select(hash => urlHashService.GetRealId(hash));
        }
    }
}