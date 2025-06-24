// Global variables
let currentUserId;
let searchUsersUrl;

// Initialize notification create page
document.addEventListener("DOMContentLoaded", function () {
  console.log("Notification Create page loaded");

  // Get current user ID from meta tag or session
  currentUserId =
    document.querySelector('meta[name="user-id"]')?.getAttribute("content") ||
    "";

  // Initialize user search immediately since we only have one target type
  initializeUserSearch();

  // Initialize character counter
  initializeCharacterCounter();

  // Initialize form validation
  initializeFormValidation();
});

// Initialize character counter for content textarea
function initializeCharacterCounter() {
  const contentInput = document.getElementById("Content");
  if (!contentInput) return;

  const maxLength = 500;

  function updateCharacterCounter() {
    const remaining = maxLength - contentInput.value.length;
    const formText = contentInput.parentNode.querySelector(".form-text");

    if (!formText) return;

    if (remaining >= 0) {
      formText.textContent = `${remaining} characters remaining`;
      formText.classList.remove("text-danger");
      formText.classList.toggle("text-warning", remaining < 50);
    } else {
      formText.textContent = `${Math.abs(remaining)} characters over limit!`;
      formText.classList.add("text-danger");
      formText.classList.remove("text-warning");
    }
  }

  contentInput.addEventListener("input", updateCharacterCounter);
  updateCharacterCounter();
}

// Initialize user search functionality
function initializeUserSearch() {
  const searchInput = document.getElementById("userSearchInput");
  const searchResults = document.getElementById("userSearchResults");

  if (!searchInput || !searchResults) return;

  let searchTimeout;

  searchInput.addEventListener("input", function () {
    clearTimeout(searchTimeout);
    const searchTerm = this.value.trim();

    if (searchTerm.length < 2) {
      searchResults.innerHTML =
        '<div class="text-center p-3 text-muted"><i class="fas fa-search me-2"></i>Type at least 2 characters to search...</div>';
      return;
    }

    searchTimeout = setTimeout(() => {
      searchUsers(searchTerm, searchResults, selectUser);
    }, 300);
  });

  // Initial message
  searchResults.innerHTML =
    '<div class="text-center p-3 text-muted"><i class="fas fa-search me-2"></i>Start typing to search for users...</div>';
}

// User search function
function searchUsers(searchTerm, resultsContainer, onUserClick) {
  resultsContainer.innerHTML =
    '<div class="search-loading"><i class="fas fa-spinner fa-spin me-2"></i>Searching...</div>';

  // Construct search URL - this will be set by the server-side code
  if (!searchUsersUrl) {
    console.error("Search URL not configured");
    resultsContainer.innerHTML =
      '<div class="text-center p-3 text-danger"><i class="fas fa-exclamation-triangle me-2"></i>Search not configured properly.</div>';
    return;
  }

  const searchUrl = `${searchUsersUrl}?searchTerm=${encodeURIComponent(
    searchTerm
  )}`;

  fetch(searchUrl)
    .then((response) => {
      if (!response.ok) throw new Error("Search failed");
      return response.json();
    })
    .then((data) => {
      resultsContainer.innerHTML = "";

      if (data.success && data.users && data.users.length > 0) {
        data.users.forEach((user) => {
          const userItem = createUserItem(user, onUserClick);
          resultsContainer.appendChild(userItem);
        });
      } else {
        resultsContainer.innerHTML =
          '<div class="text-center p-3 text-muted">No users found</div>';
      }
    })
    .catch((error) => {
      console.error("Error searching users:", error);
      resultsContainer.innerHTML =
        '<div class="text-center p-3 text-danger"><i class="fas fa-exclamation-triangle me-2"></i>Error searching users. Please try again.</div>';
    });
}

// Create user item element
function createUserItem(user, onClickCallback) {
  const userItem = document.createElement("div");
  userItem.className = "user-search-item";
  userItem.innerHTML = `
        <div class="d-flex align-items-center">
            <div class="flex-grow-1">
                <div class="fw-semibold">${user.fullName || user.userName}</div>
                <div class="small text-muted">${user.email}</div>
                <div class="small"><span class="selected-user-role">${
                  user.role
                }</span></div>
            </div>
            <i class="fas fa-plus text-primary"></i>
        </div>
    `;

  userItem.addEventListener("click", () => onClickCallback(user));
  return userItem;
}

// User selection function
function selectUser(user) {
  // Prevent selecting current user
  if (user.userId === currentUserId) {
    showToast("You cannot send notification to yourself", "warning");
    return;
  }

  const selectedUserIdInput = document.getElementById("selectedUserId");
  const selectedUserInfo = document.getElementById("selectedUserInfo");
  const selectedUserDisplay = document.getElementById("selectedUserDisplay");
  const userSearchResults = document.getElementById("userSearchResults");
  const userSearchInput = document.getElementById("userSearchInput");

  if (selectedUserIdInput) selectedUserIdInput.value = user.userId;

  if (selectedUserInfo) {
    selectedUserInfo.innerHTML = `
            <strong>${user.fullName || user.userName}</strong><br>
            <small class="text-muted">${user.email}</small>
        `;
  }

  if (selectedUserDisplay) selectedUserDisplay.style.display = "block";
  if (userSearchResults) userSearchResults.innerHTML = "";
  if (userSearchInput) userSearchInput.value = "";
}

// Clear user selection function (global function)
function clearUserSelection() {
  const selectedUserIdInput = document.getElementById("selectedUserId");
  const selectedUserDisplay = document.getElementById("selectedUserDisplay");
  const userSearchInput = document.getElementById("userSearchInput");
  const userSearchResults = document.getElementById("userSearchResults");

  if (selectedUserIdInput) selectedUserIdInput.value = "";
  if (selectedUserDisplay) selectedUserDisplay.style.display = "none";
  if (userSearchInput) userSearchInput.value = "";
  if (userSearchResults) {
    userSearchResults.innerHTML =
      '<div class="text-center p-3 text-muted"><i class="fas fa-search me-2"></i>Start typing to search for users...</div>';
  }
}

// Initialize form validation
function initializeFormValidation() {
  const form = document.getElementById("createNotificationForm");
  if (!form) return;

  form.addEventListener("submit", function (e) {
    if (!validateForm()) {
      e.preventDefault();
    }
  });
}

// Form validation function
function validateForm() {
  const selectedUserId = document.getElementById("selectedUserId")?.value;

  if (!selectedUserId) {
    showToast("Please select a user to send the notification to", "error");
    return false;
  }

  if (selectedUserId === currentUserId) {
    showToast("You cannot send notification to yourself", "error");
    return false;
  }

  return true;
}

// Helper function for toast notifications
function showToast(message, type = "info") {
  // Create toast element
  const toast = document.createElement("div");
  toast.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
  toast.style.cssText =
    "top: 20px; right: 20px; z-index: 9999; min-width: 300px;";
  toast.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

  document.body.appendChild(toast);

  // Auto remove after 5 seconds
  setTimeout(() => {
    if (toast.parentNode) {
      toast.remove();
    }
  }, 5000);
}

// Set search URL (to be called by server-side code)
function setSearchUsersUrl(url) {
  searchUsersUrl = url;
}
