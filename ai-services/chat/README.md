# BrainStormEra Chat Service

AI-powered chat service for BrainStormEra educational platform, built with Python FastAPI.

**Note**: This service uses the **same SQL Server database** as the main MVC application. No separate database setup required.

## Features

### 🔥 Real-time Chat
- **Peer-to-peer messaging** between users
- **WebSocket support** for real-time communication
- **Typing indicators** and read receipts
- **Message threading** (reply to messages)
- **Message editing and deletion**
- **User status tracking**

### 🤖 AI Chatbot
- **Google AI integration** (Gemini Pro)
- **Context-aware responses** based on current page/course
- **Conversation history** 
- **Feedback system** (1-5 star ratings)
- **Educational content optimization**

### 📊 Analytics & Monitoring
- **Real-time connection tracking**
- **Conversation statistics**
- **User engagement metrics**
- **Integrated with MVC database**

## Architecture

```
ai-services/chat/
├── app/
│   ├── api/                 # FastAPI routes
│   │   ├── chat_routes.py
│   │   └── chatbot_routes.py
│   ├── core/                # Core configurations
│   │   ├── config.py
│   │   └── database.py
│   ├── models/              # Pydantic & SQLAlchemy models
│   │   ├── chat_models.py
│   │   ├── chatbot_models.py
│   │   └── database.py
│   ├── services/            # Business logic
│   │   ├── chat_service.py
│   │   └── chatbot_service.py
│   └── websockets/          # Real-time communication
│       └── chat_websocket.py
├── main.py                  # FastAPI application
├── requirements.txt         # Python dependencies
├── Dockerfile
├── docker-compose.yml
└── README.md
```

## Quick Start

### 1. Prerequisites

- Python 3.11+
- **SQL Server** (same as MVC application)
- **ODBC Driver 17 for SQL Server**
- Google AI API Key
- MVC application database already set up

### 2. Installation

```bash
# Navigate to chat service directory
cd ai-services/chat

# Install dependencies
pip install -r requirements.txt

# Copy configuration template
cp config_example.py config.py
# Edit config.py with your database settings (same as MVC)
```

### 3. Environment Setup

Update `config.py` with your database connection (same as MVC):

```python
# Database Configuration (same as MVC)
DATABASE_URL = "mssql+aiodbc://YOUR_USER:YOUR_PASSWORD@YOUR_SERVER/BrainStormEra?driver=ODBC+Driver+17+for+SQL+Server"
DATABASE_URL_SYNC = "mssql+pyodbc://YOUR_USER:YOUR_PASSWORD@YOUR_SERVER/BrainStormEra?driver=ODBC+Driver+17+for+SQL+Server"

# Google AI
GOOGLE_AI_API_KEY = "your_google_ai_api_key_here"

# Security
SECRET_KEY = "your_secret_key_here"
```

**Important**: Use the same database credentials as your MVC application's `appsettings.json`.

### 4. Run with Docker (Recommended)

```bash
# Set environment variables
export GOOGLE_AI_API_KEY=your_api_key_here
export SECRET_KEY=your_secret_key_here

# Start all services
docker-compose up -d

# View logs
docker-compose logs -f chat-service
```

### 5. Run Locally

```bash
# Start database and Redis
docker-compose up -d postgres redis

# Run the application
python main.py
```

## API Documentation

Once running, visit:
- **Swagger UI**: http://localhost:8000/docs
- **ReDoc**: http://localhost:8000/redoc
- **Health Check**: http://localhost:8000/health

## WebSocket Connection

### Chat WebSocket
```javascript
const ws = new WebSocket('ws://localhost:8000/api/v1/chat/ws/{user_id}');

// Send message
ws.send(JSON.stringify({
    type: 'send_message',
    data: {
        receiver_id: 'user123',
        message: 'Hello!'
    }
}));

// Handle messages
ws.onmessage = (event) => {
    const data = JSON.parse(event.data);
    console.log('Received:', data);
};
```

## API Examples

### Chat API

```bash
# Send message
curl -X POST "http://localhost:8000/api/v1/chat/send?current_user_id=user1" \
  -H "Content-Type: application/json" \
  -d '{
    "receiver_id": "user2",
    "message": "Hello there!"
  }'

# Get messages
curl "http://localhost:8000/api/v1/chat/messages/user2?current_user_id=user1&page=1&page_size=20"

# Get conversations
curl "http://localhost:8000/api/v1/chat/conversations?current_user_id=user1"
```

### Chatbot API

```bash
# Chat with AI
curl -X POST "http://localhost:8000/api/v1/chatbot/chat?current_user_id=user1" \
  -H "Content-Type: application/json" \
  -d '{
    "message": "Explain quantum physics",
    "context": "Physics Course - Chapter 3"
  }'

# Get chat history
curl "http://localhost:8000/api/v1/chatbot/history?current_user_id=user1&limit=10"

# Submit feedback
curl -X POST "http://localhost:8000/api/v1/chatbot/feedback?current_user_id=user1" \
  -H "Content-Type: application/json" \
  -d '{
    "conversation_id": "conv123",
    "rating": 5
  }'
```

## Database Schema

**Note**: This service uses the existing MVC database tables. No additional tables are created.

### Used Tables (from MVC)

- **account** - User information (from MVC)
- **conversation** - Chat conversations  
- **conversation_participant** - Conversation participants
- **message_entity** - Individual chat messages
- **chatbot_conversation** - AI chat conversations

The FastAPI service connects to the same SQL Server database as the MVC application and uses the existing schema.

## Configuration

### Google AI Settings

```python
# Chatbot behavior
CHATBOT_TEMPERATURE = 0.7    # Creativity (0.0-1.0)
CHATBOT_TOP_K = 40          # Response diversity
CHATBOT_TOP_P = 0.8         # Response probability
CHATBOT_MAX_TOKENS = 1024   # Max response length
CHATBOT_CACHE_HOURS = 24    # Response caching
CHATBOT_HISTORY_LIMIT = 5   # Context history limit
```

### Performance Tuning

```python
# WebSocket settings
WS_CONNECTION_TIMEOUT = 300     # 5 minutes
WS_HEARTBEAT_INTERVAL = 30      # 30 seconds

# Database settings
DATABASE_POOL_SIZE = 10
DATABASE_MAX_OVERFLOW = 20
```

## Monitoring

### Health Checks

```bash
# Application health
curl http://localhost:8000/health

# WebSocket status
curl http://localhost:8000/ws/status

# Database check
curl http://localhost:8000/api/v1/chatbot/stats
```

### Logs

```bash
# Application logs
docker-compose logs -f chat-service

# Database logs
docker-compose logs -f postgres

# Redis logs
docker-compose logs -f redis
```

## Development

### Running Tests

```bash
# Install test dependencies
pip install pytest pytest-asyncio

# Run tests
pytest tests/

# Run with coverage
pytest --cov=app tests/
```

### Code Formatting

```bash
# Format code
black app/

# Check linting
flake8 app/

# Type checking
mypy app/
```

## Deployment

### Production Checklist

- [ ] Set `DEBUG=false`
- [ ] Configure secure `SECRET_KEY`
- [ ] Set up SSL/HTTPS
- [ ] Configure database backup
- [ ] Set up monitoring (Prometheus/Grafana)
- [ ] Configure log aggregation
- [ ] Set up reverse proxy (Nginx)

### Environment Variables

```bash
# Production settings
DEBUG=false
LOG_LEVEL=INFO
CORS_ORIGINS=["https://brainstormera.com"]

# Database (use connection pooling)
DATABASE_URL=postgresql+asyncpg://user:pass@prod-db:5432/brainstormera

# Security
SECRET_KEY=very-secure-secret-key-here
TRUSTED_HOSTS=["brainstormera.com", "api.brainstormera.com"]
```

## Troubleshooting

### Common Issues

1. **WebSocket connection fails**
   - Check CORS settings
   - Verify user authentication
   - Check firewall settings

2. **AI responses are slow**
   - Check Google AI API quota
   - Enable response caching
   - Optimize context length

3. **Database connection errors**
   - Verify connection string
   - Check database credentials
   - Ensure database is running

4. **High memory usage**
   - Adjust WebSocket connection limits
   - Configure database connection pooling
   - Enable Redis memory optimization

## Support

For questions and support:
- 📧 Email: support@brainstormera.com
- 📚 Documentation: [Internal Wiki]
- 🐛 Issues: [GitHub Issues]

## License

Internal use only - BrainStormEra Educational Platform 