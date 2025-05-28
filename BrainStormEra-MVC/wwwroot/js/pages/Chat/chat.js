/**
 * ChatService - A wrapper around SignalR functionality for chat
 * Handles connecting, message sending/receiving, and notifications
 */
class ChatService {
  constructor(hubUrl) {
    this.hubUrl = hubUrl;
    this.connection = null;
    this.callbacks = {
      onMessageReceived: null,
      onMessageRead: null,
      onConnectionStarted: null,
      onConnectionError: null,
    };
  }

  /**
   * Initialize SignalR connection
   */
  async initialize() {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl)
      .withAutomaticReconnect()
      .build();

    // Register handlers
    this.connection.on("ReceiveMessage", (message) => {
      if (this.callbacks.onMessageReceived) {
        this.callbacks.onMessageReceived(message);
      }

      // Play notification sound if message is from someone else
      const currentUserId = document.getElementById("currentUserId")?.value;
      if (currentUserId && message.senderId !== currentUserId) {
        this.playNotificationSound();
      }
    });

    this.connection.on("MessageRead", (messageId) => {
      if (this.callbacks.onMessageRead) {
        this.callbacks.onMessageRead(messageId);
      }
    });

    // Start connection
    try {
      await this.connection.start();
      console.log("SignalR Connected.");

      if (this.callbacks.onConnectionStarted) {
        this.callbacks.onConnectionStarted();
      }
    } catch (err) {
      console.error("SignalR Connection Error: ", err);

      if (this.callbacks.onConnectionError) {
        this.callbacks.onConnectionError(err);
      }

      // Try reconnecting after delay
      setTimeout(() => this.initialize(), 5000);
    }
  }

  /**
   * Join a conversation group
   * @param {string} conversationId
   */
  async joinConversation(conversationId) {
    if (!this.connection || this.connection.state !== "Connected") {
      await this.initialize();
    }

    await this.connection.invoke("JoinConversation", conversationId);
    console.log(`Joined conversation: ${conversationId}`);
  }

  /**
   * Leave a conversation group
   * @param {string} conversationId
   */
  async leaveConversation(conversationId) {
    if (this.connection && this.connection.state === "Connected") {
      await this.connection.invoke("LeaveConversation", conversationId);
      console.log(`Left conversation: ${conversationId}`);
    }
  }

  /**
   * Send a message in a conversation
   * @param {string} conversationId
   * @param {string} senderId
   * @param {string} receiverId
   * @param {string} content
   */
  async sendMessage(conversationId, senderId, receiverId, content) {
    if (!this.connection || this.connection.state !== "Connected") {
      await this.initialize();
    }

    await this.connection.invoke(
      "SendMessage",
      conversationId,
      senderId,
      receiverId,
      content
    );
  }

  /**
   * Mark a message as read
   * @param {string} messageId
   * @param {string} userId
   */
  async markMessageAsRead(messageId, userId) {
    if (!this.connection || this.connection.state !== "Connected") {
      await this.initialize();
    }

    await this.connection.invoke("MarkMessageAsRead", messageId, userId);
  }

  /**
   * Play a notification sound
   */
  playNotificationSound() {
    // Create audio element and play notification sound
    const audio = new Audio("/sounds/notification.mp3");
    audio.volume = 0.5;
    audio
      .play()
      .catch((err) => console.warn("Could not play notification sound", err));
  }

  /**
   * Set callback functions
   * @param {Object} callbacks
   */
  setCallbacks(callbacks) {
    this.callbacks = { ...this.callbacks, ...callbacks };
  }
}

// Export for use in other files
window.ChatService = ChatService;
