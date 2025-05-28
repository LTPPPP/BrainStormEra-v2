/**
 * ChatNotification - A utility class to handle chat notifications
 * Displays notifications for new messages and updates the document title
 */
class ChatNotification {
  constructor(hubUrl) {
    this.hubUrl = hubUrl;
    this.connection = null;
    this.unreadCount = 0;
    this.originalTitle = document.title;
  }

  /**
   * Initialize SignalR connection for notifications
   */
  async initialize() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl)
      .withAutomaticReconnect()
      .build();

    // Register handlers for receiving messages
    this.connection.on("ReceiveMessage", (message) => {
      // Check if the message is to the current user and not from the current user
      const currentUserId = this.getCurrentUserId();
      if (
        message.receiverId === currentUserId &&
        message.senderId !== currentUserId
      ) {
        this.incrementUnreadCount();
        this.playNotificationSound();
      }
    });

    // Start connection
    try {
      await this.connection.start();
      console.log("SignalR Notification Connected.");

      // Subscribe to relevant conversations
      const userId = this.getCurrentUserId();
      if (userId) {
        this.fetchUserConversations(userId);
      }
    } catch (err) {
      console.error("SignalR Notification Connection Error: ", err);
      setTimeout(() => this.initialize(), 5000);
    }
  }

  /**
   * Get current user ID from the page
   * @returns {string} User ID
   */
  getCurrentUserId() {
    return document.getElementById("currentUserId")?.value;
  }

  /**
   * Fetch and join conversations for the current user
   * @param {string} userId
   */
  async fetchUserConversations(userId) {
    try {
      const response = await fetch(
        `/Chat/GetUserConversations?userId=${userId}`
      );
      if (response.ok) {
        const conversations = await response.json();

        // Join each conversation group
        for (const conversation of conversations) {
          await this.connection.invoke(
            "JoinConversation",
            conversation.conversationId
          );
        }
      }
    } catch (error) {
      console.error("Failed to fetch user conversations:", error);
    }
  }

  /**
   * Increment unread message count and update UI
   */
  incrementUnreadCount() {
    this.unreadCount++;
    this.updateTitle();
    this.updateBadge();
  }

  /**
   * Update the document title to show unread count
   */
  updateTitle() {
    if (this.unreadCount > 0) {
      document.title = `(${this.unreadCount}) ${this.originalTitle}`;
    } else {
      document.title = this.originalTitle;
    }
  }
  /**
   * Update badge in the navbar (if exists)
   */
  updateBadge() {
    const badge = document.getElementById("chatNotificationBadge");
    if (badge) {
      if (this.unreadCount > 0) {
        badge.textContent = this.unreadCount;
        badge.style.display = "flex";
      } else {
        badge.style.display = "none";
      }
    }
  }

  /**
   * Play notification sound
   */
  playNotificationSound() {
    const audio = new Audio("/sounds/notification.mp3");
    audio.volume = 0.5;
    audio
      .play()
      .catch((err) => console.warn("Could not play notification sound", err));
  }

  /**
   * Reset unread count
   */
  resetUnreadCount() {
    this.unreadCount = 0;
    this.updateTitle();
    this.updateBadge();
  }
}

// Export for use in other files
window.ChatNotification = ChatNotification;
