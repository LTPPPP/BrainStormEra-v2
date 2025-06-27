/**
 * Chat URL Manager
 * Handles generation and sharing of hashed chat URLs
 */
class ChatUrlManager {
  constructor() {
    this.baseUrl = window.location.origin;
    this.currentUserId = this.getCurrentUserId();
    this.init();
  }

  init() {
    this.bindEvents();
  }

  bindEvents() {
    // Share conversation button
    document.addEventListener("click", (e) => {
      if (
        e.target.matches(".share-conversation-btn") ||
        e.target.closest(".share-conversation-btn")
      ) {
        const btn = e.target.matches(".share-conversation-btn")
          ? e.target
          : e.target.closest(".share-conversation-btn");
        const userId = btn.getAttribute("data-user-id");
        if (userId) {
          this.showShareModal(userId);
        }
      }
    });

    // Share message button
    document.addEventListener("click", (e) => {
      if (
        e.target.matches(".share-message-btn") ||
        e.target.closest(".share-message-btn")
      ) {
        const btn = e.target.matches(".share-message-btn")
          ? e.target
          : e.target.closest(".share-message-btn");
        const messageId = btn.getAttribute("data-message-id");
        if (messageId) {
          this.generateMessageUrl(messageId);
        }
      }
    });

    // Copy URL buttons
    document.addEventListener("click", (e) => {
      if (
        e.target.matches(".copy-url-btn") ||
        e.target.closest(".copy-url-btn")
      ) {
        const btn = e.target.matches(".copy-url-btn")
          ? e.target
          : e.target.closest(".copy-url-btn");
        const url = btn.getAttribute("data-url");
        if (url) {
          this.copyToClipboard(url);
        }
      }
    });
  }

  getCurrentUserId() {
    const userIdMeta = document.querySelector('meta[name="user-id"]');
    return userIdMeta ? userIdMeta.getAttribute("content") : null;
  }

  /**
   * Generate URLs for a conversation
   * @param {string} userId - Target user ID
   * @returns {Promise<Object>} URL bundle
   */
  async generateConversationUrls(userId) {
    try {
      const response = await fetch(`/Chat/GenerateUrls?userId=${userId}`, {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
      });

      const result = await response.json();
      if (result.success) {
        return result.urls;
      } else {
        throw new Error(result.message || "Failed to generate URLs");
      }
    } catch (error) {
      console.error("Error generating conversation URLs:", error);
      this.showError("Failed to generate conversation URLs");
      return null;
    }
  }

  /**
   * Generate URL for a specific message
   * @param {string} messageId - Message ID
   * @returns {Promise<string>} Message URL
   */
  async generateMessageUrl(messageId) {
    try {
      const response = await fetch("/Chat/GenerateMessageUrl", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ messageId: messageId }),
      });

      const result = await response.json();
      if (result.success) {
        this.showUrlShareModal("Message Link", result.url);
        return result.url;
      } else {
        throw new Error(result.message || "Failed to generate message URL");
      }
    } catch (error) {
      console.error("Error generating message URL:", error);
      this.showError("Failed to generate message URL");
      return null;
    }
  }

  /**
   * Generate quick chat URL
   * @param {string} targetUserId - Target user ID
   * @returns {Promise<string>} Quick chat URL
   */
  async generateQuickChatUrl(targetUserId) {
    try {
      const response = await fetch("/Chat/GenerateQuickUrl", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ targetUserId: targetUserId }),
      });

      const result = await response.json();
      if (result.success) {
        return result.url;
      } else {
        throw new Error(result.message || "Failed to generate quick chat URL");
      }
    } catch (error) {
      console.error("Error generating quick chat URL:", error);
      this.showError("Failed to generate quick chat URL");
      return null;
    }
  }

  /**
   * Show share modal for conversation
   * @param {string} userId - Target user ID
   */
  async showShareModal(userId) {
    const urls = await this.generateConversationUrls(userId);
    if (!urls) return;

    const modalHtml = `
            <div class="modal fade" id="shareUrlModal" tabindex="-1" aria-labelledby="shareUrlModalLabel" aria-hidden="true">
                <div class="modal-dialog modal-lg">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="shareUrlModalLabel">
                                <i class="fas fa-share-alt me-2"></i>Share Conversation
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body">
                            <div class="row g-3">
                                <!-- Direct URL -->
                                <div class="col-12">
                                    <div class="card">
                                        <div class="card-header">
                                            <h6 class="mb-0">
                                                <i class="fas fa-link me-2"></i>Direct Link
                                            </h6>
                                            <small class="text-muted">Simple URL for quick access</small>
                                        </div>
                                        <div class="card-body">
                                            <div class="input-group">
                                                <input type="text" class="form-control" value="${
                                                  urls.directUrl
                                                }" readonly>
                                                <button class="btn btn-outline-primary copy-url-btn" data-url="${
                                                  urls.directUrl
                                                }">
                                                    <i class="fas fa-copy"></i>
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <!-- Secure URL -->
                                <div class="col-12">
                                    <div class="card">
                                        <div class="card-header">
                                            <h6 class="mb-0">
                                                <i class="fas fa-shield-alt me-2"></i>Secure Link
                                            </h6>
                                            <small class="text-muted">Encrypted URL with expiration (24 hours)</small>
                                        </div>
                                        <div class="card-body">
                                            <div class="input-group">
                                                <input type="text" class="form-control" value="${
                                                  urls.secureUrl
                                                }" readonly>
                                                <button class="btn btn-outline-primary copy-url-btn" data-url="${
                                                  urls.secureUrl
                                                }">
                                                    <i class="fas fa-copy"></i>
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <!-- Quick URL -->
                                <div class="col-12">
                                    <div class="card">
                                        <div class="card-header">
                                            <h6 class="mb-0">
                                                <i class="fas fa-bolt me-2"></i>Quick Access
                                            </h6>
                                            <small class="text-muted">One-click conversation starter</small>
                                        </div>
                                        <div class="card-body">
                                            <div class="input-group">
                                                <input type="text" class="form-control" value="${
                                                  urls.quickUrl
                                                }" readonly>
                                                <button class="btn btn-outline-primary copy-url-btn" data-url="${
                                                  urls.quickUrl
                                                }">
                                                    <i class="fas fa-copy"></i>
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <!-- URL Info -->
                                <div class="col-12">
                                    <div class="alert alert-info">
                                        <h6><i class="fas fa-info-circle me-2"></i>URL Information</h6>
                                        <ul class="mb-0">
                                            <li><strong>Generated:</strong> ${new Date(
                                              urls.generatedAt
                                            ).toLocaleString()}</li>
                                            <li><strong>Expires:</strong> ${new Date(
                                              urls.expiresAt
                                            ).toLocaleString()}</li>
                                            <li><strong>Conversation ID:</strong> ${
                                              urls.conversationId
                                            }</li>
                                        </ul>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                            <button type="button" class="btn btn-primary" onclick="chatUrlManager.shareViaSystem('${
                              urls.directUrl
                            }')">
                                <i class="fas fa-share me-2"></i>Share via System
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

    // Remove existing modal if present
    const existingModal = document.getElementById("shareUrlModal");
    if (existingModal) {
      existingModal.remove();
    }

    // Add modal to page
    document.body.insertAdjacentHTML("beforeend", modalHtml);

    // Show modal
    const modal = new bootstrap.Modal(document.getElementById("shareUrlModal"));
    modal.show();

    // Clean up after modal is hidden
    document
      .getElementById("shareUrlModal")
      .addEventListener("hidden.bs.modal", function () {
        this.remove();
      });
  }

  /**
   * Show simple URL share modal
   * @param {string} title - Modal title
   * @param {string} url - URL to share
   */
  showUrlShareModal(title, url) {
    const modalHtml = `
            <div class="modal fade" id="simpleShareModal" tabindex="-1" aria-labelledby="simpleShareModalLabel" aria-hidden="true">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="simpleShareModalLabel">
                                <i class="fas fa-share-alt me-2"></i>${title}
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body">
                            <div class="input-group">
                                <input type="text" class="form-control" value="${url}" readonly id="shareUrl">
                                <button class="btn btn-outline-primary copy-url-btn" data-url="${url}">
                                    <i class="fas fa-copy"></i>
                                </button>
                            </div>
                            <small class="text-muted">This link will expire in 1 hour</small>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                            <button type="button" class="btn btn-primary" onclick="chatUrlManager.shareViaSystem('${url}')">
                                <i class="fas fa-share me-2"></i>Share
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

    // Remove existing modal if present
    const existingModal = document.getElementById("simpleShareModal");
    if (existingModal) {
      existingModal.remove();
    }

    // Add modal to page
    document.body.insertAdjacentHTML("beforeend", modalHtml);

    // Show modal
    const modal = new bootstrap.Modal(
      document.getElementById("simpleShareModal")
    );
    modal.show();

    // Clean up after modal is hidden
    document
      .getElementById("simpleShareModal")
      .addEventListener("hidden.bs.modal", function () {
        this.remove();
      });
  }

  /**
   * Copy text to clipboard
   * @param {string} text - Text to copy
   */
  async copyToClipboard(text) {
    try {
      await navigator.clipboard.writeText(text);
      this.showSuccess("URL copied to clipboard!");
    } catch (err) {
      // Fallback for older browsers
      const textArea = document.createElement("textarea");
      textArea.value = text;
      document.body.appendChild(textArea);
      textArea.focus();
      textArea.select();
      try {
        document.execCommand("copy");
        this.showSuccess("URL copied to clipboard!");
      } catch (fallbackErr) {
        this.showError("Failed to copy URL");
      }
      document.body.removeChild(textArea);
    }
  }

  /**
   * Share via system (Web Share API)
   * @param {string} url - URL to share
   */
  async shareViaSystem(url) {
    if (navigator.share) {
      try {
        await navigator.share({
          title: "BrainStormEra Chat",
          text: "Join me for a chat on BrainStormEra",
          url: url,
        });
      } catch (err) {
        if (err.name !== "AbortError") {
          this.copyToClipboard(url);
        }
      }
    } else {
      this.copyToClipboard(url);
    }
  }

  /**
   * Add share button to conversation header
   * @param {string} userId - Target user ID
   */
  addShareButtonToHeader(userId) {
    const chatHeader = document.querySelector(".chat-header");
    if (!chatHeader || document.querySelector(".share-conversation-btn"))
      return;

    const shareButton = document.createElement("button");
    shareButton.className =
      "btn btn-outline-primary btn-sm share-conversation-btn ms-2";
    shareButton.setAttribute("data-user-id", userId);
    shareButton.innerHTML = '<i class="fas fa-share-alt"></i>';
    shareButton.title = "Share Conversation";

    chatHeader.appendChild(shareButton);
  }

  /**
   * Add share button to messages
   */
  addShareButtonsToMessages() {
    const sentMessages = document.querySelectorAll(".message-bubble.sent");
    sentMessages.forEach((message) => {
      if (message.querySelector(".share-message-btn")) return;

      const messageId = message.getAttribute("data-message-id");
      if (!messageId || messageId.startsWith("temp_")) return;

      const messageActions = message.querySelector(".message-actions");
      if (messageActions) {
        const shareButton = document.createElement("button");
        shareButton.className = "btn btn-sm share-message-btn";
        shareButton.setAttribute("data-message-id", messageId);
        shareButton.innerHTML = '<i class="fas fa-share-alt"></i>';
        shareButton.title = "Share Message";

        messageActions.appendChild(shareButton);
      }
    });
  }

  /**
   * Show success message
   * @param {string} message - Success message
   */
  showSuccess(message) {
    // You can integrate with your existing toast notification system
    if (typeof showToast === "function") {
      showToast(message, "success");
    } else {
      alert(message);
    }
  }

  /**
   * Show error message
   * @param {string} message - Error message
   */
  showError(message) {
    // You can integrate with your existing toast notification system
    if (typeof showToast === "function") {
      showToast(message, "error");
    } else {
      alert(message);
    }
  }

  /**
   * Initialize share functionality for current page
   */
  initializeForCurrentPage() {
    // Add share button to conversation header
    const urlParams = new URLSearchParams(window.location.search);
    const userId = urlParams.get("userId");
    if (userId) {
      this.addShareButtonToHeader(userId);
    }

    // Add share buttons to existing messages
    this.addShareButtonsToMessages();

    // Watch for new messages and add share buttons
    const observer = new MutationObserver((mutations) => {
      mutations.forEach((mutation) => {
        if (mutation.type === "childList") {
          mutation.addedNodes.forEach((node) => {
            if (
              node.nodeType === Node.ELEMENT_NODE &&
              node.matches(".message-bubble.sent")
            ) {
              this.addShareButtonsToMessages();
            }
          });
        }
      });
    });

    const messagesContainer = document.getElementById("messagesContainer");
    if (messagesContainer) {
      observer.observe(messagesContainer, { childList: true, subtree: true });
    }
  }
}

// Global instance
let chatUrlManager;

// Initialize when DOM is loaded
document.addEventListener("DOMContentLoaded", function () {
  chatUrlManager = new ChatUrlManager();

  // Initialize for current page if it's a chat page
  if (window.location.pathname.includes("/Chat/")) {
    chatUrlManager.initializeForCurrentPage();
  }
});

// Export for global access
window.ChatUrlManager = ChatUrlManager;
