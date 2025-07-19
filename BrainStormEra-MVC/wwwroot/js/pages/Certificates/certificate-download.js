/**
 * Certificate Download JavaScript
 * Handles certificate download functionality with enhanced user experience
 */

class CertificateDownload {
    constructor() {
        this.initializeEventListeners();
        this.setupDownloadHandlers();
    }

    /**
     * Initialize all event listeners
     */
    initializeEventListeners() {
        document.addEventListener('DOMContentLoaded', () => {
            this.setupDownloadButton();
            this.setupPrintButton();
            this.setupCertificatePreview();
        });
    }

    /**
     * Setup download button functionality
     */
    setupDownloadButton() {
        const downloadBtn = document.getElementById('downloadBtn');
        if (downloadBtn) {
            downloadBtn.addEventListener('click', (e) => {
                e.preventDefault();
                this.handleDownloadClick(downloadBtn);
            });
        }
    }

    /**
     * Setup print button functionality
     */
    setupPrintButton() {
        const printBtn = document.querySelector('button[onclick="window.print()"]');
        if (printBtn) {
            printBtn.addEventListener('click', (e) => {
                e.preventDefault();
                this.handlePrintClick(printBtn);
            });
        }
    }

    /**
     * Setup certificate preview interactions
     */
    setupCertificatePreview() {
        const detailItems = document.querySelectorAll('.certificate-detail');
        detailItems.forEach((item) => {
            item.addEventListener('mouseenter', () => {
                this.addHoverEffect(item);
            });

            item.addEventListener('mouseleave', () => {
                this.removeHoverEffect(item);
            });
        });
    }

    /**
     * Handle download button click
     * @param {HTMLElement} button - The download button element
     */
    handleDownloadClick(button) {
        const statusDiv = document.getElementById('downloadStatus');
        const originalText = button.innerHTML;
        const originalHref = button.href;
        
        // Show loading state
        button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Generating PDF...';
        button.disabled = true;
        button.href = '#';
        
        if (statusDiv) {
            statusDiv.style.display = 'block';
        }

        // Add loading class for styling
        button.classList.add('loading');

        // Track download start time
        const startTime = Date.now();

        // Create a hidden iframe for download
        const iframe = document.createElement('iframe');
        iframe.style.display = 'none';
        iframe.src = originalHref;
        
        iframe.onload = () => {
            this.handleDownloadSuccess(button, originalText, originalHref, startTime);
        };

        iframe.onerror = () => {
            this.handleDownloadError(button, originalText, originalHref);
        };

        document.body.appendChild(iframe);

        // Fallback timeout in case iframe doesn't load
        setTimeout(() => {
            if (button.disabled) {
                this.handleDownloadSuccess(button, originalText, originalHref, startTime);
            }
        }, 15000);
    }

    /**
     * Handle print button click
     * @param {HTMLElement} button - The print button element
     */
    handlePrintClick(button) {
        const originalText = button.innerHTML;
        
        // Show loading state
        button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Preparing...';
        button.disabled = true;

        // Add loading class
        button.classList.add('loading');

        // Small delay to show loading state
        setTimeout(() => {
            try {
                window.print();
                this.handlePrintSuccess(button, originalText);
            } catch (error) {
                console.error('Print error:', error);
                this.handlePrintError(button, originalText);
            }
        }, 500);
    }

    /**
     * Handle successful download
     * @param {HTMLElement} button - The download button
     * @param {string} originalText - Original button text
     * @param {string} originalHref - Original button href
     * @param {number} startTime - Download start time
     */
    handleDownloadSuccess(button, originalText, originalHref, startTime) {
        const duration = Date.now() - startTime;
        
        // Restore button state
        button.innerHTML = originalText;
        button.disabled = false;
        button.href = originalHref;
        button.classList.remove('loading');

        // Hide status
        const statusDiv = document.getElementById('downloadStatus');
        if (statusDiv) {
            statusDiv.style.display = 'none';
        }

        // Show success message
        this.showNotification('PDF generated successfully!', 'success');

        // Log download time for analytics
        console.log(`Certificate download completed in ${duration}ms`);
    }

    /**
     * Handle download error
     * @param {HTMLElement} button - The download button
     * @param {string} originalText - Original button text
     * @param {string} originalHref - Original button href
     */
    handleDownloadError(button, originalText, originalHref) {
        // Restore button state
        button.innerHTML = originalText;
        button.disabled = false;
        button.href = originalHref;
        button.classList.remove('loading');

        // Hide status
        const statusDiv = document.getElementById('downloadStatus');
        if (statusDiv) {
            statusDiv.style.display = 'none';
        }

        // Show error message
        this.showNotification('Failed to generate PDF. Please try again.', 'error');
    }

    /**
     * Handle successful print
     * @param {HTMLElement} button - The print button
     * @param {string} originalText - Original button text
     */
    handlePrintSuccess(button, originalText) {
        button.innerHTML = originalText;
        button.disabled = false;
        button.classList.remove('loading');
        
        this.showNotification('Print dialog opened successfully!', 'success');
    }

    /**
     * Handle print error
     * @param {HTMLElement} button - The print button
     * @param {string} originalText - Original button text
     */
    handlePrintError(button, originalText) {
        button.innerHTML = originalText;
        button.disabled = false;
        button.classList.remove('loading');
        
        this.showNotification('Failed to open print dialog. Please try again.', 'error');
    }

    /**
     * Add hover effect to certificate detail items
     * @param {HTMLElement} item - The detail item element
     */
    addHoverEffect(item) {
        item.style.transform = 'translateY(-2px)';
        item.style.boxShadow = '0 4px 12px rgba(224, 184, 76, 0.3)';
        item.style.transition = 'all 0.3s ease';
    }

    /**
     * Remove hover effect from certificate detail items
     * @param {HTMLElement} item - The detail item element
     */
    removeHoverEffect(item) {
        item.style.transform = 'translateY(0)';
        item.style.boxShadow = '0 2px 8px rgba(224, 184, 76, 0.2)';
    }

    /**
     * Show notification message
     * @param {string} message - The message to display
     * @param {string} type - The type of notification (success, error, warning, info)
     */
    showNotification(message, type = 'info') {
        // Remove existing notifications
        const existingNotifications = document.querySelectorAll('.certificate-notification');
        existingNotifications.forEach(notification => notification.remove());

        // Create notification element
        const notification = document.createElement('div');
        notification.className = `certificate-notification alert alert-${type === 'error' ? 'danger' : type} alert-dismissible fade show`;
        notification.innerHTML = `
            <i class="fas fa-${this.getNotificationIcon(type)} me-2"></i>
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;

        // Add styles
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
            min-width: 300px;
            max-width: 400px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        `;

        // Add to page
        document.body.appendChild(notification);

        // Auto remove after 5 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.remove();
            }
        }, 5000);
    }

    /**
     * Get notification icon based on type
     * @param {string} type - The notification type
     * @returns {string} The icon class name
     */
    getNotificationIcon(type) {
        const icons = {
            success: 'check-circle',
            error: 'exclamation-triangle',
            warning: 'exclamation-triangle',
            info: 'info-circle'
        };
        return icons[type] || 'info-circle';
    }

    /**
     * Validate certificate data before download
     * @returns {boolean} Whether the data is valid
     */
    validateCertificateData() {
        const requiredFields = [
            'LearnerName',
            'CourseName',
            'FormattedCompletedDate',
            'CertificateCode'
        ];

        // Check if all required fields are present in the model
        const modelData = window.certificateData || {};
        
        for (const field of requiredFields) {
            if (!modelData[field]) {
                console.warn(`Missing required field: ${field}`);
                return false;
            }
        }

        return true;
    }

    /**
     * Setup download handlers for different scenarios
     */
    setupDownloadHandlers() {
        // Handle browser back/forward navigation
        window.addEventListener('popstate', () => {
            this.resetButtonStates();
        });

        // Handle page visibility changes
        document.addEventListener('visibilitychange', () => {
            if (document.hidden) {
                this.resetButtonStates();
            }
        });

        // Handle beforeunload event
        window.addEventListener('beforeunload', () => {
            this.resetButtonStates();
        });
    }

    /**
     * Reset all button states to default
     */
    resetButtonStates() {
        const buttons = document.querySelectorAll('.btn.loading');
        buttons.forEach(button => {
            button.disabled = false;
            button.classList.remove('loading');
            
            // Reset download button specifically
            if (button.id === 'downloadBtn') {
                button.innerHTML = '<i class="fas fa-download"></i> Download PDF';
                button.href = button.getAttribute('data-original-href') || button.href;
            }
            
            // Reset print button specifically
            if (button.querySelector('.fa-print')) {
                button.innerHTML = '<i class="fas fa-print"></i> Print Certificate';
            }
        });

        // Hide status div
        const statusDiv = document.getElementById('downloadStatus');
        if (statusDiv) {
            statusDiv.style.display = 'none';
        }
    }
}

// Initialize certificate download functionality
const certificateDownload = new CertificateDownload();

// Export for global access if needed
window.CertificateDownload = CertificateDownload;
window.certificateDownload = certificateDownload; 