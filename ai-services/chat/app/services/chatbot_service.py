import json
import hashlib
import httpx
from datetime import datetime, timedelta
from typing import List, Optional, Dict, Any
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy.future import select
from sqlalchemy.orm import selectinload
from sqlalchemy import func, desc

from app.models.database import ChatbotConversation, Account, ChatbotCache
from app.models.chatbot_models import (
    ChatbotConversationDTO, 
    GoogleAIResponse, 
    ChatbotServiceResult,
    ConversationStats
)
from app.core.config import settings
import logging
import uuid

logger = logging.getLogger(__name__)


class ChatbotService:
    def __init__(self, db_session: AsyncSession):
        self.db = db_session
        self.http_client = httpx.AsyncClient(timeout=30.0)

    async def get_response(
        self, 
        user_message: str, 
        user_id: str, 
        context: Optional[str] = None
    ) -> str:
        """Get AI response for user message"""
        try:
            # Create cache key for similar questions
            cache_key = self._get_message_hash(user_message.lower().strip())
            
            # Try to get cached response
            cached_response = await self._get_cached_response(cache_key)
            if cached_response:
                logger.info(f"Using cached response for message: {user_message}")
                
                # Still save conversation for tracking
                await self._save_conversation(
                    user_id=user_id,
                    user_message=user_message,
                    bot_response=cached_response,
                    context=context
                )
                return cached_response

            # Get API configuration
            api_key = settings.google_ai_api_key
            if not api_key:
                raise ValueError("Google AI API key not configured")

            # Get user context for personalization
            user_context = await self._get_user_context(user_id)
            
            # Build system prompt
            system_prompt = self._build_system_prompt(user_context, context)
            
            # Get recent conversation history
            recent_history = await self._get_recent_conversation_history(
                user_id, 
                settings.chatbot_history_limit
            )
            history_context = self._build_history_context(recent_history)

            # Prepare request payload for Google AI
            payload = {
                "contents": [
                    {
                        "parts": [
                            {"text": system_prompt},
                            {"text": history_context},
                            {"text": f"User question: {user_message}"}
                        ]
                    }
                ],
                "generationConfig": {
                    "temperature": settings.chatbot_temperature,
                    "topK": settings.chatbot_top_k,
                    "topP": settings.chatbot_top_p,
                    "maxOutputTokens": settings.chatbot_max_tokens,
                    "candidateCount": 1
                },
                "safetySettings": [
                    {"category": "HARM_CATEGORY_HARASSMENT", "threshold": "BLOCK_MEDIUM_AND_ABOVE"},
                    {"category": "HARM_CATEGORY_HATE_SPEECH", "threshold": "BLOCK_MEDIUM_AND_ABOVE"},
                    {"category": "HARM_CATEGORY_SEXUALLY_EXPLICIT", "threshold": "BLOCK_MEDIUM_AND_ABOVE"},
                    {"category": "HARM_CATEGORY_DANGEROUS_CONTENT", "threshold": "BLOCK_MEDIUM_AND_ABOVE"}
                ]
            }

            # Make API call
            api_url = f"https://generativelanguage.googleapis.com/v1beta/models/{settings.google_ai_model}:generateContent"
            headers = {"Content-Type": "application/json"}
            
            response = await self.http_client.post(
                f"{api_url}?key={api_key}",
                json=payload,
                headers=headers
            )

            if response.status_code == 200:
                response_data = response.json()
                google_response = GoogleAIResponse(**response_data)
                
                bot_response = (
                    google_response.candidates[0].content.parts[0].text 
                    if google_response.candidates and 
                       google_response.candidates[0].content.parts and
                       google_response.candidates[0].content.parts[0].text
                    else "Sorry, I couldn't generate a response. Please try again."
                )

                # Cache common educational responses
                if self._is_common_educational_question(user_message):
                    await self._cache_response(cache_key, bot_response)

                # Save conversation
                await self._save_conversation(
                    user_id=user_id,
                    user_message=user_message,
                    bot_response=bot_response,
                    context=context
                )

                return bot_response
            else:
                logger.error(f"Google AI API error: {response.status_code} - {response.text}")
                return "I'm having trouble connecting to my knowledge base. Please try again later."

        except Exception as ex:
            logger.error(f"Error in ChatbotService.get_response: {str(ex)}")
            return "I'm experiencing technical difficulties. Please try again later."

    async def get_conversation_history(self, user_id: str, limit: int = 20) -> List[ChatbotConversationDTO]:
        """Get conversation history for a user"""
        try:
            query = (
                select(ChatbotConversation)
                .where(ChatbotConversation.user_id == user_id)
                .order_by(desc(ChatbotConversation.conversation_time))
                .limit(limit)
            )
            
            result = await self.db.execute(query)
            conversations = result.scalars().all()
            
            return [
                ChatbotConversationDTO(
                    conversation_id=conv.conversation_id,
                    user_id=conv.user_id,
                    conversation_time=conv.conversation_time,
                    user_message=conv.user_message,
                    bot_response=conv.bot_response,
                    conversation_context=conv.conversation_context,
                    feedback_rating=conv.feedback_rating,
                    feedback_comment=conv.feedback_comment
                ) for conv in conversations
            ]

        except Exception as ex:
            logger.error(f"Error getting conversation history for user {user_id}: {str(ex)}")
            return []

    async def save_conversation(self, conversation: ChatbotConversation) -> bool:
        """Save a conversation"""
        try:
            self.db.add(conversation)
            await self.db.flush()
            return True
        except Exception as ex:
            logger.error(f"Error saving conversation: {str(ex)}")
            return False

    async def rate_feedback(self, conversation_id: str, rating: int) -> bool:
        """Update feedback rating for a conversation"""
        try:
            query = select(ChatbotConversation).where(
                ChatbotConversation.conversation_id == conversation_id
            )
            result = await self.db.execute(query)
            conversation = result.scalar_one_or_none()

            if conversation:
                conversation.feedback_rating = rating
                conversation.feedback_submitted_at = datetime.utcnow()
                await self.db.flush()
                return True
            
            return False

        except Exception as ex:
            logger.error(f"Error rating feedback for conversation {conversation_id}: {str(ex)}")
            return False

    async def get_conversation_stats(self) -> ConversationStats:
        """Get conversation statistics"""
        try:
            # Total conversations
            total_conversations_query = select(func.count(ChatbotConversation.conversation_id))
            total_conversations_result = await self.db.execute(total_conversations_query)
            total_conversations = total_conversations_result.scalar() or 0

            # Total unique users
            total_users_query = select(func.count(func.distinct(ChatbotConversation.user_id)))
            total_users_result = await self.db.execute(total_users_query)
            total_users = total_users_result.scalar() or 0

            # Average rating
            avg_rating_query = select(func.avg(ChatbotConversation.feedback_rating)).where(
                ChatbotConversation.feedback_rating.isnot(None)
            )
            avg_rating_result = await self.db.execute(avg_rating_query)
            average_rating = avg_rating_result.scalar()

            # Conversations with feedback
            feedback_query = select(func.count(ChatbotConversation.conversation_id)).where(
                ChatbotConversation.feedback_rating.isnot(None)
            )
            feedback_result = await self.db.execute(feedback_query)
            conversations_with_feedback = feedback_result.scalar() or 0

            # Today's conversations
            today = datetime.utcnow().date()
            today_query = select(func.count(ChatbotConversation.conversation_id)).where(
                func.date(ChatbotConversation.conversation_time) == today
            )
            today_result = await self.db.execute(today_query)
            today_conversations = today_result.scalar() or 0

            # Weekly conversations
            week_ago = datetime.utcnow() - timedelta(days=7)
            weekly_query = select(func.count(ChatbotConversation.conversation_id)).where(
                ChatbotConversation.conversation_time >= week_ago
            )
            weekly_result = await self.db.execute(weekly_query)
            weekly_conversations = weekly_result.scalar() or 0

            return ConversationStats(
                total_conversations=total_conversations,
                total_users=total_users,
                average_rating=average_rating,
                conversations_with_feedback=conversations_with_feedback,
                today_conversations=today_conversations,
                weekly_conversations=weekly_conversations
            )

        except Exception as ex:
            logger.error(f"Error getting conversation stats: {str(ex)}")
            return ConversationStats(
                total_conversations=0,
                total_users=0,
                conversations_with_feedback=0,
                today_conversations=0,
                weekly_conversations=0
            )

    # Private helper methods
    async def _get_user_context(self, user_id: str) -> str:
        """Get user context for personalization"""
        try:
            query = select(Account).where(Account.user_id == user_id)
            result = await self.db.execute(query)
            user = result.scalar_one_or_none()

            if not user:
                return ""

            context_parts = [f"User {user.full_name or user.username} ({user.user_email})"]
            
            # Add more context based on user data if available
            # This could include enrolled courses, progress, etc.
            
            return "\n".join(context_parts)

        except Exception as ex:
            logger.error(f"Error getting user context for {user_id}: {str(ex)}")
            return ""

    def _build_system_prompt(self, user_context: str, page_context: Optional[str]) -> str:
        """Build system prompt for AI"""
        base_prompt = """You are BrainStormEra's educational AI assistant. You help students learn and understand course materials. 
        
        Guidelines:
        - Provide clear, educational responses
        - Be encouraging and supportive
        - Break down complex topics into understandable parts
        - Ask follow-up questions to ensure understanding
        - Stay focused on educational content
        """

        if user_context:
            base_prompt += f"\n\nUser Context:\n{user_context}"

        if page_context:
            base_prompt += f"\n\nPage Context:\n{page_context}"

        return base_prompt

    def _get_message_hash(self, message: str) -> str:
        """Generate hash for message caching"""
        return hashlib.md5(message.encode()).hexdigest()

    async def _get_recent_conversation_history(self, user_id: str, limit: int = 3) -> List[ChatbotConversation]:
        """Get recent conversation history for context"""
        try:
            query = (
                select(ChatbotConversation)
                .where(ChatbotConversation.user_id == user_id)
                .order_by(desc(ChatbotConversation.conversation_time))
                .limit(limit)
            )
            
            result = await self.db.execute(query)
            return result.scalars().all()

        except Exception as ex:
            logger.error(f"Error getting recent history for {user_id}: {str(ex)}")
            return []

    def _build_history_context(self, history: List[ChatbotConversation]) -> str:
        """Build context from conversation history"""
        if not history:
            return ""

        context_parts = ["Recent conversation context:"]
        for conv in reversed(history):  # Chronological order
            context_parts.append(f"User: {conv.user_message}")
            context_parts.append(f"Assistant: {conv.bot_response}")

        return "\n".join(context_parts)

    def _is_common_educational_question(self, message: str) -> bool:
        """Check if message is a common educational question for caching"""
        common_patterns = [
            "what is", "how to", "explain", "definition", "meaning",
            "difference between", "example", "steps", "process"
        ]
        
        message_lower = message.lower()
        return any(pattern in message_lower for pattern in common_patterns)

    async def _get_cached_response(self, cache_key: str) -> Optional[str]:
        """Get cached response if available"""
        try:
            query = select(ChatbotCache).where(
                ChatbotCache.cache_key == cache_key,
                ChatbotCache.expires_at > datetime.utcnow()
            )
            result = await self.db.execute(query)
            cache_entry = result.scalar_one_or_none()

            return cache_entry.cached_response if cache_entry else None

        except Exception as ex:
            logger.error(f"Error getting cached response: {str(ex)}")
            return None

    async def _cache_response(self, cache_key: str, response: str):
        """Cache response for future use"""
        try:
            expires_at = datetime.utcnow() + timedelta(hours=settings.chatbot_cache_hours)
            
            cache_entry = ChatbotCache(
                cache_key=cache_key,
                cached_response=response,
                expires_at=expires_at
            )
            
            self.db.add(cache_entry)
            await self.db.flush()

        except Exception as ex:
            logger.error(f"Error caching response: {str(ex)}")

    async def _save_conversation(
        self, 
        user_id: str, 
        user_message: str, 
        bot_response: str, 
        context: Optional[str] = None
    ):
        """Save conversation to database"""
        try:
            conversation = ChatbotConversation(
                conversation_id=str(uuid.uuid4()),
                user_id=user_id,
                conversation_time=datetime.utcnow(),
                user_message=user_message,
                bot_response=bot_response,
                conversation_context=context
            )
            
            self.db.add(conversation)
            await self.db.flush()

        except Exception as ex:
            logger.error(f"Error saving conversation: {str(ex)}")

    async def __aenter__(self):
        return self

    async def __aexit__(self, exc_type, exc_val, exc_tb):
        await self.http_client.aclose() 