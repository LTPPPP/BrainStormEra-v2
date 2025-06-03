/**
 * Learner Dashboard JavaScript
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
    elements.notificationItems =
      document.querySelectorAll(".notification-item");
    elements.markAllReadBtn = document.querySelector(".mark-all-read");
    elements.courseCards = document.querySelectorAll(".course-card");
    elements.statItems = document.querySelectorAll(".stat-item");
  }

  /**
   * Set up event listeners
   */
  function setupEventListeners() {
    // Mark all notifications as read
    if (elements.markAllReadBtn) {
      elements.markAllReadBtn.addEventListener(
        "click",
        markAllNotificationsAsRead
      );
    }

    // Add hover effects to course cards
    elements.courseCards.forEach((card) => {
      card.addEventListener("mouseenter", handleCardHover);
      card.addEventListener("mouseleave", handleCardLeave);
    });

    // Add ripple effect to buttons
    const buttons = document.querySelectorAll(
      ".btn, .continue-btn, .enroll-btn"
    );
    buttons.forEach((button) => {
      button.addEventListener("click", createRippleEffect);
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

    // Add staggered entrance animation to course cards
    elements.courseCards.forEach((card, index) => {
      setTimeout(() => {
        card.style.opacity = "1";
        card.style.transform = "translateY(0)";
      }, 100 * index);
    });
  }

  /**
   * Mark all notifications as read
   */
  function markAllNotificationsAsRead(e) {
    e.preventDefault();

    elements.notificationItems.forEach((item) => {
      item.classList.remove("unread");
    });

    // Here you would typically make an API call to mark notifications as read in the database
  }

  /**
   * Handle card hover animation
   */
  function handleCardHover(e) {
    const card = this;
    const image = card.querySelector("img");

    if (image) {
      image.style.transform = "scale(1.05)";
    }
  }

  /**
   * Handle card leave animation
   */
  function handleCardLeave(e) {
    const card = this;
    const image = card.querySelector("img");

    if (image) {
      image.style.transform = "scale(1)";
    }
  }

  /**
   * Create ripple effect on button click
   */
  function createRippleEffect(e) {
    const button = this;

    // Remove existing ripples
    const ripple = button.querySelector(".ripple");
    if (ripple) {
      ripple.remove();
    }

    // Create ripple element
    const circle = document.createElement("span");
    circle.classList.add("ripple");
    button.appendChild(circle);

    // Set ripple position
    const rect = button.getBoundingClientRect();
    const size = Math.max(rect.width, rect.height);

    circle.style.width = circle.style.height = `${size}px`;

    const x = e.clientX - rect.left - size / 2;
    const y = e.clientY - rect.top - size / 2;

    circle.style.left = `${x}px`;
    circle.style.top = `${y}px`;

    // Remove after animation
    setTimeout(() => {
      circle.remove();
    }, 600);
  }
})();
