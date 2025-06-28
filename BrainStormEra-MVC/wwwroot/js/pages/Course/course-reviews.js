// Course Reviews Management
class CourseReviews {
    constructor(courseId) {
        this.courseId = courseId;
        this.editingReviewId = null;
        this.init();
    }

    init() {
        this.initializeElements();
        this.attachEventListeners();
        this.checkReviewEligibility();
    }

    initializeElements() {
        this.writeReviewBtn = document.getElementById('writeReviewBtn');
        this.createReviewSection = document.getElementById('create-review-section');
        this.createReviewForm = document.getElementById('createReviewForm');
        this.cancelReviewBtn = document.getElementById('cancelReviewBtn');
        this.submitReviewBtn = document.getElementById('submitReviewBtn');
        this.starRatingInput = document.getElementById('star-rating-input');
        this.ratingText = document.getElementById('rating-text');
        this.reviewComment = document.getElementById('reviewComment');
        this.charCount = document.getElementById('char-count');
        this.eligibilityMessage = document.getElementById('review-eligibility-message');
        
        this.selectedRating = 0;
    }

    attachEventListeners() {
        // Write Review Button
        if (this.writeReviewBtn) {
            this.writeReviewBtn.addEventListener('click', () => this.showReviewForm());
        }

        // Cancel Review Button
        if (this.cancelReviewBtn) {
            this.cancelReviewBtn.addEventListener('click', () => this.hideReviewForm());
        }

        // Submit Review Form
        if (this.createReviewForm) {
            this.createReviewForm.addEventListener('submit', (e) => this.handleSubmitReview(e));
        }

        // Star Rating Input
        if (this.starRatingInput) {
            this.initializeStarRating();
        }

        // Character Count for Comment
        if (this.reviewComment) {
            this.reviewComment.addEventListener('input', () => this.updateCharCount());
        }
    }

    initializeStarRating() {
        const stars = this.starRatingInput.querySelectorAll('input[type="radio"]');
        const labels = this.starRatingInput.querySelectorAll('.star-label');

        stars.forEach((star, index) => {
            star.addEventListener('change', () => {
                this.selectedRating = parseInt(star.value);
                this.updateStarDisplay();
                this.updateRatingText();
            });
        });

        // Hover effects
        labels.forEach((label, index) => {
            label.addEventListener('mouseenter', () => {
                this.highlightStars(5 - index);
            });
        });

        this.starRatingInput.addEventListener('mouseleave', () => {
            this.updateStarDisplay();
        });
    }

    highlightStars(rating) {
        const labels = this.starRatingInput.querySelectorAll('.star-label');
        labels.forEach((label, index) => {
            if (5 - index <= rating) {
                label.classList.add('hovered');
            } else {
                label.classList.remove('hovered');
            }
        });
    }

    updateStarDisplay() {
        const labels = this.starRatingInput.querySelectorAll('.star-label');
        labels.forEach((label, index) => {
            label.classList.remove('hovered');
            if (5 - index <= this.selectedRating) {
                label.classList.add('selected');
            } else {
                label.classList.remove('selected');
            }
        });
    }

    updateRatingText() {
        const ratingTexts = {
            1: 'Poor',
            2: 'Fair',
            3: 'Good',
            4: 'Very Good',
            5: 'Excellent'
        };
        
        if (this.selectedRating > 0) {
            this.ratingText.textContent = `${this.selectedRating} star${this.selectedRating > 1 ? 's' : ''} - ${ratingTexts[this.selectedRating]}`;
        } else {
            this.ratingText.textContent = 'Click to rate';
        }
    }

    updateCharCount() {
        const count = this.reviewComment.value.length;
        this.charCount.textContent = count;
        
        if (count > 1000) {
            this.charCount.style.color = '#dc3545';
        } else if (count > 900) {
            this.charCount.style.color = '#ffc107';
        } else {
            this.charCount.style.color = '#6c757d';
        }
    }

    async checkReviewEligibility() {
        try {
            const response = await fetch(`/Course/CheckReviewEligibility?courseId=${this.courseId}`);
            const result = await response.json();

            if (result.success) {
                if (result.canCreateReview) {
                    this.showWriteReviewButton();
                } else if (!result.isEnrolled) {
                    this.showEligibilityMessage('You must be enrolled in this course to write a review.', 'warning');
                } else if (result.hasExistingReview) {
                    this.showEligibilityMessage('You have already reviewed this course.', 'info');
                }
            }
        } catch (error) {
            console.error('Error checking review eligibility:', error);
        }
    }

    showWriteReviewButton() {
        if (this.writeReviewBtn) {
            this.writeReviewBtn.style.display = 'inline-block';
        }
        if (this.eligibilityMessage) {
            this.eligibilityMessage.style.display = 'none';
        }
    }

    hideWriteReviewButton() {
        if (this.writeReviewBtn) {
            this.writeReviewBtn.style.display = 'none';
        }
    }

    showEligibilityMessage(message, type) {
        if (this.eligibilityMessage) {
            this.eligibilityMessage.textContent = message;
            this.eligibilityMessage.className = `alert alert-${type}`;
            this.eligibilityMessage.style.display = 'block';
        }
    }

    showReviewForm(isEdit = false, reviewData = null) {
        this.createReviewSection.style.display = 'block';
        this.writeReviewBtn.style.display = 'none';
        
        if (isEdit && reviewData) {
            this.editingReviewId = reviewData.reviewId;
            this.populateFormForEdit(reviewData);
            this.updateFormForEdit();
        } else {
            this.editingReviewId = null;
            this.updateFormForCreate();
        }
        
        // Scroll to form
        this.createReviewSection.scrollIntoView({ 
            behavior: 'smooth', 
            block: 'start' 
        });
    }

    populateFormForEdit(reviewData) {
        // Set star rating
        this.selectedRating = reviewData.starRating;
        const starInput = this.starRatingInput.querySelector(`input[value="${reviewData.starRating}"]`);
        if (starInput) {
            starInput.checked = true;
        }
        this.updateStarDisplay();
        this.updateRatingText();

        // Set comment
        if (this.reviewComment) {
            this.reviewComment.value = reviewData.reviewComment || '';
            this.updateCharCount();
        }
    }

    updateFormForEdit() {
        // Update form title
        const formTitle = this.createReviewSection.querySelector('h4');
        if (formTitle) {
            formTitle.innerHTML = '<i class="fas fa-edit"></i> Edit Your Review';
        }

        // Update submit button
        if (this.submitReviewBtn) {
            this.submitReviewBtn.innerHTML = '<i class="fas fa-save"></i> Update Review';
        }
    }

    updateFormForCreate() {
        // Update form title
        const formTitle = this.createReviewSection.querySelector('h4');
        if (formTitle) {
            formTitle.innerHTML = '<i class="fas fa-star"></i> Write a Review';
        }

        // Update submit button
        if (this.submitReviewBtn) {
            this.submitReviewBtn.innerHTML = '<i class="fas fa-paper-plane"></i> Submit Review';
        }
    }

    hideReviewForm() {
        this.createReviewSection.style.display = 'none';
        this.writeReviewBtn.style.display = 'inline-block';
        this.editingReviewId = null;
        this.resetForm();
    }

    resetForm() {
        this.createReviewForm.reset();
        this.selectedRating = 0;
        this.updateStarDisplay();
        this.updateRatingText();
        this.charCount.textContent = '0';
        this.charCount.style.color = '#6c757d';
        this.updateFormForCreate();
    }

    async handleSubmitReview(e) {
        e.preventDefault();

        if (!this.validateForm()) {
            return;
        }

        this.setSubmitButtonLoading(true);

        try {
            const formData = {
                courseId: this.courseId,
                starRating: this.selectedRating,
                comment: this.reviewComment.value.trim()
            };

            let endpoint, method;
            if (this.editingReviewId) {
                // Update existing review
                endpoint = '/Course/UpdateReview';
                formData.reviewId = this.editingReviewId;
            } else {
                // Create new review
                endpoint = '/Course/CreateReview';
            }

            const response = await fetch(endpoint, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                },
                body: JSON.stringify(formData)
            });

            const result = await response.json();

            if (result.success) {
                const message = this.editingReviewId ? 'Review updated successfully!' : 'Review submitted successfully!';
                this.showSuccessMessage(message);
                this.hideReviewForm();
                
                if (!this.editingReviewId) {
                    // Hide write review button and show eligibility message for new reviews
                    this.hideWriteReviewButton();
                    this.showEligibilityMessage('You have already reviewed this course.', 'info');
                }
                
                // Refresh reviews list
                await this.refreshReviews();
            } else {
                this.showErrorMessage(result.message || 'Failed to submit review');
            }
        } catch (error) {
            this.showErrorMessage('An error occurred while submitting your review');
        } finally {
            this.setSubmitButtonLoading(false);
        }
    }

    validateForm() {
        if (this.selectedRating === 0) {
            this.showErrorMessage('Please select a rating');
            return false;
        }

        if (this.reviewComment.value.length > 1000) {
            this.showErrorMessage('Comment cannot exceed 1000 characters');
            return false;
        }

        return true;
    }

    setSubmitButtonLoading(isLoading) {
        if (isLoading) {
            this.submitReviewBtn.disabled = true;
            const loadingText = this.editingReviewId ? 'Updating...' : 'Submitting...';
            this.submitReviewBtn.innerHTML = `<i class="fas fa-spinner fa-spin"></i> ${loadingText}`;
        } else {
            this.submitReviewBtn.disabled = false;
            const buttonText = this.editingReviewId ? 'Update Review' : 'Submit Review';
            const iconClass = this.editingReviewId ? 'fas fa-save' : 'fas fa-paper-plane';
            this.submitReviewBtn.innerHTML = `<i class="${iconClass}"></i> ${buttonText}`;
        }
    }

    async refreshReviews() {
        try {
            const response = await fetch(`/Course/GetCourseReviews?courseId=${this.courseId}&page=1&pageSize=10`);
            const result = await response.json();

            if (result.success) {
                // Update reviews list
                this.updateReviewsList(result.reviews);
                // Update rating summary
                this.updateRatingSummary(result.averageRating, result.totalReviews);
            }
        } catch (error) {
            console.error('Error refreshing reviews:', error);
        }
    }

    updateReviewsList(reviews) {
        const reviewsList = document.querySelector('.reviews-list');
        if (!reviewsList) return;

        if (reviews && reviews.length > 0) {
            let reviewsHtml = '';
            reviews.forEach(review => {
                reviewsHtml += this.generateReviewHtml(review);
            });
            reviewsList.innerHTML = reviewsHtml;
        } else {
            reviewsList.innerHTML = `
                <div class="no-reviews">
                    <i class="fas fa-comment-slash"></i>
                    <h4>No Reviews Yet</h4>
                    <p>Be the first to review this course!</p>
                </div>
            `;
        }
    }

    generateReviewHtml(review) {
        const verifiedBadge = review.isVerifiedPurchase ? 
            '<span class="verified-badge"><i class="fas fa-check-circle"></i> Verified Purchase</span>' : '';

        const stars = this.generateStarsHtml(review.starRating);

        // Check if this is the current user's review
        const currentUserId = document.querySelector('meta[name="user-id"]')?.getAttribute('content');
        const isCurrentUserReview = currentUserId && review.userId === currentUserId;
        
        const actionButtons = isCurrentUserReview ? 
            `<div class="review-actions-buttons">
                <button class="btn btn-sm btn-outline-primary edit-review-btn" onclick="courseReviews.editReview('${review.reviewId}', ${review.starRating}, '${(review.reviewComment || '').replace(/'/g, "\\'")}')">
                    <i class="fas fa-edit"></i> Edit
                </button>
                <button class="btn btn-sm btn-outline-danger delete-review-btn" onclick="courseReviews.deleteReview('${review.reviewId}')">
                    <i class="fas fa-trash"></i> Delete
                </button>
            </div>` : '';

        return `
            <div class="review-item">
                <div class="reviewer-info">
                    <img src="${review.userImage}" alt="${review.userName}"
                        class="reviewer-avatar"
                        onerror="this.onerror=null; this.src='/SharedMedia/defaults/default-avatar.svg';">
                    <div class="reviewer-details">
                        <h5>${review.userName}</h5>
                        <div class="review-rating">${stars}</div>
                        <span class="review-date">${new Date(review.reviewDate).toLocaleDateString('en-US', {month: 'short', day: 'numeric', year: 'numeric'})}</span>
                        ${verifiedBadge}
                    </div>
                </div>
                <div class="review-content">
                    <p>${review.reviewComment}</p>
                    ${actionButtons}
                </div>
            </div>
        `;
    }

    generateStarsHtml(rating) {
        let starsHtml = '';
        for (let i = 1; i <= 5; i++) {
            if (i <= rating) {
                starsHtml += `
                    <span class="star-combined">
                        <i class="fas fa-star-half-alt star-left"></i>
                        <i class="fas fa-star-half-alt star-right"></i>
                    </span>
                `;
            } else {
                starsHtml += `
                    <span class="star-combined star-empty">
                        <i class="fas fa-star-half-alt star-left"></i>
                        <i class="fas fa-star-half-alt star-right"></i>
                    </span>
                `;
            }
        }
        return starsHtml;
    }

    updateRatingSummary(averageRating, totalReviews) {
        const ratingNumber = document.querySelector('.rating-number');
        const ratingCount = document.querySelector('.rating-count');

        if (ratingNumber) {
            ratingNumber.textContent = averageRating.toFixed(1);
        }

        if (ratingCount) {
            ratingCount.textContent = `(${totalReviews} review${totalReviews !== 1 ? 's' : ''})`;
        }
    }

    editReview(reviewId, starRating, reviewComment) {
        const reviewData = {
            reviewId: reviewId,
            starRating: starRating,
            reviewComment: reviewComment
        };
        
        this.showReviewForm(true, reviewData);
    }

    showSuccessMessage(message) {
        this.showToast(message, 'success');
    }

    showErrorMessage(message) {
        this.showToast(message, 'error');
    }

    showToast(message, type) {
        // Create toast notification
        const toast = document.createElement('div');
        toast.className = `toast-notification toast-${type}`;
        toast.innerHTML = `
            <div class="toast-content">
                <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'}"></i>
                <span>${message}</span>
            </div>
        `;

        document.body.appendChild(toast);

        // Show toast
        setTimeout(() => toast.classList.add('show'), 100);

        // Hide toast after 3 seconds
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => document.body.removeChild(toast), 300);
        }, 3000);
    }

    async deleteReview(reviewId) {
        if (!confirm('Are you sure you want to delete your review? This action cannot be undone.')) {
            return;
        }

        try {
            const response = await fetch('/Course/DeleteReview', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                },
                body: JSON.stringify({ reviewId: reviewId })
            });

            const result = await response.json();

            if (result.success) {
                this.showSuccessMessage('Review deleted successfully!');
                
                // Show write review button again
                this.showWriteReviewButton();
                this.hideEligibilityMessage();
                
                // Refresh reviews list
                await this.refreshReviews();
            } else {
                this.showErrorMessage(result.message || 'Failed to delete review');
            }
        } catch (error) {
            this.showErrorMessage('An error occurred while deleting your review');
        }
    }

    hideEligibilityMessage() {
        if (this.eligibilityMessage) {
            this.eligibilityMessage.style.display = 'none';
        }
    }
}