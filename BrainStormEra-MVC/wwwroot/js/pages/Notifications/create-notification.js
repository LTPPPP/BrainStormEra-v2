// Global variables
let currentUserId;
let getAllUsersUrl;
let getCoursesUrl;
let allUsers = [];
let allCourses = [];
let filteredUsers = [];
let selectedUserId = null;
let selectedUserIds = [];
let selectedCourseId = null;
let currentPage = 1;
const usersPerPage = 10;
let currentTargetType = "MultipleUsers";

// Initialize notification create page
document.addEventListener("DOMContentLoaded", function () {
  console.log("Notification Create page loaded");

  // Get current user ID from meta tag or session
  currentUserId =
    document.querySelector('meta[name="user-id"]')?.getAttribute("content") ||
    "";
  // Initialize components
  initializeCharacterCounter();
  initializeTargetTypeSelection();
  initializeUserTable();
  initializeCourseSelection();
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

// Initialize user table functionality
function initializeUserTable() {
  const searchInput = document.getElementById("userSearchInput");
  const roleFilter = document.getElementById("roleFilter");
  const courseFilterUsers = document.getElementById("courseFilterUsers");

  if (searchInput) {
    let searchTimeout;
    searchInput.addEventListener("input", function () {
      clearTimeout(searchTimeout);
      searchTimeout = setTimeout(() => {
        filterUsers();
      }, 300);
    });
  }

  if (roleFilter) {
    roleFilter.addEventListener("change", function () {
      filterUsers();
    });
  }

  if (courseFilterUsers) {
    courseFilterUsers.addEventListener("change", function () {
      filterUsers();
    });
  }

  // Load all users
  loadAllUsers();
}

// Load all users from server
function loadAllUsers() {
  if (!getAllUsersUrl) {
    console.warn("Get all users URL not configured, using mock data");
    loadMockUsers();
    return;
  }
  const tableBody = document.getElementById("usersTableBody");
  tableBody.innerHTML = `
    <tr>
      <td colspan="5" class="text-center">
        <div class="loading-users">
          <i class="fas fa-spinner fa-spin me-2"></i>
          Loading users...
        </div>
      </td>
    </tr>
  `;

  fetch(getAllUsersUrl)
    .then((response) => {
      if (!response.ok) throw new Error("Failed to load users");
      return response.json();
    })
    .then((data) => {
      if (data.success && data.users) {
        allUsers = data.users.filter((user) => user.userId !== currentUserId); // Exclude current user
        filteredUsers = [...allUsers];
        renderUsersTable();
        updatePaginationInfo();
      } else {
        showError("Failed to load users");
      }
    })
    .catch((error) => {
      console.error("Error loading users:", error);
      console.log("Falling back to mock data");
      loadMockUsers();
    });
}

// Filter users based on search and role filter
function filterUsers() {
  const searchTerm =
    document.getElementById("userSearchInput")?.value.toLowerCase() || "";
  const roleFilter = document.getElementById("roleFilter")?.value || "";
  const courseFilter =
    document.getElementById("courseFilterUsers")?.value || "";

  filteredUsers = allUsers.filter((user) => {
    const matchesSearch =
      !searchTerm ||
      (user.fullName && user.fullName.toLowerCase().includes(searchTerm)) ||
      (user.userName && user.userName.toLowerCase().includes(searchTerm)) ||
      (user.email && user.email.toLowerCase().includes(searchTerm));

    const matchesRole = !roleFilter || user.role === roleFilter;

    const matchesCourse =
      !courseFilter ||
      (user.enrolledCourses && user.enrolledCourses.includes(courseFilter));

    return matchesSearch && matchesRole && matchesCourse;
  });
  currentPage = 1; // Reset to first page when filtering
  renderUserTable();
  updatePaginationInfo();
}

// Render users table
function renderUserTable() {
  const tableBody = document.getElementById("usersTableBody");

  if (filteredUsers.length === 0) {
    tableBody.innerHTML = `
      <tr>
        <td colspan="5" class="text-center">
          <div class="empty-state">
            <i class="fas fa-users"></i>
            <div>No users found</div>
            <small class="text-muted">Try adjusting your search criteria</small>
          </div>
        </td>
      </tr>
    `;
    return;
  }

  const startIndex = (currentPage - 1) * usersPerPage;
  const endIndex = startIndex + usersPerPage;
  const usersToShow = filteredUsers.slice(startIndex, endIndex);

  tableBody.innerHTML = usersToShow
    .map((user) => createUserTableRow(user))
    .join("");

  // Update pagination
  renderPagination();
}

// Create user table row
function createUserTableRow(user) {
  const isSelectedMultiple = selectedUserIds.includes(user.userId);
  const rowClass = isSelectedMultiple ? "selected-row" : "";

  // Create avatar display
  const avatarDisplay = user.avatarUrl
    ? `<img src="${user.avatarUrl}" alt="${
        user.fullName || user.userName
      }" class="user-avatar">`
    : `<div class="user-avatar-placeholder">${getInitials(
        user.fullName || user.userName
      )}</div>`;
  // Create role badge
  const roleBadge = `<span class="role-badge ${user.role}">${user.role}</span>`;

  // Create checkbox column for multi-select mode (only one checkbox needed)
  const checkboxColumn = `<td><input type="checkbox" class="form-check-input user-checkbox" 
                data-user-id="${user.userId}" 
                ${isSelectedMultiple ? "checked" : ""}
                onchange="toggleUserSelection('${user.userId}')"></td>`;

  return `
    <tr class="${rowClass}" data-user-id="${user.userId}">
      ${checkboxColumn}
      <td>${avatarDisplay}</td>
      <td>
        <div class="fw-semibold">${user.fullName || user.userName}</div>
        <small class="text-muted">@${user.userName}</small>
      </td>
      <td>${user.email}</td>
      <td>${roleBadge}</td>
    </tr>
  `;
}

// Get initials from name
function getInitials(name) {
  if (!name) return "U";
  return name
    .split(" ")
    .map((n) => n[0])
    .join("")
    .toUpperCase()
    .substring(0, 2);
}

// Select user function (global) - now only handles multi-select
window.selectUser = function (userId) {
  toggleUserSelection(userId);
};

// Clear user selection function (global function)
window.clearUserSelection = function () {
  clearAllUsers();
  // Update table display
  renderUserTable();
};

// Toggle user selection for multi-select mode
window.toggleUserSelection = function (userId) {
  if (currentTargetType !== "MultipleUsers") return;

  const user = allUsers.find((u) => u.userId === userId);
  if (!user || user.userId === currentUserId) return;

  const index = selectedUserIds.indexOf(userId);
  if (index > -1) {
    selectedUserIds.splice(index, 1);
  } else {
    selectedUserIds.push(userId);
  }

  updateSelectedUsersDisplay();
  updateMultipleUserHiddenInputs();
  renderUserTable();
};

// Update selected users display for multi-select
function updateSelectedUsersDisplay() {
  const selectedUserDisplay = document.getElementById("selectedUserDisplay");
  const selectAllBtn = document.getElementById("selectAllBtn");

  // Always hide the selected user display alert when users are selected
  // Instead, show count in the "All" button
  if (selectedUserIds.length > 0) {
    selectedUserDisplay.style.display = "none";

    // Update the Select All button to show count
    if (selectAllBtn) {
      selectAllBtn.innerHTML = `<i class="fas fa-check-double me-1"></i> All (${selectedUserIds.length})`;
      selectAllBtn.setAttribute("data-selected", "true");
    }
  } else {
    selectedUserDisplay.style.display = "none";

    // Reset Select All button text
    if (selectAllBtn) {
      selectAllBtn.innerHTML = `<i class="fas fa-check-double me-1"></i> All`;
      selectAllBtn.removeAttribute("data-selected");
    }
  }
}

// Update hidden inputs for multiple users
function updateMultipleUserHiddenInputs() {
  const container = document.getElementById("multipleUserIdsContainer");
  container.innerHTML = "";

  selectedUserIds.forEach((userId, index) => {
    const input = document.createElement("input");
    input.type = "hidden";
    input.name = `TargetUserIds[${index}]`;
    input.value = userId;
    container.appendChild(input);
  });
}

// Initialize target type selection
function initializeTargetTypeSelection() {
  const targetTypeRadios = document.querySelectorAll(
    'input[name="targetType"]'
  );
  const targetTypeHidden = document.querySelector('input[name="TargetType"]');
  const userSelectionContainer = document.getElementById(
    "userSelectionContainer"
  );
  const courseSelectionContainer = document.getElementById(
    "courseSelectionContainer"
  );
  const selectAllHeader = document.getElementById("selectAllHeader");
  const selectAllBtn = document.getElementById("selectAllBtn");
  const clearAllBtn = document.getElementById("clearAllBtn");
  const userSelectionLabel = document.getElementById("userSelectionLabel");
  const userSelectionHelpText = document.getElementById(
    "userSelectionHelpText"
  );

  // Set initial state for MultipleUsers (default)
  if (selectAllHeader) selectAllHeader.style.display = "table-cell";
  if (selectAllBtn) selectAllBtn.style.display = "inline-block";
  if (clearAllBtn) clearAllBtn.style.display = "inline-block";
  if (userSelectionLabel) userSelectionLabel.textContent = "Select Users";
  if (userSelectionHelpText)
    userSelectionHelpText.textContent =
      "Select multiple users from the table who will receive this notification. Note: You cannot send notifications to yourself.";
  updateTableColspan(6);

  targetTypeRadios.forEach((radio) => {
    radio.addEventListener("change", function () {
      currentTargetType = this.value;
      if (targetTypeHidden) {
        targetTypeHidden.value = this.value;
      }

      // Clear previous selections
      clearUserSelection();
      clearCourseSelection();

      // Show/hide appropriate sections
      if (this.value === "Course") {
        userSelectionContainer.style.display = "none";
        courseSelectionContainer.style.display = "block";
      } else {
        userSelectionContainer.style.display = "block";
        courseSelectionContainer.style.display = "none";

        // Configure UI for MultipleUsers (only option now)
        selectAllHeader.style.display = "table-cell";
        selectAllBtn.style.display = "inline-block";
        clearAllBtn.style.display = "inline-block";
        userSelectionLabel.textContent = "Select Users";
        userSelectionHelpText.textContent =
          "Select multiple users from the table who will receive this notification. Note: You cannot send notifications to yourself.";
        updateTableColspan(6);

        // Re-render table with current settings
        renderUserTable();
      }
    });
  });

  // Initialize select all/clear all buttons
  if (selectAllBtn) {
    selectAllBtn.addEventListener("click", selectAllUsers);
  }

  if (clearAllBtn) {
    clearAllBtn.addEventListener("click", clearAllUsers);
  }

  // Initialize select all checkbox
  const selectAllCheckbox = document.getElementById("selectAllCheckbox");
  if (selectAllCheckbox) {
    selectAllCheckbox.addEventListener("change", function () {
      if (this.checked) {
        selectAllUsers();
      } else {
        clearAllUsers();
      }
    });
  }
}

// Initialize course selection
function initializeCourseSelection() {
  const courseFilter = document.getElementById("courseFilter");

  if (courseFilter) {
    courseFilter.addEventListener("change", function () {
      selectedCourseId = this.value;
      const courseIdHidden = document.getElementById("selectedCourseId");
      if (courseIdHidden) {
        courseIdHidden.value = this.value;
      }
    });
  }

  // Load courses
  loadCourses();
}

// Load courses from server
function loadCourses() {
  const courseFilter = document.getElementById("courseFilter");
  const courseFilterUsers = document.getElementById("courseFilterUsers");

  if (!getCoursesUrl) {
    console.warn("Get courses URL not configured, using mock data");
    loadMockCourses();
    return;
  }

  fetch(getCoursesUrl)
    .then((response) => {
      if (!response.ok) throw new Error("Failed to load courses");
      return response.json();
    })
    .then((data) => {
      allCourses = data || [];
      populateCourseFilters();
    })
    .catch((error) => {
      console.error("Error loading courses:", error);
      loadMockCourses();
    });
}

// Load mock courses for development
function loadMockCourses() {
  allCourses = [
    { courseId: "1", title: "Introduction to Programming", studentsCount: 25 },
    { courseId: "2", title: "Web Development Fundamentals", studentsCount: 30 },
    { courseId: "3", title: "Database Design", studentsCount: 20 },
    { courseId: "4", title: "Mobile App Development", studentsCount: 18 },
    { courseId: "5", title: "Data Structures", studentsCount: 22 },
  ];
  populateCourseFilters();
}

// Populate course filter dropdowns
function populateCourseFilters() {
  const courseFilter = document.getElementById("courseFilter");
  const courseFilterUsers = document.getElementById("courseFilterUsers");

  if (courseFilter) {
    courseFilter.innerHTML = '<option value="">Select a course...</option>';
    allCourses.forEach((course) => {
      const option = document.createElement("option");
      option.value = course.courseId;
      option.textContent = `${course.title} (${course.studentsCount} students)`;
      courseFilter.appendChild(option);
    });
  }

  if (courseFilterUsers) {
    courseFilterUsers.innerHTML = '<option value="">All Courses</option>';
    allCourses.forEach((course) => {
      const option = document.createElement("option");
      option.value = course.courseId;
      option.textContent = course.title;
      courseFilterUsers.appendChild(option);
    });
  }
}

// Update table colspan based on target type
function updateTableColspan(colspan) {
  const loadingColspan = document.getElementById("loadingColspan");
  if (loadingColspan) {
    loadingColspan.colSpan = colspan;
  }
}

// Select all users
function selectAllUsers() {
  if (currentTargetType !== "MultipleUsers") return;

  selectedUserIds = [...filteredUsers.map((user) => user.userId)];
  // Remove current user if present
  selectedUserIds = selectedUserIds.filter((id) => id !== currentUserId);

  updateSelectedUsersDisplay();
  updateMultipleUserHiddenInputs();
  renderUserTable();

  const selectAllCheckbox = document.getElementById("selectAllCheckbox");
  if (selectAllCheckbox) {
    selectAllCheckbox.checked = true;
  }
}

// Clear all users
function clearAllUsers() {
  selectedUserIds = [];
  updateSelectedUsersDisplay();
  updateMultipleUserHiddenInputs();
  renderUserTable();

  const selectAllCheckbox = document.getElementById("selectAllCheckbox");
  if (selectAllCheckbox) {
    selectAllCheckbox.checked = false;
  }
}

// Clear course selection
function clearCourseSelection() {
  selectedCourseId = null;
  const courseFilter = document.getElementById("courseFilter");
  const courseIdHidden = document.getElementById("selectedCourseId");

  if (courseFilter) {
    courseFilter.value = "";
  }
  if (courseIdHidden) {
    courseIdHidden.value = "";
  }
}

// Render pagination
function renderPagination() {
  const totalPages = Math.ceil(filteredUsers.length / usersPerPage);
  const pagination = document.getElementById("usersPagination");

  if (!pagination) return;

  if (totalPages <= 1) {
    pagination.innerHTML = "";
    return;
  }

  let paginationHtml = "";

  // Previous button
  paginationHtml += `
    <li class="page-item ${currentPage === 1 ? "disabled" : ""}">
      <a class="page-link" href="#" onclick="changePage(${
        currentPage - 1
      }); return false;" aria-label="Previous">
        <span aria-hidden="true">&laquo;</span>
      </a>
    </li>
  `;

  // Page numbers
  const startPage = Math.max(1, currentPage - 2);
  const endPage = Math.min(totalPages, currentPage + 2);

  if (startPage > 1) {
    paginationHtml += `<li class="page-item"><a class="page-link" href="#" onclick="changePage(1); return false;">1</a></li>`;
    if (startPage > 2) {
      paginationHtml += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
    }
  }

  for (let i = startPage; i <= endPage; i++) {
    paginationHtml += `
      <li class="page-item ${i === currentPage ? "active" : ""}">
        <a class="page-link" href="#" onclick="changePage(${i}); return false;">${i}</a>
      </li>
    `;
  }

  if (endPage < totalPages) {
    if (endPage < totalPages - 1) {
      paginationHtml += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
    }
    paginationHtml += `<li class="page-item"><a class="page-link" href="#" onclick="changePage(${totalPages}); return false;">${totalPages}</a></li>`;
  }

  // Next button
  paginationHtml += `
    <li class="page-item ${currentPage === totalPages ? "disabled" : ""}">
      <a class="page-link" href="#" onclick="changePage(${
        currentPage + 1
      }); return false;" aria-label="Next">
        <span aria-hidden="true">&raquo;</span>
      </a>
    </li>
  `;

  pagination.innerHTML = paginationHtml;
}

// Change page function (global)
window.changePage = function (page) {
  const totalPages = Math.ceil(filteredUsers.length / usersPerPage);
  if (page < 1 || page > totalPages) return;

  currentPage = page;
  renderUsersTable();
};

// Update pagination info
function updatePaginationInfo() {
  const showingStart = document.getElementById("showingStart");
  const showingEnd = document.getElementById("showingEnd");
  const totalUsers = document.getElementById("totalUsers");

  if (showingStart && showingEnd && totalUsers) {
    const startIndex = (currentPage - 1) * usersPerPage + 1;
    const endIndex = Math.min(currentPage * usersPerPage, filteredUsers.length);

    showingStart.textContent = filteredUsers.length > 0 ? startIndex : 0;
    showingEnd.textContent = endIndex;
    totalUsers.textContent = filteredUsers.length;
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
  if (currentTargetType === "Course") {
    const selectedCourseIdValue =
      document.getElementById("selectedCourseId")?.value;

    if (!selectedCourseIdValue) {
      showToast("Please select a course to send the notification to", "error");
      return false;
    }

    return true;
  } else {
    // Multiple users validation (default mode)
    if (selectedUserIds.length === 0) {
      showToast(
        "Please select at least one user to send the notification to",
        "error"
      );
      return false;
    }

    return true;
  }
}

// Helper function for toast notifications
function showToast(message, type = "info") {
  const toast = document.createElement("div");
  toast.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
  toast.style.cssText =
    "top: 20px; right: 20px; z-index: 9999; min-width: 300px;";
  toast.innerHTML = `
    ${message}
    <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
  `;

  document.body.appendChild(toast);

  setTimeout(() => {
    if (toast.parentNode) {
      toast.remove();
    }
  }, 5000);
}

// Show error in table
function showError(message) {
  const tableBody = document.getElementById("usersTableBody");
  if (tableBody) {
    tableBody.innerHTML = `
      <tr>        <td colspan="5" class="text-center">
          <div class="empty-state text-danger">
            <i class="fas fa-exclamation-triangle"></i>
            <div>${message}</div>
          </div>
        </td>
      </tr>
    `;
  }
}

// Show error in course table
function showCourseError(message) {
  const tableBody = document.getElementById("coursesTableBody");
  if (tableBody) {
    tableBody.innerHTML = `
      <tr>        <td colspan="5" class="text-center">
          <div class="empty-state text-danger">
            <i class="fas fa-exclamation-triangle"></i>
            <div>${message}</div>
          </div>
        </td>
      </tr>
    `;
  }
}

// Set URLs for external script configuration (global functions)
window.setGetAllUsersUrl = function (url) {
  getAllUsersUrl = url;
};

window.setGetCoursesUrl = function (url) {
  getCoursesUrl = url;
};

// Set get all users URL (to be called by server-side code)
function setGetAllUsersUrl(url) {
  getAllUsersUrl = url;
}

// Load mock users for demonstration
function loadMockUsers() {
  // Mock user data for demonstration
  const mockUsers = [
    {
      userId: "user001",
      userName: "john_doe",
      fullName: "John Doe",
      email: "john.doe@example.com",
      role: "learner",
      isActive: true,
      avatarUrl: null,
      enrolledCourses: ["1", "2"],
    },
    {
      userId: "user002",
      userName: "jane_smith",
      fullName: "Jane Smith",
      email: "jane.smith@example.com",
      role: "instructor",
      isActive: true,
      avatarUrl: null,
      enrolledCourses: ["2", "3", "4"],
    },
    {
      userId: "user003",
      userName: "admin_user",
      fullName: "Admin User",
      email: "admin@example.com",
      role: "admin",
      isActive: true,
      avatarUrl: null,
      enrolledCourses: [],
    },
    {
      userId: "user004",
      userName: "alice_johnson",
      fullName: "Alice Johnson",
      email: "alice.johnson@example.com",
      role: "learner",
      isActive: true,
      avatarUrl: null,
      enrolledCourses: ["1", "3", "5"],
    },
    {
      userId: "user005",
      userName: "bob_wilson",
      fullName: "Bob Wilson",
      email: "bob.wilson@example.com",
      role: "instructor",
      isActive: false,
      avatarUrl: null,
    },
    {
      userId: "user006",
      userName: "charlie_brown",
      fullName: "Charlie Brown",
      email: "charlie.brown@example.com",
      role: "learner",
      isActive: true,
      avatarUrl: null,
    },
    {
      userId: "user007",
      userName: "diana_prince",
      fullName: "Diana Prince",
      email: "diana.prince@example.com",
      role: "instructor",
      isActive: true,
      avatarUrl: null,
    },
    {
      userId: "user008",
      userName: "edward_norton",
      fullName: "Edward Norton",
      email: "edward.norton@example.com",
      role: "learner",
      isActive: true,
      avatarUrl: null,
    },
    {
      userId: "user009",
      userName: "fiona_green",
      fullName: "Fiona Green",
      email: "fiona.green@example.com",
      role: "admin",
      isActive: true,
      avatarUrl: null,
    },
    {
      userId: "user010",
      userName: "george_white",
      fullName: "George White",
      email: "george.white@example.com",
      role: "learner",
      isActive: false,
      avatarUrl: null,
    },
    {
      userId: "user011",
      userName: "helen_black",
      fullName: "Helen Black",
      email: "helen.black@example.com",
      role: "instructor",
      isActive: true,
      avatarUrl: null,
    },
    {
      userId: "user012",
      userName: "ivan_red",
      fullName: "Ivan Red",
      email: "ivan.red@example.com",
      role: "learner",
      isActive: true,
      avatarUrl: null,
    },
    {
      userId: "user013",
      userName: "julia_yellow",
      fullName: "Julia Yellow",
      email: "julia.yellow@example.com",
      role: "instructor",
      isActive: true,
      avatarUrl: null,
    },
    {
      userId: "user014",
      userName: "kevin_blue",
      fullName: "Kevin Blue",
      email: "kevin.blue@example.com",
      role: "learner",
      isActive: true,
      avatarUrl: null,
    },
    {
      userId: "user015",
      userName: "linda_purple",
      fullName: "Linda Purple",
      email: "linda.purple@example.com",
      role: "admin",
      isActive: true,
      avatarUrl: null,
    },
  ];

  // Filter out current user from mock data
  allUsers = mockUsers.filter((user) => user.userId !== currentUserId);
  filteredUsers = [...allUsers];

  // Simulate loading delay
  setTimeout(() => {
    renderUsersTable();
    updatePaginationInfo();
  }, 500);
}
