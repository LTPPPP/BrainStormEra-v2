// Admin Profile Page JavaScript
document.addEventListener('DOMContentLoaded', function() {
    initializeProfile();
});

function initializeProfile() {
    // Initialize tooltips if Bootstrap is available
    if (typeof bootstrap !== 'undefined') {
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    // Add smooth scrolling for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });

    // Add loading states to action buttons
    addLoadingStates();
}

// Avatar Management Functions
function openAvatarModal() {
    const modal = new bootstrap.Modal(document.getElementById('avatarModal'));
    modal.show();
}

function previewAvatar(input) {
    if (input.files && input.files[0]) {
        const file = input.files[0];
        
        // Validate file size (2MB)
        if (file.size > 2 * 1024 * 1024) {
            showToast('File size must be less than 2MB', 'error');
            input.value = '';
            return;
        }

        // Validate file type
        const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
        if (!allowedTypes.includes(file.type.toLowerCase())) {
            showToast('Only image files (JPG, PNG, GIF) are allowed', 'error');
            input.value = '';
            return;
        }

        const reader = new FileReader();
        reader.onload = function(e) {
            const preview = document.getElementById('avatarPreview');
            if (preview) {
                preview.src = e.target.result;
            }
        };
        reader.readAsDataURL(file);
    }
}

async function uploadAvatar() {
    const fileInput = document.getElementById('avatarFile');
    const file = fileInput.files[0];

    if (!file) {
        showToast('Please select a file first', 'warning');
        return;
    }

    const uploadButton = document.querySelector('#avatarModal .btn-primary');
    const originalText = uploadButton.innerHTML;
    
    try {
        // Show loading state
        uploadButton.disabled = true;
        uploadButton.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Uploading...';

        const formData = new FormData();
        formData.append('avatarFile', file);

        const response = await fetch('/admin/profile?handler=UploadAvatar', {
            method: 'POST',
            body: formData,
            headers: {
                'RequestVerificationToken': getAntiForgeryToken()
            }
        });

        const result = await response.json();

        if (result.success) {
            // Update avatar images
            updateAvatarImages(result.avatarUrl);
            
            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('avatarModal'));
            modal.hide();
            
            // Reset form
            document.getElementById('avatarForm').reset();
            
            showToast('Avatar updated successfully!', 'success');
        } else {
            showToast(result.message || 'Failed to upload avatar', 'error');
        }
    } catch (error) {
        console.error('Avatar upload error:', error);
        showToast('An error occurred while uploading the avatar', 'error');
    } finally {
        // Restore button state
        uploadButton.disabled = false;
        uploadButton.innerHTML = originalText;
    }
}

function updateAvatarImages(newAvatarUrl) {
    // Update profile avatar
    const profileAvatar = document.getElementById('profileAvatar');
    if (profileAvatar) {
        profileAvatar.src = newAvatarUrl;
    }

    // Update navbar avatar
    const navbarAvatar = document.querySelector('.avatar-img');
    if (navbarAvatar) {
        navbarAvatar.src = newAvatarUrl;
    }

    // Update modal preview
    const avatarPreview = document.getElementById('avatarPreview');
    if (avatarPreview) {
        avatarPreview.src = newAvatarUrl;
    }
}

// Utility Functions
function getAntiForgeryToken() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    return token ? token.value : '';
}

function showToast(message, type = 'info') {
    // Create toast element
    const toastId = 'toast-' + Date.now();
    const toastHtml = `
        <div id="${toastId}" class="toast align-items-center text-white bg-${getBootstrapColorClass(type)} border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="${getToastIcon(type)} me-2"></i>
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;

    // Create toast container if it doesn't exist
    let toastContainer = document.getElementById('toastContainer');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toastContainer';
        toastContainer.className = 'toast-container position-fixed top-0 end-0 p-3';
        toastContainer.style.zIndex = '9999';
        document.body.appendChild(toastContainer);
    }

    // Add toast to container
    toastContainer.insertAdjacentHTML('beforeend', toastHtml);

    // Initialize and show toast
    const toastElement = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastElement, {
        autohide: true,
        delay: type === 'error' ? 5000 : 3000
    });
    
    toast.show();

    // Remove toast element after it's hidden
    toastElement.addEventListener('hidden.bs.toast', function() {
        toastElement.remove();
    });
}

function getBootstrapColorClass(type) {
    const colors = {
        'success': 'success',
        'error': 'danger',
        'warning': 'warning',
        'info': 'info'
    };
    return colors[type] || 'info';
}

function getToastIcon(type) {
    const icons = {
        'success': 'fas fa-check-circle',
        'error': 'fas fa-exclamation-circle',
        'warning': 'fas fa-exclamation-triangle',
        'info': 'fas fa-info-circle'
    };
    return icons[type] || 'fas fa-info-circle';
}

function addLoadingStates() {
    // Add loading states to buttons that navigate to other pages
    const actionButtons = document.querySelectorAll('.profile-actions .btn, .quick-action-item');
    
    actionButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            // Don't add loading state for modals or JavaScript actions
            if (this.getAttribute('data-bs-toggle') || this.getAttribute('onclick')) {
                return;
            }

            const originalContent = this.innerHTML;
            
            // Add loading spinner
            if (this.classList.contains('btn')) {
                this.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Loading...';
                this.disabled = true;
            } else {
                // For quick action items
                const icon = this.querySelector('.quick-action-icon i');
                if (icon) {
                    icon.className = 'fas fa-spinner fa-spin';
                }
            }

            // Restore original content after a delay (in case navigation fails)
            setTimeout(() => {
                this.innerHTML = originalContent;
                this.disabled = false;
            }, 3000);
        });
    });
}



// Statistics animation
function animateStatistics() {
    const statNumbers = document.querySelectorAll('.stat-number');
    
    statNumbers.forEach(stat => {
        const finalValue = parseInt(stat.textContent);
        let currentValue = 0;
        const increment = Math.ceil(finalValue / 50);
        const timer = setInterval(() => {
            currentValue += increment;
            if (currentValue >= finalValue) {
                currentValue = finalValue;
                clearInterval(timer);
            }
            stat.textContent = currentValue;
        }, 30);
    });
}

// Initialize statistics animation when the stats section comes into view
function initializeStatsAnimation() {
    const statsSection = document.querySelector('.profile-card .card-title');
    if (statsSection) {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    animateStatistics();
                    observer.unobserve(entry.target);
                }
            });
        });
        
        observer.observe(statsSection);
    }
}

// Initialize stats animation
document.addEventListener('DOMContentLoaded', function() {
    setTimeout(initializeStatsAnimation, 500);
});

// Handle modal cleanup
document.addEventListener('hidden.bs.modal', function (e) {
    if (e.target.id === 'avatarModal') {
        // Reset form when modal is closed
        const form = document.getElementById('avatarForm');
        if (form) {
            form.reset();
        }
        
        // Reset preview image
        const preview = document.getElementById('avatarPreview');
        const profileAvatar = document.getElementById('profileAvatar');
        if (preview && profileAvatar) {
            preview.src = profileAvatar.src;
        }
    }
});

// Export functions for global access
window.openAvatarModal = openAvatarModal;
window.previewAvatar = previewAvatar;
window.uploadAvatar = uploadAvatar; 