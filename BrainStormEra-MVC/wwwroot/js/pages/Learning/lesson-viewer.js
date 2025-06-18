// Learning Lesson Viewer JavaScript

// Global variables
let lessonId = '';
let courseId = '';
let startTime = Date.now();
let lastProgressUpdate = Date.now();

// Initialize lesson viewer
function initializeLessonViewer(lessonId, courseHashId) {
    if (!lessonId || !courseHashId || lessonId === 'undefined' || courseHashId === 'undefined') {
        return;
    }
    
    currentLessonId = lessonId;
    courseId = courseHashId;
    
    
    
    // Start time tracking
    startTimeTracking();
    
    // Load course navigation
    loadCourseNavigation();
    
    // Set up keyboard shortcuts
    setupKeyboardShortcuts();
}

// Start time tracking for the lesson
function startTimeTracking() {
    if (!currentLessonId || currentLessonId === 'undefined') return;
    
    startTime = Date.now();
    
    // Auto-save progress every 30 seconds
    timeTrackingInterval = setInterval(() => {
        updateLessonProgress(false); // false = not completed
    }, 30000);
}

// Update lesson progress
function updateLessonProgress(isCompleted = false) {
    if (!currentLessonId || currentLessonId === 'undefined') return;
    
    const currentTime = Date.now();
    const timeSpent = Math.floor((currentTime - lastProgressUpdate) / 1000); // in seconds
    lastProgressUpdate = currentTime;
    
    const data = {
        lessonId: currentLessonId,
        timeSpent: timeSpent,
        isCompleted: isCompleted
    };
    
    fetch('/Learning/UpdateProgress', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        },
        body: JSON.stringify(data)
    })
    .then(response => response.json())
    .then(data => {
        // Progress updated successfully
    })
    .catch(error => {
        
    });
}

// Mark lesson as completed
function markLessonComplete() {
    if (!currentLessonId || currentLessonId === 'undefined') {
        return;
    }
    
    fetch('/Learning/MarkComplete', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        },
        body: JSON.stringify({ lessonId: currentLessonId })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            // Show success message
            if (typeof showToast === 'function') {
                showToast('Lesson completed successfully!', 'success');
            }
            
            // Refresh navigation to show updated progress
            loadCourseNavigation();
        }
    })
    .catch(error => {
        
    });
}

// Load course navigation
function loadCourseNavigation() {
    const navigationContainer = document.getElementById('course-navigation');
    
    if (!navigationContainer) {
        return;
    }
    
    if (!courseId || courseId === 'undefined') {
        return;
    }
    
    // Show loading state
    navigationContainer.innerHTML = '<div class="loading-navigation"><i class="fas fa-spinner fa-spin"></i> Loading course navigation...</div>';
    
    // Get course ID from the page
    let apiUrl = `/Learning/GetCourseNavigation/${courseId}`;
    
    
    
    fetch(apiUrl)
        .then(response => {
            
            
            if (!response.ok) {
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }
            return response.text();
        })
        .then(html => {
            
            
            if (!html || html.trim() === '') {
                throw new Error('Empty response received');
            }
            
            navigationContainer.innerHTML = html;
            
            // Restore expanded chapter states
            setTimeout(() => {
                restoreChapterStates();
                initializeNavigation();
            }, 100);
            
            
        })
        .catch(error => {
            navigationContainer.innerHTML = `
                <div class="navigation-error">
                    <i class="fas fa-exclamation-triangle"></i>
                    <p>Failed to load course navigation</p>
                    <button onclick="loadCourseNavigation()" class="btn btn-sm btn-primary">
                        <i class="fas fa-redo"></i> Retry
                    </button>
                </div>`;
        });
}

// Initialize navigation functionality
function initializeNavigation() {
    // Add click handlers for chapters
    document.querySelectorAll('.chapter-header').forEach(header => {
        header.addEventListener('click', function() {
            const chapterId = this.getAttribute('data-chapter');
            if (chapterId) {
                toggleNavChapter(chapterId);
            }
        });
    });
}

// Toggle chapter in navigation (fallback function)
function toggleNavChapter(chapterId) {
    const chapterContent = document.getElementById(`chapter-${chapterId}`);
    const expandIcon = document.querySelector(`[data-chapter="${chapterId}"] .expand-icon`);
    
    if (chapterContent) {
        const isExpanded = chapterContent.style.display !== 'none';
        
        chapterContent.style.display = isExpanded ? 'none' : 'block';
        
        if (expandIcon) {
            expandIcon.textContent = isExpanded ? '+' : '−';
        }
    }
    
    
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