/**
 * BrainStormEra Registration Module
 * Optimized for performance with minimal DOM operations and efficient animations
 */
(function () {
  "use strict";

  // Cache DOM elements for better performance
  let formElements = {};
  let animationFrame;
  let formSubmitted = false;
  let usernameTimer;
  let emailTimer;
  let passwordStrengthTimeout;

  // Initialize when DOM is fully loaded
  document.addEventListener("DOMContentLoaded", init);

  /**
   * Initialize the registration module
   */
  function init() {
    // Cache DOM elements
    cacheElements();

    // Set up event listeners
    setupEventListeners();

    // Apply entrance animations
    applyEntranceAnimations();

    // Preload background images for smoother experience
    preloadImages(); // Start quote rotation
    startQuoteRotation();
  }

  /**
   * Cache DOM elements to minimize DOM queries
   */
  function cacheElements() {
    // Forms
    formElements.registerForm = document.getElementById("registerForm");

    // Inputs
    formElements.usernameInput = document.querySelector(
      'input[name="Username"]'
    );
    formElements.emailInput = document.querySelector('input[name="Email"]');
    formElements.passwordInput = document.querySelector(
      'input[name="Password"]'
    );
    formElements.confirmPasswordInput = document.querySelector(
      'input[name="ConfirmPassword"]'
    );

    // Buttons
    formElements.submitButtons = document.querySelectorAll(".register-btn");
    formElements.togglePasswordButtons =
      document.querySelectorAll(".toggle-password");
    formElements.toggleDetailsButton = document.getElementById("toggleDetails");

    // Validation
    formElements.validationSummary = document.querySelector(
      ".validation-summary"
    );
    formElements.usernameFeedback =
      document.getElementById("username-feedback");
    formElements.emailFeedback = document.getElementById("email-feedback");
    formElements.passwordStrengthBar = document.querySelector(".strength-bar");

    // Additional Fields
    formElements.additionalFields = document.getElementById("additionalFields");

    // Quotes
    formElements.quotes = document.querySelectorAll(".quote-item");

    // Parallax
    formElements.parallaxBg = document.querySelector(".parallax-bg");
  }

  /**
   * Set up event listeners using event delegation where possible
   */
  function setupEventListeners() {
    // Form submissions
    if (formElements.registerForm) {
      formElements.registerForm.addEventListener("submit", handleFormSubmit);
    }

    // Toggle password visibility
    formElements.togglePasswordButtons.forEach((button) => {
      button.addEventListener("click", togglePasswordVisibility);
    });

    // Focus effects for inputs
    const inputs = document.querySelectorAll(".form-control");
    inputs.forEach((input) => {
      input.addEventListener("focus", handleInputFocus);
      input.addEventListener("blur", handleInputBlur);

      // Special validation for username and email
      if (input.dataset.validate === "username") {
        input.addEventListener("input", debounceUsernameCheck);
      } else if (input.dataset.validate === "email") {
        input.addEventListener("input", debounceEmailCheck);
      }
    });

    // Password strength meter
    if (formElements.passwordInput) {
      formElements.passwordInput.addEventListener(
        "input",
        updatePasswordStrength
      );
    }

    // Toggle additional fields
    if (formElements.toggleDetailsButton) {
      formElements.toggleDetailsButton.addEventListener(
        "click",
        toggleAdditionalFields
      );
    }

    // Auto focus username on registration page
    if (formElements.usernameInput && !formElements.validationSummary) {
      setTimeout(() => formElements.usernameInput.focus(), 500);
    }
  }

  /**
   * Handle input focus for floating label effect
   */
  function handleInputFocus(e) {
    const formGroup = e.target.closest(".form-group");
    if (formGroup) {
      formGroup.classList.add("focused");
    }
  }

  /**
   * Handle input blur for floating label effect
   */
  function handleInputBlur(e) {
    const formGroup = e.target.closest(".form-group");
    if (formGroup && !e.target.value) {
      formGroup.classList.remove("focused");
    }
  }

  /**
   * Toggle password visibility
   */
  function togglePasswordVisibility(e) {
    const button = e.currentTarget;
    const inputGroup = button.closest(".input-group");
    const passwordInput = inputGroup.querySelector("input");
    const icon = button.querySelector("i");

    if (passwordInput.type === "password") {
      passwordInput.type = "text";
      icon.classList.remove("fa-eye");
      icon.classList.add("fa-eye-slash");
      button.setAttribute("aria-label", "Hide password");
    } else {
      passwordInput.type = "password";
      icon.classList.remove("fa-eye-slash");
      icon.classList.add("fa-eye");
      button.setAttribute("aria-label", "Show password");
    }

    // Create ripple effect
    createRipple(button, e);
  }

  /**
   * Toggle additional fields visibility
   */
  function toggleAdditionalFields() {
    const additionalFields = formElements.additionalFields;
    const toggleButton = formElements.toggleDetailsButton;
    const toggleSpan = toggleButton.querySelector("span");
    const toggleIcon = toggleButton.querySelector("i");

    if (
      additionalFields.style.display === "none" ||
      !additionalFields.style.display
    ) {
      // Show fields
      additionalFields.style.display = "block";
      toggleSpan.textContent = "Hide additional details";
      toggleIcon.classList.remove("fa-chevron-down");
      toggleIcon.classList.add("fa-chevron-up");

      // Animate the height
      additionalFields.style.maxHeight = "0px";
      additionalFields.style.opacity = "0";

      // Force reflow
      additionalFields.offsetHeight;

      // Animate in
      additionalFields.style.transition =
        "max-height 0.5s ease, opacity 0.4s ease";
      additionalFields.style.maxHeight = additionalFields.scrollHeight + "px";
      additionalFields.style.opacity = "1";
    } else {
      // Hide fields
      toggleSpan.textContent = "Add more details";
      toggleIcon.classList.remove("fa-chevron-up");
      toggleIcon.classList.add("fa-chevron-down");

      // Animate out
      additionalFields.style.maxHeight = "0px";
      additionalFields.style.opacity = "0";

      // Hide after animation completes
      setTimeout(() => {
        additionalFields.style.display = "none";
      }, 500);
    }

    // Create ripple effect
    createRipple(toggleButton, event);
  }

  /**
   * Check username availability with debounce
   */
  function debounceUsernameCheck() {
    clearTimeout(usernameTimer);
    const username = formElements.usernameInput.value;

    // Clear feedback if empty
    if (!username || username.length < 3) {
      formElements.usernameFeedback.innerHTML = "";
      formElements.usernameFeedback.className = "availability-feedback";
      return;
    }

    // Show checking status
    formElements.usernameFeedback.innerHTML =
      '<i class="fas fa-spinner fa-spin"></i> Checking availability...';
    formElements.usernameFeedback.className = "availability-feedback checking";

    // Debounce the check
    usernameTimer = setTimeout(() => {
      checkUsernameAvailability(username);
    }, 500);
  }

  /**
   * Check email availability with debounce
   */
  function debounceEmailCheck() {
    clearTimeout(emailTimer);
    const email = formElements.emailInput.value;

    // Clear feedback if empty or invalid
    if (!email || !isValidEmail(email)) {
      formElements.emailFeedback.innerHTML = "";
      formElements.emailFeedback.className = "availability-feedback";
      return;
    }

    // Show checking status
    formElements.emailFeedback.innerHTML =
      '<i class="fas fa-spinner fa-spin"></i> Checking availability...';
    formElements.emailFeedback.className = "availability-feedback checking";

    // Debounce the check
    emailTimer = setTimeout(() => {
      checkEmailAvailability(email);
    }, 500);
  }

  /**
   * Check if username is available via AJAX
   */
  function checkUsernameAvailability(username) {
    fetch(`/Register/CheckUsername?username=${encodeURIComponent(username)}`)
      .then((response) => response.json())
      .then((data) => {
        if (data.valid) {
          formElements.usernameFeedback.innerHTML =
            '<i class="fas fa-check-circle"></i> Username is available';
          formElements.usernameFeedback.className =
            "availability-feedback available";
        } else {
          formElements.usernameFeedback.innerHTML = `<i class="fas fa-times-circle"></i> ${
            data.message || "Username is not available"
          }`;
          formElements.usernameFeedback.className =
            "availability-feedback unavailable";
        }
      })
      .catch(() => {
        formElements.usernameFeedback.innerHTML = "";
      });
  }

  /**
   * Check if email is available via AJAX
   */
  function checkEmailAvailability(email) {
    fetch(`/Register/CheckEmail?email=${encodeURIComponent(email)}`)
      .then((response) => response.json())
      .then((data) => {
        if (data.valid) {
          formElements.emailFeedback.innerHTML =
            '<i class="fas fa-check-circle"></i> Email is available';
          formElements.emailFeedback.className =
            "availability-feedback available";
        } else {
          formElements.emailFeedback.innerHTML = `<i class="fas fa-times-circle"></i> ${
            data.message || "Email is already registered"
          }`;
          formElements.emailFeedback.className =
            "availability-feedback unavailable";
        }
      })
      .catch(() => {
        formElements.emailFeedback.innerHTML = "";
      });
  }

  /**
   * Update password strength meter
   */
  function updatePasswordStrength() {
    clearTimeout(passwordStrengthTimeout);

    const password = formElements.passwordInput.value;
    if (!password) {
      formElements.passwordStrengthBar.style.width = "0%";
      formElements.passwordStrengthBar.className = "strength-bar";
      return;
    }

    // Debounce the check to improve performance
    passwordStrengthTimeout = setTimeout(() => {
      const strength = calculatePasswordStrength(password);
      formElements.passwordStrengthBar.style.width = strength.percent + "%";
      formElements.passwordStrengthBar.className = `strength-bar ${strength.level}`;
    }, 100);
  }

  /**
   * Calculate password strength
   */
  function calculatePasswordStrength(password) {
    let strength = 0;

    // Basic length check
    if (password.length >= 8) strength += 25;

    // Complexity checks
    if (password.match(/[a-z]+/)) strength += 10;
    if (password.match(/[A-Z]+/)) strength += 15;
    if (password.match(/[0-9]+/)) strength += 15;
    if (password.match(/[^A-Za-z0-9]+/)) strength += 15;

    // Additional length bonus
    if (password.length >= 12) strength += 20;

    // Cap at 100
    strength = Math.min(strength, 100);

    // Determine level
    let level = "weak";
    if (strength >= 70) level = "strong";
    else if (strength >= 40) level = "medium";

    return {
      percent: strength,
      level: level,
    };
  }

  /**
   * Validate email format
   */
  function isValidEmail(email) {
    const emailRegex = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
    return emailRegex.test(email);
  }

  /**
   * Show error toast for registration failures
   */
  function showRegistrationError(message) {
    if (window.showErrorToast) {
      window.showErrorToast(message);
    } else {
      alert(message); // Fallback for older browsers
    }
  }

  /**
   * Show success toast for registration success
   */
  function showRegistrationSuccess(message) {
    if (window.showSuccessToast) {
      window.showSuccessToast(message);
    }
  }

  /**
   * Show warning toast
   */
  function showRegistrationWarning(message) {
    if (window.showWarningToast) {
      window.showWarningToast(message);
    }
  }

  /**
   * Enhanced form validation with toast notifications
   */
  function validateFormWithToasts(form) {
    const username = formElements.usernameInput?.value?.trim();
    const email = formElements.emailInput?.value?.trim();
    const password = formElements.passwordInput?.value;
    const confirmPassword = formElements.confirmPasswordInput?.value;
    const fullName = formElements.fullNameInput?.value?.trim();

    const errors = [];

    // Validate username
    if (!username) {
      errors.push("Username is required");
    } else if (username.length < 3) {
      errors.push("Username must be at least 3 characters long");
    } else if (!/^[a-zA-Z0-9_-]+$/.test(username)) {
      errors.push(
        "Username can only contain letters, numbers, underscores, and hyphens"
      );
    }

    // Validate email
    if (!email) {
      errors.push("Email is required");
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      errors.push("Please enter a valid email address");
    }

    // Validate password
    if (!password) {
      errors.push("Password is required");
    } else if (password.length < 6) {
      errors.push("Password must be at least 6 characters long");
    }

    // Validate confirm password
    if (!confirmPassword) {
      errors.push("Please confirm your password");
    } else if (password !== confirmPassword) {
      errors.push("Passwords do not match");
    }

    // Validate full name
    if (!fullName) {
      errors.push("Full name is required");
    }

    if (errors.length > 0) {
      showRegistrationError(
        "Please fix the following errors: " + errors.join(", ")
      );
      return false;
    }

    return true;
  }

  /**
   * Handle form submission with enhanced validation
   */
  function handleEnhancedFormSubmission(form) {
    form.addEventListener("submit", function (e) {
      // Prevent multiple submissions
      if (formSubmitted) {
        e.preventDefault();
        showRegistrationWarning(
          "Registration is already in progress. Please wait..."
        );
        return false;
      }

      // Validate form
      if (!validateFormWithToasts(form)) {
        e.preventDefault();
        return false;
      }

      // Mark form as submitted
      formSubmitted = true;

      // Show loading state
      const submitButton = form.querySelector('button[type="submit"]');
      if (submitButton) {
        const originalText = submitButton.innerHTML;
        submitButton.innerHTML =
          '<span class="spinner-border spinner-border-sm me-2"></span>Creating Account...';
        submitButton.disabled = true;

        // Show progress message
        showRegistrationWarning("Creating your account, please wait...");

        // Re-enable after 15 seconds as failsafe
        setTimeout(() => {
          submitButton.innerHTML = originalText;
          submitButton.disabled = false;
          formSubmitted = false;
        }, 15000);
      }
    });
  }

  /**
   * Handle form submission with loading state and animations
   */
  function handleFormSubmit(e) {
    // Prevent multiple submissions
    if (formSubmitted) {
      e.preventDefault();
      return;
    }

    // Set form to submitting state
    formSubmitted = true;

    // Update button state
    formElements.submitButtons.forEach((button) => {
      button.classList.add("submitting");
      const btnText = button.querySelector(".btn-text");
      const spinner = button.querySelector(".spinner-border");

      if (btnText) btnText.textContent = "Creating Account...";
      if (spinner) spinner.classList.remove("d-none");

      // Disable button with a slight delay to show the visual change
      setTimeout(() => {
        button.disabled = true;
      }, 10);
    });

    // Let form submit normally
  }

  /**
   * Apply entrance animations for elements
   */
  function applyEntranceAnimations() {
    // Add animation classes with staggered timing
    document
      .querySelectorAll(".login-form-wrapper > div")
      .forEach((el, index) => {
        setTimeout(() => {
          el.classList.add("visible");
        }, 100 * (index + 1));
      });
  }
  /**
   * Preload images for better performance
   */
  function preloadImages() {
    // Preload background image
    const img = new Image();
    img.src = "/img/login-bg.jpg";
  }

  /**
   * Create ripple effect on buttons
   */
  function createRipple(button, e) {
    // Remove existing ripples
    const ripple = button.querySelector(".ripple");
    if (ripple) {
      ripple.remove();
    }

    // Create new ripple
    const circle = document.createElement("span");
    circle.classList.add("ripple");
    button.appendChild(circle);

    // Set ripple position
    const rect = button.getBoundingClientRect();
    const d = Math.max(rect.width, rect.height) * 2;
    circle.style.width = circle.style.height = `${d}px`;

    const x = e.clientX - rect.left - d / 2;
    const y = e.clientY - rect.top - d / 2;

    circle.style.left = `${x}px`;
    circle.style.top = `${y}px`;

    // Remove after animation
    setTimeout(() => circle.remove(), 600);
  }

  /**
   * Rotate quotes in the background
   */
  function startQuoteRotation() {
    if (!formElements.quotes || formElements.quotes.length <= 1) return;

    let currentQuote = 0;
    setInterval(() => {
      formElements.quotes[currentQuote].classList.remove("active");
      currentQuote = (currentQuote + 1) % formElements.quotes.length;
      formElements.quotes[currentQuote].classList.add("active");
    }, 8000);
  }
})();
