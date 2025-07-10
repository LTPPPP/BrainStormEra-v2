# 🚀 SignalR Hubs - BrainStormEra

## 📋 Tổng quan

SignalR Hubs là trung tâm giao tiếp real-time trong dự án BrainStormEra, cung cấp khả năng:
- **Chat real-time** giữa users
- **Thông báo instant** (notifications)
- **Live updates** trạng thái online/offline
- **Group messaging** theo course, role

## 🏗️ Kiến trúc

```
┌─────────────────┐    WebSocket/SSE    ┌─────────────────┐
│   CLIENT SIDE   │ ◄─────────────────► │   SERVER SIDE   │
│                 │                     │                 │
│ • JavaScript    │                     │ • ChatHub.cs    │
│ • SignalR       │                     │ • Notification  │
│   Client        │                     │   Hub.cs        │
│ • Event         │                     │ • Hub Services  │
│   Handlers      │                     │ • Groups Mgmt   │
└─────────────────┘                     └─────────────────┘
```

---

## 🎯 Các Hub có sẵn

### 1. **ChatHub** 💬
**📍 File:** `ChatHub.cs`
**🔗 Endpoint:** `/chatHub`

#### **Chức năng:**
- Gửi/nhận tin nhắn real-time
- Typing indicators
- Message read status
- Online/offline status

#### **Methods:**
```csharp
// Gửi tin nhắn
public async Task SendMessage(string receiverId, string message, string? replyToMessageId = null)

// Đánh dấu đã đọc
public async Task MarkMessageAsRead(string messageId)

// Typing indicators
public async Task StartTyping(string receiverId)
public async Task StopTyping(string receiverId)
```

#### **Events từ Server:**
- `ReceiveMessage` - Nhận tin nhắn mới
- `MessageSent` - Xác nhận tin nhắn đã gửi
- `MessageRead` - Tin nhắn đã được đọc
- `UserStartedTyping` / `UserStoppedTyping`
- `MessageError` - Lỗi gửi tin nhắn

---

### 2. **NotificationHub** 🔔
**📍 File:** `NotificationHub.cs`
**🔗 Endpoint:** `/notificationHub`

#### **Chức năng:**
- Thông báo real-time
- Group notifications (Course, Role)
- Unread count updates
- Notification management

#### **Methods:**
```csharp
// Đánh dấu đã đọc
public async Task MarkAsRead(string notificationId)
public async Task MarkAllAsRead()

// Group management
public async Task JoinCourseGroup(string courseId)
public async Task LeaveCourseGroup(string courseId)
public async Task JoinRoleGroup(string role)
```

#### **Events từ Server:**
- `ReceiveNotification` - Nhận thông báo mới
- `UpdateUnreadCount` - Cập nhật số lượng chưa đọc
- `NotificationUpdated` - Thông báo được cập nhật

---

## 💻 Client Implementation

### **JavaScript Connection**

#### **Chat Client:**
```javascript
// Khởi tạo kết nối
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .withAutomaticReconnect()
    .build();

// Lắng nghe tin nhắn
connection.on("ReceiveMessage", (message) => {
    displayMessage(message);
});

// Gửi tin nhắn
await connection.invoke("SendMessage", receiverId, messageContent);
```

#### **Notification Client:**
```javascript
// Khởi tạo kết nối
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .withAutomaticReconnect()
    .build();

// Lắng nghe thông báo
connection.on("ReceiveNotification", (notification) => {
    showToastNotification(notification);
});

// Join group
await connection.invoke("JoinCourseGroup", courseId);
```

---

## ⚙️ Cấu hình Server

### **Program.cs Registration:**
```csharp
// Add SignalR service
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Map Hubs
app.MapHub<BusinessLogicLayer.Hubs.NotificationHub>("/notificationHub");
app.MapHub<BusinessLogicLayer.Hubs.ChatHub>("/chatHub");
```

### **Authentication:**
```csharp
[Authorize] // Yêu cầu đăng nhập
public class ChatHub : Hub
{
    // Hub implementation
}
```

---

## 🔄 Group Management

### **Group Types:**
1. **User Groups:** `User_{userId}` - Cho notifications cá nhân
2. **Course Groups:** `Course_{courseId}` - Cho course updates
3. **Role Groups:** `Role_{roleName}` - Cho role-based notifications

### **Usage Examples:**
```csharp
// Gửi thông báo cho một user
await Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", notification);

// Gửi cho tất cả trong course
await Clients.Group($"Course_{courseId}").SendAsync("ReceiveNotification", notification);

// Gửi cho role (Admin, Instructor, Learner)
await Clients.Group($"Role_{role}").SendAsync("ReceiveNotification", notification);
```

---

## 📱 Client Files

| **File** | **Mục đích** | **Vị trí** |
|----------|--------------|------------|
| `chat.js` | Chat system chính | `/wwwroot/js/components/` |
| `notification-system.js` | Hệ thống thông báo | `/wwwroot/js/shared/` |
| `_ChatSignalR.cshtml` | Global chat integration | `/Views/Shared/` |
| `notification-index.js` | Notification page | `/wwwroot/js/pages/Notifications/` |

---

## 🛠️ Debugging & Monitoring

### **Enable Detailed Errors:**
```csharp
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true; // Chỉ trong Development
});
```

### **JavaScript Logging:**
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .configureLogging(signalR.LogLevel.Information) // Debug level
    .build();
```

### **Server-side Logging:**
```csharp
private readonly ILogger<ChatHub> _logger;

_logger.LogInformation($"User {userId} connected to ChatHub with connection {Context.ConnectionId}");
```

---

## 🔧 Troubleshooting

### **Common Issues:**

#### **1. Connection Failed**
```javascript
// Check SignalR library loaded
if (typeof signalR === "undefined") {
    console.error("SignalR library not loaded");
    return;
}

// Add error handling
connection.onclose(async () => {
    await handleReconnect();
});
```

#### **2. Authentication Issues**
```csharp
// Ensure user is authenticated
var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
if (userId == null) return;
```

#### **3. Group Join Failures**
```javascript
// Wait for connection before joining groups
await connection.start();
await connection.invoke("JoinCourseGroup", courseId);
```

---

## 📊 Performance Tips

### **1. Connection Pooling:**
```csharp
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});
```

### **2. Message Batching:**
```javascript
// Throttle typing indicators
const debouncedTyping = debounce(() => {
    connection.invoke("StopTyping", receiverId);
}, 1000);
```

### **3. Cleanup Connections:**
```javascript
// Cleanup on page unload
window.addEventListener('beforeunload', () => {
    if (connection) {
        connection.stop();
    }
});
```

---

## 🚦 Status Indicators

### **Connection States:**
- `Disconnected` - Chưa kết nối
- `Connecting` - Đang kết nối
- `Connected` - Đã kết nối
- `Disconnecting` - Đang ngắt kết nối
- `Reconnecting` - Đang kết nối lại

### **Visual Indicators:**
```javascript
// Show connection status
connection.onreconnecting(() => {
    showConnectionStatus("Reconnecting...", "warning");
});

connection.onreconnected(() => {
    showConnectionStatus("Connected", "success");
});
```

---

## 📚 API Reference

### **ChatHub Methods:**
| Method | Parameters | Description |
|--------|------------|-------------|
| `SendMessage` | `receiverId`, `message`, `replyToMessageId?` | Gửi tin nhắn |
| `MarkMessageAsRead` | `messageId` | Đánh dấu đã đọc |
| `StartTyping` | `receiverId` | Bắt đầu typing |
| `StopTyping` | `receiverId` | Dừng typing |

### **NotificationHub Methods:**
| Method | Parameters | Description |
|--------|------------|-------------|
| `MarkAsRead` | `notificationId` | Đánh dấu thông báo đã đọc |
| `MarkAllAsRead` | - | Đánh dấu tất cả đã đọc |
| `JoinCourseGroup` | `courseId` | Tham gia nhóm course |
| `LeaveCourseGroup` | `courseId` | Rời nhóm course |
| `JoinRoleGroup` | `role` | Tham gia nhóm role |

---

## 📝 Changelog

### **v1.0.0**
- ✅ Implemented ChatHub with real-time messaging
- ✅ Implemented NotificationHub with group management
- ✅ Added typing indicators
- ✅ Added message read status
- ✅ Added automatic reconnection

### **Future Improvements:**
- 🔄 Message history pagination
- 📎 File attachments support
- 🎥 Video call integration
- 📱 Mobile push notifications

---

## 👥 Contributors

- **Development Team** - BrainStormEra Project
- **SignalR Version** - ASP.NET Core 8.0

---

**🔗 Related Documentation:**
- [Microsoft SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/)
- [JavaScript Client API](https://docs.microsoft.com/en-us/aspnet/core/signalr/javascript-client) 