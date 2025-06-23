/**
 * Quiz Details Page JavaScript
 * Handles quiz management functionality, question operations, and UI interactions
 */

// Global function for delete confirmation (used by inline onclick)
function confirmDelete(questionId, questionText) {
  document.getElementById("questionToDelete").textContent = questionText;
  document.getElementById("deleteForm").action =
    "/Question/Delete/" + questionId;

  var deleteModal = new bootstrap.Modal(document.getElementById("deleteModal"));
  deleteModal.show();
}

// Auto-hide alerts after 5 seconds
document.addEventListener("DOMContentLoaded", function () {
  const alerts = document.querySelectorAll(".alert");
  alerts.forEach(function (alert) {
    setTimeout(function () {
      alert.style.transition = "opacity 0.5s ease";
      alert.style.opacity = "0";
      setTimeout(function () {
        alert.remove();
      }, 500);
    }, 5000);
  });
});

// Hide preloader when page is loaded
window.addEventListener("load", function () {
  const loader = document.querySelector(".page-loader");
  if (loader) {
    loader.classList.add("loaded");
    setTimeout(() => {
      loader.style.display = "none";
    }, 500);
  }
});

// Change header style on scroll
window.addEventListener("scroll", function () {
  const header = document.querySelector("header");
  if (header) {
    if (window.scrollY > 100) {
      header.classList.add("scrolled");
    } else {
      header.classList.remove("scrolled");
    }
  }
});

// Initialize sortable for questions
document.addEventListener("DOMContentLoaded", function () {
  const questionsList = document.getElementById("questions-sortable");
  if (questionsList && typeof Sortable !== "undefined") {
    new Sortable(questionsList, {
      handle: ".drag-handle",
      animation: 150,
      ghostClass: "sortable-ghost",
      chosenClass: "sortable-chosen",
      dragClass: "sortable-drag",
      onEnd: function (evt) {
        // Get the new order of question IDs
        const questionIds = Array.from(questionsList.children).map((item) =>
          item.getAttribute("data-question-id")
        );

        // Get quiz ID - try multiple ways to find it
        const quizId =
          questionsList.getAttribute("data-quiz-id") ||
          document
            .querySelector("[data-quiz-id]")
            ?.getAttribute("data-quiz-id") ||
          document.querySelector('input[name="QuizId"]')?.value;

        if (!quizId) {
          console.error("Quiz ID not found");
          return;
        }

        // Send AJAX request to update order
        const formData = new FormData();
        formData.append("quizId", quizId);
        questionIds.forEach((id, index) => {
          formData.append("questionIds[" + index + "]", id);
        });

        const token = document.querySelector(
          'input[name="__RequestVerificationToken"]'
        )?.value;
        if (token) {
          formData.append("__RequestVerificationToken", token);
        }

        fetch("/Question/ReorderQuestions", {
          method: "POST",
          body: formData,
        })
          .then((response) => response.json())
          .then((data) => {
            if (data.success) {
              // Update question order numbers in UI
              Array.from(questionsList.children).forEach((item, index) => {
                const orderElement = item.querySelector(".h5.text-primary");
                if (orderElement) {
                  orderElement.textContent = index + 1;
                }
              });

              // Show success message
              showToast(
                "success",
                data.message || "Question order has been updated!"
              );
            } else {
              showToast(
                "error",
                "An error occurred while updating question order."
              );
            }
          })
          .catch((error) => {
            console.error("Error:", error);
            showToast(
              "error",
              "An error occurred while updating question order."
            );
          });
      },
    });
  }
});

// Toast notification function
function showToast(type, message) {
  // Create toast element
  const toast = document.createElement("div");
  toast.className =
    "toast align-items-center text-white bg-" +
    (type === "success" ? "success" : "danger") +
    " border-0";
  toast.setAttribute("role", "alert");
  toast.innerHTML =
    '<div class="d-flex"><div class="toast-body">' +
    message +
    '</div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>';

  // Add to page
  let toastContainer = document.querySelector(".toast-container");
  if (!toastContainer) {
    toastContainer = document.createElement("div");
    toastContainer.className = "toast-container position-fixed top-0 end-0 p-3";
    document.body.appendChild(toastContainer);
  }

  toastContainer.appendChild(toast);

  // Show toast
  const bsToast = new bootstrap.Toast(toast);
  bsToast.show();

  // Remove from DOM after hiding
  toast.addEventListener("hidden.bs.toast", function () {
    toast.remove();
  });
}

// Initialize tooltips
document.addEventListener("DOMContentLoaded", function () {
  const tooltipTriggerList = [].slice.call(
    document.querySelectorAll('[data-bs-toggle="tooltip"]')
  );
  tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl);
  });
});

// Smooth scrolling for anchor links
document.addEventListener("click", function (e) {
  if (e.target.matches('a[href^="#"]')) {
    e.preventDefault();
    const target = document.querySelector(e.target.getAttribute("href"));
    if (target) {
      target.scrollIntoView({
        behavior: "smooth",
        block: "start",
      });
    }
  }
});

// Add loading state to forms
document.addEventListener("submit", function (e) {
  if (e.target.matches("#deleteForm")) {
    const submitButton = e.target.querySelector('button[type="submit"]');
    if (submitButton) {
      submitButton.disabled = true;
      const originalText = submitButton.innerHTML;
      submitButton.innerHTML =
        '<i class="fas fa-spinner fa-spin me-1"></i>Deleting...';

      // Reset button after 30 seconds (fallback)
      setTimeout(() => {
        submitButton.disabled = false;
        submitButton.innerHTML = originalText;
      }, 30000);
    }
  }
});

// Handle card hover animations
document.addEventListener("DOMContentLoaded", function () {
  const cards = document.querySelectorAll(".card");
  cards.forEach((card, index) => {
    card.style.animationDelay = index * 0.1 + "s";
    card.classList.add("fade-in");
  });
});
