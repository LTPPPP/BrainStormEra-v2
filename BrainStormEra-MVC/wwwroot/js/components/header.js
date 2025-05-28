// Header notification functionality
document.addEventListener("DOMContentLoaded", function () {
  // This function will be used to show notifications in the header
  function updateChatNotificationBadge(count) {
    const badge = document.getElementById("chatNotificationBadge");
    if (badge) {
      if (count > 0) {
        badge.textContent = count;
        badge.style.display = "inline-block";
      } else {
        badge.style.display = "none";
      }
    }
  }

  // Expose the function globally so it can be called from SignalR or other scripts
  window.updateChatNotificationBadge = updateChatNotificationBadge;

  // For testing purposes, you can uncomment this to see the badge
  // updateChatNotificationBadge(5);
});
