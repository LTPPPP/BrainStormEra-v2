// Admin Notifications Page JavaScript
$(document).ready(function() {
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
      const notificationId = $(e.currentTarget).data("notification-id");
      this.markAsRead(notificationId, e.currentTarget);
    });
    
    // Delete notification functionality
    $(document).on("click", ".delete-btn", (e) => {
      e.preventDefault();
      const element = e.currentTarget;
      const notificationId = $(element).data("notification-id");
      console.log("Delete button clicked, notification ID:", notificationId);

      if (!notificationId) {
        console.error("No notification ID found");
        showToast("Notification ID not found", "error");
        return;
      }

      this.showDeleteConfirmation(notificationId, element);
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
    $("#targetUserId").on("input", this.debounce((e) => {
      this.searchUsers($(e.target).val());
    }, 300));
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
        console.error("SignalR Connection Error: ", err);
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
  }

  async markAsRead(notificationId, buttonElement) {
    // Immediately update UI for better UX
    const notificationCard = $(buttonElement).closest(".notification-item");
    const originalBackground = notificationCard.css("background");

    // Show loading state
    $(buttonElement)
      .prop("disabled", true)
      .html('<i class="fas fa-spinner fa-spin"></i>');

    // Apply immediate visual feedback
    notificationCard.css("transition", "all 0.3s ease");
    notificationCard.removeClass("unread").addClass("read");

    try {
      const response = await fetch("/Admin/Notifications?handler=MarkAsRead", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
          "RequestVerificationToken": this.antiForgeryToken
        },
        body: `notificationId=${encodeURIComponent(notificationId)}`
      });

      if (response.ok) {
        const result = await response.json();
        if (result.success) {
          // Remove the mark as read button with animation
          $(buttonElement).fadeOut(300, function () {
            $(this).remove();
          });

          // Remove the "New" badge if present with animation
          notificationCard.find(".badge.bg-primary").fadeOut(300, function () {
            $(this).remove();
          });

          // Show success animation
          notificationCard.addClass("read-success");
          setTimeout(() => {
            notificationCard.removeClass("read-success");
          }, 1500);

          // Update notification bell count
          this.updateHeaderNotificationBell();
          
          // Show toast notification
          showToast("Notification marked as read", "success");
          
          // Hide the "Mark All Read" button if no more unread notifications
          if ($(".notification-item.unread").length === 0) {
            $("#markAllRead").fadeOut(300);
          }
        } else {
          // Revert UI changes on error
          notificationCard.css("background", originalBackground);
          notificationCard.removeClass("read").addClass("unread");
          $(buttonElement)
            .prop("disabled", false)
            .html('<i class="fas fa-check"></i>');
          showToast(result.message || "Error marking notification as read", "error");
        }
      } else {
        throw new Error("Server returned an error");
      }
    } catch (error) {
      console.error("Error marking notification as read:", error);
      // Revert UI changes on error
      notificationCard.css("background", originalBackground);
      notificationCard.removeClass("read").addClass("unread");
      $(buttonElement)
        .prop("disabled", false)
        .html('<i class="fas fa-check"></i>');
      showToast("Failed to mark notification as read", "error");
    }
  }

  async markAllAsRead() {
    // Show loading state
    const originalButtonHtml = $("#markAllRead").html();
    $("#markAllRead")
      .prop("disabled", true)
      .html('<i class="fas fa-spinner fa-spin"></i> Processing...');
    
    // Apply immediate visual feedback to all unread notifications
    $(".notification-item.unread").each(function() {
      $(this).css("transition", "all 0.3s ease");
      $(this).removeClass("unread").addClass("read");
      
      // Hide the mark as read buttons with animation
      $(this).find(".mark-read-btn").fadeOut(300);
      
      // Hide the "New" badges with animation
      $(this).find(".badge.bg-primary").fadeOut(300);
    });

    try {
      const response = await fetch("/Admin/Notifications?handler=MarkAllAsRead", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
          "RequestVerificationToken": this.antiForgeryToken
        }
      });

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
          showToast("All notifications marked as read", "success");
          
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
            
          $(".notification-item.unread")
            .find(".badge.bg-primary")
            .fadeIn(300);
            
          $("#markAllRead")
            .prop("disabled", false)
            .html(originalButtonHtml);
            
          showToast(result.message || "Error marking all notifications as read", "error");
        }
      } else {
        throw new Error("Server returned an error");
      }
    } catch (error) {
      console.error("Error marking all notifications as read:", error);
      
      // Revert UI changes on error
      $(".notification-item.read")
        .removeClass("read")
        .addClass("unread")
        .find(".mark-read-btn")
        .fadeIn(300);
        
      $(".notification-item.unread")
        .find(".badge.bg-primary")
        .fadeIn(300);
        
      $("#markAllRead")
        .prop("disabled", false)
        .html(originalButtonHtml);
        
      showToast("Failed to mark all notifications as read", "error");
    }
  }

  showDeleteConfirmation(notificationId, buttonElement) {
    if (confirm("Are you sure you want to delete this notification? This action cannot be undone.")) {
      this.deleteNotification(notificationId, buttonElement);
    }
  }

  async deleteNotification(notificationId, buttonElement) {
    // Show loading state
    $(buttonElement)
      .prop("disabled", true)
      .html('<i class="fas fa-spinner fa-spin"></i>');

    try {
      const response = await fetch("/Admin/Notifications?handler=DeleteNotification", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
          "RequestVerificationToken": this.antiForgeryToken
        },
        body: `notificationId=${encodeURIComponent(notificationId)}`
      });

      if (response.ok) {
        const result = await response.json();
        if (result.success) {
          // Remove the notification with animation
          const notificationItem = $(buttonElement).closest(".notification-item");
          notificationItem.slideUp(400, function() {
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
      console.error("Error deleting notification:", error);
      
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
      const response = await fetch("/Admin/Notifications?handler=RefreshNotifications", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
          "RequestVerificationToken": this.antiForgeryToken
        }
      });

      if (response.ok) {
        const result = await response.json();
        if (result.success) {
          // Replace notifications list with new content
          $("#notificationsList").html(result.html);
          
          // Update notification counts
          if (result.unreadCount > 0) {
            $("#markAllRead").fadeIn(300);
            $(".badge.bg-danger").text(result.unreadCount + " New").fadeIn(300);
          } else {
            $("#markAllRead").fadeOut(300);
            $(".badge.bg-danger").fadeOut(300);
          }

          // Update notification bell count
          this.updateHeaderNotificationBell();
          
          // Show toast notification
          showToast("Notifications refreshed", "success");
          
          // Apply animations
          $(".notification-item").addClass("new-notification");
          setTimeout(() => {
            $(".notification-item").removeClass("new-notification");
          }, 1000);
        } else {
          showToast(result.message || "Error refreshing notifications", "error");
        }
      } else {
        throw new Error("Server returned an error");
      }
    } catch (error) {
      console.error("Error refreshing notifications:", error);
      showToast("Failed to refresh notifications", "error");
    } finally {
      // Reset button state
      $("#refreshNotifications")
        .prop("disabled", false)
        .html(originalButtonHtml);
    }
  }

  async showEditModal(notificationId) {
    try {
      const response = await fetch(`/Admin/Notifications?handler=GetNotificationDetails&notificationId=${notificationId}`, {
        method: "GET",
        headers: {
          "Content-Type": "application/json"
        }
      });

      if (response.ok) {
        const notification = await response.json();
        
        // Populate the edit form
        $("#editNotificationId").val(notification.notificationId);
        $("#editNotificationTitle").val(notification.notificationTitle);
        $("#editNotificationType").val(notification.notificationType || "General");
        $("#editRecipientUser").val(notification.recipientName || "All Users");
        $("#editNotificationContent").val(notification.notificationContent);
        
        // Show the modal
        new bootstrap.Modal(document.getElementById("editNotificationModal")).show();
      } else {
        throw new Error("Server returned an error");
      }
    } catch (error) {
      console.error("Error fetching notification details:", error);
      showToast("Failed to load notification details", "error");
    }
  }

  async createNotification() {
    // Validate form
    if (!this.validateForm("#createNotificationForm")) {
      return;
    }

    // Disable form submission and show loading
    const submitBtn = $("#createNotificationForm button[type='submit']");
    const originalBtnHtml = submitBtn.html();
    submitBtn.prop("disabled", true).html('<i class="fas fa-spinner fa-spin"></i> Creating...');

    try {
      // Serialize form data
      const formData = new FormData(document.getElementById("createNotificationForm"));
      
      const response = await fetch("/Admin/Notifications?handler=CreateNotification", {
        method: "POST",
        headers: {
          "RequestVerificationToken": this.antiForgeryToken
        },
        body: formData
      });

      if (response.ok) {
        const result = await response.json();
        if (result.success) {
          // Reset form
          $("#createNotificationForm")[0].reset();
          
          // Hide modal
          bootstrap.Modal.getInstance(document.getElementById("createNotificationModal")).hide();
          
          // Show success message
          showToast("Notification created successfully", "success");
          
          // Refresh notifications
          this.refreshNotifications();
        } else {
          showToast(result.message || "Error creating notification", "error");
        }
      } else {
        throw new Error("Server returned an error");
      }
    } catch (error) {
      console.error("Error creating notification:", error);
      showToast("Failed to create notification", "error");
    } finally {
      // Reset button state
      submitBtn.prop("disabled", false).html(originalBtnHtml);
    }
  }

  async updateNotification() {
    // Validate form
    if (!this.validateForm("#editNotificationForm")) {
      return;
    }

    // Disable form submission and show loading
    const submitBtn = $("#editNotificationForm button[type='submit']");
    const originalBtnHtml = submitBtn.html();
    submitBtn.prop("disabled", true).html('<i class="fas fa-spinner fa-spin"></i> Updating...');

    try {
      // Serialize form data
      const formData = new FormData(document.getElementById("editNotificationForm"));
      
      const response = await fetch("/Admin/Notifications?handler=UpdateNotification", {
        method: "POST",
        headers: {
          "RequestVerificationToken": this.antiForgeryToken
        },
        body: formData
      });

      if (response.ok) {
        const result = await response.json();
        if (result.success) {
          // Hide modal
          bootstrap.Modal.getInstance(document.getElementById("editNotificationModal")).hide();
          
          // Show success message
          showToast("Notification updated successfully", "success");
          
          // Refresh notifications
          this.refreshNotifications();
        } else {
          showToast(result.message || "Error updating notification", "error");
        }
      } else {
        throw new Error("Server returned an error");
      }
    } catch (error) {
      console.error("Error updating notification:", error);
      showToast("Failed to update notification", "error");
    } finally {
      // Reset button state
      submitBtn.prop("disabled", false).html(originalBtnHtml);
    }
  }

  validateForm(formSelector) {
    const form = $(formSelector);
    let isValid = true;
    
    // Check required fields
    form.find("[required]").each(function() {
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
    // Adjust form fields based on target type
    switch (targetType) {
      case "0": // Specific User
        $("#targetUserContainer").show();
        break;
      case "1": // Multiple Users
        $("#targetUserContainer").show();
        break;
      default:
        $("#targetUserContainer").hide();
        break;
    }
  }

  async searchUsers(query) {
    if (!query || query.length < 2) {
      $(".user-suggestions").remove();
      return;
    }
    
    try {
      const response = await fetch(`/Admin/Notifications?handler=SearchUsers&query=${encodeURIComponent(query)}`, {
        method: "GET",
        headers: {
          "Content-Type": "application/json"
        }
      });

      if (response.ok) {
        const users = await response.json();
        
        // Remove existing suggestions
        $(".user-suggestions").remove();
        
        if (users.length > 0) {
          // Create suggestions dropdown
          const suggestionsHtml = `
            <div class="user-suggestions">
              ${users.map(user => `
                <div class="user-suggestion" data-user-id="${user.id}">
                  <strong>${user.name}</strong>
                  <small>${user.email}</small>
                </div>
              `).join("")}
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
      console.error("Error searching users:", error);
    }
  }

  handleNewNotification(notification) {
    // Check if notification already exists
    if ($(`[data-notification-id="${notification.notificationId}"]`).length > 0) {
      return;
    }
    
    // Create notification HTML
    const notificationHtml = this.createNotificationHtml(notification);
    
    // Add to top of list
    $("#notificationsList").prepend(notificationHtml);
    
    // Remove empty state if it exists
    $(".empty-state").remove();
    
    // Apply animation
    $(`[data-notification-id="${notification.notificationId}"]`).addClass("new-notification");
    setTimeout(() => {
      $(`[data-notification-id="${notification.notificationId}"]`).removeClass("new-notification");
    }, 1000);
    
    // Update counts
    const unreadCount = $(".notification-item.unread").length;
    if (unreadCount > 0) {
      $("#markAllRead").fadeIn(300);
      $(".badge.bg-danger").text(unreadCount + " New").fadeIn(300);
    }
    
    // Update bell icon
    this.updateHeaderNotificationBell();
    
    // Show toast notification
    showToast("New notification received", "info");
  }

  createNotificationHtml(notification) {
    return `
      <div class="list-group-item notification-item unread" data-notification-id="${notification.notificationId}">
        <div class="d-flex w-100 justify-content-between align-items-start">
          <div class="notification-content flex-grow-1">
            <div class="d-flex justify-content-between align-items-start mb-2">
              <h6 class="mb-0 fw-bold notification-title">${notification.notificationTitle}</h6>
              <div class="notification-meta d-flex align-items-center">
                ${notification.notificationType ? 
                  `<span class="badge bg-secondary me-2">${notification.notificationType}</span>` : ''}
                ${notification.createdBy === notification.currentUserId ? 
                  `<span class="badge bg-info me-2">Created by you</span>` : ''}
                <span class="badge bg-primary me-2">New</span>
                <small class="text-muted">${notification.createdAtFormatted}</small>
              </div>
            </div>
            <p class="mb-2 notification-message">${notification.notificationContent}</p>
            ${notification.courseName ? 
              `<div class="course-reference">
                <i class="fas fa-graduation-cap me-1"></i>
                <span class="text-muted">Related to: <strong>${notification.courseName}</strong></span>
              </div>` : ''}
          </div>
          <div class="notification-actions ms-3 d-flex">
            <button class="btn btn-sm mark-read-btn" data-notification-id="${notification.notificationId}" title="Mark as read">
              <i class="fas fa-check"></i>
            </button>
            ${notification.createdBy === notification.currentUserId ? 
              `<button class="btn btn-sm edit-btn" data-notification-id="${notification.notificationId}" title="Edit notification">
                <i class="fas fa-edit"></i>
              </button>` : ''}
            <button class="btn btn-sm delete-btn" data-notification-id="${notification.notificationId}" title="Delete notification">
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
    loadMoreBtn.prop("disabled", true).html('<i class="fas fa-spinner fa-spin"></i> Loading...');

    try {
      const response = await fetch(`/Admin/Notifications?handler=LoadMoreNotifications&page=${page}`, {
        method: "GET",
        headers: {
          "Content-Type": "application/json"
        }
      });

      if (response.ok) {
        const result = await response.json();
        if (result.success) {
          // Append new notifications
          $("#notificationsList").append(result.html);
          
          // Update load more button
          if (result.hasNextPage) {
            loadMoreBtn.data("page", page + 1).html(originalBtnHtml);
          } else {
            loadMoreBtn.parent().fadeOut(300, function() {
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
          showToast(result.message || "Error loading more notifications", "error");
        }
      } else {
        throw new Error("Server returned an error");
      }
    } catch (error) {
      console.error("Error loading more notifications:", error);
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
