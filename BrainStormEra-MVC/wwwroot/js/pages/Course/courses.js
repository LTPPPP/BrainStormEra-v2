// Course functionality for BrainStormEra (Legacy functions only)
document.addEventListener("DOMContentLoaded", function () {
  // Initialize legacy course functionality (non-search related)
  initializePageLoader();
  initializeEnrollmentForm();
  initializeShareButtons();
  initializeCurriculumAccordions();
  initializeLegacyCourseCards();
  initializeTabNavigation();
  initializeSortSectionAnimations();

  // Note: Search and filter functionality is now handled by course-search.js
});

// Page Loader
function initializePageLoader() {
  const loader = document.querySelector(".page-loader");
  if (loader) {
    window.addEventListener("load", function () {
      setTimeout(() => {
        loader.style.opacity = "0";
        setTimeout(() => {
          loader.style.display = "none";
        }, 300);
      }, 500);
    });
  }
}

// Enrollment Form Handler
function initializeEnrollmentForm() {
  const enrollmentForms = document.querySelectorAll(".enrollment-form");

  enrollmentForms.forEach((form) => {
    form.addEventListener("submit", function (e) {
      e.preventDefault();

      const submitBtn = form.querySelector(".enroll-btn");
      const originalText = submitBtn.innerHTML;
      const courseId = form.querySelector('input[name="courseId"]').value;

      // Show loading state
      submitBtn.innerHTML =
        '<i class="fas fa-spinner fa-spin me-2"></i>Processing...';
      submitBtn.disabled = true;

      // Simulate enrollment process
      fetch("/Course/Enroll", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
          RequestVerificationToken:
            document.querySelector('input[name="__RequestVerificationToken"]')
              ?.value || "",
        },
        body: `courseId=${courseId}`,
      })
        .then((response) => {
          if (response.ok) {
            return response.json();
          }
          throw new Error("Enrollment failed");
        })
        .then((data) => {
          if (data.success) {
            showEnrollmentSuccess(data);
            updateEnrollmentUI(submitBtn);
          } else {
            showEnrollmentError(data.message || "Enrollment failed");
          }
        })
        .catch((error) => {
          showEnrollmentError(
            "An error occurred during enrollment. Please try again."
          );
        })
        .finally(() => {
          // Reset button state if enrollment failed
          if (!submitBtn.classList.contains("enrolled")) {
            submitBtn.innerHTML = originalText;
            submitBtn.disabled = false;
          }
        });
    });
  });
}

// Show enrollment success
function showEnrollmentSuccess(data) {
  const modal = document.getElementById("enrollmentModal");
  const message = document.getElementById("enrollmentMessage");
  const continueBtn = document.getElementById("continueLearning");

  if (modal && message) {
    message.innerHTML = `
            <div class="alert alert-success">
                <i class="fas fa-check-circle me-2"></i>
                <strong>Congratulations!</strong> You have successfully enrolled in this course.
            </div>
        `;

    if (continueBtn) {
      continueBtn.style.display = "inline-block";
      continueBtn.onclick = () => {
        window.location.href = data.redirectUrl || "/Course/MyCourses";
      };
    }

    const bootstrapModal = new bootstrap.Modal(modal);
    bootstrapModal.show();
  }
}

// Show enrollment error
function showEnrollmentError(message) {
  const modal = document.getElementById("enrollmentModal");
  const messageDiv = document.getElementById("enrollmentMessage");

  if (modal && messageDiv) {
    messageDiv.innerHTML = `
            <div class="alert alert-danger">
                <i class="fas fa-exclamation-triangle me-2"></i>
                <strong>Enrollment Failed!</strong> ${message}
            </div>
        `;

    const bootstrapModal = new bootstrap.Modal(modal);
    bootstrapModal.show();
  }
}

// Update enrollment UI after successful enrollment
function updateEnrollmentUI(submitBtn) {
  submitBtn.innerHTML = '<i class="fas fa-check-circle me-2"></i>Enrolled';
  submitBtn.classList.remove("btn-primary");
  submitBtn.classList.add("btn-success", "enrolled");
  submitBtn.disabled = true;

  // Add continue learning button
  const continueBtn = document.createElement("a");
  continueBtn.href = "#";
  continueBtn.className = "btn btn-outline-primary btn-lg w-100 mt-2";
  continueBtn.innerHTML = "Continue Learning";
  submitBtn.parentNode.appendChild(continueBtn);
}

// Share functionality
function initializeShareButtons() {
  const shareButtons = document.querySelectorAll("[data-share]");

  shareButtons.forEach((button) => {
    button.addEventListener("click", function (e) {
      e.preventDefault();

      const shareType = this.getAttribute("data-share");
      const url = window.location.href;
      const title = document.title;

      switch (shareType) {
        case "facebook":
          window.open(
            `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(
              url
            )}`,
            "_blank",
            "width=600,height=400"
          );
          break;
        case "twitter":
          window.open(
            `https://twitter.com/intent/tweet?url=${encodeURIComponent(
              url
            )}&text=${encodeURIComponent(title)}`,
            "_blank",
            "width=600,height=400"
          );
          break;
        case "linkedin":
          window.open(
            `https://www.linkedin.com/sharing/share-offsite/?url=${encodeURIComponent(
              url
            )}`,
            "_blank",
            "width=600,height=400"
          );
          break;
        case "copy":
          copyToClipboard(url);
          showCopySuccess(this);
          break;
      }
    });
  });
}

// Copy to clipboard
function copyToClipboard(text) {
  if (navigator.clipboard) {
    navigator.clipboard.writeText(text);
  } else {
    // Fallback for older browsers
    const textArea = document.createElement("textarea");
    textArea.value = text;
    document.body.appendChild(textArea);
    textArea.select();
    document.execCommand("copy");
    document.body.removeChild(textArea);
  }
}

// Show copy success feedback
function showCopySuccess(button) {
  const originalIcon = button.innerHTML;
  button.innerHTML = '<i class="fas fa-check"></i>';
  button.style.color = "#28a745";

  setTimeout(() => {
    button.innerHTML = originalIcon;
    button.style.color = "";
  }, 2000);
}

// Curriculum accordions
function initializeCurriculumAccordions() {
  const sectionHeaders = document.querySelectorAll(".section-header");

  sectionHeaders.forEach((header) => {
    header.addEventListener("click", function () {
      const chevron = this.querySelector(".fa-chevron-down, .fa-chevron-up");
      const target = this.getAttribute("data-bs-target");
      const section = document.querySelector(target);

      if (chevron) {
        if (section && section.classList.contains("show")) {
          chevron.classList.remove("fa-chevron-up");
          chevron.classList.add("fa-chevron-down");
        } else {
          chevron.classList.remove("fa-chevron-down");
          chevron.classList.add("fa-chevron-up");
        }
      }
    });
  });
}

// Course card interactions (legacy - kept for non-search pages)
function initializeLegacyCourseCards() {
  const courseCards = document.querySelectorAll(".course-card");

  courseCards.forEach((card) => {
    // Add hover effects
    card.addEventListener("mouseenter", function () {
      this.style.transform = "translateY(-8px)";
    });

    card.addEventListener("mouseleave", function () {
      this.style.transform = "translateY(0)";
    });

    // Handle course card clicks
    const courseLink = card.querySelector(".course-link");
    if (courseLink) {
      card.style.cursor = "pointer";
      card.addEventListener("click", function (e) {
        // Don't trigger if clicking on buttons or links
        if (!e.target.closest("button, a")) {
          courseLink.click();
        }
      });
    }
  });
}

// Tab navigation
function initializeTabNavigation() {
  const tabButtons = document.querySelectorAll('[data-bs-toggle="tab"]');

  tabButtons.forEach((button) => {
    button.addEventListener("click", function () {
      // Update URL hash
      const target = this.getAttribute("data-bs-target");
      if (target) {
        const tabName = target.replace("#", "");
        history.replaceState(null, null, `#${tabName}`);
      }
    });
  });

  // Load tab from URL hash
  const hash = window.location.hash;
  if (hash) {
    const tabButton = document.querySelector(`[data-bs-target="${hash}"]`);
    if (tabButton) {
      const tab = new bootstrap.Tab(tabButton);
      tab.show();
    }
  }
}

// Enhanced Sort Section Animations
function initializeSortSectionAnimations() {
  const sortSection = document.querySelector(".sort-section");
  const sortSelect = document.querySelector("#sortSelect");
  const sortLabel = document.querySelector(".sort-label");

  if (!sortSection || !sortSelect) return;

  // Add loading class during sort operations
  sortSelect.addEventListener("change", function () {
    sortSection.classList.add("loading");

    // Remove loading class after a short delay
    setTimeout(() => {
      sortSection.classList.remove("loading");
    }, 800);
  });

  // Enhanced hover effects
  sortSection.addEventListener("mouseenter", function () {
    this.style.setProperty("--hover-intensity", "1");
  });

  sortSection.addEventListener("mouseleave", function () {
    this.style.setProperty("--hover-intensity", "0");
  });

  // Ripple effect on select focus
  sortSelect.addEventListener("focus", function () {
    createRippleEffect(this);
  });

  // Particle animation on interaction
  sortSelect.addEventListener("click", function () {
    createParticleEffect(sortSection);
  });
}

// Create ripple effect
function createRippleEffect(element) {
  const ripple = document.createElement("div");
  ripple.className = "ripple-effect";
  ripple.style.cssText = `
    position: absolute;
    border-radius: 50%;
    background: rgba(255, 255, 255, 0.4);
    transform: scale(0);
    animation: ripple 0.6s linear;
    pointer-events: none;
    z-index: 100;
  `;

  const rect = element.getBoundingClientRect();
  const size = Math.max(rect.width, rect.height);
  ripple.style.width = ripple.style.height = size + "px";
  ripple.style.left = rect.width / 2 - size / 2 + "px";
  ripple.style.top = rect.height / 2 - size / 2 + "px";

  element.style.position = "relative";
  element.appendChild(ripple);

  setTimeout(() => {
    if (ripple.parentNode) {
      ripple.parentNode.removeChild(ripple);
    }
  }, 600);
}

// Create particle effect
function createParticleEffect(container) {
  const particleCount = 6;

  for (let i = 0; i < particleCount; i++) {
    setTimeout(() => {
      const particle = document.createElement("div");
      particle.className = "particle";
      particle.style.cssText = `
        position: absolute;
        width: 4px;
        height: 4px;
        background: rgba(255, 255, 255, 0.8);
        border-radius: 50%;
        pointer-events: none;
        z-index: 10;
        animation: particle-float 2s ease-out forwards;
      `;

      // Random starting position
      const x = Math.random() * container.offsetWidth;
      const y = Math.random() * container.offsetHeight;
      particle.style.left = x + "px";
      particle.style.top = y + "px";

      container.appendChild(particle);

      setTimeout(() => {
        if (particle.parentNode) {
          particle.parentNode.removeChild(particle);
        }
      }, 2000);
    }, i * 100);
  }
}

// Add particle animation keyframes to CSS
const particleStyles = document.createElement("style");
particleStyles.textContent = `
  @keyframes particle-float {
    0% {
      opacity: 1;
      transform: translateY(0) scale(1);
    }
    100% {
      opacity: 0;
      transform: translateY(-30px) scale(0.5);
    }
  }
`;
document.head.appendChild(particleStyles);

// Utility functions
function showToast(message, type = "info") {
  const toast = document.createElement("div");
  toast.className = `toast align-items-center text-white bg-${type} border-0`;
  toast.setAttribute("role", "alert");
  toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">${message}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;

  // Add to toast container or create one
  let toastContainer = document.querySelector(".toast-container");
  if (!toastContainer) {
    toastContainer = document.createElement("div");
    toastContainer.className =
      "toast-container position-fixed bottom-0 end-0 p-3";
    document.body.appendChild(toastContainer);
  }

  toastContainer.appendChild(toast);

  const bsToast = new bootstrap.Toast(toast);
  bsToast.show();

  // Remove from DOM after hiding
  toast.addEventListener("hidden.bs.toast", function () {
    toast.remove();
  });
}

// Format price display
function formatPrice(price) {
  if (price === 0) {
    return '<span class="free-badge">FREE</span>';
  }
  return `$${price.toFixed(2)}`;
}

// Smooth scroll to element
function scrollToElement(element, offset = 0) {
  const targetPosition = element.offsetTop - offset;
  window.scrollTo({
    top: targetPosition,
    behavior: "smooth",
  });
}

// Lazy loading for images
function initializeLazyLoading() {
  const images = document.querySelectorAll("img[data-src]");

  const imageObserver = new IntersectionObserver((entries, observer) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting) {
        const img = entry.target;
        img.src = img.dataset.src;
        img.classList.remove("lazy");
        observer.unobserve(img);
      }
    });
  });

  images.forEach((img) => imageObserver.observe(img));
}

// Export functions for external use
window.CourseManager = {
  showToast,
  formatPrice,
  scrollToElement,
  copyToClipboard,
};
