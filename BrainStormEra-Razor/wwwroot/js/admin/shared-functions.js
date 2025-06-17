/* ==============================================
   SHARED ADMIN FUNCTIONS
   ============================================== */

// Refresh dashboard data
function refreshDashboard() {
    // Show loading state
    showLoading();
    
    // Reload the page to refresh data
    window.location.reload();
}

// Export data functionality
function exportData() {
    const currentPage = window.location.pathname;
    let exportUrl = '';
    
    if (currentPage.includes('/admin/users')) {
        exportUrl = '/admin/users?handler=Export';
    } else if (currentPage.includes('/admin/courses')) {
        exportUrl = '/admin/courses?handler=Export';
    } else {
        exportUrl = '/admin?handler=Export';
    }
    
    // Get current filters
    const urlParams = new URLSearchParams(window.location.search);
    const params = [];
    
    urlParams.forEach((value, key) => {
        if (key !== 'CurrentPage') {
            params.push(`${key}=${encodeURIComponent(value)}`);
        }
    });
    
    if (params.length > 0) {
        exportUrl += '&' + params.join('&');
    }
    
    // Create a temporary link and click it to download
    const link = document.createElement('a');
    link.href = exportUrl;
    link.download = '';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

// Show loading overlay
function showLoading() {
    const loadingHtml = `
        <div class="loading-overlay" id="loadingOverlay">
            <div class="loading-spinner"></div>
        </div>
    `;
    
    if (!document.getElementById('loadingOverlay')) {
        document.body.insertAdjacentHTML('beforeend', loadingHtml);
    }
}

// Hide loading overlay
function hideLoading() {
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) {
        overlay.remove();
    }
}

// Show toast notification
function showToast(message, type = 'info') {
    const toastHtml = `
        <div class="toast toast-${type}" id="adminToast">
            <div class="toast-content">
                <i class="fas ${getToastIcon(type)}"></i>
                <span>${message}</span>
            </div>
            <button type="button" class="toast-close" onclick="hideToast()">
                <i class="fas fa-times"></i>
            </button>
        </div>
    `;
    
    // Remove existing toast
    const existingToast = document.getElementById('adminToast');
    if (existingToast) {
        existingToast.remove();
    }
    
    // Add new toast
    document.body.insertAdjacentHTML('beforeend', toastHtml);
    
    // Auto-hide after 5 seconds
    setTimeout(() => {
        hideToast();
    }, 5000);
}

// Hide toast notification
function hideToast() {
    const toast = document.getElementById('adminToast');
    if (toast) {
        toast.classList.add('fade-out');
        setTimeout(() => {
            toast.remove();
        }, 300);
    }
}

// Get appropriate icon for toast type
function getToastIcon(type) {
    switch (type) {
        case 'success': return 'fa-check-circle';
        case 'error': return 'fa-exclamation-circle';
        case 'warning': return 'fa-exclamation-triangle';
        default: return 'fa-info-circle';
    }
}

// Confirm action with custom dialog
function confirmAction(message, onConfirm, onCancel = null) {
    const modalHtml = `
        <div class="admin-modal-overlay" id="confirmModal">
            <div class="admin-modal">
                <div class="admin-modal-header">
                    <h5>Confirm Action</h5>
                </div>
                <div class="admin-modal-body">
                    <p>${message}</p>
                </div>
                <div class="admin-modal-footer">
                    <button type="button" class="btn-action btn-cancel" onclick="hideConfirmModal()">Cancel</button>
                    <button type="button" class="btn-action btn-confirm" onclick="confirmActionExecute()">Confirm</button>
                </div>
            </div>
        </div>
    `;
    
    // Store callbacks
    window.confirmActionCallback = onConfirm;
    window.cancelActionCallback = onCancel;
    
    // Show modal
    document.body.insertAdjacentHTML('beforeend', modalHtml);
}

// Execute confirmed action
function confirmActionExecute() {
    hideConfirmModal();
    if (window.confirmActionCallback) {
        window.confirmActionCallback();
    }
}

// Hide confirmation modal
function hideConfirmModal() {
    const modal = document.getElementById('confirmModal');
    if (modal) {
        modal.remove();
    }
    
    if (window.cancelActionCallback) {
        window.cancelActionCallback();
    }
    
    // Clean up callbacks
    delete window.confirmActionCallback;
    delete window.cancelActionCallback;
}

// Format numbers with thousands separator
function formatNumber(num) {
    return num.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
}

// Format date to readable string
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
}

// Debounce function for search inputs
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

// Initialize common admin functionality
document.addEventListener('DOMContentLoaded', function() {
    // Add loading states to buttons
    const actionButtons = document.querySelectorAll('.btn-action');
    actionButtons.forEach(button => {
        button.addEventListener('click', function() {
            if (!this.disabled) {
                this.classList.add('loading');
            }
        });
    });
    
    // Auto-hide alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            alert.style.opacity = '0';
            setTimeout(() => {
                alert.remove();
            }, 300);
        }, 5000);
    });
    
    // Add search debouncing
    const searchInputs = document.querySelectorAll('input[type="text"][name*="Search"]');
    searchInputs.forEach(input => {
        const debouncedSearch = debounce(() => {
            input.closest('form').submit();
        }, 500);
        
        input.addEventListener('input', debouncedSearch);
    });
});

// Animation for spinning refresh button
document.addEventListener('DOMContentLoaded', function() {
    const refreshBtns = document.querySelectorAll('.refresh-btn');
    refreshBtns.forEach(btn => {
        btn.addEventListener('click', function() {
            const icon = this.querySelector('i');
            icon.style.animation = 'spin 1s linear infinite';
            
            setTimeout(() => {
                icon.style.animation = '';
            }, 1000);
        });
    });
});

// CSS for animations (injected dynamically)
const adminStyles = `
<style>
@keyframes spin {
    from { transform: rotate(0deg); }
    to { transform: rotate(360deg); }
}

.toast {
    position: fixed;
    top: 20px;
    right: 20px;
    background: white;
    border-radius: 8px;
    box-shadow: 0 4px 12px rgba(0,0,0,0.15);
    padding: 15px;
    min-width: 300px;
    z-index: 10000;
    display: flex;
    align-items: center;
    justify-content: space-between;
    animation: slideIn 0.3s ease;
}

.toast.fade-out {
    animation: slideOut 0.3s ease;
}

.toast-content {
    display: flex;
    align-items: center;
    gap: 10px;
}

.toast-success { border-left: 4px solid #28a745; }
.toast-error { border-left: 4px solid #dc3545; }
.toast-warning { border-left: 4px solid #ffc107; }
.toast-info { border-left: 4px solid #17a2b8; }

.toast-close {
    background: none;
    border: none;
    color: #6c757d;
    cursor: pointer;
    font-size: 14px;
}

.admin-modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: rgba(0,0,0,0.5);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 10001;
}

.admin-modal {
    background: white;
    border-radius: 8px;
    min-width: 400px;
    max-width: 90vw;
}

.admin-modal-header {
    padding: 20px;
    border-bottom: 1px solid #dee2e6;
}

.admin-modal-header h5 {
    margin: 0;
    color: #495057;
}

.admin-modal-body {
    padding: 20px;
}

.admin-modal-footer {
    padding: 20px;
    border-top: 1px solid #dee2e6;
    display: flex;
    gap: 10px;
    justify-content: flex-end;
}

.btn-cancel {
    background: #6c757d;
    color: white;
}

.btn-confirm {
    background: #dc3545;
    color: white;
}

.btn-action.loading {
    position: relative;
    color: transparent;
}

.btn-action.loading::after {
    content: '';
    position: absolute;
    top: 50%;
    left: 50%;
    margin: -8px 0 0 -8px;
    width: 16px;
    height: 16px;
    border: 2px solid transparent;
    border-top: 2px solid currentColor;
    border-radius: 50%;
    animation: spin 1s linear infinite;
}

@keyframes slideIn {
    from { transform: translateX(100%); }
    to { transform: translateX(0); }
}

@keyframes slideOut {
    from { transform: translateX(0); }
    to { transform: translateX(100%); }
}
</style>
`;

// Inject styles
document.head.insertAdjacentHTML('beforeend', adminStyles); 