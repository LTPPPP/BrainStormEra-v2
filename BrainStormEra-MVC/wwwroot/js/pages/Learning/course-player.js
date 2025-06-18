// Course Player JavaScript
class CoursePlayer {
    constructor() {
        this.init();
    }

    init() {
        this.setupEventListeners();
        this.initializeProgressTracking();
        this.setupAutoSave();
    }

    setupEventListeners() {
        // Chapter toggle functionality
        document.querySelectorAll('.chapter-header').forEach(header => {
            header.addEventListener('click', (e) => {
                e.preventDefault();
                const chapterId = header.getAttribute('onclick')?.match(/'([^']+)'/)?.[1];
                if (chapterId) {
                    this.toggleChapter(chapterId);
                }
            });
        });

        // Lesson link tracking
        document.querySelectorAll('.lesson-link').forEach(link => {
            link.addEventListener('click', (e) => {
                this.trackLessonAccess(link.getAttribute('href'));
            });
        });

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            this.handleKeyboardShortcuts(e);
        });
    }

    toggleChapter(chapterId) {
        const chapterContent = document.getElementById(`chapter-${chapterId}`);
        const expandIcon = chapterContent?.previousElementSibling?.querySelector('.expand-icon');
        
        if (!chapterContent || !expandIcon) return;

        const isExpanded = chapterContent.style.display === 'block';
        
        if (isExpanded) {
            this.collapseChapter(chapterContent, expandIcon);
        } else {
            this.expandChapter(chapterContent, expandIcon);
        }

        // Save chapter state to localStorage
        this.saveChapterState(chapterId, !isExpanded);
    }

    expandChapter(chapterContent, expandIcon) {
        chapterContent.style.display = 'block';
        expandIcon.style.transform = 'rotate(180deg)';
        
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

    collapseChapter(chapterContent, expandIcon) {
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
            expandIcon.style.transform = 'rotate(0deg)';
        }, 300);
    }

    saveChapterState(chapterId, isExpanded) {
        const courseId = this.getCurrentCourseId();
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

    restoreChapterStates() {
        const courseId = this.getCurrentCourseId();
        const key = `coursePlayer_${courseId}_chapters`;
        
        try {
            const chapterStates = JSON.parse(localStorage.getItem(key) || '{}');
            
            Object.entries(chapterStates).forEach(([chapterId, isExpanded]) => {
                if (isExpanded) {
                    const chapterContent = document.getElementById(`chapter-${chapterId}`);
                    const expandIcon = chapterContent?.previousElementSibling?.querySelector('.expand-icon');
                    
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

    getCurrentCourseId() {
        // Extract course ID from URL or data attribute
        const url = window.location.pathname;
        const matches = url.match(/\/Learning\/Course\/([^\/]+)/);
        return matches ? matches[1] : 'unknown';
    }

    trackLessonAccess(lessonUrl) {
        // Track lesson access for analytics
        if (typeof gtag !== 'undefined') {
            gtag('event', 'lesson_access', {
                'event_category': 'Learning',
                'event_label': lessonUrl
            });
        }
        
        // Update last accessed time
        localStorage.setItem('lastLessonAccess', Date.now().toString());
    }

    initializeProgressTracking() {
        this.updateProgressDisplay();
        this.trackTimeSpent();
    }

    updateProgressDisplay() {
        const progressElements = document.querySelectorAll('.progress-fill');
        progressElements.forEach(element => {
            const width = element.style.width;
            if (width) {
                this.animateProgress(element, 0, parseFloat(width));
            }
        });
    }

    animateProgress(element, start, end) {
        const duration = 1000; // 1 second
        const startTime = performance.now();
        
        const animate = (currentTime) => {
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);
            const current = start + (end - start) * this.easeOutCubic(progress);
            
            element.style.width = current + '%';
            
            if (progress < 1) {
                requestAnimationFrame(animate);
            }
        };
        
        requestAnimationFrame(animate);
    }

    easeOutCubic(t) {
        return 1 - Math.pow(1 - t, 3);
    }

    trackTimeSpent() {
        let startTime = Date.now();
        let totalTime = 0;
        
        // Track time when page is visible
        document.addEventListener('visibilitychange', () => {
            if (document.hidden) {
                totalTime += Date.now() - startTime;
                this.saveTimeSpent(totalTime);
            } else {
                startTime = Date.now();
            }
        });
        
        // Save time before page unload
        window.addEventListener('beforeunload', () => {
            totalTime += Date.now() - startTime;
            this.saveTimeSpent(totalTime);
        });
        
        // Periodic save every 30 seconds
        setInterval(() => {
            totalTime += Date.now() - startTime;
            this.saveTimeSpent(totalTime);
            startTime = Date.now();
        }, 30000);
    }

    saveTimeSpent(timeMs) {
        const courseId = this.getCurrentCourseId();
        const key = `courseTime_${courseId}`;
        
        try {
            const existingTime = parseInt(localStorage.getItem(key) || '0');
            localStorage.setItem(key, (existingTime + timeMs).toString());
        } catch (e) {
            console.warn('Error saving time spent:', e);
        }
    }

    setupAutoSave() {
        // Auto-save scroll position
        let scrollTimeout;
        window.addEventListener('scroll', () => {
            clearTimeout(scrollTimeout);
            scrollTimeout = setTimeout(() => {
                this.saveScrollPosition();
            }, 250);
        });
        
        // Restore scroll position on load
        this.restoreScrollPosition();
    }

    saveScrollPosition() {
        const courseId = this.getCurrentCourseId();
        const key = `courseScroll_${courseId}`;
        localStorage.setItem(key, window.scrollY.toString());
    }

    restoreScrollPosition() {
        const courseId = this.getCurrentCourseId();
        const key = `courseScroll_${courseId}`;
        
        try {
            const scrollY = parseInt(localStorage.getItem(key) || '0');
            if (scrollY > 0) {
                setTimeout(() => {
                    window.scrollTo({ top: scrollY, behavior: 'smooth' });
                }, 100);
            }
        } catch (e) {
            console.warn('Error restoring scroll position:', e);
        }
    }

    handleKeyboardShortcuts(e) {
        // Only handle shortcuts when not typing in an input
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') {
            return;
        }
        
        switch (e.key) {
            case 'c':
                // Focus on continue button
                const continueBtn = document.querySelector('.btn[href*="Continue"]');
                if (continueBtn) {
                    continueBtn.focus();
                    e.preventDefault();
                }
                break;
                
            case 'Escape':
                // Collapse all chapters
                document.querySelectorAll('.chapter-content[style*="block"]').forEach(content => {
                    const chapterId = content.id.replace('chapter-', '');
                    this.toggleChapter(chapterId);
                });
                e.preventDefault();
                break;
                
            case 'Enter':
                // Activate focused element
                if (document.activeElement && document.activeElement.tagName === 'A') {
                    document.activeElement.click();
                    e.preventDefault();
                }
                break;
        }
    }

    // Public methods for external use
    expandAllChapters() {
        document.querySelectorAll('.chapter-header').forEach(header => {
            const chapterId = header.getAttribute('onclick')?.match(/'([^']+)'/)?.[1];
            if (chapterId) {
                const chapterContent = document.getElementById(`chapter-${chapterId}`);
                if (chapterContent && chapterContent.style.display !== 'block') {
                    this.toggleChapter(chapterId);
                }
            }
        });
    }

    collapseAllChapters() {
        document.querySelectorAll('.chapter-header').forEach(header => {
            const chapterId = header.getAttribute('onclick')?.match(/'([^']+)'/)?.[1];
            if (chapterId) {
                const chapterContent = document.getElementById(`chapter-${chapterId}`);
                if (chapterContent && chapterContent.style.display === 'block') {
                    this.toggleChapter(chapterId);
                }
            }
        });
    }

    showProgressSummary() {
        const totalLessons = document.querySelectorAll('.lesson-item').length;
        const completedLessons = document.querySelectorAll('.lesson-item.completed').length;
        const lockedLessons = document.querySelectorAll('.lesson-item.locked').length;
        
        return {
            total: totalLessons,
            completed: completedLessons,
            locked: lockedLessons,
            available: totalLessons - lockedLessons,
            completionRate: totalLessons > 0 ? (completedLessons / totalLessons * 100).toFixed(1) : 0
        };
    }
}

// Global function for backward compatibility
function toggleChapter(chapterId) {
    if (window.coursePlayer) {
        window.coursePlayer.toggleChapter(chapterId);
    }
}

// Initialize course player when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    window.coursePlayer = new CoursePlayer();
    
    // Restore chapter states after initialization
    setTimeout(() => {
        window.coursePlayer.restoreChapterStates();
    }, 100);
});

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = CoursePlayer;
} 