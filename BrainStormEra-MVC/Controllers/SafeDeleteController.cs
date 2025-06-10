using BrainStormEra_MVC.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BrainStormEra_MVC.Controllers
{
    /// <summary>
    /// Controller for managing safe delete operations and deleted items
    /// </summary>
    [Authorize]
    public class SafeDeleteController : BaseController
    {
        private readonly SafeDeleteServiceImpl _safeDeleteServiceImpl;

        public SafeDeleteController(SafeDeleteServiceImpl safeDeleteServiceImpl)
        {
            _safeDeleteServiceImpl = safeDeleteServiceImpl;
        }        /// <summary>
                 /// Validate if an entity can be safely deleted
                 /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateDelete([FromBody] SafeDeleteServiceImpl.ValidateDeleteRequest request)
        {
            var result = await _safeDeleteServiceImpl.HandleValidateDeleteAsync(request);
            return Json(result.JsonResponse);
        }

        /// <summary>
        /// Perform soft delete operation
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDelete([FromBody] SafeDeleteServiceImpl.DeleteRequest request)
        {
            var result = await _safeDeleteServiceImpl.HandleSoftDeleteAsync(request);
            return Json(result.JsonResponse);
        }        /// <summary>
                 /// Perform hard delete operation (admin only for most entities)
                 /// </summary>
        [HttpPost]
        [Authorize(Roles = "admin,instructor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HardDelete([FromBody] SafeDeleteServiceImpl.DeleteRequest request)
        {
            var result = await _safeDeleteServiceImpl.HandleHardDeleteAsync(request);
            return Json(result.JsonResponse);
        }

        /// <summary>
        /// Restore a soft-deleted entity
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore([FromBody] SafeDeleteServiceImpl.RestoreRequest request)
        {
            var result = await _safeDeleteServiceImpl.HandleRestoreAsync(request);
            return Json(result.JsonResponse);
        }

        /// <summary>
        /// Get deleted entities for management interface
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDeletedEntities(string entityType)
        {
            var result = await _safeDeleteServiceImpl.HandleGetDeletedEntitiesAsync(entityType);
            return Json(result.JsonResponse);
        }        /// <summary>
                 /// Admin interface for managing deleted items
                 /// </summary>
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AdminDeletedItems(string search = "", string entityType = "All", int page = 1, int pageSize = 10)
        {
            var result = await _safeDeleteServiceImpl.HandleAdminDeletedItemsAsync(User, search, entityType, page, pageSize);

            if (!result.IsSuccess)
            {
                if (!string.IsNullOrEmpty(result.TempDataErrorMessage))
                {
                    TempData["ErrorMessage"] = result.TempDataErrorMessage;
                }

                if (result.ShouldRedirect)
                {
                    return RedirectToAction(result.RedirectAction, result.RedirectController);
                }
            }

            return View(result.ViewModel);
        }
    }
}
