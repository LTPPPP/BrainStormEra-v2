using Microsoft.AspNetCore.Mvc;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IResponseService
    {
        IActionResult HandleError(string message, string? redirectAction = null, string? controller = null);
        IActionResult HandleSuccess<T>(string viewPath, T model, string? successMessage = null);
        JsonResult HandleJsonError(string message);
        JsonResult HandleJsonSuccess(object data, string message);
    }
}







