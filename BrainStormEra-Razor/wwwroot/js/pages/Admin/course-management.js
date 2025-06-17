// Course Management JavaScript
class CourseManagement {
    constructor() {
        this.currentFilters = {
            search: '',
            category: '',
            status: '',
            price: '',
            difficulty: '',
            instructor: '',
            sortBy: 'newest'
        };
        this.isLoading = false;
        this.init();
    }

    // Utility function to get proper image URL
    getImageUrl(imagePath, defaultImage = '/SharedMedia/defaults/default-course.svg') {
        if (!imagePath || imagePath.trim() === '') {
            return defaultImage;
        }
        
        // If it's already a full URL, return as is
        if (imagePath.startsWith('http://') || imagePath.startsWith('https://')) {
            return imagePath;
        }
        
        // If it starts with /, it's already a proper relative path
        if (imagePath.startsWith('/')) {
            return imagePath;
        }
        
        // Otherwise, assume it needs to be prefixed with /SharedMedia/
        return imagePath.startsWith('SharedMedia/') ? `/${imagePath}` : `/SharedMedia/${imagePath}`;
    }

    // Utility function to handle property mapping (supports both camelCase and PascalCase)
    getProperty(obj, propName) {
        if (!obj) return null;
        
        // Try the exact case first
        if (obj.hasOwnProperty(propName)) {
            return obj[propName];
        }
        
        // Try camelCase
        const camelCase = propName.charAt(0).toLowerCase() + propName.slice(1);
        if (obj.hasOwnProperty(camelCase)) {
            return obj[camelCase];
        }
        
        // Try PascalCase
        const pascalCase = propName.charAt(0).toUpperCase() + propName.slice(1);
        if (obj.hasOwnProperty(pascalCase)) {
            return obj[pascalCase];
        }
        
        return null;
    }

    init() {
        this.bindEvents();
        this.initializeFilters();
        this.setupKeyboardShortcuts();
    }

    bindEvents() {
        // Filter form submission
        const filterForm = document.querySelector('.filters-section form');
        if (filterForm) {
            filterForm.addEventListener('submit', (e) => this.handleFilterSubmit(e));
        }

        // Real-time search
        const searchInput = document.querySelector('#SearchQuery');
        if (searchInput) {
            let searchTimeout;
            searchInput.addEventListener('input', (e) => {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(() => {
                    this.handleRealTimeSearch(e.target.value);
                }, 300);
            });
        }

        // Filter changes
        document.querySelectorAll('.filter-group select, .filter-group input').forEach(element => {
            element.addEventListener('change', (e) => this.handleFilterChange(e));
        });

        // Course action buttons
        document.querySelectorAll('.btn-approve').forEach(btn => {
            btn.addEventListener('click', (e) => this.handleApprove(e));
        });

        document.querySelectorAll('.btn-reject').forEach(btn => {
            btn.addEventListener('click', (e) => this.handleReject(e));
        });

        document.querySelectorAll('.btn-ban').forEach(btn => {
            btn.addEventListener('click', (e) => this.handleBan(e));
        });

        document.querySelectorAll('.btn-details').forEach(btn => {
            btn.addEventListener('click', (e) => this.handleViewDetails(e));
        });
    }

    initializeFilters() {
        // Load filters from URL parameters
        const urlParams = new URLSearchParams(window.location.search);
        
        Object.keys(this.currentFilters).forEach(key => {
            const value = urlParams.get(key === 'search' ? 'SearchQuery' : this.capitalizeFirst(key) + 'Filter');
            if (value) {
                this.currentFilters[key] = value;
                const element = document.querySelector(`[name="${key === 'search' ? 'SearchQuery' : this.capitalizeFirst(key) + 'Filter'}"]`);
                if (element) {
                    element.value = value;
                }
            }
        });
    }

    setupKeyboardShortcuts() {
        document.addEventListener('keydown', (e) => {
            // Ctrl/Cmd + K to focus search
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                const searchInput = document.querySelector('#SearchQuery');
                if (searchInput) {
                    searchInput.focus();
                    searchInput.select();
                }
            }

            // Escape to clear search
            if (e.key === 'Escape') {
                const searchInput = document.querySelector('#SearchQuery');
                if (searchInput && document.activeElement === searchInput) {
                    searchInput.value = '';
                    this.handleRealTimeSearch('');
                }
            }
        });
    }

    handleFilterSubmit(e) {
        e.preventDefault();
        this.applyFilters();
    }

    handleFilterChange(e) {
        const name = e.target.name;
        const value = e.target.value;
        
        if (name === 'SearchQuery') {
            this.currentFilters.search = value;
        } else if (name.endsWith('Filter')) {
            const filterType = name.replace('Filter', '').toLowerCase();
            this.currentFilters[filterType] = value;
        }

        // Auto-apply filters with debounce
        if (this.filterTimeout) {
            clearTimeout(this.filterTimeout);
        }
        this.filterTimeout = setTimeout(() => {
            this.applyFilters();
        }, 500);
    }

    handleRealTimeSearch(searchTerm) {
        this.currentFilters.search = searchTerm;
        this.applyFilters();
    }

    applyFilters() {
        if (this.isLoading) return;

        this.showLoading();
        
        // Build URL with current filters
        const params = new URLSearchParams();
        
        if (this.currentFilters.search) params.set('SearchQuery', this.currentFilters.search);
        if (this.currentFilters.category) params.set('CategoryFilter', this.currentFilters.category);
        if (this.currentFilters.status) params.set('StatusFilter', this.currentFilters.status);
        if (this.currentFilters.price) params.set('PriceFilter', this.currentFilters.price);
        if (this.currentFilters.difficulty) params.set('DifficultyFilter', this.currentFilters.difficulty);
        if (this.currentFilters.instructor) params.set('InstructorFilter', this.currentFilters.instructor);
        if (this.currentFilters.sortBy) params.set('SortBy', this.currentFilters.sortBy);
        
        params.set('CurrentPage', '1'); // Reset to first page

        // Navigate to filtered URL
        window.location.href = `${window.location.pathname}?${params.toString()}`;
    }

    async handleApprove(e) {
        const button = e.target.closest('.btn-approve');
        const courseId = button.dataset.courseId;
        const courseName = button.dataset.courseName;

        if (!await this.confirmAction('approve', courseName)) {
            return;
        }

        await this.updateCourseStatus(courseId, true, 'Course approved successfully');
    }

    async handleReject(e) {
        const button = e.target.closest('.btn-reject');
        const courseId = button.dataset.courseId;
        const courseName = button.dataset.courseName;

        if (!await this.confirmAction('reject', courseName)) {
            return;
        }

        await this.updateCourseStatus(courseId, false, 'Course rejected successfully');
    }

    async handleBan(e) {
        const button = e.target.closest('.btn-ban');
        const courseId = button.dataset.courseId;
        const courseName = button.dataset.courseName;

        if (!await this.confirmAction('ban', courseName)) {
            return;
        }

        await this.banCourse(courseId, 'Course banned successfully');
    }

    async handleViewDetails(e) {
        const button = e.target.closest('.btn-details');
        const courseId = button.dataset.courseId;
        
        await this.showCourseDetails(courseId);
    }

    async confirmAction(action, courseName, isDestructive = false) {
        const messages = {
            approve: `Are you sure you want to approve the course "${courseName}"?`,
            reject: `Are you sure you want to reject the course "${courseName}"?`,
            ban: `Are you sure you want to ban the course "${courseName}"? This will make it unavailable to all users.`
        };

        const result = await this.showConfirmModal(messages[action], isDestructive);
        return result;
    }

    async updateCourseStatus(courseId, isApproved, successMessage) {
        try {
            this.showLoading();

            const response = await fetch('/admin/courses?handler=UpdateCourseStatus', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify({ courseId, isApproved })
            });

            const data = await response.json();

            if (data.success) {
                this.showToast(successMessage, 'success');
                setTimeout(() => {
                    window.location.reload();
                }, 1000);
            } else {
                this.showToast(data.message || 'Failed to update course status', 'error');
            }
        } catch (error) {
            console.error('Error updating course status:', error);
            this.showToast('An error occurred while updating course status', 'error');
        } finally {
            this.hideLoading();
        }
    }

    async banCourse(courseId, successMessage) {
        try {
            this.showLoading();

            const response = await fetch('/admin/courses?handler=BanCourse', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify({ courseId })
            });

            const data = await response.json();

            if (data.success) {
                this.showToast(successMessage, 'success');
                setTimeout(() => {
                    window.location.reload();
                }, 1000);
            } else {
                this.showToast(data.message || 'Failed to ban course', 'error');
            }
        } catch (error) {
            console.error('Error banning course:', error);
            this.showToast('An error occurred while banning course', 'error');
        } finally {
            this.hideLoading();
        }
    }

    async showCourseDetails(courseId) {
        try {
            this.showLoading();

            const response = await fetch(`/admin/courses?handler=CourseDetails&courseId=${courseId}`);
            const result = await response.json();

            if (result.success && result.data) {
                this.displayCourseDetailsModal(result.data);
            } else {
                this.showToast('Failed to load course details', 'error');
            }
        } catch (error) {
            console.error('Error loading course details:', error);
            this.showToast('An error occurred while loading course details', 'error');
        } finally {
            this.hideLoading();
        }
    }

    displayCourseDetailsModal(courseData) {
        // Create modal HTML
        const modalHtml = this.createCourseDetailsModalHtml(courseData);
        
        // Remove existing modal if any
        const existingModal = document.getElementById('courseDetailsModal');
        if (existingModal) {
            existingModal.remove();
        }

        // Add modal to DOM
        document.body.insertAdjacentHTML('beforeend', modalHtml);

        // Show modal
        const modal = new bootstrap.Modal(document.getElementById('courseDetailsModal'));
        modal.show();

        // Clean up when modal is hidden
        document.getElementById('courseDetailsModal').addEventListener('hidden.bs.modal', function() {
            this.remove();
        });
    }

    createCourseDetailsModalHtml(course) {
        return `
            <div class="modal fade" id="courseDetailsModal" tabindex="-1" aria-labelledby="courseDetailsModalLabel" aria-hidden="true">
                <div class="modal-dialog modal-xl">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="courseDetailsModalLabel">
                                <i class="fas fa-info-circle"></i> Course Details
                            </h5>
                            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body">
                            <div class="course-details-header">
                                <img src="${this.getImageUrl(course.coursePicture || course.CoursePicture)}" alt="${course.courseName || course.CourseName}"
                                     onerror="this.onerror=null; this.src='/SharedMedia/defaults/default-course.svg';" 
                                     class="course-details-image">
                                <div class="course-details-info">
                                    <h2 class="course-details-title">${course.courseName || course.CourseName}</h2>
                                    <div class="course-details-instructor">
                                        <i class="fas fa-user"></i> By ${this.getProperty(course, 'instructorName') || this.getProperty(course, 'InstructorName') || 'Unknown Instructor'}
                                    </div>
                                    <div class="course-details-stats">
                                        <div class="stat-item">
                                            <span class="stat-item-value">${this.getProperty(course, 'priceText') || this.getProperty(course, 'PriceText') || 'Price not available'}</span>
                                            <span class="stat-item-label">Price</span>
                                        </div>
                                        <div class="stat-item">
                                            <span class="stat-item-value">${this.getProperty(course, 'enrollmentCount') || this.getProperty(course, 'EnrollmentCount') || 'Enrollment count not available'}</span>
                                            <span class="stat-item-label">Students</span>
                                        </div>
                                        <div class="stat-item">
                                            <span class="stat-item-value">${(this.getProperty(course, 'averageRating') || this.getProperty(course, 'AverageRating') || 0).toFixed ? (this.getProperty(course, 'averageRating') || this.getProperty(course, 'AverageRating')).toFixed(1) : (this.getProperty(course, 'averageRating') || this.getProperty(course, 'AverageRating') || 0)}</span>
                                            <span class="stat-item-label">Rating</span>
                                        </div>
                                        <div class="stat-item">
                                            <span class="stat-item-value">${this.getProperty(course, 'totalLessons') || this.getProperty(course, 'TotalLessons') || 'Lesson count not available'}</span>
                                            <span class="stat-item-label">Lessons</span>
                                        </div>
                                        <div class="stat-item">
                                            <span class="stat-item-value">${this.getProperty(course, 'durationText') || this.getProperty(course, 'DurationText') || 'Duration not available'}</span>
                                            <span class="stat-item-label">Duration</span>
                                        </div>
                                        <div class="stat-item">
                                            <span class="stat-item-value"><span class="status-badge ${this.getProperty(course, 'statusBadgeClass') || this.getProperty(course, 'StatusBadgeClass')}">${this.getProperty(course, 'statusText') || this.getProperty(course, 'StatusText') || 'Status not available'}</span></span>
                                            <span class="stat-item-label">Status</span>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div class="course-details-section">
                                <h4 class="section-title"><i class="fas fa-align-left"></i> Description</h4>
                                <div class="course-description">${this.getProperty(course, 'courseDescription') || this.getProperty(course, 'CourseDescription') || 'No description provided.'}</div>
                            </div>

                            <div class="course-details-section">
                                <h4 class="section-title"><i class="fas fa-info-circle"></i> Course Information</h4>
                                <div class="row">
                                    <div class="col-md-6">
                                        <p><strong>Difficulty:</strong> ${this.getProperty(course, 'difficultyText') || this.getProperty(course, 'DifficultyText') || 'Difficulty not available'}</p>
                                        <p><strong>Categories:</strong> ${this.getProperty(course, 'categoriesText') || this.getProperty(course, 'CategoriesText') || 'Categories not available'}</p>
                                        <p><strong>Created:</strong> ${new Date(this.getProperty(course, 'createdAt') || this.getProperty(course, 'CreatedAt')).toLocaleDateString()}</p>
                                    </div>
                                    <div class="col-md-6">
                                        <p><strong>Sequential Access:</strong> ${(this.getProperty(course, 'enforceSequentialAccess') || this.getProperty(course, 'EnforceSequentialAccess')) ? 'Yes' : 'No'}</p>
                                        <p><strong>Preview Allowed:</strong> ${(this.getProperty(course, 'allowLessonPreview') || this.getProperty(course, 'AllowLessonPreview')) ? 'Yes' : 'No'}</p>
                                        <p><strong>Featured:</strong> ${(this.getProperty(course, 'isFeatured') || this.getProperty(course, 'IsFeatured')) ? 'Yes' : 'No'}</p>
                                    </div>
                                </div>
                            </div>

                            ${(this.getProperty(course, 'chapters') || this.getProperty(course, 'Chapters')) && (this.getProperty(course, 'chapters') || this.getProperty(course, 'Chapters')).length > 0 ? `
                            <div class="course-details-section">
                                <h4 class="section-title"><i class="fas fa-list"></i> Course Structure</h4>
                                <div class="chapters-list">
                                    ${(this.getProperty(course, 'chapters') || this.getProperty(course, 'Chapters')).map(chapter => `
                                        <div class="chapter-item">
                                            <div class="chapter-info">
                                                <div class="chapter-order">${this.getProperty(chapter, 'chapterOrder') || this.getProperty(chapter, 'ChapterOrder')}</div>
                                                <div>
                                                    <div class="chapter-name">${this.getProperty(chapter, 'chapterName') || this.getProperty(chapter, 'ChapterName')}</div>
                                                    <div class="chapter-stats">${this.getProperty(chapter, 'lessonCount') || this.getProperty(chapter, 'LessonCount')} lesson(s)</div>
                                                </div>
                                            </div>
                                            ${(this.getProperty(chapter, 'isLocked') || this.getProperty(chapter, 'IsLocked')) ? '<i class="fas fa-lock text-warning"></i>' : '<i class="fas fa-unlock text-success"></i>'}
                                        </div>
                                    `).join('')}
                                </div>
                            </div>
                            ` : ''}

                            ${(this.getProperty(course, 'recentReviews') || this.getProperty(course, 'RecentReviews')) && (this.getProperty(course, 'recentReviews') || this.getProperty(course, 'RecentReviews')).length > 0 ? `
                            <div class="course-details-section">
                                <h4 class="section-title"><i class="fas fa-star"></i> Recent Reviews</h4>
                                <div class="reviews-list">
                                    ${(this.getProperty(course, 'recentReviews') || this.getProperty(course, 'RecentReviews')).map(review => `
                                        <div class="review-item">
                                            <div class="review-header">
                                                <div class="review-user">
                                                    <img src="${this.getImageUrl(this.getProperty(review, 'userImage') || this.getProperty(review, 'UserImage'), '/SharedMedia/defaults/default-avatar.svg')}" 
                                                         alt="${this.getProperty(review, 'userName') || this.getProperty(review, 'UserName')}" 
                                                         onerror="this.onerror=null; this.src='/SharedMedia/defaults/default-avatar.svg';" 
                                                         class="review-avatar">
                                                    <span>${this.getProperty(review, 'userName') || this.getProperty(review, 'UserName')}</span>
                                                </div>
                                                <div class="review-rating">
                                                    ${'★'.repeat(Math.floor(this.getProperty(review, 'rating') || this.getProperty(review, 'Rating')))}${'☆'.repeat(5 - Math.floor(this.getProperty(review, 'rating') || this.getProperty(review, 'Rating')))} ${this.getProperty(review, 'rating') || this.getProperty(review, 'Rating')}
                                                </div>
                                            </div>
                                            <div class="review-comment">${this.getProperty(review, 'comment') || this.getProperty(review, 'Comment') || 'No comment provided'}</div>
                                        </div>
                                    `).join('')}
                                </div>
                            </div>
                            ` : ''}
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    showConfirmModal(message, isDestructive = false) {
        return new Promise((resolve) => {
            const modalHtml = `
                <div class="modal fade" id="confirmModal" tabindex="-1" aria-hidden="true">
                    <div class="modal-dialog">
                        <div class="modal-content">
                            <div class="modal-header ${isDestructive ? 'bg-danger text-white' : 'bg-primary text-white'}">
                                <h5 class="modal-title">
                                    <i class="fas ${isDestructive ? 'fa-exclamation-triangle' : 'fa-question-circle'}"></i>
                                    Confirm Action
                                </h5>
                            </div>
                            <div class="modal-body">
                                <p>${message}</p>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                                <button type="button" class="btn ${isDestructive ? 'btn-danger' : 'btn-primary'}" id="confirmButton">
                                    ${isDestructive ? 'Delete' : 'Confirm'}
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `;

            document.body.insertAdjacentHTML('beforeend', modalHtml);
            const modal = new bootstrap.Modal(document.getElementById('confirmModal'));
            
            document.getElementById('confirmButton').addEventListener('click', () => {
                modal.hide();
                resolve(true);
            });

            document.getElementById('confirmModal').addEventListener('hidden.bs.modal', function() {
                this.remove();
                resolve(false);
            });

            modal.show();
        });
    }

    showLoading() {
        if (document.getElementById('loadingOverlay')) return;

        const loadingHtml = `
            <div id="loadingOverlay" class="loading-overlay">
                <div class="loading-spinner"></div>
            </div>
        `;
        document.body.insertAdjacentHTML('beforeend', loadingHtml);
        this.isLoading = true;
    }

    hideLoading() {
        const loadingOverlay = document.getElementById('loadingOverlay');
        if (loadingOverlay) {
            loadingOverlay.remove();
        }
        this.isLoading = false;
    }

    showToast(message, type = 'info') {
        // Remove existing toast
        const existingToast = document.getElementById('courseToast');
        if (existingToast) {
            existingToast.remove();
        }

        const toastClass = {
            success: 'bg-success',
            error: 'bg-danger',
            warning: 'bg-warning',
            info: 'bg-info'
        }[type] || 'bg-info';

        const icon = {
            success: 'fa-check-circle',
            error: 'fa-exclamation-circle',
            warning: 'fa-exclamation-triangle',
            info: 'fa-info-circle'
        }[type] || 'fa-info-circle';

        const toastHtml = `
            <div id="courseToast" class="toast-container position-fixed top-0 end-0 p-3">
                <div class="toast show ${toastClass} text-white" role="alert">
                    <div class="toast-header ${toastClass} text-white border-0">
                        <i class="fas ${icon} me-2"></i>
                        <strong class="me-auto">Course Management</strong>
                        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
                    </div>
                    <div class="toast-body">
                        ${message}
                    </div>
                </div>
            </div>
        `;

        document.body.insertAdjacentHTML('beforeend', toastHtml);

        // Auto-hide after 5 seconds
        setTimeout(() => {
            const toast = document.getElementById('courseToast');
            if (toast) {
                toast.remove();
            }
        }, 5000);
    }

    getAntiForgeryToken() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        return token ? token.value : '';
    }

    capitalizeFirst(str) {
        return str.charAt(0).toUpperCase() + str.slice(1);
    }
}

// Global functions for backward compatibility
function updateCourseStatus(courseId, isApproved) {
    const courseManagement = window.courseManagement || new CourseManagement();
    courseManagement.updateCourseStatus(courseId, isApproved, 
        isApproved ? 'Course approved successfully' : 'Course rejected successfully');
}

function banCourse(courseId, courseName) {
    const courseManagement = window.courseManagement || new CourseManagement();
    courseManagement.handleBan({
        target: {
            closest: () => ({
                dataset: { courseId, courseName }
            })
        }
    });
}

function showCourseDetails(courseId) {
    const courseManagement = window.courseManagement || new CourseManagement();
    courseManagement.showCourseDetails(courseId);
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    window.courseManagement = new CourseManagement();
});

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = CourseManagement;
} 