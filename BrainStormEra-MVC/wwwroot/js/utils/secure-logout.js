/**
 * Secure Logout Utility
 *
 * This script provides a secure way to handle user logout by:
 * 1. Clearing all client-side storage (localStorage, sessionStorage)
 * 2. Performing a proper server-side logout
 * 3. Redirecting to the login page
 */

class SecureLogout {
  constructor() {
    this.bindLogoutEvents();
  }

  /**
   * Bind click events to all logout buttons
   */
  bindLogoutEvents() {
    document.addEventListener("DOMContentLoaded", () => {
      // Find all logout links/buttons
      const logoutLinks = document.querySelectorAll(
        'a[href*="Login/Logout"], .logout-btn, [data-action="logout"]'
      );

      logoutLinks.forEach((link) => {
        link.addEventListener("click", (e) => {
          e.preventDefault();
          this.performSecureLogout();
        });
      });
    });
  }
  /**
   * Perform a secure logout
   */
  performSecureLogout() {
    // Show logout popup
    this.showLogoutPopup();

    // Clear client-side storage
    this.clearClientStorage();

    // Make server-side logout request using fetch to avoid full page redirect
    fetch("/Login/LogoutComplete", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Requested-With": "XMLHttpRequest",
      },
      credentials: "same-origin",
    })
      .then((response) => response.json())
      .then((data) => {
        // After successful server-side logout, redirect to guest homepage
        setTimeout(() => {
          window.location.href = "/Home/Index";
        }, 1500); // Show popup for 1.5 seconds before redirecting
      })
      .catch((error) => {
        // Redirect to guest homepage anyway in case of error
        window.location.href = "/Home/Index";
      });
  }

  /**
   * Show a popup indicating logout in progress
   */
  showLogoutPopup() {
    // Create popup element if it doesn't exist
    let popup = document.getElementById("logoutPopup");

    if (!popup) {
      popup = document.createElement("div");
      popup.id = "logoutPopup";
      popup.className = "logout-popup";

      // Create popup content
      popup.innerHTML = `
        <div class="logout-popup-content">
          <div class="spinner"></div>
          <h3>Logging Out</h3>
          <p>Please wait...</p>
        </div>
      `;

      // Add styles
      const style = document.createElement("style");
      style.textContent = `
        .logout-popup {
          position: fixed;
          top: 0;
          left: 0;
          width: 100%;
          height: 100%;
          background-color: rgba(0, 0, 0, 0.5);
          display: flex;
          align-items: center;
          justify-content: center;
          z-index: 9999;
        }
        .logout-popup-content {
          background-color: white;
          padding: 2rem;
          border-radius: 8px;
          text-align: center;
          box-shadow: 0 2px 10px rgba(0, 0, 0, 0.2);
          max-width: 300px;
        }
        .spinner {
          border: 4px solid rgba(0, 0, 0, 0.1);
          width: 36px;
          height: 36px;
          border-radius: 50%;
          border-left-color: #09f;
          animation: spin 1s linear infinite;
          margin: 0 auto 1rem;
        }
        @keyframes spin {
          0% { transform: rotate(0deg); }
          100% { transform: rotate(360deg); }
        }
      `;

      document.head.appendChild(style);
      document.body.appendChild(popup);
    } else {
      // Show existing popup
      popup.style.display = "flex";
    }
  }

  /**
   * Clear all client-side storage
   */
  clearClientStorage() {
    // Clear localStorage
    try {
      localStorage.clear();
    } catch (e) {
      // Error clearing localStorage
    }

    // Clear sessionStorage
    try {
      sessionStorage.clear();
    } catch (e) {
      // Error clearing sessionStorage
    }

    // Remove any auth-related cookies via JavaScript
    this.clearCookies();
  }

  /**
   * Clear cookies via JavaScript
   * Note: This is a best-effort approach as HTTP-only cookies can't be cleared by JavaScript
   */
  clearCookies() {
    const cookies = document.cookie.split(";");

    for (let i = 0; i < cookies.length; i++) {
      const cookie = cookies[i];
      const eqPos = cookie.indexOf("=");
      const name = eqPos > -1 ? cookie.substr(0, eqPos).trim() : cookie.trim();

      // Expire the cookie
      document.cookie =
        name + "=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/;";
      document.cookie =
        name +
        "=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/;domain=" +
        window.location.hostname;
    }
  }
}

// Initialize the secure logout handler
const secureLogout = new SecureLogout();
