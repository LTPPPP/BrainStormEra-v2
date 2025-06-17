// Admin Dashboard JavaScript
document.addEventListener("DOMContentLoaded", function () {
  // Initialize dashboard
  initializeDashboard();

  // Setup event listeners
  setupEventListeners();
});

function initializeDashboard() {
  console.log("Admin Dashboard initialized");

  // Update login time display
  updateLoginTime();

  // Add hover effects to cards
  addCardHoverEffects();
}

function setupEventListeners() {
  // Action button handlers
  const actionButtons = document.querySelectorAll(".action-btn");
  actionButtons.forEach((button) => {
    button.addEventListener("click", handleActionClick);
  });

  // Logout confirmation
  const logoutBtn = document.querySelector(".btn-logout");
  if (logoutBtn) {
    logoutBtn.addEventListener("click", handleLogoutClick);
  }
}

function handleActionClick(event) {
  const buttonText = event.target.textContent.trim();

  switch (buttonText) {
    case "Manage Users":
      showFeatureAlert(
        "User Management",
        "User management feature will be available soon!"
      );
      break;
    case "Manage Courses":
      showFeatureAlert(
        "Course Management",
        "Course management feature will be available soon!"
      );
      break;
    case "System Settings":
      showFeatureAlert(
        "System Settings",
        "System settings feature will be available soon!"
      );
      break;
    case "View Reports":
      showFeatureAlert("Reports", "Reports feature will be available soon!");
      break;
    default:
      alert("Feature coming soon!");
  }
}

function handleLogoutClick(event) {
  const confirmed = confirm("Are you sure you want to logout?");
  if (!confirmed) {
    event.preventDefault();
    return false;
  }
  return true;
}

function showFeatureAlert(title, message) {
  alert(`${title}\n\n${message}`);
}

function updateLoginTime() {
  // This would typically fetch real-time data
  // For now, it's just a placeholder
  console.log("Login time updated");
}

function addCardHoverEffects() {
  const cards = document.querySelectorAll(".info-card");
  cards.forEach((card) => {
    card.addEventListener("mouseenter", function () {
      this.style.transform = "translateY(-2px)";
      this.style.boxShadow = "0 4px 8px rgba(0, 0, 0, 0.15)";
    });

    card.addEventListener("mouseleave", function () {
      this.style.transform = "translateY(0)";
      this.style.boxShadow = "0 2px 4px rgba(0, 0, 0, 0.1)";
    });
  });
}
