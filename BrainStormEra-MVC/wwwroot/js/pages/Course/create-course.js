// Create Course JavaScript
document.addEventListener("DOMContentLoaded", function () {
  const categoryInput = document.getElementById("categoryInput");
  const categorySuggestions = document.getElementById("categorySuggestions");
  const selectedCategoriesContainer =
    document.getElementById("selectedCategories");
  const selectedCategoriesInputsContainer = document.getElementById(
    "selectedCategoriesInputs"
  );
  const loadingSpinner = document.querySelector(".loading-spinner");

  let selectedCategories = [];
  let currentSuggestions = [];
  let selectedIndex = -1;
  let searchTimeout;

  // Initialize category autocomplete
  function initializeCategoryAutocomplete() {
    // Check if required elements exist before initializing
    if (
      !categoryInput ||
      !categorySuggestions ||
      !selectedCategoriesContainer
    ) {
      console.log(
        "Category autocomplete elements not found - skipping initialization"
      );
      return;
    }

    categoryInput.addEventListener("input", handleCategoryInput);
    categoryInput.addEventListener("keydown", handleKeyNavigation);
    categoryInput.addEventListener("blur", handleInputBlur);

    // Click outside to close suggestions
    document.addEventListener("click", function (e) {
      if (!e.target.closest(".category-input-container")) {
        hideSuggestions();
      }
    });
  }

  // Handle category input with debouncing
  function handleCategoryInput(e) {
    const searchTerm = e.target.value.trim();

    // Clear previous timeout
    if (searchTimeout) {
      clearTimeout(searchTimeout);
    }

    if (searchTerm.length === 0) {
      hideSuggestions();
      return;
    }

    // Show loading spinner
    showLoadingSpinner();

    // Debounce search to avoid too many requests
    searchTimeout = setTimeout(() => {
      searchCategories(searchTerm);
    }, 300);
  }
  // Search categories via AJAX
  function searchCategories(searchTerm) {
    fetch(`/Course/SearchCategories?term=${encodeURIComponent(searchTerm)}`)
      .then((response) => response.json())
      .then((categories) => {
        hideLoadingSpinner();

        displaySuggestions(categories);
      })
      .catch((error) => {
        hideLoadingSpinner();
        hideSuggestions();
      });
  } // Display category suggestions
  function displaySuggestions(categories) {
    if (!categories || categories.length === 0) {
      showNoResultsMessage();
      return;
    }

    // Filter out already selected categories
    currentSuggestions = categories.filter(
      (cat) =>
        !selectedCategories.some(
          (selected) => selected.CategoryId === cat.CategoryId
        )
    );

    if (currentSuggestions.length === 0) {
      showNoResultsMessage("All matching categories are already selected");
      return;
    }

    categorySuggestions.innerHTML = "";
    selectedIndex = -1;
    currentSuggestions.forEach((category, index) => {
      const suggestionItem = document.createElement("div");
      suggestionItem.className = "suggestion-item";

      // Add staggered animation delay
      suggestionItem.style.animationDelay = `${index * 0.05}s`;

      // Handle potential undefined values with fallbacks
      const categoryName =
        category.CategoryName || category.categoryName || "Unknown Category";
      const categoryIcon =
        category.CategoryIcon || category.categoryIcon || "fas fa-tag";

      suggestionItem.innerHTML = `
                <i class="${categoryIcon} suggestion-icon"></i>
                <span>${categoryName}</span>
            `;

      suggestionItem.addEventListener("click", () => {
        // Create normalized category object
        const normalizedCategory = {
          CategoryId: category.CategoryId || category.categoryId,
          CategoryName: categoryName,
          CategoryIcon: categoryIcon,
        };
        selectCategory(normalizedCategory);
      });

      suggestionItem.addEventListener("mouseenter", () => {
        selectedIndex = index;
        updateSelectedSuggestion();
      });

      categorySuggestions.appendChild(suggestionItem);
    });

    showSuggestions();
  }

  // Show no results message
  function showNoResultsMessage(message = "No categories found") {
    categorySuggestions.innerHTML = `
      <div class="no-suggestions">
        <i class="fas fa-search me-2"></i>
        ${message}
      </div>
    `;
    showSuggestions();
  }

  // Handle keyboard navigation
  function handleKeyNavigation(e) {
    if (
      !categorySuggestions.style.display ||
      categorySuggestions.style.display === "none"
    ) {
      return;
    }

    switch (e.key) {
      case "ArrowDown":
        e.preventDefault();
        selectedIndex = Math.min(
          selectedIndex + 1,
          currentSuggestions.length - 1
        );
        updateSelectedSuggestion();
        break;

      case "ArrowUp":
        e.preventDefault();
        selectedIndex = Math.max(selectedIndex - 1, -1);
        updateSelectedSuggestion();
        break;

      case "Enter":
        e.preventDefault();
        if (selectedIndex >= 0 && selectedIndex < currentSuggestions.length) {
          selectCategory(currentSuggestions[selectedIndex]);
        }
        break;

      case "Escape":
        hideSuggestions();
        categoryInput.blur();
        break;
    }
  }

  // Update visual selection in suggestions
  function updateSelectedSuggestion() {
    const suggestionItems =
      categorySuggestions.querySelectorAll(".suggestion-item");
    suggestionItems.forEach((item, index) => {
      item.classList.toggle("selected", index === selectedIndex);
    });
  }
  // Select a category
  function selectCategory(category) {
    // Validate category object
    if (!category || !category.CategoryId || !category.CategoryName) {
      // Invalid category object
      return;
    }

    // Check if category is already selected
    if (
      selectedCategories.some(
        (selected) => selected.CategoryId === category.CategoryId
      )
    ) {
      return;
    }

    // Create a clean category object to avoid reference issues
    const cleanCategory = {
      CategoryId: category.CategoryId,
      CategoryName: category.CategoryName,
      CategoryIcon: category.CategoryIcon || "fas fa-tag",
    };

    selectedCategories.push(cleanCategory);

    updateSelectedCategoriesDisplay();
    updateSelectedCategoriesInputs();

    // Clear input and hide suggestions
    categoryInput.value = "";
    hideSuggestions();
    categoryInput.focus();
  }

  // Remove a selected category
  function removeCategory(categoryId) {
    selectedCategories = selectedCategories.filter(
      (cat) => cat.CategoryId !== categoryId
    );
    updateSelectedCategoriesDisplay();
    updateSelectedCategoriesInputs();
  }

  // Update selected categories display
  function updateSelectedCategoriesDisplay() {
    selectedCategoriesContainer.innerHTML = "";

    if (selectedCategories.length === 0) {
      selectedCategoriesContainer.innerHTML = `
        <div class="text-muted text-center w-100">
          <i class="fas fa-plus-circle me-2"></i>
          Selected categories will appear here
        </div>
      `;
      return;
    }
    selectedCategories.forEach((category) => {
      // Validate category data
      const categoryName = category.CategoryName || "Unknown Category";
      const categoryIcon = category.CategoryIcon || "fas fa-tag";

      const categoryTag = document.createElement("div");
      categoryTag.className = "category-tag";
      categoryTag.innerHTML = `
                <i class="${categoryIcon} me-1"></i>
                <span>${categoryName}</span>
                <span class="remove-btn">
                    <i class="fas fa-times"></i>
                </span>
            `;

      // Add event listener directly to the remove button instead of using inline onclick
      const removeBtn = categoryTag.querySelector(".remove-btn");
      if (removeBtn) {
        removeBtn.addEventListener("click", () => {
          removeCategory(category.CategoryId);
        });
      }

      selectedCategoriesContainer.appendChild(categoryTag);
    });
  }
  // Update hidden inputs for form submission
  function updateSelectedCategoriesInputs() {
    selectedCategoriesInputsContainer.innerHTML = "";

    selectedCategories.forEach((category, index) => {
      const hiddenInput = document.createElement("input");
      hiddenInput.type = "hidden";
      // Use the correct format for ASP.NET Core model binding for string arrays
      hiddenInput.name = "SelectedCategories";
      hiddenInput.value = category.CategoryId;
      selectedCategoriesInputsContainer.appendChild(hiddenInput);
    });
  }

  // Show suggestions dropdown
  function showSuggestions() {
    categorySuggestions.style.display = "block";
    // Force reflow to ensure the display change is applied
    categorySuggestions.offsetHeight;
    // Add the show class to trigger the animation
    setTimeout(() => {
      categorySuggestions.classList.add("show");
    }, 10);
  }

  // Hide suggestions dropdown
  function hideSuggestions() {
    categorySuggestions.classList.remove("show");
    // Wait for animation to complete before hiding
    setTimeout(() => {
      categorySuggestions.style.display = "none";
    }, 300);
    selectedIndex = -1;
  }

  // Handle input blur with delay to allow for clicks
  function handleInputBlur() {
    setTimeout(() => {
      if (
        !categoryInput.matches(":focus") &&
        !categorySuggestions.matches(":hover")
      ) {
        hideSuggestions();
      }
    }, 150);
  }

  // Show loading spinner
  function showLoadingSpinner() {
    const container = document.querySelector(".category-input-container");
    container.classList.add("loading");
    loadingSpinner.style.display = "inline-block";
  }

  // Hide loading spinner
  function hideLoadingSpinner() {
    const container = document.querySelector(".category-input-container");
    container.classList.remove("loading");
    loadingSpinner.style.display = "none";
  }
  // Form validation
  function validateForm() {
    const form = document.getElementById("createCourseForm");
    let isValid = true;

    // Basic form validation - let ASP.NET handle most validation
    const courseName = document
      .querySelector('[name="CourseName"]')
      ?.value?.trim();
    const courseDescription = document
      .querySelector('[name="CourseDescription"]')
      ?.value?.trim();
    const price = document.querySelector('[name="Price"]')?.value;
    const difficultyLevel = document.querySelector(
      '[name="DifficultyLevel"]:checked'
    )?.value;

    // Validate categories selection (most important one that's often missed)
    if (selectedCategories.length === 0) {
      showFieldError(
        "SelectedCategories",
        "Please select at least one category"
      );
      isValid = false;
    } else {
      hideFieldError("SelectedCategories");
    } // Optional: Basic required field validation (ASP.NET will also validate these)
    if (!courseName) {
      isValid = false;
    }

    if (!courseDescription) {
      isValid = false;
    }

    if (!price && price !== "0") {
      isValid = false;
    }

    if (!difficultyLevel) {
      isValid = false;
    }

    return isValid;
  }

  // Show field error
  function showFieldError(fieldName, message) {
    const field = document.querySelector(`[asp-validation-for="${fieldName}"]`);
    if (field) {
      field.textContent = message;
      field.style.display = "block";
    }
  }

  // Hide field error
  function hideFieldError(fieldName) {
    const field = document.querySelector(`[asp-validation-for="${fieldName}"]`);
    if (field) {
      field.style.display = "none";
    }
  }
  // Form submission handling
  const form = document.getElementById("createCourseForm");
  if (form) {
    form.addEventListener("submit", function (e) {
      // Always update selected categories inputs before validation
      updateSelectedCategoriesInputs();

      // Debug: Log all form data
      const formData = new FormData(form);

      // Perform validation
      const validationResult = validateForm();

      if (!validationResult) {
        e.preventDefault();
        return false;
      }

      // Let the form submit naturally
    });
  } else {
    // Form with ID 'createCourseForm' not found!
  }

  // Initialize everything - only if we're on a page that has the required elements
  if (categoryInput) {
    initializeCategoryAutocomplete();
  }

  // Page animations
  function animatePageLoad() {
    const sections = document.querySelectorAll(".form-section");
    sections.forEach((section, index) => {
      section.style.opacity = "0";
      section.style.transform = "translateY(30px)";

      setTimeout(() => {
        section.style.transition = "all 0.6s ease";
        section.style.opacity = "1";
        section.style.transform = "translateY(0)";
      }, index * 200);
    });
  }

  // Initialize page animations
  setTimeout(animatePageLoad, 100);

  // Remove page loader
  setTimeout(() => {
    const pageLoader = document.querySelector(".page-loader");
    if (pageLoader) {
      pageLoader.style.display = "none";
    }
  }, 500);
});
