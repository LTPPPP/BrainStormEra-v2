/**
 * Instructor Dashboard JavaScript
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

    // Load analytics data
    loadAnalyticsData();
  }

  /**
   * Cache DOM elements for better performance
   */
  function cacheElements() {
    elements.markAllReadBtn = document.querySelector(".mark-all-read");
    elements.statItems = document.querySelectorAll(".stat-item");
    elements.createCourseBtn = document.querySelector(".create-course-btn");
    elements.editButtons = document.querySelectorAll(".edit-btn");
    elements.chartPlaceholder = document.querySelector(".chart-placeholder");
    elements.statValues = document.querySelectorAll(".stat-value");
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

    // Add ripple effect to buttons
    const buttons = document.querySelectorAll(".btn");
    buttons.forEach((button) => {
      button.addEventListener("click", createRippleEffect);
    });

    // Create course button - now links to actual CreateCourse action

    // Edit course buttons
    if (elements.editButtons) {
      elements.editButtons.forEach((button) => {
        button.addEventListener("click", function (e) {
          e.preventDefault();
          showToast("Coming soon: Course editing functionality");
        });
      });
    }
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
          const numericValue = parseFloat(targetValue.replace("$", ""));
          animateValue(statValue, 0, numericValue, 1500, "$");
        } else {
          // Handle numeric values
          const numericValue = parseInt(targetValue, 10);
          animateValue(statValue, 0, numericValue, 1500);
        }
      });
    }
  }

  /**
   * Load analytics data from server
   */
  function loadAnalyticsData() {
    if (!elements.chartPlaceholder) return;

    // Show loading state
    elements.chartPlaceholder.innerHTML = `
      <div class="chart-content">
        <div class="loading-analytics">
          <i class="fas fa-spinner fa-spin me-2"></i>
          Loading analytics data...
        </div>
      </div>
    `;

    // Fetch analytics data from server
    fetch('/Instructor/GetAnalyticsData')
      .then(response => {
        if (!response.ok) throw new Error('Failed to load analytics');
        return response.json();
      })
      .then(data => {
        renderAnalyticsChart(data);
      })
      .catch(error => {
        console.error('Error loading analytics:', error);
        elements.chartPlaceholder.innerHTML = `
          <div class="chart-content">
            <div class="error-state">
              <i class="fas fa-exclamation-triangle text-warning me-2"></i>
              Unable to load analytics data
            </div>
          </div>
        `;
      });
  }

  /**
   * Render analytics chart with real data
   */
  function renderAnalyticsChart(data) {
    if (!elements.chartPlaceholder || !data || !data.courses) return;

    const courses = data.courses;
    const maxStudents = Math.max(...courses.map(c => c.studentCount), 1);

    const chartBars = courses.map(course => `
      <div class="chart-bar" style="height: ${(course.studentCount / maxStudents) * 100}%;" 
           title="${course.courseName}: ${course.studentCount} students">
        <span>${course.courseName}</span>
      </div>
    `).join('');

    elements.chartPlaceholder.innerHTML = `
      <div class="chart-content">
        <h6 class="chart-title">Course Enrollment Analytics</h6>
        <div class="chart-bars">
          ${chartBars}
        </div>
      </div>
    `;

    // Animate the bars
    const bars = document.querySelectorAll(".chart-bar");
    bars.forEach((bar, index) => {
      setTimeout(() => {
        bar.classList.add("animate");
      }, index * 100);
    });
  }

  /**
   * Mark all notifications as read
   */
  function markAllNotificationsAsRead(e) {
    e.preventDefault();
    showToast("All notifications marked as read");
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

  /**
   * Animate a value counting up
   */
  function animateValue(
    element,
    start,
    end,
    duration,
    prefix = "",
    suffix = ""
  ) {
    if (start === end) return;

    const range = end - start;
    const increment = end > start ? 1 : -1;
    const stepTime = Math.abs(Math.floor(duration / range));
    let current = start;

    const timer = setInterval(() => {
      current += increment;
      // For currency values, show with 2 decimal places
      if (prefix === "$") {
        element.textContent = `${prefix}${current.toFixed(2)}${suffix}`;
      } else {
        element.textContent = `${prefix}${current}${suffix}`;
      }

      if (current === end) {
        clearInterval(timer);
      }
    }, stepTime);
  }

  /**
   * Show a toast notification
   */
  function showToast(message) {
    // Check if toast container exists, create if not
    let toastContainer = document.querySelector(".toast-container");
    if (!toastContainer) {
      toastContainer = document.createElement("div");
      toastContainer.className = "toast-container";
      document.body.appendChild(toastContainer);
    }

    // Create toast element
    const toast = document.createElement("div");
    toast.className = "toast";
    toast.textContent = message;
    toastContainer.appendChild(toast);

    // Show toast
    setTimeout(() => {
      toast.classList.add("show");
    }, 10);

    // Hide and remove toast after 3 seconds
    setTimeout(() => {
      toast.classList.remove("show");
      setTimeout(() => {
        toast.remove();
      }, 300);
    }, 3000);
  }
})();
