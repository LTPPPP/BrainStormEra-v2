# Chatbot Component - BrainStormEra

## Tổng quan

Chatbot component được thiết kế để chỉ hiển thị khi user đang ở trang lesson, cung cấp hỗ trợ AI cho việc học tập.

## Tính năng chính

### 1. Hiển thị có điều kiện
- **Chỉ hiển thị ở lesson**: Chatbot chỉ xuất hiện khi user đang ở trang lesson
- **Tự động ẩn**: Khi user rời khỏi lesson, chatbot sẽ tự động ẩn
- **Responsive**: Hoạt động tốt trên cả desktop và mobile

### 2. Giao diện người dùng
- **Floating button**: Nút tròn với icon robot ở góc phải dưới
- **Chat window**: Cửa sổ chat có thể mở/đóng
- **Typing indicator**: Hiển thị khi AI đang xử lý
- **Message history**: Lưu trữ lịch sử tin nhắn trong session

### 3. Tích hợp AI
- **Context-aware**: Gửi thông tin lesson hiện tại cho AI
- **Conversation history**: Duy trì context của cuộc hội thoại
- **Error handling**: Xử lý lỗi kết nối và hiển thị thông báo phù hợp

## Cấu trúc file

```
BrainStormEra-Razor/
├── Pages/Shared/
│   └── _Chatbot.cshtml          # HTML structure của chatbot
├── wwwroot/
│   ├── css/components/
│   │   └── chatbot.css          # Styles cho chatbot
│   └── js/components/
│       └── chatbot.js           # JavaScript logic
```

## Cách hoạt động

### 1. Kiểm tra trang hiện tại
```javascript
function isUserOnLessonPage() {
    const lessonPatterns = [
        '/Lesson/Learn',
        '/lesson/learn',
        '/Course/Learn',
        '/course/learn'
    ];
    
    return lessonPatterns.some(pattern => 
        currentPath.includes(pattern) || currentUrl.includes(pattern)
    );
}
```

### 2. Hiển thị/ẩn tự động
- Khi trang load: Kiểm tra URL và hiển thị/ẩn chatbot
- Khi navigation: Lắng nghe sự kiện `popstate` và `pushState`
- Khi URL thay đổi: Tự động cập nhật trạng thái hiển thị

### 3. Trích xuất context
```javascript
extractLessonContext() {
    const lessonId = urlParams.get('id') || this.extractLessonIdFromUrl();
    if (lessonId) {
        this.currentLessonId = lessonId;
    }
}
```

## API Integration

### Endpoint
- **URL**: `/Chatbot/Ask`
- **Method**: `POST`
- **Content-Type**: `application/json`

### Request Data
```json
{
    "question": "string",
    "lessonId": "string",
    "conversationHistory": [
        {
            "role": "user|assistant",
            "content": "string",
            "timestamp": "datetime"
        }
    ],
    "context": {
        "currentPage": "string",
        "courseId": "string",
        "userId": "string"
    }
}
```

### Response
```json
{
    "success": true,
    "response": "AI response message"
}
```

## CSS Classes

### Container
- `.chatbot-container`: Container chính của chatbot
- `.chatbot-toggle`: Nút mở/đóng chatbot
- `.chatbot-window`: Cửa sổ chat

### Messages
- `.chatbot-message`: Container tin nhắn
- `.user-message`: Tin nhắn của user
- `.bot-message`: Tin nhắn của AI
- `.typing-indicator`: Hiệu ứng đang gõ

### Input
- `.chatbot-input`: Container input
- `.chatbot-input input`: Ô nhập tin nhắn
- `.chatbot-input button`: Nút gửi

## Responsive Design

### Desktop (>768px)
- Chatbot window: 350px width, 500px height
- Position: bottom-right corner

### Tablet (≤768px)
- Chatbot window: 300px width, 400px height
- Adjusted positioning

### Mobile (≤480px)
- Chatbot window: Full width minus margins
- Smaller toggle button

## Browser Support

- **Modern browsers**: Chrome, Firefox, Safari, Edge
- **JavaScript**: ES6+ features
- **CSS**: Flexbox, CSS Grid, animations

## Security

- **Anti-forgery token**: Sử dụng `__RequestVerificationToken`
- **User authentication**: Chỉ hiển thị cho user đã đăng nhập
- **Input sanitization**: Escape HTML trong tin nhắn

## Troubleshooting

### Chatbot không hiển thị
1. Kiểm tra user đã đăng nhập chưa
2. Kiểm tra URL có chứa pattern lesson không
3. Kiểm tra console có lỗi JavaScript không

### Chatbot không gửi tin nhắn
1. Kiểm tra endpoint `/Chatbot/Ask` có hoạt động không
2. Kiểm tra anti-forgery token
3. Kiểm tra network tab trong DevTools

### Styling issues
1. Kiểm tra file CSS đã load chưa
2. Kiểm tra CSS variables (--primary-color, --primary-dark)
3. Kiểm tra responsive breakpoints

## Future Enhancements

1. **Voice input**: Hỗ trợ nhập tin nhắn bằng giọng nói
2. **File upload**: Cho phép upload ảnh/tài liệu
3. **Multi-language**: Hỗ trợ nhiều ngôn ngữ
4. **Offline mode**: Cache responses cho offline
5. **Analytics**: Theo dõi usage patterns
6. **Customization**: Cho phép user tùy chỉnh giao diện