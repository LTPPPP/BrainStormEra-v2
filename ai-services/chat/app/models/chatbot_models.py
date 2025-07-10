from datetime import datetime
from typing import Optional, List, Dict, Any
from pydantic import BaseModel, Field, validator


# Request Models
class ChatbotRequest(BaseModel):
    message: str = Field(..., min_length=1, max_length=2000, description="User message to chatbot")
    context: Optional[str] = Field(None, description="Additional context for the conversation")
    page_path: Optional[str] = Field(None, description="Current page path for context")
    course_id: Optional[str] = Field(None, description="Current course ID")
    chapter_id: Optional[str] = Field(None, description="Current chapter ID")
    lesson_id: Optional[str] = Field(None, description="Current lesson ID")

    @validator('message')
    def validate_message(cls, v):
        if not v.strip():
            raise ValueError('Message cannot be empty')
        return v.strip()


class FeedbackRequest(BaseModel):
    conversation_id: str = Field(..., description="ID of the conversation to rate")
    rating: int = Field(..., ge=1, le=5, description="Rating from 1 to 5")


class ChatHistoryRequest(BaseModel):
    limit: int = Field(20, ge=1, le=100, description="Number of conversations to retrieve")


# Response Models
class ChatbotConversationDTO(BaseModel):
    conversation_id: str
    user_id: str
    conversation_time: datetime
    user_message: str
    bot_response: str
    conversation_context: Optional[str] = None
    feedback_rating: Optional[int] = None

    class Config:
        from_attributes = True


class ChatbotResponse(BaseModel):
    success: bool
    bot_response: Optional[str] = None
    conversation_id: Optional[str] = None
    timestamp: datetime
    message: str
    errors: Optional[List[str]] = None


class ChatHistoryResponse(BaseModel):
    success: bool
    conversations: List[ChatbotConversationDTO] = []
    total_count: int = 0
    message: str
    errors: Optional[List[str]] = None


class FeedbackResponse(BaseModel):
    success: bool
    message: str
    errors: Optional[List[str]] = None


# Google AI API Response Models (for internal use)
class GoogleAIPart(BaseModel):
    text: Optional[str] = None


class GoogleAIContent(BaseModel):
    parts: List[GoogleAIPart] = []


class GoogleAICandidate(BaseModel):
    content: GoogleAIContent


class GoogleAIResponse(BaseModel):
    candidates: List[GoogleAICandidate] = []


# Validation Result Models
class ValidationResult(BaseModel):
    is_valid: bool
    errors: List[str] = []


# Service Result Models
class ChatbotServiceResult(BaseModel):
    success: bool
    data: Optional[Any] = None
    message: str
    errors: List[str] = []
    status_code: int = 200


# Analytics Models
class ConversationStats(BaseModel):
    total_conversations: int
    total_users: int
    average_rating: Optional[float] = None
    conversations_with_feedback: int
    today_conversations: int
    weekly_conversations: int


class DailyUsageStats(BaseModel):
    date: str
    conversation_count: int


class FeedbackStats(BaseModel):
    rating: int
    count: int
    percentage: float 