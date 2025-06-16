// Admin Profile JavaScript
class AdminProfile {
    constructor() {
        this.init();
    }

    init() {
        this.initDropdown();
        this.initFormValidation();
        this.initAvatarUpload();
        this.initPasswordStrength();
        this.initNotifications();
    }

    // Dropdown functionality
    initDropdown() {
        const dropdown = document.querySelector('.admin-info.dropdown');
        const dropdownToggle = document.querySelector('#adminProfileDropdown');
        const dropdownMenu = document.querySelector('.admin-dropdown-menu');

        if (!dropdown || !dropdownToggle || !dropdownMenu) return;

        dropdownToggle.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            
            const isOpen = dropdown.classList.contains('show');
            
            document.querySelectorAll('.dropdown.show').forEach(d => {
                if (d !== dropdown) {
                    d.classList.remove('show');
                }
            });
            
            dropdown.classList.toggle('show', !isOpen);
        });

        document.addEventListener('click', (e) => {
            if (!dropdown.contains(e.target)) {
                dropdown.classList.remove('show');
            }
        });

        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                dropdown.classList.remove('show');
            }
        });

        dropdownMenu.addEventListener('click', (e) => {
            if (e.target.classList.contains('dropdown-item')) {
                dropdown.classList.remove('show');
            }
        });
    }

    // Form validation
    initFormValidation() {
        const forms = document.querySelectorAll('form');
        
        forms.forEach(form => {
            form.addEventListener('submit', (e) => {
                if (!this.validateForm(form)) {
                    e.preventDefault();
                    return false;
                }
                
                this.handleFormSubmit(form, e);
            });

            // Real-time validation
            const inputs = form.querySelectorAll('input, textarea');
            inputs.forEach(input => {
                input.addEventListener('blur', () => {
                    this.validateField(input);
                });

                input.addEventListener('input', () => {
                    this.clearFieldError(input);
                });
            });
        });
    }

    validateForm(form) {
        let isValid = true;
        const inputs = form.querySelectorAll('input[required], textarea[required]');
        
        inputs.forEach(input => {
            if (!this.validateField(input)) {
                isValid = false;
            }
        });

        // Special validation for password forms
        if (form.classList.contains('security-form')) {
            isValid = this.validatePasswordForm(form) && isValid;
        }

        return isValid;
    }

    validateField(input) {
        const value = input.value.trim();
        let isValid = true;
        let errorMessage = '';

        // Required field validation
        if (input.hasAttribute('required') && !value) {
            errorMessage = 'This field is required';
            isValid = false;
        }

        // Email validation
        if (input.type === 'email' && value) {
            const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            if (!emailPattern.test(value)) {
                errorMessage = 'Please enter a valid email address';
                isValid = false;
            }
        }

        // Phone validation
        if (input.type === 'tel' && value) {
            const phonePattern = /^[\+]?[\d\s\-\(\)]{10,}$/;
            if (!phonePattern.test(value)) {
                errorMessage = 'Please enter a valid phone number';
                isValid = false;
            }
        }

        // Username validation
        if (input.name === 'Username' && value) {
            const usernamePattern = /^[a-zA-Z0-9_]{3,20}$/;
            if (!usernamePattern.test(value)) {
                errorMessage = 'Username must be 3-20 characters and contain only letters, numbers, and underscores';
                isValid = false;
            }
        }

        this.showFieldError(input, errorMessage);
        return isValid;
    }

    validatePasswordForm(form) {
        const newPassword = form.querySelector('#newPassword');
        const confirmPassword = form.querySelector('#confirmPassword');
        let isValid = true;

        if (newPassword && confirmPassword) {
            // Password strength validation
            if (newPassword.value.length < 8) {
                this.showFieldError(newPassword, 'Password must be at least 8 characters long');
                isValid = false;
            }

            // Password match validation
            if (newPassword.value !== confirmPassword.value) {
                this.showFieldError(confirmPassword, 'Passwords do not match');
                isValid = false;
            }
        }

        return isValid;
    }

    showFieldError(input, message) {
        this.clearFieldError(input);
        
        if (message) {
            input.classList.add('error');
            const errorElement = document.createElement('div');
            errorElement.className = 'field-error';
            errorElement.textContent = message;
            input.parentNode.appendChild(errorElement);
        }
    }

    clearFieldError(input) {
        input.classList.remove('error');
        const existingError = input.parentNode.querySelector('.field-error');
        if (existingError) {
            existingError.remove();
        }
    }

    // Avatar upload functionality
    initAvatarUpload() {
        window.triggerAvatarUpload = () => {
            document.getElementById('avatarUpload').click();
        };

        window.previewAvatar = (input) => {
            if (input.files && input.files[0]) {
                const reader = new FileReader();
                
                reader.onload = (e) => {
                    const avatarImg = document.getElementById('profileAvatar');
                    const sidebarAvatar = document.querySelector('.admin-avatar');
                    
                    if (avatarImg) {
                        avatarImg.src = e.target.result;
                    }
                    if (sidebarAvatar) {
                        sidebarAvatar.src = e.target.result;
                    }
                    
                    this.showNotification('Avatar updated successfully!', 'success');
                };
                
                reader.readAsDataURL(input.files[0]);
            }
        };
    }

    // Password strength indicator
    initPasswordStrength() {
        const passwordInput = document.getElementById('newPassword');
        if (!passwordInput) return;

        passwordInput.addEventListener('input', (e) => {
            this.updatePasswordStrength(e.target.value);
        });

        // Create password strength indicator
        const strengthIndicator = document.createElement('div');
        strengthIndicator.className = 'password-strength';
        strengthIndicator.innerHTML = `
            <div class="strength-bar">
                <div class="strength-fill"></div>
            </div>
            <div class="strength-text">Password strength: <span>Weak</span></div>
        `;
        passwordInput.parentNode.appendChild(strengthIndicator);
    }

    updatePasswordStrength(password) {
        const strengthFill = document.querySelector('.strength-fill');
        const strengthText = document.querySelector('.strength-text span');
        
        if (!strengthFill || !strengthText) return;

        let strength = 0;
        let label = 'Weak';
        let color = '#e53e3e';

        // Length check
        if (password.length >= 8) strength += 25;
        if (password.length >= 12) strength += 25;

        // Character variety checks
        if (/[a-z]/.test(password)) strength += 12.5;
        if (/[A-Z]/.test(password)) strength += 12.5;
        if (/[0-9]/.test(password)) strength += 12.5;
        if (/[^A-Za-z0-9]/.test(password)) strength += 12.5;

        // Determine label and color
        if (strength >= 75) {
            label = 'Strong';
            color = '#38a169';
        } else if (strength >= 50) {
            label = 'Medium';
            color = '#ed8936';
        } else if (strength >= 25) {
            label = 'Fair';
            color = '#f6ad55';
        }

        strengthFill.style.width = `${strength}%`;
        strengthFill.style.backgroundColor = color;
        strengthText.textContent = label;
        strengthText.style.color = color;
    }

    // Form submission handler
    handleFormSubmit(form, event) {
        event.preventDefault();
        
        const submitBtn = form.querySelector('button[type="submit"]');
        const originalText = submitBtn.innerHTML;
        
        // Show loading state
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';

        // Simulate API call
        setTimeout(() => {
            // Reset button
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;

            if (form.classList.contains('security-form')) {
                this.showNotification('Password updated successfully!', 'success');
                form.reset();
                // Clear password strength indicator
                const strengthFill = document.querySelector('.strength-fill');
                const strengthText = document.querySelector('.strength-text span');
                if (strengthFill) strengthFill.style.width = '0%';
                if (strengthText) {
                    strengthText.textContent = 'Weak';
                    strengthText.style.color = '#e53e3e';
                }
            } else {
                this.showNotification('Profile updated successfully!', 'success');
            }
        }, 2000);
    }

    // Notification system
    initNotifications() {
        // Create notification container if it doesn't exist
        if (!document.getElementById('notificationContainer')) {
            const container = document.createElement('div');
            container.id = 'notificationContainer';
            container.className = 'notification-container';
            document.body.appendChild(container);
        }
    }

    showNotification(message, type = 'info', duration = 4000) {
        const container = document.getElementById('notificationContainer');
        const notification = document.createElement('div');
        
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <i class="fas fa-${this.getNotificationIcon(type)}"></i>
                <span>${message}</span>
            </div>
            <button class="notification-close" onclick="this.parentElement.remove()">
                <i class="fas fa-times"></i>
            </button>
        `;

        container.appendChild(notification);

        // Animate in
        setTimeout(() => {
            notification.classList.add('show');
        }, 100);

        // Auto remove
        setTimeout(() => {
            if (notification.parentElement) {
                notification.classList.remove('show');
                setTimeout(() => {
                    notification.remove();
                }, 300);
            }
        }, duration);
    }

    getNotificationIcon(type) {
        const icons = {
            success: 'check-circle',
            error: 'exclamation-circle',
            warning: 'exclamation-triangle',
            info: 'info-circle'
        };
        return icons[type] || 'info-circle';
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new AdminProfile();
});

// Export for testing
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AdminProfile;
}