// Course Management JavaScript
class CourseManagement {
  constructor() {
    this.currentFilters = {
      search: "",
      category: "",
      status: "",
      price: "",
      difficulty: "",
      instructor: "",
      sortBy: "newest",
    };
    this.isLoading = false;
    this.init();
  }

  // Utility function to get proper image URL
  getImageUrl(
    imagePath,
    defaultImage = "/SharedMedia/defaults/default-course.svg"
  ) {
    if (!imagePath || imagePath.trim() === "") {
      return defaultImage;
    }

    // If it's already a full URL, return as is
    if (imagePath.startsWith("http://") || imagePath.startsWith("https://")) {
      return imagePath;
    }

    // If it starts with /, it's already a proper relative path
    if (imagePath.startsWith("/")) {
      return imagePath;
    }

    // Otherwise, assume it needs to be prefixed with /SharedMedia/
    return imagePath.startsWith("SharedMedia/")
      ? `/${imagePath}`
      : `/SharedMedia/${imagePath}`;
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
    const filterForm = document.querySelector(".filters-section form");
    if (filterForm) {
      filterForm.addEventListener("submit", (e) => this.handleFilterSubmit(e));
    }

    // Real-time search
    const searchInput = document.querySelector("#SearchQuery");
    if (searchInput) {
      let searchTimeout;
      searchInput.addEventListener("input", (e) => {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
          this.handleRealTimeSearch(e.target.value);
        }, 300);
      });
    }

    // Filter changes
    document
      .querySelectorAll(".filter-group select, .filter-group input")
      .forEach((element) => {
        element.addEventListener("change", (e) => this.handleFilterChange(e));
      });

    // Course action buttons
    document.querySelectorAll(".btn-approve").forEach((btn) => {
      btn.addEventListener("click", (e) => this.handleApprove(e));
    });

    document.querySelectorAll(".btn-reject").forEach((btn) => {
      btn.addEventListener("click", (e) => this.handleReject(e));
    });

    document.querySelectorAll(".btn-ban").forEach((btn) => {
      btn.addEventListener("click", (e) => this.handleBan(e));
    });

    document.querySelectorAll(".btn-details").forEach((btn) => {
      btn.addEventListener("click", (e) => this.handleViewDetails(e));
    });

    // Bulk selection
    this.initBulkSelection();
  }

  initializeFilters() {
    // Load filters from URL parameters
    const urlParams = new URLSearchParams(window.location.search);

    Object.keys(this.currentFilters).forEach((key) => {
      const value = urlParams.get(
        key === "search" ? "SearchQuery" : this.capitalizeFirst(key) + "Filter"
      );
      if (value) {
        this.currentFilters[key] = value;
        const element = document.querySelector(
          `[name="${
            key === "search"
              ? "SearchQuery"
              : this.capitalizeFirst(key) + "Filter"
          }"]`
        );
        if (element) {
          element.value = value;
        }
      }
    });
  }

  setupKeyboardShortcuts() {
    document.addEventListener("keydown", (e) => {
      // Ctrl/Cmd + K to focus search
      if ((e.ctrlKey || e.metaKey) && e.key === "k") {
        e.preventDefault();
        const searchInput = document.querySelector("#SearchQuery");
        if (searchInput) {
          searchInput.focus();
          searchInput.select();
        }
      }

      // Escape to clear search
      if (e.key === "Escape") {
        const searchInput = document.querySelector("#SearchQuery");
        if (searchInput && document.activeElement === searchInput) {
          searchInput.value = "";
          this.handleRealTimeSearch("");
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

    if (name === "SearchQuery") {
      this.currentFilters.search = value;
    } else if (name.endsWith("Filter")) {
      const filterType = name.replace("Filter", "").toLowerCase();
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

    if (this.currentFilters.search)
      params.set("SearchQuery", this.currentFilters.search);
    if (this.currentFilters.category)
      params.set("CategoryFilter", this.currentFilters.category);
    if (this.currentFilters.status)
      params.set("StatusFilter", this.currentFilters.status);
    if (this.currentFilters.price)
      params.set("PriceFilter", this.currentFilters.price);
    if (this.currentFilters.difficulty)
      params.set("DifficultyFilter", this.currentFilters.difficulty);
    if (this.currentFilters.instructor)
      params.set("InstructorFilter", this.currentFilters.instructor);
    if (this.currentFilters.sortBy)
      params.set("SortBy", this.currentFilters.sortBy);

    params.set("CurrentPage", "1"); // Reset to first page

    // Navigate to filtered URL
    window.location.href = `${window.location.pathname}?${params.toString()}`;
  }

  async handleApprove(e) {
    const button = e.target.closest(".btn-approve");
    const courseId = button.dataset.courseId;
    const courseName = button.dataset.courseName;

    // Show enhanced confirmation with course details
    const confirmed = await this.showApprovalConfirmModal(
      courseId,
      courseName,
      "approve"
    );
    if (!confirmed) {
      return;
    }

    await this.updateCourseStatus(
      courseId,
      true,
      "Course approved successfully"
    );
  }

  async handleReject(e) {
    const button = e.target.closest(".btn-reject");
    const courseId = button.dataset.courseId;
    const courseName = button.dataset.courseName;

    // Show rejection modal with reason input
    const result = await this.showRejectModal(courseId, courseName);
    if (!result.confirmed) {
      return;
    }

    await this.rejectCourse(
      courseId,
      result.reason,
      "Course rejected successfully"
    );
  }

  async handleBan(e) {
    const button = e.target.closest(".btn-ban");
    const courseId = button.dataset.courseId;
    const courseName = button.dataset.courseName;

    // Show ban modal with reason input
    const result = await this.showBanModal(courseId, courseName);
    if (!result.confirmed) {
      return;
    }

    await this.banCourse(courseId, result.reason, "Course banned successfully");
  }

  async handleViewDetails(e) {
    const button = e.target.closest(".btn-details");
    const courseId = button.dataset.courseId;

    await this.showCourseDetails(courseId);
  }

  async confirmAction(action, courseName, isDestructive = false) {
    const messages = {
      approve: `Are you sure you want to approve the course "${courseName}"?`,
      reject: `Are you sure you want to reject the course "${courseName}"?`,
      ban: `Are you sure you want to ban the course "${courseName}"? This will make it unavailable to all users.`,
    };

    const result = await this.showConfirmModal(messages[action], isDestructive);
    return result;
  }

  async updateCourseStatus(courseId, isApproved, successMessage) {
    try {
      this.showLoading();

      // Create FormData for Razor Pages
      const formData = new FormData();
      formData.append("courseId", courseId);
      formData.append("isApproved", isApproved);
      formData.append("__RequestVerificationToken", this.getAntiForgeryToken());

      const response = await fetch(
        "/admin/courses?handler=UpdateCourseStatus",
        {
          method: "POST",
          body: formData,
        }
      );

      const data = await response.json();

      if (data.success) {
        this.showToast(successMessage, "success");
        setTimeout(() => {
          window.location.reload();
        }, 1000);
      } else {
        this.showToast(
          data.message || "Failed to update course status",
          "error"
        );
      }
    } catch (error) {
      this.showToast("An error occurred while updating course status", "error");
    } finally {
      this.hideLoading();
    }
  }

  async banCourse(courseId, reason, successMessage) {
    try {
      this.showLoading();

      // Create FormData for Razor Pages
      const formData = new FormData();
      formData.append("courseId", courseId);
      formData.append("reason", reason);
      formData.append("__RequestVerificationToken", this.getAntiForgeryToken());

      const response = await fetch("/admin/courses?handler=BanCourse", {
        method: "POST",
        body: formData,
      });

      const data = await response.json();

      if (data.success) {
        this.showToast(successMessage, "success");
        setTimeout(() => {
          window.location.reload();
        }, 1000);
      } else {
        this.showToast(data.message || "Failed to ban course", "error");
      }
    } catch (error) {
      this.showToast("An error occurred while banning course", "error");
    } finally {
      this.hideLoading();
    }
  }

  async rejectCourse(courseId, reason, successMessage) {
    try {
      this.showLoading();

      // Create FormData for Razor Pages
      const formData = new FormData();
      formData.append("courseId", courseId);
      formData.append("reason", reason);
      formData.append("__RequestVerificationToken", this.getAntiForgeryToken());

      const response = await fetch("/admin/courses?handler=RejectCourse", {
        method: "POST",
        body: formData,
      });

      const data = await response.json();

      if (data.success) {
        this.showToast(successMessage, "success");
        setTimeout(() => {
          window.location.reload();
        }, 1000);
      } else {
        this.showToast(data.message || "Failed to reject course", "error");
      }
    } catch (error) {
      this.showToast("An error occurred while rejecting course", "error");
    } finally {
      this.hideLoading();
    }
  }

  async showCourseDetails(courseId) {
    try {
      this.showLoading();

      const response = await fetch(
        `/admin/courses?handler=CourseDetails&courseId=${courseId}`
      );
      const result = await response.json();

      if (result.success && result.data) {
        this.displayCourseDetailsModal(result.data);
      } else {
        this.showToast("Failed to load course details", "error");
      }
    } catch (error) {
      this.showToast("An error occurred while loading course details", "error");
    } finally {
      this.hideLoading();
    }
  }

  displayCourseDetailsModal(courseData) {
    // Create modal HTML
    const modalHtml = this.createCourseDetailsModalHtml(courseData);

    // Remove existing modal if any
    const existingModal = document.getElementById("courseDetailsModal");
    if (existingModal) {
      existingModal.remove();
    }

    // Add modal to DOM
    document.body.insertAdjacentHTML("beforeend", modalHtml);

    // Show modal
    const modal = new bootstrap.Modal(
      document.getElementById("courseDetailsModal")
    );
    modal.show();

    // Clean up when modal is hidden
    document
      .getElementById("courseDetailsModal")
      .addEventListener("hidden.bs.modal", function () {
        this.remove();
      });
  }

  createCourseDetailsModalHtml(course) {
    return `
            <div class="modal fade course-details-modal" id="courseDetailsModal" tabindex="-1" aria-labelledby="courseDetailsModalLabel" aria-hidden="true">
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
                                <img src="${this.getImageUrl(
                                  course.coursePicture || course.CoursePicture
                                )}" alt="${
      course.courseName || course.CourseName
    }"
                                     onerror="this.onerror=null; this.src='/SharedMedia/defaults/default-course.svg';" 
                                     class="course-details-image">
                                <div class="course-details-info">
                                    <h2 class="course-details-title">${
                                      course.courseName || course.CourseName
                                    }</h2>
                                    <div class="course-details-instructor">
                                        <i class="fas fa-user"></i> By ${
                                          this.getProperty(
                                            course,
                                            "instructorName"
                                          ) ||
                                          this.getProperty(
                                            course,
                                            "InstructorName"
                                          ) ||
                                          "Unknown Instructor"
                                        }
                                    </div>
                                    <div class="course-details-stats">
                                        <div class="stat-item">
                                            <span class="stat-item-value">${
                                              this.getProperty(
                                                course,
                                                "priceText"
                                              ) ||
                                              this.getProperty(
                                                course,
                                                "PriceText"
                                              ) ||
                                              "Price not available"
                                            }</span>
                                            <span class="stat-item-label">Price</span>
                                        </div>
                                        <div class="stat-item">
                                            <span class="stat-item-value">${
                                              this.getProperty(
                                                course,
                                                "enrollmentCount"
                                              ) ||
                                              this.getProperty(
                                                course,
                                                "EnrollmentCount"
                                              ) ||
                                              "Enrollment count not available"
                                            }</span>
                                            <span class="stat-item-label">Students</span>
                                        </div>
                                        <div class="stat-item">
                                            <span class="stat-item-value">${
                                              (
                                                this.getProperty(
                                                  course,
                                                  "averageRating"
                                                ) ||
                                                this.getProperty(
                                                  course,
                                                  "AverageRating"
                                                ) ||
                                                0
                                              ).toFixed
                                                ? (
                                                    this.getProperty(
                                                      course,
                                                      "averageRating"
                                                    ) ||
                                                    this.getProperty(
                                                      course,
                                                      "AverageRating"
                                                    )
                                                  ).toFixed(1)
                                                : this.getProperty(
                                                    course,
                                                    "averageRating"
                                                  ) ||
                                                  this.getProperty(
                                                    course,
                                                    "AverageRating"
                                                  ) ||
                                                  0
                                            }</span>
                                            <span class="stat-item-label">Rating</span>
                                        </div>
                                        <div class="stat-item">
                                            <span class="stat-item-value">${
                                              this.getProperty(
                                                course,
                                                "totalLessons"
                                              ) ||
                                              this.getProperty(
                                                course,
                                                "TotalLessons"
                                              ) ||
                                              "Lesson count not available"
                                            }</span>
                                            <span class="stat-item-label">Lessons</span>
                                        </div>
                                        <div class="stat-item">
                                            <span class="stat-item-value">${
                                              this.getProperty(
                                                course,
                                                "durationText"
                                              ) ||
                                              this.getProperty(
                                                course,
                                                "DurationText"
                                              ) ||
                                              "Duration not available"
                                            }</span>
                                            <span class="stat-item-label">Duration</span>
                                        </div>
                                        <div class="stat-item">
                                            <span class="stat-item-value"><span class="status-badge ${
                                              this.getProperty(
                                                course,
                                                "statusBadgeClass"
                                              ) ||
                                              this.getProperty(
                                                course,
                                                "StatusBadgeClass"
                                              )
                                            }">${
      this.getProperty(course, "statusText") ||
      this.getProperty(course, "StatusText") ||
      "Status not available"
    }</span></span>
                                            <span class="stat-item-label">Status</span>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div class="course-details-section">
                                <h4 class="section-title"><i class="fas fa-align-left"></i> Description</h4>
                                <div class="course-description">${
                                  this.getProperty(
                                    course,
                                    "courseDescription"
                                  ) ||
                                  this.getProperty(
                                    course,
                                    "CourseDescription"
                                  ) ||
                                  "No description provided."
                                }</div>
                            </div>

                            <div class="course-details-section">
                                <h4 class="section-title"><i class="fas fa-info-circle"></i> Course Information</h4>
                                <div class="row">
                                    <div class="col-md-6">
                                        <p><strong>Difficulty:</strong> ${
                                          this.getProperty(
                                            course,
                                            "difficultyText"
                                          ) ||
                                          this.getProperty(
                                            course,
                                            "DifficultyText"
                                          ) ||
                                          "Difficulty not available"
                                        }</p>
                                        <p><strong>Categories:</strong> ${
                                          this.getProperty(
                                            course,
                                            "categoriesText"
                                          ) ||
                                          this.getProperty(
                                            course,
                                            "CategoriesText"
                                          ) ||
                                          "Categories not available"
                                        }</p>
                                        <p><strong>Created:</strong> ${new Date(
                                          this.getProperty(
                                            course,
                                            "createdAt"
                                          ) ||
                                            this.getProperty(
                                              course,
                                              "CreatedAt"
                                            )
                                        ).toLocaleDateString()}</p>
                                    </div>
                                    <div class="col-md-6">
                                        <p><strong>Sequential Access:</strong> ${
                                          this.getProperty(
                                            course,
                                            "enforceSequentialAccess"
                                          ) ||
                                          this.getProperty(
                                            course,
                                            "EnforceSequentialAccess"
                                          )
                                            ? "Yes"
                                            : "No"
                                        }</p>
                                        <p><strong>Preview Allowed:</strong> ${
                                          this.getProperty(
                                            course,
                                            "allowLessonPreview"
                                          ) ||
                                          this.getProperty(
                                            course,
                                            "AllowLessonPreview"
                                          )
                                            ? "Yes"
                                            : "No"
                                        }</p>
                                        <p><strong>Featured:</strong> ${
                                          this.getProperty(
                                            course,
                                            "isFeatured"
                                          ) ||
                                          this.getProperty(course, "IsFeatured")
                                            ? "Yes"
                                            : "No"
                                        }</p>
                                    </div>
                                </div>
                            </div>

                            ${
                              (this.getProperty(course, "chapters") ||
                                this.getProperty(course, "Chapters")) &&
                              (
                                this.getProperty(course, "chapters") ||
                                this.getProperty(course, "Chapters")
                              ).length > 0
                                ? `
                            <div class="course-details-section">
                                <h4 class="section-title"><i class="fas fa-list"></i> Course Structure</h4>
                                <div class="chapters-list">
                                    ${(
                                      this.getProperty(course, "chapters") ||
                                      this.getProperty(course, "Chapters")
                                    )
                                      .map(
                                        (chapter) => `
                                        <div class="chapter-item">
                                            <div class="chapter-info">
                                                <div class="chapter-order">${
                                                  this.getProperty(
                                                    chapter,
                                                    "chapterOrder"
                                                  ) ||
                                                  this.getProperty(
                                                    chapter,
                                                    "ChapterOrder"
                                                  )
                                                }</div>
                                                <div>
                                                    <div class="chapter-name">${
                                                      this.getProperty(
                                                        chapter,
                                                        "chapterName"
                                                      ) ||
                                                      this.getProperty(
                                                        chapter,
                                                        "ChapterName"
                                                      )
                                                    }</div>
                                                    <div class="chapter-stats">${
                                                      this.getProperty(
                                                        chapter,
                                                        "lessonCount"
                                                      ) ||
                                                      this.getProperty(
                                                        chapter,
                                                        "LessonCount"
                                                      )
                                                    } lesson(s)</div>
                                                </div>
                                            </div>
                                            ${
                                              this.getProperty(
                                                chapter,
                                                "isLocked"
                                              ) ||
                                              this.getProperty(
                                                chapter,
                                                "IsLocked"
                                              )
                                                ? '<i class="fas fa-lock text-warning"></i>'
                                                : '<i class="fas fa-unlock text-success"></i>'
                                            }
                                        </div>
                                    `
                                      )
                                      .join("")}
                                </div>
                            </div>
                            `
                                : ""
                            }

                            ${
                              (this.getProperty(course, "recentReviews") ||
                                this.getProperty(course, "RecentReviews")) &&
                              (
                                this.getProperty(course, "recentReviews") ||
                                this.getProperty(course, "RecentReviews")
                              ).length > 0
                                ? `
                            <div class="course-details-section">
                                <h4 class="section-title"><i class="fas fa-star"></i> Recent Reviews</h4>
                                <div class="reviews-list">
                                    ${(
                                      this.getProperty(
                                        course,
                                        "recentReviews"
                                      ) ||
                                      this.getProperty(course, "RecentReviews")
                                    )
                                      .map(
                                        (review) => `
                                        <div class="review-item">
                                            <div class="review-header">
                                                <div class="review-user">
                                                    <img src="${this.getImageUrl(
                                                      this.getProperty(
                                                        review,
                                                        "userImage"
                                                      ) ||
                                                        this.getProperty(
                                                          review,
                                                          "UserImage"
                                                        ),
                                                      "/SharedMedia/defaults/default-avatar.svg"
                                                    )}" 
                                                         alt="${
                                                           this.getProperty(
                                                             review,
                                                             "userName"
                                                           ) ||
                                                           this.getProperty(
                                                             review,
                                                             "UserName"
                                                           )
                                                         }" 
                                                         onerror="this.onerror=null; this.src='/SharedMedia/defaults/default-avatar.svg';" 
                                                         class="review-avatar">
                                                    <span>${
                                                      this.getProperty(
                                                        review,
                                                        "userName"
                                                      ) ||
                                                      this.getProperty(
                                                        review,
                                                        "UserName"
                                                      )
                                                    }</span>
                                                </div>
                                                <div class="review-rating">
                                                    ${"★".repeat(
                                                      Math.floor(
                                                        this.getProperty(
                                                          review,
                                                          "rating"
                                                        ) ||
                                                          this.getProperty(
                                                            review,
                                                            "Rating"
                                                          )
                                                      )
                                                    )}${"☆".repeat(
                                          5 -
                                            Math.floor(
                                              this.getProperty(
                                                review,
                                                "rating"
                                              ) ||
                                                this.getProperty(
                                                  review,
                                                  "Rating"
                                                )
                                            )
                                        )} ${
                                          this.getProperty(review, "rating") ||
                                          this.getProperty(review, "Rating")
                                        }
                                                </div>
                                            </div>
                                            <div class="review-comment">${
                                              this.getProperty(
                                                review,
                                                "comment"
                                              ) ||
                                              this.getProperty(
                                                review,
                                                "Comment"
                                              ) ||
                                              "No comment provided"
                                            }</div>
                                        </div>
                                    `
                                      )
                                      .join("")}
                                </div>
                            </div>
                            `
                                : ""
                            }
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
                            <div class="modal-header ${
                              isDestructive
                                ? "bg-danger text-white"
                                : "bg-primary text-white"
                            }">
                                <h5 class="modal-title">
                                    <i class="fas ${
                                      isDestructive
                                        ? "fa-exclamation-triangle"
                                        : "fa-question-circle"
                                    }"></i>
                                    Confirm Action
                                </h5>
                            </div>
                            <div class="modal-body">
                                <p>${message}</p>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                                <button type="button" class="btn ${
                                  isDestructive ? "btn-danger" : "btn-primary"
                                }" id="confirmButton">
                                    ${isDestructive ? "Delete" : "Confirm"}
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `;

      document.body.insertAdjacentHTML("beforeend", modalHtml);
      const modal = new bootstrap.Modal(
        document.getElementById("confirmModal")
      );

      document.getElementById("confirmButton").addEventListener("click", () => {
        modal.hide();
        resolve(true);
      });

      document
        .getElementById("confirmModal")
        .addEventListener("hidden.bs.modal", function () {
          this.remove();
          resolve(false);
        });

      modal.show();
    });
  }

  showLoading() {
    if (document.getElementById("loadingOverlay")) return;

    const loadingHtml = `
            <div id="loadingOverlay" class="loading-overlay">
                <div class="loading-spinner"></div>
            </div>
        `;
    document.body.insertAdjacentHTML("beforeend", loadingHtml);
    this.isLoading = true;
  }

  hideLoading() {
    const loadingOverlay = document.getElementById("loadingOverlay");
    if (loadingOverlay) {
      loadingOverlay.remove();
    }
    this.isLoading = false;
  }

  showToast(message, type = "info") {
    // Remove existing toast
    const existingToast = document.getElementById("courseToast");
    if (existingToast) {
      existingToast.remove();
    }

    const toastClass =
      {
        success: "bg-success",
        error: "bg-danger",
        warning: "bg-warning",
        info: "bg-info",
      }[type] || "bg-info";

    const icon =
      {
        success: "fa-check-circle",
        error: "fa-exclamation-circle",
        warning: "fa-exclamation-triangle",
        info: "fa-info-circle",
      }[type] || "fa-info-circle";

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

    document.body.insertAdjacentHTML("beforeend", toastHtml);

    // Auto-hide after 5 seconds
    setTimeout(() => {
      const toast = document.getElementById("courseToast");
      if (toast) {
        toast.remove();
      }
    }, 5000);
  }

  getAntiForgeryToken() {
    const token = document.querySelector(
      'input[name="__RequestVerificationToken"]'
    );
    return token ? token.value : "";
  }

  capitalizeFirst(str) {
    return str.charAt(0).toUpperCase() + str.slice(1);
  }

  // Enhanced approval modal with course details preview
  async showApprovalConfirmModal(courseId, courseName, action) {
    return new Promise((resolve) => {
      const modalHtml = `
                <div class="modal fade" id="approvalConfirmModal" tabindex="-1" aria-hidden="true">
                    <div class="modal-dialog modal-lg">
                        <div class="modal-content">
                            <div class="modal-header bg-success text-white">
                                <h5 class="modal-title">
                                    <i class="fas fa-check-circle"></i> Approve Course
                                </h5>
                                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                            </div>
                            <div class="modal-body">
                                <div class="approval-info">
                                    <h6>You are about to approve:</h6>
                                    <div class="course-info-box">
                                        <h5 class="text-primary">${courseName}</h5>
                                        <p class="text-muted">Course ID: ${courseId}</p>
                                    </div>
                                    <div class="alert alert-info">
                                        <i class="fas fa-info-circle"></i>
                                        <strong>What happens when you approve this course:</strong>
                                        <ul class="mt-2 mb-0">
                                            <li>The course will be publicly available</li>
                                            <li>Students can enroll and access content</li>
                                            <li>The instructor will be notified</li>
                                        </ul>
                                    </div>
                                </div>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
                                    <i class="fas fa-times"></i> Cancel
                                </button>
                                <button type="button" class="btn btn-success" id="confirmApprovalBtn">
                                    <i class="fas fa-check"></i> Approve Course
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `;

      // Remove existing modal if any
      const existingModal = document.getElementById("approvalConfirmModal");
      if (existingModal) existingModal.remove();

      // Add modal to DOM
      document.body.insertAdjacentHTML("beforeend", modalHtml);

      // Show modal
      const modal = new bootstrap.Modal(
        document.getElementById("approvalConfirmModal")
      );
      modal.show();

      // Handle buttons
      document
        .getElementById("confirmApprovalBtn")
        .addEventListener("click", () => {
          modal.hide();
          resolve(true);
        });

      document
        .getElementById("approvalConfirmModal")
        .addEventListener("hidden.bs.modal", () => {
          document.getElementById("approvalConfirmModal").remove();
          resolve(false);
        });
    });
  }

  // Enhanced reject modal with reason input
  async showRejectModal(courseId, courseName) {
    return new Promise((resolve) => {
      const modalHtml = `
                <div class="modal fade" id="rejectModal" tabindex="-1" aria-hidden="true">
                    <div class="modal-dialog modal-lg">
                        <div class="modal-content">
                            <div class="modal-header bg-warning text-dark">
                                <h5 class="modal-title">
                                    <i class="fas fa-times-circle"></i> Reject Course
                                </h5>
                                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                            </div>
                            <div class="modal-body">
                                <div class="reject-info">
                                    <h6>You are about to reject:</h6>
                                    <div class="course-info-box">
                                        <h5 class="text-primary">${courseName}</h5>
                                        <p class="text-muted">Course ID: ${courseId}</p>
                                    </div>
                                    
                                    <div class="alert alert-warning">
                                        <i class="fas fa-exclamation-triangle"></i>
                                        <strong>Please provide a reason for rejection:</strong>
                                        <p class="mb-0 mt-1">This will help the instructor understand what needs to be improved.</p>
                                    </div>
                                    
                                    <div class="form-group mt-3">
                                        <label for="rejectReason" class="form-label">Reason for rejection <span class="text-danger">*</span></label>
                                        <textarea class="form-control" id="rejectReason" rows="4" 
                                                  placeholder="Please explain why this course is being rejected..."></textarea>
                                        <div class="form-text">Common reasons: Content quality issues, inappropriate content, missing information, etc.</div>
                                    </div>
                                </div>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
                                    <i class="fas fa-times"></i> Cancel
                                </button>
                                <button type="button" class="btn btn-warning" id="confirmRejectBtn">
                                    <i class="fas fa-times-circle"></i> Reject Course
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `;

      // Remove existing modal if any
      const existingModal = document.getElementById("rejectModal");
      if (existingModal) existingModal.remove();

      // Add modal to DOM
      document.body.insertAdjacentHTML("beforeend", modalHtml);

      // Show modal
      const modal = new bootstrap.Modal(document.getElementById("rejectModal"));
      modal.show();

      // Handle buttons
      document
        .getElementById("confirmRejectBtn")
        .addEventListener("click", () => {
          const reason = document.getElementById("rejectReason").value.trim();
          if (!reason) {
            document.getElementById("rejectReason").classList.add("is-invalid");
            return;
          }
          modal.hide();
          resolve({ confirmed: true, reason });
        });

      document
        .getElementById("rejectModal")
        .addEventListener("hidden.bs.modal", () => {
          document.getElementById("rejectModal").remove();
          resolve({ confirmed: false });
        });

      // Real-time validation
      document
        .getElementById("rejectReason")
        .addEventListener("input", function () {
          this.classList.remove("is-invalid");
        });
    });
  }

  // Enhanced ban modal with reason input
  async showBanModal(courseId, courseName) {
    return new Promise((resolve) => {
      const modalHtml = `
                <div class="modal fade" id="banModal" tabindex="-1" aria-hidden="true">
                    <div class="modal-dialog modal-lg">
                        <div class="modal-content">
                            <div class="modal-header bg-danger text-white">
                                <h5 class="modal-title">
                                    <i class="fas fa-ban"></i> Ban Course
                                </h5>
                                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                            </div>
                            <div class="modal-body">
                                <div class="ban-info">
                                    <h6>You are about to ban:</h6>
                                    <div class="course-info-box">
                                        <h5 class="text-primary">${courseName}</h5>
                                        <p class="text-muted">Course ID: ${courseId}</p>
                                    </div>
                                    
                                    <div class="alert alert-danger">
                                        <i class="fas fa-exclamation-triangle"></i>
                                        <strong>Warning: This is a serious action!</strong>
                                        <ul class="mt-2 mb-0">
                                            <li>The course will be completely removed from public access</li>
                                            <li>Current students will lose access immediately</li>
                                            <li>The instructor will be notified</li>
                                            <li>This action may affect instructor reputation</li>
                                        </ul>
                                    </div>
                                    
                                    <div class="form-group mt-3">
                                        <label for="banReason" class="form-label">Reason for banning <span class="text-danger">*</span></label>
                                        <textarea class="form-control" id="banReason" rows="4" 
                                                  placeholder="Please explain why this course is being banned..."></textarea>
                                        <div class="form-text">Common reasons: Policy violations, inappropriate content, copyright issues, etc.</div>
                                    </div>
                                </div>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
                                    <i class="fas fa-times"></i> Cancel
                                </button>
                                <button type="button" class="btn btn-danger" id="confirmBanBtn">
                                    <i class="fas fa-ban"></i> Ban Course
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `;

      // Remove existing modal if any
      const existingModal = document.getElementById("banModal");
      if (existingModal) existingModal.remove();

      // Add modal to DOM
      document.body.insertAdjacentHTML("beforeend", modalHtml);

      // Show modal
      const modal = new bootstrap.Modal(document.getElementById("banModal"));
      modal.show();

      // Handle buttons
      document.getElementById("confirmBanBtn").addEventListener("click", () => {
        const reason = document.getElementById("banReason").value.trim();
        if (!reason) {
          document.getElementById("banReason").classList.add("is-invalid");
          return;
        }
        modal.hide();
        resolve({ confirmed: true, reason });
      });

      document
        .getElementById("banModal")
        .addEventListener("hidden.bs.modal", () => {
          document.getElementById("banModal").remove();
          resolve({ confirmed: false });
        });

      // Real-time validation
      document
        .getElementById("banReason")
        .addEventListener("input", function () {
          this.classList.remove("is-invalid");
        });
    });
  }

  // Bulk Selection Methods
  initBulkSelection() {
    const selectAllCheckbox = document.getElementById("selectAllCourses");
    const courseCheckboxes = document.querySelectorAll(".course-select");
    const bulkActionsBar = document.getElementById("bulkActionsBar");
    const selectedCountSpan = document.getElementById("selectedCount");

    // Select all functionality
    if (selectAllCheckbox) {
      selectAllCheckbox.addEventListener("change", (e) => {
        const isChecked = e.target.checked;
        courseCheckboxes.forEach((checkbox) => {
          checkbox.checked = isChecked;
          this.toggleCourseSelection(checkbox, isChecked);
        });
        this.updateBulkActionsVisibility();
      });
    }

    // Individual course selection
    courseCheckboxes.forEach((checkbox) => {
      checkbox.addEventListener("change", (e) => {
        this.toggleCourseSelection(e.target, e.target.checked);
        this.updateBulkActionsVisibility();
        this.updateSelectAllState();
      });
    });

    // Bulk action buttons
    document
      .getElementById("bulkApproveBtn")
      ?.addEventListener("click", () => this.handleBulkApprove());
    document
      .getElementById("bulkRejectBtn")
      ?.addEventListener("click", () => this.handleBulkReject());
    document
      .getElementById("bulkBanBtn")
      ?.addEventListener("click", () => this.handleBulkBan());
    document
      .getElementById("clearSelectionBtn")
      ?.addEventListener("click", () => this.clearAllSelections());
  }

  toggleCourseSelection(checkbox, isSelected) {
    const courseCard = checkbox.closest(".course-card");
    if (courseCard) {
      courseCard.classList.toggle("selected", isSelected);
    }
  }

  updateBulkActionsVisibility() {
    const selectedCheckboxes = document.querySelectorAll(
      ".course-select:checked"
    );
    const bulkActionsBar = document.getElementById("bulkActionsBar");
    const selectedCountSpan = document.getElementById("selectedCount");

    if (selectedCheckboxes.length > 0) {
      bulkActionsBar.style.display = "block";
      selectedCountSpan.textContent = selectedCheckboxes.length;
    } else {
      bulkActionsBar.style.display = "none";
    }
  }

  updateSelectAllState() {
    const selectAllCheckbox = document.getElementById("selectAllCourses");
    const courseCheckboxes = document.querySelectorAll(".course-select");
    const checkedCheckboxes = document.querySelectorAll(
      ".course-select:checked"
    );

    if (selectAllCheckbox) {
      if (checkedCheckboxes.length === 0) {
        selectAllCheckbox.checked = false;
        selectAllCheckbox.indeterminate = false;
      } else if (checkedCheckboxes.length === courseCheckboxes.length) {
        selectAllCheckbox.checked = true;
        selectAllCheckbox.indeterminate = false;
      } else {
        selectAllCheckbox.checked = false;
        selectAllCheckbox.indeterminate = true;
      }
    }
  }

  clearAllSelections() {
    const courseCheckboxes = document.querySelectorAll(".course-select");
    const selectAllCheckbox = document.getElementById("selectAllCourses");

    courseCheckboxes.forEach((checkbox) => {
      checkbox.checked = false;
      this.toggleCourseSelection(checkbox, false);
    });

    if (selectAllCheckbox) {
      selectAllCheckbox.checked = false;
      selectAllCheckbox.indeterminate = false;
    }

    this.updateBulkActionsVisibility();
  }

  getSelectedCourses() {
    const selectedCheckboxes = document.querySelectorAll(
      ".course-select:checked"
    );
    return Array.from(selectedCheckboxes).map((checkbox) => ({
      id: checkbox.value,
      name: checkbox.dataset.courseName,
      status: checkbox.dataset.status,
    }));
  }

  // Bulk Operations
  async handleBulkApprove() {
    const selectedCourses = this.getSelectedCourses();
    const pendingCourses = selectedCourses.filter(
      (course) => course.status === "pending"
    );

    if (pendingCourses.length === 0) {
      this.showToast("No pending courses selected for approval", "warning");
      return;
    }

    const confirmed = await this.showBulkConfirmModal(
      "Approve Selected Courses",
      `You are about to approve ${pendingCourses.length} course(s). This will make them publicly available.`,
      "success"
    );

    if (!confirmed) return;

    await this.processBulkOperation(
      pendingCourses,
      "approve",
      "Courses approved successfully"
    );
  }

  async handleBulkReject() {
    const selectedCourses = this.getSelectedCourses();
    const pendingCourses = selectedCourses.filter(
      (course) => course.status === "pending"
    );

    if (pendingCourses.length === 0) {
      this.showToast("No pending courses selected for rejection", "warning");
      return;
    }

    const result = await this.showBulkRejectModal(pendingCourses);
    if (!result.confirmed) return;

    await this.processBulkOperation(
      pendingCourses,
      "reject",
      "Courses rejected successfully",
      result.reason
    );
  }

  async handleBulkBan() {
    const selectedCourses = this.getSelectedCourses();

    if (selectedCourses.length === 0) {
      this.showToast("No courses selected for banning", "warning");
      return;
    }

    const result = await this.showBulkBanModal(selectedCourses);
    if (!result.confirmed) return;

    await this.processBulkOperation(
      selectedCourses,
      "ban",
      "Courses banned successfully",
      result.reason
    );
  }

  async processBulkOperation(
    courses,
    operation,
    successMessage,
    reason = null
  ) {
    this.showLoading();
    let successCount = 0;
    let failCount = 0;

    for (const course of courses) {
      try {
        let result = false;

        switch (operation) {
          case "approve":
            result = await this.approveSingleCourse(course.id);
            break;
          case "reject":
            result = await this.rejectSingleCourse(course.id, reason);
            break;
          case "ban":
            result = await this.banSingleCourse(course.id, reason);
            break;
        }

        if (result) {
          successCount++;
        } else {
          failCount++;
        }
      } catch (error) {
        failCount++;
      }
    }

    this.hideLoading();

    if (successCount > 0) {
      this.showToast(`${successMessage}: ${successCount} course(s)`, "success");
    }

    if (failCount > 0) {
      this.showToast(`Failed to process ${failCount} course(s)`, "error");
    }

    // Reload page after bulk operation
    setTimeout(() => {
      window.location.reload();
    }, 1500);
  }

  // Single operation helpers for bulk processing
  async approveSingleCourse(courseId) {
    try {
      // Create FormData for Razor Pages
      const formData = new FormData();
      formData.append("courseId", courseId);
      formData.append("isApproved", "true");
      formData.append("__RequestVerificationToken", this.getAntiForgeryToken());

      const response = await fetch(
        "/admin/courses?handler=UpdateCourseStatus",
        {
          method: "POST",
          body: formData,
        }
      );
      const data = await response.json();
      return data.success;
    } catch (error) {
      return false;
    }
  }

  async rejectSingleCourse(courseId, reason) {
    try {
      // Create FormData for Razor Pages
      const formData = new FormData();
      formData.append("courseId", courseId);
      formData.append("reason", reason);
      formData.append("__RequestVerificationToken", this.getAntiForgeryToken());

      const response = await fetch("/admin/courses?handler=RejectCourse", {
        method: "POST",
        body: formData,
      });
      const data = await response.json();
      return data.success;
    } catch (error) {
      return false;
    }
  }

  async banSingleCourse(courseId, reason) {
    try {
      // Create FormData for Razor Pages
      const formData = new FormData();
      formData.append("courseId", courseId);
      formData.append("reason", reason);
      formData.append("__RequestVerificationToken", this.getAntiForgeryToken());

      const response = await fetch("/admin/courses?handler=BanCourse", {
        method: "POST",
        body: formData,
      });
      const data = await response.json();
      return data.success;
    } catch (error) {
      return false;
    }
  }

  // Bulk Modal Methods
  async showBulkConfirmModal(title, message, type = "info") {
    return new Promise((resolve) => {
      const modalHtml = `
                <div class="modal fade" id="bulkConfirmModal" tabindex="-1" aria-hidden="true">
                    <div class="modal-dialog">
                        <div class="modal-content">
                            <div class="modal-header bg-${
                              type === "success"
                                ? "success"
                                : type === "warning"
                                ? "warning"
                                : "info"
                            } text-white">
                                <h5 class="modal-title">
                                    <i class="fas fa-${
                                      type === "success"
                                        ? "check-circle"
                                        : type === "warning"
                                        ? "exclamation-triangle"
                                        : "info-circle"
                                    }"></i>
                                    ${title}
                                </h5>
                                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                            </div>
                            <div class="modal-body">
                                <p>${message}</p>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                                <button type="button" class="btn btn-${
                                  type === "success"
                                    ? "success"
                                    : type === "warning"
                                    ? "warning"
                                    : "primary"
                                }" id="confirmBulkBtn">
                                    Confirm
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `;

      // Remove existing modal if any
      const existingModal = document.getElementById("bulkConfirmModal");
      if (existingModal) existingModal.remove();

      // Add modal to DOM
      document.body.insertAdjacentHTML("beforeend", modalHtml);

      // Show modal
      const modal = new bootstrap.Modal(
        document.getElementById("bulkConfirmModal")
      );
      modal.show();

      // Handle buttons
      document
        .getElementById("confirmBulkBtn")
        .addEventListener("click", () => {
          modal.hide();
          resolve(true);
        });

      document
        .getElementById("bulkConfirmModal")
        .addEventListener("hidden.bs.modal", () => {
          document.getElementById("bulkConfirmModal").remove();
          resolve(false);
        });
    });
  }

  async showBulkRejectModal(courses) {
    return new Promise((resolve) => {
      const modalHtml = `
                <div class="modal fade" id="bulkRejectModal" tabindex="-1" aria-hidden="true">
                    <div class="modal-dialog modal-lg">
                        <div class="modal-content">
                            <div class="modal-header bg-warning text-dark">
                                <h5 class="modal-title">
                                    <i class="fas fa-times-circle"></i> Reject Selected Courses
                                </h5>
                                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                            </div>
                            <div class="modal-body">
                                <p>You are about to reject <strong>${
                                  courses.length
                                }</strong> course(s):</p>
                                <ul class="course-list">
                                    ${courses
                                      .map(
                                        (course) => `<li>${course.name}</li>`
                                      )
                                      .join("")}
                                </ul>
                                
                                <div class="form-group mt-3">
                                    <label for="bulkRejectReason" class="form-label">Reason for rejection <span class="text-danger">*</span></label>
                                    <textarea class="form-control" id="bulkRejectReason" rows="4" 
                                              placeholder="Please explain why these courses are being rejected..."></textarea>
                                    <div class="form-text">This reason will be sent to all affected instructors.</div>
                                </div>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                                <button type="button" class="btn btn-warning" id="confirmBulkRejectBtn">
                                    <i class="fas fa-times-circle"></i> Reject All
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `;

      // Remove existing modal if any
      const existingModal = document.getElementById("bulkRejectModal");
      if (existingModal) existingModal.remove();

      // Add modal to DOM
      document.body.insertAdjacentHTML("beforeend", modalHtml);

      // Show modal
      const modal = new bootstrap.Modal(
        document.getElementById("bulkRejectModal")
      );
      modal.show();

      // Handle buttons
      document
        .getElementById("confirmBulkRejectBtn")
        .addEventListener("click", () => {
          const reason = document
            .getElementById("bulkRejectReason")
            .value.trim();
          if (!reason) {
            document
              .getElementById("bulkRejectReason")
              .classList.add("is-invalid");
            return;
          }
          modal.hide();
          resolve({ confirmed: true, reason });
        });

      document
        .getElementById("bulkRejectModal")
        .addEventListener("hidden.bs.modal", () => {
          document.getElementById("bulkRejectModal").remove();
          resolve({ confirmed: false });
        });

      // Real-time validation
      document
        .getElementById("bulkRejectReason")
        .addEventListener("input", function () {
          this.classList.remove("is-invalid");
        });
    });
  }

  async showBulkBanModal(courses) {
    return new Promise((resolve) => {
      const modalHtml = `
                <div class="modal fade" id="bulkBanModal" tabindex="-1" aria-hidden="true">
                    <div class="modal-dialog modal-lg">
                        <div class="modal-content">
                            <div class="modal-header bg-danger text-white">
                                <h5 class="modal-title">
                                    <i class="fas fa-ban"></i> Ban Selected Courses
                                </h5>
                                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                            </div>
                            <div class="modal-body">
                                <div class="alert alert-danger">
                                    <i class="fas fa-exclamation-triangle"></i>
                                    <strong>Warning: This is a serious action!</strong>
                                    <p class="mb-0 mt-1">You are about to ban <strong>${
                                      courses.length
                                    }</strong> course(s). This will remove them from public access immediately.</p>
                                </div>
                                
                                <p><strong>Courses to be banned:</strong></p>
                                <ul class="course-list">
                                    ${courses
                                      .map(
                                        (course) =>
                                          `<li>${course.name} <span class="text-muted">(${course.status})</span></li>`
                                      )
                                      .join("")}
                                </ul>
                                
                                <div class="form-group mt-3">
                                    <label for="bulkBanReason" class="form-label">Reason for banning <span class="text-danger">*</span></label>
                                    <textarea class="form-control" id="bulkBanReason" rows="4" 
                                              placeholder="Please explain why these courses are being banned..."></textarea>
                                    <div class="form-text">This reason will be recorded and sent to all affected instructors.</div>
                                </div>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                                <button type="button" class="btn btn-danger" id="confirmBulkBanBtn">
                                    <i class="fas fa-ban"></i> Ban All Courses
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `;

      // Remove existing modal if any
      const existingModal = document.getElementById("bulkBanModal");
      if (existingModal) existingModal.remove();

      // Add modal to DOM
      document.body.insertAdjacentHTML("beforeend", modalHtml);

      // Show modal
      const modal = new bootstrap.Modal(
        document.getElementById("bulkBanModal")
      );
      modal.show();

      // Handle buttons
      document
        .getElementById("confirmBulkBanBtn")
        .addEventListener("click", () => {
          const reason = document.getElementById("bulkBanReason").value.trim();
          if (!reason) {
            document
              .getElementById("bulkBanReason")
              .classList.add("is-invalid");
            return;
          }
          modal.hide();
          resolve({ confirmed: true, reason });
        });

      document
        .getElementById("bulkBanModal")
        .addEventListener("hidden.bs.modal", () => {
          document.getElementById("bulkBanModal").remove();
          resolve({ confirmed: false });
        });

      // Real-time validation
      document
        .getElementById("bulkBanReason")
        .addEventListener("input", function () {
          this.classList.remove("is-invalid");
        });
    });
  }
}

// Global functions for backward compatibility
function updateCourseStatus(courseId, isApproved) {
  const courseManagement = window.courseManagement || new CourseManagement();
  courseManagement.updateCourseStatus(
    courseId,
    isApproved,
    isApproved ? "Course approved successfully" : "Course rejected successfully"
  );
}

function banCourse(courseId, courseName) {
  const courseManagement = window.courseManagement || new CourseManagement();
  courseManagement.handleBan({
    target: {
      closest: () => ({
        dataset: { courseId, courseName },
      }),
    },
  });
}

function showCourseDetails(courseId) {
  const courseManagement = window.courseManagement || new CourseManagement();
  courseManagement.showCourseDetails(courseId);
}

// Initialize when DOM is loaded
document.addEventListener("DOMContentLoaded", function () {
  window.courseManagement = new CourseManagement();
});

// Export for module systems
if (typeof module !== "undefined" && module.exports) {
  module.exports = CourseManagement;
}
