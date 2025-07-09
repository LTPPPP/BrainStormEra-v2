import logging
import uvicorn
from contextlib import asynccontextmanager
from fastapi import FastAPI, Depends
from fastapi.middleware.cors import CORSMiddleware
from fastapi.middleware.trustedhost import TrustedHostMiddleware
from fastapi.responses import JSONResponse

from app.core.config import settings
from app.core.database import create_tables
from app.api.chat_routes import router as chat_router
from app.api.chatbot_routes import router as chatbot_router

# Configure logging
logging.basicConfig(
    level=getattr(logging, settings.log_level.upper()),
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
    handlers=[
        logging.StreamHandler(),
        logging.FileHandler("chat_service.log") if not settings.debug else logging.StreamHandler()
    ]
)

logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Application lifespan manager"""
    # Startup
    logger.info("Starting BrainStormEra Chat Service...")
    
    try:
        # Create database tables
        await create_tables()
        logger.info("Database tables created successfully")
    except Exception as e:
        logger.error(f"Failed to create database tables: {str(e)}")
        raise
    
    yield
    
    # Shutdown
    logger.info("Shutting down BrainStormEra Chat Service...")


# Create FastAPI application
app = FastAPI(
    title=settings.project_name,
    description="AI-powered chat service for BrainStormEra educational platform",
    version="1.0.0",
    docs_url="/docs" if settings.debug else None,
    redoc_url="/redoc" if settings.debug else None,
    lifespan=lifespan
)

# Add CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.cors_origins,
    allow_credentials=True,
    allow_methods=["GET", "POST", "PUT", "DELETE", "OPTIONS"],
    allow_headers=["*"],
)

# Add trusted host middleware for production
if not settings.debug:
    app.add_middleware(
        TrustedHostMiddleware,
        allowed_hosts=["*.brainstormera.com", "localhost", "127.0.0.1"]
    )


# Exception handlers
@app.exception_handler(404)
async def not_found_handler(request, exc):
    return JSONResponse(
        status_code=404,
        content={"detail": "Endpoint not found"}
    )


@app.exception_handler(500)
async def internal_error_handler(request, exc):
    logger.error(f"Internal server error: {str(exc)}")
    return JSONResponse(
        status_code=500,
        content={"detail": "Internal server error"}
    )


# Health check endpoint
@app.get("/health")
async def health_check():
    """Health check endpoint"""
    return {
        "status": "healthy",
        "service": "BrainStormEra Chat Service",
        "version": "1.0.0"
    }


@app.get("/")
async def root():
    """Root endpoint"""
    return {
        "message": "Welcome to BrainStormEra Chat Service",
        "docs": "/docs" if settings.debug else "Documentation not available in production",
        "health": "/health"
    }


# Include API routers
app.include_router(chat_router, prefix=settings.api_v1_prefix)
app.include_router(chatbot_router, prefix=settings.api_v1_prefix)


# Add middleware for request logging
@app.middleware("http")
async def log_requests(request, call_next):
    """Log all HTTP requests"""
    start_time = time.time()
    
    # Log request
    logger.info(f"Request: {request.method} {request.url}")
    
    # Process request
    response = await call_next(request)
    
    # Log response
    process_time = time.time() - start_time
    logger.info(f"Response: {response.status_code} - {process_time:.4f}s")
    
    return response


# WebSocket status endpoint
@app.get("/ws/status")
async def websocket_status():
    """Get WebSocket connection status"""
    from app.websockets.chat_websocket import get_online_users, manager
    
    online_users = get_online_users()
    total_connections = sum(
        len(connections) for connections in manager.active_connections.values()
    )
    
    return {
        "online_users_count": len(online_users),
        "total_connections": total_connections,
        "online_users": online_users
    }


if __name__ == "__main__":
    import time
    
    # Run the application
    uvicorn.run(
        "main:app",
        host="127.0.0.1",
        port=8001,
        reload=settings.debug,
        log_level=settings.log_level.lower(),
        access_log=True,
        loop="asyncio"
    ) 