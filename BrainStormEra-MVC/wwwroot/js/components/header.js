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

  // Load notifications from server
  function loadNotifications() {
    // This would normally be an API call to get notifications
    // For demo purposes, we'll create some dummy notifications
    const dummyNotifications = [
      {
        id: 1,
        title: "New course available",
        message: "A new course on JavaScript has been added.",
        time: "2 hours ago",
        isRead: false,
      },
      {
        id: 2,
        title: "Your progress",
        message: "You've completed 50% of Python Basics course.",
        time: "Yesterday",
        isRead: true,
      },
      {
        id: 3,
        title: "Course Update",
        message: "Your enrolled course has new content available.",
        time: "3 days ago",
        isRead: false,
      },
    ];

    if (notificationDropdown) {
      // Clear the placeholder
      const emptyItem = notificationDropdown.querySelector(
        ".notification-empty"
      );
      if (emptyItem && dummyNotifications.length > 0) {
        emptyItem.remove();
      }

      // Add notifications to the dropdown
      // Find the last li element that contains a dropdown-divider
      const dropdownDividerLi = Array.from(notificationDropdown.children)
        .filter((li) => li.querySelector(".dropdown-divider"))
        .pop();

      dummyNotifications.forEach((notification) => {
        const notificationItem = document.createElement("li");
        notificationItem.classList.add("notification-item");
        if (!notification.isRead) {
          notificationItem.classList.add("unread");
        }

        notificationItem.innerHTML = `
          <div class="notification-content">
            <h6>${notification.title}</h6>
            <p>${notification.message}</p>
            <div class="notification-time">${notification.time}</div>
          </div>
        `;

        if (dropdownDividerLi) {
          notificationDropdown.insertBefore(
            notificationItem,
            dropdownDividerLi
          );
        } else {
          // Fallback: append to the end
          notificationDropdown.appendChild(notificationItem);
        }
      });

      // Update badge count (unread notifications)
      const unreadCount = dummyNotifications.filter((n) => !n.isRead).length;
      updateNotificationBadge(unreadCount);
    }
  }

  // Initialize
  if (userAvatar) {
    // User is logged in, load notifications
    loadNotifications();
  }

  // Expose functions globally
  window.updateNotificationBadge = updateNotificationBadge;

  // Add ripple effect to login and register buttons
  actionButtons.forEach((button) => {
    button.addEventListener("mousedown", createRippleEffect);
  });

  function createRippleEffect(event) {
    const button = this;

    // Remove existing ripples
    const existingRipple = button.querySelector(".ripple");
    if (existingRipple) {
      existingRipple.remove();
    }

    // Create ripple element
    const ripple = document.createElement("span");
    ripple.classList.add("ripple");
    button.appendChild(ripple);

    // Set ripple size and position
    const diameter = Math.max(button.clientWidth, button.clientHeight);
    const radius = diameter / 2;

    ripple.style.width = ripple.style.height = `${diameter}px`;

    const rect = button.getBoundingClientRect();
    const offsetX = event.clientX - rect.left;
    const offsetY = event.clientY - rect.top;

    ripple.style.left = `${offsetX - radius}px`;
    ripple.style.top = `${offsetY - radius}px`;
    ripple.classList.add("active");

    // Remove ripple after animation completes
    setTimeout(() => {
      ripple.remove();
    }, 600);
  }
});
