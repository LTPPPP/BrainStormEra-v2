from datetime import datetime
from sqlalchemy import Column, String, DateTime, Boolean, Text, Integer, ForeignKey, Float
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import relationship
from sqlalchemy.dialects.postgresql import UUID
import uuid

Base = declarative_base()


class Account(Base):
    __tablename__ = "accounts"
    
    user_id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    username = Column(String(100), nullable=False)
    user_email = Column(String(255), nullable=False, unique=True)
    full_name = Column(String(255), nullable=True)
    user_image = Column(String(500), nullable=True)
    last_active = Column(DateTime, nullable=True)
    is_online = Column(Boolean, default=False)
    created_at = Column(DateTime, default=datetime.utcnow)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    
    # Relationships
    sent_messages = relationship("MessageEntity", foreign_keys="MessageEntity.sender_id", back_populates="sender")
    received_messages = relationship("MessageEntity", foreign_keys="MessageEntity.receiver_id", back_populates="receiver")
    chatbot_conversations = relationship("ChatbotConversation", back_populates="user")


class Conversation(Base):
    __tablename__ = "conversations"
    
    conversation_id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    participant1_id = Column(String, ForeignKey("accounts.user_id"), nullable=False)
    participant2_id = Column(String, ForeignKey("accounts.user_id"), nullable=False)
    last_message_id = Column(String, nullable=True)
    last_message_at = Column(DateTime, nullable=True)
    conversation_created_at = Column(DateTime, default=datetime.utcnow)
    conversation_updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    
    # Relationships
    participant1 = relationship("Account", foreign_keys=[participant1_id])
    participant2 = relationship("Account", foreign_keys=[participant2_id])
    messages = relationship("MessageEntity", back_populates="conversation")


class MessageEntity(Base):
    __tablename__ = "messages"
    
    message_id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    sender_id = Column(String, ForeignKey("accounts.user_id"), nullable=False)
    receiver_id = Column(String, ForeignKey("accounts.user_id"), nullable=False)
    conversation_id = Column(String, ForeignKey("conversations.conversation_id"), nullable=False)
    message_content = Column(Text, nullable=False)
    message_type = Column(String(20), default="text")  # text, image, file, etc.
    is_read = Column(Boolean, default=False)
    is_deleted_by_sender = Column(Boolean, default=False)
    is_deleted_by_receiver = Column(Boolean, default=False)
    reply_to_message_id = Column(String, nullable=True)
    is_edited = Column(Boolean, default=False)
    message_created_at = Column(DateTime, default=datetime.utcnow)
    message_updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)
    
    # Relationships
    sender = relationship("Account", foreign_keys=[sender_id], back_populates="sent_messages")
    receiver = relationship("Account", foreign_keys=[receiver_id], back_populates="received_messages")
    conversation = relationship("Conversation", back_populates="messages")


class ChatbotConversation(Base):
    __tablename__ = "chatbot_conversations"
    
    conversation_id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    user_id = Column(String, ForeignKey("accounts.user_id"), nullable=False)
    conversation_time = Column(DateTime, default=datetime.utcnow)
    user_message = Column(Text, nullable=False)
    bot_response = Column(Text, nullable=False)
    conversation_context = Column(Text, nullable=True)
    feedback_rating = Column(Integer, nullable=True)  # 1-5 rating
    feedback_comment = Column(Text, nullable=True)
    feedback_submitted_at = Column(DateTime, nullable=True)
    
    # Relationships
    user = relationship("Account", back_populates="chatbot_conversations")


# Additional utility tables for caching and analytics
class ChatbotCache(Base):
    __tablename__ = "chatbot_cache"
    
    cache_key = Column(String, primary_key=True)
    cached_response = Column(Text, nullable=False)
    created_at = Column(DateTime, default=datetime.utcnow)
    expires_at = Column(DateTime, nullable=False)


class UserSession(Base):
    __tablename__ = "user_sessions"
    
    session_id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    user_id = Column(String, ForeignKey("accounts.user_id"), nullable=False)
    connection_id = Column(String, nullable=True)  # WebSocket connection ID
    connected_at = Column(DateTime, default=datetime.utcnow)
    disconnected_at = Column(DateTime, nullable=True)
    is_active = Column(Boolean, default=True)
    
    # Relationships
    user = relationship("Account")


# Analytics tables
class ConversationAnalytics(Base):
    __tablename__ = "conversation_analytics"
    
    id = Column(String, primary_key=True, default=lambda: str(uuid.uuid4()))
    date = Column(DateTime, nullable=False)
    total_conversations = Column(Integer, default=0)
    unique_users = Column(Integer, default=0)
    average_response_time = Column(Float, nullable=True)
    created_at = Column(DateTime, default=datetime.utcnow) 