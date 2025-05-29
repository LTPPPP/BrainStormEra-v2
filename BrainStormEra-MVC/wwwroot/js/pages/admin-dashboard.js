/**
 * Admin Dashboard JavaScript
 */
(function () {
  "use strict";

  // DOM elements
  let elements = {};

  // Initialize when DOM is fully loaded
  document.addEventListener("DOMContentLoaded", init);

  /**
   * Initialize the dashboard
   */
  function init() {
    // Cache DOM elements
    cacheElements();

    // Set up event listeners
    setupEventListeners();

    // Initialize animations
    initAnimations();
  }

  /**
   * Cache DOM elements for better performance
   */
  function cacheElements() {
    elements.statItems = document.querySelectorAll(".stat-item");
    elements.statValues = document.querySelectorAll(".stat-value");
    elements.manageUsersBtn = document.querySelector(".manage-users-btn");
    elements.viewButtons = document.querySelectorAll(".view-btn");
    elements.editButtons = document.querySelectorAll(".edit-btn");
    elements.deleteButtons = document.querySelectorAll(".delete-btn");
    elements.userActions = document.querySelectorAll(".dropdown-item");
  }

  /**
   * Set up event listeners
   */
  function setupEventListeners() {
    // Add ripple effect to buttons
    const buttons = document.querySelectorAll(".btn");
    buttons.forEach((button) => {
      button.addEventListener("click", createRippleEffect);
    });

    // Manage users button
    if (elements.manageUsersBtn) {
      elements.manageUsersBtn.addEventListener("click", function (e) {
        e.preventDefault();
        showInfoToast("User management functionality coming soon!");
      });
    }

    // Course action buttons
    elements.viewButtons.forEach((button) => {
      button.addEventListener("click", function (e) {
        e.preventDefault();
        const courseId = this.getAttribute("data-course-id");
        showInfoToast(`Viewing course: ${courseId}`);
      });
    });

    elements.editButtons.forEach((button) => {
      button.addEventListener("click", function (e) {
        e.preventDefault();
        const courseId = this.getAttribute("data-course-id");
        showInfoToast(`Editing course: ${courseId}`);
      });
    });

    elements.deleteButtons.forEach((button) => {
      button.addEventListener("click", function (e) {
        e.preventDefault();
        const courseId = this.getAttribute("data-course-id");
        if (confirm("Are you sure you want to delete this course?")) {
          showWarningToast(`Course ${courseId} deletion requested`);
        }
      });
    });

    // User action buttons
    elements.userActions.forEach((action) => {
      action.addEventListener("click", function (e) {
        e.preventDefault();
        const userId = this.getAttribute("data-user-id");
        const actionText = this.textContent.trim();

        if (actionText.includes("Ban") || actionText.includes("Unban")) {
          if (
            confirm(`Are you sure you want to ${actionText.toLowerCase()}?`)
          ) {
            showWarningToast(`User ${userId}: ${actionText} requested`);
          }
        } else {
          showInfoToast(`User ${userId}: ${actionText} requested`);
        }
      });
    });
  }

  /**
   * Initialize animations for dashboard elements
   */
  function initAnimations() {
    // Animate stats on scroll
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add("animated");
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.1 }
    );

    elements.statItems.forEach((item) => {
      observer.observe(item);
    });

    // Animate stats counting up
    if (elements.statValues) {
      elements.statValues.forEach((statValue) => {
        const targetValue = statValue.textContent;
        if (targetValue.includes("$")) {
          // Handle currency values
          const numericValue = parseFloat(
            targetValue.replace("$", "").replace(",", "")
          );
          animateValue(statValue, 0, numericValue, 1500, "$");
        } else {
          // Handle numeric values
          const numericValue = parseInt(targetValue.replace(",", ""), 10);
          if (!isNaN(numericValue)) {
            animateValue(statValue, 0, numericValue, 1500);
          }
        }
      });
    }
  }

  /**
   * Animate counting up to a value
   */
  function animateValue(element, start, end, duration, prefix = "") {
    const range = end - start;
    const startTime = performance.now();

    function updateValue(currentTime) {
      const elapsed = currentTime - startTime;
      const progress = Math.min(elapsed / duration, 1);
      const easeOutQuart = 1 - Math.pow(1 - progress, 4);
      const current = Math.floor(start + range * easeOutQuart);

      if (prefix === "$") {
        element.textContent = `$${current.toLocaleString()}.00`;
      } else {
        element.textContent = current.toLocaleString();
      }

      if (progress < 1) {
        requestAnimationFrame(updateValue);
      }
    }

    requestAnimationFrame(updateValue);
  }

  /**
   * Create ripple effect on button click
   */
  function createRippleEffect(e) {
    const button = this;
    const ripple = document.createElement("span");
    const rect = button.getBoundingClientRect();
    const size = Math.max(rect.width, rect.height);
    const x = e.clientX - rect.left - size / 2;
    const y = e.clientY - rect.top - size / 2;

    ripple.style.width = ripple.style.height = size + "px";
    ripple.style.left = x + "px";
    ripple.style.top = y + "px";
    ripple.classList.add("ripple");

    // Remove existing ripples
    const existingRipple = button.querySelector(".ripple");
    if (existingRipple) {
      existingRipple.remove();
    }

    button.appendChild(ripple);

    // Remove ripple after animation
    setTimeout(() => {
      ripple.remove();
    }, 600);
  }

  /**
   * Show toast notification
   */
  function showToast(message, type = "info") {
    if (window.showToast) {
      window.showToast(message, type);
    } else {
      console.log(`${type.toUpperCase()}: ${message}`);
    }
  }

  // Export functions for global access
  window.showInfoToast = (message) => showToast(message, "info");
  window.showSuccessToast = (message) => showToast(message, "success");
  window.showWarningToast = (message) => showToast(message, "warning");
  window.showErrorToast = (message) => showToast(message, "error");
})();
