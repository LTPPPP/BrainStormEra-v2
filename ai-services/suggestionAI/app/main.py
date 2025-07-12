"""
Course Suggestion AI Service - Main Application
"""

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from .api import router
from .core.config import settings


def create_app() -> FastAPI:
    """Create and configure the FastAPI application"""
    
    app = FastAPI(
        title="Course Suggestion AI Service",
        description="AI service to suggest similar courses based on user description",
        version="1.0.0"
    )

    # CORS middleware
    app.add_middleware(
        CORSMiddleware,
        allow_origins=["*"],
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"],
    )

    # Include routers
    app.include_router(router)

    return app


app = create_app() 