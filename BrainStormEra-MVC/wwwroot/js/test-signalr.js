// Test SignalR connection - temporary test file
console.log("Testing SignalR availability...");

if (typeof signalR !== "undefined") {
  console.log("✓ SignalR is available");
  console.log("SignalR version:", signalR.VERSION || "unknown");

  // Test connection creation
  try {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl("/notificationHub")
      .build();
    console.log("✓ SignalR connection created successfully");
  } catch (err) {
    console.error("✗ Failed to create SignalR connection:", err);
  }
} else {
  console.error("✗ SignalR is not available");
}
