using BusinessLogicLayer.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace BusinessLogicLayer.Services
{
    public class ResponseService : IResponseService
    {
        public IActionResult HandleError(string message, string? redirectAction = null, string? controller = null)
        {
            var routeValues = new RouteValueDictionary { { "ErrorMessage", message } };
            return new RedirectToActionResult(redirectAction ?? "Index", controller ?? "Certificate", routeValues);
        }

        public IActionResult HandleSuccess<T>(string viewPath, T model, string? successMessage = null)
        {
            var result = new ViewResult { ViewName = viewPath };
            if (model != null)
            {
                result.ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(
                    new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
                    new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
                {
                    Model = model
                };
            }
            return result;
        }

        public JsonResult HandleJsonError(string message)
        {
            return new JsonResult(new { success = false, message });
        }

        public JsonResult HandleJsonSuccess(object data, string message)
        {
            return new JsonResult(new { success = true, data, message });
        }
    }
}







