// Image error handler utility
function handleImageError(img, fallbackSrc) {
  img.onerror = null; // Prevent infinite loop
  img.src = fallbackSrc || "/SharedMedia/defaults/default-image.svg";
}
