// Certificate PDF JavaScript
document.addEventListener('DOMContentLoaded', function() {
    // Initialize certificate functionality
    initializeCertificate();
});

function initializeCertificate() {
    // Add loading state to download button
    const downloadBtn = document.querySelector('button[type="submit"]');
    if (downloadBtn) {
        downloadBtn.addEventListener('click', function() {
            this.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Generating PDF...';
            this.disabled = true;
            
            // Re-enable button after a delay (in case of error)
            setTimeout(() => {
                this.innerHTML = '<i class="fas fa-download"></i> Download PDF';
                this.disabled = false;
            }, 10000);
        });
    }
    
    // Add certificate preview functionality
    setupCertificatePreview();
}

function setupCertificatePreview() {
    // Add hover effects for certificate details
    const detailItems = document.querySelectorAll('.certificate-detail');
    detailItems.forEach(item => {
        item.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-2px)';
            this.style.boxShadow = '0 4px 12px rgba(224, 184, 76, 0.3)';
        });
        
        item.addEventListener('mouseleave', function() {
            this.style.transform = 'translateY(0)';
            this.style.boxShadow = '0 2px 8px rgba(224, 184, 76, 0.2)';
        });
    });
}

// Function to validate certificate data before download
function validateCertificateData() {
    const requiredFields = [
        'LearnerName',
        'CourseName', 
        'FormattedCompletedDate',
        'CertificateCode'
    ];
    
    // This would be called before PDF generation
    return true;
}

// Function to handle PDF download errors
function handleDownloadError(error) {
    console.error('PDF download error:', error);
    
    // Show error message to user
    const errorMessage = document.createElement('div');
    errorMessage.className = 'alert alert-danger mt-3';
    errorMessage.innerHTML = '<i class="fas fa-exclamation-triangle"></i> Error generating PDF. Please try again.';
    
    const container = document.querySelector('.certificate-container');
    if (container) {
        container.parentNode.insertBefore(errorMessage, container.nextSibling);
    }
    
    // Remove error message after 5 seconds
    setTimeout(() => {
        if (errorMessage.parentNode) {
            errorMessage.parentNode.removeChild(errorMessage);
        }
    }, 5000);
} 