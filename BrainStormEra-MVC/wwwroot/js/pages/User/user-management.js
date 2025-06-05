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

    document.querySelectorAll(".btn-update-progress").forEach((btn) => {
      btn.addEventListener("click", (e) => this.handleUpdateProgress(e));
    });

    document.querySelectorAll(".btn-change-status").forEach((btn) => {
      btn.addEventListener("click", (e) => this.handleChangeStatus(e));
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
    if (searchInput) {
      let searchTimeout;
      searchInput.addEventListener("input", (e) => {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
          this.handleSearch(e.target.value);
        }, 500);
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

  handleUpdateProgress(e) {
    const userId = e.target.dataset.userId;
    this.openProgressModal(userId);
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
    this.showModal("updateProgressModal");
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
      case "updateProgressModal":
        endpoint = "/User/UpdateProgress";
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
  }

  // Search functionality
  handleSearch(searchTerm) {
    const params = this.getCurrentParams();
    if (searchTerm) {
      params.set("search", searchTerm);
    } else {
      params.delete("search");
    }
    params.set("page", "1");

    this.loadUsers(params);
  }

  // Data loading
  loadUsers(params) {
    this.showTableLoading();

    fetch(`/User?${params.toString()}`, {
      headers: {
        "X-Requested-With": "XMLHttpRequest",
      },
    })
      .then((response) => response.text())
      .then((html) => {
        const tableContainer = document.querySelector(".user-table-container");
        tableContainer.innerHTML = html;
        this.bindTableEvents();
      })
      .catch((error) => {
        console.error("Load users error:", error);
        this.showAlert("danger", "Failed to load users");
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
        console.error("Load user data error:", error);
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
        console.error("Submit form error:", error);
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
    const formData = new FormData(form);
    const params = new URLSearchParams();

    for (let [key, value] of formData.entries()) {
      if (value) {
        params.set(key, value);
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
    const tableContainer = document.querySelector(".user-table-container");
    tableContainer.classList.add("loading");
  }

  hideTableLoading() {
    const tableContainer = document.querySelector(".user-table-container");
    tableContainer.classList.remove("loading");
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

  initializeTooltips() {
    // Initialize tooltips if using a tooltip library
    const tooltipElements = document.querySelectorAll("[data-tooltip]");
    tooltipElements.forEach((element) => {
      element.title = element.dataset.tooltip;
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

// Export for use in other scripts
window.UserManagement = UserManagement;
