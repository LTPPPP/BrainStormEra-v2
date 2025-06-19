// Text Lesson JavaScript
let currentFontSize = 16;
let baseContentElement;

document.addEventListener('DOMContentLoaded', function() {
    baseContentElement = document.querySelector('.content-body');
    initializeReadingTracking();
});

function adjustFontSize(change) {
    currentFontSize += change;
    if (currentFontSize < 12) currentFontSize = 12;
    if (currentFontSize > 24) currentFontSize = 24;
    
    if (baseContentElement) {
        baseContentElement.style.fontSize = currentFontSize + 'px';
    }
    
    // Update button states
    updateFontSizeButtons();
}

function resetFontSize() {
    currentFontSize = 16;
    if (baseContentElement) {
        baseContentElement.style.fontSize = currentFontSize + 'px';
    }
    updateFontSizeButtons();
}

function updateFontSizeButtons() {
    document.querySelectorAll('.font-controls .btn').forEach(btn => {
        btn.classList.remove('btn-primary');
        btn.classList.add('btn-outline-secondary');
    });
    
    if (currentFontSize === 16) {
        const resetButton = document.querySelector('.font-controls .btn:nth-child(3)');
        if (resetButton) {
            resetButton.classList.remove('btn-outline-secondary');
            resetButton.classList.add('btn-primary');
        }
    }
}

function toggleDarkMode() {
    const contentWrapper = document.querySelector('.text-content-wrapper');
    const button = event.target.closest('button');
    
    if (contentWrapper && button) {
        contentWrapper.classList.toggle('dark-mode');
        
        if (contentWrapper.classList.contains('dark-mode')) {
            button.innerHTML = '<i class="fas fa-sun"></i> Light Mode';
        } else {
            button.innerHTML = '<i class="fas fa-moon"></i> Dark Mode';
        }
    }
}

// Reading progress tracking
function initializeReadingTracking() {
    let readingStartTime = Date.now();
    let isVisible = true;
    
    // Track visibility to pause timer when user leaves tab
    document.addEventListener('visibilitychange', function() {
        isVisible = !document.hidden;
        if (!isVisible) {
            // User left the tab
        } else {
            // User returned to tab
            readingStartTime = Date.now();
        }
    });
    
    // Simple reading time tracking
    setInterval(function() {
        if (isVisible) {
            const timeSpent = Math.floor((Date.now() - readingStartTime) / 1000);
            // TODO: Send reading progress to server
            console.log('Reading time:', timeSpent, 'seconds');
        }
    }, 10000); // Update every 10 seconds
} 