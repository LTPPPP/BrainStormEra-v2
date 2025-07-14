"""
API routes for Course Suggestion AI Service
"""

from fastapi import APIRouter, HTTPException, Depends
from sqlalchemy.orm import Session
from typing import List
from datetime import datetime

from ..models import CourseRequest, CourseResponse, EnhancedSuggestionResponse, UserAnalysisResponse
from ..core.database import get_db
from ..core.config import settings
from ..services import find_matching_courses, extract_keywords, gemini_service

router = APIRouter()


@router.get("/")
async def root():
    return {"message": "Course Suggestion AI Service is running!"}

@router.post("/suggest-courses", response_model=EnhancedSuggestionResponse)
async def suggest_courses_enhanced(request: CourseRequest, db: Session = Depends(get_db)):
    """
    Enhanced course suggestions with Gemini AI analysis and detailed insights
    """
    if not request.description or len(request.description.strip()) < 3:
        raise HTTPException(status_code=400, detail="Description must be at least 3 characters long")
    
    try:
        # Get user analysis
        user_analysis = gemini_service.analyze_user_intent(request.description)
        
        # Get enhanced keywords
        basic_keywords = extract_keywords(request.description)
        enhanced_keywords = gemini_service.enhance_keywords(request.description, basic_keywords)
        
        # Generate smart query
        smart_query = gemini_service.generate_smart_query(request.description, user_analysis)
        
        # Get course suggestions
        suggestions = find_matching_courses(request.description, db)
        
        # Return enhanced response
        return EnhancedSuggestionResponse(
            user_analysis=UserAnalysisResponse(**user_analysis),
            suggestions=suggestions,
            enhanced_keywords=enhanced_keywords,
            smart_query=smart_query
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error processing request: {str(e)}")


@router.get("/health")
async def health_check():
    """Health check endpoint"""
    return {
        "status": "healthy", 
        "timestamp": datetime.now(),
        "gemini_enabled": gemini_service.enabled,
        "gemini_model": settings.GEMINI_MODEL if gemini_service.enabled else None
    }


@router.get("/gemini-status")
async def gemini_status():
    """Check Gemini AI service status"""
    return {
        "enabled": gemini_service.enabled,
        "model": settings.GEMINI_MODEL if gemini_service.enabled else None,
        "api_key_configured": bool(settings.GEMINI_API_KEY),
        "use_gemini_setting": settings.USE_GEMINI
    } 