// Global notification system with SignalR
class NotificationSystem {
  constructor() {
    this.connection = null;
    this.isConnected = false;
    this.reconnectAttempts = 0;
    this.maxReconnectAttempts = 5;
    this.init();
  }

  async init() {
    await this.initializeSignalR();
    this.setupEventHandlers();
    this.updateUnreadCount();
  }

  async initializeSignalR() {
    try {
      // Check if SignalR is available
      if (typeof signalR === "undefined") {
        return;
      }

      this.connection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Connection event handlers
      this.connection.onreconnecting(() => {
        this.isConnected = false;
      });

      this.connection.onreconnected(() => {
        this.isConnected = true;
        this.reconnectAttempts = 0;
        this.joinGroups();
      });

      this.connection.onclose(async () => {
        this.isConnected = false;
        await this.handleReconnect();
      });

      // Message handlers
      this.connection.on("ReceiveNotification", (notification) => {
        this.handleNewNotification(notification);
      });

      this.connection.on("UpdateUnreadCount", (count) => {
        this.updateNotificationBadge(count);
      });

      // Start connection
      await this.connection.start();

      this.isConnected = true;
      this.joinGroups();
    } catch (err) {
      await this.handleReconnect();
    }
  }

  async handleReconnect() {
    if (this.reconnectAttempts < this.maxReconnectAttempts) {
      this.reconnectAttempts++;
      const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), 30000);

      setTimeout(async () => {
        try {
          await this.connection.start();

          this.isConnected = true;
          this.reconnectAttempts = 0;
          this.joinGroups();
        } catch (err) {
          await this.handleReconnect();
        }
      }, delay);
    } else {
      this.showReconnectMessage();
    }
  }

  joinGroups() {
    if (!this.isConnected) return;

    try {
      // Join role-specific group
      const userRole = document.querySelector(
        'meta[name="user-role"]'
      )?.content;
      if (userRole) {
        this.connection.invoke("JoinRoleGroup", userRole);
      }

      // Join course-specific groups if on course pages
      const courseId = document.querySelector(
        'meta[name="current-course"]'
      )?.content;
      if (courseId) {
        this.connection.invoke("JoinCourseGroup", courseId);
      }
    } catch (err) {}
  }

  handleNewNotification(notification) {
    // Show toast notification
    this.showToastNotification(notification);

    // Play sound
    this.playNotificationSound();

    // Trigger custom event for other parts of the app
    const event = new CustomEvent("newNotification", { detail: notification });
    document.dispatchEvent(event);
  }

  showToastNotification(notification) {
    // Create toast container if it doesn't exist
    let toastContainer = document.getElementById("global-toast-container");
    if (!toastContainer) {
      toastContainer = document.createElement("div");
      toastContainer.id = "global-toast-container";
      toastContainer.className = "position-fixed bottom-0 end-0 p-3";
      toastContainer.style.zIndex = "1070";
      document.body.appendChild(toastContainer);
    }

    const toastId = "toast-" + Date.now();
    const toastHtml = `
            <div id="${toastId}" class="toast show notification-toast" role="alert" aria-live="assertive" aria-atomic="true" data-bs-autohide="true" data-bs-delay="5000">
                <div class="toast-header">
                    <i class="fas fa-bell text-primary me-2"></i>
                    <strong class="me-auto">${this.escapeHtml(
                      notification.title
                    )}</strong>
                    <small class="text-muted">now</small>
                    <button type="button" class="btn-close" data-bs-dismiss="toast"></button>
                </div>
                <div class="toast-body">
                    ${this.escapeHtml(notification.content)}
                    ${
                      notification.type
                        ? `<br><small class="text-muted">Type: ${notification.type}</small>`
                        : ""
                    }
                </div>
            </div>
        `;

    toastContainer.insertAdjacentHTML("beforeend", toastHtml);

    // Auto remove toast after it's hidden
    const toastElement = document.getElementById(toastId);
    toastElement.addEventListener("hidden.bs.toast", () => {
      toastElement.remove();
    });

    // Initialize Bootstrap toast
    if (typeof bootstrap !== "undefined") {
      new bootstrap.Toast(toastElement);
    }
  }
  updateNotificationBadge(count) {
    // Fallback implementation for notification badges
    const badges = document.querySelectorAll(".notification-badge");
    badges.forEach((badge) => {
      if (count > 0) {
        badge.textContent = count > 99 ? "99+" : count;
        badge.style.display = "inline-block";
      } else {
        badge.style.display = "none";
      }
    });

    // Update page title
    this.updatePageTitle(count);
  }

  updatePageTitle(count) {
    const originalTitle = document.title.replace(/^\(\d+\)\s*/, "");
    if (count > 0) {
      document.title = `(${count}) ${originalTitle}`;
    } else {
      document.title = originalTitle;
    }
  }

  async updateUnreadCount() {
    try {
      const response = await fetch("/Notification/GetUnreadCount");
      if (response.ok) {
        const data = await response.json();
        this.updateNotificationBadge(data.count);
      }
    } catch (error) {}
  }

  playNotificationSound() {
    try {
      // Check if user has allowed audio
      if (localStorage.getItem("notificationSoundEnabled") !== "false") {
        const audio = new Audio("/sounds/notification.mp3");
        audio.volume = 0.3;
        audio.play().catch((e) => {});
      }
    } catch (error) {}
  }

  setupEventHandlers() {
    // Mark notification as read when clicked
    document.addEventListener("click", async (e) => {
      if (e.target.closest(".mark-notification-read")) {
        const notificationId = e.target.closest(".mark-notification-read")
          .dataset.notificationId;
        await this.markAsRead(notificationId);
      }
    });

    // Toggle notification sound
    document.addEventListener("click", (e) => {
      if (e.target.closest(".toggle-notification-sound")) {
        this.toggleNotificationSound();
      }
    });
  }

  async markAsRead(notificationId) {
    try {
      const response = await fetch("/Notification/MarkAsRead", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
        },
        body: `notificationId=${encodeURIComponent(notificationId)}`,
      });

      if (response.ok) {
        // The server will send updated count via SignalR
      }
    } catch (error) {}
  }

  toggleNotificationSound() {
    const isEnabled =
      localStorage.getItem("notificationSoundEnabled") !== "false";
    localStorage.setItem("notificationSoundEnabled", (!isEnabled).toString());

    const soundButton = document.querySelector(".toggle-notification-sound");
    if (soundButton) {
      const icon = soundButton.querySelector("i");
      if (isEnabled) {
        icon.className = "fas fa-volume-mute";
        soundButton.title = "Enable notification sound";
      } else {
        icon.className = "fas fa-volume-up";
        soundButton.title = "Disable notification sound";
      }
    }
  }

  showReconnectMessage() {
    const messageHtml = `
            <div class="alert alert-warning alert-dismissible fade show position-fixed top-0 start-50 translate-middle-x mt-3" style="z-index: 1080;">
                <i class="fas fa-exclamation-triangle me-2"></i>
                Connection lost. Please refresh the page to restore real-time notifications.
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;

    document.body.insertAdjacentHTML("afterbegin", messageHtml);
  }

  escapeHtml(text) {
    const div = document.createElement("div");
    div.textContent = text;
    return div.innerHTML;
  }

  // Public methods for external use
  async sendNotificationToUser(
    userId,
    title,
    content,
    type = null,
    courseId = null
  ) {
    try {
      const response = await fetch("/Notification/SendToUser", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
        },
        body: new URLSearchParams({
          targetUserId: userId,
          title: title,
          content: content,
          type: type || "",
          courseId: courseId || "",
        }),
      });

      return response.ok;
    } catch (error) {
      return false;
    }
  }

  joinCourseGroup(courseId) {
    if (this.isConnected && this.connection) {
      this.connection.invoke("JoinCourseGroup", courseId);
    }
  }

  leaveCourseGroup(courseId) {
    if (this.isConnected && this.connection) {
      this.connection.invoke("LeaveCourseGroup", courseId);
    }
  }
}

// Initialize notification system when DOM is loaded
document.addEventListener("DOMContentLoaded", () => {
  // Check if user is authenticated
  const isAuthenticated =
    document.querySelector('meta[name="user-authenticated"]')?.content ===
    "true";

  if (isAuthenticated) {
    window.notificationSystem = new NotificationSystem();
  }
});

// Expose notification system globally for other scripts
window.NotificationSystem = NotificationSystem;
