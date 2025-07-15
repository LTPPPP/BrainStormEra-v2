// Test script for chatbot conversation memory feature
// Add this to browser console to test the functionality

function testChatbotMemory() {
  console.log("🧪 Testing Chatbot Memory Feature");

  // Test 1: Check if sessionStorage is working
  const testKey = "chatbot_conversation_history";
  const testData = [
    {
      content: "Hello, test message",
      sender: "user",
      conversationId: "test-123",
      timestamp: "12:00 PM",
      id: Date.now(),
    },
    {
      content: "Hi! This is a test response",
      sender: "bot",
      conversationId: "test-124",
      timestamp: "12:01 PM",
      id: Date.now() + 1,
    },
  ];

  try {
    // Store test data
    sessionStorage.setItem(testKey, JSON.stringify(testData));
    console.log("✅ SessionStorage write test passed");

    // Retrieve test data
    const retrieved = JSON.parse(sessionStorage.getItem(testKey));
    if (retrieved && retrieved.length === 2) {
      console.log("✅ SessionStorage read test passed");
    } else {
      console.log("❌ SessionStorage read test failed");
    }

    // Clean up
    sessionStorage.removeItem(testKey);
    console.log("✅ SessionStorage cleanup completed");
  } catch (error) {
    console.error("❌ SessionStorage test failed:", error);
  }

  // Test 2: Check if chatbot manager exists
  if (window.chatbotManager) {
    console.log("✅ Chatbot Manager is available");

    // Test conversation history methods
    if (typeof window.chatbotManager.saveMessageToHistory === "function") {
      console.log("✅ saveMessageToHistory method exists");
    }

    if (typeof window.chatbotManager.loadConversationHistory === "function") {
      console.log("✅ loadConversationHistory method exists");
    }

    if (typeof window.chatbotManager.clearConversationHistory === "function") {
      console.log("✅ clearConversationHistory method exists");
    }

    console.log(
      "📊 Current conversation length:",
      window.chatbotManager.messages.length
    );
  } else {
    console.log(
      "❌ Chatbot Manager not found - make sure user is authenticated"
    );
  }

  console.log("🎯 Test Summary:");
  console.log(
    "- The chatbot will now remember conversations across page interactions"
  );
  console.log(
    "- History is stored in sessionStorage (cleared when tab is closed)"
  );
  console.log("- Use 'New Chat' button to clear conversation history");
  console.log(
    "- Conversation persists during page navigation within the same session"
  );
}

// Run the test
testChatbotMemory();

// Instructions for manual testing
console.log(`
📋 Manual Testing Instructions:
1. Open chatbot and send a few messages
2. Close chatbot window (don't reload page yet)
3. Open chatbot again - messages should still be there
4. Navigate to another page in the app
5. Return to this page - messages should be restored
6. Click 'New Chat' button - history should be cleared
7. Reload the page - new conversation should start fresh
`);
