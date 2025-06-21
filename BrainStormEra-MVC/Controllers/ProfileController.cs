using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class ProfileController : BaseController
    {
        private readonly AuthServiceImpl _authServiceImpl;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            AuthServiceImpl authServiceImpl,
            ILogger<ProfileController> logger)
        {
            _authServiceImpl = authServiceImpl;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "You must be logged in to view your profile.";
                    return RedirectToAction("Index", "Login");
                }

                var result = await _authServiceImpl.GetProfileViewModelAsync(userId, CurrentUserRole ?? "");

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Home");
                }

                return View("~/Views/Profile/Index.cshtml", result.ViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile for user: {UserId}", CurrentUserId);
                TempData["ErrorMessage"] = "An error occurred while loading your profile.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "You must be logged in to edit your profile.";
                    return RedirectToAction("Index", "Login");
                }

                var result = await _authServiceImpl.GetEditProfileViewModelAsync(userId);

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index");
                }

                return View("~/Views/Profile/Edit.cshtml", result.ViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit profile for user: {UserId}", CurrentUserId);
                TempData["ErrorMessage"] = "An error occurred while loading the edit profile page.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Profile/Edit.cshtml", model);
            }

            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "You must be logged in to edit your profile.";
                    return RedirectToAction("Index", "Login");
                }

                var result = await _authServiceImpl.UpdateProfileAsync(userId, model);

                if (!result.Success)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return View("~/Views/Profile/Edit.cshtml", result.ViewModel);
                }

                TempData["SuccessMessage"] = result.SuccessMessage;
                if (!string.IsNullOrEmpty(result.WarningMessage))
                {
                    TempData["WarningMessage"] = result.WarningMessage;
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user: {UserId}", CurrentUserId);
                TempData["ErrorMessage"] = "An error occurred while updating your profile.";
                return View("~/Views/Profile/Edit.cshtml", model);
            }
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View("~/Views/Profile/ChangePassword.cshtml", new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Profile/ChangePassword.cshtml", model);
            }

            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "You must be logged in to change your password.";
                    return RedirectToAction("Index", "Login");
                }

                var result = await _authServiceImpl.ChangePasswordAsync(userId, model);

                if (!result.Success)
                {
                    if (!string.IsNullOrEmpty(result.ValidationError) && !string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        ModelState.AddModelError(result.ValidationError, result.ErrorMessage);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = result.ErrorMessage;
                    }
                    return View("~/Views/Profile/ChangePassword.cshtml", result.ViewModel);
                }

                TempData["SuccessMessage"] = result.SuccessMessage;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", CurrentUserId);
                TempData["ErrorMessage"] = "An error occurred while changing your password.";
                return View("~/Views/Profile/ChangePassword.cshtml", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvatar(string? userId = null)
        {
            try
            {
                var targetUserId = userId ?? CurrentUserId;
                var result = await _authServiceImpl.GetUserAvatarAsync(targetUserId);

                return File(result.ImageBytes, result.ContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving avatar for user: {UserId}", userId);
                var defaultResult = await _authServiceImpl.GetUserAvatarAsync(null);
                return File(defaultResult.ImageBytes, defaultResult.ContentType);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvatar()
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var result = await _authServiceImpl.DeleteUserAvatarAsync(userId);

                return Json(new { success = result.Success, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting avatar for user: {UserId}", CurrentUserId);
                return Json(new { success = false, message = "An error occurred while deleting avatar" });
            }
        }

        [HttpGet]
        public IActionResult VietQRGenerator()
        {
            var model = new VietQRViewModel();

            // Pre-fill with user's bank info if available
            var userId = CurrentUserId;
            if (!string.IsNullOrEmpty(userId))
            {
                var userResult = _authServiceImpl.GetProfileViewModelAsync(userId, CurrentUserRole ?? "").Result;
                if (userResult.Success && userResult.ViewModel != null)
                {
                    model.AccountNumber = userResult.ViewModel.BankAccountNumber ?? "";
                    model.AccountName = userResult.ViewModel.AccountHolderName ?? userResult.ViewModel.FullName;
                }
            }

            return PartialView("_VietQRGenerator", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GenerateVietQR(VietQRViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_VietQRGenerator", model);
            }

            try
            {
                // Build VietQR URL
                var baseUrl = "https://img.vietqr.io/image";
                var url = $"{baseUrl}/{model.BankId}-{model.AccountNumber}-{model.Template}.png";

                var queryParams = new List<string>();

                if (model.Amount.HasValue && model.Amount > 0)
                {
                    queryParams.Add($"amount={model.Amount}");
                }

                if (!string.IsNullOrEmpty(model.Description))
                {
                    queryParams.Add($"addInfo={Uri.EscapeDataString(model.Description)}");
                }

                if (!string.IsNullOrEmpty(model.AccountName))
                {
                    queryParams.Add($"accountName={Uri.EscapeDataString(model.AccountName)}");
                }

                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                model.GeneratedQRUrl = url;

                return PartialView("_VietQRGenerator", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating VietQR for user: {UserId}", CurrentUserId);
                ModelState.AddModelError("", "An error occurred while generating the QR code.");
                return PartialView("_VietQRGenerator", model);
            }
        }
    }
}

