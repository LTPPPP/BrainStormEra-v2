// Create Achievement Page JavaScript
document.addEventListener("DOMContentLoaded", function () {
  initializeCreateAchievementPage();
});

function initializeCreateAchievementPage() {
  initializeIconPicker();
  initializeFileUpload();
  initializeLivePreview();
  initializeFormValidation();
  initializeFormHandlers();
}

// Icon Picker Functionality
function initializeIconPicker() {
  const iconOptions = document.querySelectorAll(".icon-option");
  const hiddenInput = document.querySelector(
    'input[name="Achievement.AchievementIcon"]'
  );
  const selectedIcon = document.querySelector(".selected-icon");
  const iconName = document.querySelector(".icon-name");
  const iconClass = document.querySelector(".icon-class");

  iconOptions.forEach((option) => {
    option.addEventListener("click", function () {
      const iconValue = this.getAttribute("data-icon");
      const iconNameValue = this.getAttribute("data-name");

      // Update hidden input
      hiddenInput.value = iconValue;

      // Update selected icon display
      selectedIcon.className = iconValue + " selected-icon";
      iconName.textContent = iconNameValue;
      iconClass.textContent = iconValue;

      // Update visual selection
      iconOptions.forEach((opt) => opt.classList.remove("selected"));
      this.classList.add("selected");

      // Update preview
      updatePreviewIcon(iconValue);

      // Close picker
      closeIconPicker();
    });
  });

  // Set initial selection
  const currentIcon = hiddenInput.value;
  const currentOption = document.querySelector(`[data-icon="${currentIcon}"]`);
  if (currentOption) {
    currentOption.classList.add("selected");
  }
}

function openIconPicker() {
  const iconGrid = document.getElementById("iconGrid");
  iconGrid.style.display = "block";

  // Add click outside to close
  setTimeout(() => {
    document.addEventListener("click", handleClickOutsideIconPicker);
  }, 100);
}

function closeIconPicker() {
  const iconGrid = document.getElementById("iconGrid");
  iconGrid.style.display = "none";
  document.removeEventListener("click", handleClickOutsideIconPicker);
}

function handleClickOutsideIconPicker(event) {
  const iconGrid = document.getElementById("iconGrid");
  const iconPickerContainer = document.querySelector(".icon-picker-container");

  if (!iconPickerContainer.contains(event.target)) {
    closeIconPicker();
  }
}

// Live Preview Functionality
function initializeLivePreview() {
  const nameInput = document.querySelector(
    'input[name="Achievement.AchievementName"]'
  );
  const descriptionInput = document.querySelector(
    'textarea[name="Achievement.AchievementDescription"]'
  );
  const typeSelect = document.querySelector(
    'select[name="Achievement.AchievementType"]'
  );
  const iconInput = document.querySelector(
    'input[name="Achievement.AchievementIcon"]'
  );

  // Update preview on input changes
  nameInput.addEventListener("input", updatePreviewName);
  descriptionInput.addEventListener("input", updatePreviewDescription);
  typeSelect.addEventListener("change", updatePreviewType);
  iconInput.addEventListener("change", function () {
    updatePreviewIcon(this.value);
  });

  // Initial preview update
  updatePreviewName();
  updatePreviewDescription();
  updatePreviewType();
  updatePreviewIcon(iconInput.value);
}

function updatePreviewName() {
  const nameInput = document.querySelector(
    'input[name="Achievement.AchievementName"]'
  );
  const previewName = document.querySelector(".preview-name");

  const name = nameInput.value.trim();
  previewName.textContent = name || "Achievement Name";
}

function updatePreviewDescription() {
  const descriptionInput = document.querySelector(
    'textarea[name="Achievement.AchievementDescription"]'
  );
  const previewDescription = document.querySelector(".preview-description");

  const description = descriptionInput.value.trim();
  previewDescription.textContent =
    description || "Achievement description will appear here...";
}

function updatePreviewType() {
  const typeSelect = document.querySelector(
    'select[name="Achievement.AchievementType"]'
  );
  const previewType = document.querySelector(".preview-type");

  const type = typeSelect.value;
  let typeText = "Select Type";

  switch (type) {
    case "course_completion":
      typeText = "Course Completion";
      break;
    case "first_course":
      typeText = "First Course";
      break;
    case "quiz_master":
      typeText = "Quiz Master";
      break;
    case "streak":
      typeText = "Streak Achievement";
      break;
    case "instructor":
      typeText = "Instructor Achievement";
      break;
    case "student_engagement":
      typeText = "Student Engagement";
      break;
  }

  previewType.innerHTML = `<i class="fas fa-tag"></i> ${typeText}`;
}

function updatePreviewIcon(iconClass) {
  const previewIcon = document.querySelector(".preview-icon");
  const badgeIcon = document.querySelector(".achievement-badge i");

  if (iconClass) {
    previewIcon.className = iconClass + " preview-icon";
    badgeIcon.className = iconClass;
  }
}

// File Upload Functionality
function initializeFileUpload() {
  const dropzone = document.getElementById("uploadDropzone");
  const fileInput = document.getElementById("iconFileInput");

  // Drag and drop events
  dropzone.addEventListener("dragover", handleDragOver);
  dropzone.addEventListener("dragleave", handleDragLeave);
  dropzone.addEventListener("drop", handleDrop);
  dropzone.addEventListener("click", triggerFileInput);

  // File input change event
  fileInput.addEventListener("change", function () {
    handleFileSelect(this);
  });
}

function handleDragOver(e) {
  e.preventDefault();
  e.stopPropagation();
  this.classList.add("dragover");
}

function handleDragLeave(e) {
  e.preventDefault();
  e.stopPropagation();
  this.classList.remove("dragover");
}

function handleDrop(e) {
  e.preventDefault();
  e.stopPropagation();
  this.classList.remove("dragover");

  const files = e.dataTransfer.files;
  if (files.length > 0) {
    const fileInput = document.getElementById("iconFileInput");
    fileInput.files = files;
    handleFileSelect(fileInput);
  }
}

function triggerFileInput() {
  document.getElementById("iconFileInput").click();
}

function handleFileSelect(input) {
  const file = input.files[0];
  if (!file) return;

  // Validate file
  const validation = validateImageFile(file);
  if (!validation.valid) {
    showUploadError(validation.error);
    input.value = "";
    return;
  }

  // Show preview
  showUploadPreview(file);

  // Update icon value for backend
  const iconInput = document.querySelector(
    'input[name="Achievement.AchievementIcon"]'
  );
  iconInput.value = `CUSTOM_UPLOAD:${file.name}`;

  // Update live preview
  const reader = new FileReader();
  reader.onload = function (e) {
    updatePreviewWithCustomIcon(e.target.result);
  };
  reader.readAsDataURL(file);
}

function validateImageFile(file) {
  const allowedTypes = [
    "image/jpeg",
    "image/jpg",
    "image/png",
    "image/svg+xml",
    "image/webp",
  ];
  const maxSize = 2 * 1024 * 1024; // 2MB

  if (!allowedTypes.includes(file.type)) {
    return {
      valid: false,
      error: "Please select a valid image file (PNG, JPG, SVG, WEBP)",
    };
  }

  if (file.size > maxSize) {
    return {
      valid: false,
      error: "File size must be less than 2MB",
    };
  }

  return { valid: true };
}

function showUploadPreview(file) {
  const uploadPreview = document.getElementById("uploadPreview");
  const previewImage = document.getElementById("previewImage");
  const fileName = document.getElementById("fileName");
  const fileSize = document.getElementById("fileSize");

  // Show preview container
  uploadPreview.style.display = "block";

  // Update file info
  fileName.textContent = file.name;
  fileSize.textContent = formatFileSize(file.size);

  // Load and show image
  const reader = new FileReader();
  reader.onload = function (e) {
    previewImage.src = e.target.result;
  };
  reader.readAsDataURL(file);

  // Hide dropzone
  document.getElementById("uploadDropzone").style.display = "none";
}

function removeUploadedImage() {
  const uploadPreview = document.getElementById("uploadPreview");
  const uploadDropzone = document.getElementById("uploadDropzone");
  const fileInput = document.getElementById("iconFileInput");
  const iconInput = document.querySelector(
    'input[name="Achievement.AchievementIcon"]'
  );

  // Reset UI
  uploadPreview.style.display = "none";
  uploadDropzone.style.display = "block";
  uploadDropzone.classList.remove("upload-error");

  // Clear inputs
  fileInput.value = "";
  iconInput.value = "fas fa-trophy";

  // Reset preview to default icon
  updatePreviewIcon("fas fa-trophy");

  // Clear any error messages
  clearUploadError();
}

function showUploadError(message) {
  const dropzone = document.getElementById("uploadDropzone");
  dropzone.classList.add("upload-error");

  // Remove existing error message
  clearUploadError();

  // Add error message
  const errorDiv = document.createElement("div");
  errorDiv.className = "upload-error-message";
  errorDiv.innerHTML = `<i class="fas fa-exclamation-triangle"></i> ${message}`;

  dropzone.parentNode.appendChild(errorDiv);

  // Auto-remove error after 5 seconds
  setTimeout(clearUploadError, 5000);
}

function clearUploadError() {
  const dropzone = document.getElementById("uploadDropzone");
  const errorMessage = dropzone.parentNode.querySelector(
    ".upload-error-message"
  );

  dropzone.classList.remove("upload-error");
  if (errorMessage) {
    errorMessage.remove();
  }
}

function formatFileSize(bytes) {
  if (bytes === 0) return "0 Bytes";
  const k = 1024;
  const sizes = ["Bytes", "KB", "MB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i];
}

function updatePreviewWithCustomIcon(imageSrc) {
  const previewIcon = document.querySelector(".preview-icon");
  const previewCustomIcon = document.querySelector(".preview-custom-icon");

  // Hide FontAwesome icon, show custom image
  previewIcon.style.display = "none";
  previewCustomIcon.src = imageSrc;
  previewCustomIcon.style.display = "block";
}

// Form Validation
function initializeFormValidation() {
  const form = document.querySelector(".achievement-form");
  const nameInput = document.querySelector(
    'input[name="Achievement.AchievementName"]'
  );
  const typeSelect = document.querySelector(
    'select[name="Achievement.AchievementType"]'
  );

  // Real-time validation
  nameInput.addEventListener("blur", validateName);
  typeSelect.addEventListener("blur", validateType);

  // Character count for name
  nameInput.addEventListener("input", updateCharacterCount);
}

function validateName() {
  const nameInput = document.querySelector(
    'input[name="Achievement.AchievementName"]'
  );
  const value = nameInput.value.trim();

  clearFieldErrors(nameInput);

  if (!value) {
    showFieldError(nameInput, "Achievement name is required.");
    return false;
  }

  if (value.length > 100) {
    showFieldError(nameInput, "Achievement name cannot exceed 100 characters.");
    return false;
  }

  return true;
}

function validateType() {
  const typeSelect = document.querySelector(
    'select[name="Achievement.AchievementType"]'
  );
  const value = typeSelect.value;

  clearFieldErrors(typeSelect);

  if (!value) {
    showFieldError(typeSelect, "Achievement type is required.");
    return false;
  }

  return true;
}

function updateCharacterCount() {
  const nameInput = document.querySelector(
    'input[name="Achievement.AchievementName"]'
  );
  const currentLength = nameInput.value.length;
  const maxLength = 100;

  // Find or create character count display
  let charCount = nameInput.parentNode.querySelector(".char-count");
  if (!charCount) {
    charCount = document.createElement("div");
    charCount.className = "char-count";
    charCount.style.fontSize = "0.75rem";
    charCount.style.color = "#6b7280";
    charCount.style.marginTop = "0.25rem";
    nameInput.parentNode.appendChild(charCount);
  }

  charCount.textContent = `${currentLength}/${maxLength} characters`;

  if (currentLength > maxLength) {
    charCount.style.color = "#dc2626";
  } else if (currentLength > maxLength * 0.8) {
    charCount.style.color = "#f59e0b";
  } else {
    charCount.style.color = "#6b7280";
  }
}

function showFieldError(field, message) {
  const errorSpan = field.parentNode.querySelector(".field-validation-error");
  if (errorSpan) {
    errorSpan.textContent = message;
    errorSpan.style.display = "block";
  }

  field.style.borderColor = "#dc2626";
}

function clearFieldErrors(field) {
  const errorSpan = field.parentNode.querySelector(".field-validation-error");
  if (errorSpan) {
    errorSpan.style.display = "none";
  }

  field.style.borderColor = "#d1d5db";
}

// Form Handlers
function initializeFormHandlers() {
  const form = document.querySelector(".achievement-form");
  const createBtn = document.getElementById("createBtn");

  form.addEventListener("submit", handleFormSubmit);
}

function handleFormSubmit(event) {
  const createBtn = document.getElementById("createBtn");
  const isValid = validateForm();

  if (!isValid) {
    event.preventDefault();
    return false;
  }

  // Show loading state
  createBtn.disabled = true;
  createBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Creating...';

  // Form will submit normally for server-side processing
  return true;
}

function validateForm() {
  const nameValid = validateName();
  const typeValid = validateType();

  return nameValid && typeValid;
}

// Cancel Handler
function cancelCreate() {
  if (
    confirm(
      "Are you sure you want to cancel? Any unsaved changes will be lost."
    )
  ) {
    window.location.href = "/admin/achievements";
  }
}

// Auto-hide alerts
document.addEventListener("DOMContentLoaded", function () {
  const alerts = document.querySelectorAll(".alert");
  alerts.forEach((alert) => {
    setTimeout(() => {
      alert.style.opacity = "0";
      alert.style.transform = "translateX(100%)";
      setTimeout(() => alert.remove(), 300);
    }, 5000);
  });
});

// Utility Functions
function debounce(func, wait) {
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

// Export functions for global access
window.openIconPicker = openIconPicker;
window.closeIconPicker = closeIconPicker;
window.cancelCreate = cancelCreate;
