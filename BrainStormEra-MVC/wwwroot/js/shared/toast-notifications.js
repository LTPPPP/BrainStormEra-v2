/**
 * Toast Notification System
 * Provides functions to show success, error, warning, and info notifications
 */

class ToastNotification {
  constructor() {
    this.container = null;
    this.init();
  }

  init() {
    // Create toast container if it doesn't exist
    if (!document.getElementById("toast-container")) {
      this.container = document.createElement("div");
      this.container.id = "toast-container";
      this.container.className =
        "toast-container position-fixed top-0 end-0 p-3";
      this.container.style.zIndex = "9999";
      document.body.appendChild(this.container);
    } else {
      this.container = document.getElementById("toast-container");
    }
  }

  show(message, type = "info", duration = 5000) {
    const toastId = "toast-" + Date.now();
    const iconMap = {
      success: "fas fa-check-circle",
      error: "fas fa-exclamation-circle",
      warning: "fas fa-exclamation-triangle",
      info: "fas fa-info-circle",
    };

    const bgColorMap = {
      success: "bg-success",
      error: "bg-danger",
      warning: "bg-warning",
      info: "bg-info",
    };

    const toast = document.createElement("div");
    toast.id = toastId;
    toast.className = `toast align-items-center text-white ${bgColorMap[type]} border-0`;
    toast.setAttribute("role", "alert");
    toast.setAttribute("aria-live", "assertive");
    toast.setAttribute("aria-atomic", "true");

    toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body d-flex align-items-center">
                    <i class="${iconMap[type]} me-2"></i>
                    <span>${message}</span>
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        `;

    this.container.appendChild(toast);

    // Initialize Bootstrap toast
    const bsToast = new bootstrap.Toast(toast, {
      autohide: true,
      delay: duration,
    });

    // Show the toast
    bsToast.show();

    // Remove toast element after it's hidden
    toast.addEventListener("hidden.bs.toast", () => {
      if (toast.parentNode) {
        toast.parentNode.removeChild(toast);
      }
    });

    return bsToast;
  }

  success(message, duration = 5000) {
    return this.show(message, "success", duration);
  }

  error(message, duration = 7000) {
    return this.show(message, "error", duration);
  }

  warning(message, duration = 6000) {
    return this.show(message, "warning", duration);
  }

  info(message, duration = 5000) {
    return this.show(message, "info", duration);
  }
}

// Create global instance
window.Toast = new ToastNotification();

// Convenience functions for global access
window.showToast = (message, type = "info", duration = 5000) => {
  return window.Toast.show(message, type, duration);
};

window.showSuccessToast = (message, duration = 5000) => {
  return window.Toast.success(message, duration);
};

window.showErrorToast = (message, duration = 7000) => {
  return window.Toast.error(message, duration);
};

window.showWarningToast = (message, duration = 6000) => {
  return window.Toast.warning(message, duration);
};

window.showInfoToast = (message, duration = 5000) => {
  return window.Toast.info(message, duration);
};

// Auto-show toasts based on TempData
document.addEventListener("DOMContentLoaded", function () {
  // Check for success messages
  const successMessage = document.querySelector("[data-toast-success]");
  if (successMessage) {
    const message = successMessage.getAttribute("data-toast-success");
    showSuccessToast(message);
    successMessage.remove();
  }

  // Check for error messages
  const errorMessage = document.querySelector("[data-toast-error]");
  if (errorMessage) {
    const message = errorMessage.getAttribute("data-toast-error");
    showErrorToast(message);
    errorMessage.remove();
  }

  // Check for warning messages
  const warningMessage = document.querySelector("[data-toast-warning]");
  if (warningMessage) {
    const message = warningMessage.getAttribute("data-toast-warning");
    showWarningToast(message);
    warningMessage.remove();
  }

  // Check for info messages
  const infoMessage = document.querySelector("[data-toast-info]");
  if (infoMessage) {
    const message = infoMessage.getAttribute("data-toast-info");
    showInfoToast(message);
    infoMessage.remove();
  }
});
