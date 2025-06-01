// Enhanced Change Password page JavaScript functionality
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

      // Add typing animation
      if (this.value.length > 0) {
        $(this).addClass("active-input");
      } else {
        $(this).removeClass("active-input");
      }
    });
  }

  // Confirm password validation
  if (confirmPasswordField && newPasswordField) {
    confirmPasswordField.addEventListener("input", function () {
      validatePasswordMatch();

      // Add typing animation
      if (this.value.length > 0) {
        $(this).addClass("active-input");
      } else {
        $(this).removeClass("active-input");
      }
    });

    newPasswordField.addEventListener("input", function () {
      if (confirmPasswordField.value) {
        validatePasswordMatch();
      }
    });
  }

  // Form submission validation with improved feedback
  if (passwordForm) {
    passwordForm.addEventListener("submit", function (e) {
      if (!validatePasswordForm()) {
        e.preventDefault();

        // Highlight the password requirements section
        $(".password-requirements").addClass("pulse-animation");
        setTimeout(function () {
          $(".password-requirements").removeClass("pulse-animation");
        }, 1000);
      } else {
        // Show loading state on button
        const submitBtn = this.querySelector('button[type="submit"]');
        const originalText = submitBtn.innerHTML;
        submitBtn.innerHTML =
          '<i class="fas fa-spinner fa-spin"></i> Updating...';
        submitBtn.disabled = true;

        // You can add additional logic here if needed
      }
    });
  }

  // Focus effect on input fields
  const allInputs = document.querySelectorAll(".form-control");
  allInputs.forEach((input) => {
    input.addEventListener("focus", function () {
      this.parentElement.classList.add("input-focused");
    });

    input.addEventListener("blur", function () {
      this.parentElement.classList.remove("input-focused");
      validatePasswordField(this);
    });
  });

  // Add animations to page elements
  animateElements();
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

          // Add a small animation
          $(this).addClass("toggled");
          setTimeout(() => {
            $(this).removeClass("toggled");
          }, 300);
        } else {
          passwordField.type = "password";
          icon.className = "fas fa-eye";

          // Add a small animation
          $(this).addClass("toggled");
          setTimeout(() => {
            $(this).removeClass("toggled");
          }, 300);
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
    strengthText.textContent = "Password Strength";
    strengthBar.style.backgroundColor = "#e9ecef";
    return;
  }

  // Length check - improved scoring
  if (password.length >= 10) score += 25;
  else if (password.length >= 8) score += 20;
  else if (password.length >= 6) score += 10;

  // Uppercase check
  if (/[A-Z]/.test(password)) score += 20;

  // Lowercase check
  if (/[a-z]/.test(password)) score += 20;

  // Number check
  if (/[0-9]/.test(password)) score += 20;

  // Special character check
  if (/[^A-Za-z0-9]/.test(password)) score += 20;

  // Variety of characters check
  const uniqueChars = new Set(password.split("")).size;
  if (uniqueChars > 6) score += 15;
  else if (uniqueChars > 4) score += 10;
  else if (uniqueChars > 2) score += 5;

  // Set strength level and color with smooth animation
  if (score < 40) {
    feedback = "Weak";
    strengthBar.style.backgroundColor = "#dc3545";
  } else if (score < 60) {
    feedback = "Medium";
    strengthBar.style.backgroundColor = "#ffc107";
  } else if (score < 80) {
    feedback = "Strong";
    strengthBar.style.backgroundColor = "#28a745";
  } else {
    feedback = "Very Strong";
    strengthBar.style.backgroundColor = "#20c997";
  }

  // Smooth animation for strength bar
  strengthBar.style.transition =
    "width 0.5s ease-in-out, background-color 0.5s";
  strengthBar.style.width = Math.min(100, score) + "%";
  strengthText.textContent = `Strength: ${feedback}`;

  // Add animation to strength text
  $(strengthText).addClass("strength-updated");
  setTimeout(() => {
    $(strengthText).removeClass("strength-updated");
  }, 500);
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

      // Check if state is changing
      const wasSuccessful = icon.classList.contains("fa-check");
      const isSuccessful = requirements[id];

      if (isSuccessful) {
        icon.className = "fas fa-check text-success";
        if (!wasSuccessful) {
          // Add animation for newly satisfied requirement
          $(element).addClass("requirement-satisfied");
          setTimeout(() => {
            $(element).removeClass("requirement-satisfied");
          }, 800);
        }
      } else {
        icon.className = "fas fa-times text-danger";
      }
    }
  });

  // Check if all requirements are met
  const allMet = Object.values(requirements).every(Boolean);
  if (allMet && password.length > 0) {
    $(".password-requirements").addClass("all-requirements-met");
  } else {
    $(".password-requirements").removeClass("all-requirements-met");
  }
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
    errorMsg.textContent = "Passwords don't match";

    return false;
  } else if (confirmPassword) {
    confirmField.classList.remove("is-invalid");
    confirmField.classList.add("is-valid");

    // Add a small success animation
    $(confirmField).addClass("match-success");
    setTimeout(() => {
      $(confirmField).removeClass("match-success");
    }, 800);

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
    // Enhanced validation with improved feedback
    const lengthValid = value.length >= 6;
    const uppercaseValid = /[A-Z]/.test(value);
    const lowercaseValid = /[a-z]/.test(value);
    const numberValid = /[0-9]/.test(value);
    const specialValid = /[^A-Za-z0-9]/.test(value);

    isValid =
      lengthValid &&
      uppercaseValid &&
      lowercaseValid &&
      numberValid &&
      specialValid;
  }

  if (field.name === "ConfirmPassword" && value) {
    const newPassword = document.querySelector(
      'input[name="NewPassword"]'
    ).value;
    if (value !== newPassword) {
      isValid = false;
    }
  }

  // Update field appearance with smooth transition
  field.style.transition = "border-color 0.3s, box-shadow 0.3s";

  if (isValid && value) {
    field.classList.remove("is-invalid");
    field.classList.add("is-valid");
  } else if (!isValid && value) {
    field.classList.remove("is-valid");
    field.classList.add("is-invalid");
  } else {
    field.classList.remove("is-valid", "is-invalid");
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
    showNotification("Please fill in all required fields", "warning");
    isValid = false;
  }

  // Check new password strength
  if (newPassword.length < 6) {
    showNotification(
      "Your new password must be at least 6 characters long",
      "warning"
    );
    isValid = false;
  }

  // Check password match
  if (newPassword !== confirmPassword) {
    showNotification("Passwords don't match", "warning");
    isValid = false;
  }

  // Check if new password is different from current
  if (currentPassword === newPassword) {
    showNotification(
      "New password must be different from your current password",
      "warning"
    );
    isValid = false;
  }

  // Check password requirements
  const lengthValid = newPassword.length >= 6;
  const uppercaseValid = /[A-Z]/.test(newPassword);
  const lowercaseValid = /[a-z]/.test(newPassword);
  const numberValid = /[0-9]/.test(newPassword);
  const specialValid = /[^A-Za-z0-9]/.test(newPassword);

  const allRequirementsMet =
    lengthValid &&
    uppercaseValid &&
    lowercaseValid &&
    numberValid &&
    specialValid;

  if (!allRequirementsMet) {
    showNotification(
      "Your password doesn't meet all the requirements",
      "warning"
    );
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
  notification.className = `alert alert-${type} alert-dismissible fade show position-fixed password-notification animate__animated animate__fadeInRight`;
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
      notification.classList.remove("animate__fadeInRight");
      notification.classList.add("animate__fadeOutRight");
      setTimeout(() => {
        if (notification.parentNode) {
          notification.remove();
        }
      }, 500);
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
  "Use at least 8 characters",
  "Mix uppercase and lowercase letters",
  "Include numbers and special characters",
  "Avoid using personal information",
  "Don't use dictionary words",
  "Change your password regularly",
  "Use different passwords for different accounts",
  "Consider using a password manager",
];

// Show random password tip
function showPasswordTip() {
  const randomTip =
    passwordTips[Math.floor(Math.random() * passwordTips.length)];
  showNotification(`💡 Tip: ${randomTip}`, "info");
}

function animateElements() {
  // Add additional animations to elements
  setTimeout(() => {
    $(".password-requirements").addClass("animate__animated animate__fadeIn");
  }, 500);
}

// Show tip when page loads
setTimeout(showPasswordTip, 2000);

// Add custom CSS animations
const style = document.createElement("style");
style.textContent = `
  .pulse-animation {
    animation: pulse 0.8s;
  }
  
  @keyframes pulse {
    0% {
      box-shadow: 0 0 0 0 rgba(220, 53, 69, 0.4);
    }
    70% {
      box-shadow: 0 0 0 10px rgba(220, 53, 69, 0);
    }
    100% {
      box-shadow: 0 0 0 0 rgba(220, 53, 69, 0);
    }
  }
  
  .input-focused {
    transition: all 0.3s ease;
  }
  
  .input-focused .form-control {
    border-color: #3498db;
    box-shadow: 0 0 0 3px rgba(52, 152, 219, 0.1);
  }
  
  .toggled {
    transform: scale(1.2);
    transition: transform 0.3s;
  }
  
  .active-input {
    border-color: #3498db !important;
  }
  
  .strength-updated {
    animation: fadeInOut 0.5s;
  }
  
  @keyframes fadeInOut {
    0% { opacity: 0.7; }
    50% { opacity: 1; }
    100% { opacity: 0.7; }
  }
  
  .requirement-satisfied {
    animation: satisfied 0.8s;
  }
  
  @keyframes satisfied {
    0% { transform: scale(1); }
    50% { transform: scale(1.1); }
    100% { transform: scale(1); }
  }
  
  .match-success {
    border-color: #28a745 !important;
    box-shadow: 0 0 0 3px rgba(40, 167, 69, 0.1) !important;
    transition: all 0.3s;
  }
  
  .all-requirements-met {
    border-left: 4px solid #28a745;
    transition: border-left 0.5s ease;
  }
`;
document.head.appendChild(style);
