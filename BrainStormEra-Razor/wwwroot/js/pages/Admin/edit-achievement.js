// Edit Achievement Page JavaScript
document.addEventListener("DOMContentLoaded", function () {
  initializeEditAchievement();
});

function initializeEditAchievement() {
  // Initialize components
  initializeIconSelection();
  initializeFileUpload();
  initializeLivePreview();
  initializeFormValidation();
  initializeCharacterCounter();

  // Load existing data into preview
  loadExistingDataIntoPreview();
}

// Icon Selection Management
function initializeIconSelection() {
  const iconTabs = document.querySelectorAll(".icon-tab");
  const predefinedSection = document.querySelector(".predefined-icons-section");
  const customSection = document.querySelector(".custom-upload-section");
  const iconOptions = document.querySelectorAll(".icon-option");
  const selectedIconInput = document.getElementById("selectedIcon");

  // Tab switching
  iconTabs.forEach((tab) => {
    tab.addEventListener("click", function () {
      const type = this.dataset.type;

      // Update active tab
      iconTabs.forEach((t) => t.classList.remove("active"));
      this.classList.add("active");

      // Show/hide sections
      if (type === "predefined") {
        predefinedSection.style.display = "block";
        customSection.style.display = "none";

        // If switching back to predefined, reset to current selection or default
        const currentIcon = selectedIconInput.value;
        if (
          currentIcon &&
          !currentIcon.startsWith("/uploads/") &&
          !currentIcon.startsWith("CUSTOM_UPLOAD:")
        ) {
          selectPredefinedIcon(currentIcon);
        } else {
          selectPredefinedIcon("fas fa-trophy");
        }
      } else {
        predefinedSection.style.display = "none";
        customSection.style.display = "block";

        // Clear predefined selection
        iconOptions.forEach((option) => option.classList.remove("selected"));

        // Set to custom upload mode
        selectedIconInput.value = "CUSTOM_UPLOAD:";
        updatePreviewIcon("CUSTOM_UPLOAD:");
      }
    });
  });

  // Predefined icon selection
  iconOptions.forEach((option) => {
    option.addEventListener("click", function () {
      const iconClass = this.dataset.icon;
      selectPredefinedIcon(iconClass);
    });
  });

  function selectPredefinedIcon(iconClass) {
    // Update selection
    iconOptions.forEach((option) => option.classList.remove("selected"));
    const selectedOption = document.querySelector(`[data-icon="${iconClass}"]`);
    if (selectedOption) {
      selectedOption.classList.add("selected");
    }

    // Update hidden input and preview
    selectedIconInput.value = iconClass;
    updatePreviewIcon(iconClass);
  }

  // Set initial selection based on existing data
  const currentIcon = selectedIconInput.value;
  if (currentIcon) {
    if (currentIcon.startsWith("/uploads/")) {
      // Custom uploaded icon
      const customTab = document.querySelector('[data-type="custom"]');
      if (customTab) {
        customTab.click();
      }
      updatePreviewIcon(currentIcon);
    } else if (!currentIcon.startsWith("CUSTOM_UPLOAD:")) {
      // Predefined icon
      selectPredefinedIcon(currentIcon);
    }
  }
}

// File Upload Management
function initializeFileUpload() {
  const uploadArea = document.getElementById("uploadArea");
  const fileInput = document.getElementById("iconFile");
  const uploadPreview = document.getElementById("uploadPreview");
  const previewImage = document.getElementById("previewImage");
  const removeButton = document.getElementById("removeUpload");

  // Click to upload
  uploadArea.addEventListener("click", () => {
    fileInput.click();
  });

  // Drag and drop
  uploadArea.addEventListener("dragover", (e) => {
    e.preventDefault();
    uploadArea.classList.add("dragover");
  });

  uploadArea.addEventListener("dragleave", (e) => {
    e.preventDefault();
    uploadArea.classList.remove("dragover");
  });

  uploadArea.addEventListener("drop", (e) => {
    e.preventDefault();
    uploadArea.classList.remove("dragover");

    const files = e.dataTransfer.files;
    if (files.length > 0) {
      handleFileSelection(files[0]);
    }
  });

  // File input change
  fileInput.addEventListener("change", (e) => {
    if (e.target.files.length > 0) {
      handleFileSelection(e.target.files[0]);
    }
  });

  // Remove upload
  removeButton.addEventListener("click", () => {
    clearFileUpload();
  });

  function handleFileSelection(file) {
    // Validate file
    const validTypes = [
      "image/jpeg",
      "image/jpg",
      "image/png",
      "image/svg+xml",
      "image/webp",
    ];
    const maxSize = 2 * 1024 * 1024; // 2MB

    if (!validTypes.includes(file.type)) {
      showError(
        "Invalid file type. Only PNG, JPG, SVG, and WEBP files are allowed."
      );
      return;
    }

    if (file.size > maxSize) {
      showError("File size must be less than 2MB.");
      return;
    }

    // Show preview
    const reader = new FileReader();
    reader.onload = function (e) {
      previewImage.src = e.target.result;
      uploadArea.style.display = "none";
      uploadPreview.style.display = "block";

      // Update preview
      updatePreviewIcon(e.target.result);
    };
    reader.readAsDataURL(file);
  }

  function clearFileUpload() {
    fileInput.value = "";
    uploadArea.style.display = "block";
    uploadPreview.style.display = "none";
    previewImage.src = "";

    // Reset to predefined icon
    const predefinedTab = document.querySelector('[data-type="predefined"]');
    if (predefinedTab) {
      predefinedTab.click();
    }
  }
}

// Live Preview Management
function initializeLivePreview() {
  const nameInput = document.querySelector(
    '[name="Achievement.AchievementName"]'
  );
  const descriptionInput = document.querySelector(
    '[name="Achievement.AchievementDescription"]'
  );
  const typeSelect = document.querySelector(
    '[name="Achievement.AchievementType"]'
  );

  // Update preview on input changes
  nameInput?.addEventListener("input", updatePreviewName);
  descriptionInput?.addEventListener("input", updatePreviewDescription);
  typeSelect?.addEventListener("change", updatePreviewType);
}

function loadExistingDataIntoPreview() {
  // Load existing values into preview
  updatePreviewName();
  updatePreviewDescription();
  updatePreviewType();

  // Load existing icon
  const iconInput = document.getElementById("selectedIcon");
  if (iconInput && iconInput.value) {
    updatePreviewIcon(iconInput.value);
  }
}

function updatePreviewName() {
  const nameInput = document.querySelector(
    '[name="Achievement.AchievementName"]'
  );
  const previewName = document.querySelector(".preview-name");

  if (nameInput && previewName) {
    const value = nameInput.value.trim();
    previewName.textContent = value || "Achievement Name";
  }
}

function updatePreviewDescription() {
  const descriptionInput = document.querySelector(
    '[name="Achievement.AchievementDescription"]'
  );
  const previewDescription = document.querySelector(".preview-description");

  if (descriptionInput && previewDescription) {
    const value = descriptionInput.value.trim();
    previewDescription.textContent =
      value || "Achievement description will appear here";
  }
}

function updatePreviewType() {
  const typeSelect = document.querySelector(
    '[name="Achievement.AchievementType"]'
  );
  const previewType = document.querySelector(".preview-type");

  if (typeSelect && previewType) {
    const value = typeSelect.value;
    const typeNames = {
      course_completion: "Course Completion",
      quiz_master: "Quiz Master",
      streak: "Learning Streak",
      first_course: "First Course",
      instructor: "Instructor Achievement",
      student_engagement: "Student Engagement",
    };

    previewType.textContent = typeNames[value] || "Type";
  }
}

function updatePreviewIcon(iconValue) {
  const previewIcon = document.querySelector(".preview-icon");
  const previewCustomIcon = document.querySelector(".preview-custom-icon");

  if (!iconValue) {
    iconValue = "fas fa-trophy";
  }

  if (iconValue.startsWith("/uploads/") || iconValue.startsWith("data:")) {
    // Custom uploaded image
    if (previewIcon) previewIcon.style.display = "none";
    if (previewCustomIcon) {
      previewCustomIcon.src = iconValue;
      previewCustomIcon.style.display = "block";
    }
  } else if (iconValue.startsWith("CUSTOM_UPLOAD:")) {
    // Custom upload placeholder
    if (previewIcon) {
      previewIcon.className = "fas fa-upload preview-icon";
      previewIcon.style.display = "block";
    }
    if (previewCustomIcon) previewCustomIcon.style.display = "none";
  } else {
    // FontAwesome icon
    if (previewIcon) {
      previewIcon.className = iconValue + " preview-icon";
      previewIcon.style.display = "block";
    }
    if (previewCustomIcon) previewCustomIcon.style.display = "none";
  }
}

// Form Validation
function initializeFormValidation() {
  const form = document.querySelector(".achievement-form");
  const submitButton = document.getElementById("updateBtn");

  if (form) {
    form.addEventListener("submit", function (e) {
      if (!validateForm()) {
        e.preventDefault();
        return false;
      }

      // Show loading state
      if (submitButton) {
        submitButton.classList.add("loading");
        submitButton.disabled = true;
      }
    });
  }
}

function validateForm() {
  let isValid = true;
  const errors = [];

  // Validate name
  const nameInput = document.querySelector(
    '[name="Achievement.AchievementName"]'
  );
  if (nameInput) {
    const name = nameInput.value.trim();
    if (!name) {
      errors.push("Achievement name is required");
      highlightError(nameInput);
      isValid = false;
    } else if (name.length > 100) {
      errors.push("Achievement name cannot exceed 100 characters");
      highlightError(nameInput);
      isValid = false;
    } else {
      clearError(nameInput);
    }
  }

  // Validate type
  const typeSelect = document.querySelector(
    '[name="Achievement.AchievementType"]'
  );
  if (typeSelect) {
    const type = typeSelect.value;
    if (!type) {
      errors.push("Achievement type is required");
      highlightError(typeSelect);
      isValid = false;
    } else {
      clearError(typeSelect);
    }
  }

  // Validate description length
  const descriptionInput = document.querySelector(
    '[name="Achievement.AchievementDescription"]'
  );
  if (descriptionInput) {
    const description = descriptionInput.value.trim();
    if (description.length > 500) {
      errors.push("Achievement description cannot exceed 500 characters");
      highlightError(descriptionInput);
      isValid = false;
    } else {
      clearError(descriptionInput);
    }
  }

  // Show errors if any
  if (errors.length > 0) {
    showError(errors.join(", "));
  }

  return isValid;
}

function highlightError(element) {
  element.style.borderColor = "#e74c3c";
  element.style.boxShadow = "0 0 0 3px rgba(231, 76, 60, 0.1)";
}

function clearError(element) {
  element.style.borderColor = "";
  element.style.boxShadow = "";
}

// Character Counter
function initializeCharacterCounter() {
  const descriptionInput = document.querySelector(
    '[name="Achievement.AchievementDescription"]'
  );
  const charCounter = document.getElementById("descCharCount");

  if (descriptionInput && charCounter) {
    function updateCounter() {
      const count = descriptionInput.value.length;
      charCounter.textContent = count;

      // Change color based on limit
      if (count > 450) {
        charCounter.style.color = "#e74c3c";
      } else if (count > 400) {
        charCounter.style.color = "#f39c12";
      } else {
        charCounter.style.color = "rgba(255, 255, 255, 0.7)";
      }
    }

    descriptionInput.addEventListener("input", updateCounter);
    updateCounter(); // Initial count
  }
}

// Utility Functions
function showError(message) {
  // Remove existing alerts
  const existingAlerts = document.querySelectorAll(".alert");
  existingAlerts.forEach((alert) => alert.remove());

  // Create and show error alert
  const alert = document.createElement("div");
  alert.className = "alert alert-error";
  alert.innerHTML = `
        <i class="fas fa-exclamation-circle"></i>
        ${message}
    `;

  document.body.appendChild(alert);

  // Auto remove after 5 seconds
  setTimeout(() => {
    if (alert.parentNode) {
      alert.remove();
    }
  }, 5000);
}

function showSuccess(message) {
  // Remove existing alerts
  const existingAlerts = document.querySelectorAll(".alert");
  existingAlerts.forEach((alert) => alert.remove());

  // Create and show success alert
  const alert = document.createElement("div");
  alert.className = "alert alert-success";
  alert.innerHTML = `
        <i class="fas fa-check-circle"></i>
        ${message}
    `;

  document.body.appendChild(alert);

  // Auto remove after 5 seconds
  setTimeout(() => {
    if (alert.parentNode) {
      alert.remove();
    }
  }, 5000);
}

// Cancel Edit Function
function cancelEdit() {
  if (
    confirm(
      "Are you sure you want to cancel? Any unsaved changes will be lost."
    )
  ) {
    window.location.href = "/admin/achievements";
  }
}

// Auto-hide alerts on page load
document.addEventListener("DOMContentLoaded", function () {
  const alerts = document.querySelectorAll(".alert");
  alerts.forEach((alert) => {
    setTimeout(() => {
      if (alert.parentNode) {
        alert.style.opacity = "0";
        alert.style.transform = "translateX(100%)";
        setTimeout(() => alert.remove(), 300);
      }
    }, 5000);
  });
});

// Prevent form submission on Enter key in text inputs (except textarea)
document.addEventListener("DOMContentLoaded", function () {
  const textInputs = document.querySelectorAll('input[type="text"], select');
  textInputs.forEach((input) => {
    input.addEventListener("keypress", function (e) {
      if (e.key === "Enter") {
        e.preventDefault();
      }
    });
  });
});

// Handle browser back button
window.addEventListener("beforeunload", function (e) {
  const form = document.querySelector(".achievement-form");
  if (form && isFormDirty()) {
    e.preventDefault();
    e.returnValue = "";
    return "";
  }
});

function isFormDirty() {
  // Simple check if form has been modified
  const inputs = document.querySelectorAll("input, textarea, select");
  for (let input of inputs) {
    if (input.defaultValue !== input.value) {
      return true;
    }
  }
  return false;
}
