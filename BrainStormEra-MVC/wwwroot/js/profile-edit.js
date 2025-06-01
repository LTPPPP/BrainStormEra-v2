// Profile Edit page JavaScript functionality
document.addEventListener("DOMContentLoaded", function () {
  // Profile image preview functionality
  const profileImageInput = document.getElementById("profileImageInput");
  const profileImagePreview = document.getElementById("profileImagePreview");
  const imageUploadOverlay = document.querySelector(".image-upload-overlay");

  if (profileImageInput && profileImagePreview) {
    // Click overlay to trigger file input
    if (imageUploadOverlay) {
      imageUploadOverlay.addEventListener("click", function () {
        profileImageInput.click();
      });
    }

    // Handle file selection
    profileImageInput.addEventListener("change", function (e) {
      const file = e.target.files[0];

      if (file) {
        // Validate file type
        const validTypes = [
          "image/jpeg",
          "image/jpg",
          "image/png",
          "image/gif",
        ];
        if (!validTypes.includes(file.type)) {
          showNotification(
            "Vui lòng chọn file ảnh hợp lệ (JPG, PNG, GIF)",
            "warning"
          );
          this.value = "";
          return;
        }

        // Validate file size (5MB)
        const maxSize = 5 * 1024 * 1024; // 5MB in bytes
        if (file.size > maxSize) {
          showNotification(
            "Kích thước file không được vượt quá 5MB",
            "warning"
          );
          this.value = "";
          return;
        }

        // Preview the image
        const reader = new FileReader();
        reader.onload = function (e) {
          if (profileImagePreview.tagName === "IMG") {
            profileImagePreview.src = e.target.result;
          } else {
            // Replace placeholder with actual image
            const img = document.createElement("img");
            img.id = "profileImagePreview";
            img.className = "profile-image-preview";
            img.src = e.target.result;
            img.alt = "Profile Image";
            profileImagePreview.parentNode.replaceChild(
              img,
              profileImagePreview
            );
          }

          showNotification("Ảnh đã được chọn thành công", "success");
        };
        reader.readAsDataURL(file);
      }
    });
  }

  // Form validation enhancement
  const editForm = document.querySelector(".profile-edit-form");
  if (editForm) {
    editForm.addEventListener("submit", function (e) {
      let isValid = true;
      const requiredFields = this.querySelectorAll(
        "input[required], select[required], textarea[required]"
      );

      requiredFields.forEach((field) => {
        if (!field.value.trim()) {
          isValid = false;
          field.classList.add("is-invalid");

          // Remove invalid class on input
          field.addEventListener(
            "input",
            function () {
              this.classList.remove("is-invalid");
            },
            { once: true }
          );
        } else {
          field.classList.remove("is-invalid");
          field.classList.add("is-valid");
        }
      });

      // Email validation
      const emailField = this.querySelector('input[type="email"]');
      if (emailField && emailField.value) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(emailField.value)) {
          isValid = false;
          emailField.classList.add("is-invalid");
          showNotification("Định dạng email không hợp lệ", "warning");
        }
      }

      // Phone validation
      const phoneField = this.querySelector('input[name="PhoneNumber"]');
      if (phoneField && phoneField.value) {
        const phoneRegex = /^[0-9+\-\s()]{10,15}$/;
        if (!phoneRegex.test(phoneField.value.replace(/\s/g, ""))) {
          isValid = false;
          phoneField.classList.add("is-invalid");
          showNotification("Số điện thoại không hợp lệ", "warning");
        }
      }

      // Date of birth validation
      const dobField = this.querySelector('input[type="date"]');
      if (dobField && dobField.value) {
        const selectedDate = new Date(dobField.value);
        const today = new Date();
        const minDate = new Date(
          today.getFullYear() - 100,
          today.getMonth(),
          today.getDate()
        );

        if (selectedDate > today) {
          isValid = false;
          dobField.classList.add("is-invalid");
          showNotification(
            "Ngày sinh không thể là ngày trong tương lai",
            "warning"
          );
        } else if (selectedDate < minDate) {
          isValid = false;
          dobField.classList.add("is-invalid");
          showNotification("Ngày sinh không hợp lệ", "warning");
        }
      }

      if (!isValid) {
        e.preventDefault();
        showNotification("Vui lòng kiểm tra lại thông tin đã nhập", "danger");
      }
    });
  }

  // Real-time validation for form fields
  const formFields = document.querySelectorAll(
    ".profile-edit-form input, .profile-edit-form select, .profile-edit-form textarea"
  );
  formFields.forEach((field) => {
    field.addEventListener("blur", function () {
      validateField(this);
    });

    field.addEventListener("input", function () {
      // Remove validation classes while typing
      this.classList.remove("is-invalid", "is-valid");
    });
  });

  // Auto-save functionality (optional)
  let autoSaveTimeout;
  const autoSaveFields = document.querySelectorAll(
    '.profile-edit-form input:not([type="file"]), .profile-edit-form select, .profile-edit-form textarea'
  );

  autoSaveFields.forEach((field) => {
    field.addEventListener("input", function () {
      clearTimeout(autoSaveTimeout);
      autoSaveTimeout = setTimeout(() => {
        saveToLocalStorage();
      }, 2000); // Save after 2 seconds of inactivity
    });
  });

  // Load saved data on page load
  loadFromLocalStorage();

  // Clear saved data on successful form submission
  if (editForm) {
    editForm.addEventListener("submit", function () {
      localStorage.removeItem("profile_edit_data");
    });
  }

  // Add character counters for text fields
  addCharacterCounters();

  // Initialize date picker with proper constraints
  initializeDatePicker();
});

function validateField(field) {
  const value = field.value.trim();
  let isValid = true;

  // Required field validation
  if (field.hasAttribute("required") && !value) {
    isValid = false;
    field.classList.add("is-invalid");
    return;
  }

  // Specific field validations
  switch (field.type) {
    case "email":
      if (value && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) {
        isValid = false;
      }
      break;
    case "tel":
      if (value && !/^[0-9+\-\s()]{10,15}$/.test(value.replace(/\s/g, ""))) {
        isValid = false;
      }
      break;
    case "date":
      if (value) {
        const selectedDate = new Date(value);
        const today = new Date();
        const minDate = new Date(
          today.getFullYear() - 100,
          today.getMonth(),
          today.getDate()
        );

        if (selectedDate > today || selectedDate < minDate) {
          isValid = false;
        }
      }
      break;
  }

  // Update field appearance
  if (isValid && value) {
    field.classList.remove("is-invalid");
    field.classList.add("is-valid");
  } else if (!isValid) {
    field.classList.remove("is-valid");
    field.classList.add("is-invalid");
  } else {
    field.classList.remove("is-invalid", "is-valid");
  }
}

function saveToLocalStorage() {
  const formData = {};
  const form = document.querySelector(".profile-edit-form");

  if (form) {
    const formElements = form.querySelectorAll(
      'input:not([type="file"]), select, textarea'
    );
    formElements.forEach((element) => {
      if (element.name) {
        formData[element.name] = element.value;
      }
    });

    localStorage.setItem("profile_edit_data", JSON.stringify(formData));
    showNotification("Thông tin đã được lưu tạm thời", "info");
  }
}

function loadFromLocalStorage() {
  const savedData = localStorage.getItem("profile_edit_data");

  if (savedData) {
    try {
      const formData = JSON.parse(savedData);
      const form = document.querySelector(".profile-edit-form");

      if (form) {
        Object.keys(formData).forEach((name) => {
          const field = form.querySelector(`[name="${name}"]`);
          if (field && field.type !== "file") {
            field.value = formData[name];
          }
        });

        showNotification("Đã khôi phục dữ liệu đã lưu", "info");
      }
    } catch (e) {
      localStorage.removeItem("profile_edit_data");
    }
  }
}

function addCharacterCounters() {
  const textFields = document.querySelectorAll(
    "input[maxlength], textarea[maxlength]"
  );

  textFields.forEach((field) => {
    const maxLength = field.getAttribute("maxlength");
    if (maxLength) {
      const counter = document.createElement("small");
      counter.className = "form-text text-muted character-counter";
      counter.innerHTML = `<span class="current">0</span>/${maxLength} ký tự`;

      field.parentNode.appendChild(counter);

      field.addEventListener("input", function () {
        const current = this.value.length;
        const currentSpan = counter.querySelector(".current");
        currentSpan.textContent = current;

        if (current > maxLength * 0.9) {
          counter.classList.add("text-warning");
        } else {
          counter.classList.remove("text-warning");
        }

        if (current === maxLength) {
          counter.classList.add("text-danger");
        } else {
          counter.classList.remove("text-danger");
        }
      });

      // Initial count
      field.dispatchEvent(new Event("input"));
    }
  });
}

function initializeDatePicker() {
  const dateField = document.querySelector('input[type="date"]');
  if (dateField) {
    const today = new Date();
    const maxDate = today.toISOString().split("T")[0];
    const minDate = new Date(
      today.getFullYear() - 100,
      today.getMonth(),
      today.getDate()
    )
      .toISOString()
      .split("T")[0];

    dateField.setAttribute("max", maxDate);
    dateField.setAttribute("min", minDate);
  }
}

function showNotification(message, type = "info") {
  // Remove existing notifications
  document.querySelectorAll(".profile-notification").forEach((n) => n.remove());

  // Create notification element
  const notification = document.createElement("div");
  notification.className = `alert alert-${type} alert-dismissible fade show position-fixed profile-notification`;
  notification.style.cssText = `
        top: 20px;
        right: 20px;
        z-index: 9999;
        min-width: 300px;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    `;

  notification.innerHTML = `
        ${message}
        <button type="button" class="btn-close" onclick="this.parentElement.remove()"></button>
    `;

  document.body.appendChild(notification);

  // Auto-remove after 4 seconds
  setTimeout(() => {
    if (notification.parentNode) {
      notification.remove();
    }
  }, 4000);
}
