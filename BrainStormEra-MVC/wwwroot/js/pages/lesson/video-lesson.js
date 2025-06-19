// Video Lesson JavaScript
function adjustPlaybackSpeed(speed) {
    const video = document.querySelector('video');
    if (video) {
        video.playbackRate = speed;
    }
    
    // Update active button
    document.querySelectorAll('.playback-controls .btn').forEach(btn => {
        btn.classList.remove('btn-primary');
        btn.classList.add('btn-outline-secondary');
    });
    event.target.classList.remove('btn-outline-secondary');
    event.target.classList.add('btn-primary');
}

// Show default video when original fails to load
function showDefaultVideo(element, defaultUrl) {
    const container = element.closest('.video-wrapper');
    if (container) {
        element.style.display = 'none';
        
        // Create default video iframe
        const defaultIframe = document.createElement('iframe');
        defaultIframe.src = defaultUrl;
        defaultIframe.title = 'Default Educational Content';
        defaultIframe.frameBorder = '0';
        defaultIframe.allow = 'accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share';
        defaultIframe.allowFullscreen = true;
        
        container.appendChild(defaultIframe);
        
        // Add warning message
        const warning = document.createElement('div');
        warning.className = 'alert alert-warning mt-3 default-video-fallback';
        warning.role = 'alert';
        warning.innerHTML = `
            <i class="fas fa-exclamation-triangle me-2"></i>
            <strong>Video load error.</strong> 
            Showing default educational content instead of the original video.
        `;
        
        container.parentNode.appendChild(warning);
    }
}

// Track video progress
document.addEventListener('DOMContentLoaded', function() {
    const video = document.querySelector('video');
    if (video) {
        video.addEventListener('timeupdate', function() {
            const progress = (video.currentTime / video.duration) * 100;
            // TODO: Send progress to server
            console.log('Video progress:', progress + '%');
        });
    }
}); 