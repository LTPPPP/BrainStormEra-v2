// Test function to check if old one still exists
function markAsComplete() {
    console.log('OLD markAsComplete() called - this should NOT be called!');
    if (window.showToast) {
        window.showToast('info', 'OLD function called - Mark as complete functionality will be implemented');
    } else {
        alert('OLD function called - Mark as complete functionality will be implemented');
    }
}

// Main Lesson Learning JavaScript
function markAsCompleteNew() {
    // Get lesson ID from the current lesson data
    const lessonId = getLessonId();
    
    if (!lessonId) {
        if (window.showToast) {
            window.showToast('error', 'Unable to find lesson ID');
        } else {
            alert('Unable to find lesson ID');
        }
        return;
    }

    // Show loading state
    const completeBtn = document.querySelector('button[onclick="markAsCompleteNew()"]');
    if (completeBtn) {
        completeBtn.disabled = true;
        completeBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Completing...';
    }

    const antiForgeryToken = getAntiForgeryToken();

    // Make AJAX call to mark lesson as completed
    fetch('/Lesson/MarkAsComplete', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: `lessonId=${encodeURIComponent(lessonId)}&__RequestVerificationToken=${encodeURIComponent(antiForgeryToken)}`
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            if (window.showToast) {
                window.showToast('success', data.message || 'Lesson completed successfully!');
            } else {
                alert(data.message || 'Lesson completed successfully!');
            }
            
            // Store completion info for Course Learn page to detect
            const courseId = getCourseId();
            if (courseId) {
                const completionKey = `lesson_completed_${courseId}`;
                const completionData = {
                    lessonId: lessonId,
                    courseId: courseId,
                    timestamp: Date.now(),
                    needsRefresh: true
                };
                sessionStorage.setItem(completionKey, JSON.stringify(completionData));
            }
            
            // Update UI to show completion
            updateUIForCompletion();
            
            // Refresh progress data without reloading page
            setTimeout(() => {
                refreshLessonData();
            }, 1000);
        } else {
            if (window.showToast) {
                window.showToast('error', data.message || 'Failed to complete lesson');
            } else {
                alert(data.message || 'Failed to complete lesson');
            }
        }
    })
    .catch(error => {
        if (window.showToast) {
            window.showToast('error', 'An error occurred while completing the lesson');
        } else {
            alert('An error occurred while completing the lesson');
        }
    })
    .finally(() => {
        // Reset button state
        if (completeBtn) {
            completeBtn.disabled = false;
            completeBtn.innerHTML = '<i class="fas fa-check-circle"></i> Mark as Complete';
        }
    });
}

// Helper function to get lesson ID from the page
function getLessonId() {
    // Try to get from URL path
    const urlParts = window.location.pathname.split('/');
    const learnIndex = urlParts.indexOf('Learn');
    if (learnIndex >= 0 && learnIndex < urlParts.length - 1) {
        const lessonIdFromUrl = urlParts[learnIndex + 1];
        return lessonIdFromUrl;
    }
    
    // Try to get from data attributes or hidden fields
    const lessonIdElement = document.querySelector('[data-lesson-id]');
    if (lessonIdElement) {
        const lessonIdFromAttr = lessonIdElement.getAttribute('data-lesson-id');
        return lessonIdFromAttr;
    }
    
    // Try to get from a hidden input
    const hiddenInput = document.querySelector('input[name="lessonId"], input[name="LessonId"]');
    if (hiddenInput) {
        const lessonIdFromInput = hiddenInput.value;
        return lessonIdFromInput;
    }
    
    return null;
}

// Helper function to get anti-forgery token
function getAntiForgeryToken() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    return token ? token.value : '';
}

// Helper function to update UI for completion
function updateUIForCompletion() {
    const completeBtn = document.querySelector('button[onclick="markAsCompleteNew()"]');
    if (completeBtn) {
        completeBtn.outerHTML = `
            <button type="button" class="btn btn-outline-success" disabled>
                <i class="fas fa-check-circle"></i> Completed
            </button>
        `;
    }
    
    // Update lesson item in sidebar if exists
    const currentLessonItem = document.querySelector('.lesson-item.current');
    if (currentLessonItem) {
        currentLessonItem.classList.add('completed');
        const lessonIcon = currentLessonItem.querySelector('.lesson-icon');
        if (lessonIcon) {
            lessonIcon.innerHTML = '<i class="fas fa-check"></i>';
        }
        const lessonStatus = currentLessonItem.querySelector('.lesson-status');
        if (lessonStatus) {
            lessonStatus.textContent = 'Complete';
        }
    }
}

// Function to go back to course
function backToChapter() {
    // Try to get course ID from data attributes or URL
    const courseId = getCourseId();
    
    if (courseId) {
        window.location.href = `/Course/Learn/${courseId}`;
    } else {
        // Fallback to course index
        window.location.href = '/Course';
    }
}

// Helper function to get course ID
function getCourseId() {
    // Try to get from main content data attribute
    const mainContent = document.querySelector('.main-content[data-course-id]');
    if (mainContent) {
        return mainContent.getAttribute('data-course-id');
    }
    
    // Try to get from lesson learning content
    const lessonContent = document.querySelector('.lesson-content[data-course-id]');
    if (lessonContent) {
        return lessonContent.getAttribute('data-course-id');
    }
    
    // Try to get from sidebar course info
    const courseInfo = document.querySelector('.course-info[data-course-id]');
    if (courseInfo) {
        return courseInfo.getAttribute('data-course-id');
    }
    
    // Try to get from hidden input
    const hiddenCourseId = document.querySelector('input[name="courseId"], input[name="CourseId"]');
    if (hiddenCourseId) {
        return hiddenCourseId.value;
    }
    
    // Try to get from URL params if this is lesson learning page
    const urlParams = new URLSearchParams(window.location.search);
    const courseIdParam = urlParams.get('courseId');
    if (courseIdParam) {
        return courseIdParam;
    }
    
    // Try to get from current page context (if we have lesson data, we can derive course ID)
    const breadcrumb = document.querySelector('.breadcrumb-course-id');
    if (breadcrumb) {
        return breadcrumb.getAttribute('data-course-id');
    }
    
    console.log('Could not find course ID from any source');
    return null;
}

// Function to refresh lesson data without page reload
function refreshLessonData() {
    const lessonId = getLessonId();
    if (!lessonId) {
        return;
    }
    
    // Fetch updated lesson data
    fetch(`/Lesson/GetLessonData/${lessonId}`, {
        method: 'GET',
        headers: {
            'Content-Type': 'application/json'
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            // Update sidebar lessons
            updateSidebar(data.chapters);
            
            // Update course progress if available
            if (data.courseProgress !== undefined) {
                updateCourseProgress(data.courseProgress);
            }
        }
    })
    .catch(error => {
        // Fallback to page reload if AJAX fails
        setTimeout(() => {
            window.location.reload();
        }, 2000);
    });
}

// Function to update sidebar with fresh data
function updateSidebar(chapters) {
    if (!chapters || !Array.isArray(chapters)) {
        console.error('Invalid chapters data for sidebar update');
        return;
    }
    
    chapters.forEach(chapter => {
        if (chapter.lessons && Array.isArray(chapter.lessons)) {
            chapter.lessons.forEach(lesson => {
                const lessonElement = document.querySelector(`[data-lesson-id="${lesson.lessonId}"]`);
                if (lessonElement) {
                    // Update completion status
                    if (lesson.isCompleted) {
                        lessonElement.classList.add('completed');
                        const statusElement = lessonElement.querySelector('.lesson-status');
                        if (statusElement) {
                            statusElement.textContent = 'Complete';
                        }
                        const iconElement = lessonElement.querySelector('.lesson-icon i');
                        if (iconElement) {
                            iconElement.className = 'fas fa-check';
                        }
                    }
                    
                    // Update progress percentage if available
                    if (lesson.progressPercentage !== undefined) {
                        const progressElement = lessonElement.querySelector('.progress-bar');
                        if (progressElement) {
                            progressElement.style.width = `${lesson.progressPercentage}%`;
                        }
                    }
                }
            });
        }
    });
}

// Function to update course progress
function updateCourseProgress(progressPercentage) {
    console.log('Updating course progress to:', progressPercentage + '%');
    
    // Update progress bar in course header if exists
    const courseProgressBar = document.querySelector('.course-progress .progress-bar');
    if (courseProgressBar) {
        courseProgressBar.style.width = `${progressPercentage}%`;
        courseProgressBar.setAttribute('aria-valuenow', progressPercentage);
    }
    
    // Update progress text if exists
    const progressText = document.querySelector('.course-progress-text');
    if (progressText) {
        progressText.textContent = `${progressPercentage}% Complete`;
    }
    
    // Update any progress percentage displays
    const progressPercentageElements = document.querySelectorAll('.progress-percentage');
    progressPercentageElements.forEach(element => {
        element.textContent = `${progressPercentage}%`;
    });
}

// Auto-update progress for video lessons
document.addEventListener('DOMContentLoaded', function() {
    const lessonType = document.querySelector('[data-lesson-type]')?.getAttribute('data-lesson-type');
    
    if (lessonType && lessonType.toLowerCase() === 'video') {
        // TODO: Implement video progress tracking
        console.log('Video lesson detected - progress tracking enabled');
    }
}); 