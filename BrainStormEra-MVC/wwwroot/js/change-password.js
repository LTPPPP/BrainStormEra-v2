// Change Password page JavaScript functionality
document.addEventListener("DOMContentLoaded", function () {
  const passwordForm = document.querySelector(".password-form");
  const newPasswordField = document.querySelector('input[name="NewPassword"]');
  const confirmPasswordField = document.querySelector(
    'input[name="ConfirmPassword"]'
  );
  const strengthBar = document.querySelector(".strength-fill");
  const strengthText = document.querySelector(".strength-text");

  // Password toggle functionality
  initializePasswordToggles();

  // Password strength checker
  if (newPasswordField) {
    newPasswordField.addEventListener("input", function () {
      checkPasswordStrength(this.value);
      updatePasswordRequirements(this.value);
    });
  }

  // Confirm password validation
  if (confirmPasswordField && newPasswordField) {
    confirmPasswordField.addEventListener("input", function () {
      validatePasswordMatch();
    });

    newPasswordField.addEventListener("input", function () {
      if (confirmPasswordField.value) {
        validatePasswordMatch();
      }
    });
  }

  // Form submission validation
  if (passwordForm) {
    passwordForm.addEventListener("submit", function (e) {
      if (!validatePasswordForm()) {
        e.preventDefault();
      }
    });
  }

  // Real-time validation for all password fields
  const passwordFields = document.querySelectorAll('input[type="password"]');
  passwordFields.forEach((field) => {
    field.addEventListener("blur", function () {
      validatePasswordField(this);
    });
  });
});

function initializePasswordToggles() {
  const toggleButtons = document.querySelectorAll(".password-toggle");

  toggleButtons.forEach((button) => {
    button.addEventListener("click", function (e) {
      e.preventDefault();

      const targetName = this.getAttribute("data-target");
      const passwordField = document.querySelector(
        `input[name="${targetName}"]`
      );
      const icon = this.querySelector("i");

      if (passwordField) {
        if (passwordField.type === "password") {
          passwordField.type = "text";
          icon.className = "fas fa-eye-slash";
        } else {
          passwordField.type = "password";
          icon.className = "fas fa-eye";
        }
      }
    });
  });
}

function checkPasswordStrength(password) {
  const strengthBar = document.querySelector(".strength-fill");
  const strengthText = document.querySelector(".strength-text");

  if (!strengthBar || !strengthText) return;

  let score = 0;
  let feedback = "";

  if (password.length === 0) {
    strengthBar.style.width = "0%";
    strengthText.textContent = "Độ mạnh mật khẩu";
    strengthBar.style.backgroundColor = "#e9ecef";
    return;
  }

  // Length check
  if (password.length >= 8) score += 20;
  else if (password.length >= 6) score += 10;

  // Uppercase check
  if (/[A-Z]/.test(password)) score += 20;

  // Lowercase check
  if (/[a-z]/.test(password)) score += 20;

  // Number check
  if (/[0-9]/.test(password)) score += 20;

  // Special character check
  if (/[^A-Za-z0-9]/.test(password)) score += 20;

  // Set strength level and color
  if (score < 40) {
    feedback = "Yếu";
    strengthBar.style.backgroundColor = "#dc3545";
  } else if (score < 60) {
    feedback = "Trung bình";
    strengthBar.style.backgroundColor = "#ffc107";
  } else if (score < 80) {
    feedback = "Mạnh";
    strengthBar.style.backgroundColor = "#28a745";
  } else {
    feedback = "Rất mạnh";
    strengthBar.style.backgroundColor = "#20c997";
  }

  strengthBar.style.width = score + "%";
  strengthText.textContent = `Độ mạnh: ${feedback}`;
}

function updatePasswordRequirements(password) {
  const requirements = {
    "length-check": password.length >= 6,
    "uppercase-check": /[A-Z]/.test(password),
    "lowercase-check": /[a-z]/.test(password),
    "number-check": /[0-9]/.test(password),
    "special-check": /[^A-Za-z0-9]/.test(password),
  };

  Object.keys(requirements).forEach((id) => {
    const element = document.getElementById(id);
    if (element) {
      const icon = element.querySelector("i");
      if (requirements[id]) {
        icon.className = "fas fa-check text-success";
        element.classList.add("text-success");
        element.classList.remove("text-danger");
      } else {
        icon.className = "fas fa-times text-danger";
        element.classList.add("text-danger");
        element.classList.remove("text-success");
      }
    }
  });
}

function validatePasswordMatch() {
  const newPassword = document.querySelector('input[name="NewPassword"]').value;
  const confirmPassword = document.querySelector(
    'input[name="ConfirmPassword"]'
  ).value;
  const confirmField = document.querySelector('input[name="ConfirmPassword"]');

  if (confirmPassword && newPassword !== confirmPassword) {
    confirmField.classList.add("is-invalid");
    confirmField.classList.remove("is-valid");

    // Add or update error message
    let errorMsg = confirmField.parentNode.querySelector(
      ".password-match-error"
    );
    if (!errorMsg) {
      errorMsg = document.createElement("div");
      errorMsg.className = "invalid-feedback password-match-error";
      confirmField.parentNode.appendChild(errorMsg);
    }
    errorMsg.textContent = "Mật khẩu xác nhận không khớp";

    return false;
  } else if (confirmPassword) {
    confirmField.classList.remove("is-invalid");
    confirmField.classList.add("is-valid");

    // Remove error message
    const errorMsg = confirmField.parentNode.querySelector(
      ".password-match-error"
    );
    if (errorMsg) {
      errorMsg.remove();
    }

    return true;
  }

  return false;
}

function validatePasswordField(field) {
  const value = field.value;
  let isValid = true;

  if (field.hasAttribute("required") && !value) {
    isValid = false;
  }

  if (field.name === "NewPassword" && value) {
    // Check minimum length
    if (value.length < 6) {
      isValid = false;
    }
  }

  if (field.name === "ConfirmPassword" && value) {
    const newPassword = document.querySelector(
      'input[name="NewPassword"]'
    ).value;
    if (value !== newPassword) {
      isValid = false;
    }
  }

  // Update field appearance
  if (isValid && value) {
    field.classList.remove("is-invalid");
    field.classList.add("is-valid");
  } else if (!isValid) {
    field.classList.remove("is-valid");
    field.classList.add("is-invalid");
  }

  return isValid;
}

function validatePasswordForm() {
  const currentPassword = document.querySelector(
    'input[name="CurrentPassword"]'
  ).value;
  const newPassword = document.querySelector('input[name="NewPassword"]').value;
  const confirmPassword = document.querySelector(
    'input[name="ConfirmPassword"]'
  ).value;

  let isValid = true;

  // Check if all fields are filled
  if (!currentPassword || !newPassword || !confirmPassword) {
    showNotification("Vui lòng điền đầy đủ thông tin", "warning");
    isValid = false;
  }

  // Check new password strength
  if (newPassword.length < 6) {
    showNotification("Mật khẩu mới phải có ít nhất 6 ký tự", "warning");
    isValid = false;
  }

  // Check password match
  if (newPassword !== confirmPassword) {
    showNotification("Mật khẩu xác nhận không khớp", "warning");
    isValid = false;
  }

  // Check if new password is different from current
  if (currentPassword === newPassword) {
    showNotification("Mật khẩu mới phải khác với mật khẩu hiện tại", "warning");
    isValid = false;
  }

  return isValid;
}

function showNotification(message, type = "info") {
  // Remove existing notifications
  document
    .querySelectorAll(".password-notification")
    .forEach((n) => n.remove());

  // Create notification element
  const notification = document.createElement("div");
  notification.className = `alert alert-${type} alert-dismissible fade show position-fixed password-notification`;
  notification.style.cssText = `
        top: 20px;
        right: 20px;
        z-index: 9999;
        min-width: 300px;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
        border-radius: 8px;
    `;

  notification.innerHTML = `
        <i class="fas fa-${getIconForType(type)} me-2"></i>
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

function getIconForType(type) {
  const icons = {
    success: "check-circle",
    warning: "exclamation-triangle",
    danger: "times-circle",
    info: "info-circle",
  };
  return icons[type] || "info-circle";
}

// Password strength tips
const passwordTips = [
  "Sử dụng ít nhất 8 ký tự",
  "Kết hợp chữ hoa và chữ thường",
  "Bao gồm số và ký tự đặc biệt",
  "Tránh sử dụng thông tin cá nhân",
  "Không sử dụng từ trong từ điển",
  "Thay đổi mật khẩu định kỳ",
];

// Show random password tip
function showPasswordTip() {
  const randomTip =
    passwordTips[Math.floor(Math.random() * passwordTips.length)];
  showNotification(`💡 Mẹo: ${randomTip}`, "info");
}

// Show tip when page loads
setTimeout(showPasswordTip, 2000);
