// Course Search and Filter with AJAX
class CourseSearchManager {
  constructor() {
    this.currentFilters = {
      search: "",
      category: "",
      sortBy: "newest",
      page: 1,
    };

    this.searchTimeout = null;
    this.isLoading = false;

    this.init();
  }

  init() {
    this.bindEvents();
    this.initializeFromURL();
  }

  bindEvents() {
    // Search input with debounce
    const searchInput = document.getElementById("searchInput");
    if (searchInput) {
      searchInput.addEventListener("input", (e) => {
        this.showClearButton(e.target.value);
        this.debounceSearch(e.target.value);
      });

      searchInput.addEventListener("keypress", (e) => {
        if (e.key === "Enter") {
          e.preventDefault();
          this.performSearch();
        }
      });
    }

    // Search button
    const searchBtn = document.getElementById("searchBtn");
    if (searchBtn) {
      searchBtn.addEventListener("click", () => {
        this.performSearch();
      });
    }

    // Clear search button
    const clearSearch = document.getElementById("clearSearch");
    if (clearSearch) {
      clearSearch.addEventListener("click", () => {
        this.clearSearch();
      });
    }

    // Category filters
    const categoryFilters = document.querySelectorAll(".category-filter");
    categoryFilters.forEach((filter) => {
      filter.addEventListener("click", (e) => {
        e.preventDefault();
        this.selectCategory(e.target);
      });
    });

    // Sort dropdown
    const sortSelect = document.getElementById("sortSelect");
    if (sortSelect) {
      sortSelect.addEventListener("change", (e) => {
        this.currentFilters.sortBy = e.target.value;
        this.currentFilters.page = 1;
        this.performSearch();
      });
    }

    // Pagination buttons (delegated event)
    document.addEventListener("click", (e) => {
      if (
        e.target.classList.contains("pagination-btn") ||
        e.target.closest(".pagination-btn")
      ) {
        e.preventDefault();
        const btn = e.target.classList.contains("pagination-btn")
          ? e.target
          : e.target.closest(".pagination-btn");
        const page = parseInt(btn.dataset.page);
        if (page && page !== this.currentFilters.page) {
          this.goToPage(page);
        }
      }
    });

    // Clear all filters
    const clearAllFilters = document.getElementById("clearAllFilters");
    if (clearAllFilters) {
      clearAllFilters.addEventListener("click", () => {
        this.clearAllFilters();
      });
    }
  }

  initializeFromURL() {
    const urlParams = new URLSearchParams(window.location.search);
    this.currentFilters.search = urlParams.get("search") || "";
    this.currentFilters.category = urlParams.get("category") || "";
    this.currentFilters.sortBy = urlParams.get("sortBy") || "newest";
    this.currentFilters.page = parseInt(urlParams.get("page")) || 1;

    // Update UI
    const searchInput = document.getElementById("searchInput");
    if (searchInput) {
      searchInput.value = this.currentFilters.search;
      this.showClearButton(this.currentFilters.search);
    }

    const sortSelect = document.getElementById("sortSelect");
    if (sortSelect) {
      sortSelect.value = this.currentFilters.sortBy;
    }

    // Update category filter active state
    this.updateCategoryFilters();
  }

  debounceSearch(searchTerm) {
    clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => {
      if (searchTerm.length >= 2 || searchTerm.length === 0) {
        this.currentFilters.search = searchTerm;
        this.currentFilters.page = 1;
        this.performSearch();
      }
    }, 500);
  }

  performSearch() {
    if (this.isLoading) return;

    const searchInput = document.getElementById("searchInput");
    if (searchInput) {
      this.currentFilters.search = searchInput.value.trim();
    }

    this.showLoading();
    this.updateURL();

    const params = new URLSearchParams({
      search: this.currentFilters.search,
      category: this.currentFilters.category,
      sortBy: this.currentFilters.sortBy,
      page: this.currentFilters.page,
      pageSize: 12,
    });

    // Remove empty parameters
    for (let [key, value] of [...params.entries()]) {
      if (!value) {
        params.delete(key);
      }
    }

    fetch(`/Course/SearchCourses?${params.toString()}`, {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        "X-Requested-With": "XMLHttpRequest",
      },
    })
      .then((response) => response.json())
      .then((data) => {
        if (data.success) {
          this.updateCoursesDisplay(data);
        } else {
          this.showError(
            data.message || "An error occurred while searching courses"
          );
        }
      })
      .catch((error) => {
        console.error("Error:", error);
        this.showError("Network error occurred. Please try again.");
      })
      .finally(() => {
        this.hideLoading();
      });
  }

  updateCoursesDisplay(data) {
    const coursesContainer = document.getElementById("coursesContainer");
    if (!coursesContainer) return;

    // Update results count
    const resultsCount = document.getElementById("resultsCount");
    if (resultsCount) {
      resultsCount.textContent = `${data.totalCourses} courses found`;
    }

    if (data.courses && data.courses.length > 0) {
      // Generate courses HTML
      const coursesHTML = this.generateCoursesHTML(data.courses);
      const paginationHTML = this.generatePaginationHTML(data);

      coursesContainer.innerHTML = `
                <div class="row" id="coursesGrid">
                    ${coursesHTML}
                </div>
                ${paginationHTML}
            `;
    } else {
      // Show no courses message
      coursesContainer.innerHTML = this.generateNoCoursesHTML();
    }

    // Animate course cards
    this.animateCourseCards();
  }

  generateCoursesHTML(courses) {
    return courses
      .map(
        (course) => `
            <div class="col-lg-3 col-md-4 col-sm-6 mb-4">
                <div class="course-card" data-course-id="${course.courseId}">
                    <div class="course-image">
                        <img src="${course.coursePicture}" alt="${
          course.courseName
        }" loading="lazy">
                        <div class="course-overlay">
                            <a href="/Course/Details/${
                              course.courseId
                            }" class="view-course-btn">
                                <i class="fas fa-eye"></i>
                                View Details
                            </a>
                        </div>
                        ${
                          course.price > 0
                            ? `<div class="course-price">$${course.price.toLocaleString()}</div>`
                            : `<div class="course-price free">Free</div>`
                        }
                    </div>
                    <div class="course-content">
                        <div class="course-categories">
                            ${course.courseCategories
                              .slice(0, 2)
                              .map(
                                (cat) =>
                                  `<span class="category-badge">${cat}</span>`
                              )
                              .join("")}
                        </div>
                        <h3 class="course-title">
                            <a href="/Course/Details/${course.courseId}">
                                ${course.courseName}
                            </a>
                        </h3>
                        <p class="course-description">
                            ${
                              course.description &&
                              course.description.length > 100
                                ? course.description.substring(0, 100) + "..."
                                : course.description || ""
                            }
                        </p>
                        <div class="course-meta">
                            <div class="instructor">
                                <i class="fas fa-user"></i>
                                <span>${course.createdBy}</span>
                            </div>
                            <div class="rating">
                                ${this.generateStarRating(course.starRating)}
                                <span class="rating-text">(${
                                  course.starRating
                                }.0)</span>
                            </div>
                        </div>
                        <div class="course-stats">
                            <span class="enrollment-count">
                                <i class="fas fa-users"></i>
                                ${course.enrollmentCount} students
                            </span>
                        </div>
                        <div class="course-actions">
                            <a href="/Course/Details/${
                              course.courseId
                            }" class="btn btn-outline-primary">
                                <i class="fas fa-info-circle"></i>
                                Learn More
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        `
      )
      .join("");
  }

  generateStarRating(rating) {
    let stars = "";
    for (let i = 1; i <= 5; i++) {
      stars += `<i class="fas fa-star ${i <= rating ? "filled" : ""}"></i>`;
    }
    return stars;
  }

  generatePaginationHTML(data) {
    if (data.totalPages <= 1) return "";

    let paginationHTML =
      '<div class="pagination-wrapper" id="paginationContainer">';
    paginationHTML +=
      '<nav aria-label="Course pagination"><ul class="pagination justify-content-center">';

    // Previous button
    if (data.hasPreviousPage) {
      paginationHTML += `
                <li class="page-item">
                    <button class="page-link pagination-btn" data-page="${
                      data.currentPage - 1
                    }">
                        <i class="fas fa-chevron-left"></i>
                        Previous
                    </button>
                </li>
            `;
    }

    // Page numbers
    const startPage = Math.max(1, data.currentPage - 2);
    const endPage = Math.min(data.totalPages, data.currentPage + 2);

    for (let i = startPage; i <= endPage; i++) {
      paginationHTML += `
                <li class="page-item ${i === data.currentPage ? "active" : ""}">
                    <button class="page-link pagination-btn" data-page="${i}">
                        ${i}
                    </button>
                </li>
            `;
    }

    // Next button
    if (data.hasNextPage) {
      paginationHTML += `
                <li class="page-item">
                    <button class="page-link pagination-btn" data-page="${
                      data.currentPage + 1
                    }">
                        Next
                        <i class="fas fa-chevron-right"></i>
                    </button>
                </li>
            `;
    }

    paginationHTML += "</ul></nav>";

    // Pagination info
    const startItem = (data.currentPage - 1) * 12 + 1;
    const endItem = Math.min(data.currentPage * 12, data.totalCourses);
    paginationHTML += `
            <div class="pagination-info">
                <span id="paginationInfo">Showing ${startItem} to ${endItem} of ${data.totalCourses} courses</span>
            </div>
        `;

    paginationHTML += "</div>";
    return paginationHTML;
  }

  generateNoCoursesHTML() {
    const hasFilters =
      this.currentFilters.search || this.currentFilters.category;

    return `
            <div class="no-courses" id="noCoursesMessage">
                <div class="no-courses-content">
                    <i class="fas fa-search fa-3x"></i>
                    <h3>No courses found</h3>
                    ${
                      hasFilters
                        ? `
                        <p>Try adjusting your search criteria or browse all courses.</p>
                        <button id="clearAllFilters" class="btn btn-primary">
                            <i class="fas fa-refresh"></i>
                            Show All Courses
                        </button>
                    `
                        : `
                        <p>No courses are available at the moment. Please check back later.</p>
                    `
                    }
                </div>
            </div>
        `;
  }

  selectCategory(categoryElement) {
    // Remove active class from all category filters
    document.querySelectorAll(".category-filter").forEach((filter) => {
      filter.classList.remove("active");
    });

    // Add active class to selected category
    categoryElement.classList.add("active");

    // Update current filters
    this.currentFilters.category = categoryElement.dataset.category || "";
    this.currentFilters.page = 1;

    // Perform search
    this.performSearch();
  }

  updateCategoryFilters() {
    document.querySelectorAll(".category-filter").forEach((filter) => {
      filter.classList.remove("active");
      if (filter.dataset.category === this.currentFilters.category) {
        filter.classList.add("active");
      }
    });
  }

  goToPage(page) {
    this.currentFilters.page = page;
    this.performSearch();

    // Scroll to top of courses section
    const coursesSection = document.querySelector(".courses-section");
    if (coursesSection) {
      coursesSection.scrollIntoView({ behavior: "smooth", block: "start" });
    }
  }

  clearSearch() {
    const searchInput = document.getElementById("searchInput");
    if (searchInput) {
      searchInput.value = "";
      searchInput.focus();
    }

    this.currentFilters.search = "";
    this.currentFilters.page = 1;
    this.showClearButton("");
    this.performSearch();
  }

  clearAllFilters() {
    // Reset all filters
    this.currentFilters = {
      search: "",
      category: "",
      sortBy: "newest",
      page: 1,
    };

    // Update UI
    const searchInput = document.getElementById("searchInput");
    if (searchInput) {
      searchInput.value = "";
    }

    const sortSelect = document.getElementById("sortSelect");
    if (sortSelect) {
      sortSelect.value = "newest";
    }

    this.showClearButton("");
    this.updateCategoryFilters();
    this.performSearch();
  }

  showClearButton(searchValue) {
    const clearBtn = document.getElementById("clearSearch");
    if (clearBtn) {
      clearBtn.style.display = searchValue ? "block" : "none";
    }
  }

  showLoading() {
    this.isLoading = true;
    const loadingIndicator = document.getElementById("loadingIndicator");
    if (loadingIndicator) {
      loadingIndicator.style.display = "flex";
    }

    // Disable search controls
    this.toggleControls(false);
  }

  hideLoading() {
    this.isLoading = false;
    const loadingIndicator = document.getElementById("loadingIndicator");
    if (loadingIndicator) {
      loadingIndicator.style.display = "none";
    }

    // Enable search controls
    this.toggleControls(true);
  }

  toggleControls(enabled) {
    const controls = ["searchInput", "searchBtn", "clearSearch", "sortSelect"];

    controls.forEach((id) => {
      const element = document.getElementById(id);
      if (element) {
        element.disabled = !enabled;
      }
    });

    // Toggle category filters
    document.querySelectorAll(".category-filter").forEach((filter) => {
      filter.disabled = !enabled;
      if (enabled) {
        filter.style.pointerEvents = "auto";
        filter.style.opacity = "1";
      } else {
        filter.style.pointerEvents = "none";
        filter.style.opacity = "0.6";
      }
    });
  }

  updateURL() {
    const params = new URLSearchParams();

    if (this.currentFilters.search) {
      params.set("search", this.currentFilters.search);
    }

    if (this.currentFilters.category) {
      params.set("category", this.currentFilters.category);
    }

    if (this.currentFilters.sortBy !== "newest") {
      params.set("sortBy", this.currentFilters.sortBy);
    }

    if (this.currentFilters.page > 1) {
      params.set("page", this.currentFilters.page);
    }

    const newURL = `${window.location.pathname}${
      params.toString() ? "?" + params.toString() : ""
    }`;
    window.history.replaceState(null, "", newURL);
  }

  animateCourseCards() {
    const courseCards = document.querySelectorAll(".course-card");
    courseCards.forEach((card, index) => {
      card.style.opacity = "0";
      card.style.transform = "translateY(20px)";

      setTimeout(() => {
        card.style.transition = "all 0.3s ease";
        card.style.opacity = "1";
        card.style.transform = "translateY(0)";
      }, index * 50);
    });
  }

  showError(message) {
    // You can implement a toast notification or alert here
    console.error("Search error:", message);

    // Simple alert for now - you can replace with a better notification system
    alert(message);
  }
}

// Initialize when DOM is loaded
document.addEventListener("DOMContentLoaded", function () {
  new CourseSearchManager();
});
