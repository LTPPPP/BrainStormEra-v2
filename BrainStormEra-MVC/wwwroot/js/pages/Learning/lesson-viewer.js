// Learning Lesson Viewer JavaScript

// Global variables
let lessonId = '';
let courseId = '';
let startTime = Date.now();
let lastProgressUpdate = Date.now();

// Initialize lesson viewer
function initializeLessonViewer(lessonHashId, courseHashId) {
    lessonId = lessonHashId;
    courseId = courseHashId;
    
    // Start time tracking
    startTime = Date.now();
    lastProgressUpdate = Date.now();
    
    // Set up auto-save progress every 30 seconds
    setInterval(updateLessonProgress, 30000);
    
    // Save progress when leaving page
    window.addEventListener('beforeunload', function() {
        updateLessonProgress();
    });
    
    // Load course navigation when page loads
    document.addEventListener('DOMContentLoaded', function() {
        loadCourseNavigation();
    });
}

// Update lesson progress
function updateLessonProgress() {
    const currentTime = Date.now();
    const timeSpent = Math.floor((currentTime - lastProgressUpdate) / 1000);
    
    if (timeSpent > 5) { // Only update if at least 5 seconds passed
        fetch('/Learning/UpdateTimeSpent', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: `lessonId=${lessonId}&seconds=${timeSpent}`
        }).catch(error => {
            console.error('Error updating lesson progress:', error);
        });
        
        lastProgressUpdate = currentTime;
    }
}

// Mark lesson as complete
function markLessonComplete() {
    const markCompleteBtn = document.getElementById('markCompleteBtn');
    
    fetch('/Learning/CompleteLesson', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `lessonId=${lessonId}`
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            markCompleteBtn.innerHTML = '<i class="fas fa-check"></i> Completed';
            markCompleteBtn.disabled = true;
            
            const progressFill = document.getElementById('progressFill');
            const progressPercentage = document.getElementById('progressPercentage');
            
            if (progressFill) progressFill.style.width = '100%';
            if (progressPercentage) progressPercentage.textContent = '100%';
            
            if (window.showToast) {
                window.showToast('success', 'Lesson completed successfully!');
            }
            
            // Reload navigation to reflect completion
            setTimeout(loadCourseNavigation, 1000);
        }
    })
    .catch(error => {
        console.error('Error marking lesson complete:', error);
        if (window.showToast) {
            window.showToast('error', 'Failed to mark lesson as complete. Please try again.');
        }
    });
}

// Load course navigation
function loadCourseNavigation() {
    const navigationContainer = document.getElementById('courseNavigationContainer');
    if (!navigationContainer) return;
    
    fetch(`/Learning/GetCourseNavigation?courseId=${courseId}`)
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to load navigation');
            }
            return response.text();
        })
        .then(html => {
            navigationContainer.innerHTML = html;
            
            // Auto-expand current lesson's chapter after loading
            setTimeout(() => {
                expandCurrentLessonChapter();
            }, 100);
        })
        .catch(error => {
            console.error('Error loading course navigation:', error);
            navigationContainer.innerHTML = 
                '<div style="padding: 20px; text-align: center; color: #dc2626;">' +
                '<i class="fas fa-exclamation-triangle"></i> Failed to load navigation' +
                '</div>';
        });
}

// Auto-expand current lesson's chapter
function expandCurrentLessonChapter() {
    const currentLesson = document.querySelector('.nav-lesson.current');
    if (!currentLesson) return;
    
    const chapter = currentLesson.closest('.nav-chapter');
    if (!chapter) return;
    
    const chapterContent = chapter.querySelector('.chapter-lessons');
    const expandIcon = chapter.querySelector('.expand-icon');
    
    if (chapterContent) {
        chapterContent.style.display = 'block';
    }
    
    if (expandIcon) {
        expandIcon.style.transform = 'rotate(180deg)';
    }
    
    // Add active class to chapter
    chapter.classList.add('active');
}

// Video player event handlers
function setupVideoTracking() {
    const videoPlayer = document.getElementById('videoPlayer');
    if (!videoPlayer) return;
    
    // Track video progress for completion
    videoPlayer.addEventListener('timeupdate', function() {
        const progress = (videoPlayer.currentTime / videoPlayer.duration) * 100;
        updateVideoProgress(progress);
    });
    
    videoPlayer.addEventListener('ended', function() {
        // Auto-mark as complete when video ends
        updateVideoProgress(100);
        
        const markCompleteBtn = document.getElementById('markCompleteBtn');
        if (markCompleteBtn && !markCompleteBtn.disabled) {
            markLessonComplete();
        }
    });
}

// Update video progress
function updateVideoProgress(percentage) {
    const progressFill = document.getElementById('progressFill');
    const progressPercentage = document.getElementById('progressPercentage');
    
    if (progressFill) {
        progressFill.style.width = percentage + '%';
    }
    
    if (progressPercentage) {
        progressPercentage.textContent = Math.round(percentage) + '%';
    }
}

// Text lesson reading progress tracking
function setupTextReadingTracking() {
    const textContent = document.querySelector('.text-content');
    if (!textContent) return;
    
    let readingStartTime = Date.now();
    let isReading = false;
    
    // Track scrolling and reading time
    window.addEventListener('scroll', function() {
        if (!isReading) {
            isReading = true;
            readingStartTime = Date.now();
        }
        
        // Calculate reading progress based on scroll position
        const scrolled = window.pageYOffset;
        const maxHeight = document.body.scrollHeight - window.innerHeight;
        const progress = Math.min((scrolled / maxHeight) * 100, 100);
        
        updateVideoProgress(progress);
    });
    
    // Auto-complete when spending enough time reading
    setTimeout(() => {
        const markCompleteBtn = document.getElementById('markCompleteBtn');
        if (markCompleteBtn && !markCompleteBtn.disabled) {
            // Auto-suggest completion after 2 minutes of reading
            if (window.showToast) {
                window.showToast('info', 'Ready to mark this lesson as complete?');
            }
        }
    }, 120000); // 2 minutes
}

// PDF document tracking
function setupPDFTracking() {
    const pdfEmbed = document.querySelector('.pdf-embed');
    if (!pdfEmbed) return;
    
    // Mark as viewed when PDF is loaded
    pdfEmbed.addEventListener('load', function() {
        updateVideoProgress(50); // 50% for viewing PDF
        
        // Auto-complete after some time
        setTimeout(() => {
            updateVideoProgress(100);
            
            const markCompleteBtn = document.getElementById('markCompleteBtn');
            if (markCompleteBtn && !markCompleteBtn.disabled) {
                if (window.showToast) {
                    window.showToast('info', 'Document loaded. You can mark this lesson as complete.');
                }
            }
        }, 30000); // 30 seconds
    });
}

// Initialize appropriate tracking based on lesson type
function initializeLessonTracking() {
    // Video tracking
    setupVideoTracking();
    
    // Text reading tracking
    setupTextReadingTracking();
    
    // PDF tracking
    setupPDFTracking();
}

// Export functions for global access
window.markLessonComplete = markLessonComplete;
window.loadCourseNavigation = loadCourseNavigation;
window.initializeLessonViewer = initializeLessonViewer;

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    initializeLessonTracking();
}); 