from datetime import datetime
from typing import List, Optional
from fastapi import APIRouter, Depends, HTTPException, Query
from sqlalchemy.ext.asyncio import AsyncSession

from app.core.database import get_async_session
from app.services.chatbot_service import ChatbotService
from app.models.chatbot_models import (
    ChatbotRequest,
    FeedbackRequest,
    ChatHistoryRequest,
    ChatbotResponse,
    ChatHistoryResponse,
    FeedbackResponse,
    ChatbotConversationDTO,
    ConversationStats
)
import logging

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/chatbot", tags=["chatbot"])


@router.post("/chat", response_model=ChatbotResponse)
async def chat_with_bot(
    request: ChatbotRequest,
    current_user_id: str = Query(..., description="Current user ID"),  # In real app, this would come from auth
    db: AsyncSession = Depends(get_async_session)
):
    """Send a message to the AI chatbot and get a response"""
    try:
        chatbot_service = ChatbotService(db)
        
        # Get context for enhanced response
        context = request.context
        if request.page_path or request.course_id or request.chapter_id or request.lesson_id:
            # Build enhanced context from page information
            context_parts = []
            if request.page_path:
                context_parts.append(f"Current page: {request.page_path}")
            if request.course_id:
                context_parts.append(f"Course ID: {request.course_id}")
            if request.chapter_id:
                context_parts.append(f"Chapter ID: {request.chapter_id}")
            if request.lesson_id:
                context_parts.append(f"Lesson ID: {request.lesson_id}")
            
            enhanced_context = ". ".join(context_parts)
            context = f"{context}. {enhanced_context}" if context else enhanced_context

        # Get AI response
        bot_response = await chatbot_service.get_response(
            user_message=request.message,
            user_id=current_user_id,
            context=context
        )

        return ChatbotResponse(
            success=True,
            bot_response=bot_response,
            timestamp=datetime.utcnow(),
            message="Response generated successfully"
        )

    except Exception as e:
        logger.error(f"Error in chatbot chat: {str(e)}")
        return ChatbotResponse(
            success=False,
            timestamp=datetime.utcnow(),
            message="Failed to generate response",
            errors=[str(e)]
        )


@router.get("/history", response_model=ChatHistoryResponse)
async def get_conversation_history(
    current_user_id: str = Query(..., description="Current user ID"),
    limit: int = Query(20, ge=1, le=100, description="Number of conversations to retrieve"),
    db: AsyncSession = Depends(get_async_session)
):
    """Get conversation history for the current user"""
    try:
        chatbot_service = ChatbotService(db)
        
        conversations = await chatbot_service.get_conversation_history(
            user_id=current_user_id,
            limit=limit
        )

        return ChatHistoryResponse(
            success=True,
            conversations=conversations,
            total_count=len(conversations),
            message="History retrieved successfully"
        )

    except Exception as e:
        logger.error(f"Error getting chatbot history: {str(e)}")
        return ChatHistoryResponse(
            success=False,
            message="Failed to retrieve conversation history",
            errors=[str(e)]
        )


@router.post("/feedback", response_model=FeedbackResponse)
async def submit_feedback(
    request: FeedbackRequest,
    current_user_id: str = Query(..., description="Current user ID"),
    db: AsyncSession = Depends(get_async_session)
):
    """Submit feedback for a chatbot conversation"""
    try:
        chatbot_service = ChatbotService(db)
        
        success = await chatbot_service.rate_feedback(
            conversation_id=request.conversation_id,
            rating=request.rating
        )

        if success:
            return FeedbackResponse(
                success=True,
                message="Feedback submitted successfully"
            )
        else:
            return FeedbackResponse(
                success=False,
                message="Failed to submit feedback",
                errors=["Conversation not found or unauthorized"]
            )

    except Exception as e:
        logger.error(f"Error submitting chatbot feedback: {str(e)}")
        return FeedbackResponse(
            success=False,
            message="Failed to submit feedback",
            errors=[str(e)]
        )


@router.get("/stats", response_model=ConversationStats)
async def get_conversation_stats(
    current_user_id: str = Query(..., description="Current user ID"),  # Admin check would be here
    db: AsyncSession = Depends(get_async_session)
):
    """Get chatbot conversation statistics (admin endpoint)"""
    try:
        # Note: In a real application, you would check if the user is an admin here
        chatbot_service = ChatbotService(db)
        
        stats = await chatbot_service.get_conversation_stats()
        return stats

    except Exception as e:
        logger.error(f"Error getting chatbot stats: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to retrieve statistics")


@router.get("/conversation/{conversation_id}", response_model=ChatbotConversationDTO)
async def get_conversation_by_id(
    conversation_id: str,
    current_user_id: str = Query(..., description="Current user ID"),
    db: AsyncSession = Depends(get_async_session)
):
    """Get a specific conversation by ID"""
    try:
        from sqlalchemy.future import select
        from app.models.database import ChatbotConversation
        
        # Check if conversation exists and belongs to user
        query = select(ChatbotConversation).where(
            ChatbotConversation.conversation_id == conversation_id,
            ChatbotConversation.user_id == current_user_id
        )
        result = await db.execute(query)
        conversation = result.scalar_one_or_none()

        if not conversation:
            raise HTTPException(status_code=404, detail="Conversation not found")

        return ChatbotConversationDTO(
            conversation_id=conversation.conversation_id,
            user_id=conversation.user_id,
            conversation_time=conversation.conversation_time,
            user_message=conversation.user_message,
            bot_response=conversation.bot_response,
            conversation_context=conversation.conversation_context,
            feedback_rating=conversation.feedback_rating,
            feedback_comment=conversation.feedback_comment
        )

    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error getting conversation by ID: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to retrieve conversation")


@router.delete("/conversation/{conversation_id}", response_model=FeedbackResponse)
async def delete_conversation(
    conversation_id: str,
    current_user_id: str = Query(..., description="Current user ID"),
    db: AsyncSession = Depends(get_async_session)
):
    """Delete a conversation (soft delete)"""
    try:
        from sqlalchemy.future import select
        from app.models.database import ChatbotConversation
        
        # Check if conversation exists and belongs to user
        query = select(ChatbotConversation).where(
            ChatbotConversation.conversation_id == conversation_id,
            ChatbotConversation.user_id == current_user_id
        )
        result = await db.execute(query)
        conversation = result.scalar_one_or_none()

        if not conversation:
            return FeedbackResponse(
                success=False,
                message="Conversation not found",
                errors=["Conversation not found or unauthorized"]
            )

        # In a real application, you might implement soft delete
        # For now, we'll just mark it as deleted by removing it
        await db.delete(conversation)
        await db.commit()

        return FeedbackResponse(
            success=True,
            message="Conversation deleted successfully"
        )

    except Exception as e:
        logger.error(f"Error deleting conversation: {str(e)}")
        return FeedbackResponse(
            success=False,
            message="Failed to delete conversation",
            errors=[str(e)]
        )


# Admin endpoints for analytics
@router.get("/admin/daily-usage", response_model=List[dict])
async def get_daily_usage(
    current_user_id: str = Query(..., description="Current user ID"),  # Admin check would be here
    days: int = Query(7, ge=1, le=90, description="Number of days to retrieve"),
    db: AsyncSession = Depends(get_async_session)
):
    """Get daily chatbot usage statistics (admin endpoint)"""
    try:
        from sqlalchemy import func, text
        from app.models.database import ChatbotConversation
        from datetime import datetime, timedelta

        # Note: In a real application, you would check if the user is an admin here
        
        # Get daily conversation counts
        start_date = datetime.utcnow() - timedelta(days=days)
        
        query = (
            select(
                func.date(ChatbotConversation.conversation_time).label('date'),
                func.count(ChatbotConversation.conversation_id).label('count')
            )
            .where(ChatbotConversation.conversation_time >= start_date)
            .group_by(func.date(ChatbotConversation.conversation_time))
            .order_by(func.date(ChatbotConversation.conversation_time))
        )

        result = await db.execute(query)
        daily_stats = result.all()

        return [
            {
                "date": str(stat.date),
                "conversation_count": stat.count
            }
            for stat in daily_stats
        ]

    except Exception as e:
        logger.error(f"Error getting daily usage: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to retrieve daily usage statistics")


@router.get("/admin/feedback-stats", response_model=List[dict])
async def get_feedback_stats(
    current_user_id: str = Query(..., description="Current user ID"),  # Admin check would be here
    db: AsyncSession = Depends(get_async_session)
):
    """Get feedback rating statistics (admin endpoint)"""
    try:
        from sqlalchemy import func
        from app.models.database import ChatbotConversation

        # Note: In a real application, you would check if the user is an admin here
        
        # Get feedback rating distribution
        query = (
            select(
                ChatbotConversation.feedback_rating.label('rating'),
                func.count(ChatbotConversation.conversation_id).label('count')
            )
            .where(ChatbotConversation.feedback_rating.isnot(None))
            .group_by(ChatbotConversation.feedback_rating)
            .order_by(ChatbotConversation.feedback_rating)
        )

        result = await db.execute(query)
        feedback_stats = result.all()

        total_feedback = sum(stat.count for stat in feedback_stats)

        return [
            {
                "rating": stat.rating,
                "count": stat.count,
                "percentage": round((stat.count / total_feedback) * 100, 2) if total_feedback > 0 else 0
            }
            for stat in feedback_stats
        ]

    except Exception as e:
        logger.error(f"Error getting feedback stats: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to retrieve feedback statistics") 