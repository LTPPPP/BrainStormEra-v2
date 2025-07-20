// Course Lazy Loading and Performance Optimizations

// Virtual scrolling for large lists (if more than 50 courses)
function initVirtualScrolling() {
    const courseCards = document.querySelectorAll('.course-card');
    if (courseCards.length > 50) {
        const container = document.getElementById('coursesGrid');
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    // Only animate visible cards
                    const card = entry.target;
                    if (!card.classList.contains('animated')) {
                        card.classList.add('animated');
                        card.style.opacity = '1';
                        card.style.transform = 'translateY(0)';
                    }
                }
            });
        }, { threshold: 0.1 });
        
        courseCards.forEach(card => {
            card.style.opacity = '0';
            card.style.transform = 'translateY(20px)';
            observer.observe(card);
        });
    }
}

// Memory management - cleanup observers
function cleanupObservers() {
    // Clean up any existing observers to prevent memory leaks
    if (window.imageObserver) {
        window.imageObserver.disconnect();
    }
    if (window.scrollObserver) {
        window.scrollObserver.disconnect();
    }
}

// Lazy loading for images
function initLazyLoading() {
    const lazyImages = document.querySelectorAll('.lazy-image');
    
    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    
                    // Load image
                    img.src = img.dataset.src;
                    
                    // Add loaded class when image finishes loading
                    img.onload = function() {
                        this.classList.add('loaded');
                        this.classList.remove('lazy-image');
                    };
                    
                    // Handle error
                    img.onerror = function() {
                        this.src = '/SharedMedia/defaults/default-course.svg';
                        this.classList.add('loaded');
                        this.classList.remove('lazy-image');
                    };
                    
                    imageObserver.unobserve(img);
                }
            });
        }, {
            rootMargin: '100px 0px', // Start loading 100px before image comes into view
            threshold: 0.01
        });
        
        lazyImages.forEach(img => imageObserver.observe(img));
    } else {
        // Fallback for older browsers
        lazyImages.forEach(img => {
            img.src = img.dataset.src;
            img.onload = function() {
                this.classList.add('loaded');
                this.classList.remove('lazy-image');
            };
            img.onerror = function() {
                this.src = '/SharedMedia/defaults/default-course.svg';
                this.classList.add('loaded');
                this.classList.remove('lazy-image');
            };
        });
    }
}

// Debounced scroll handler for better performance
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Optimized animation with requestAnimationFrame
function animateCourseCards() {
    const courseCards = document.querySelectorAll('.course-card');
    let index = 0;
    
    function animateNext() {
        if (index >= courseCards.length) return;
        
        const card = courseCards[index];
        card.style.opacity = '0';
        card.style.transform = 'translateY(20px)';
        
        requestAnimationFrame(() => {
            card.style.transition = 'all 0.3s ease';
            card.style.opacity = '1';
            card.style.transform = 'translateY(0)';
        });
        
        index++;
        setTimeout(animateNext, 30); // Reduced delay for faster animation
    }
    
    animateNext();
}

// Optimized loadAllCourses with better error handling
function loadAllCourses() {
    // Cleanup before loading new content
    cleanupObservers();
    
    const search = document.getElementById('courseSearchInput')?.value || '';
    const category = document.getElementById('categoryFilter')?.value || '';
    const price = document.getElementById('priceFilter')?.value || '';
    const difficulty = document.getElementById('difficultyFilter')?.value || '';
    const sortBy = document.getElementById('sortSelect')?.value || 'newest';
    
    // Show loading indicator
    const loadingIndicator = document.getElementById('loadingIndicator');
    if (loadingIndicator) {
        loadingIndicator.style.display = 'flex';
    }
    
    // Use AbortController for better request management
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 30000); // 30 second timeout
    
    // Use a large pageSize to get all courses at once
    fetch(`/Course/SearchCourses?courseSearch=${encodeURIComponent(search)}&categorySearch=${encodeURIComponent(category)}&page=1&pageSize=1000&sortBy=${sortBy}&price=${price}&difficulty=${difficulty}`, {
        signal: controller.signal
    })
        .then(res => {
            clearTimeout(timeoutId);
            if (!res.ok) {
                throw new Error(`HTTP error! status: ${res.status}`);
            }
            return res.json();
        })
        .then(data => {
            if (data.success && data.courses && data.courses.length > 0) {
                updateCoursesDisplay(data.courses);
                // Initialize virtual scrolling for large lists
                setTimeout(initVirtualScrolling, 100);
            } else {
                showNoCoursesMessage();
            }
        })
        .catch(error => {
            clearTimeout(timeoutId);
            console.error('Error loading courses:', error);
            if (error.name === 'AbortError') {
                showNoCoursesMessage('Request timeout. Please try again.');
            } else {
                showNoCoursesMessage('Failed to load courses. Please refresh the page.');
            }
        })
        .finally(() => {
            if (loadingIndicator) {
                loadingIndicator.style.display = 'none';
            }
        });
}

function updateCoursesDisplay(courses) {
    const grid = document.getElementById('coursesGrid');
    if (!grid) return;
    
    grid.innerHTML = '';
    
    // Create a document fragment for better performance
    const fragment = document.createDocumentFragment();
    
    courses.forEach((course, index) => {
        const col = document.createElement('div');
        col.className = 'col-lg-4 col-md-6 col-sm-12 mb-4';
        col.innerHTML = `
        <div class="course-card" data-course-id="${course.courseId}">
            <div class="course-image">
                <img src="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 300 200'%3E%3Crect width='300' height='200' fill='%23f8f9fa'/%3E%3Ctext x='150' y='100' text-anchor='middle' fill='%236c757d' font-family='Arial' font-size='14'%3ELoading...%3C/text%3E%3C/svg%3E" 
                     data-src="${course.coursePicture}" 
                     alt="${course.courseName}" 
                     class="lazy-image"
                     onerror="this.onerror=null; this.src='/SharedMedia/defaults/default-course.svg';">
                ${course.price > 0 ? `<div class="course-price">$${course.price.toLocaleString()}</div>` : `<div class="course-price free">Free</div>`}
            </div>
            <div class="course-details">
                <div class="course-categories">
                    ${(course.courseCategories || []).slice(0,2).map(cat => `<span class="category-badge">${cat}</span>`).join('')}
                </div>
                <h3 class="course-title">${course.courseName}</h3>
                <p class="course-description">${course.description && course.description.length > 100 ? course.description.substring(0, 100) + '...' : course.description || ''}</p>
                <div class="course-meta">
                    <div class="instructor">
                        <i class="fas fa-user"></i>
                        <span>${course.createdBy}</span>
                    </div>
                    <div class="rating">
                        <span class="rating-text">(${Number(course.starRating).toFixed(1)})</span>
                    </div>
                </div>
                <p class="course-students">${course.enrollmentCount} enrolled</p>
                <div class="course-actions">
                    <a href="/Course/Details/${course.courseId}" class="btn btn-sm btn-outline-info" title="View Course Details">
                        <i class="fas fa-eye"></i> Details
                    </a>
                    ${window.userAuth.isAuthenticated && window.userAuth.userRole === 'Learner' ? 
                        (course.price > 0 ? 
                            `<button class="btn btn-sm btn-outline-primary" onclick="enrollInCourse('${course.courseId}')" title="Purchase Course">
                                <i class="fas fa-shopping-cart"></i> Buy Now
                            </button>` : 
                            `<button class="btn btn-sm btn-outline-success" onclick="enrollInCourse('${course.courseId}')" title="Enroll for Free">
                                <i class="fas fa-play"></i> Enroll Free
                            </button>`
                        ) : ''
                    }
                    ${window.userAuth.isAuthenticated && window.userAuth.userRole === 'Instructor' ? 
                        generateInstructorButtons(course) : ''
                    }
                </div>
            </div>
        </div>
        `;
        fragment.appendChild(col);
    });
    
    // Append all elements at once for better performance
    grid.appendChild(fragment);
    
    // Initialize lazy loading for images
    initLazyLoading();
    
    // Animate course cards with staggered delay
    animateCourseCards();
}

function showNoCoursesMessage(customMessage = null) {
    const grid = document.getElementById('coursesGrid');
    if (!grid) return;
    
    const message = customMessage || 'No courses found. Try adjusting your search criteria or browse all courses.';
    
    grid.innerHTML = `
        <div class="no-courses">
            <div class="no-courses-content">
                <i class="fas fa-search fa-3x"></i>
                <h3>No courses found</h3>
                <p>${message}</p>
                <button onclick="loadAllCourses()" class="btn btn-primary">
                    <i class="fas fa-refresh"></i>
                    Show All Courses
                </button>
            </div>
        </div>
    `;
}

function generateInstructorButtons(course) {
    const approvalStatus = course.approvalStatus?.toLowerCase();
    const courseStatus = course.courseStatus;
    
    if (approvalStatus === 'draft' || courseStatus === 5) {
        return `
            <div class="d-flex gap-2">
                <a href="/Course/Edit/${course.courseId}" class="btn btn-sm btn-outline-warning" title="Edit Course">
                    <i class="fas fa-edit"></i> Edit
                </a>
                ${approvalStatus === 'draft' ? 
                    `<span class="btn btn-sm btn-info" title="Course Status">
                        <i class="fas fa-edit"></i> Draft
                    </span>` : ''
                }
                ${courseStatus === 5 ? 
                    `<span class="btn btn-sm btn-secondary" title="Course Status">
                        <i class="fas fa-pause-circle"></i> Inactive
                    </span>` : ''
                }
            </div>
        `;
    } else if (approvalStatus === 'pending') {
        return `
            <span class="btn btn-sm btn-warning" title="Course Status">
                <i class="fas fa-clock"></i> Pending
            </span>
        `;
    } else if (approvalStatus === 'denied' || approvalStatus === 'rejected') {
        return `
            <div class="d-flex gap-2">
                <a href="/Course/Edit/${course.courseId}" class="btn btn-sm btn-outline-warning" title="Edit Course">
                    <i class="fas fa-edit"></i> Edit
                </a>
                <span class="btn btn-sm btn-danger" title="Course Status">
                    <i class="fas fa-times-circle"></i> Rejected
                </span>
            </div>
        `;
    } else if (courseStatus === 4) {
        return `
            <span class="btn btn-sm btn-secondary" title="Course Status">
                <i class="fas fa-trash"></i> Deleted
            </span>
        `;
    } else if (approvalStatus === 'approved') {
        return `
            <span class="btn btn-sm btn-success" title="Course Status">
                <i class="fas fa-check-circle"></i> Approved
            </span>
        `;
    } else if (!approvalStatus) {
        return `
            <div class="d-flex gap-2">
                <a href="/Course/Edit/${course.courseId}" class="btn btn-sm btn-outline-warning" title="Edit Course">
                    <i class="fas fa-edit"></i> Edit
                </a>
                <span class="btn btn-sm btn-outline-info" title="Course Status">
                    <i class="fas fa-edit"></i> Draft
                </span>
            </div>
        `;
    } else {
        return `
            <span class="btn btn-sm btn-outline-secondary" title="Course Status">
                <i class="fas fa-question-circle"></i> ${approvalStatus || 'Unknown'}
            </span>
        `;
    }
}

// Handle filter changes with performance optimizations
document.addEventListener('DOMContentLoaded', function() {
    const searchInput = document.getElementById('courseSearchInput');
    const categoryFilter = document.getElementById('categoryFilter');
    const priceFilter = document.getElementById('priceFilter');
    const difficultyFilter = document.getElementById('difficultyFilter');
    const sortSelect = document.getElementById('sortSelect');
    const clearAllFilters = document.getElementById('clearAllFilters');
    
    // Add event listeners for all filters with throttling
    [categoryFilter, priceFilter, difficultyFilter, sortSelect].forEach(el => {
        if (el) {
            el.addEventListener('change', debounce(() => {
                loadAllCourses();
            }, 300));
        }
    });
    
    // Handle search input with debounce
    if (searchInput) {
        let searchTimeout;
        searchInput.addEventListener('input', function() {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                loadAllCourses();
            }, 500);
        });
    }
    
    // Handle clear all filters button
    if (clearAllFilters) {
        clearAllFilters.addEventListener('click', function() {
            // Reset all filters
            if (searchInput) searchInput.value = '';
            if (categoryFilter) categoryFilter.value = '';
            if (priceFilter) priceFilter.value = '';
            if (difficultyFilter) difficultyFilter.value = '';
            if (sortSelect) sortSelect.value = 'newest';
            
            loadAllCourses();
        });
    }
    
    // Optimize scroll performance
    let ticking = false;
    const header = document.querySelector('.bse-sticky-header');
    
    function updateHeaderOnScroll() {
        if (window.scrollY > 50) {
            header?.classList.add('scrolled');
        } else {
            header?.classList.remove('scrolled');
        }
        ticking = false;
    }
    
    window.addEventListener('scroll', function() {
        if (!ticking) {
            requestAnimationFrame(updateHeaderOnScroll);
            ticking = true;
        }
    });
    
    // Preload critical images
    const preloadImages = () => {
        const imageUrls = [
            '/SharedMedia/defaults/default-course.svg'
        ];
        
        imageUrls.forEach(url => {
            const img = new Image();
            img.src = url;
        });
    };
    
    // Preload images after page load
    setTimeout(preloadImages, 1000);
});

// Initialize when page loads
window.addEventListener('load', function() {
    // Load all courses on page load
    if (typeof loadAllCourses === 'function') {
        loadAllCourses();
    }
}); 