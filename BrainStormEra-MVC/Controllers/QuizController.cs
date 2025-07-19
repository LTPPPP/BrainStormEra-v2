using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BrainStormEra_MVC.Filters;
using Microsoft.Extensions.Logging;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class QuizController : BaseController
    {
        private readonly QuizServiceImpl _quizServiceImpl;
        private readonly ILogger<QuizController> _logger;

        public QuizController(QuizServiceImpl quizServiceImpl, ILogger<QuizController> logger) : base()
        {
            _quizServiceImpl = quizServiceImpl;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "instructor")]
        public async Task<IActionResult> Create(string chapterId)
        {
            // Use chapter ID directly without decoding
            var realChapterId = chapterId;

            var result = await _quizServiceImpl.GetCreateQuizAsync(User, realChapterId);

            // Clear ModelState to ensure no validation errors show on initial load
            ModelState.Clear();
            ViewBag.IsPost = false;
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(result.RedirectAction, result.RedirectController, result.RouteValues);
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
                    ViewBag.IsPost = true;
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

            return RedirectToAction("Details", "Quiz", new { id = result.QuizId });
        }

        // GET: Quiz/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            // Use ID directly without decoding
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

            return RedirectToAction("Details", "Quiz", new { id = result.QuizId });
        }

        // POST: Quiz/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            // Use ID directly without decoding
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
        }        // GET: Quiz/Details/5
        [RequireAuthentication("You need to login to view quiz details. Please login to continue.")]
        public async Task<IActionResult> Details(string id)
        {
            // Use ID directly without decoding
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
            // Use ID directly without decoding
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

        // GET: Quiz/Take/5
        [RequireAuthentication("You need to login to take the quiz. Please login to continue.")]
        [Authorize(Roles = "learner")]
        public async Task<IActionResult> Take(string id)
        {
            // Use ID directly without decoding
            var result = await _quizServiceImpl.GetQuizTakeAsync(User, id);

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
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction(result.RedirectAction, result.RedirectController, result.RouteValues);
                }
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Home");
                }
            }

            return View(result.ViewModel);
        }

        // POST: Quiz/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "learner")]
        public async Task<IActionResult> Submit(QuizTakeSubmitViewModel model)
        {
            var result = await _quizServiceImpl.SubmitQuizAsync(User, model, ModelState);

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
                if (result.ReturnView)
                {
                    // Return to take quiz view with errors
                    var takeResult = await _quizServiceImpl.GetQuizTakeAsync(User, model.QuizId);
                    if (takeResult.Success)
                    {
                        return View("Take", takeResult.ViewModel);
                    }
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

            return RedirectToAction("Result", new { id = result.AttemptId });
        }

        // GET: Quiz/Result/5
        [RequireAuthentication("You need to login to view quiz results. Please login to continue.")]
        [Authorize(Roles = "learner")]
        public async Task<IActionResult> Result(string id)
        {
            // Use ID directly without decoding (attempt ID)
            var result = await _quizServiceImpl.GetQuizResultAsync(User, id);

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
                    return RedirectToAction(result.RedirectAction, result.RedirectController, result.RouteValues);
                }
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Home");
                }
            }

            return View(result.ViewModel);
        }

        // GET: Quiz/ManageQuestions/5
        [Authorize(Roles = "instructor")]
        public async Task<IActionResult> ManageQuestions(string quizId)
        {
            // Use quiz ID directly without decoding
            var result = await _quizServiceImpl.GetQuizQuestionsAsync(User, quizId);

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

            return View(result.ViewModel);
        }
    }
}

