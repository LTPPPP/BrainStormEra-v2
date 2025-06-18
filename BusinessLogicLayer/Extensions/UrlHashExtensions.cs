using BusinessLogicLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLogicLayer.Extensions
{
    public static class UrlHashExtensions
    {
        /// <summary>
        /// Encode ID to hash for use in URL
        /// </summary>
        /// <param name="urlHelper">URL Helper</param>
        /// <param name="realId">Real ID</param>
        /// <returns>Hash ID</returns>
        public static string EncodeId(this IUrlHelper urlHelper, string realId)
        {
            var urlHashService = urlHelper.ActionContext.HttpContext.RequestServices
                .GetService<IUrlHashService>();

            if (urlHashService == null || string.IsNullOrEmpty(realId))
                return realId;

            return urlHashService.GetHash(realId);
        }

        /// <summary>
        /// Create URL action with hash ID
        /// </summary>
        /// <param name="urlHelper">URL Helper</param>
        /// <param name="actionName">Action name</param>
        /// <param name="realId">Real ID</param>
        /// <returns>URL with hash ID</returns>
        public static string? ActionWithHash(this IUrlHelper urlHelper, string actionName, string realId)
        {
            var hashId = urlHelper.EncodeId(realId);
            return urlHelper.Action(actionName, new { id = hashId });
        }

        /// <summary>
        /// Create URL action with hash ID and controller
        /// </summary>
        /// <param name="urlHelper">URL Helper</param>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="realId">Real ID</param>
        /// <returns>URL with hash ID</returns>
        public static string? ActionWithHash(this IUrlHelper urlHelper, string actionName, string controllerName, string realId)
        {
            var hashId = urlHelper.EncodeId(realId);
            return urlHelper.Action(actionName, controllerName, new { id = hashId });
        }

        /// <summary>
        /// Create URL action with hash ID and route values
        /// </summary>
        /// <param name="urlHelper">URL Helper</param>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="realId">Real ID</param>
        /// <param name="routeValues">Other route values</param>
        /// <returns>URL with hash ID</returns>
        public static string? ActionWithHash(this IUrlHelper urlHelper, string actionName, string controllerName, string realId, object routeValues)
        {
            var hashId = urlHelper.EncodeId(realId);
            var routes = new Dictionary<string, object> { ["id"] = hashId };

            // Merge with other route values
            if (routeValues != null)
            {
                var properties = routeValues.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    routes[prop.Name] = prop.GetValue(routeValues) ?? "";
                }
            }

            return urlHelper.Action(actionName, controllerName, routes);
        }
    }

    public static class HtmlHashExtensions
    {
        /// <summary>
        /// Encode ID to hash from HtmlHelper
        /// </summary>
        /// <param name="htmlHelper">HTML Helper</param>
        /// <param name="realId">Real ID</param>
        /// <returns>Hash ID</returns>
        public static string EncodeId(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string realId)
        {
            var urlHashService = htmlHelper.ViewContext.HttpContext.RequestServices
                .GetService<IUrlHashService>();

            if (urlHashService == null || string.IsNullOrEmpty(realId))
                return realId;

            return urlHashService.GetHash(realId);
        }

        /// <summary>
        /// Decode hash ID to real ID from HtmlHelper
        /// </summary>
        /// <param name="htmlHelper">HTML Helper</param>
        /// <param name="hashId">Hash ID</param>
        /// <returns>Real ID</returns>
        public static string DecodeId(this Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper htmlHelper, string hashId)
        {
            var urlHashService = htmlHelper.ViewContext.HttpContext.RequestServices
                .GetService<IUrlHashService>();

            if (urlHashService == null || string.IsNullOrEmpty(hashId))
                return hashId;

            return urlHashService.GetRealId(hashId);
        }
    }
}