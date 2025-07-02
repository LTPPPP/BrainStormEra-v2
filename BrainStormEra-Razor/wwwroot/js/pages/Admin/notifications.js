// Admin Notifications Page JavaScript
class AdminNotifications {
  constructor() {
    this.antiForgeryToken = $('input[name="__RequestVerificationToken"]').val();
    this.init();
  }

  init() {
    this.bindEvents();
    this.setupSignalR();
    this.initializeComponents();
  }

  bindEvents() {
    // Mark as read buttons
    $(document).on("click", ".mark-read-btn", (e) => {
      const notificationId = $(e.target)
        .closest("button")
        .data("notification-id");
      this.markAsRead(notificationId);
    });

    // Mark all as read button
    $("#markAllRead").on("click", () => {
      this.markAllAsRead();
    });

    // Refresh notifications button
    $("#refreshNotifications").on("click", () => {
      this.refreshNotifications();
    });

    // Delete notification buttons
    $(document).on("click", ".delete-btn", (e) => {
      const notificationId = $(e.target)
        .closest("button")
        .data("notification-id");
      this.deleteNotification(notificationId);
    });

    // Edit notification buttons
    $(document).on("click", ".edit-btn", (e) => {
      const notificationId = $(e.target)
        .closest("button")
        .data("notification-id");
      this.showEditModal(notificationId);
    });

    // Load more button
    $("#loadMore").on("click", (e) => {
      const page = $(e.target).data("page");
      this.loadMoreNotifications(page);
    });

    // Create notification form
    $("#createNotificationForm").on("submit", (e) => {
      e.preventDefault();
      this.createNotification();
    });

    // Edit notification form
    $("#editNotificationForm").on("submit", (e) => {
      e.preventDefault();
      this.updateNotification();
    });

    // Target type change
    $("#targetType").on("change", (e) => {
      this.handleTargetTypeChange($(e.target).val());
    });

    // User search
    $("#targetUserId").on(
      "input",
      debounce((e) => {
        this.searchUsers($(e.target).val());
      }, 300)
    );
  }

  setupSignalR() {
    // Check if SignalR is available
    if (typeof signalR === "undefined") {
      console.error(
        "SignalR is not loaded. Please ensure the SignalR library is included before this script."
      );
      return;
    }

    // Initialize SignalR connection for real-time updates
    const connection = new signalR.HubConnectionBuilder()
      .withUrl("/notificationHub")
      .configureLogging(signalR.LogLevel.Information)
      .build();

    connection
      .start()
      .then(() => {
        console.log("SignalR Connected for Admin Notifications");
      })
      .catch((err) => {
        console.error("SignalR Connection Error:", err);
      });

    // Handle new notifications
    connection.on("ReceiveNotification", (notification) => {
      this.handleNewNotification(notification);
    });

    // Handle notification updates
    connection.on("NotificationUpdated", (notification) => {
      this.handleUpdatedNotification(notification);
    });

    // Handle unread count updates
    connection.on("UpdateUnreadCount", (count) => {
      this.updateUnreadCountDisplay(count);
    });
  }

  initializeComponents() {
    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(
      document.querySelectorAll('[data-bs-toggle="tooltip"]')
    );
    tooltipTriggerList.map(function (tooltipTriggerEl) {
      return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Initialize target type visibility
    this.handleTargetTypeChange($("#targetType").val());
  }

  async markAsRead(notificationId) {
    try {
      const response = await fetch("/Admin/Notifications?handler=MarkAsRead", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken: this.antiForgeryToken,
        },
        body: JSON.stringify(notificationId),
      });

      const result = await response.json();

      if (result.success) {
        // Update UI
        const notificationItem = $(
          `.notification-item[data-notification-id="${notificationId}"]`
        );
        notificationItem.removeClass("unread");
        notificationItem.find(".mark-read-btn").remove();
        notificationItem.find('.badge.bg-primary:contains("New")').remove();

        // Update unread count
        this.updateUnreadCount();
        this.showToast("Notification marked as read", "success");
      } else {
        this.showToast(
          result.message || "Failed to mark notification as read",
          "error"
        );
      }
    } catch (error) {
      console.error("Error marking notification as read:", error);
      this.showToast(
        "An error occurred while marking notification as read",
        "error"
      );
    }
  }

  async markAllAsRead() {
    try {
      const response = await fetch(
        "/Admin/Notifications?handler=MarkAllAsRead",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            RequestVerificationToken: this.antiForgeryToken,
          },
        }
      );

      const result = await response.json();

      if (result.success) {
        // Update UI
        $(".notification-item.unread").removeClass("unread");
        $(".mark-read-btn").remove();
        $('.badge.bg-primary:contains("New")').remove();
        $("#markAllRead").hide();

        // Update unread count
        this.updateUnreadCountDisplay(0);
        this.showToast("All notifications marked as read", "success");
      } else {
        this.showToast(
          result.message || "Failed to mark all notifications as read",
          "error"
        );
      }
    } catch (error) {
      console.error("Error marking all notifications as read:", error);
      this.showToast(
        "An error occurred while marking all notifications as read",
        "error"
      );
    }
  }

  async deleteNotification(notificationId) {
    if (!confirm("Are you sure you want to delete this notification?")) {
      return;
    }

    try {
      const response = await fetch("/Admin/Notifications?handler=Delete", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken: this.antiForgeryToken,
        },
        body: JSON.stringify(notificationId),
      });

      const result = await response.json();

      if (result.success) {
        // Remove from UI
        const notificationItem = $(
          `.notification-item[data-notification-id="${notificationId}"]`
        );
        notificationItem.fadeOut(300, function () {
          $(this).remove();

          // Check if list is empty
          if ($(".notification-item").length === 0) {
            $("#notificationsList").html(`
                            <div class="empty-state">
                                <i class="fas fa-bell-slash"></i>
                                <h5>Your inbox is empty</h5>
                                <p>No notifications to display. You'll receive updates here as they become available.</p>
                            </div>
                        `);
          }
        });

        // Update unread count
        this.updateUnreadCount();
        this.showToast("Notification deleted successfully", "success");
      } else {
        this.showToast(
          result.message || "Failed to delete notification",
          "error"
        );
      }
    } catch (error) {
      console.error("Error deleting notification:", error);
      this.showToast("An error occurred while deleting notification", "error");
    }
  }

  async showEditModal(notificationId) {
    try {
      const response = await fetch(
        `/Admin/Notifications?handler=NotificationForEdit&notificationId=${notificationId}`
      );
      const result = await response.json();

      if (result.success && result.notification) {
        const notification = result.notification;

        // Populate edit form
        $("#editNotificationId").val(notification.notificationId);
        $("#editNotificationTitle").val(notification.title);
        $("#editNotificationContent").val(notification.content);
        $("#editNotificationType").val(notification.type || "General");
        $("#editRecipientUser").val(notification.recipientUserName);

        // Show modal
        $("#editNotificationModal").modal("show");
      } else {
        this.showToast(
          result.message || "Failed to load notification for editing",
          "error"
        );
      }
    } catch (error) {
      console.error("Error loading notification for edit:", error);
      this.showToast("An error occurred while loading notification", "error");
    }
  }

  async createNotification() {
    try {
      const formData = new FormData(
        document.getElementById("createNotificationForm")
      );

      const response = await fetch("/Admin/Notifications?handler=Create", {
        method: "POST",
        headers: {
          RequestVerificationToken: this.antiForgeryToken,
        },
        body: formData,
      });

      if (response.ok) {
        $("#createNotificationModal").modal("hide");
        this.showToast("Notification created successfully!", "success");
        this.refreshNotifications();
        document.getElementById("createNotificationForm").reset();
      } else {
        this.showToast("Failed to create notification", "error");
      }
    } catch (error) {
      console.error("Error creating notification:", error);
      this.showToast("An error occurred while creating notification", "error");
    }
  }

  async updateNotification() {
    try {
      const formData = new FormData(
        document.getElementById("editNotificationForm")
      );

      const response = await fetch("/Admin/Notifications?handler=Edit", {
        method: "POST",
        headers: {
          RequestVerificationToken: this.antiForgeryToken,
        },
        body: formData,
      });

      if (response.ok) {
        $("#editNotificationModal").modal("hide");
        this.showToast("Notification updated successfully!", "success");
        this.refreshNotifications();
      } else {
        this.showToast("Failed to update notification", "error");
      }
    } catch (error) {
      console.error("Error updating notification:", error);
      this.showToast("An error occurred while updating notification", "error");
    }
  }

  async refreshNotifications() {
    try {
      const response = await fetch(
        "/Admin/Notifications?handler=Notifications&page=1&pageSize=10"
      );
      const result = await response.json();

      if (result.success) {
        this.updateNotificationsList(result.notifications);
        this.updateUnreadCountDisplay(result.unreadCount);
        this.showToast("Notifications refreshed", "info");
      } else {
        this.showToast(
          result.message || "Failed to refresh notifications",
          "error"
        );
      }
    } catch (error) {
      console.error("Error refreshing notifications:", error);
      this.showToast(
        "An error occurred while refreshing notifications",
        "error"
      );
    }
  }

  async loadMoreNotifications(page) {
    try {
      const response = await fetch(
        `/Admin/Notifications?handler=Notifications&page=${page}&pageSize=10`
      );
      const result = await response.json();

      if (result.success && result.notifications.length > 0) {
        result.notifications.forEach((notification) => {
          const notificationHtml = this.createNotificationHtml(notification);
          $("#notificationsList").append(notificationHtml);
        });

        // Update load more button
        if (result.hasNextPage) {
          $("#loadMore").data("page", result.currentPage + 1);
        } else {
          $("#loadMore").parent().remove();
        }
      } else {
        $("#loadMore").parent().remove();
      }
    } catch (error) {
      console.error("Error loading more notifications:", error);
      this.showToast(
        "An error occurred while loading more notifications",
        "error"
      );
    }
  }

  async searchUsers(searchTerm) {
    try {
      const response = await fetch(
        `/Admin/Notifications?handler=SearchUsers&searchTerm=${encodeURIComponent(
          searchTerm
        )}`
      );
      const users = await response.json();

      // Create dropdown with user suggestions
      const dropdown = $('<div class="user-suggestions"></div>');
      users.forEach((user) => {
        const userOption = $(`
                    <div class="user-suggestion" data-user-id="${user.id}">
                        <strong>${user.name}</strong><br>
                        <small>${user.email}</small>
                    </div>
                `);
        dropdown.append(userOption);
      });

      // Replace existing dropdown
      $(".user-suggestions").remove();
      $("#targetUserId").after(dropdown);

      // Handle user selection
      $(".user-suggestion").on("click", function () {
        const userId = $(this).data("user-id");
        const userName = $(this).find("strong").text();
        $("#targetUserId").val(userName).data("user-id", userId);
        $(".user-suggestions").remove();
      });
    } catch (error) {
      console.error("Error searching users:", error);
    }
  }

  handleTargetTypeChange(targetType) {
    const targetUserContainer = $("#targetUserContainer");
    const targetUserId = $("#targetUserId");

    switch (targetType) {
      case "0": // Specific User
        targetUserContainer.show();
        targetUserId.attr("placeholder", "Search for user...");
        break;
      case "1": // Multiple Users
        targetUserContainer.show();
        targetUserId.attr(
          "placeholder",
          "Search for users (separate with commas)..."
        );
        break;
      case "2": // Course
        targetUserContainer.show();
        targetUserId.attr("placeholder", "Search for course...");
        break;
      case "3": // Role
        targetUserContainer.show();
        targetUserId.attr(
          "placeholder",
          "Enter role (admin, instructor, learner)..."
        );
        break;
      case "4": // All Users
        targetUserContainer.hide();
        break;
      default:
        targetUserContainer.show();
        break;
    }
  }

  updateNotificationsList(notifications) {
    if (notifications.length === 0) {
      $("#notificationsList").html(`
                <div class="empty-state">
                    <i class="fas fa-bell-slash"></i>
                    <h5>Your inbox is empty</h5>
                    <p>No notifications to display. You'll receive updates here as they become available.</p>
                </div>
            `);
      return;
    }

    const notificationsHtml = notifications
      .map((notification) => this.createNotificationHtml(notification))
      .join("");

    $("#notificationsList").html(notificationsHtml);
  }

  createNotificationHtml(notification) {
    const isUnread = !notification.isRead;
    const badges = [];

    if (notification.notificationType) {
      badges.push(
        `<span class="badge bg-secondary me-2">${notification.notificationType}</span>`
      );
    }

    if (isUnread) {
      badges.push(`<span class="badge bg-primary me-2">New</span>`);
    }

    const courseReference = notification.course
      ? `<div class="course-reference">
                <i class="fas fa-graduation-cap me-1"></i>
                <span class="text-muted">Related to: <strong>${notification.course.courseName}</strong></span>
            </div>`
      : "";

    const markReadBtn = isUnread
      ? `<button class="btn btn-sm mark-read-btn" data-notification-id="${notification.notificationId}" title="Mark as read">
                <i class="fas fa-check"></i>
            </button>`
      : "";

    return `
            <div class="list-group-item notification-item ${
              isUnread ? "unread" : ""
            }" data-notification-id="${notification.notificationId}">
                <div class="d-flex w-100 justify-content-between align-items-start">
                    <div class="notification-content flex-grow-1">
                        <div class="d-flex justify-content-between align-items-start mb-2">
                            <h6 class="mb-0 fw-bold notification-title">${
                              notification.notificationTitle
                            }</h6>
                            <div class="notification-meta d-flex align-items-center">
                                ${badges.join("")}
                                <small class="text-muted">${new Date(
                                  notification.notificationCreatedAt
                                ).toLocaleDateString("en-US", {
                                  month: "short",
                                  day: "numeric",
                                  year: "numeric",
                                  hour: "2-digit",
                                  minute: "2-digit",
                                })}</small>
                            </div>
                        </div>
                        <p class="mb-2 notification-message">${
                          notification.notificationContent
                        }</p>
                        ${courseReference}
                    </div>
                    <div class="notification-actions ms-3 d-flex">
                        ${markReadBtn}
                        <button class="btn btn-sm edit-btn" data-notification-id="${
                          notification.notificationId
                        }" title="Edit notification">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button class="btn btn-sm delete-btn" data-notification-id="${
                          notification.notificationId
                        }" title="Delete notification">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </div>
            </div>
        `;
  }

  handleNewNotification(notification) {
    // Add new notification to the top of the list
    const notificationHtml = this.createNotificationHtml(notification);

    // Remove empty state if it exists
    $(".empty-state").remove();

    $("#notificationsList").prepend(notificationHtml);

    // Show toast for new notification
    this.showToast(`New notification: ${notification.title}`, "info");

    // Update unread count
    this.updateUnreadCount();
  }

  handleUpdatedNotification(notification) {
    // Find and update the existing notification
    const existingNotification = $(
      `.notification-item[data-notification-id="${notification.id}"]`
    );
    if (existingNotification.length) {
      existingNotification.find(".notification-title").text(notification.title);
      existingNotification
        .find(".notification-message")
        .text(notification.content);

      // Update type badge if it exists
      const typeBadge = existingNotification.find(".badge.bg-secondary");
      if (typeBadge.length && notification.type) {
        typeBadge.text(notification.type);
      }

      this.showToast("Notification updated", "info");
    }
  }

  async updateUnreadCount() {
    try {
      const response = await fetch("/Admin/Notifications?handler=UnreadCount");
      const result = await response.json();
      this.updateUnreadCountDisplay(result.count);
    } catch (error) {
      console.error("Error updating unread count:", error);
    }
  }

  updateUnreadCountDisplay(count) {
    $(".stat-number").text(count);

    const badgeText = count > 0 ? `${count} New` : "";
    const existingBadge = $(".card-header .badge.bg-danger");

    if (count > 0) {
      if (existingBadge.length) {
        existingBadge.text(badgeText);
      } else {
        $(".card-header h5").append(
          `<span class="badge bg-danger ms-2">${badgeText}</span>`
        );
      }
      $("#markAllRead").show();
    } else {
      existingBadge.remove();
      $("#markAllRead").hide();
    }
  }

  showToast(message, type = "info") {
    // Use the global toast function if available
    if (typeof showToast === "function") {
      showToast(message, type);
    } else {
      // Fallback to console log
      console.log(`${type.toUpperCase()}: ${message}`);
    }
  }
}

// Utility function for debouncing
function debounce(func, wait, immediate) {
  let timeout;
  return function executedFunction() {
    const context = this;
    const args = arguments;
    const later = function () {
      timeout = null;
      if (!immediate) func.apply(context, args);
    };
    const callNow = immediate && !timeout;
    clearTimeout(timeout);
    timeout = setTimeout(later, wait);
    if (callNow) func.apply(context, args);
  };
}

// Initialize when DOM is ready
$(document).ready(function () {
  window.adminNotifications = new AdminNotifications();
});
