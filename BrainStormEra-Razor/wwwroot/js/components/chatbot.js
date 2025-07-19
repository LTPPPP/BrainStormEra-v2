// Chatbot component functionality
class ChatbotManager {
    constructor() {
        this.isInitialized = false;
        this.isVisible = false;
        this.currentLessonId = null;
        this.conversationHistory = [];
    }

    initialize() {
        if (this.isInitialized) return;
        
        this.checkVisibility();
        this.setupEventListeners();
        this.isInitialized = true;
        
        // Lắng nghe sự kiện thay đổi URL
        this.setupNavigationListener();
    }

    checkVisibility() {
        const isOnLessonPage = this.isUserOnLessonPage();
        const chatbotContainer = document.getElementById('chatbot-container');
        
        if (chatbotContainer) {
            if (isOnLessonPage) {
                chatbotContainer.style.display = 'block';
                this.isVisible = true;
                this.extractLessonContext();
            } else {
                chatbotContainer.style.display = 'none';
                this.isVisible = false;
            }
        }
    }

    isUserOnLessonPage() {
        const currentPath = window.location.pathname;
        const currentUrl = window.location.href;
        
        // Kiểm tra các pattern URL của lesson
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

    extractLessonContext() {
        // Trích xuất thông tin lesson từ URL hoặc DOM
        const urlParams = new URLSearchParams(window.location.search);
        const lessonId = urlParams.get('id') || this.extractLessonIdFromUrl();
        
        if (lessonId) {
            this.currentLessonId = lessonId;
            console.log('Chatbot: Current lesson ID:', lessonId);
        }
    }

    extractLessonIdFromUrl() {
        // Trích xuất lesson ID từ URL path
        const pathParts = window.location.pathname.split('/');
        const learnIndex = pathParts.findIndex(part => 
            part.toLowerCase() === 'learn'
        );
        
        if (learnIndex !== -1 && learnIndex + 1 < pathParts.length) {
            return pathParts[learnIndex + 1];
        }
        
        return null;
    }

    setupEventListeners() {
        const chatbotToggle = document.getElementById('chatbot-toggle');
        const chatbotWindow = document.getElementById('chatbot-window');
        const chatbotClose = document.getElementById('chatbot-close');
        const chatbotInput = document.getElementById('chatbot-input');
        const chatbotSend = document.getElementById('chatbot-send');

        if (chatbotToggle) {
            chatbotToggle.addEventListener('click', () => this.toggleChatWindow());
        }

        if (chatbotClose) {
            chatbotClose.addEventListener('click', () => this.closeChatWindow());
        }

        if (chatbotSend && chatbotInput) {
            chatbotSend.addEventListener('click', () => this.sendMessage());
            chatbotInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') {
                    this.sendMessage();
                }
            });
        }
    }

    setupNavigationListener() {
        // Lắng nghe sự kiện thay đổi URL
        window.addEventListener('popstate', () => {
            setTimeout(() => this.checkVisibility(), 100);
        });

        // Override pushState để bắt navigation
        const originalPushState = history.pushState;
        history.pushState = (...args) => {
            originalPushState.apply(history, args);
            setTimeout(() => this.checkVisibility(), 100);
        };
    }

    toggleChatWindow() {
        const chatbotWindow = document.getElementById('chatbot-window');
        if (chatbotWindow) {
            const isVisible = chatbotWindow.style.display !== 'none';
            chatbotWindow.style.display = isVisible ? 'none' : 'block';
            
            if (!isVisible) {
                // Focus vào input khi mở chat
                const chatbotInput = document.getElementById('chatbot-input');
                if (chatbotInput) {
                    setTimeout(() => chatbotInput.focus(), 100);
                }
            }
        }
    }

    closeChatWindow() {
        const chatbotWindow = document.getElementById('chatbot-window');
        if (chatbotWindow) {
            chatbotWindow.style.display = 'none';
        }
    }

    sendMessage() {
        const chatbotInput = document.getElementById('chatbot-input');
        const message = chatbotInput?.value.trim();
        
        if (!message) return;

        // Thêm tin nhắn của user
        this.addMessage('user', message);
        chatbotInput.value = '';

        // Lưu vào lịch sử
        this.conversationHistory.push({
            role: 'user',
            content: message,
            timestamp: new Date()
        });

        // Gửi đến AI service
        this.sendToAIService(message);
    }

    addMessage(sender, content) {
        const chatbotMessages = document.getElementById('chatbot-messages');
        if (!chatbotMessages) return;

        const messageDiv = document.createElement('div');
        messageDiv.className = `chatbot-message ${sender}-message`;
        
        const icon = sender === 'user' ? 'fas fa-user' : 'fas fa-robot';
        messageDiv.innerHTML = `
            <div class="message-content">
                <i class="${icon}"></i>
                <p>${this.escapeHtml(content)}</p>
            </div>
        `;
        
        chatbotMessages.appendChild(messageDiv);
        chatbotMessages.scrollTop = chatbotMessages.scrollHeight;
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    async sendToAIService(message) {
        try {
            // Hiển thị typing indicator
            this.showTypingIndicator();

            // Chuẩn bị dữ liệu gửi
            const requestData = {
                question: message,
                lessonId: this.currentLessonId,
                conversationHistory: this.conversationHistory.slice(-5), // Lấy 5 tin nhắn gần nhất
                context: {
                    currentPage: window.location.pathname,
                    courseId: this.extractCourseId(),
                    userId: this.getUserId()
                }
            };

            // Gửi request đến AI service
            const response = await fetch('/Chatbot/Ask', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                },
                body: JSON.stringify(requestData)
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();
            
            // Ẩn typing indicator
            this.hideTypingIndicator();

            if (result.success) {
                const aiResponse = result.response || 'I understand your question. Let me help you with that.';
                this.addMessage('bot', aiResponse);
                
                // Lưu phản hồi vào lịch sử
                this.conversationHistory.push({
                    role: 'assistant',
                    content: aiResponse,
                    timestamp: new Date()
                });
            } else {
                this.addMessage('bot', 'Sorry, I encountered an error. Please try again later.');
            }

        } catch (error) {
            console.error('Chatbot error:', error);
            this.hideTypingIndicator();
            this.addMessage('bot', 'Sorry, I\'m having trouble connecting right now. Please try again later.');
        }
    }

    showTypingIndicator() {
        const chatbotMessages = document.getElementById('chatbot-messages');
        if (!chatbotMessages) return;

        const typingDiv = document.createElement('div');
        typingDiv.className = 'chatbot-message bot-message typing-indicator';
        typingDiv.id = 'typing-indicator';
        typingDiv.innerHTML = `
            <div class="message-content">
                <i class="fas fa-robot"></i>
                <div class="typing-dots">
                    <span></span>
                    <span></span>
                    <span></span>
                </div>
            </div>
        `;
        
        chatbotMessages.appendChild(typingDiv);
        chatbotMessages.scrollTop = chatbotMessages.scrollHeight;
    }

    hideTypingIndicator() {
        const typingIndicator = document.getElementById('typing-indicator');
        if (typingIndicator) {
            typingIndicator.remove();
        }
    }

    getAntiForgeryToken() {
        const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenElement ? tokenElement.value : '';
    }

    getUserId() {
        const userIdMeta = document.querySelector('meta[name="user-id"]');
        return userIdMeta ? userIdMeta.content : null;
    }

    extractCourseId() {
        // Trích xuất course ID từ URL hoặc DOM
        const urlParams = new URLSearchParams(window.location.search);
        return urlParams.get('courseId') || null;
    }
}

// Khởi tạo chatbot khi trang load
document.addEventListener('DOMContentLoaded', function() {
    window.chatbotManager = new ChatbotManager();
    window.chatbotManager.initialize();
});

// Export cho việc sử dụng từ bên ngoài
window.ChatbotManager = ChatbotManager;
