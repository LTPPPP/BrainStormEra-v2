from datetime import datetime
from typing import Optional, List
from pydantic import BaseModel, Field


# Request Models
class SendMessageRequest(BaseModel):
    receiver_id: str = Field(..., description="ID of the message receiver")
    message: str = Field(..., description="Message content")
    reply_to_message_id: Optional[str] = Field(None, description="ID of message being replied to")


class MarkAsReadRequest(BaseModel):
    message_id: str = Field(..., description="ID of the message to mark as read")


class DeleteMessageRequest(BaseModel):
    message_id: str = Field(..., description="ID of the message to delete")


class EditMessageRequest(BaseModel):
    message_id: str = Field(..., description="ID of the message to edit")
    new_content: str = Field(..., description="New message content")


class GetMessagesRequest(BaseModel):
    receiver_id: str = Field(..., description="ID of the conversation partner")
    page: int = Field(1, ge=1, description="Page number")
    page_size: int = Field(50, ge=1, le=100, description="Number of messages per page")


class GetUnreadCountRequest(BaseModel):
    sender_id: str = Field(..., description="ID of the message sender")


# Response Models
class ChatMessageDTO(BaseModel):
    message_id: str
    sender_id: str
    receiver_id: str
    content: str
    message_type: str
    is_read: bool
    reply_to_message_id: Optional[str] = None
    is_edited: bool
    created_at: datetime
    sender_name: str
    sender_avatar: Optional[str] = None

    class Config:
        from_attributes = True


class ChatUserDTO(BaseModel):
    user_id: str
    username: str
    user_image: Optional[str] = None
    last_active: Optional[datetime] = None
    is_online: bool
    last_message: Optional[str] = None
    last_message_time: Optional[datetime] = None
    unread_count: int

    class Config:
        from_attributes = True


class ConversationViewModel(BaseModel):
    current_user_id: str
    receiver_id: str
    chat_users: List[ChatUserDTO] = []
    messages: List[ChatMessageDTO] = []


class ChatIndexViewModel(BaseModel):
    current_user_id: str
    users: List[ChatUserDTO] = []


# WebSocket Message Types
class WebSocketMessage(BaseModel):
    type: str  # message_sent, message_received, user_typing, user_stopped_typing, etc.
    data: dict


class TypingIndicator(BaseModel):
    sender_id: str
    receiver_id: str
    is_typing: bool


# Response wrapper
class ChatResponse(BaseModel):
    success: bool
    message: str
    data: Optional[dict] = None
    errors: Optional[List[str]] = None 