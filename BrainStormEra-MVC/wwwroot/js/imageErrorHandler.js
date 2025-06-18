/**
 * Image Error Handler
 * This script handles cases when images fail to load by replacing them with a default image
 */
document.addEventListener("DOMContentLoaded", function () {
  // Set default images for different contexts
  const defaultProfileImage = "/SharedMedia/defaults/default-avatar.svg";
  const defaultCourseImage = "/SharedMedia/defaults/default-course.svg";
  const defaultAchievementImage = "/SharedMedia/defaults/default-achievement.svg";
  const defaultLogo = "/SharedMedia/defaults/default-logo.svg";

  // SharedMedia constants for uploaded content
  const SHARED_MEDIA_PATHS = {
    avatars: "/SharedMedia/avatars/",
    courses: "/SharedMedia/courses/",
    documents: "/SharedMedia/documents/",
    uploads: "/SharedMedia/uploads/",
    images: "/SharedMedia/images/"
  };

  // Preload default images to ensure they're available
  const defaultImages = [
    defaultProfileImage,
    defaultCourseImage,
    defaultAchievementImage,
    defaultLogo,
  ];
  defaultImages.forEach((imageSrc) => {
    const img = new Image();
    img.src = imageSrc;
  });
  
  // Function to handle image error
  function handleImageError(img) {
    const imgSrc = img.src || "";
    const imgAlt = img.alt || "";
    const imgClasses = img.className || "";

    // Prevent infinite loops by checking if we've already tried to fix this image
    if (img.hasAttribute("data-error-handled")) {
      return;
    }

    // Mark image as handled to prevent infinite loops
    img.setAttribute("data-error-handled", "true");

    // Add loading state
    const container = img.closest(".image-container") || img.parentElement;
    if (container) {
      container.classList.add("error");
    }
    
    // Suppress console error for image loading (optional - remove comment to see errors)
    // console.warn(`Image failed to load: ${imgSrc}`);

    // Select appropriate default image based on context
    let defaultImage = defaultLogo; // fallback default

    if (
      imgAlt.toLowerCase().includes("avatar") ||
      imgSrc.toLowerCase().includes("avatar") ||
      imgSrc.toLowerCase().includes("profile") ||
      imgClasses.includes("avatar") ||
      imgClasses.includes("profile") ||
      imgClasses.includes("author-avatar") ||
      imgClasses.includes("profile-image")
    ) {
      defaultImage = defaultProfileImage;
    } else if (
      imgAlt.toLowerCase().includes("course") ||
      imgSrc.toLowerCase().includes("course") ||
      imgClasses.includes("course") ||
      imgClasses.includes("course-img") ||
      imgClasses.includes("course-image") ||
      img.closest(".course-card") ||
      img.closest(".course-image")
    ) {
      defaultImage = defaultCourseImage;
    } else if (
      imgAlt.toLowerCase().includes("achievement") ||
      imgSrc.toLowerCase().includes("achievement") ||
      imgClasses.includes("achievement")
    ) {
      defaultImage = defaultAchievementImage;
    } else if (
      imgAlt.toLowerCase().includes("logo") ||
      imgSrc.toLowerCase().includes("logo") ||
      imgClasses.includes("logo")
    ) {
      defaultImage = defaultLogo;
    }

    // Set the default image
    img.src = defaultImage;
    
    // Add subtle styling to indicate this is a fallback image
    img.style.opacity = "0.9";
    img.style.filter = "grayscale(10%)";

    // Remove onerror to prevent infinite loops
    img.onerror = null;

    // Add a small delay to ensure the default image loads
    setTimeout(() => {
      if (container) {
        container.classList.remove("error");
      }
    }, 100);
  }
  
  // Add error handler to all images
  document.querySelectorAll("img").forEach((img) => {
    // Skip if already has error handler
    if (img.hasAttribute("data-error-handler-added")) {
      return;
    }

    img.setAttribute("data-error-handler-added", "true");

    img.onerror = function () {
      handleImageError(this);
    };

    // Also handle cases where src is empty or invalid
    if (!img.src || img.src === window.location.href || img.src.endsWith("/")) {
      handleImageError(img);
    }
  });

  // Handle dynamically added images
  const observer = new MutationObserver((mutations) => {
    mutations.forEach((mutation) => {
      mutation.addedNodes.forEach((node) => {
        // Check if the added node is an element
        if (node.nodeType === Node.ELEMENT_NODE) {
          // If it's an image, add the error handler
          if (node.tagName === "IMG") {
            if (!node.hasAttribute("data-error-handler-added")) {
              node.setAttribute("data-error-handler-added", "true");
              node.onerror = function () {
                handleImageError(this);
              };

              // Check if image src is invalid
              if (
                !node.src ||
                node.src === window.location.href ||
                node.src.endsWith("/")
              ) {
                handleImageError(node);
              }
            }
          }
          // If it contains images, add handlers to them
          const images = node.querySelectorAll("img");
          images.forEach((img) => {
            if (!img.hasAttribute("data-error-handler-added")) {
              img.setAttribute("data-error-handler-added", "true");
              img.onerror = function () {
                handleImageError(this);
              };

              // Check if image src is invalid
              if (
                !img.src ||
                img.src === window.location.href ||
                img.src.endsWith("/")
              ) {
                handleImageError(img);
              }
            }
          });
        }
      });
    });
  });

  // Observe the entire document for changes
  observer.observe(document.body, {
    childList: true,
    subtree: true,
  });
});
