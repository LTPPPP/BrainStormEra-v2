/**
 * BrainStormEra Login Module
 * Optimized for performance with minimal DOM operations and efficient animations
 */
(function () {
  "use strict";

  // Cache DOM elements for better performance
  let formElements = {};
  let animationFrame;
  let formSubmitted = false;

  // Initialize when DOM is fully loaded
  document.addEventListener("DOMContentLoaded", init);

  /**
   * Initialize the login module
   */
  function init() {
    // Cache DOM elements
    cacheElements();

    // Set up event listeners
    setupEventListeners();

    // Apply entrance animations
    applyEntranceAnimations();

    // Preload background images for smoother experience
    preloadImages();

    // Initialize the parallax effect
    initParallax();

    // Start quote rotation
    startQuoteRotation();
  }

  /**
   * Cache DOM elements to minimize DOM queries
   */
  function cacheElements() {
    // Forms
    formElements.loginForm = document.getElementById("loginForm");

    // Inputs
    formElements.usernameInput = document.querySelector(
      'input[name="Username"]'
    );
    formElements.passwordInput = document.querySelector(
      'input[name="Password"]'
    );

    // Buttons
    formElements.submitButtons = document.querySelectorAll(".login-btn");
    formElements.togglePasswordButtons =
      document.querySelectorAll(".toggle-password");

    // Validation
    formElements.validationSummary = document.querySelector(
      ".validation-summary"
    );

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
    if (formElements.loginForm) {
      formElements.loginForm.addEventListener("submit", handleFormSubmit);
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
    });

    // Auto focus username on login page
    if (formElements.usernameInput && !formElements.validationSummary) {
      setTimeout(() => formElements.usernameInput.focus(), 500);
    }
  }

  /**
   * Handle input focus for floating label effect
   */
  function handleInputFocus(e) {
    const input = e.target;
    const formGroup = input.closest(".form-group");
    if (formGroup) {
      formGroup.classList.add("focused");
    }
  }

  /**
   * Handle input blur for floating label effect
   */
  function handleInputBlur(e) {
    const input = e.target;
    const formGroup = input.closest(".form-group");
    if (formGroup && !input.value) {
      formGroup.classList.remove("focused");
    }
  }

  /**
   * Apply entrance animations with staggered timing
   */
  function applyEntranceAnimations() {
    // Header, form and footer animations are handled by CSS classes

    // Animate inputs with staggering
    const inputs = document.querySelectorAll(".form-group");
    inputs.forEach((input, index) => {
      input.style.opacity = "0";
      input.style.transform = "translateY(20px)";

      setTimeout(() => {
        input.style.transition = "opacity 0.5s ease, transform 0.5s ease";
        input.style.opacity = "1";
        input.style.transform = "translateY(0)";
      }, 100 + index * 70);
    });

    // Add ripple effect to login button
    const loginBtn = document.querySelector(".login-btn");
    if (loginBtn) {
      loginBtn.addEventListener("mousedown", function (e) {
        createRippleEffect(e, this);
      });
    }
  }

  /**
   * Create ripple effect on button click
   */
  function createRippleEffect(e, button) {
    const rect = button.getBoundingClientRect();
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;

    const ripple = document.createElement("span");
    ripple.className = "ripple";
    ripple.style.left = `${x}px`;
    ripple.style.top = `${y}px`;
    button.appendChild(ripple);

    setTimeout(() => {
      ripple.remove();
    }, 600);
  }

  /**
   * Handle form submission
   * @param {Event} e - The submit event
   */
  function handleFormSubmit(e) {
    if (formSubmitted) {
      e.preventDefault();
      return;
    }

    formSubmitted = true;

    const form = e.target;
    const submitBtn = form.querySelector(".login-btn");
    const btnText = submitBtn.querySelector(".btn-text");
    const spinner = submitBtn.querySelector(".spinner-border");

    if (btnText && spinner) {
      // Add visual feedback with smooth animation
      btnText.textContent = "Signing in...";
      btnText.style.opacity = "0";

      setTimeout(() => {
        btnText.style.opacity = "1";
        spinner.classList.remove("d-none");
      }, 200);

      submitBtn.disabled = true;
      submitBtn.style.transform = "translateZ(0)";

      // Add ripple effect for better visual feedback
      const ripple = document.createElement("span");
      ripple.classList.add("ripple");
      ripple.style.left = "50%";
      ripple.style.top = "50%";
      submitBtn.appendChild(ripple);

      setTimeout(() => {
        ripple.remove();
      }, 600);
    }
  }

  /**
   * Toggle password visibility
   * @param {Event} e - The click event
   */
  function togglePasswordVisibility(e) {
    const button = e.currentTarget;
    const passwordInput = button.closest(".input-group").querySelector("input");
    const icon = button.querySelector("i");

    if (passwordInput.type === "password") {
      passwordInput.type = "text";
      icon.classList.remove("fa-eye");
      icon.classList.add("fa-eye-slash");
    } else {
      passwordInput.type = "password";
      icon.classList.remove("fa-eye-slash");
      icon.classList.add("fa-eye");
    }
  }

  /**
   * Handle OTP input for better UX
   * @param {Event} e - The input event
   */
  /**
   * Initialize parallax effect for background
   */
  function initParallax() {
    if (!formElements.parallaxBg) return;

    // Simple parallax effect on mouse movement
    document.addEventListener("mousemove", function (e) {
      const moveX = (e.clientX / window.innerWidth) * 15;
      const moveY = (e.clientY / window.innerHeight) * 15;

      // Use requestAnimationFrame for better performance
      requestAnimationFrame(function () {
        formElements.parallaxBg.style.backgroundPosition = `calc(50% + ${moveX}px) calc(50% + ${moveY}px)`;
      });
    });
  }

  /**
   * Preload images for smoother experience
   */
  function preloadImages() {
    // Preload background image if it exists
    const bgImage = new Image();
    bgImage.src = "/images/login-bg.jpg";
  }

  /**
   * Start quote rotation with optimized animation
   */
  function startQuoteRotation() {
    if (!formElements.quotes || formElements.quotes.length <= 1) return;

    let currentQuote = 0;

    function rotateQuotes() {
      formElements.quotes.forEach((quote) => quote.classList.remove("active"));
      formElements.quotes[currentQuote].classList.add("active");
      currentQuote = (currentQuote + 1) % formElements.quotes.length;
    }

    // Initial delay to allow page to load, then rotate quotes every 8 seconds
    setTimeout(() => {
      setInterval(rotateQuotes, 8000);
    }, 2000);
  }

  /**
   * Show error toast for login failures
   */
  function showLoginError(message) {
    if (window.showErrorToast) {
      window.showErrorToast(message);
    } else {
      alert(message); // Fallback for older browsers
    }
  }

  /**
   * Show success toast for login success
   */
  function showLoginSuccess(message) {
    if (window.showSuccessToast) {
      window.showSuccessToast(message);
    }
  }

  /**
   * Handle form submission with validation
   */
  function handleFormSubmission(form) {
    form.addEventListener("submit", function (e) {
      // Prevent multiple submissions
      if (formSubmitted) {
        e.preventDefault();
        return false;
      }

      // Basic client-side validation
      const username = formElements.usernameInput.value.trim();
      const password = formElements.passwordInput.value.trim();

      if (!username) {
        e.preventDefault();
        showLoginError("Please enter your username.");
        formElements.usernameInput.focus();
        return false;
      }

      if (!password) {
        e.preventDefault();
        showLoginError("Please enter your password.");
        formElements.passwordInput.focus();
        return false;
      }

      // Mark form as submitted to prevent double submission
      formSubmitted = true;

      // Show loading state
      const submitButton = form.querySelector('button[type="submit"]');
      if (submitButton) {
        const originalText = submitButton.innerHTML;
        submitButton.innerHTML =
          '<span class="spinner-border spinner-border-sm me-2"></span>Signing In...';
        submitButton.disabled = true;

        // Re-enable after 10 seconds as failsafe
        setTimeout(() => {
          submitButton.innerHTML = originalText;
          submitButton.disabled = false;
          formSubmitted = false;
        }, 10000);
      }
    });
  }

  // ...existing code...

  /**
   * Set up event listeners
   */
  function setupEventListeners() {
    // Form submission handling
    if (formElements.loginForm) {
      handleFormSubmission(formElements.loginForm);
    }
  }
})();
