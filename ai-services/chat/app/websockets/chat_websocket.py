import json
import logging
from datetime import datetime
from typing import Dict, Set, Optional
from fastapi import WebSocket, WebSocketDisconnect
from sqlalchemy.ext.asyncio import AsyncSession

from app.services.chat_service import ChatService
from app.models.chat_models import WebSocketMessage, TypingIndicator
from app.core.database import get_async_session

logger = logging.getLogger(__name__)


class ConnectionManager:
    """Manages WebSocket connections for real-time chat"""
    
    def __init__(self):
        # user_id -> set of websocket connections
        self.active_connections: Dict[str, Set[WebSocket]] = {}
        # websocket -> user_id mapping
        self.connection_user_map: Dict[WebSocket, str] = {}

    async def connect(self, websocket: WebSocket, user_id: str):
        """Accept and register a new connection"""
        await websocket.accept()
        
        if user_id not in self.active_connections:
            self.active_connections[user_id] = set()
        
        self.active_connections[user_id].add(websocket)
        self.connection_user_map[websocket] = user_id
        
        # Set user online status
        async for db_session in get_async_session():
            chat_service = ChatService(db_session)
            await chat_service.set_user_online_status(user_id, True)
            break

        logger.info(f"User {user_id} connected. Total connections: {len(self.connection_user_map)}")

    async def disconnect(self, websocket: WebSocket):
        """Remove connection and update user status"""
        user_id = self.connection_user_map.get(websocket)
        
        if user_id:
            # Remove websocket from user's connections
            if user_id in self.active_connections:
                self.active_connections[user_id].discard(websocket)
                
                # If no more connections for this user, set offline
                if not self.active_connections[user_id]:
                    del self.active_connections[user_id]
                    
                    # Set user offline status
                    async for db_session in get_async_session():
                        chat_service = ChatService(db_session)
                        await chat_service.set_user_online_status(user_id, False)
                        break

            # Remove from connection map
            del self.connection_user_map[websocket]
            
            logger.info(f"User {user_id} disconnected. Total connections: {len(self.connection_user_map)}")

    async def send_personal_message(self, message: dict, user_id: str):
        """Send message to all connections of a specific user"""
        if user_id in self.active_connections:
            disconnected_websockets = []
            
            for websocket in self.active_connections[user_id].copy():
                try:
                    await websocket.send_text(json.dumps(message))
                except Exception as e:
                    logger.error(f"Error sending message to {user_id}: {str(e)}")
                    disconnected_websockets.append(websocket)
            
            # Clean up disconnected websockets
            for websocket in disconnected_websockets:
                await self.disconnect(websocket)

    async def send_to_connection(self, message: dict, websocket: WebSocket):
        """Send message to a specific websocket connection"""
        try:
            await websocket.send_text(json.dumps(message))
        except Exception as e:
            logger.error(f"Error sending message to websocket: {str(e)}")
            await self.disconnect(websocket)

    def get_user_connections(self, user_id: str) -> Set[WebSocket]:
        """Get all active connections for a user"""
        return self.active_connections.get(user_id, set())

    def is_user_online(self, user_id: str) -> bool:
        """Check if user has any active connections"""
        return user_id in self.active_connections and len(self.active_connections[user_id]) > 0


# Global connection manager instance
manager = ConnectionManager()


class ChatWebSocketHandler:
    """Handles WebSocket events for chat functionality"""
    
    def __init__(self, websocket: WebSocket, user_id: str, db_session: AsyncSession):
        self.websocket = websocket
        self.user_id = user_id
        self.db = db_session
        self.chat_service = ChatService(db_session)

    async def handle_connection(self):
        """Handle WebSocket connection lifecycle"""
        await manager.connect(self.websocket, self.user_id)
        
        try:
            while True:
                # Wait for messages from client
                data = await self.websocket.receive_text()
                message_data = json.loads(data)
                
                # Handle different message types
                await self.handle_message(message_data)
                
        except WebSocketDisconnect:
            logger.info(f"WebSocket disconnected for user {self.user_id}")
        except Exception as e:
            logger.error(f"WebSocket error for user {self.user_id}: {str(e)}")
        finally:
            await manager.disconnect(self.websocket)

    async def handle_message(self, message_data: dict):
        """Handle incoming WebSocket messages"""
        message_type = message_data.get("type")
        data = message_data.get("data", {})

        try:
            if message_type == "send_message":
                await self.handle_send_message(data)
            elif message_type == "mark_as_read":
                await self.handle_mark_as_read(data)
            elif message_type == "start_typing":
                await self.handle_typing_indicator(data, True)
            elif message_type == "stop_typing":
                await self.handle_typing_indicator(data, False)
            elif message_type == "ping":
                await self.handle_ping()
            else:
                logger.warning(f"Unknown message type: {message_type}")
                
        except Exception as e:
            logger.error(f"Error handling message type {message_type}: {str(e)}")
            await self.send_error("Error processing message")

    async def handle_send_message(self, data: dict):
        """Handle sending a new message"""
        receiver_id = data.get("receiver_id")
        message_content = data.get("message")
        reply_to_message_id = data.get("reply_to_message_id")

        if not receiver_id or not message_content:
            await self.send_error("Missing receiver_id or message content")
            return

        # Send message through chat service
        message_entity = await self.chat_service.send_message(
            sender_id=self.user_id,
            receiver_id=receiver_id,
            message=message_content,
            reply_to_message_id=reply_to_message_id
        )

        if message_entity:
            # Prepare message data
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

            # Send to receiver
            await manager.send_personal_message({
                "type": "receive_message",
                "data": message_data
            }, receiver_id)

            # Send confirmation to sender
            await manager.send_to_connection({
                "type": "message_sent",
                "data": message_data
            }, self.websocket)

        else:
            await self.send_error("Failed to send message")

    async def handle_mark_as_read(self, data: dict):
        """Handle marking message as read"""
        message_id = data.get("message_id")

        if not message_id:
            await self.send_error("Missing message_id")
            return

        success = await self.chat_service.mark_message_as_read(message_id, self.user_id)
        
        if success:
            # Get message details to notify sender
            message = await self.chat_service.get_message_by_id(message_id)
            if message:
                await manager.send_personal_message({
                    "type": "message_read",
                    "data": {
                        "message_id": message_id,
                        "reader_id": self.user_id,
                        "read_at": datetime.utcnow().isoformat()
                    }
                }, message.sender_id)

    async def handle_typing_indicator(self, data: dict, is_typing: bool):
        """Handle typing indicators"""
        receiver_id = data.get("receiver_id")

        if not receiver_id:
            return

        event_type = "user_started_typing" if is_typing else "user_stopped_typing"
        
        await manager.send_personal_message({
            "type": event_type,
            "data": {
                "sender_id": self.user_id,
                "receiver_id": receiver_id,
                "is_typing": is_typing
            }
        }, receiver_id)

    async def handle_ping(self):
        """Handle ping messages for connection keepalive"""
        await manager.send_to_connection({
            "type": "pong",
            "data": {"timestamp": datetime.utcnow().isoformat()}
        }, self.websocket)

    async def send_error(self, error_message: str):
        """Send error message to client"""
        await manager.send_to_connection({
            "type": "error",
            "data": {"message": error_message}
        }, self.websocket)


# Utility functions for external use
async def notify_user_message(user_id: str, message_data: dict):
    """Send message notification to user (for external services)"""
    await manager.send_personal_message({
        "type": "notification",
        "data": message_data
    }, user_id)


async def broadcast_user_status(user_id: str, is_online: bool):
    """Broadcast user online status to relevant users"""
    # This could be enhanced to only notify users who have chatted with this user
    status_message = {
        "type": "user_status_changed",
        "data": {
            "user_id": user_id,
            "is_online": is_online,
            "timestamp": datetime.utcnow().isoformat()
        }
    }
    
    # For now, we don't broadcast to everyone - this would need optimization
    # to only send to relevant users based on chat history
    pass


def get_online_users() -> list:
    """Get list of currently online user IDs"""
    return list(manager.active_connections.keys())


def get_user_connection_count(user_id: str) -> int:
    """Get number of active connections for a user"""
    return len(manager.get_user_connections(user_id)) 