// Default Lesson JavaScript
function printContent() {
  window.print();
}

function shareContent() {
  if (navigator.share) {
    navigator
      .share({
        title: document.title,
        text: document.querySelector('meta[name="description"]')?.content || "",
        url: window.location.href,
      })
      .catch(() => {
        // Error occurred
      });
  } else {
    // Fallback: copy URL to clipboard
    navigator.clipboard
      .writeText(window.location.href)
      .then(() => {
        if (window.showToast) {
          window.showToast("success", "Lesson URL copied to clipboard");
        } else {
          alert("Lesson URL copied to clipboard");
        }
      })
      .catch(() => {
        if (window.showToast) {
          window.showToast("error", "Could not copy URL to clipboard");
        } else {
          alert("Could not copy URL to clipboard");
        }
      });
  }
}
