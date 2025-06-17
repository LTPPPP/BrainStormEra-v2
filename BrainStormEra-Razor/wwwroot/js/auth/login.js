// Login Page JavaScript
document.addEventListener("DOMContentLoaded", function () {
  // Initialize login form
  initializeLoginForm();

  // Setup event listeners
  setupEventListeners();
});

function initializeLoginForm() {
  console.log("Login form initialized");

  // Focus on username field
  const usernameInput = document.querySelector(
    'input[name="LoginData.Username"]'
  );
  if (usernameInput) {
    usernameInput.focus();
  }

  // Clear any previous error messages after user starts typing
  setupInputValidation();
}

function setupEventListeners() {
  const loginForm = document.querySelector(".login-form");
  if (loginForm) {
    loginForm.addEventListener("submit", handleFormSubmit);
  }

  // Add enter key support
  const inputs = document.querySelectorAll(
    'input[type="text"], input[type="password"]'
  );
  inputs.forEach((input) => {
    input.addEventListener("keypress", handleKeyPress);
    input.addEventListener("input", clearFieldError);
  });

  // Show/hide password functionality could be added here
  setupPasswordVisibility();
}

function handleFormSubmit(event) {
  // Basic client-side validation
  const username = document
    .querySelector('input[name="LoginData.Username"]')
    .value.trim();
  const password = document
    .querySelector('input[name="LoginData.Password"]')
    .value.trim();

  if (!username || !password) {
    event.preventDefault();
    showError("Please fill in all required fields.");
    return false;
  }

  // Show loading state
  showLoadingState();

  return true;
}

function handleKeyPress(event) {
  if (event.key === "Enter") {
    const form = event.target.closest("form");
    if (form) {
      form.submit();
    }
  }
}

function clearFieldError(event) {
  const field = event.target;
  const errorSpan = field.parentNode.querySelector(".error-text");
  if (errorSpan && errorSpan.textContent) {
    errorSpan.textContent = "";
  }

  // Clear general error message
  const alertError = document.querySelector(".alert.error");
  if (alertError) {
    alertError.style.display = "none";
  }
}

function setupInputValidation() {
  const inputs = document.querySelectorAll("input[required]");
  inputs.forEach((input) => {
    input.addEventListener("blur", validateField);
  });
}

function validateField(event) {
  const field = event.target;
  const value = field.value.trim();

  if (!value && field.hasAttribute("required")) {
    showFieldError(field, "This field is required.");
  }
}

function showFieldError(field, message) {
  const errorSpan = field.parentNode.querySelector(".error-text");
  if (errorSpan) {
    errorSpan.textContent = message;
  }
}

function showError(message) {
  let alertDiv = document.querySelector(".alert.error");
  if (!alertDiv) {
    alertDiv = document.createElement("div");
    alertDiv.className = "alert error";
    const loginForm = document.querySelector(".login-form");
    loginForm.parentNode.insertBefore(alertDiv, loginForm);
  }
  alertDiv.textContent = message;
  alertDiv.style.display = "block";
}

function showLoadingState() {
  const submitBtn = document.querySelector(".btn-login");
  if (submitBtn) {
    submitBtn.disabled = true;
    submitBtn.textContent = "Logging in...";
  }
}

function setupPasswordVisibility() {
  // This could be enhanced to add show/hide password functionality
  // For now, it's just a placeholder for future enhancement
  console.log("Password visibility setup complete");
}
