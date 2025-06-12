using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using BrainStormEra_MVC.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class QuizController : Controller
    {
        private readonly QuizServiceImpl _quizServiceImpl;

        public QuizController(QuizServiceImpl quizServiceImpl)
        {
            _quizServiceImpl = quizServiceImpl;
        }

        // GET: Quiz/Create
        public async Task<IActionResult> Create(string chapterId)
        {
            var result = await _quizServiceImpl.GetCreateQuizAsync(User, chapterId);

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

            return RedirectToAction(result.RedirectAction, new { id = result.QuizId });
        }

        // GET: Quiz/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            var result = await _quizServiceImpl.GetEditQuizAsync(User, id);

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

            return RedirectToAction(result.RedirectAction, new { id = result.QuizId });
        }

        // POST: Quiz/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _quizServiceImpl.DeleteQuizAsync(User, id);

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

            return Redirect($"/Course/Details/{result.CourseId}#curriculum");
        }

        // GET: Quiz/Details/5
        public async Task<IActionResult> Details(string id)
        {
            var result = await _quizServiceImpl.GetQuizDetailsAsync(User, id);

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
            var result = await _quizServiceImpl.GetQuizPreviewAsync(User, id);

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
