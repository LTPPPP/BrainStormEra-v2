from datetime import datetime
from typing import List, Optional
from fastapi import APIRouter, Depends, HTTPException, WebSocket, WebSocketDisconnect, Query
from sqlalchemy.ext.asyncio import AsyncSession

from app.core.database import get_async_session
from app.services.chat_service import ChatService
from app.models.chat_models import (
    SendMessageRequest,
    MarkAsReadRequest,
    DeleteMessageRequest,
    EditMessageRequest,
    GetMessagesRequest,
    GetUnreadCountRequest,
    ChatMessageDTO,
    ChatUserDTO,
    ConversationViewModel,
    ChatIndexViewModel,
    ChatResponse
)
from app.websockets.chat_websocket import ChatWebSocketHandler, manager
import logging

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/chat", tags=["chat"])


# WebSocket endpoint for real-time chat
@router.websocket("/ws/{user_id}")
async def websocket_endpoint(websocket: WebSocket, user_id: str):
    """WebSocket endpoint for real-time chat communication"""
    async for db_session in get_async_session():
        handler = ChatWebSocketHandler(websocket, user_id, db_session)
        await handler.handle_connection()
        break


# REST API endpoints
@router.post("/send", response_model=ChatResponse)
async def send_message(
    request: SendMessageRequest,
    current_user_id: str = Query(..., description="Current user ID"),  # In real app, this would come from auth
    db: AsyncSession = Depends(get_async_session)
):
    """Send a message to another user"""
    try:
        chat_service = ChatService(db)
        
        message_entity = await chat_service.send_message(
            sender_id=current_user_id,
            receiver_id=request.receiver_id,
            message=request.message,
            reply_to_message_id=request.reply_to_message_id
        )

        if message_entity:
            # Notify receiver via WebSocket if online
            message_data = {
                "message_id": message_entity.message_id,
                "sender_id": message_entity.sender_id,
                "receiver_id": message_entity.receiver_id,
                "content": message_entity.message_content,
                "message_type": message_entity.message_type,
                "reply_to_message_id": message_entity.reply_to_message_id,
                "created_at": message_entity.message_created_at.isoformat(),
                "sender_name": message_entity.sender.username if message_entity.sender else "Unknown",
                "sender_avatar": message_entity.sender.user_image if message_entity.sender else None
            }
            
            await manager.send_personal_message({
                "type": "receive_message",
                "data": message_data
            }, request.receiver_id)

            return ChatResponse(
                success=True,
                message="Message sent successfully",
                data={"message_id": message_entity.message_id}
            )
        else:
            return ChatResponse(
                success=False,
                message="Failed to send message",
                errors=["Could not save message to database"]
            )

    except Exception as e:
        logger.error(f"Error sending message: {str(e)}")
        return ChatResponse(
            success=False,
            message="Internal server error",
            errors=[str(e)]
        )


@router.get("/messages/{receiver_id}", response_model=List[ChatMessageDTO])
async def get_messages(
    receiver_id: str,
    current_user_id: str = Query(..., description="Current user ID"),
    page: int = Query(1, ge=1),
    page_size: int = Query(50, ge=1, le=100),
    db: AsyncSession = Depends(get_async_session)
):
    """Get conversation messages between current user and another user"""
    try:
        chat_service = ChatService(db)
        
        messages = await chat_service.get_conversation_messages(
            sender_id=current_user_id,
            receiver_id=receiver_id,
            page=page,
            page_size=page_size
        )

        return [
            ChatMessageDTO(
                message_id=msg.message_id,
                sender_id=msg.sender_id,
                receiver_id=msg.receiver_id,
                content=msg.message_content,
                message_type=msg.message_type,
                is_read=msg.is_read,
                reply_to_message_id=msg.reply_to_message_id,
                is_edited=msg.is_edited,
                created_at=msg.message_created_at,
                sender_name=msg.sender.username if msg.sender else "Unknown",
                sender_avatar=msg.sender.user_image if msg.sender else None
            ) for msg in messages
        ]

    except Exception as e:
        logger.error(f"Error getting messages: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to retrieve messages")


@router.post("/mark-read", response_model=ChatResponse)
async def mark_message_as_read(
    request: MarkAsReadRequest,
    current_user_id: str = Query(..., description="Current user ID"),
    db: AsyncSession = Depends(get_async_session)
):
    """Mark a message as read"""
    try:
        chat_service = ChatService(db)
        
        success = await chat_service.mark_message_as_read(
            message_id=request.message_id,
            user_id=current_user_id
        )

        if success:
            # Notify sender via WebSocket
            message = await chat_service.get_message_by_id(request.message_id)
            if message:
                await manager.send_personal_message({
                    "type": "message_read",
                    "data": {
                        "message_id": request.message_id,
                        "reader_id": current_user_id,
                        "read_at": datetime.utcnow().isoformat()
                    }
                }, message.sender_id)

            return ChatResponse(
                success=True,
                message="Message marked as read"
            )
        else:
            return ChatResponse(
                success=False,
                message="Failed to mark message as read",
                errors=["Message not found or unauthorized"]
            )

    except Exception as e:
        logger.error(f"Error marking message as read: {str(e)}")
        return ChatResponse(
            success=False,
            message="Internal server error",
            errors=[str(e)]
        )


@router.delete("/message/{message_id}", response_model=ChatResponse)
async def delete_message(
    message_id: str,
    current_user_id: str = Query(..., description="Current user ID"),
    db: AsyncSession = Depends(get_async_session)
):
    """Soft delete a message"""
    try:
        chat_service = ChatService(db)
        
        success = await chat_service.delete_message(
            message_id=message_id,
            user_id=current_user_id
        )

        if success:
            return ChatResponse(
                success=True,
                message="Message deleted successfully"
            )
        else:
            return ChatResponse(
                success=False,
                message="Failed to delete message",
                errors=["Message not found or unauthorized"]
            )

    except Exception as e:
        logger.error(f"Error deleting message: {str(e)}")
        return ChatResponse(
            success=False,
            message="Internal server error",
            errors=[str(e)]
        )


@router.put("/message/{message_id}", response_model=ChatResponse)
async def edit_message(
    message_id: str,
    request: EditMessageRequest,
    current_user_id: str = Query(..., description="Current user ID"),
    db: AsyncSession = Depends(get_async_session)
):
    """Edit a message"""
    try:
        chat_service = ChatService(db)
        
        success = await chat_service.edit_message(
            message_id=message_id,
            new_content=request.new_content,
            user_id=current_user_id
        )

        if success:
            return ChatResponse(
                success=True,
                message="Message edited successfully"
            )
        else:
            return ChatResponse(
                success=False,
                message="Failed to edit message",
                errors=["Message not found or unauthorized"]
            )

    except Exception as e:
        logger.error(f"Error editing message: {str(e)}")
        return ChatResponse(
            success=False,
            message="Internal server error",
            errors=[str(e)]
        )


@router.get("/conversations", response_model=List[ChatUserDTO])
async def get_user_conversations(
    current_user_id: str = Query(..., description="Current user ID"),
    db: AsyncSession = Depends(get_async_session)
):
    """Get all conversations for the current user"""
    try:
        chat_service = ChatService(db)
        
        conversations = await chat_service.get_user_conversations(current_user_id)
        chat_users = []

        for conversation in conversations:
            # Determine the other participant
            other_user_id = (
                conversation.participant1_id 
                if conversation.participant2_id == current_user_id 
                else conversation.participant2_id
            )
            other_user = (
                conversation.participant1 
                if conversation.participant2_id == current_user_id 
                else conversation.participant2
            )

            if other_user:
                # Get last message and unread count
                last_message = await chat_service.get_last_message_between_users(
                    current_user_id, other_user_id
                )
                unread_count = await chat_service.get_unread_message_count(
                    current_user_id, other_user_id
                )

                chat_user = ChatUserDTO(
                    user_id=other_user.user_id,
                    username=other_user.username,
                    user_image=other_user.user_image,
                    last_active=other_user.last_active,
                    is_online=manager.is_user_online(other_user.user_id),
                    last_message=last_message.message_content if last_message else None,
                    last_message_time=last_message.message_created_at if last_message else None,
                    unread_count=unread_count
                )
                chat_users.append(chat_user)

        return chat_users

    except Exception as e:
        logger.error(f"Error getting user conversations: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to retrieve conversations")


@router.get("/unread-count/{sender_id}")
async def get_unread_count(
    sender_id: str,
    current_user_id: str = Query(..., description="Current user ID"),
    db: AsyncSession = Depends(get_async_session)
):
    """Get unread message count from a specific sender"""
    try:
        chat_service = ChatService(db)
        
        count = await chat_service.get_unread_message_count(
            user_id=current_user_id,
            sender_id=sender_id
        )

        return {"unread_count": count}

    except Exception as e:
        logger.error(f"Error getting unread count: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to get unread count")


@router.get("/unread-messages", response_model=List[ChatMessageDTO])
async def get_unread_messages(
    current_user_id: str = Query(..., description="Current user ID"),
    db: AsyncSession = Depends(get_async_session)
):
    """Get all unread messages for the current user"""
    try:
        chat_service = ChatService(db)
        
        messages = await chat_service.get_unread_messages(current_user_id)

        return [
            ChatMessageDTO(
                message_id=msg.message_id,
                sender_id=msg.sender_id,
                receiver_id=msg.receiver_id,
                content=msg.message_content,
                message_type=msg.message_type,
                is_read=msg.is_read,
                reply_to_message_id=msg.reply_to_message_id,
                is_edited=msg.is_edited,
                created_at=msg.message_created_at,
                sender_name=msg.sender.username if msg.sender else "Unknown",
                sender_avatar=msg.sender.user_image if msg.sender else None
            ) for msg in messages
        ]

    except Exception as e:
        logger.error(f"Error getting unread messages: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to retrieve unread messages")


@router.get("/online-users")
async def get_online_users():
    """Get list of currently online users"""
    try:
        from app.websockets.chat_websocket import get_online_users
        online_user_ids = get_online_users()
        
        return {"online_users": online_user_ids, "count": len(online_user_ids)}

    except Exception as e:
        logger.error(f"Error getting online users: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to get online users") 