// Hide page loader when the document is fully loaded
document.addEventListener("DOMContentLoaded", function () {
  console.log("DOM Content loaded - hiding loader");
  setTimeout(hideLoader, 1000); // Đảm bảo rằng loader ít nhất hiển thị 1 giây
});

// Also hide loader on window load (in case of images and resources)
window.addEventListener("load", function () {
  console.log("Window loaded - hiding loader");
  hideLoader();
});

function hideLoader() {
  const pageLoader = document.querySelector(".page-loader");
  if (pageLoader) {
    pageLoader.classList.add("hidden");
    // Xóa hoàn toàn sau khi animation hoàn thành
    setTimeout(() => {
      pageLoader.style.display = "none";
      console.log("Loader hidden");
    }, 500); // thời gian của transition trong CSS
  }
}
