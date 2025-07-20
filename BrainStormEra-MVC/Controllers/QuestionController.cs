using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Implementations;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class QuestionController : Controller
    {
        private readonly QuestionService _questionService;

        public QuestionController(QuestionService questionService)
        {
            _questionService = questionService;
        }

        // GET: Question/Create/5 (quizId)
        public async Task<IActionResult> Create(string quizId)
        {
            var result = await _questionService.GetCreateQuestionViewModelAsync(User, quizId);

            if (!result.Success)
            {
                if (result.RedirectToLogin)
                {
                    return RedirectToAction("Login", "Account");
                }

                TempData["ErrorMessage"] = result.ErrorMessage;
                return NotFound();
            }

            return View(result.ViewModel);
        }

        // POST: Question/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateQuestionViewModel model)
        {
            var result = await _questionService.CreateQuestionAsync(User, model, ModelState);

            if (!result.Success)
            {
                if (result.RedirectToLogin)
                {
                    return RedirectToAction("Login", "Account");
                }

                if (result.ReturnView)
                {
                    // Add validation errors to ModelState
                    if (result.ValidationErrors != null)
                    {
                        foreach (var error in result.ValidationErrors)
                        {
                            foreach (var errorMessage in error.Value)
                            {
                                ModelState.AddModelError(error.Key, errorMessage);
                            }
                        }
                    }
                    return View(result.ViewModel);
                }

                TempData["ErrorMessage"] = result.ErrorMessage;
                return View(model);
            }

            if (!string.IsNullOrEmpty(result.SuccessMessage))
            {
                TempData["SuccessMessage"] = result.SuccessMessage;
            }

            return RedirectToAction(result.RedirectAction, result.RedirectController, result.RedirectValues);
        }

        // GET: Question/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            var result = await _questionService.GetEditQuestionViewModelAsync(User, id);

            if (!result.Success)
            {
                if (result.RedirectToLogin)
                {
                    return RedirectToAction("Login", "Account");
                }

                TempData["ErrorMessage"] = result.ErrorMessage;
                return NotFound();
            }

            return View("Create", result.ViewModel);
        }

        // POST: Question/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CreateQuestionViewModel model)
        {
            var result = await _questionService.UpdateQuestionAsync(User, model, ModelState);

            if (!result.Success)
            {
                if (result.RedirectToLogin)
                {
                    return RedirectToAction("Login", "Account");
                }

                if (result.ReturnView)
                {
                    // Add validation errors to ModelState
                    if (result.ValidationErrors != null)
                    {
                        foreach (var error in result.ValidationErrors)
                        {
                            foreach (var errorMessage in error.Value)
                            {
                                ModelState.AddModelError(error.Key, errorMessage);
                            }
                        }
                    }
                    return View("Create", result.ViewModel);
                }

                TempData["ErrorMessage"] = result.ErrorMessage;
                return View("Create", model);
            }

            if (!string.IsNullOrEmpty(result.SuccessMessage))
            {
                TempData["SuccessMessage"] = result.SuccessMessage;
            }

            return RedirectToAction(result.RedirectAction, result.RedirectController, result.RedirectValues);
        }

        // POST: Question/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _questionService.DeleteQuestionAsync(User, id);

            if (!result.Success)
            {
                if (result.RedirectToLogin)
                {
                    return RedirectToAction("Login", "Account");
                }

                TempData["ErrorMessage"] = result.ErrorMessage;
                return NotFound();
            }

            if (!string.IsNullOrEmpty(result.SuccessMessage))
            {
                TempData["SuccessMessage"] = result.SuccessMessage;
            }

            return RedirectToAction(result.RedirectAction, result.RedirectController, result.RedirectValues);
        }

        // POST: Question/Reorder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReorderQuestions(string quizId, List<string> questionIds)
        {
            var result = await _questionService.ReorderQuestionsAsync(User, quizId, questionIds);

            if (!result.Success)
            {
                return Json(new { success = false, message = result.ErrorMessage });
            }

            return Json(new { success = true, message = result.SuccessMessage });
        }

        // POST: Question/Duplicate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duplicate(string id)
        {
            var result = await _questionService.DuplicateQuestionAsync(User, id);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return NotFound();
            }

            if (!string.IsNullOrEmpty(result.SuccessMessage))
            {
                TempData["SuccessMessage"] = result.SuccessMessage;
            }

            return RedirectToAction(result.RedirectAction, result.RedirectController, result.RedirectValues);
        }
    }
}

