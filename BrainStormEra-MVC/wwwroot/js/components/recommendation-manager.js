/**
 * Recommendation System Helper
 * Handles initialization and management of course recommendations
 */

class RecommendationManager {
  constructor() {
    this.baseUrl = window.location.origin;
    this.initialized = false;
  }

  /**
   * Initialize recommendations if needed
   */
  async initializeIfNeeded() {
    try {
      // First check stats to see if we need initialization
      const stats = await this.getStats();

      if (
        stats.success &&
        stats.stats.featuredCourses === 0 &&
        stats.stats.totalActiveCourses > 0
      ) {

        await this.initialize();
        return true;
      }

      return false;
    } catch (error) {

      return false;
    }
  }

  /**
   * Get recommendation system statistics
   */
  async getStats() {
    const response = await fetch(
      `${this.baseUrl}/Home/GetRecommendationStats`,
      {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
      }
    );

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    return await response.json();
  }

  /**
   * Initialize the recommendation system
   */
  async initialize() {
    try {
      const response = await fetch(
        `${this.baseUrl}/Home/InitializeRecommendations`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            RequestVerificationToken: this.getAntiForgeryToken(),
          },
        }
      );

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const result = await response.json();

      if (result.success) {

        this.initialized = true;

        // Refresh the page to show updated recommendations
        if (window.location.pathname.includes("LearnerDashboard")) {
          window.location.reload();
        }
      } else {

      }

      return result;
    } catch (error) {

      return { success: false, message: error.message };
    }
  }

  /**
   * Get anti-forgery token for POST requests
   */
  getAntiForgeryToken() {
    const token = document.querySelector(
      'input[name="__RequestVerificationToken"]'
    );
    return token ? token.value : "";
  }

  /**
   * Show a user-friendly notification about recommendation status
   */
  showRecommendationStatus(stats) {
    if (!stats.success) return;

    const { totalActiveCourses, featuredCourses, coursesWithEnrollments } =
      stats.stats;

    if (totalActiveCourses === 0) {
      this.showNotification(
        "No courses available yet. Please check back later!",
        "info"
      );
    } else if (featuredCourses === 0) {
      this.showNotification(
        "Setting up personalized recommendations for you...",
        "info"
      );
    } else {

    }
  }

  /**
   * Show notification to user
   */
  showNotification(message, type = "info") {
    // Create notification element if it doesn't exist
    let notification = document.getElementById("recommendation-notification");

    if (!notification) {
      notification = document.createElement("div");
      notification.id = "recommendation-notification";
      notification.className =
        "alert alert-dismissible fade show position-fixed";
      notification.style.cssText =
        "top: 20px; right: 20px; z-index: 9999; max-width: 400px;";
      document.body.appendChild(notification);
    }

    // Set notification content and type
    notification.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
    notification.innerHTML = `
            <i class="fas fa-info-circle me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;

    // Auto-hide after 5 seconds
    setTimeout(() => {
      if (notification && notification.parentNode) {
        notification.remove();
      }
    }, 5000);
  }
}

// Global recommendation manager instance
window.recommendationManager = new RecommendationManager();

// Auto-initialize on learner dashboard
document.addEventListener("DOMContentLoaded", async function () {
  if (window.location.pathname.includes("LearnerDashboard")) {
    // Check if recommendations section is empty
    const recommendedCoursesSection = document.querySelector(
      ".dashboard-card-body"
    );
    const emptyState = recommendedCoursesSection?.querySelector(".empty-state");

    if (emptyState) {

      const initialized =
        await window.recommendationManager.initializeIfNeeded();

      // Hide loading spinner after a reasonable time
      setTimeout(() => {
        const spinner = emptyState.querySelector(".spinner-border");
        const loadingText = emptyState.querySelector(".text-muted.small");

        if (spinner) spinner.style.display = "none";
        if (loadingText) {
          if (initialized) {
            loadingText.textContent =
              "Recommendations have been set up! Please refresh the page.";
            loadingText.classList.remove("text-muted");
            loadingText.classList.add("text-success");

            // Add refresh button
            const refreshBtn = document.createElement("button");
            refreshBtn.className = "btn btn-success btn-sm mt-2";
            refreshBtn.innerHTML =
              '<i class="fas fa-sync me-1"></i>Refresh Page';
            refreshBtn.onclick = () => window.location.reload();
            loadingText.parentNode.appendChild(refreshBtn);
          } else {
            loadingText.textContent =
              "Recommendations will be available soon. Meanwhile, explore our courses below!";
          }
        }
      }, 3000);
    }

    // Always get and show stats for debugging
    try {
      const stats = await window.recommendationManager.getStats();
      console.log("Recommendation Stats:", stats);
      window.recommendationManager.showRecommendationStatus(stats);
    } catch (error) {
      console.log(
        "Could not get recommendation stats (user may not be authenticated)"
      );
    }
  }
});
