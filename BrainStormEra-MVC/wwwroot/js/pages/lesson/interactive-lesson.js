// Interactive Lesson JavaScript
function toggleFullscreen() {
    const wrapper = document.querySelector('.interactive-wrapper');
    if (!document.fullscreenElement) {
        wrapper.requestFullscreen().catch(err => {
            console.log('Error attempting to enable fullscreen:', err);
        });
    } else {
        document.exitFullscreen();
    }
}

function resetInteraction() {
    // Reload the interactive content
    location.reload();
}

// Initialize interaction tracking
document.addEventListener('DOMContentLoaded', function() {
    initializeInteractionTracking();
});

function initializeInteractionTracking() {
    // Track interaction time
    let interactionStartTime = Date.now();
    let totalInteractionTime = 0;
    
    // Track mouse movements and clicks as engagement indicators
    document.addEventListener('mousemove', function() {
        // User is actively engaging
    });
    
    document.addEventListener('click', function() {
        // User clicked - mark as interaction
        totalInteractionTime = Date.now() - interactionStartTime;
        console.log('Total interaction time:', Math.floor(totalInteractionTime / 1000), 'seconds');
    });
} 