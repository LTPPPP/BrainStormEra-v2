/**
 * Achievement Management JavaScript
 * Handles CRUD operations for achievements in admin panel
 */

// Global variables
let achievementToDelete = null;
let currentEditingAchievement = null;

// DOM Ready
document.addEventListener("DOMContentLoaded", function () {
  initializeAchievementManagement();
});

// Initialize functions
function initializeAchievementManagement() {
  setupIconPreviews();
  setupModalEventListeners();
  setupFormValidation();
  setupFilterAutoSubmit();
}

// Icon Preview Functions
function setupIconPreviews() {
  updateIconPreview("createIcon", "createIconPreview");
  updateIconPreview("editIcon", "editIconPreview");
}

function updateIconPreview(inputId, previewId) {
  const iconInput = document.getElementById(inputId);
  const iconPreview = document.getElementById(previewId);
  const imagePreview = document.getElementById(
    inputId.replace("Icon", "IconImage")
  );

  if (!iconInput || !iconPreview) return;

  iconInput.addEventListener("input", function () {
    const iconValue = this.value || "fas fa-trophy";

    // Check if it's a URL/path to an image
    if (iconValue.startsWith("/") || iconValue.startsWith("http")) {
      iconPreview.style.display = "none";
      if (imagePreview) {
        imagePreview.src = iconValue;
        imagePreview.style.display = "inline";
      }
    } else {
      // It's a FontAwesome class
      iconPreview.className = iconValue;
      iconPreview.style.display = "inline";
      if (imagePreview) {
        imagePreview.style.display = "none";
      }
    }
  });

  // Initial update
  const iconValue = iconInput.value || "fas fa-trophy";
  if (iconValue.startsWith("/") || iconValue.startsWith("http")) {
    iconPreview.style.display = "none";
    if (imagePreview) {
      imagePreview.src = iconValue;
      imagePreview.style.display = "inline";
    }
  } else {
    iconPreview.className = iconValue;
    iconPreview.style.display = "inline";
    if (imagePreview) {
      imagePreview.style.display = "none";
    }
  }
}

// Icon Upload Functions
function triggerIconUpload(type) {
  const fileInput = document.getElementById(type + "IconFile");
  if (fileInput) {
    fileInput.click();
  }
}

async function handleIconUpload(fileInput, type) {
  const file = fileInput.files[0];
  if (!file) return;

  // Validate file type
  const allowedTypes = [
    "image/jpeg",
    "image/jpg",
    "image/png",
    "image/gif",
    "image/webp",
    "image/svg+xml",
  ];
  if (!allowedTypes.includes(file.type)) {
    showError("Please select a valid image file (JPG, PNG, GIF, WEBP, SVG)");
    fileInput.value = "";
    return;
  }

  // Validate file size (2MB)
  if (file.size > 2 * 1024 * 1024) {
    showError("File size must be less than 2MB");
    fileInput.value = "";
    return;
  }

  // Store file for later upload and show preview
  const iconInput = document.getElementById(type + "Icon");
  const iconPreview = document.getElementById(type + "IconPreview");
  const imagePreview = document.getElementById(type + "IconImage");

  // Mark as file upload instead of FontAwesome
  iconInput.value = `UPLOAD:${file.name}`;
  iconInput.setAttribute("data-file-selected", "true");

  // Show preview using FileReader
  const reader = new FileReader();
  reader.onload = function (e) {
    // Update preview
    if (iconPreview) iconPreview.style.display = "none";
    if (imagePreview) {
      imagePreview.src = e.target.result;
      imagePreview.style.display = "inline";
    }
  };
  reader.readAsDataURL(file);

  showSuccess(
    "Icon selected. It will be uploaded when you save the achievement."
  );
}

// Modal Event Listeners
function setupModalEventListeners() {
  // Close modals when clicking outside
  window.onclick = function (event) {
    const createModal = document.getElementById("createModal");
    const editModal = document.getElementById("editModal");
    const deleteModal = document.getElementById("deleteModal");

    if (event.target === createModal) {
      closeCreateModal();
    } else if (event.target === editModal) {
      closeEditModal();
    } else if (event.target === deleteModal) {
      closeDeleteModal();
    }
  };

  // ESC key to close modals
  document.addEventListener("keydown", function (event) {
    if (event.key === "Escape") {
      closeAllModals();
    }
  });
}

function closeAllModals() {
  closeCreateModal();
  closeEditModal();
  closeDeleteModal();
}

// Form Validation
function setupFormValidation() {
  const createForm = document.getElementById("createAchievementForm");
  const editForm = document.getElementById("editAchievementForm");

  if (createForm) {
    createForm.addEventListener("submit", function (e) {
      e.preventDefault();
      if (validateAchievementForm("create")) {
        createAchievement();
      }
    });
  }
}

function validateAchievementForm(type) {
  const prefix = type === "create" ? "create" : "edit";
  const name = document.getElementById(prefix + "Name").value.trim();
  const achievementType = document.getElementById(prefix + "Type").value;

  if (!name) {
    showError("Achievement name is required");
    return false;
  }

  if (name.length > 100) {
    showError("Achievement name must be 100 characters or less");
    return false;
  }

  if (!achievementType) {
    showError("Achievement type is required");
    return false;
  }

  return true;
}

// Create Modal Functions
function openCreateModal() {
  document.getElementById("createModal").style.display = "block";
  resetCreateForm();
  document.body.style.overflow = "hidden"; // Prevent background scrolling
}

function closeCreateModal() {
  document.getElementById("createModal").style.display = "none";
  resetCreateForm();
  document.body.style.overflow = "auto";
}

function resetCreateForm() {
  const form = document.getElementById("createAchievementForm");
  if (form) {
    form.reset();
    document.getElementById("createIcon").value = "fas fa-trophy";
    updateIconPreview("createIcon", "createIconPreview");

    // Reset file inputs and image previews
    const createIconFile = document.getElementById("createIconFile");
    const createIconImage = document.getElementById("createIconImage");
    if (createIconFile) createIconFile.value = "";
    if (createIconImage) createIconImage.style.display = "none";
  }
}

// Edit Achievement Functions
function editAchievement(achievementId) {
  if (!achievementId) {
    showError("Achievement ID is required");
    return;
  }

  // Navigate to the dedicated edit page
  window.location.href = `/admin/achievements/edit/${achievementId}`;
}

// CRUD Functions
async function createAchievement() {
  if (!validateAchievementForm("create")) return;

  const form = document.getElementById("createAchievementForm");
  const formData = new FormData(form);

  // Check if user selected a file for upload
  const iconInput = document.getElementById("createIcon");
  const fileInput = document.getElementById("createIconFile");
  const hasFileSelected =
    iconInput.getAttribute("data-file-selected") === "true" &&
    fileInput.files.length > 0;

  let iconValue = formData.get("AchievementIcon");

  // If file upload, handle it separately
  if (hasFileSelected) {
    try {
      // First create achievement with temporary icon
      const tempAchievement = {
        AchievementName: formData.get("AchievementName"),
        AchievementDescription: formData.get("AchievementDescription"),
        AchievementType: formData.get("AchievementType"),
        AchievementIcon: "fas fa-trophy", // Temporary icon
      };

      showLoading("Creating achievement...");

      // Create achievement first
      const createResponse = await fetch(
        "/admin/achievements?handler=CreateAchievement",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            RequestVerificationToken: getRequestVerificationToken(),
          },
          body: JSON.stringify(tempAchievement),
        }
      );

      if (!createResponse.ok) {
        const errorText = await createResponse.text();
        showError(
          `Server error: ${createResponse.status} ${createResponse.statusText}`
        );
        hideLoading();
        return;
      }

      const createResult = await createResponse.json();

      if (!createResult.success) {
        showError(
          "Error: " + (createResult.message || "Failed to create achievement")
        );
        hideLoading();
        return;
      }

      // Now upload the icon if achievement was created successfully
      showLoading("Uploading icon...");

      const uploadFormData = new FormData();
      uploadFormData.append("iconFile", fileInput.files[0]);
      uploadFormData.append(
        "achievementId",
        createResult.achievementId || "temp"
      ); // Assuming server returns ID

      const uploadResponse = await fetch(
        "/admin/achievements?handler=UploadAchievementIcon",
        {
          method: "POST",
          headers: {
            RequestVerificationToken: getRequestVerificationToken(),
          },
          body: uploadFormData,
        }
      );

      hideLoading();

      if (uploadResponse.ok) {
        const uploadResult = await uploadResponse.json();
        if (uploadResult.success) {
          showSuccess("Achievement created with custom icon successfully");
        } else {
          showSuccess(
            "Achievement created, but icon upload failed: " +
              uploadResult.message
          );
        }
      } else {
        showSuccess("Achievement created, but icon upload failed");
      }

      closeCreateModal();
      setTimeout(() => {
        location.reload();
      }, 1000);
    } catch (error) {
      hideLoading();
      showError("An error occurred while creating the achievement");
    }
  } else {
    // Regular creation without file upload
    const achievement = {
      AchievementName: formData.get("AchievementName"),
      AchievementDescription: formData.get("AchievementDescription"),
      AchievementType: formData.get("AchievementType"),
      AchievementIcon: iconValue,
    };

    showLoading("Creating achievement...");

    try {
      const response = await fetch(
        "/admin/achievements?handler=CreateAchievement",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            RequestVerificationToken: getRequestVerificationToken(),
          },
          body: JSON.stringify(achievement),
        }
      );

      hideLoading();

      if (!response.ok) {
        const errorText = await response.text();
        showError(`Server error: ${response.status} ${response.statusText}`);
        return;
      }

      const result = await response.json();

      if (result.success) {
        showSuccess("Achievement created successfully");
        closeCreateModal();
        setTimeout(() => {
          location.reload();
        }, 1000);
      } else {
        showError(
          "Error: " + (result.message || "Failed to create achievement")
        );
      }
    } catch (error) {
      hideLoading();
      showError("An error occurred while creating the achievement");
    }
  }
}

async function updateAchievement() {
  // This function has been moved to the dedicated edit page
  // Redirecting to edit page for better UX
}

function viewAchievement(achievementId) {
  // For now, just open the edit modal in view mode
  // You can enhance this to create a separate view modal
  editAchievement(achievementId);
}

// Delete Functions
function confirmDelete(achievementId, achievementName) {
  if (!achievementId || !achievementName) {
    showError("Invalid achievement data");
    return;
  }

  achievementToDelete = achievementId;

  // Enhanced message with better formatting and warning
  const deleteMessage = document.getElementById("deleteMessage");
  deleteMessage.innerHTML = `
    Are you sure you want to delete the achievement "<strong>${escapeHtml(
      achievementName
    )}</strong>"?
    <small class="text-muted">
      <i class="fas fa-exclamation-triangle"></i>
      This action cannot be undone and will permanently remove this achievement from the system. 
      All users who have earned this achievement will lose it from their profiles.
    </small>
  `;

  // Show modal with enhanced animation
  const modal = document.getElementById("deleteModal");
  modal.style.display = "flex";
  modal.classList.add("show");
  document.body.style.overflow = "hidden";

  // Focus management for accessibility
  const confirmButton = modal.querySelector(".btn-confirm");
  if (confirmButton) {
    setTimeout(() => confirmButton.focus(), 100);
  }

  // Add escape key listener
  document.addEventListener("keydown", handleModalEscape);
}

function closeDeleteModal() {
  achievementToDelete = null;
  const modal = document.getElementById("deleteModal");
  modal.classList.remove("show");

  // Add fade out animation
  setTimeout(() => {
    modal.style.display = "none";
    document.body.style.overflow = "auto";
  }, 300);

  // Remove escape key listener
  document.removeEventListener("keydown", handleModalEscape);
}

function handleModalEscape(event) {
  if (event.key === "Escape") {
    closeDeleteModal();
  }
}

async function deleteAchievement() {
  if (!achievementToDelete) {
    showError("No achievement selected for deletion");
    return;
  }

  // Show loading state on confirm button
  const confirmButton = document.querySelector("#deleteModal .btn-confirm");
  const originalText = confirmButton.innerHTML;
  confirmButton.classList.add("loading");
  confirmButton.disabled = true;
  confirmButton.innerHTML =
    '<i class="fas fa-spinner fa-spin"></i> Deleting...';

  try {
    const response = await fetch(
      "/admin/achievements?handler=DeleteAchievement",
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken: getRequestVerificationToken(),
        },
        body: JSON.stringify({ AchievementId: achievementToDelete }),
      }
    );

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(
        `Server error: ${response.status} ${response.statusText}`
      );
    }

    const result = await response.json();

    if (result.success) {
      // Show success state
      confirmButton.innerHTML = '<i class="fas fa-check"></i> Deleted!';
      confirmButton.style.background =
        "linear-gradient(135deg, #27ae60 0%, #2ecc71 100%)";

      showSuccess("Achievement deleted successfully");

      // Close modal and reload after short delay
      setTimeout(() => {
        closeDeleteModal();
        setTimeout(() => {
          location.reload();
        }, 500);
      }, 1000);
    } else {
      throw new Error(result.message || "Failed to delete achievement");
    }
  } catch (error) {
    showError(
      error.message || "Failed to delete achievement. Please try again."
    );

    // Reset button state
    confirmButton.classList.remove("loading");
    confirmButton.disabled = false;
    confirmButton.innerHTML = originalText;
    confirmButton.style.background = "";
  }
}

// Utility Functions
function getRequestVerificationToken() {
  const token = document.querySelector(
    'input[name="__RequestVerificationToken"]'
  )?.value;
  return token || "";
}

function escapeHtml(text) {
  const div = document.createElement("div");
  div.textContent = text;
  return div.innerHTML;
}

function showLoading(message = "Loading...") {
  // Create or update loading overlay
  let overlay = document.getElementById("loadingOverlay");
  if (!overlay) {
    overlay = document.createElement("div");
    overlay.id = "loadingOverlay";
    overlay.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: rgba(0,0,0,0.5);
            display: flex;
            justify-content: center;
            align-items: center;
            z-index: 9999;
            color: white;
            font-size: 18px;
        `;
    document.body.appendChild(overlay);
  }
  overlay.innerHTML = `
        <div style="text-align: center;">
            <div style="margin-bottom: 10px;">
                <i class="fas fa-spinner fa-spin fa-2x"></i>
            </div>
            <div>${message}</div>
        </div>
    `;
  overlay.style.display = "flex";
}

function hideLoading() {
  const overlay = document.getElementById("loadingOverlay");
  if (overlay) {
    overlay.style.display = "none";
  }
}

function showError(message) {
  showNotification(message, "error");
}

function showSuccess(message) {
  showNotification(message, "success");
}

function showNotification(message, type = "info") {
  // Create notification element
  const notification = document.createElement("div");
  notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        padding: 15px 20px;
        border-radius: 5px;
        color: white;
        z-index: 10000;
        max-width: 400px;
        word-wrap: break-word;
        ${type === "error" ? "background: #dc3545;" : ""}
        ${type === "success" ? "background: #28a745;" : ""}
        ${type === "info" ? "background: #17a2b8;" : ""}
    `;

  notification.innerHTML = `
        <div style="display: flex; align-items: center; gap: 10px;">
            <i class="fas ${
              type === "error"
                ? "fa-exclamation-circle"
                : type === "success"
                ? "fa-check-circle"
                : "fa-info-circle"
            }"></i>
            <span>${message}</span>
            <button onclick="this.parentElement.parentElement.remove()" style="background: none; border: none; color: white; cursor: pointer; margin-left: auto;">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `;

  document.body.appendChild(notification);

  // Auto remove after 5 seconds
  setTimeout(() => {
    if (notification.parentElement) {
      notification.remove();
    }
  }, 5000);
}

// Filter and Search Functions
// Filter Functions
function setupFilterAutoSubmit() {
  const typeFilter = document.getElementById("typeFilter");
  const sortBy = document.getElementById("sortBy");
  const searchQuery = document.getElementById("searchQuery");

  // Auto-submit on dropdown changes
  if (typeFilter) {
    typeFilter.addEventListener("change", function () {
      this.form.submit();
    });
  }

  if (sortBy) {
    sortBy.addEventListener("change", function () {
      this.form.submit();
    });
  }

  // Search with debounce
  if (searchQuery) {
    let searchTimeout;
    searchQuery.addEventListener("input", function () {
      clearTimeout(searchTimeout);
      searchTimeout = setTimeout(() => {
        this.form.submit();
      }, 500); // 500ms delay
    });

    // Also submit on Enter key
    searchQuery.addEventListener("keypress", function (e) {
      if (e.key === "Enter") {
        clearTimeout(searchTimeout);
        this.form.submit();
      }
    });
  }
}

function clearFilters() {
  window.location.href = "/admin/achievements";
}

function exportAchievements() {
  // Placeholder for export functionality
  showNotification("Export functionality will be implemented soon", "info");
}

// Keyboard shortcuts
document.addEventListener("keydown", function (event) {
  // Ctrl/Cmd + N = New Achievement
  if ((event.ctrlKey || event.metaKey) && event.key === "n") {
    event.preventDefault();
    openCreateModal();
  }

  // ESC = Close modals
  if (event.key === "Escape") {
    closeAllModals();
  }
});

// Global function exports for inline onclick handlers
window.openCreateModal = openCreateModal;
window.closeCreateModal = closeCreateModal;
window.editAchievement = editAchievement;
window.closeEditModal = closeEditModal;
window.viewAchievement = viewAchievement;
window.confirmDelete = confirmDelete;
window.closeDeleteModal = closeDeleteModal;
window.deleteAchievement = deleteAchievement;
window.handleModalEscape = handleModalEscape;
window.createAchievement = createAchievement;
window.updateAchievement = updateAchievement;
