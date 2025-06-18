using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BrainStormEra_MVC.Filters;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class QuizController : BaseController
    {
        private readonly QuizServiceImpl _quizServiceImpl;

        public QuizController(QuizServiceImpl quizServiceImpl, IUrlHashService urlHashService)
            : base(urlHashService)
        {
            _quizServiceImpl = quizServiceImpl;
        }

        // GET: Quiz/Create
        public async Task<IActionResult> Create(string chapterId)
        {
            // Decode hash ID to real ID
            var realChapterId = DecodeHashId(chapterId);
            var result = await _quizServiceImpl.GetCreateQuizAsync(User, realChapterId);

            if (!result.Success)
            {
                if (result.IsNotFound)
                {
                    return NotFound();
                }
                if (result.IsForbidden)
                {
                    return Forbid();
                }
                if (!string.IsNullOrEmpty(result.RedirectAction) && !string.IsNullOrEmpty(result.RedirectController))
                {
                    return RedirectToAction(result.RedirectAction, result.RedirectController);
                }
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Home");
                }
            }

            return View(result.ViewModel);
        }

        // POST: Quiz/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateQuizViewModel model)
        {
            var result = await _quizServiceImpl.CreateQuizAsync(User, model, ModelState);

            if (!result.Success)
            {
                if (result.IsNotFound)
                {
                    return NotFound();
                }
                if (result.IsForbidden)
                {
                    return Forbid();
                }
                if (!string.IsNullOrEmpty(result.RedirectAction) && !string.IsNullOrEmpty(result.RedirectController))
                {
                    return RedirectToAction(result.RedirectAction, result.RedirectController);
                }
                if (result.ReturnView)
                {
                    return View(result.ViewModel);
                }
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Home");
                }
            }

            if (!string.IsNullOrEmpty(result.SuccessMessage))
            {
                TempData["SuccessMessage"] = result.SuccessMessage;
            }

            return RedirectToActionWithHash(result.RedirectAction, result.QuizId);
        }

        // GET: Quiz/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            // Decode hash ID to real ID
            var realId = DecodeHashId(id);
            var result = await _quizServiceImpl.GetEditQuizAsync(User, realId);

            if (!result.Success)
            {
                if (result.IsNotFound)
                {
                    return NotFound();
                }
                if (result.IsForbidden)
                {
                    return Forbid();
                }
                if (!string.IsNullOrEmpty(result.RedirectAction) && !string.IsNullOrEmpty(result.RedirectController))
                {
                    return RedirectToAction(result.RedirectAction, result.RedirectController);
                }
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Home");
                }
            }

            return View("Create", result.ViewModel);
        }

        // POST: Quiz/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CreateQuizViewModel model)
        {
            var result = await _quizServiceImpl.UpdateQuizAsync(User, model, ModelState);

            if (!result.Success)
            {
                if (result.IsNotFound)
                {
                    return NotFound();
                }
                if (result.IsForbidden)
                {
                    return Forbid();
                }
                if (!string.IsNullOrEmpty(result.RedirectAction) && !string.IsNullOrEmpty(result.RedirectController))
                {
                    return RedirectToAction(result.RedirectAction, result.RedirectController);
                }
                if (result.ReturnView)
                {
                    return View("Create", result.ViewModel);
                }
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Home");
                }
            }

            if (!string.IsNullOrEmpty(result.SuccessMessage))
            {
                TempData["SuccessMessage"] = result.SuccessMessage;
            }

            return RedirectToActionWithHash(result.RedirectAction, result.QuizId);
        }

        // POST: Quiz/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            // Decode hash ID to real ID
            var realId = DecodeHashId(id);
            var result = await _quizServiceImpl.DeleteQuizAsync(User, realId);

            if (!result.Success)
            {
                if (result.IsNotFound)
                {
                    return NotFound();
                }
                if (result.IsForbidden)
                {
                    return Forbid();
                }
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Home");
                }
            }

            if (!string.IsNullOrEmpty(result.SuccessMessage))
            {
                TempData["SuccessMessage"] = result.SuccessMessage;
            }

            var hashCourseId = EncodeToHash(result.CourseId);
            return Redirect($"/Course/Details/{hashCourseId}#curriculum");
        }        // GET: Quiz/Details/5
        [RequireAuthentication("You need to login to view quiz details. Please login to continue.")]
        public async Task<IActionResult> Details(string id)
        {
            // Decode hash ID to real ID
            var realId = DecodeHashId(id);
            var result = await _quizServiceImpl.GetQuizDetailsAsync(User, realId);

            if (!result.Success)
            {
                if (result.IsNotFound)
                {
                    return NotFound();
                }
                if (result.IsForbidden)
                {
                    return Forbid();
                }
                if (!string.IsNullOrEmpty(result.RedirectAction) && !string.IsNullOrEmpty(result.RedirectController))
                {
                    return RedirectToAction(result.RedirectAction, result.RedirectController);
                }
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Home");
                }
            }

            return View(result.ViewModel);
        }

        // GET: Quiz/Preview/5
        public async Task<IActionResult> Preview(string id)
        {
            // Decode hash ID to real ID
            var realId = DecodeHashId(id);
            var result = await _quizServiceImpl.GetQuizPreviewAsync(User, realId);

            if (!result.Success)
            {
                if (result.IsNotFound)
                {
                    return NotFound();
                }
                if (result.IsForbidden)
                {
                    return Forbid();
                }
                if (!string.IsNullOrEmpty(result.RedirectAction) && !string.IsNullOrEmpty(result.RedirectController))
                {
                    return RedirectToAction(result.RedirectAction, result.RedirectController);
                }
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Home");
                }
            }

            return View(result.ViewModel);
        }
    }
}

