# Configuration Example
# Copy this file to config.py and update with your values

import os
from typing import List

# Database Configuration
DATABASE_URL = "postgresql+asyncpg://username:password@localhost:5432/brainstormera"
DATABASE_URL_SYNC = "postgresql://username:password@localhost:5432/brainstormera"

# Redis Configuration
REDIS_URL = "redis://localhost:6379/0"

# Google AI Configuration
GOOGLE_AI_API_KEY = "your_google_ai_api_key_here"
GOOGLE_AI_MODEL = "gemini-pro"

# Chatbot Configuration
CHATBOT_TEMPERATURE = 0.7
CHATBOT_TOP_K = 40
CHATBOT_TOP_P = 0.8
CHATBOT_MAX_TOKENS = 1024
CHATBOT_CACHE_HOURS = 24
CHATBOT_HISTORY_LIMIT = 5

# Security
SECRET_KEY = "your_secret_key_here"
ALGORITHM = "HS256"
ACCESS_TOKEN_EXPIRE_MINUTES = 30

# Application Settings
DEBUG = True
LOG_LEVEL = "INFO"
CORS_ORIGINS: List[str] = ["http://localhost:3000", "http://localhost:5000"]

# WebSocket Settings
WS_CONNECTION_TIMEOUT = 300
WS_HEARTBEAT_INTERVAL = 30 