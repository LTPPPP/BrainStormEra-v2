/**
 * Create/Edit Chapter JavaScript
 * Handles form validation, user experience enhancements, and real-time feedback
 */

document.addEventListener("DOMContentLoaded", function () {
  // Form validation and enhancement
  initializeFormValidation();
  initializeCharacterCounters();
  initializeTooltips();
  initializeFormSubmissionHandling();
});

/**
 * Initialize form validation with real-time feedback
 */
function initializeFormValidation() {
  const form = document.querySelector("form");
  if (!form) return;

  const inputs = form.querySelectorAll(
    "input[required], textarea[required], select[required]"
  );

  inputs.forEach((input) => {
    // Add real-time validation
    input.addEventListener("blur", function () {
      validateField(this);
    });

    input.addEventListener("input", function () {
      // Clear validation state on input
      this.classList.remove("is-valid", "is-invalid");
      const feedback = this.parentNode.querySelector(
        ".invalid-feedback, .valid-feedback"
      );
      if (feedback) {
        feedback.style.display = "none";
      }
    });
  });
}

/**
 * Validate individual form field
 */
function validateField(field) {
  const value = field.value.trim();
  const isValid = field.checkValidity();

  // Remove existing validation classes
  field.classList.remove("is-valid", "is-invalid");

  // Remove existing feedback
  let existingFeedback = field.parentNode.querySelector(
    ".invalid-feedback, .valid-feedback"
  );
  if (existingFeedback) {
    existingFeedback.remove();
  }

  // Add validation class and feedback
  if (value !== "") {
    if (isValid) {
      field.classList.add("is-valid");
      addValidationFeedback(field, "valid", "Looks good!");
    } else {
      field.classList.add("is-invalid");
      addValidationFeedback(field, "invalid", field.validationMessage);
    }
  }
}

/**
 * Add validation feedback message
 */
function addValidationFeedback(field, type, message) {
  const feedback = document.createElement("div");
  feedback.className = type === "valid" ? "valid-feedback" : "invalid-feedback";
  feedback.textContent = message;
  feedback.style.display = "block";

  // Insert after the field
  field.parentNode.insertBefore(feedback, field.nextSibling);
}

/**
 * Initialize character counters for text inputs
 */
function initializeCharacterCounters() {
  const textFields = document.querySelectorAll(
    "input[maxlength], textarea[maxlength]"
  );

  textFields.forEach((field) => {
    const maxLength = parseInt(field.getAttribute("maxlength"));

    // Create counter element
    const counter = document.createElement("div");
    counter.className = "character-counter text-muted small mt-1";
    counter.innerHTML = `<span class="current">0</span> / <span class="max">${maxLength}</span> characters`;

    // Insert counter after form-text or after field
    const formText = field.parentNode.querySelector(".form-text");
    if (formText) {
      formText.parentNode.insertBefore(counter, formText.nextSibling);
    } else {
      field.parentNode.appendChild(counter);
    }

    // Update counter on input
    field.addEventListener("input", function () {
      updateCharacterCounter(this, counter);
    });

    // Initial count
    updateCharacterCounter(field, counter);
  });
}

/**
 * Update character counter display
 */
function updateCharacterCounter(field, counter) {
  const current = field.value.length;
  const max = parseInt(field.getAttribute("maxlength"));
  const currentSpan = counter.querySelector(".current");

  currentSpan.textContent = current;

  // Color coding
  const percentage = (current / max) * 100;
  counter.classList.remove("text-warning", "text-danger");

  if (percentage >= 90) {
    counter.classList.add("text-danger");
  } else if (percentage >= 75) {
    counter.classList.add("text-warning");
  }
}

/**
 * Initialize Bootstrap tooltips
 */
function initializeTooltips() {
  const tooltipTriggerList = [].slice.call(
    document.querySelectorAll('[data-bs-toggle="tooltip"]')
  );
  tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl);
  });
}

/**
 * Handle form submission with loading states
 */
function initializeFormSubmissionHandling() {
  const form = document.querySelector("form");
  if (!form) return;

  form.addEventListener("submit", function (e) {
    const submitBtn = form.querySelector('button[type="submit"]');
    if (submitBtn) {
      // Disable button and show loading state
      submitBtn.disabled = true;

      const originalContent = submitBtn.innerHTML;
      const isUpdate = submitBtn.textContent.includes("Update");

      submitBtn.innerHTML = isUpdate
        ? '<i class="fas fa-spinner fa-spin me-2"></i>Updating Chapter...'
        : '<i class="fas fa-spinner fa-spin me-2"></i>Creating Chapter...';

      // Re-enable after 10 seconds as fallback
      setTimeout(() => {
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalContent;
      }, 10000);
    }
  });
}

/**
 * Smart chapter order suggestion
 */
function initializeChapterOrderSuggestion() {
  const orderInput = document.querySelector('input[name="ChapterOrder"]');
  const existingChapters = document.querySelectorAll(".chapter-item");

  if (orderInput && existingChapters.length > 0) {
    // Get highest order number
    let maxOrder = 0;
    existingChapters.forEach((chapter) => {
      const orderElement = chapter.querySelector(".order-number");
      if (orderElement) {
        const order = parseInt(orderElement.textContent);
        if (order > maxOrder) maxOrder = order;
      }
    });

    // Suggest next order if field is empty
    if (!orderInput.value && maxOrder > 0) {
      orderInput.value = maxOrder + 1;
      orderInput.classList.add("suggested-value");

      // Add suggestion indicator
      const suggestion = document.createElement("div");
      suggestion.className = "suggestion-indicator text-info small mt-1";
      suggestion.innerHTML =
        '<i class="fas fa-lightbulb me-1"></i>Suggested next position';
      orderInput.parentNode.appendChild(suggestion);

      // Remove suggestion styling when user modifies
      orderInput.addEventListener(
        "input",
        function () {
          this.classList.remove("suggested-value");
          if (suggestion.parentNode) {
            suggestion.remove();
          }
        },
        { once: true }
      );
    }
  }
}

/**
 * Prerequisite chapter validation
 */
function initializePrerequisiteValidation() {
  const lockCheckbox = document.querySelector('input[name="IsLocked"]');
  const prerequisiteSelect = document.querySelector(
    'select[name="UnlockAfterChapterId"]'
  );
  const orderInput = document.querySelector('input[name="ChapterOrder"]');

  if (!lockCheckbox || !prerequisiteSelect || !orderInput) return;

  function validatePrerequisite() {
    if (lockCheckbox.checked && prerequisiteSelect.value) {
      const currentOrder = parseInt(orderInput.value);
      const selectedOption =
        prerequisiteSelect.options[prerequisiteSelect.selectedIndex];

      if (selectedOption && selectedOption.textContent) {
        const prerequisiteOrder = parseInt(
          selectedOption.textContent.match(/Chapter (\d+):/)?.[1] || "0"
        );

        if (prerequisiteOrder >= currentOrder) {
          showPrerequisiteError(
            "Prerequisite chapter must come before this chapter in the sequence."
          );
          return false;
        } else {
          clearPrerequisiteError();
          return true;
        }
      }
    }
    clearPrerequisiteError();
    return true;
  }

  function showPrerequisiteError(message) {
    clearPrerequisiteError();
    const error = document.createElement("div");
    error.className = "prerequisite-error text-danger small mt-1";
    error.innerHTML = `<i class="fas fa-exclamation-triangle me-1"></i>${message}`;
    prerequisiteSelect.parentNode.appendChild(error);
    prerequisiteSelect.classList.add("is-invalid");
  }

  function clearPrerequisiteError() {
    const error = document.querySelector(".prerequisite-error");
    if (error) error.remove();
    prerequisiteSelect.classList.remove("is-invalid");
  }

  // Validate on change
  lockCheckbox.addEventListener("change", validatePrerequisite);
  prerequisiteSelect.addEventListener("change", validatePrerequisite);
  orderInput.addEventListener("input", validatePrerequisite);
}

/**
 * Initialize all enhanced features
 */
function initializeEnhancedFeatures() {
  initializeChapterOrderSuggestion();
  initializePrerequisiteValidation();
}

// Initialize enhanced features when DOM is ready
if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", initializeEnhancedFeatures);
} else {
  initializeEnhancedFeatures();
}

// Export functions for potential external use
window.ChapterFormHelpers = {
  validateField,
  updateCharacterCounter,
  initializeFormValidation,
};
