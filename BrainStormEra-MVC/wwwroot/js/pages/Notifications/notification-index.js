// Notification Index Page JavaScript
class NotificationIndex {
  constructor() {
    this.init();
  }

  init() {
    this.setupEventHandlers();
    this.setupSignalR();
  }

  setupEventHandlers() {
    // Mark as read functionality
    $(document).on("click", ".mark-read-btn", (e) => {
      e.preventDefault();
      const notificationId = $(e.currentTarget).data("notification-id");
      this.markAsRead(notificationId, e.currentTarget);
    });

    // Delete notification functionality
    $(document).on("click", ".delete-btn", (e) => {
      e.preventDefault();
      const notificationId = $(e.currentTarget).data("notification-id");
      this.showDeleteConfirmation(notificationId, e.currentTarget);
    });

    // Load more functionality
    $(document).on("click", "#loadMore", (e) => {
      e.preventDefault();
      const page = $(e.currentTarget).data("page");
      this.loadMoreNotifications(page);
    }); // Mark all as read functionality
    $(document).on("click", "#markAllRead", (e) => {
      e.preventDefault();
      this.markAllAsRead();
    });

    // Refresh notifications functionality
    $(document).on("click", "#refreshNotifications", (e) => {
      e.preventDefault();
      this.refreshNotifications();
    });
  }

  async markAsRead(notificationId, buttonElement) {
    try {
      const response = await fetch("/Notification/MarkAsRead", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
          RequestVerificationToken: $(
            'input[name="__RequestVerificationToken"]'
          ).val(),
        },
        body: `notificationId=${encodeURIComponent(notificationId)}`,
      });

      if (response.ok) {
        const result = await response.json();
        if (result.success) {
          // Remove the mark as read button
          $(buttonElement).fadeOut(300, function () {
            $(this).remove();
          });

          // Update the notification card to show as read
          const notificationCard =
            $(buttonElement).closest(".notification-item");
          notificationCard.removeClass("unread").addClass("read");

          // Remove the "New" badge if present
          notificationCard.find(".badge.bg-primary").fadeOut(300, function () {
            $(this).remove();
          });

          this.showToast("Notification marked as read", "success");
          this.updateUnreadCount();
        } else {
          this.showToast("Failed to mark notification as read", "error");
        }
      } else {
        this.showToast("Error marking notification as read", "error");
      }
    } catch (error) {
      console.error("Error marking notification as read:", error);
      this.showToast("Error marking notification as read", "error");
    }
  }

  showDeleteConfirmation(notificationId, buttonElement) {
    // Create a modern confirmation modal
    const modal = $(`
            <div class="modal fade" id="deleteConfirmModal" tabindex="-1" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content">
                        <div class="modal-header border-0">
                            <h5 class="modal-title">
                                <i class="fas fa-exclamation-triangle text-warning me-2"></i>
                                Confirm Deletion
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <p class="mb-0">Are you sure you want to delete this notification? This action cannot be undone.</p>
                        </div>
                        <div class="modal-footer border-0">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
                                <i class="fas fa-times me-1"></i> Cancel
                            </button>
                            <button type="button" class="btn btn-danger" id="confirmDelete">
                                <i class="fas fa-trash me-1"></i> Delete
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `);

    $("body").append(modal);
    const bootstrapModal = new bootstrap.Modal(modal[0]);
    bootstrapModal.show();

    // Handle confirm delete
    modal.find("#confirmDelete").on("click", () => {
      this.deleteNotification(notificationId, buttonElement);
      bootstrapModal.hide();
    });

    // Clean up modal after hiding
    modal.on("hidden.bs.modal", () => {
      modal.remove();
    });
  }

  async deleteNotification(notificationId, buttonElement) {
    try {
      const response = await fetch("/Notification/Delete", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
          RequestVerificationToken: $(
            'input[name="__RequestVerificationToken"]'
          ).val(),
        },
        body: `notificationId=${encodeURIComponent(notificationId)}`,
      });

      if (response.ok) {
        const result = await response.json();
        if (result.success) {
          // Animate and remove the notification card
          const notificationCard =
            $(buttonElement).closest(".notification-item");
          notificationCard.fadeOut(400, function () {
            $(this).remove();

            // Check if there are no notifications left
            if ($(".notification-item").length === 0) {
              $(".notifications-container").html(`
                                <div class="empty-state">
                                    <i class="fas fa-bell-slash"></i>
                                    <h5>Your inbox is empty</h5>
                                    <p>No notifications to display. You'll receive updates here as they become available.</p>
                                </div>
                            `);
            }
          });

          this.showToast("Notification deleted successfully", "success");
          this.updateUnreadCount();
        } else {
          this.showToast("Failed to delete notification", "error");
        }
      } else {
        this.showToast("Error deleting notification", "error");
      }
    } catch (error) {
      console.error("Error deleting notification:", error);
      this.showToast("Error deleting notification", "error");
    }
  }
  async markAllAsRead() {
    try {
      const response = await fetch("/Notification/MarkAllAsRead", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
          RequestVerificationToken: $(
            'input[name="__RequestVerificationToken"]'
          ).val(),
        },
      });

      if (response.ok) {
        const result = await response.json();
        if (result.success) {
          // Remove all mark as read buttons
          $(".mark-read-btn").fadeOut(300, function () {
            $(this).remove();
          });

          // Update all notification cards to show as read
          $(".notification-item.unread").removeClass("unread").addClass("read");

          // Remove all "New" badges
          $(".badge.bg-primary").fadeOut(300, function () {
            $(this).remove();
          });

          this.showToast("All notifications marked as read", "success");
          this.updateUnreadCount();
        } else {
          this.showToast("Failed to mark all notifications as read", "error");
        }
      } else {
        this.showToast("Error marking all notifications as read", "error");
      }
    } catch (error) {
      console.error("Error marking all notifications as read:", error);
      this.showToast("Error marking all notifications as read", "error");
    }
  }

  async refreshNotifications() {
    try {
      const refreshBtn = $("#refreshNotifications");
      const originalHtml = refreshBtn.html();

      // Show loading state
      refreshBtn.html(
        '<i class="fas fa-spinner fa-spin me-1"></i>Refreshing...'
      );
      refreshBtn.prop("disabled", true);

      // Reload the page to get fresh data
      window.location.reload();
    } catch (error) {
      console.error("Error refreshing notifications:", error);
      this.showToast("Error refreshing notifications", "error");
    }
  }

  async loadMoreNotifications(page) {
    try {
      const loadMoreBtn = $("#loadMore");
      const originalText = loadMoreBtn.html();

      // Show loading state
      loadMoreBtn.html('<i class="fas fa-spinner fa-spin me-2"></i>Loading...');
      loadMoreBtn.prop("disabled", true);

      const response = await fetch(`/Notification?page=${page}&pageSize=10`, {
        method: "GET",
        headers: {
          "X-Requested-With": "XMLHttpRequest",
        },
      });

      if (response.ok) {
        const html = await response.text();
        const newContent = $(html).find(".notification-item");

        if (newContent.length > 0) {
          $(".notifications-container .card-body").append(newContent);

          // Update the load more button's page number
          loadMoreBtn.data("page", page + 1);

          // Check if there are more pages
          const hasNextPage = $(html).find("#loadMore").length > 0;
          if (!hasNextPage) {
            loadMoreBtn.parent().fadeOut(300, function () {
              $(this).remove();
            });
          }
        } else {
          // No more notifications
          loadMoreBtn.parent().fadeOut(300, function () {
            $(this).remove();
          });
        }
      } else {
        this.showToast("Error loading more notifications", "error");
      }
    } catch (error) {
      console.error("Error loading more notifications:", error);
      this.showToast("Error loading more notifications", "error");
    } finally {
      // Reset button state
      const loadMoreBtn = $("#loadMore");
      if (loadMoreBtn.length) {
        loadMoreBtn.html(originalText);
        loadMoreBtn.prop("disabled", false);
      }
    }
  }

  async updateUnreadCount() {
    try {
      const response = await fetch("/Notification/GetUnreadCount");
      if (response.ok) {
        const result = await response.json();

        // Update unread count in header if it exists
        const unreadBadge = $(".notification-badge, .unread-count");
        if (result.count > 0) {
          unreadBadge.text(result.count).show();
        } else {
          unreadBadge.hide();
        }

        // Update page title if needed
        const baseTitle = document.title.replace(/^\(\d+\)\s*/, "");
        document.title =
          result.count > 0 ? `(${result.count}) ${baseTitle}` : baseTitle;
      }
    } catch (error) {
      console.error("Error updating unread count:", error);
    }
  }

  setupSignalR() {
    // Initialize SignalR connection for real-time updates
    const connection = new signalR.HubConnectionBuilder()
      .withUrl("/notificationHub")
      .configureLogging(signalR.LogLevel.Information)
      .build();

    connection
      .start()
      .then(() => {

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

  handleNewNotification(notification) {
    // Add new notification to the top of the list
    const notificationHtml = this.createNotificationHtml(notification);
    $(".notifications-container .card-body").prepend(notificationHtml);

    // Remove empty state if it exists
    $(".empty-state").remove();

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

  updateUnreadCountDisplay(count) {
    const unreadBadge = $(".notification-badge, .unread-count");
    if (count > 0) {
      unreadBadge.text(count).show();
    } else {
      unreadBadge.hide();
    }

    // Update page title
    const baseTitle = document.title.replace(/^\(\d+\)\s*/, "");
    document.title = count > 0 ? `(${count}) ${baseTitle}` : baseTitle;
  }

  createNotificationHtml(notification) {
    // This would create the HTML for a new notification
    // For now, just reload the page to get the proper server-rendered HTML
    return "";
  }

  showToast(message, type = "info") {
    // Use the existing toast notification system
    if (typeof showToast === "function") {
      showToast(message, type);
    } else {
      // Fallback to console log
  
    }
  }
}

// Initialize when DOM is ready
$(document).ready(() => {
  new NotificationIndex();
});
