class ChatbotManager {
  constructor() {
    console.log("ChatbotManager constructor called");
    this.isOpen = false;
    this.messages = [];
    this.isTyping = false;
    this.conversationHistory = [];
    this.init();
  }

  init() {
    console.log("ChatbotManager init called");
    this.createChatbotHTML();
    this.bindEvents();
    this.loadConversationHistory();
  }
  createChatbotHTML() {
    const chatbotHTML = `
            <div class="chatbot-container">
                <!-- Chatbot Bubble -->
                <div class="chatbot-bubble" id="chatbot-bubble">
                    <img src="/img/logo/logowithoutbackground.png" alt="BrainStorm Bot" />
                </div>

                <!-- Chatbot Window -->
                <div class="chatbot-window" id="chatbot-window">
                    <!-- Header -->
                    <div class="chatbot-header">
                        <div class="bot-info">
                            <img src="/img/logo/logowithoutbackground.png" alt="Bot" class="bot-avatar" />
                            <div>
                                <div class="bot-name">BrainStorm Bot</div>
                                <div class="bot-status">Trực tuyến</div>
                            </div>
                        </div>
                        <button class="chatbot-close" id="chatbot-close">
                            <i class="fas fa-times"></i>
                        </button>
                    </div>

                    <!-- Messages Area -->
                    <div class="chatbot-messages" id="chatbot-messages">
                        <div class="message">
                            <img src="/img/logo/logowithoutbackground.png" alt="Bot" class="message-avatar" />
                            <div class="message-content">
                                Xin chào! Tôi là BrainStorm Bot, trợ lý AI của bạn. Tôi có thể giúp bạn:
                                <br>• Trả lời câu hỏi về bài học
                                <br>• Giải thích các khái niệm khó hiểu
                                <br>• Hướng dẫn sử dụng nền tảng
                                <br>• Hỗ trợ học tập
                                <br><br>Hãy hỏi tôi bất cứ điều gì bạn muốn biết! 😊
                            </div>
                        </div>
                    </div>

                    <!-- Input Area -->
                    <div class="chatbot-input">
                        <textarea 
                            class="chatbot-input-field" 
                            id="chatbot-input" 
                            placeholder="Nhập câu hỏi của bạn..."
                            rows="1"
                        ></textarea>
                        <button class="chatbot-send-btn" id="chatbot-send">
                            <i class="fas fa-paper-plane"></i>
                        </button>
                    </div>
                </div>
            </div>
        `;

    // Insert chatbot HTML into the designated container or body
    const chatbotRoot = document.getElementById("chatbot-root");
    if (chatbotRoot) {
      chatbotRoot.innerHTML = chatbotHTML;
    } else {
      document.body.insertAdjacentHTML("beforeend", chatbotHTML);
    }
  }

  bindEvents() {
    const bubble = document.getElementById("chatbot-bubble");
    const window = document.getElementById("chatbot-window");
    const closeBtn = document.getElementById("chatbot-close");
    const sendBtn = document.getElementById("chatbot-send");
    const input = document.getElementById("chatbot-input");

    // Toggle chatbot window
    bubble.addEventListener("click", () => this.toggleChatbot());
    closeBtn.addEventListener("click", () => this.closeChatbot());

    // Send message
    sendBtn.addEventListener("click", () => this.sendMessage());

    // Handle enter key
    input.addEventListener("keydown", (e) => {
      if (e.key === "Enter" && !e.shiftKey) {
        e.preventDefault();
        this.sendMessage();
      }
    });

    // Auto-resize textarea
    input.addEventListener("input", () => this.autoResizeTextarea(input));

    // Click outside to close
    document.addEventListener("click", (e) => {
      if (
        this.isOpen &&
        !window.contains(e.target) &&
        !bubble.contains(e.target)
      ) {
        this.closeChatbot();
      }
    });
  }

  toggleChatbot() {
    const window = document.getElementById("chatbot-window");
    this.isOpen = !this.isOpen;

    if (this.isOpen) {
      window.classList.add("show");
      this.scrollToBottom();
      document.getElementById("chatbot-input").focus();
    } else {
      window.classList.remove("show");
    }
  }

  closeChatbot() {
    const window = document.getElementById("chatbot-window");
    this.isOpen = false;
    window.classList.remove("show");
  }
  async sendMessage() {
    const input = document.getElementById("chatbot-input");
    const message = input.value.trim();

    if (!message || this.isTyping) return;

    // Clear input
    input.value = "";
    this.autoResizeTextarea(input);

    // Add user message
    this.addMessage(message, "user");

    // Show typing indicator
    this.showTypingIndicator();

    try {
      // Get current page context
      const context = this.getCurrentPageContext();
      const pageInfo = this.getPageInfo();

      // Send to API
      const response = await fetch("/api/chatbot/chat", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken: this.getAntiForgeryToken(),
        },
        body: JSON.stringify({
          message: message,
          context: context,
          pagePath: pageInfo.path,
          courseId: pageInfo.courseId,
          chapterId: pageInfo.chapterId,
          lessonId: pageInfo.lessonId,
        }),
      });

      const data = await response.json();

      if (response.ok) {
        this.addMessage(data.message, "bot", data.conversationId);
      } else {
        this.handleError(new Error(data.error || "API Error"), message);
      }
    } catch (error) {
      this.handleError(error, message);
    } finally {
      this.hideTypingIndicator();
    }
  }

  // Enhanced message formatting with better parsing
  formatMessage(message) {
    // Convert line breaks to <br>
    message = message.replace(/\n/g, "<br>");

    // Convert **bold** to <strong>
    message = message.replace(/\*\*(.*?)\*\*/g, "<strong>$1</strong>");

    // Convert *italic* to <em>
    message = message.replace(/\*(.*?)\*/g, "<em>$1</em>");

    // Convert code blocks
    message = message.replace(/```(.*?)```/gs, "<pre><code>$1</code></pre>");
    message = message.replace(/`(.*?)`/g, "<code>$1</code>");

    // Convert numbered lists
    message = message.replace(/^\d+\.\s(.+)$/gm, "<li>$1</li>");
    message = message.replace(/(<li>.*<\/li>)/s, "<ol>$1</ol>");

    // Convert bullet points
    message = message.replace(/^[•\-\*]\s(.+)$/gm, "<li>$1</li>");
    message = message.replace(/(<li>.*<\/li>)/s, "<ul>$1</ul>");

    return message;
  }

  // Add feedback buttons to bot messages
  addMessage(content, sender, conversationId = null) {
    const messagesContainer = document.getElementById("chatbot-messages");
    const isUser = sender === "user";
    const timestamp = new Date().toLocaleTimeString("vi-VN", {
      hour: "2-digit",
      minute: "2-digit",
    });

    const avatarSrc = isUser
      ? this.getUserAvatar() || "/img/default-avatar.svg"
      : "/img/logo/logowithoutbackground.png";

    const feedbackButtons =
      !isUser && conversationId
        ? `
            <div class="message-actions">
                <button class="action-btn" onclick="window.chatbotManager.rateFeedback('${conversationId}', 5)" title="Hữu ích">
                    <i class="fas fa-thumbs-up"></i>
                </button>
                <button class="action-btn" onclick="window.chatbotManager.rateFeedback('${conversationId}', 1)" title="Không hữu ích">
                    <i class="fas fa-thumbs-down"></i>
                </button>
                <button class="action-btn" onclick="window.chatbotManager.copyMessage('${content}')" title="Sao chép">
                    <i class="fas fa-copy"></i>
                </button>
            </div>
        `
        : "";

    const messageHTML = `
            <div class="message ${
              isUser ? "user" : ""
            }" data-conversation-id="${conversationId || ""}">
                <img src="${avatarSrc}" alt="${
      isUser ? "User" : "Bot"
    }" class="message-avatar" />
                <div>
                    <div class="message-content">${this.formatMessage(
                      content
                    )}</div>
                    <div class="message-time">${timestamp}</div>
                    ${feedbackButtons}
                </div>
            </div>
        `;

    messagesContainer.insertAdjacentHTML("beforeend", messageHTML);
    this.scrollToBottom();
  }

  // Rate feedback for bot response
  async rateFeedback(conversationId, rating) {
    try {
      const response = await fetch("/api/chatbot/feedback", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken: this.getAntiForgeryToken(),
        },
        body: JSON.stringify({
          conversationId: conversationId,
          rating: rating,
        }),
      });

      if (response.ok) {
        this.showToast(
          rating >= 4 ? "Cảm ơn phản hồi tích cực!" : "Cảm ơn phản hồi của bạn!"
        );

        // Disable feedback buttons for this message
        const messageElement = document.querySelector(
          `[data-conversation-id="${conversationId}"]`
        );
        if (messageElement) {
          const actionButtons = messageElement.querySelectorAll(".action-btn");
          actionButtons.forEach((btn) => {
            if (btn.innerHTML.includes("thumbs")) {
              btn.disabled = true;
              btn.style.opacity = "0.5";
            }
          });
        }
      }
    } catch (error) {
      console.error("Error rating feedback:", error);
    }
  }

  // Copy message to clipboard
  async copyMessage(content) {
    try {
      // Remove HTML tags for clean text copy
      const textContent = content
        .replace(/<[^>]*>/g, "")
        .replace(/&nbsp;/g, " ");
      await navigator.clipboard.writeText(textContent);
      this.showToast("Đã sao chép tin nhắn!");
    } catch (error) {
      console.error("Error copying message:", error);
      this.showToast("Không thể sao chép tin nhắn");
    }
  }

  // Show toast notification
  showToast(message) {
    const toast = document.createElement("div");
    toast.className = "chatbot-toast";
    toast.textContent = message;
    toast.style.cssText = `
            position: fixed;
            bottom: 100px;
            right: 20px;
            background: #333;
            color: white;
            padding: 10px 15px;
            border-radius: 5px;
            z-index: 10000;
            animation: slideInUp 0.3s ease-out;
        `;

    document.body.appendChild(toast);

    setTimeout(() => {
      toast.style.animation = "slideOutDown 0.3s ease-out";
      setTimeout(() => toast.remove(), 300);
    }, 2000);
  }

  // Enhanced error handling
  handleError(error, userMessage) {
    console.error("Chatbot error:", error);

    let errorMessage = "Xin lỗi, có lỗi xảy ra. Vui lòng thử lại sau.";

    if (error.message?.includes("fetch")) {
      errorMessage =
        "Không thể kết nối đến server. Vui lòng kiểm tra kết nối mạng.";
    } else if (error.message?.includes("timeout")) {
      errorMessage = "Yêu cầu quá thời gian chờ. Vui lòng thử lại.";
    }

    this.addMessage(errorMessage, "bot");

    // Store failed message for retry
    this.failedMessage = userMessage;
    this.showRetryOption();
  }

  // Show retry option for failed messages
  showRetryOption() {
    const messagesContainer = document.getElementById("chatbot-messages");
    const retryHTML = `
            <div class="message-retry">
                <button class="retry-btn" onclick="window.chatbotManager.retryLastMessage()">
                    <i class="fas fa-redo"></i> Thử lại
                </button>
            </div>
        `;
    messagesContainer.insertAdjacentHTML("beforeend", retryHTML);
    this.scrollToBottom();
  }

  // Retry last failed message
  async retryLastMessage() {
    if (this.failedMessage) {
      // Remove retry button
      const retryElement = document.querySelector(".message-retry");
      if (retryElement) retryElement.remove();

      // Retry sending the message
      await this.sendMessageInternal(this.failedMessage);
      this.failedMessage = null;
    }
  } // Internal send message method for retry functionality
  async sendMessageInternal(message) {
    // Show typing indicator
    this.showTypingIndicator();

    try {
      // Get current page context
      const context = this.getCurrentPageContext();
      const pageInfo = this.getPageInfo();

      // Send to API
      const response = await fetch("/api/chatbot/chat", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          RequestVerificationToken: this.getAntiForgeryToken(),
        },
        body: JSON.stringify({
          message: message,
          context: context,
          pagePath: pageInfo.path,
          courseId: pageInfo.courseId,
          chapterId: pageInfo.chapterId,
          lessonId: pageInfo.lessonId,
        }),
      });

      const data = await response.json();

      if (response.ok) {
        this.addMessage(data.message, "bot", data.conversationId);
      } else {
        throw new Error(data.error || "API Error");
      }
    } catch (error) {
      this.handleError(error, message);
    } finally {
      this.hideTypingIndicator();
    }
  }

  autoResizeTextarea(textarea) {
    textarea.style.height = "auto";
    textarea.style.height = Math.min(textarea.scrollHeight, 120) + "px";
  }

  scrollToBottom() {
    const messagesContainer = document.getElementById("chatbot-messages");
    if (messagesContainer) {
      messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }
  }

  showTypingIndicator() {
    const messagesContainer = document.getElementById("chatbot-messages");
    this.isTyping = true;

    const typingHTML = `
            <div class="message typing-indicator" id="typing-indicator">
                <img src="/img/logo/logowithoutbackground.png" alt="Bot" class="message-avatar" />
                <div class="message-content">
                    <div class="dots">
                        <div class="dot"></div>
                        <div class="dot"></div>
                        <div class="dot"></div>
                    </div>
                </div>
            </div>
        `;

    messagesContainer.insertAdjacentHTML("beforeend", typingHTML);
    this.scrollToBottom();
  }

  hideTypingIndicator() {
    const typingIndicator = document.getElementById("typing-indicator");
    if (typingIndicator) {
      typingIndicator.remove();
    }
    this.isTyping = false;
  }

  getCurrentPageContext() {
    const path = window.location.pathname;
    const title = document.title;

    let context = `Trang hiện tại: ${title}`;

    if (path.includes("/Course")) {
      context += " - Đang xem khóa học";
    } else if (path.includes("/Chapter")) {
      context += " - Đang xem chương học";
    } else if (path.includes("/Lesson")) {
      context += " - Đang xem bài học";
    } else if (path.includes("/Home")) {
      context += " - Trang chủ";
    }

    return context;
  }

  getPageInfo() {
    const path = window.location.pathname;
    const urlParams = new URLSearchParams(window.location.search);

    // Extract IDs from URL path or query parameters
    const pathSegments = path.split("/").filter((s) => s);

    let courseId = null;
    let chapterId = null;
    let lessonId = null;

    // Try to get from URL parameters first
    courseId = urlParams.get("courseId") || urlParams.get("id");
    chapterId = urlParams.get("chapterId") || urlParams.get("id");
    lessonId = urlParams.get("lessonId") || urlParams.get("id");

    // Try to extract from path segments
    if (path.includes("/Course/") && !courseId) {
      const courseIndex = pathSegments.indexOf("Course");
      if (courseIndex >= 0 && pathSegments[courseIndex + 1]) {
        courseId = pathSegments[courseIndex + 1];
      }
    }

    if (path.includes("/Chapter/") && !chapterId) {
      const chapterIndex = pathSegments.indexOf("Chapter");
      if (chapterIndex >= 0 && pathSegments[chapterIndex + 1]) {
        chapterId = pathSegments[chapterIndex + 1];
      }
    }

    if (path.includes("/Lesson/") && !lessonId) {
      const lessonIndex = pathSegments.indexOf("Lesson");
      if (lessonIndex >= 0 && pathSegments[lessonIndex + 1]) {
        lessonId = pathSegments[lessonIndex + 1];
      }
    }

    return {
      path: path,
      courseId: courseId,
      chapterId: chapterId,
      lessonId: lessonId,
    };
  }

  getUserAvatar() {
    const metaTag = document.querySelector('meta[name="user-avatar"]');
    return metaTag ? metaTag.getAttribute("content") : null;
  }

  getAntiForgeryToken() {
    const token = document.querySelector(
      'input[name="__RequestVerificationToken"]'
    );
    return token ? token.value : "";
  }

  async loadConversationHistory() {
    try {
      const response = await fetch("/api/chatbot/history?limit=10");
      if (response.ok) {
        const history = await response.json();
        this.conversationHistory = history;
      }
    } catch (error) {
      console.error("Failed to load conversation history:", error);
    }
  }

  // Public method to show notification
  showNotification() {
    const bubble = document.getElementById("chatbot-bubble");
    if (!bubble.querySelector(".notification-badge")) {
      bubble.insertAdjacentHTML(
        "beforeend",
        '<div class="notification-badge">1</div>'
      );
    }
  }

  // Public method to clear notification
  clearNotification() {
    const badge = document.querySelector(".chatbot-bubble .notification-badge");
    if (badge) {
      badge.remove();
    }
  }
}

// Initialize chatbot when DOM is loaded
document.addEventListener("DOMContentLoaded", () => {
  console.log("Chatbot script loaded");
  
  // Only initialize if user is authenticated
  const userId = document.querySelector('meta[name="user-id"]');
  console.log("User ID meta tag:", userId);
  
  if (userId) {
    console.log("Initializing chatbot for authenticated user");
    window.chatbotManager = new ChatbotManager();
  } else {
    console.log("No user authentication found, chatbot not initialized");
  }
});

// Global function to trigger chatbot from external sources
window.openChatbot = function () {
  if (window.chatbotManager && !window.chatbotManager.isOpen) {
    window.chatbotManager.toggleChatbot();
  }
};

window.showChatbotNotification = function () {
  if (window.chatbotManager) {
    window.chatbotManager.showNotification();
  }
};
