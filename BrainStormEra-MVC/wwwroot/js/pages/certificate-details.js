// Certificate Details Page JavaScript

// Initialize page when DOM is loaded
document.addEventListener("DOMContentLoaded", function () {
  initializePage();
  setupEventListeners();
});

function initializePage() {
  // Check if we're in print mode
  const urlParams = new URLSearchParams(window.location.search);
  const isPrintMode =
    urlParams.has("print") ||
    (window.certificateData && window.certificateData.printMode);

  if (isPrintMode) {
    // Auto-print in print mode
    setTimeout(function () {
      window.print();
    }, 1000);
  } else {
    // Hide page loader when content is loaded
    window.addEventListener("load", function () {
      const loader = document.querySelector(".page-loader");
      if (loader) {
        loader.classList.add("loaded");
        setTimeout(function () {
          loader.style.display = "none";
        }, 500);
      }
    });
  }
}

function setupEventListeners() {
  // Setup share button event listeners
  const shareButtons = document.querySelectorAll(".share-buttons button");
  shareButtons.forEach((button) => {
    button.addEventListener("click", function () {
      const action = this.getAttribute("onclick");
      if (action) {
        eval(action);
      }
    });
  });
}

// Share functions
function shareOnLinkedIn() {
  const url = encodeURIComponent(window.location.href);
  const courseName =
    window.certificateData?.courseName ||
    document.querySelector(".course-name")?.textContent ||
    "a course";
  const text = encodeURIComponent(
    `I just completed the course "${courseName}" on BrainStormEra and earned my certificate!`
  );
  window.open(
    `https://www.linkedin.com/sharing/share-offsite/?url=${url}&title=${text}`,
    "_blank"
  );
}

function shareOnTwitter() {
  const url = encodeURIComponent(window.location.href);
  const courseName =
    window.certificateData?.courseName ||
    document.querySelector(".course-name")?.textContent ||
    "a course";
  const text = encodeURIComponent(
    `I just completed "${courseName}" on BrainStormEra! 🎓 #BrainStormEra #Learning #Certificate`
  );
  window.open(
    `https://twitter.com/intent/tweet?url=${url}&text=${text}`,
    "_blank"
  );
}

function copyUrl() {
  if (navigator.clipboard) {
    navigator.clipboard
      .writeText(window.location.href)
      .then(function () {
        showToast("success", "Certificate URL copied to clipboard!");
      })
      .catch(function () {
        showToast("error", "Failed to copy URL to clipboard");
      });
  } else {
    // Fallback for older browsers
    const textArea = document.createElement("textarea");
    textArea.value = window.location.href;
    document.body.appendChild(textArea);
    textArea.select();
    try {
      document.execCommand("copy");
      showToast("success", "Certificate URL copied to clipboard!");
    } catch (err) {
      showToast("error", "Failed to copy URL to clipboard");
    }
    document.body.removeChild(textArea);
  }
}

function showToast(type, message) {
  // Remove existing toasts
  const existingToasts = document.querySelectorAll(".toast-notification");
  existingToasts.forEach((toast) => toast.remove());

  const toastHtml = `
        <div class="toast-notification toast-${type}" style="position: fixed; top: 20px; right: 20px; z-index: 9999; padding: 12px 20px; border-radius: 6px; color: white; background-color: ${
    type === "success" ? "#28a745" : "#dc3545"
  }; box-shadow: 0 4px 12px rgba(0,0,0,0.1);">
            <div class="toast-content" style="display: flex; align-items: center; gap: 8px;">
                <i class="fas fa-${
                  type === "success" ? "check-circle" : "exclamation-circle"
                }"></i>
                <span>${message}</span>
            </div>
        </div>
    `;
  document.body.insertAdjacentHTML("beforeend", toastHtml);

  setTimeout(function () {
    const toast = document.querySelector(".toast-notification");
    if (toast) {
      toast.remove();
    }
  }, 3000);
}

// Print functionality
function printCertificate() {
  window.print();
}
