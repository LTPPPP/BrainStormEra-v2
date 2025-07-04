// Admin Notifications Page JavaScript
$(document).ready(function () {
  // Initialize the notification system
  const adminNotifications = new AdminNotifications();
});

class AdminNotifications {
  constructor() {
    this.antiForgeryToken = $('input[name="__RequestVerificationToken"]').val();
    this.init();
  }

  init() {
    this.setupEventHandlers();
    this.setupSignalR();
    this.updateHeaderNotificationBell();
    this.initializeNotificationStates();
  }

  setupEventHandlers() {
    // Mark as read functionality
    $(document).on("click", ".mark-read-btn", (e) => {
      e.preventDefault();
      e.stopPropagation();
      const notificationId = $(e.currentTarget).data("notification-id");

      if (!notificationId) {
        showToast("Notification ID not found", "error");
        return;
      }

      this.markAsRead(notificationId, e.currentTarget);
    });

    // Delete notification functionality
    $(document).on("click", ".delete-btn", (e) => {
      e.preventDefault();
      e.stopPropagation();
      const element = e.currentTarget;
      const notificationId = $(element).data("notification-id");

      if (!notificationId) {
        showToast("Notification ID not found", "error");
        return;
      }

      this.showDeleteConfirmation(notificationId, element);
    });

    // Edit notification buttons
    $(document).on("click", ".edit-btn", (e) => {
      e.preventDefault();
      e.stopPropagation();
      const notificationId = $(e.currentTarget).data("notification-id");

      if (!notificationId) {
        showToast("Notification ID not found", "error");
        return;
      }

      this.showEditModal(notificationId);
    });

    // Load more functionality
    $(document).on("click", "#loadMore", (e) => {
      e.preventDefault();
      const page = $(e.currentTarget).data("page");
      this.loadMoreNotifications(page);
    });

    // Mark all as read functionality
    $(document).on("click", "#markAllRead", (e) => {
      e.preventDefault();
      this.markAllAsRead();
    });

    // Refresh notifications functionality
    $(document).on("click", "#refreshNotifications", (e) => {
      e.preventDefault();
      this.refreshNotifications();
    });

    // Edit notification buttons
    $(document).on("click", ".edit-btn", (e) => {
      e.preventDefault();
      const notificationId = $(e.currentTarget).data("notification-id");
      this.showEditModal(notificationId);
    });

    // Target type change
    $("#targetType").on("change", (e) => {
      this.handleTargetTypeChange($(e.target).val());
    });

    // User search
    $("#targetUserId").on(
      "input",
      this.debounce((e) => {
        this.searchUsers($(e.target).val());
      }, 300)
    );

    // Form validation before submit
    $("#createNotificationForm").on("submit", (e) => {
      if (!this.validateForm("#createNotificationForm")) {
        e.preventDefault();
        return false;
      }
      // Let the form submit normally
    });

    $("#editNotificationForm").on("submit", (e) => {
      if (!this.validateForm("#editNotificationForm")) {
        e.preventDefault();
        return false;
      }
      // Let the form submit normally
    });
  }

  setupSignalR() {
    // Check if SignalR is available
    if (typeof signalR === "undefined") {
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
        // SignalR connected
      })
      .catch((err) => {
        // SignalR connection error
      });

    // Handle incoming notifications
    connection.on("ReceiveNotification", (notification) => {
      this.handleNewNotification(notification);
    });

    // Store connection
    this.connection = connection;
  }

  initializeNotificationStates() {
    // Check for unread notifications
    const hasUnread = $(".notification-item.unread").length > 0;
    if (!hasUnread) {
      $("#markAllRead").hide();
    }
  }

  updateHeaderNotificationBell() {
    const unreadCount = $(".notification-item.unread").length;

    const bellIcon = $(".notification-bell-count");
    if (bellIcon.length) {
      if (unreadCount > 0) {
        bellIcon.text(unreadCount).show();
      } else {
        bellIcon.hide();
      }
    }

    // Update the unread count badge in the page header
    const headerBadge = $(".page-header .badge.bg-danger");
    if (headerBadge.length) {
      if (unreadCount > 0) {
        headerBadge.text(unreadCount + " New").show();
      } else {
        headerBadge.fadeOut(300);
      }
    }
  }

  debugNotificationStates() {
    $(".notification-item").each(function (index) {
      const $item = $(this);
      // Debug functionality removed
    });

    const totalUnread = $(".notification-item.unread").length;
    const totalRead = $(".notification-item.read").length;
  }

  // Debug function to manually test mark as read with database update
  async debugMarkAsRead(notificationId) {
    const button = $(
      `.mark-read-btn[data-notification-id="${notificationId}"]`
    );
    if (button.length === 0) {
      return;
    }

    await this.markAsRead(notificationId, button[0]);
  }

  // Debug function to check database state
  async debugCheckDatabaseState() {
    try {
      const response = await fetch("/Admin/Notifications?page=1&pageSize=50", {
        method: "GET",
      });

      if (response.ok) {
        window.location.reload();
      }
    } catch (error) {
      // Error occurred
    }
  }

  // Debug function to inspect DOM elements
  debugDOMElements() {
    const notifications = $(".notification-item");

    notifications.each(function (index) {
      const $item = $(this);
      const notificationId = $item.data("notification-id");
      const markReadBtn = $item.find(".mark-read-btn");
      const editBtn = $item.find(".edit-btn");
      const deleteBtn = $item.find(".delete-btn");
    });

    // Check if buttons are being created at all
    const allMarkReadBtns = $(".mark-read-btn");
    const allEditBtns = $(".edit-btn");
    const allDeleteBtns = $(".delete-btn");
  }

  // Debug function to manually trigger events
  debugTriggerEvent(notificationId, eventType = "mark-read") {
    let selector;
    switch (eventType) {
      case "mark-read":
        selector = `.mark-read-btn[data-notification-id="${notificationId}"]`;
        break;
      case "edit":
        selector = `.edit-btn[data-notification-id="${notificationId}"]`;
        break;
      case "delete":
        selector = `.delete-btn[data-notification-id="${notificationId}"]`;
        break;
      default:
        return;
    }

    const button = $(selector);

    if (button.length > 0) {
      button.trigger("click");
    }
  }

  async markAsRead(notificationId, buttonElement) {
    // Immediately update UI for better UX
    const notificationCard = $(buttonElement).closest(".notification-item");
    const originalClasses = notificationCard.attr("class");

    // Show loading state
    $(buttonElement)
      .prop("disabled", true)
      .html('<i class="fas fa-spinner fa-spin"></i>');

    // Apply immediate visual feedback
    notificationCard.css("transition", "all 0.3s ease");
    notificationCard.removeClass("unread").addClass("read");

    try {
      // Use form data instead of JSON for better compatibility
      const formData = new FormData();
      formData.append("notificationId", notificationId);

      const response = await fetch("/Admin/Notifications?handler=MarkAsRead", {
        method: "POST",
        headers: {
          RequestVerificationToken: this.antiForgeryToken,
        },
        body: formData,
      });

      if (response.ok) {
        const contentType = response.headers.get("content-type");

        if (contentType && contentType.includes("application/json")) {
          const result = await response.json();

          if (result.success) {
            // Remove the mark as read button with animation
            $(buttonElement).fadeOut(300, function () {
              $(this).remove();
            });

            // Remove the "New" badge if present with animation
            notificationCard
              .find(".badge.bg-primary")
              .fadeOut(300, function () {
                $(this).remove();
              });

            // Remove "Created by you" badge if present
            notificationCard.find(".badge.bg-info").fadeOut(300, function () {
              $(this).remove();
            });

            // Show success animation
            notificationCard.addClass("read-success");
            setTimeout(() => {
              notificationCard.removeClass("read-success");
            }, 1500);

            // Update notification bell count
            this.updateHeaderNotificationBell();

            // Update unread count in header
            const unreadCountElement = $(".badge.bg-danger");
            const currentUnreadCount =
              parseInt(unreadCountElement.text().replace(" New", "")) || 0;
            if (currentUnreadCount > 1) {
              unreadCountElement.text(currentUnreadCount - 1 + " New");
            } else {
              unreadCountElement.fadeOut(300);
            }

            // Show toast notification
            showToast(
              "Notification marked as read and saved to database",
              "success"
            );

            // Hide the "Mark All Read" button if no more unread notifications
            if ($(".notification-item.unread").length === 0) {
              $("#markAllRead").fadeOut(300);
            }
          } else {
            // Revert UI changes on error
            notificationCard.attr("class", originalClasses);
            $(buttonElement)
              .prop("disabled", false)
              .html('<i class="fas fa-check"></i>');
            showToast(
              result.message || "Error marking notification as read",
              "error"
            );
          }
        } else {
          throw new Error("Server returned non-JSON response");
        }
      } else {
        const errorText = await response.text();
        throw new Error(
          `Server returned status ${response.status}: ${errorText}`
        );
      }
    } catch (error) {
      // Revert UI changes on error
      notificationCard.attr("class", originalClasses);
      $(buttonElement)
        .prop("disabled", false)
        .html('<i class="fas fa-check"></i>');
      showToast(
        "Failed to mark notification as read: " + error.message,
        "error"
      );
    }
  }

  async markAllAsRead() {
    // Check if there are unread notifications
    const unreadNotifications = $(".notification-item.unread");

    if (unreadNotifications.length === 0) {
      showToast("No unread notifications to mark", "info");
      return;
    }

    // Show loading state
    const originalButtonHtml = $("#markAllRead").html();
    $("#markAllRead")
      .prop("disabled", true)
      .html('<i class="fas fa-spinner fa-spin"></i> Processing...');

    // Apply immediate visual feedback to all unread notifications
    unreadNotifications.each(function () {
      $(this).css("transition", "all 0.3s ease");
      $(this).removeClass("unread").addClass("read");

      // Hide the mark as read buttons with animation
      $(this).find(".mark-read-btn").fadeOut(300);

      // Hide the "New" badges with animation
      $(this).find(".badge.bg-primary").fadeOut(300);
    });

    try {
      const response = await fetch(
        "/Admin/Notifications?handler=MarkAllAsRead",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/x-www-form-urlencoded",
            RequestVerificationToken: this.antiForgeryToken,
          },
        }
      );

      if (response.ok) {
        const result = await response.json();

        if (result.success) {
          // Update UI
          $("#markAllRead").fadeOut(300);

          // Show success animation on all notifications
          $(".notification-item").addClass("read-success");
          setTimeout(() => {
            $(".notification-item").removeClass("read-success");
          }, 1500);

          // Update notification bell count
          this.updateHeaderNotificationBell();

          // Show toast notification
          showToast(
            "All notifications marked as read and saved to database",
            "success"
          );

          // Remove all mark as read buttons
          $(".mark-read-btn").remove();

          // Remove all "New" badges
          $(".badge.bg-primary").remove();
        } else {
          // Revert UI changes on error
          $(".notification-item.read")
            .removeClass("read")
            .addClass("unread")
            .find(".mark-read-btn")
            .fadeIn(300);

          $(".notification-item.unread").find(".badge.bg-primary").fadeIn(300);

          $("#markAllRead").prop("disabled", false).html(originalButtonHtml);

          showToast(
            result.message || "Error marking all notifications as read",
            "error"
          );
        }
      } else {
        throw new Error(
          `Server returned status ${response.status}: ${response.statusText}`
        );
      }
    } catch (error) {
      // Revert UI changes on error
      $(".notification-item.read")
        .removeClass("read")
        .addClass("unread")
        .find(".mark-read-btn")
        .fadeIn(300);

      $(".notification-item.unread").find(".badge.bg-primary").fadeIn(300);

      $("#markAllRead").prop("disabled", false).html(originalButtonHtml);

      showToast("Failed to mark all notifications as read", "error");
    }
  }

  showDeleteConfirmation(notificationId, buttonElement) {
    if (
      confirm(
        "Are you sure you want to delete this notification? This action cannot be undone."
      )
    ) {
      this.deleteNotification(notificationId, buttonElement);
    }
  }

  async deleteNotification(notificationId, buttonElement) {
    // Show loading state
    $(buttonElement)
      .prop("disabled", true)
      .html('<i class="fas fa-spinner fa-spin"></i>');

    try {
      const response = await fetch("/Admin/Notifications?handler=Delete", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken: this.antiForgeryToken,
        },
        body: JSON.stringify(notificationId),
      });

      if (response.ok) {
        const result = await response.json();
        if (result.success) {
          // Remove the notification with animation
          const notificationItem =
            $(buttonElement).closest(".notification-item");
          notificationItem.slideUp(400, function () {
            $(this).remove();

            // Check if notifications list is empty
            if ($("#notificationsList").children().length === 0) {
              $("#notificationsList").html(`
                <div class="empty-state">
                  <i class="fas fa-bell-slash"></i>
                  <h5>Your inbox is empty</h5>
                  <p>No notifications to display. You'll receive updates here as they become available.</p>
                </div>
              `);
            }
          });

          // Update notification bell count
          this.updateHeaderNotificationBell();

          // Show toast notification
          showToast("Notification deleted successfully", "success");
        } else {
          // Reset button state on error
          $(buttonElement)
            .prop("disabled", false)
            .html('<i class="fas fa-trash"></i>');

          showToast(result.message || "Error deleting notification", "error");
        }
      } else {
        throw new Error("Server returned an error");
      }
    } catch (error) {
      // Reset button state on error
      $(buttonElement)
        .prop("disabled", false)
        .html('<i class="fas fa-trash"></i>');

      showToast("Failed to delete notification", "error");
    }
  }

  async refreshNotifications() {
    // Show loading state
    const originalButtonHtml = $("#refreshNotifications").html();
    $("#refreshNotifications")
      .prop("disabled", true)
      .html('<i class="fas fa-spinner fa-spin"></i>');

    try {
      // Simply reload the page for refresh functionality
      window.location.reload();
    } catch (error) {
      showToast("Failed to refresh notifications", "error");

      // Reset button state
      $("#refreshNotifications")
        .prop("disabled", false)
        .html(originalButtonHtml);
    }
  }

  async showEditModal(notificationId) {
    try {
      const response = await fetch(
        `/Admin/Notifications?handler=NotificationForEdit&notificationId=${notificationId}`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json",
          },
        }
      );

      if (response.ok) {
        const result = await response.json();
        if (result.success && result.notification) {
          const notification = result.notification;

          // Populate the edit form
          $("#editNotificationId").val(notification.NotificationId);
          $("#editNotificationTitle").val(notification.Title);
          $("#editNotificationType").val(notification.Type || "General");
          $("#editRecipientUser").val(
            notification.RecipientUserName || "All Users"
          );
          $("#editNotificationContent").val(notification.Content);

          // Show the modal
          new bootstrap.Modal(
            document.getElementById("editNotificationModal")
          ).show();
        } else {
          showToast(
            result.message || "Failed to load notification details",
            "error"
          );
        }
      } else {
        throw new Error("Server returned an error");
      }
    } catch (error) {
      showToast("Failed to load notification details", "error");
    }
  }

  validateForm(formSelector) {
    const form = $(formSelector);
    let isValid = true;

    // Check required fields
    form.find("[required]").each(function () {
      if (!$(this).val()) {
        $(this).addClass("is-invalid");
        $(this).next(".invalid-feedback").text("This field is required");
        isValid = false;
      } else {
        $(this).removeClass("is-invalid");
        $(this).next(".invalid-feedback").text("");
      }
    });

    return isValid;
  }

  handleTargetTypeChange(targetType) {
    // Hide all target containers first
    $("#targetUserContainer").hide();
    $("#targetRoleContainer").hide();
    $("#targetCourseContainer").hide();

    // Show appropriate container based on target type
    switch (targetType) {
      case "0": // Specific User
        $("#targetUserContainer").show();
        break;
      case "1": // Multiple Users
        $("#targetUserContainer").show();
        break;
      case "2": // Course Students
        $("#targetCourseContainer").show();
        break;
      case "3": // Role
        $("#targetRoleContainer").show();
        break;
      case "4": // All Users
        // No additional fields needed
        break;
      default:
        break;
    }
  }

  async searchUsers(query) {
    if (!query || query.length < 2) {
      $(".user-suggestions").remove();
      return;
    }

    try {
      const response = await fetch(
        `/Admin/Notifications?handler=SearchUsers&searchTerm=${encodeURIComponent(
          query
        )}`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json",
          },
        }
      );

      if (response.ok) {
        const users = await response.json();

        // Remove existing suggestions
        $(".user-suggestions").remove();

        if (users.length > 0) {
          // Create suggestions dropdown
          const suggestionsHtml = `
            <div class="user-suggestions">
              ${users
                .map(
                  (user) => `
                <div class="user-suggestion" data-user-id="${user.id}">
                  <strong>${user.name}</strong>
                  <small>${user.email}</small>
                </div>
              `
                )
                .join("")}
            </div>
          `;

          $("#targetUserContainer").append(suggestionsHtml);

          // Add click handlers
          $(".user-suggestion").on("click", (e) => {
            const userId = $(e.currentTarget).data("user-id");
            const userName = $(e.currentTarget).find("strong").text();

            $("#targetUserId").val(userId);
            $(".user-suggestions").remove();
          });
        }
      }
    } catch (error) {
      // Error occurred during user search
    }
  }

  handleNewNotification(notification) {
    // Check if notification already exists
    if (
      $(`[data-notification-id="${notification.notificationId}"]`).length > 0
    ) {
      return;
    }

    // Create notification HTML
    const notificationHtml = this.createNotificationHtml(notification);

    // Add to top of list
    $("#notificationsList").prepend(notificationHtml);

    // Remove empty state if it exists
    $(".empty-state").remove();

    // Apply animation
    $(`[data-notification-id="${notification.notificationId}"]`).addClass(
      "new-notification"
    );
    setTimeout(() => {
      $(`[data-notification-id="${notification.notificationId}"]`).removeClass(
        "new-notification"
      );
    }, 1000);

    // Update counts
    const unreadCount = $(".notification-item.unread").length;
    if (unreadCount > 0) {
      $("#markAllRead").fadeIn(300);
      $(".badge.bg-danger")
        .text(unreadCount + " New")
        .fadeIn(300);
    }

    // Update bell icon
    this.updateHeaderNotificationBell();

    // Show toast notification
    showToast("New notification received", "info");
  }

  createNotificationHtml(notification) {
    return `
      <div class="list-group-item notification-item unread" data-notification-id="${
        notification.notificationId
      }">
        <div class="d-flex w-100 justify-content-between align-items-start">
          <div class="notification-content flex-grow-1">
            <div class="d-flex justify-content-between align-items-start mb-2">
              <h6 class="mb-0 fw-bold notification-title">${
                notification.notificationTitle
              }</h6>
              <div class="notification-meta d-flex align-items-center">
                ${
                  notification.notificationType
                    ? `<span class="badge bg-secondary me-2">${notification.notificationType}</span>`
                    : ""
                }
                ${
                  notification.createdBy === notification.currentUserId
                    ? `<span class="badge bg-info me-2">Created by you</span>`
                    : ""
                }
                <span class="badge bg-primary me-2">New</span>
                <small class="text-muted">${
                  notification.createdAtFormatted
                }</small>
              </div>
            </div>
            <p class="mb-2 notification-message">${
              notification.notificationContent
            }</p>
            ${
              notification.courseName
                ? `<div class="course-reference">
                <i class="fas fa-graduation-cap me-1"></i>
                <span class="text-muted">Related to: <strong>${notification.courseName}</strong></span>
              </div>`
                : ""
            }
          </div>
          <div class="notification-actions ms-3 d-flex">
            <button class="btn btn-sm mark-read-btn" data-notification-id="${
              notification.notificationId
            }" title="Mark as read">
              <i class="fas fa-check"></i>
            </button>
            ${
              notification.createdBy === notification.currentUserId
                ? `<button class="btn btn-sm edit-btn" data-notification-id="${notification.notificationId}" title="Edit notification">
                <i class="fas fa-edit"></i>
              </button>`
                : ""
            }
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

  async loadMoreNotifications(page) {
    // Show loading state
    const loadMoreBtn = $("#loadMore");
    const originalBtnHtml = loadMoreBtn.html();
    loadMoreBtn
      .prop("disabled", true)
      .html('<i class="fas fa-spinner fa-spin"></i> Loading...');

    try {
      const response = await fetch(
        `/Admin/Notifications?handler=LoadMoreNotifications&page=${page}`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json",
          },
        }
      );

      if (response.ok) {
        const result = await response.json();
        if (result.success) {
          // Append new notifications
          $("#notificationsList").append(result.html);

          // Update load more button
          if (result.hasNextPage) {
            loadMoreBtn.data("page", page + 1).html(originalBtnHtml);
          } else {
            loadMoreBtn.parent().fadeOut(300, function () {
              $(this).remove();
            });
          }

          // Apply animations
          $(".notification-item").addClass("new-notification");
          setTimeout(() => {
            $(".notification-item").removeClass("new-notification");
          }, 1000);
        } else {
          loadMoreBtn.html(originalBtnHtml);
          showToast(
            result.message || "Error loading more notifications",
            "error"
          );
        }
      } else {
        throw new Error("Server returned an error");
      }
    } catch (error) {
      loadMoreBtn.html(originalBtnHtml);
      showToast("Failed to load more notifications", "error");
    } finally {
      // Reset button state
      loadMoreBtn.prop("disabled", false);
    }
  }

  // Utility function for debouncing
  debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
      const later = () => {
        clearTimeout(timeout);
        func(...args);
      };
      clearTimeout(timeout);
      timeout = setTimeout(later, wait);
    };
  }
}
