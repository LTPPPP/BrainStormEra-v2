class ChatbotManager {
  constructor() {
    this.isOpen = false;
    this.messages = [];
    this.isTyping = false;
    this.conversationHistory = [];
    this.typingSpeed = 10; // milliseconds per character (adjustable)
    this.isPageVisible = true;
    this.pendingResponse = false;
    this.currentTypewriterTask = null;
    this.queuedResponses = [];
    this.init();
  }
  init() {
    this.createChatbotHTML();
    this.bindEvents();
    this.setupVisibilityTracking();
    this.loadConversationHistory();
    // Add welcome message with typewriter effect
    setTimeout(() => {
      this.addMessage("Hello! How can I help you today? 😊", "bot");
    }, 500);
  }
  createChatbotHTML() {
    const chatbotHTML = `
            <div class="chatbot-container">                <!-- Chatbot Bubble -->
                <div class="chatbot-bubble" id="chatbot-bubble">
                    <img src="/img/logo/logowithoutbackground.png" alt="BrainStormEra" style="width: 32px; height: 32px; object-fit: contain;" />
                </div>

                <!-- Chatbot Window -->
                <div class="chatbot-window" id="chatbot-window">
                    <!-- Header -->
                    <div class="chatbot-header">
                        <div class="bot-info">
                            <img src="/img/logo/logowithoutbackground.png" alt="Bot" class="bot-avatar" />
                            <div>
                                <div class="bot-name">BrainStormEra</div>
                                <div class="bot-status">Online</div>
                            </div>
                        </div>
                        <button class="chatbot-close" id="chatbot-close">
                            <i class="fas fa-times"></i>
                        </button>
                    </div>                    <!-- Messages Area -->
                    <div class="chatbot-messages" id="chatbot-messages">
                    </div>

                    <!-- Input Area -->
                    <div class="chatbot-input">
                        <textarea 
                            class="chatbot-input-field" 
                            id="chatbot-input" 
                            placeholder="Type your message..."
                            rows="1"
                            style="height: 40px; overflow-y: hidden;"
                        ></textarea>
                        <button class="chatbot-send-btn" id="chatbot-send">
                            <i class="fas fa-paper-plane"></i>
                        </button>
                    </div>
                </div>
            </div>
        `;

    document.body.insertAdjacentHTML("beforeend", chatbotHTML);
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
  // Setup page visibility tracking
  setupVisibilityTracking() {
    document.addEventListener("visibilitychange", () => {
      const wasVisible = this.isPageVisible;
      this.isPageVisible = !document.hidden;

      // If page becomes visible after being hidden
      if (this.isPageVisible && !wasVisible) {
        // Show notification if there were pending responses
        if (this.pendingResponse) {
          this.showNotification();
          this.pendingResponse = false;
        }

        // Resume any paused typewriter effects
        this.resumeTypewriterEffects();

        // Process any queued responses
        this.processQueuedResponses();
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
      // Clear notifications when chatbot is opened
      this.clearNotification();
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
        // If page is not visible, queue the response
        if (!this.isPageVisible) {
          this.queuedResponses.push({
            message: data.message,
            conversationId: data.conversationId,
            timestamp: Date.now(),
          });
          this.pendingResponse = true;
          // Show browser notification if possible
          this.showBrowserNotification("New chatbot response available!");
        } else {
          this.addMessage(data.message, "bot", data.conversationId);
        }
      } else {
        this.handleError(new Error(data.error || "API Error"), message);
      }
    } catch (error) {
      this.handleError(error, message);
    } finally {
      this.hideTypingIndicator();
    }
  } // Simple message formatting
  formatMessage(message) {
    message = String(message || "").trim();

    // Convert line breaks to <br>
    message = message.replace(/\n/g, "<br>");

    // Convert **bold** to <strong>
    message = message.replace(/\*\*(.*?)\*\*/g, "<strong>$1</strong>");

    // Convert *italic* to <em>
    message = message.replace(/\*(.*?)\*/g, "<em>$1</em>");

    return message;
  } // Simple message display with typewriter effect
  addMessage(content, sender, conversationId = null) {
    const messagesContainer = document.getElementById("chatbot-messages");
    const isUser = sender === "user";
    const timestamp = new Date().toLocaleTimeString("en-US", {
      hour: "2-digit",
      minute: "2-digit",
    });

    const avatarSrc = isUser
      ? this.getUserAvatar() || "/img/default-avatar.svg"
      : "/img/logo/logowithoutbackground.png";

    const messageHTML = `
            <div class="message ${
              isUser ? "user" : ""
            }" data-conversation-id="${conversationId || ""}">
                <img src="${avatarSrc}" alt="${
      isUser ? "User" : "Bot"
    }" class="message-avatar" onerror="this.src='/img/default-avatar.svg'" />
                <div class="message-wrapper">
                    <div class="message-content" id="message-${Date.now()}"></div>
                    <div class="message-time">${timestamp}</div>
                </div>
            </div>
        `;

    messagesContainer.insertAdjacentHTML("beforeend", messageHTML);

    const messageElement =
      messagesContainer.lastElementChild.querySelector(".message-content");

    if (isUser) {
      // For user messages, show immediately
      messageElement.innerHTML = this.formatMessage(content);
    } else {
      // For bot messages, use typewriter effect
      this.typewriterEffect(messageElement, content);
    }

    this.scrollToBottom();
  } // Typewriter effect for bot messages
  async typewriterEffect(element, text) {
    const formattedText = this.formatMessage(text);
    const tempDiv = document.createElement("div");
    tempDiv.innerHTML = formattedText;
    const plainText = tempDiv.textContent || tempDiv.innerText || "";

    element.innerHTML = "";
    element.classList.add("typing");

    // Create a unique task ID for this typewriter effect
    const taskId = Date.now() + Math.random();
    this.currentTypewriterTask = {
      id: taskId,
      element: element,
      text: plainText,
      formattedText: formattedText,
      currentIndex: 0,
      paused: false,
    };

    for (let i = 0; i < plainText.length; i++) {
      // Check if this task is still current and not paused
      if (
        this.currentTypewriterTask?.id !== taskId ||
        this.currentTypewriterTask?.paused
      ) {
        // Save progress and exit
        if (
          this.currentTypewriterTask &&
          this.currentTypewriterTask.id === taskId
        ) {
          this.currentTypewriterTask.currentIndex = i;
        }
        return;
      }

      element.textContent += plainText[i];
      this.scrollToBottom();

      // Use configurable typing speed, but pause if page is not visible
      if (this.isPageVisible) {
        await new Promise((resolve) => setTimeout(resolve, this.typingSpeed));
      } else {
        // Page is not visible, pause the effect
        this.currentTypewriterTask.currentIndex = i + 1;
        this.currentTypewriterTask.paused = true;
        return;
      }
    }

    // Complete the typewriter effect
    element.classList.remove("typing");
    element.innerHTML = formattedText;
    this.scrollToBottom();

    // Clear current task
    if (this.currentTypewriterTask?.id === taskId) {
      this.currentTypewriterTask = null;
    }
  } // Simple error handling with typewriter effect
  handleError(error, userMessage) {
    console.error("Chatbot error:", error);
    let errorMessage = "Sorry, an error occurred. Please try again later.";
    this.addMessage(errorMessage, "bot");
  }
  autoResizeTextarea(textarea) {
    textarea.style.height = "auto";
    textarea.style.height = Math.min(textarea.scrollHeight, 100) + "px";
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
                <div class="message-wrapper">
                    <div class="message-content">
                        <div class="typing-dots">
                            <span></span>
                            <span></span>
                            <span></span>
                        </div>
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
    const title = document.title;
    return `Current page: ${title}`;
  }

  getPageInfo() {
    const path = window.location.pathname;
    const urlParams = new URLSearchParams(window.location.search);
    const pathSegments = path.split("/").filter((s) => s);

    let courseId = urlParams.get("courseId") || urlParams.get("id");
    let chapterId = urlParams.get("chapterId") || urlParams.get("id");
    let lessonId = urlParams.get("lessonId") || urlParams.get("id");

    // Extract from path segments
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
    // Simplified - no history loading for simple chat
    return;
  }
  // Simple notification methods
  showNotification() {
    const bubble = document.getElementById("chatbot-bubble");
    if (!bubble.querySelector(".notification-badge")) {
      bubble.insertAdjacentHTML(
        "beforeend",
        '<div class="notification-badge">1</div>'
      );
    }
  }

  clearNotification() {
    const badge = document.querySelector(".chatbot-bubble .notification-badge");
    if (badge) {
      badge.remove();
    }
  }

  // Resume paused typewriter effects when page becomes visible
  async resumeTypewriterEffects() {
    if (this.currentTypewriterTask && this.currentTypewriterTask.paused) {
      const task = this.currentTypewriterTask;
      task.paused = false;

      const element = task.element;
      const plainText = task.text;
      const formattedText = task.formattedText;
      let currentIndex = task.currentIndex;

      // Continue from where we left off
      for (let i = currentIndex; i < plainText.length; i++) {
        // Check if task is still current and not paused again
        if (
          this.currentTypewriterTask?.id !== task.id ||
          this.currentTypewriterTask?.paused
        ) {
          if (
            this.currentTypewriterTask &&
            this.currentTypewriterTask.id === task.id
          ) {
            this.currentTypewriterTask.currentIndex = i;
          }
          return;
        }

        element.textContent += plainText[i];
        this.scrollToBottom();

        if (this.isPageVisible) {
          await new Promise((resolve) => setTimeout(resolve, this.typingSpeed));
        } else {
          // Page became invisible again, pause
          this.currentTypewriterTask.currentIndex = i + 1;
          this.currentTypewriterTask.paused = true;
          return;
        }
      }

      // Complete the typewriter effect
      element.classList.remove("typing");
      element.innerHTML = formattedText;
      this.scrollToBottom();

      // Clear current task
      if (this.currentTypewriterTask?.id === task.id) {
        this.currentTypewriterTask = null;
      }
    }
  }

  // Process queued responses when page becomes visible
  processQueuedResponses() {
    if (this.queuedResponses.length > 0) {
      // Process responses one by one
      const response = this.queuedResponses.shift();
      this.addMessage(response.message, "bot", response.conversationId);

      // If there are more responses, process them after a delay
      if (this.queuedResponses.length > 0) {
        setTimeout(() => {
          this.processQueuedResponses();
        }, 1000); // 1 second delay between responses
      }
    }
  }

  // Show browser notification when page is not visible
  showBrowserNotification(message) {
    // Check if notifications are supported and permission is granted
    if ("Notification" in window) {
      if (Notification.permission === "granted") {
        new Notification("BrainStormEra Chat", {
          body: message,
          icon: "/img/logo/logowithoutbackground.png",
          badge: "/img/logo/logowithoutbackground.png",
        });
      } else if (Notification.permission !== "denied") {
        // Request permission
        Notification.requestPermission().then((permission) => {
          if (permission === "granted") {
            new Notification("BrainStormEra Chat", {
              body: message,
              icon: "/img/logo/logowithoutbackground.png",
              badge: "/img/logo/logowithoutbackground.png",
            });
          }
        });
      }
    }
  }
}

// Initialize chatbot when DOM is loaded
document.addEventListener("DOMContentLoaded", () => {
  // Only initialize if user is authenticated
  const userId = document.querySelector('meta[name="user-id"]');
  if (userId) {
    window.chatbotManager = new ChatbotManager();
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
