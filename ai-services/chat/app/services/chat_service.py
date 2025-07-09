from datetime import datetime
from typing import List, Optional
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy.future import select
from sqlalchemy.orm import selectinload
from sqlalchemy import or_, and_, func, desc

from app.models.database import MessageEntity, Conversation, Account
from app.models.chat_models import ChatMessageDTO, ChatUserDTO
import logging
import uuid

logger = logging.getLogger(__name__)


class ChatService:
    def __init__(self, db_session: AsyncSession):
        self.db = db_session

    async def get_conversation_messages(
        self, 
        sender_id: str, 
        receiver_id: str, 
        page: int = 1, 
        page_size: int = 50
    ) -> List[MessageEntity]:
        """Get conversation messages between two users with pagination"""
        try:
            offset = (page - 1) * page_size
            
            query = (
                select(MessageEntity)
                .options(selectinload(MessageEntity.sender))
                .where(
                    or_(
                        and_(
                            MessageEntity.sender_id == sender_id,
                            MessageEntity.receiver_id == receiver_id,
                            MessageEntity.is_deleted_by_sender == False
                        ),
                        and_(
                            MessageEntity.sender_id == receiver_id,
                            MessageEntity.receiver_id == sender_id,
                            MessageEntity.is_deleted_by_receiver == False
                        )
                    )
                )
                .order_by(desc(MessageEntity.message_created_at))
                .offset(offset)
                .limit(page_size)
            )
            
            result = await self.db.execute(query)
            messages = result.scalars().all()
            
            return list(reversed(messages))  # Return in chronological order
            
        except Exception as ex:
            logger.error(f"Error getting conversation messages between {sender_id} and {receiver_id}: {str(ex)}")
            return []

    async def send_message(
        self, 
        sender_id: str, 
        receiver_id: str, 
        message: str, 
        reply_to_message_id: Optional[str] = None
    ) -> Optional[MessageEntity]:
        """Send a message between users"""
        try:
            logger.info(f"Attempting to send message from {sender_id} to {receiver_id}")

            # Create or get conversation
            conversation = await self.get_or_create_conversation(sender_id, receiver_id)
            if not conversation:
                logger.error(f"Failed to create/get conversation between {sender_id} and {receiver_id}")
                return None

            logger.info(f"Using conversation: {conversation.conversation_id}")

            # Create message entity
            message_entity = MessageEntity(
                message_id=str(uuid.uuid4()),
                sender_id=sender_id,
                receiver_id=receiver_id,
                conversation_id=conversation.conversation_id,
                message_content=message,
                message_type="text",
                is_read=False,
                is_deleted_by_sender=False,
                is_deleted_by_receiver=False,
                reply_to_message_id=reply_to_message_id,
                is_edited=False,
                message_created_at=datetime.utcnow(),
                message_updated_at=datetime.utcnow()
            )

            logger.info(f"Created message entity with ID: {message_entity.message_id}")

            # Add to database
            self.db.add(message_entity)
            await self.db.flush()

            # Update conversation's last message
            conversation.last_message_id = message_entity.message_id
            conversation.last_message_at = message_entity.message_created_at
            conversation.conversation_updated_at = datetime.utcnow()

            await self.db.flush()

            # Get message with sender information
            result = await self.get_message_by_id(message_entity.message_id)
            
            logger.info("Message saved to database successfully")
            return result

        except Exception as ex:
            logger.error(f"Error sending message from {sender_id} to {receiver_id}: {str(ex)}")
            await self.db.rollback()
            return None

    async def mark_message_as_read(self, message_id: str, user_id: str) -> bool:
        """Mark a message as read by the receiver"""
        try:
            query = select(MessageEntity).where(
                MessageEntity.message_id == message_id,
                MessageEntity.receiver_id == user_id
            )
            result = await self.db.execute(query)
            message = result.scalar_one_or_none()

            if message:
                message.is_read = True
                message.message_updated_at = datetime.utcnow()
                await self.db.flush()
                return True
            
            return False

        except Exception as ex:
            logger.error(f"Error marking message {message_id} as read by {user_id}: {str(ex)}")
            return False

    async def get_message_by_id(self, message_id: str) -> Optional[MessageEntity]:
        """Get message by ID with sender information"""
        try:
            query = (
                select(MessageEntity)
                .options(selectinload(MessageEntity.sender))
                .where(MessageEntity.message_id == message_id)
            )
            result = await self.db.execute(query)
            return result.scalar_one_or_none()

        except Exception as ex:
            logger.error(f"Error getting message {message_id}: {str(ex)}")
            return None

    async def get_user_conversations(self, user_id: str) -> List[Conversation]:
        """Get all conversations for a user"""
        try:
            query = (
                select(Conversation)
                .options(
                    selectinload(Conversation.participant1),
                    selectinload(Conversation.participant2)
                )
                .where(
                    or_(
                        Conversation.participant1_id == user_id,
                        Conversation.participant2_id == user_id
                    )
                )
                .order_by(desc(Conversation.last_message_at))
            )
            
            result = await self.db.execute(query)
            return result.scalars().all()

        except Exception as ex:
            logger.error(f"Error getting conversations for user {user_id}: {str(ex)}")
            return []

    async def get_unread_message_count(self, user_id: str, sender_id: str) -> int:
        """Get count of unread messages from a specific sender"""
        try:
            query = select(func.count(MessageEntity.message_id)).where(
                MessageEntity.receiver_id == user_id,
                MessageEntity.sender_id == sender_id,
                MessageEntity.is_read == False,
                MessageEntity.is_deleted_by_receiver == False
            )
            
            result = await self.db.execute(query)
            return result.scalar() or 0

        except Exception as ex:
            logger.error(f"Error getting unread message count for {user_id} from {sender_id}: {str(ex)}")
            return 0

    async def get_unread_messages(self, user_id: str) -> List[MessageEntity]:
        """Get all unread messages for a user"""
        try:
            query = (
                select(MessageEntity)
                .options(selectinload(MessageEntity.sender))
                .where(
                    MessageEntity.receiver_id == user_id,
                    MessageEntity.is_read == False,
                    MessageEntity.is_deleted_by_receiver == False
                )
                .order_by(desc(MessageEntity.message_created_at))
            )
            
            result = await self.db.execute(query)
            return result.scalars().all()

        except Exception as ex:
            logger.error(f"Error getting unread messages for user {user_id}: {str(ex)}")
            return []

    async def can_user_access_conversation(self, user_id: str, conversation_id: str) -> bool:
        """Check if user can access a conversation"""
        try:
            query = select(Conversation).where(
                Conversation.conversation_id == conversation_id,
                or_(
                    Conversation.participant1_id == user_id,
                    Conversation.participant2_id == user_id
                )
            )
            
            result = await self.db.execute(query)
            conversation = result.scalar_one_or_none()
            return conversation is not None

        except Exception as ex:
            logger.error(f"Error checking conversation access for user {user_id} and conversation {conversation_id}: {str(ex)}")
            return False

    async def get_chat_users(self, current_user_id: str) -> List[Account]:
        """Get users that the current user has chatted with"""
        try:
            # Get all users who have sent or received messages from current user
            query = (
                select(Account)
                .join(
                    MessageEntity,
                    or_(
                        Account.user_id == MessageEntity.sender_id,
                        Account.user_id == MessageEntity.receiver_id
                    )
                )
                .where(
                    or_(
                        MessageEntity.sender_id == current_user_id,
                        MessageEntity.receiver_id == current_user_id
                    ),
                    Account.user_id != current_user_id
                )
                .distinct()
                .order_by(Account.last_active.desc())
            )
            
            result = await self.db.execute(query)
            return result.scalars().all()

        except Exception as ex:
            logger.error(f"Error getting chat users for {current_user_id}: {str(ex)}")
            return []

    async def get_last_message_between_users(self, user_id1: str, user_id2: str) -> Optional[MessageEntity]:
        """Get the last message between two users"""
        try:
            query = (
                select(MessageEntity)
                .where(
                    or_(
                        and_(
                            MessageEntity.sender_id == user_id1,
                            MessageEntity.receiver_id == user_id2
                        ),
                        and_(
                            MessageEntity.sender_id == user_id2,
                            MessageEntity.receiver_id == user_id1
                        )
                    )
                )
                .order_by(desc(MessageEntity.message_created_at))
                .limit(1)
            )
            
            result = await self.db.execute(query)
            return result.scalar_one_or_none()

        except Exception as ex:
            logger.error(f"Error getting last message between {user_id1} and {user_id2}: {str(ex)}")
            return None

    async def set_user_online_status(self, user_id: str, is_online: bool) -> bool:
        """Set user online status"""
        try:
            query = select(Account).where(Account.user_id == user_id)
            result = await self.db.execute(query)
            user = result.scalar_one_or_none()

            if user:
                user.is_online = is_online
                user.last_active = datetime.utcnow() if is_online else user.last_active
                await self.db.flush()
                return True
            
            return False

        except Exception as ex:
            logger.error(f"Error setting online status for user {user_id}: {str(ex)}")
            return False

    async def get_or_create_conversation(self, user_id1: str, user_id2: str) -> Optional[Conversation]:
        """Get existing conversation or create new one between two users"""
        try:
            # Try to find existing conversation
            query = select(Conversation).where(
                or_(
                    and_(
                        Conversation.participant1_id == user_id1,
                        Conversation.participant2_id == user_id2
                    ),
                    and_(
                        Conversation.participant1_id == user_id2,
                        Conversation.participant2_id == user_id1
                    )
                )
            )
            
            result = await self.db.execute(query)
            conversation = result.scalar_one_or_none()

            if not conversation:
                # Create new conversation
                conversation = Conversation(
                    conversation_id=str(uuid.uuid4()),
                    participant1_id=user_id1,
                    participant2_id=user_id2,
                    conversation_created_at=datetime.utcnow(),
                    conversation_updated_at=datetime.utcnow()
                )
                self.db.add(conversation)
                await self.db.flush()

            return conversation

        except Exception as ex:
            logger.error(f"Error getting/creating conversation between {user_id1} and {user_id2}: {str(ex)}")
            return None

    async def delete_message(self, message_id: str, user_id: str) -> bool:
        """Soft delete message for a user"""
        try:
            query = select(MessageEntity).where(MessageEntity.message_id == message_id)
            result = await self.db.execute(query)
            message = result.scalar_one_or_none()

            if not message:
                return False

            # Mark as deleted for the appropriate user
            if message.sender_id == user_id:
                message.is_deleted_by_sender = True
            elif message.receiver_id == user_id:
                message.is_deleted_by_receiver = True
            else:
                return False  # User not authorized

            message.message_updated_at = datetime.utcnow()
            await self.db.flush()
            return True

        except Exception as ex:
            logger.error(f"Error deleting message {message_id} for user {user_id}: {str(ex)}")
            return False

    async def edit_message(self, message_id: str, new_content: str, user_id: str) -> bool:
        """Edit message content"""
        try:
            query = select(MessageEntity).where(
                MessageEntity.message_id == message_id,
                MessageEntity.sender_id == user_id  # Only sender can edit
            )
            result = await self.db.execute(query)
            message = result.scalar_one_or_none()

            if message:
                message.message_content = new_content
                message.is_edited = True
                message.message_updated_at = datetime.utcnow()
                await self.db.flush()
                return True
            
            return False

        except Exception as ex:
            logger.error(f"Error editing message {message_id}: {str(ex)}")
            return False 