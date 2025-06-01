// Header notification functionality
document.addEventListener("DOMContentLoaded", function () {
  // DOM Elements
  const notificationBadge = document.getElementById("notificationBadge");
  const notificationDropdown = document.querySelector(".notification-dropdown");
  const userAvatar = document.querySelector(".user-avatar");
  const actionButtons = document.querySelectorAll(".btn_login, .btn_register");

  // Function to update notification badge
  function updateNotificationBadge(count) {
    if (notificationBadge) {
      if (count > 0) {
        notificationBadge.textContent = count > 9 ? "9+" : count;
        notificationBadge.classList.add("show");
      } else {
        notificationBadge.classList.remove("show");
      }
    }
  }

  // Function to add notification to dropdown
  function addNotificationToDropdown(notification) {
    if (!notificationDropdown) return;

    // Find the notification list container
    const listContainer = notificationDropdown.querySelector(
      ".notification-list-container"
    );
    if (!listContainer) return;

    // Remove empty state if exists
    const emptyItem = listContainer.querySelector(".notification-empty");
    if (emptyItem) {
      emptyItem.remove();
    }

    const notificationItem = document.createElement("li");
    notificationItem.classList.add("notification-item");
    if (!notification.isRead) {
      notificationItem.classList.add("unread");
    }

    notificationItem.innerHTML = `
      <div class="dropdown-item notification-content">
        <div class="d-flex">
          <div class="notification-icon">
            <i class="fas fa-bell text-primary"></i>
          </div>
          <div class="flex-grow-1">
            <h6 class="mb-1">${notification.title}</h6>
            <p class="mb-1 text-muted small">${notification.message}</p>
            <div class="notification-time text-muted small">${
              notification.time || "Just now"
            }</div>
          </div>
        </div>
      </div>
    `;

    // Add to the beginning of the list container
    listContainer.insertBefore(notificationItem, listContainer.firstChild);
  }

  // Initialize for logged in users
  if (userAvatar) {
    // User is logged in, notification system will handle real-time updates
    // The NotificationSystem class will call updateNotificationBadge and addNotificationToDropdown

    // Make functions globally available for NotificationSystem
    window.updateNotificationBadge = updateNotificationBadge;
    window.addNotificationToDropdown = addNotificationToDropdown;
  }

  // Expose functions globally
  window.updateNotificationBadge = updateNotificationBadge;

  // Add ripple effect to login and register buttons
  actionButtons.forEach((button) => {
    button.addEventListener("mousedown", createRippleEffect);
  });

  function createRippleEffect(event) {
    const button = event.currentTarget;
    const rect = button.getBoundingClientRect();
    const size = Math.max(rect.width, rect.height);
    const x = event.clientX - rect.left - size / 2;
    const y = event.clientY - rect.top - size / 2;

    const ripple = document.createElement("span");
    ripple.style.cssText = `
      position: absolute;
      width: ${size}px;
      height: ${size}px;
      left: ${x}px;
      top: ${y}px;
      background: rgba(255, 255, 255, 0.3);
      border-radius: 50%;
      transform: scale(0);
      animation: ripple 0.6s linear;
      pointer-events: none;
    `;

    button.style.position = "relative";
    button.style.overflow = "hidden";
    button.appendChild(ripple);

    setTimeout(() => {
      ripple.remove();
    }, 600);
  }

  // Add CSS for ripple animation if not already present
  if (!document.querySelector("#ripple-styles")) {
    const style = document.createElement("style");
    style.id = "ripple-styles";
    style.textContent = `
      @keyframes ripple {
        to {
          transform: scale(4);
          opacity: 0;
        }
      }
    `;
    document.head.appendChild(style);
  }
});
