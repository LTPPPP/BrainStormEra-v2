// User Management JavaScript
class UserManagement {
  constructor() {
    this.sortColumn = "";
    this.sortDirection = "asc";
    this.currentPage = 1;
    this.totalPages = 1;

    this.init();
  }
  init() {
    this.bindEvents();
    this.initializeTooltips();
    this.setupSearchKeyboardNavigation();
    this.preloadUserAvatars();
  } // Preload user avatars to ensure they're cached
  preloadUserAvatars() {
    // Create a cache for user avatars
    this.avatarCache = new Map();

    document.querySelectorAll(".user-details").forEach((userDetail) => {
      const userId = userDetail
        .closest("tr")
        .querySelector(".btn-edit-user, .btn-change-status")?.dataset.userId;
      const avatarImg = userDetail.querySelector(".user-avatar img");

      if (userId && avatarImg && avatarImg.src) {
        // Store the avatar URL in our cache, whether it's default or custom
        this.avatarCache.set(userId, avatarImg.src);

        // Preload the image to ensure it loads properly
        const img = new Image();
        img.onload = () => {
          // Image loaded successfully, update cache with actual loaded URL
          this.avatarCache.set(userId, img.src);
        };
        img.onerror = () => {
          // If image fails to load, use default avatar
          const defaultAvatar = "/SharedMedia/defaults/default-avatar.svg";
          const SHARED_MEDIA_AVATARS = "/SharedMedia/avatars/";
          this.avatarCache.set(userId, defaultAvatar);
          if (avatarImg.src !== defaultAvatar) {
            avatarImg.src = defaultAvatar;
          }
        };
        img.src = avatarImg.src;
      }
    });
  }

  bindEvents() {
    // Filter form events
    const filterForm = document.querySelector(".filter-section form");
    if (filterForm) {
      filterForm.addEventListener("submit", (e) => this.handleFilterSubmit(e));
    }

    const clearFilterBtn = document.querySelector(".btn-clear");
    if (clearFilterBtn) {
      clearFilterBtn.addEventListener("click", () => this.clearFilters());
    }

    // Table events
    this.bindTableEvents(); // Modal events
    this.bindModalEvents();

    // Search events
    this.bindSearchEvents();
  }
  bindTableEvents() {
    // Sortable headers
    document.querySelectorAll(".sortable").forEach((header) => {
      header.addEventListener("click", (e) => this.handleSort(e));
    });

    // Action buttons
    document.querySelectorAll(".btn-edit-user").forEach((btn) => {
      btn.addEventListener("click", (e) => this.handleEditUser(e));
    });

    document.querySelectorAll(".btn-change-status").forEach((btn) => {
      btn.addEventListener("click", (e) => this.handleChangeStatus(e));
    });

    document.querySelectorAll(".btn-view-progress").forEach((btn) => {
      btn.addEventListener("click", (e) =>
        this.openProgressModal(e.target.dataset.userId)
      );
    });

    // Pagination
    document.querySelectorAll(".pagination .page-link").forEach((link) => {
      link.addEventListener("click", (e) => {
        e.preventDefault();
        const page = e.target.closest(".page-link").dataset.page;
        if (page) {
          this.currentPage = parseInt(page);
          this.refreshTable();
        }
      });
    });
  }

  bindModalEvents() {
    // Close modal events
    document.querySelectorAll(".close, .btn-cancel").forEach((btn) => {
      btn.addEventListener("click", (e) => this.closeModal(e));
    });

    // Save events
    document.querySelectorAll(".btn-save").forEach((btn) => {
      btn.addEventListener("click", (e) => this.handleSave(e));
    });

    // Click outside modal to close
    document.querySelectorAll(".modal").forEach((modal) => {
      modal.addEventListener("click", (e) => {
        if (e.target === modal) {
          this.closeModal(e);
        }
      });
    });
  }
  bindSearchEvents() {
    const searchInput = document.querySelector('input[name="search"]');
    const searchIndicator = document.querySelector("#searchIndicator");

    if (searchInput) {
      let searchTimeout;

      // Real-time search on input
      searchInput.addEventListener("input", (e) => {
        clearTimeout(searchTimeout);

        // Show loading indicator
        if (searchIndicator) {
          searchIndicator.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
        }

        // Debounce search - trigger after 300ms of inactivity
        searchTimeout = setTimeout(() => {
          this.handleSearch(e.target.value);
        }, 300);
      });

      // Prevent form submission on Enter key
      searchInput.addEventListener("keydown", (e) => {
        if (e.key === "Enter") {
          e.preventDefault();
          // Trigger immediate search if Enter is pressed
          clearTimeout(searchTimeout);
          this.handleSearch(e.target.value);
        }
      });

      // Clear search with Escape key
      searchInput.addEventListener("keydown", (e) => {
        if (e.key === "Escape") {
          e.target.value = "";
          this.handleSearch("");
        }
      });
    }
  }

  // Filter handling
  handleFilterSubmit(e) {
    e.preventDefault();
    this.applyFilters();
  }

  applyFilters() {
    const formData = new FormData(
      document.querySelector(".filter-section form")
    );
    const params = new URLSearchParams();

    for (let [key, value] of formData.entries()) {
      if (value) {
        params.append(key, value);
      }
    }

    params.append("page", "1"); // Reset to first page
    this.loadUsers(params);
  }

  clearFilters() {
    document.querySelector(".filter-section form").reset();
    this.loadUsers(new URLSearchParams({ page: "1" }));
  }

  handleSort(e) {
    const column = e.target.dataset.column;
    if (!column) return;

    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === "asc" ? "desc" : "asc";
    } else {
      this.sortColumn = column;
      this.sortDirection = "asc";
    }

    this.updateSortIndicators();

    const params = this.getCurrentParams();
    params.set("sortBy", this.sortColumn);
    params.set("sortDirection", this.sortDirection);
    params.set("page", "1");

    this.loadUsers(params);
  }

  updateSortIndicators() {
    document.querySelectorAll(".sort-indicator").forEach((indicator) => {
      indicator.textContent = "";
    });

    const currentHeader = document.querySelector(
      `[data-column="${this.sortColumn}"] .sort-indicator`
    );
    if (currentHeader) {
      currentHeader.textContent = this.sortDirection === "asc" ? "↑" : "↓";
    }
  }

  // User actions
  handleEditUser(e) {
    const userId = e.target.dataset.userId;
    this.openEditModal(userId);
  }

  handleChangeStatus(e) {
    const userId = e.target.dataset.userId;
    const currentStatus = e.target.dataset.currentStatus;
    this.openStatusModal(userId, currentStatus);
  }

  // Modal handling
  openEditModal(userId) {
    this.showModal("editUserModal");
    this.loadUserData(userId, "edit");
  }

  openProgressModal(userId) {
    this.loadUserData(userId, "progress");
  }

  openStatusModal(userId, currentStatus) {
    this.showModal("changeStatusModal");
    document.querySelector("#statusUserId").value = userId;
    document.querySelector("#statusSelect").value = currentStatus;
  }

  showModal(modalId) {
    const modal = document.getElementById(modalId);
    if (modal) {
      modal.classList.add("show");
      document.body.style.overflow = "hidden";
    }
  }

  closeModal(e) {
    const modal = e.target.closest(".modal");
    if (modal) {
      modal.classList.remove("show");
      document.body.style.overflow = "";
      this.resetModal(modal);
    }
  }

  resetModal(modal) {
    const forms = modal.querySelectorAll("form");
    forms.forEach((form) => form.reset());

    const alerts = modal.querySelectorAll(".alert");
    alerts.forEach((alert) => alert.remove());
  }

  // Save handling
  handleSave(e) {
    const modal = e.target.closest(".modal");
    const form = modal.querySelector("form");

    if (!form) return;

    const formData = new FormData(form);
    const modalId = modal.id;

    this.showLoading(e.target);

    let endpoint;
    switch (modalId) {
      case "editUserModal":
        endpoint = "/User/UpdateUser";
        break;
      case "changeStatusModal":
        endpoint = "/User/ChangeStatus";
        break;
      default:
        return;
    }

    this.submitForm(endpoint, formData, modal);
  }

  // Export functionality
  handleExport() {
    const params = this.getCurrentParams();
    params.set("export", "true");

    const url = `/User/Export?${params.toString()}`;
    window.open(url, "_blank");
  } // Search functionality
  handleSearch(searchTerm) {
    const params = this.getCurrentParams();
    const searchIndicator = document.querySelector("#searchIndicator");

    // Clear previous highlights
    this.clearSearchHighlights();

    if (searchTerm && searchTerm.trim() !== "") {
      params.set("search", searchTerm.trim());
    } else {
      params.delete("search");
    }
    params.set("page", "1");

    // Show loading indicator
    if (searchIndicator) {
      searchIndicator.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
      searchIndicator.classList.add("searching");
    }
    this.loadUsers(params)
      .then(() => {
        // Highlight search results
        if (searchTerm && searchTerm.trim() !== "") {
          setTimeout(() => {
            this.highlightSearchResults(searchTerm.trim());
            // Check for no results and show appropriate message
            this.showNoSearchResults(searchTerm.trim());
          }, 100);
        }

        // Reset search indicator
        if (searchIndicator) {
          searchIndicator.classList.remove("searching");
          if (searchTerm && searchTerm.trim() !== "") {
            searchIndicator.innerHTML =
              '<i class="fas fa-check text-success"></i>';
            searchIndicator.classList.add("success");
            setTimeout(() => {
              searchIndicator.innerHTML = '<i class="fas fa-search"></i>';
              searchIndicator.classList.remove("success");
            }, 1500);
          } else {
            searchIndicator.innerHTML = '<i class="fas fa-search"></i>';
          }
        }
      })
      .catch((error) => {
        // Error handling
        if (searchIndicator) {
          searchIndicator.classList.remove("searching");
          searchIndicator.classList.add("error");
          searchIndicator.innerHTML =
            '<i class="fas fa-exclamation-triangle text-danger"></i>';
          setTimeout(() => {
            searchIndicator.innerHTML = '<i class="fas fa-search"></i>';
            searchIndicator.classList.remove("error");
          }, 2000);
        }
        this.handleSearchError(error);
      });
  }

  // Add a no results message when search returns empty
  showNoSearchResults(searchTerm) {
    const tableContainer = document.querySelector(".users-section");
    if (!tableContainer) return;

    const noResultsHtml = `
      <div class="card">
        <div class="card-body no-search-results">
          <i class="fas fa-search"></i>
          <h4>No Users Found</h4>
          <p>No users match your search for "<strong>${searchTerm}</strong>"</p>
          <button class="btn btn-outline-primary" onclick="clearFilters()">
            <i class="fas fa-times me-2"></i>Clear Search
          </button>
        </div>
      </div>
    `;

    // Check if there are no results
    if (!document.querySelector("#usersTable tbody tr")) {
      // If the table exists but has no rows, replace with no results message
      const existingTable = document.querySelector(".users-section .card");
      if (existingTable) {
        existingTable.outerHTML = noResultsHtml;
      }
    }
  }
  // Keyboard navigation for search
  setupSearchKeyboardNavigation() {
    const searchInput = document.querySelector("#searchBox");
    if (!searchInput) return;

    // Current highlighted row index
    let currentRowIndex = -1;

    // Move through search results with arrow keys
    searchInput.addEventListener("keydown", (e) => {
      const rows = document.querySelectorAll("#usersTable tbody tr");
      if (!rows.length) return;

      if (e.key === "ArrowDown") {
        e.preventDefault();

        // Remove previous highlight
        document
          .querySelectorAll("#usersTable tbody tr.table-active")
          .forEach((row) => {
            row.classList.remove("table-active");
          });

        // Move to next row or first row if at the end
        currentRowIndex =
          currentRowIndex < rows.length - 1 ? currentRowIndex + 1 : 0;

        // Highlight current row
        const currentRow = rows[currentRowIndex];
        if (currentRow) {
          currentRow.classList.add("table-active");
          currentRow.scrollIntoView({ behavior: "smooth", block: "center" });
        }
      } else if (e.key === "ArrowUp") {
        e.preventDefault();

        // Remove previous highlight
        document
          .querySelectorAll("#usersTable tbody tr.table-active")
          .forEach((row) => {
            row.classList.remove("table-active");
          });

        // Move to previous row or last row if at the beginning
        currentRowIndex =
          currentRowIndex > 0 ? currentRowIndex - 1 : rows.length - 1;

        // Highlight current row
        const currentRow = rows[currentRowIndex];
        if (currentRow) {
          currentRow.classList.add("table-active");
          currentRow.scrollIntoView({ behavior: "smooth", block: "center" });
        }
      } else if (e.key === "Enter" && currentRowIndex >= 0) {
        e.preventDefault();

        // If Enter pressed on a highlighted row, click the first action button
        const currentRow = rows[currentRowIndex];
        if (currentRow) {
          const actionBtn = currentRow.querySelector(".dropdown-toggle");
          if (actionBtn) {
            actionBtn.click();
          }
        }
      }
    });

    // Reset navigation when search input loses focus
    searchInput.addEventListener("blur", () => {
      currentRowIndex = -1;

      // Small delay to not interfere with click actions
      setTimeout(() => {
        document
          .querySelectorAll("#usersTable tbody tr.table-active")
          .forEach((row) => {
            row.classList.remove("table-active");
          });
      }, 200);
    });
  } // Enhanced search functionality
  highlightSearchResults(searchTerm) {
    if (!searchTerm || searchTerm.trim() === "") return;

    const searchRegex = new RegExp(`(${searchTerm.trim()})`, "gi");
    const userCells = document.querySelectorAll(
      ".user-details h6, .user-details small, .course-name"
    );

    let matchCount = 0;
    const rowsWithMatches = new Set();

    userCells.forEach((cell) => {
      const originalText = cell.textContent;
      if (originalText.toLowerCase().includes(searchTerm.toLowerCase())) {
        cell.innerHTML = originalText.replace(
          searchRegex,
          '<span class="search-highlight">$1</span>'
        );

        // Count unique rows with matches
        const row = cell.closest("tr");
        if (row && !rowsWithMatches.has(row)) {
          rowsWithMatches.add(row);
          matchCount++;
        }
      }
    });

    // Show match count
    const searchCountEl = document.getElementById("searchCount");
    if (searchCountEl) {
      if (matchCount > 0) {
        searchCountEl.textContent = `${matchCount} found`;
        searchCountEl.classList.add("visible");
      } else {
        searchCountEl.classList.remove("visible");
      }
    }
  }
  // Clear search highlights
  clearSearchHighlights() {
    document.querySelectorAll(".search-highlight").forEach((element) => {
      const parent = element.parentNode;
      parent.replaceChild(
        document.createTextNode(element.textContent),
        element
      );
      parent.normalize();
    });

    // Clear count badge
    const searchCountEl = document.getElementById("searchCount");
    if (searchCountEl) {
      searchCountEl.classList.remove("visible");
    }

    // Remove active row highlighting
    document
      .querySelectorAll("#usersTable tbody tr.table-active")
      .forEach((row) => {
        row.classList.remove("table-active");
      });
  } // Data loading
  loadUsers(params) {
    this.showTableLoading();

    // Use the avatar cache if it exists, otherwise create a temporary one for this request
    const avatarCache = this.avatarCache || new Map();

    // Update cache with current avatars before refreshing
    document.querySelectorAll(".user-details").forEach((userDetail) => {
      const userId = userDetail
        .closest("tr")
        .querySelector(".btn-edit-user, .btn-change-status")?.dataset.userId;
      const avatarImg = userDetail.querySelector(".user-avatar img");

      if (userId && avatarImg && avatarImg.src) {
        // Cache all avatar sources (including default) to maintain consistency
        avatarCache.set(userId, avatarImg.src);
      }
    });

    return fetch(`/User?${params.toString()}`, {
      headers: {
        "X-Requested-With": "XMLHttpRequest",
      },
    })
      .then((response) => {
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }
        return response.text();
      })
      .then((html) => {
        const tableContainer = document.querySelector(".users-section");
        if (tableContainer) {
          tableContainer.innerHTML = html;
          this.bindTableEvents(); // Restore avatars from our cache after DOM update
          document.querySelectorAll(".user-details").forEach((userDetail) => {
            const userId = userDetail
              .closest("tr")
              .querySelector(".btn-edit-user, .btn-change-status")
              ?.dataset.userId;
            const avatarImg = userDetail.querySelector(".user-avatar img");

            if (userId && avatarImg && avatarCache.has(userId)) {
              const cachedAvatar = avatarCache.get(userId);
              // Always use cached avatar to maintain consistency during navigation
              if (cachedAvatar && avatarImg.src !== cachedAvatar) {
                avatarImg.src = cachedAvatar;

                // Add error handling for the restored avatar
                avatarImg.onerror = function () {
                  this.onerror = null;
                  this.src = "/SharedMedia/defaults/default-avatar.svg";
                  avatarCache.set(
                    userId,
                    "/SharedMedia/defaults/default-avatar.svg"
                  );
                };
              }
            } else if (userId && avatarImg) {
              // If no cache exists, ensure proper error handling
              avatarImg.onerror = function () {
                this.onerror = null;
                this.src = "/SharedMedia/defaults/default-avatar.svg";
                avatarCache.set(
                  userId,
                  "/SharedMedia/defaults/default-avatar.svg"
                );
              };
            }
          });

          // Update the class avatar cache
          this.avatarCache = avatarCache;
        }
      })
      .catch((error) => {
        this.showAlert("danger", "Failed to load users");
        throw error; // Re-throw for search feedback
      })
      .finally(() => {
        this.hideTableLoading();
      });
  }

  loadUserData(userId, type) {
    const endpoint = `/User/GetUserData/${userId}?type=${type}`;

    fetch(endpoint)
      .then((response) => response.json())
      .then((data) => {
        if (data.success) {
          this.populateModal(type, data.user);
        } else {
          this.showAlert("danger", data.message || "Failed to load user data");
        }
      })
      .catch((error) => {
        this.showAlert("danger", "An error occurred while loading user data");
      });
  }

  populateModal(type, userData) {
    switch (type) {
      case "edit":
        document.querySelector("#editUserId").value = userData.userId;
        document.querySelector("#editUserName").value = userData.fullName;
        document.querySelector("#editUserEmail").value = userData.email;
        break;
      case "progress":
        document.querySelector("#progressUserId").value = userData.userId;
        document.querySelector("#progressUserName").textContent =
          userData.fullName;
        // Populate course progress fields
        break;
    }
  }

  // Form submission
  submitForm(endpoint, formData, modal) {
    const data = {};
    for (let [key, value] of formData.entries()) {
      data[key] = value;
    }

    this.makeRequest(endpoint, "POST", data)
      .then((response) => {
        if (response.success) {
          this.showAlert(
            "success",
            response.message || "Operation completed successfully"
          );
          this.closeModal({ target: modal });
          this.refreshTable();
        } else {
          this.showModalAlert(
            modal,
            "danger",
            response.message || "Operation failed"
          );
        }
      })
      .catch((error) => {
        this.showModalAlert(
          modal,
          "danger",
          "An error occurred during the operation"
        );
      })
      .finally(() => {
        this.hideLoading(modal.querySelector(".btn-save"));
      });
  }

  // Export for use in other scripts
  export() {
    return {
      handleSearch: this.handleSearch.bind(this),
      highlightSearchResults: this.highlightSearchResults.bind(this),
      clearSearchHighlights: this.clearSearchHighlights.bind(this),
    };
  }

  // Utility functions
  makeRequest(url, method, data) {
    const options = {
      method: method,
      headers: {
        "Content-Type": "application/json",
        "X-Requested-With": "XMLHttpRequest",
      },
    };

    if (data) {
      options.body = JSON.stringify(data);
    }

    return fetch(url, options).then((response) => response.json());
  }
  getCurrentParams() {
    const form = document.querySelector(".filter-section form");
    const params = new URLSearchParams();

    if (form) {
      const formData = new FormData(form);
      for (let [key, value] of formData.entries()) {
        if (value) {
          params.set(key, value);
        }
      }
    }

    return params;
  }

  refreshTable() {
    const params = this.getCurrentParams();
    params.set("page", this.currentPage.toString());
    this.loadUsers(params);
  }

  showLoading(element) {
    element.disabled = true;
    element.innerHTML = '<span class="spinner"></span> Loading...';
  }

  hideLoading(element) {
    element.disabled = false;
    element.innerHTML = element.dataset.originalText || "Save";
  }
  showTableLoading() {
    const tableContainer = document.querySelector(".users-section");
    if (tableContainer) {
      tableContainer.classList.add("loading");
    }
  }

  hideTableLoading() {
    const tableContainer = document.querySelector(".users-section");
    if (tableContainer) {
      tableContainer.classList.remove("loading");
    }
  }

  showAlert(type, message) {
    const alertHtml = `
            <div class="alert alert-${type}" role="alert">
                ${message}
                <button type="button" class="close" aria-label="Close" style="float: right; background: none; border: none;">
                    <span aria-hidden="true">&times;</span>
                </button>
            </div>
        `;

    const container =
      document.querySelector(".content-container") || document.body;
    container.insertAdjacentHTML("afterbegin", alertHtml);

    // Auto remove after 5 seconds
    setTimeout(() => {
      const alert = container.querySelector(".alert");
      if (alert) {
        alert.remove();
      }
    }, 5000);

    // Add close button functionality
    const closeBtn = container.querySelector(".alert .close");
    if (closeBtn) {
      closeBtn.addEventListener("click", () => {
        closeBtn.closest(".alert").remove();
      });
    }
  }

  showModalAlert(modal, type, message) {
    const existingAlert = modal.querySelector(".alert");
    if (existingAlert) {
      existingAlert.remove();
    }

    const alertHtml = `
            <div class="alert alert-${type}" role="alert">
                ${message}
            </div>
        `;

    const modalBody = modal.querySelector(".modal-body");
    modalBody.insertAdjacentHTML("afterbegin", alertHtml);
  }

  // Enhanced initialize tooltips
  initializeTooltips() {
    // Initialize tooltips if using a tooltip library
    const tooltipElements = document.querySelectorAll("[data-tooltip]");
    tooltipElements.forEach((element) => {
      element.title = element.dataset.tooltip;
    });

    // Add search tips
    const searchInput = document.querySelector("#searchBox");
    if (searchInput) {
      searchInput.setAttribute(
        "title",
        "Search by name, email, or username. Results update automatically as you type."
      );
    }
  }

  // Enhanced error handling
  handleSearchError(error) {
    const searchIndicator = document.querySelector("#searchIndicator");
    if (searchIndicator) {
      searchIndicator.innerHTML =
        '<i class="fas fa-exclamation-triangle text-warning"></i>';
      setTimeout(() => {
        searchIndicator.innerHTML = '<i class="fas fa-search"></i>';
      }, 2000);
    }

    this.showAlert(
      "warning",
      "Search temporarily unavailable. Please try again."
    );
  }

  // Debug method to check avatar cache state
  debugAvatarCache() {
    document.querySelectorAll(".user-details").forEach((userDetail) => {
      const userId = userDetail
        .closest("tr")
        .querySelector(".btn-edit-user, .btn-change-status")?.dataset.userId;
      const avatarImg = userDetail.querySelector(".user-avatar img");
      if (userId && avatarImg) {
        // Debug info removed
      }
    });
  }
}

// Statistics Animation
class StatsAnimation {
  constructor() {
    this.animateStats();
  }

  animateStats() {
    const statNumbers = document.querySelectorAll(".stat-number");

    statNumbers.forEach((stat) => {
      const finalValue = parseInt(stat.textContent);
      if (isNaN(finalValue)) return;

      const duration = 2000; // 2 seconds
      const increment = finalValue / (duration / 16); // 60fps
      let currentValue = 0;

      const timer = setInterval(() => {
        currentValue += increment;
        if (currentValue >= finalValue) {
          currentValue = finalValue;
          clearInterval(timer);
        }
        stat.textContent = Math.floor(currentValue).toLocaleString();
      }, 16);
    });
  }
}

// Progress Bar Animation
class ProgressBarAnimation {
  constructor() {
    this.animateProgressBars();
  }

  animateProgressBars() {
    const progressBars = document.querySelectorAll(".progress-fill");

    const observer = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          const progressBar = entry.target;
          const width = progressBar.dataset.width || progressBar.style.width;
          progressBar.style.width = "0%";

          setTimeout(() => {
            progressBar.style.width = width;
            progressBar.style.transition = "width 1s ease-in-out";
          }, 100);

          observer.unobserve(progressBar);
        }
      });
    });

    progressBars.forEach((bar) => observer.observe(bar));
  }
}

// Auto-refresh functionality
class AutoRefresh {
  constructor(interval = 300000) {
    // 5 minutes default
    this.interval = interval;
    this.timer = null;
    this.isActive = false;
  }

  start() {
    if (this.isActive) return;

    this.isActive = true;
    this.timer = setInterval(() => {
      if (document.visibilityState === "visible") {
        this.refreshData();
      }
    }, this.interval);
  }

  stop() {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
    this.isActive = false;
  }

  refreshData() {
    // Only refresh if no modals are open
    const openModals = document.querySelectorAll(".modal.show");
    if (openModals.length === 0) {
      if (window.userManagement) {
        window.userManagement.refreshTable();
      }
    }
  }
}

// Initialize everything when DOM is loaded
document.addEventListener("DOMContentLoaded", function () {
  // Initialize user management
  window.userManagement = new UserManagement();

  // Initialize animations
  new StatsAnimation();
  new ProgressBarAnimation();

  // Initialize auto-refresh (optional)
  const autoRefresh = new AutoRefresh();

  // Start auto-refresh when page becomes visible
  document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "visible") {
      autoRefresh.start();
    } else {
      autoRefresh.stop();
    }
  });

  // Keyboard shortcuts
  document.addEventListener("keydown", (e) => {
    // Escape key to close modals
    if (e.key === "Escape") {
      const openModal = document.querySelector(".modal.show");
      if (openModal) {
        window.userManagement.closeModal({ target: openModal });
      }
    }

    // Ctrl+R to refresh table
    if (e.ctrlKey && e.key === "r") {
      e.preventDefault();
      window.userManagement.refreshTable();
    }
  });
});

// Make UserManagement accessible globally without redeclaring
if (!window.UserManagement) {
  window.UserManagement = UserManagement;
}

// Define global function for clearing filters (used in HTML)
window.clearFilters = function () {
  if (window.userManagement) {
    window.userManagement.clearFilters();
  }
};
