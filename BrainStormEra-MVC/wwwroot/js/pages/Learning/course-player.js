// Course Player JavaScript for Learning Experience

// Global variables
let courseId = '';
let isEnrolled = false;

// Initialize course player
function initializeCoursePlayer(courseHashId) {

    if (!courseHashId || courseHashId === '' || courseHashId === 'undefined') {
        return;
    }
    
    courseId = courseHashId;

    
    // Check enrollment status
    checkEnrollmentStatus();
    
    // Load progress summary
    loadProgressSummary();
    
    // Set up event listeners
    setupEventListeners();
    
    // Restore chapter states
    restoreChapterStates();
    
    // Initialize progress tracking
    initializeProgressTracking();
}

// Check enrollment status
function checkEnrollmentStatus() {
    if (!courseId) return;
    
    fetch(`/Learning/CheckEnrollment?courseId=${encodeURIComponent(courseId)}`)
        .then(response => response.json())
        .then(data => {
            isEnrolled = data.isEnrolled;
            updateEnrollmentUI(data.isEnrolled);
            
            if (data.isEnrolled) {
                loadProgressSummary();
            }
        })
        .catch(error => {
            
        });
}

// Update enrollment UI based on status
function updateEnrollmentUI(isEnrolled) {
    const enrollBtn = document.getElementById('enrollBtn');
    const takeBtn = document.getElementById('takeCourseBtn');
    const continueBtn = document.getElementById('continueCourseBtn');
    
    if (enrollBtn) {
        enrollBtn.style.display = isEnrolled ? 'none' : 'block';
    }
    
    if (takeBtn) {
        takeBtn.style.display = isEnrolled ? 'inline-block' : 'none';
    }
    
    if (continueBtn) {
        continueBtn.style.display = isEnrolled ? 'inline-block' : 'none';
    }
}

// Enroll in course
function enrollInCourse() {

    if (!courseId) {
        return;
    }
    
    const enrollBtn = document.getElementById('enrollBtn');
    if (enrollBtn) {
        enrollBtn.disabled = true;
        enrollBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Enrolling...';
    }
    
    fetch('/Learning/Enroll', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        },
        body: JSON.stringify({ courseId: courseId })
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            updateEnrollmentUI(true);
            loadProgressSummary();
            
            // Show success message
            if (typeof showToast === 'function') {
                showToast('Successfully enrolled in course!', 'success');
            }
        } else {
            // Show error message
            if (typeof showToast === 'function') {
                showToast(data.message || 'Failed to enroll in course', 'error');
            }
        }
    })
    .catch(error => {
        
    })
    .finally(() => {
        if (enrollBtn) {
            enrollBtn.disabled = false;
            enrollBtn.innerHTML = '<i class="fas fa-play"></i> Enroll Now';
        }
    });
}

// Load progress summary
function loadProgressSummary() {
    if (!courseId) return;
    
    fetch(`/Learning/GetProgressSummary?courseId=${encodeURIComponent(courseId)}`)
        .then(response => response.json())
        .then(data => {
            if (!data.error) {
                updateProgressUI(data);
            }
        })
        .catch(error => {
            
        });
}

// Update progress UI
function updateProgressUI(progressData) {
    // Update progress bar
    const progressBar = document.querySelector('.progress-fill');
    const progressText = document.querySelector('.progress-percentage');
    
    if (progressBar) {
        animateProgress(progressBar, 0, progressData.progressPercentage);
    }
    
    if (progressText) {
        progressText.textContent = `${Math.round(progressData.progressPercentage)}%`;
    }
    
    // Update lesson counts
    const completedLessonsElement = document.querySelector('.completed-lessons');
    const totalLessonsElement = document.querySelector('.total-lessons');
    
    if (completedLessonsElement) {
        completedLessonsElement.textContent = progressData.completedLessons;
    }
    
    if (totalLessonsElement) {
        totalLessonsElement.textContent = progressData.totalLessons;
    }
    
    // Update time spent
    const timeSpentElement = document.querySelector('.time-spent');
    if (timeSpentElement) {
        timeSpentElement.textContent = progressData.totalTimeSpent;
    }
    
    // Update estimated time remaining
    const timeRemainingElement = document.querySelector('.time-remaining');
    if (timeRemainingElement) {
        timeRemainingElement.textContent = progressData.estimatedTimeRemaining;
    }
    
    // Show completion status
    if (progressData.isCompleted) {
        const completionBadge = document.querySelector('.completion-badge');
        if (completionBadge) {
            completionBadge.style.display = 'inline-block';
        }
    }
}

// Navigate to lesson
function navigateToLesson(lessonId) {
    if (!lessonId) {
        return;
    }
    
    // Check if lesson is accessible
    fetch(`/Learning/CheckLessonAccess?lessonId=${encodeURIComponent(lessonId)}`)
        .then(response => response.json())
        .then(data => {
            if (data.hasAccess) {
                window.location.href = `/Learning/Lesson/${lessonId}`;
            } else {
                if (typeof showToast === 'function') {
                    showToast(data.message || 'This lesson is not yet available', 'warning');
                }
            }
        })
        .catch(error => {
            
        });
}

// Toggle chapter (course player version)
function toggleChapter(chapterId) {

    const chapterContent = document.getElementById(`chapter-${chapterId}`);
    const expandIcon = document.querySelector(`[data-chapter="${chapterId}"] .expand-icon`);
    
    if (chapterContent) {
        const isExpanded = chapterContent.style.display !== 'none';
        
        chapterContent.style.display = isExpanded ? 'none' : 'block';
        
        if (expandIcon) {
            expandIcon.textContent = isExpanded ? '+' : '−';
        }
        
        // Save state to localStorage
        const expandedChapters = JSON.parse(localStorage.getItem('expandedChapters') || '{}');
        expandedChapters[chapterId] = !isExpanded;
        localStorage.setItem('expandedChapters', JSON.stringify(expandedChapters));
    }
}

// Expand chapter
function expandChapter(chapterContent, expandIcon) {
    chapterContent.style.display = 'block';
    if (expandIcon) {
        expandIcon.style.transform = 'rotate(180deg)';
    }
    
    // Smooth animation
    chapterContent.style.maxHeight = '0';
    chapterContent.style.overflow = 'hidden';
    chapterContent.style.transition = 'max-height 0.3s ease';
    
    setTimeout(() => {
        chapterContent.style.maxHeight = chapterContent.scrollHeight + 'px';
    }, 10);
    
    setTimeout(() => {
        chapterContent.style.maxHeight = '';
        chapterContent.style.overflow = '';
        chapterContent.style.transition = '';
    }, 300);
}

// Collapse chapter
function collapseChapter(chapterContent, expandIcon) {
    chapterContent.style.maxHeight = chapterContent.scrollHeight + 'px';
    chapterContent.style.overflow = 'hidden';
    chapterContent.style.transition = 'max-height 0.3s ease';
    
    setTimeout(() => {
        chapterContent.style.maxHeight = '0';
    }, 10);
    
    setTimeout(() => {
        chapterContent.style.display = 'none';
        chapterContent.style.maxHeight = '';
        chapterContent.style.overflow = '';
        chapterContent.style.transition = '';
        if (expandIcon) {
            expandIcon.style.transform = 'rotate(0deg)';
        }
    }, 300);
}

// Save chapter state
function saveChapterState(chapterId, isExpanded) {
    const key = `coursePlayer_${courseId}_chapters`;
    
    let chapterStates = {};
    try {
        chapterStates = JSON.parse(localStorage.getItem(key) || '{}');
    } catch (e) {
        console.warn('Error parsing chapter states from localStorage:', e);
    }
    
    chapterStates[chapterId] = isExpanded;
    localStorage.setItem(key, JSON.stringify(chapterStates));
}

// Restore chapter states
function restoreChapterStates() {
    if (!courseId) return;
    
    const key = `coursePlayer_${courseId}_chapters`;
    
    try {
        const chapterStates = JSON.parse(localStorage.getItem(key) || '{}');
        
        Object.entries(chapterStates).forEach(([chapterId, isExpanded]) => {
            if (isExpanded) {
                const chapterContent = document.getElementById(`chapter-${chapterId}`);
                const expandIcon = document.querySelector(`[data-chapter="${chapterId}"] .expand-icon`);
                
                if (chapterContent && expandIcon) {
                    chapterContent.style.display = 'block';
                    expandIcon.style.transform = 'rotate(180deg)';
                }
            }
        });
    } catch (e) {
        console.warn('Error restoring chapter states:', e);
    }
}

// Set up event listeners
function setupEventListeners() {
    // Enroll button
    const enrollBtn = document.getElementById('enrollBtn');
    if (enrollBtn) {
        enrollBtn.addEventListener('click', enrollInCourse);
    }
    
    // Chapter toggle functionality
    document.addEventListener('click', function(e) {
        if (e.target.matches('.chapter-header, .chapter-header *')) {
            const header = e.target.closest('.chapter-header');
            if (header) {
                const chapterId = header.getAttribute('data-chapter-id') || 
                                header.getAttribute('onclick')?.match(/'([^']+)'/)?.[1];
                if (chapterId) {
                    toggleChapter(chapterId);
                }
            }
        }
        
        // Lesson navigation
        if (e.target.matches('[data-lesson-id], [data-lesson-id] *')) {
            const element = e.target.closest('[data-lesson-id]');
            if (element) {
                const lessonId = element.getAttribute('data-lesson-id');
                navigateToLesson(lessonId);
            }
        }
    });
}

// Initialize progress tracking
function initializeProgressTracking() {
    updateProgressDisplay();
}

// Update progress display with animation
function updateProgressDisplay() {
    const progressElements = document.querySelectorAll('.progress-fill');
    progressElements.forEach(element => {
        const width = parseFloat(element.style.width) || 0;
        if (width > 0) {
            animateProgress(element, 0, width);
        }
    });
}

// Animate progress bar
function animateProgress(element, start, end) {
    const duration = 1000; // 1 second
    const startTime = performance.now();
    
    const animate = (currentTime) => {
        const elapsed = currentTime - startTime;
        const progress = Math.min(elapsed / duration, 1);
        const current = start + (end - start) * easeOutCubic(progress);
        
        element.style.width = current + '%';
        
        if (progress < 1) {
            requestAnimationFrame(animate);
        }
    };
    
    requestAnimationFrame(animate);
}

// Easing function
function easeOutCubic(t) {
    return 1 - Math.pow(1 - t, 3);
}

// Auto-expand current lesson's chapter
function expandCurrentLessonChapter() {
    const currentLesson = document.querySelector('.lesson-item.current, .nav-lesson.current');
    if (!currentLesson) return;
    
    const chapter = currentLesson.closest('.chapter-content, .chapter-lessons');
    if (!chapter) return;
    
    const chapterId = chapter.id.replace(/^(chapter-|nav-chapter-)/, '');
    const expandIcon = document.querySelector(`[data-chapter="${chapterId}"] .expand-icon, [onclick*="${chapterId}"] .expand-icon`);
    
    if (chapter) {
        chapter.style.display = 'block';
    }
    
    if (expandIcon) {
        expandIcon.style.transform = 'rotate(180deg)';
    }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Auto-expand current lesson's chapter if we're on course page
    expandCurrentLessonChapter();
}); 