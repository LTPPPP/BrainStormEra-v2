from datetime import datetime
from sqlalchemy import Column, String, DateTime, Boolean, Text, Integer, ForeignKey, Float, BigInteger
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import relationship
from sqlalchemy.dialects.postgresql import UUID
import uuid

Base = declarative_base()


class Account(Base):
    __tablename__ = "account"
    
    user_id = Column(String(36), primary_key=True)
    user_role = Column(String(36), nullable=False)
    username = Column(String(255), nullable=False, unique=True)
    password_hash = Column(String(255), nullable=False)
    user_email = Column(String(255), nullable=False, unique=True)
    full_name = Column(String(255), nullable=True)
    payment_point = Column(String, nullable=True)  # Using decimal in DB
    date_of_birth = Column(String, nullable=True)  # DateOnly in C#
    gender = Column(Integer, nullable=True)  # short in C#
    phone_number = Column(String(15), nullable=True)
    user_address = Column(Text, nullable=True)
    user_image = Column(Text, nullable=True)
    is_banned = Column(Boolean, default=False)
    bank_account_number = Column(String(50), nullable=True)
    bank_name = Column(String(255), nullable=True)
    account_holder_name = Column(String(255), nullable=True)
    last_login = Column(DateTime, nullable=True)
    account_created_at = Column(DateTime, default=datetime.utcnow)
    account_updated_at = Column(DateTime, default=datetime.utcnow)
    
    # Relationships
    sent_messages = relationship("MessageEntity", foreign_keys="MessageEntity.sender_id", back_populates="sender")
    received_messages = relationship("MessageEntity", foreign_keys="MessageEntity.receiver_id", back_populates="receiver")
    chatbot_conversations = relationship("ChatbotConversation", back_populates="user")
    conversation_participants = relationship("ConversationParticipant", back_populates="user")
    created_conversations = relationship("Conversation", back_populates="created_by_navigation")


class Conversation(Base):
    __tablename__ = "conversation"
    
    conversation_id = Column(String(36), primary_key=True)
    conversation_type = Column(String, nullable=True)
    created_by = Column(String(36), ForeignKey("account.user_id"), nullable=False)
    is_active = Column(Boolean, default=True)
    last_message_id = Column(String(36), nullable=True)
    last_message_at = Column(DateTime, nullable=True)
    conversation_created_at = Column(DateTime, default=datetime.utcnow)
    conversation_updated_at = Column(DateTime, default=datetime.utcnow)
    
    # Relationships
    created_by_navigation = relationship("Account", back_populates="created_conversations")
    last_message = relationship("MessageEntity", foreign_keys=[last_message_id], post_update=True)
    message_entities = relationship("MessageEntity", foreign_keys="MessageEntity.conversation_id", back_populates="conversation")
    conversation_participants = relationship("ConversationParticipant", back_populates="conversation")


class ConversationParticipant(Base):
    __tablename__ = "conversation_participant"
    
    conversation_id = Column(String(36), ForeignKey("conversation.conversation_id"), primary_key=True)
    user_id = Column(String(36), ForeignKey("account.user_id"), primary_key=True)
    participant_role = Column(String, nullable=True)
    joined_at = Column(DateTime, default=datetime.utcnow)
    left_at = Column(DateTime, nullable=True)
    is_active = Column(Boolean, default=True)
    is_muted = Column(Boolean, default=False)
    last_read_message_id = Column(String(36), nullable=True)
    last_read_at = Column(DateTime, nullable=True)
    
    # Relationships
    conversation = relationship("Conversation", back_populates="conversation_participants")
    user = relationship("Account", back_populates="conversation_participants")
    last_read_message = relationship("MessageEntity", foreign_keys=[last_read_message_id], post_update=True)


class MessageEntity(Base):
    __tablename__ = "message_entity"
    
    message_id = Column(String(36), primary_key=True)
    sender_id = Column(String(36), ForeignKey("account.user_id"), nullable=False)
    receiver_id = Column(String(36), ForeignKey("account.user_id"), nullable=False)
    conversation_id = Column(String(36), ForeignKey("conversation.conversation_id"), nullable=False)
    message_content = Column(Text, nullable=False)
    message_type = Column(String, nullable=True)
    attachment_url = Column(Text, nullable=True)
    attachment_name = Column(String, nullable=True)
    attachment_size = Column(BigInteger, nullable=True)
    is_read = Column(Boolean, default=False)
    read_at = Column(DateTime, nullable=True)
    is_deleted_by_sender = Column(Boolean, default=False)
    is_deleted_by_receiver = Column(Boolean, default=False)
    reply_to_message_id = Column(String(36), nullable=True)
    is_edited = Column(Boolean, default=False)
    edited_at = Column(DateTime, nullable=True)
    original_content = Column(Text, nullable=True)
    message_created_at = Column(DateTime, default=datetime.utcnow)
    message_updated_at = Column(DateTime, default=datetime.utcnow)
    
    # Relationships
    sender = relationship("Account", foreign_keys=[sender_id], back_populates="sent_messages")
    receiver = relationship("Account", foreign_keys=[receiver_id], back_populates="received_messages")
    conversation = relationship("Conversation", foreign_keys=[conversation_id], back_populates="message_entities")
    reply_to_message = relationship("MessageEntity", remote_side=[message_id], post_update=True)
    inverse_reply_to_message = relationship("MessageEntity", remote_side=[reply_to_message_id], post_update=True)


class ChatbotConversation(Base):
    __tablename__ = "chatbot_conversation"
    
    conversation_id = Column(String(36), primary_key=True)
    user_id = Column(String(36), ForeignKey("account.user_id"), nullable=False)
    conversation_time = Column(DateTime, default=datetime.utcnow)
    user_message = Column(Text, nullable=False)
    bot_response = Column(Text, nullable=False)
    conversation_context = Column(Text, nullable=True)
    feedback_rating = Column(Integer, nullable=True)  # byte in C#, but using Integer for compatibility
    
    # Relationships
    user = relationship("Account", back_populates="chatbot_conversations") 