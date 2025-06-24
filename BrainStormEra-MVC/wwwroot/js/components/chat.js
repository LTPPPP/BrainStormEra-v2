/**
 * Chat System JavaScript
 * Handles real-time chat functionality using SignalR
 */

class ChatSystem {  constructor() {
    this.connection = null;
    this.currentUserId = null;
    this.receiverId = null;
    this.replyToMessageId = null;
    this.typingTimer = null;
    this.reconnectAttempts = 0;
    this.maxReconnectAttempts = 5;
    this.messageDisplayCount = {}; // Track message display count for debugging

    this.init();
  }

  /**
   * Initialize the chat system
   */
  init() {
    this.currentUserId = document.querySelector(
      'meta[name="user-id"]'
    )?.content;
    this.receiverId = this.getReceiverIdFromUrl();

    if (this.currentUserId) {
      this.setupEventListeners();
      this.initializeSignalR();
    }
  }

  /**
   * Get receiver ID from URL parameters
   */
  getReceiverIdFromUrl() {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get("userId");
  }

  /**
   * Setup event listeners for chat interface
   */
  setupEventListeners() {
    // Message input handlers
    const messageInput = document.getElementById("messageInput");
    const sendButton = document.getElementById("sendButton");

    if (messageInput) {
      messageInput.addEventListener("keypress", (e) => {
        if (e.key === "Enter" && !e.shiftKey) {
          e.preventDefault();
          this.sendMessage();
        } else {
          this.handleTyping();
        }
      });

      messageInput.addEventListener("input", () => {
        this.handleTyping();
      });
    }

    if (sendButton) {
      sendButton.addEventListener("click", () => this.sendMessage());
    }

    // Window focus/blur handlers for read receipts
    window.addEventListener("focus", () => {
      this.markVisibleMessagesAsRead();
    });

    // Notification permission request
    this.requestNotificationPermission();
  }

  /**
   * Initialize SignalR connection
   */
  async initializeSignalR() {
    try {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl("/chatHub")
        .withAutomaticReconnect()
        .build();

      this.setupSignalRHandlers();
      await this.connection.start();

      console.log("SignalR Connected");
      this.reconnectAttempts = 0;

      if (this.receiverId) {
        this.loadMessages();
      }
    } catch (err) {
      console.error("SignalR Connection Error:", err);
      this.handleConnectionError();
    }
  }
  /**
   * Setup SignalR event handlers
   */
  setupSignalRHandlers() {
    // Clear any existing handlers to prevent duplicates
    this.connection.off("ReceiveMessage");
    this.connection.off("MessageSent");
    this.connection.off("MessageRead");
    this.connection.off("UserStartedTyping");
    this.connection.off("UserStoppedTyping");
    this.connection.off("MessageError");

    this.connection.on("ReceiveMessage", (message) => {
      console.log("ReceiveMessage event:", {
        messageId: message.messageId,
        senderId: message.senderId,
        receiverId: message.receiverId,
        currentUserId: this.currentUserId,
        chatWithUserId: this.receiverId,
      });

      // Chỉ hiển thị tin nhắn nếu tôi là người nhận VÀ tin nhắn này từ người tôi đang chat
      if (
        message.receiverId === this.currentUserId &&
        message.senderId === this.receiverId
      ) {
        console.log("Displaying received message");
        this.displayMessage(message, false);
        this.markMessageAsRead(message.messageId);
        this.scrollToBottom();
        this.showNotification(message);
      } else {
        console.log(
          "Not displaying message - not for me or not from current chat partner"
        );
        this.updateUnreadCounts();
      }
    });
    this.connection.on("MessageSent", (message) => {
      console.log("MessageSent confirmation:", message);

      // Tìm và thay thế tin nhắn tạm bằng tin nhắn thật từ server
      const tempMessages = document.querySelectorAll(
        '[data-message-id^="temp_"]'
      );
      if (tempMessages.length > 0) {
        console.log("Replacing temporary message with real message");
        const lastTempMessage = tempMessages[tempMessages.length - 1];
        lastTempMessage.setAttribute("data-message-id", message.messageId);

        // Cập nhật timestamp và status icon
        const timeElement = lastTempMessage.querySelector(".message-time");
        if (timeElement) {
          timeElement.innerHTML = `
            ${this.formatTime(message.createdAt)}
            <i class="fas fa-check text-success ms-1" data-message-id="${
              message.messageId
            }" title="Sent"></i>
          `;
        }

        // Thêm các action buttons cho tin nhắn đã gửi thành công
        const actionsElement =
          lastTempMessage.querySelector(".message-actions");
        if (actionsElement) {
          const replyButton = actionsElement.querySelector("button");
          if (replyButton) {
            actionsElement.innerHTML = `
              <button class="btn btn-sm" onclick="chatSystem.replyToMessage('${
                message.messageId
              }', '${this.escapeHtml(message.content)}')" title="Reply">
                  <i class="fas fa-reply"></i>
              </button>
              <button class="btn btn-sm" onclick="chatSystem.editMessage('${
                message.messageId
              }')" title="Edit">
                  <i class="fas fa-edit"></i>
              </button>
              <button class="btn btn-sm" onclick="chatSystem.deleteMessage('${
                message.messageId
              }')" title="Delete">
                  <i class="fas fa-trash"></i>
              </button>
            `;
          }
        }
      }
    });

    this.connection.on("MessageRead", (data) => {
      this.updateMessageStatus(data.messageId, "read");
    });

    this.connection.on("UserStartedTyping", (senderId) => {
      if (senderId === this.receiverId) {
        this.showTypingIndicator();
      }
    });

    this.connection.on("UserStoppedTyping", (senderId) => {
      if (senderId === this.receiverId) {
        this.hideTypingIndicator();
      }
    });
    this.connection.on("MessageError", (error) => {
      this.showError("Failed to send message: " + error);

      // Xóa tin nhắn tạm nếu có lỗi
      const tempMessages = document.querySelectorAll(
        '[data-message-id^="temp_"]'
      );
      if (tempMessages.length > 0) {
        const lastTempMessage = tempMessages[tempMessages.length - 1];
        lastTempMessage.remove();
      }
    });

    this.connection.onreconnecting(() => {
      console.log("SignalR reconnecting...");
      this.showConnectionStatus("Reconnecting...", "warning");
    });

    this.connection.onreconnected(() => {
      console.log("SignalR reconnected");
      this.showConnectionStatus("Connected", "success");
      this.reconnectAttempts = 0;
    });

    this.connection.onclose(() => {
      console.log("SignalR connection closed");
      this.showConnectionStatus("Disconnected", "danger");
      this.handleConnectionError();
    });
  }

  /**
   * Handle connection errors and attempt reconnection
   */
  async handleConnectionError() {
    if (this.reconnectAttempts < this.maxReconnectAttempts) {
      this.reconnectAttempts++;
      const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), 30000);

      console.log(
        `Attempting to reconnect in ${delay}ms (attempt ${this.reconnectAttempts})`
      );

      setTimeout(() => {
        this.initializeSignalR();
      }, delay);
    } else {
      this.showError(
        "Unable to connect to chat server. Please refresh the page."
      );
    }
  }

  /**
   * Load conversation messages
   */
  async loadMessages() {
    try {
      this.showLoadingMessages();

      const response = await fetch(
        `/Chat/GetMessages?receiverId=${this.receiverId}`
      );
      const data = await response.json();

      this.hideLoadingMessages();

      if (data.success) {
        const container = document.getElementById("messagesContainer");
        if (container) {
          container.innerHTML = "";

          data.messages.forEach((message) => {
            this.displayMessage(
              message,
              message.senderId === this.currentUserId
            );
          });

          this.scrollToBottom();
          this.markVisibleMessagesAsRead();
        }
      }
    } catch (error) {
      console.error("Error loading messages:", error);
      this.showError("Failed to load messages");
      this.hideLoadingMessages();
    }
  }  /**
   * Display a message in the chat
   */
  displayMessage(message, isSent) {
    // Debug tracking
    if (!this.messageDisplayCount[message.messageId]) {
      this.messageDisplayCount[message.messageId] = 0;
    }
    this.messageDisplayCount[message.messageId]++;
    
    console.log(`Displaying message ${message.messageId} - attempt #${this.messageDisplayCount[message.messageId]}`, {
      messageId: message.messageId,
      content: message.content,
      isSent: isSent,
      callStack: new Error().stack
    });

    const container = document.getElementById("messagesContainer");
    if (!container) return;

    // Kiểm tra xem tin nhắn đã tồn tại chưa để tránh duplicate
    const existingMessage = container.querySelector(`[data-message-id="${message.messageId}"]`);
    if (existingMessage) {
      console.log("Message already exists, skipping:", message.messageId);
      return;
    }

    const messageDiv = document.createElement("div");
    messageDiv.className = `message-bubble ${
      isSent ? "sent slide-in-right" : "received slide-in-left"
    }`;
    messageDiv.setAttribute("data-message-id", message.messageId);

    let replyHtml = "";
    if (message.replyToMessageId) {
      replyHtml = `<div class="reply-line">
                <i class="fas fa-reply me-1"></i>Replying to a message
            </div>`;
    }
    const editedHtml = message.isEdited
      ? '<small class="text-muted">(edited)</small>'
      : "";

    // Kiểm tra xem đây có phải tin nhắn tạm không
    const isTemporary =
      message.messageId && message.messageId.startsWith("temp_");
    const statusIcon = isSent
      ? isTemporary
        ? `<i class="fas fa-clock text-muted ms-1" title="Sending..."></i>`
        : `<i class="fas fa-check message-status ms-1" data-message-id="${message.messageId}"></i>`
      : "";

    messageDiv.innerHTML = `
            ${replyHtml}
            <div class="message-content">${this.escapeHtml(
              message.content
            )} ${editedHtml}</div>
            <div class="message-time">
                ${this.formatTime(message.createdAt)}
                ${statusIcon}
            </div>            <div class="message-actions">
                <button class="btn btn-sm" onclick="chatSystem.replyToMessage('${
                  message.messageId
                }', '${this.escapeHtml(message.content)}')" title="Reply">
                    <i class="fas fa-reply"></i>
                </button>
                ${
                  isSent && !isTemporary
                    ? `
                    <button class="btn btn-sm" onclick="chatSystem.editMessage('${message.messageId}')" title="Edit">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button class="btn btn-sm" onclick="chatSystem.deleteMessage('${message.messageId}')" title="Delete">
                        <i class="fas fa-trash"></i>
                    </button>
                `
                    : ""
                }
            </div>
        `;

    container.appendChild(messageDiv);
  }
  /**
   * Send a message
   */
  async sendMessage() {
    const input = document.getElementById("messageInput");
    if (!input) return;

    const message = input.value.trim();
    if (message === "" || !this.receiverId) return;

    const tempMessageId = "temp_" + Date.now();

    try {
      if (
        this.connection &&
        this.connection.state === signalR.HubConnectionState.Connected
      ) {
        // Hiển thị tin nhắn ngay lập tức cho người gửi
        this.displayMessage(
          {
            messageId: tempMessageId,
            senderId: this.currentUserId,
            receiverId: this.receiverId,
            content: message,
            messageType: "text",
            replyToMessageId: this.replyToMessageId,
            createdAt: new Date().toISOString(),
            isEdited: false,
            senderName: "You",
            senderAvatar: null,
          },
          true
        );

        this.scrollToBottom();

        await this.connection.invoke(
          "SendMessage",
          this.receiverId,
          message,
          this.replyToMessageId
        );

        input.value = "";
        this.cancelReply();
      } else {
        throw new Error("Not connected to chat server");
      }
    } catch (error) {
      console.error("Error sending message:", error);
      this.showError("Failed to send message. Please try again.");

      // Xóa tin nhắn tạm nếu gửi thất bại
      const tempMessage = document.querySelector(
        `[data-message-id="${tempMessageId}"]`
      );
      if (tempMessage) {
        tempMessage.remove();
      }
    }
  }

  /**
   * Mark message as read
   */
  async markMessageAsRead(messageId) {
    try {
      await fetch("/Chat/MarkAsRead", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ messageId: messageId }),
      });
    } catch (error) {
      console.error("Error marking message as read:", error);
    }
  }

  /**
   * Mark all visible messages as read
   */
  markVisibleMessagesAsRead() {
    const messages = document.querySelectorAll(".message-bubble.received");
    messages.forEach((messageElement) => {
      const messageId = messageElement.getAttribute("data-message-id");
      if (messageId) {
        this.markMessageAsRead(messageId);
      }
    });
  }

  /**
   * Reply to a message
   */
  replyToMessage(messageId, content) {
    this.replyToMessageId = messageId;
    const replyPreview = document.getElementById("replyPreview");
    const replyContent = document.getElementById("replyToContent");
    const messageInput = document.getElementById("messageInput");

    if (replyPreview && replyContent && messageInput) {
      replyPreview.style.display = "block";
      replyContent.textContent =
        content.substring(0, 50) + (content.length > 50 ? "..." : "");
      messageInput.focus();
    }
  }

  /**
   * Cancel reply
   */
  cancelReply() {
    this.replyToMessageId = null;
    const replyPreview = document.getElementById("replyPreview");
    if (replyPreview) {
      replyPreview.style.display = "none";
    }
  }

  /**
   * Edit a message
   */
  async editMessage(messageId) {
    const messageElement = document.querySelector(
      `[data-message-id="${messageId}"]`
    );
    if (!messageElement) return;

    const contentElement = messageElement.querySelector(".message-content");
    const currentContent = contentElement.textContent.trim();

    const newContent = prompt("Edit message:", currentContent);
    if (newContent && newContent !== currentContent) {
      try {
        const response = await fetch("/Chat/EditMessage", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            messageId: messageId,
            newContent: newContent,
          }),
        });

        const result = await response.json();
        if (result.success) {
          contentElement.innerHTML =
            this.escapeHtml(newContent) +
            ' <small class="text-muted">(edited)</small>';
        } else {
          this.showError("Failed to edit message");
        }
      } catch (error) {
        console.error("Error editing message:", error);
        this.showError("Failed to edit message");
      }
    }
  }

  /**
   * Delete a message
   */
  async deleteMessage(messageId) {
    if (!confirm("Are you sure you want to delete this message?")) return;

    try {
      const response = await fetch("/Chat/DeleteMessage", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ messageId: messageId }),
      });

      const result = await response.json();
      if (result.success) {
        const messageElement = document.querySelector(
          `[data-message-id="${messageId}"]`
        );
        if (messageElement) {
          messageElement.style.opacity = "0.5";
          messageElement.querySelector(".message-content").innerHTML =
            "<em>This message was deleted</em>";
          messageElement.querySelector(".message-actions").style.display =
            "none";
        }
      } else {
        this.showError("Failed to delete message");
      }
    } catch (error) {
      console.error("Error deleting message:", error);
      this.showError("Failed to delete message");
    }
  }

  /**
   * Handle typing indicators
   */
  handleTyping() {
    if (this.connection && this.receiverId) {
      this.connection.invoke("StartTyping", this.receiverId);

      clearTimeout(this.typingTimer);
      this.typingTimer = setTimeout(() => {
        this.connection.invoke("StopTyping", this.receiverId);
      }, 1000);
    }
  }

  /**
   * Show typing indicator
   */
  showTypingIndicator() {
    const indicator = document.querySelector(".typing-indicator");
    if (indicator) {
      indicator.style.display = "block";
    }
  }

  /**
   * Hide typing indicator
   */
  hideTypingIndicator() {
    const indicator = document.querySelector(".typing-indicator");
    if (indicator) {
      indicator.style.display = "none";
    }
  }

  /**
   * Update message status (read/delivered)
   */
  updateMessageStatus(messageId, status) {
    const statusIcon = document.querySelector(
      `[data-message-id="${messageId}"] .message-status`
    );
    if (statusIcon && status === "read") {
      statusIcon.className = "fas fa-check-double message-status ms-1 read";
    }
  }

  /**
   * Show notification for new messages
   */
  showNotification(message) {
    if (!document.hasFocus() && Notification.permission === "granted") {
      const notification = new Notification(
        `New message from ${message.senderName}`,
        {
          body: message.content.substring(0, 100),
          icon: "/SharedMedia/logo/logowithoutbackground.png",
          tag: "chat-message",
        }
      );

      notification.onclick = () => {
        window.focus();
        notification.close();
      };

      setTimeout(() => notification.close(), 5000);
    }
  }

  /**
   * Request notification permission
   */
  requestNotificationPermission() {
    if ("Notification" in window && Notification.permission === "default") {
      Notification.requestPermission();
    }
  }

  /**
   * Update unread message counts
   */
  updateUnreadCounts() {
    const userItems = document.querySelectorAll(".chat-user-item");
    userItems.forEach((item) => {
      const userId = item.getAttribute("data-user-id");
      const unreadBadge = item.querySelector(".unread-count");

      if (userId && unreadBadge) {
        fetch(`/Chat/GetUnreadCount?senderId=${userId}`)
          .then((response) => response.json())
          .then((data) => {
            if (data.success && data.count > 0) {
              unreadBadge.textContent = data.count;
              unreadBadge.style.display = "flex";
            } else {
              unreadBadge.style.display = "none";
            }
          })
          .catch((error) =>
            console.error("Error fetching unread count:", error)
          );
      }
    });
  }

  /**
   * Show loading messages indicator
   */
  showLoadingMessages() {
    const container = document.getElementById("messagesContainer");
    if (container) {
      container.innerHTML = `
                <div class="loading-messages">
                    <div class="loading-spinner"></div>
                </div>
            `;
    }
  }

  /**
   * Hide loading messages indicator
   */
  hideLoadingMessages() {
    const loadingElement = document.querySelector(".loading-messages");
    if (loadingElement) {
      loadingElement.remove();
    }
  }

  /**
   * Show connection status
   */
  showConnectionStatus(message, type) {
    // You can implement a status bar or toast notification here
    console.log(`Connection Status: ${message} (${type})`);
  }

  /**
   * Show error message
   */
  showError(message) {
    // You can implement a toast notification or alert here
    console.error("Chat Error:", message);
    alert(message); // Temporary - replace with better UI
  }

  /**
   * Scroll to bottom of messages
   */
  scrollToBottom() {
    const chatMessages = document.getElementById("chatMessages");
    if (chatMessages) {
      chatMessages.scrollTop = chatMessages.scrollHeight;
    }
  }

  /**
   * Format timestamp for display
   */
  formatTime(timestamp) {
    const date = new Date(timestamp);
    const now = new Date();
    const diffInHours = (now - date) / (1000 * 60 * 60);

    if (diffInHours < 24) {
      return date.toLocaleTimeString([], {
        hour: "2-digit",
        minute: "2-digit",
      });
    } else if (diffInHours < 168) {
      // 7 days
      return date.toLocaleDateString([], {
        weekday: "short",
        hour: "2-digit",
        minute: "2-digit",
      });
    } else {
      return date.toLocaleDateString([], {
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      });
    }
  }

  /**
   * Escape HTML to prevent XSS
   */
  escapeHtml(text) {
    const div = document.createElement("div");
    div.textContent = text;
    return div.innerHTML;
  }

  /**
   * Open chat with specific user
   */
  openChat(userId) {
    window.location.href = `/Chat/Conversation?userId=${userId}`;
  }

  /**
   * Cleanup when leaving page
   */
  cleanup() {
    if (this.connection) {
      this.connection.stop();
    }
    clearTimeout(this.typingTimer);
  }
}

// Global instance
let chatSystem;

// Initialize when DOM is loaded
document.addEventListener("DOMContentLoaded", function () {
  chatSystem = new ChatSystem();
});

// Cleanup on page unload
window.addEventListener("beforeunload", function () {
  if (chatSystem) {
    chatSystem.cleanup();
  }
});

// Global function for opening chat (used in HTML)
function openChat(userId) {
  if (chatSystem) {
    chatSystem.openChat(userId);
  } else {
    window.location.href = `/Chat/Conversation?userId=${userId}`;
  }
}
