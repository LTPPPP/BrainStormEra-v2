// Hide page loader when the document is fully loaded
document.addEventListener("DOMContentLoaded", function () {
  setTimeout(hideLoader, 1000); // Ensure that the loader displays for at least 1 second
});

// Also hide loader on window load (in case of images and resources)
window.addEventListener("load", function () {
  hideLoader();
});

function hideLoader() {
  const pageLoader = document.querySelector(".page-loader");
  if (pageLoader) {
    pageLoader.classList.add("hidden");
    // Completely remove after animation completes
    setTimeout(() => {
      pageLoader.style.display = "none";
    }, 500); // transition time in CSS
  }
}
