from fastapi import FastAPI, HTTPException, Depends
from fastapi.middleware.cors import CORSMiddleware
from sqlalchemy import create_engine, text
from sqlalchemy.orm import sessionmaker, Session
from typing import List, Optional
import os
from pydantic import BaseModel
import re
from datetime import datetime
from difflib import SequenceMatcher
import json
from config import settings
from gemini_service import gemini_service

# Pydantic models
class CourseRequest(BaseModel):
    description: str

class CourseResponse(BaseModel):
    course_id: str
    course_name: str
    course_description: Optional[str]
    author_name: Optional[str]
    categories: List[str]
    chapters: List[str]
    created_at: datetime
    similarity_score: float
    match_reasons: List[str]
    gemini_relevance_score: Optional[float] = None
    gemini_match_points: Optional[List[str]] = None

class UserAnalysisResponse(BaseModel):
    main_subjects: List[str]
    skill_level: str
    learning_goals: List[str]
    keywords: List[str]
    course_type_preference: str
    technologies: List[str]
    career_focus: str
    urgency: str

class EnhancedSuggestionResponse(BaseModel):
    user_analysis: UserAnalysisResponse
    suggestions: List[CourseResponse]
    enhanced_keywords: List[str]
    smart_query: str

# FastAPI app
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

# Database configuration
engine = create_engine(settings.DATABASE_URL, echo=False)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()

# AI Analysis functions
def extract_keywords(text: str) -> List[str]:
    """Extract meaningful keywords from description"""
    # Convert to lowercase and remove punctuation
    text = re.sub(r'[^\w\s]', ' ', text.lower())
    
    # Common stop words in Vietnamese and English
    stop_words = {
        'và', 'các', 'của', 'trong', 'với', 'là', 'có', 'được', 'cho', 'về', 'từ', 'một', 'tôi', 'bạn',
        'and', 'the', 'in', 'to', 'of', 'a', 'for', 'is', 'on', 'with', 'as', 'by', 'an', 'are', 'this', 'that'
    }
    
    words = text.split()
    keywords = [word for word in words if len(word) > 2 and word not in stop_words]
    
    return keywords

def calculate_similarity(desc1: str, desc2: str) -> float:
    """Calculate similarity between two descriptions"""
    if not desc1 or not desc2:
        return 0.0
    
    return SequenceMatcher(None, desc1.lower(), desc2.lower()).ratio()

def find_matching_courses(description: str, db: Session) -> List[CourseResponse]:
    """Find courses matching the description using AI analysis"""
    
    # Step 1: Analyze user intent with Gemini
    user_analysis = gemini_service.analyze_user_intent(description)
    
    # Step 2: Extract and enhance keywords
    basic_keywords = extract_keywords(description)
    enhanced_keywords = gemini_service.enhance_keywords(description, basic_keywords)
    user_keywords = enhanced_keywords
    
    # Step 3: Generate smart query for better matching
    smart_query = gemini_service.generate_smart_query(description, user_analysis)
    
    # SQL query to get courses with related data
    query = text("""
        SELECT DISTINCT
            c.course_id as CourseId,
            c.course_name as CourseName,
            c.course_description as CourseDescription,
            c.course_created_at as CourseCreatedAt,
            a.full_name as AuthorName,
            a.username as AuthorUsername,
            STRING_AGG(DISTINCT cat.course_category_name, ', ') as Categories,
            STRING_AGG(DISTINCT ch.chapter_name, '|||') as Chapters
        FROM course c
        INNER JOIN account a ON c.author_id = a.user_id
        LEFT JOIN CourseCategoryMapping ccm ON c.course_id = ccm.CourseId
        LEFT JOIN course_category cat ON ccm.CourseCategoryId = cat.course_category_id
        LEFT JOIN chapter ch ON c.course_id = ch.course_id
        WHERE c.course_status = 1 AND c.approval_status = 'approved'
        GROUP BY c.course_id, c.course_name, c.course_description, c.course_created_at, a.full_name, a.username
        ORDER BY c.course_created_at DESC
    """)
    
    try:
        result = db.execute(query)
        courses = result.fetchall()
    except Exception as e:
        # Fallback query if there's an issue with the main query
        fallback_query = text("""
            SELECT 
                c.course_id as CourseId,
                c.course_name as CourseName,
                c.course_description as CourseDescription,
                c.course_created_at as CourseCreatedAt,
                a.full_name as AuthorName,
                a.username as AuthorUsername
            FROM course c
            INNER JOIN account a ON c.author_id = a.user_id
            WHERE c.course_status = 1
            ORDER BY c.course_created_at DESC
        """)
        result = db.execute(fallback_query)
        courses = result.fetchall()
    
    suggestions = []
    
    for course in courses:
        # Calculate similarity scores using both original description and smart query
        name_similarity = max(
            calculate_similarity(description, course.CourseName or ""),
            calculate_similarity(smart_query, course.CourseName or "")
        )
        desc_similarity = max(
            calculate_similarity(description, course.CourseDescription or ""),
            calculate_similarity(smart_query, course.CourseDescription or "")
        )
        
        # Enhanced keyword matching
        course_text = f"{course.CourseName or ''} {course.CourseDescription or ''}"
        course_keywords = extract_keywords(course_text)
        
        keyword_matches = len(set(user_keywords) & set(course_keywords))
        keyword_score = keyword_matches / len(user_keywords) if user_keywords else 0
        
        # Category matching
        categories = course.Categories.split(', ') if hasattr(course, 'Categories') and course.Categories else []
        category_match_score = 0
        if categories:
            for keyword in user_keywords:
                for category in categories:
                    if keyword in category.lower():
                        category_match_score += 0.2
        
        # Chapter matching
        chapters = course.Chapters.split('|||') if hasattr(course, 'Chapters') and course.Chapters else []
        chapter_match_score = 0
        if chapters:
            for keyword in user_keywords:
                for chapter in chapters:
                    if keyword in chapter.lower():
                        chapter_match_score += 0.1
        
        # Calculate overall similarity score
        overall_score = (
            name_similarity * 0.3 +
            desc_similarity * 0.4 +
            keyword_score * 0.2 +
            category_match_score * 0.05 +
            chapter_match_score * 0.05
        )
        
        # Determine match reasons
        match_reasons = []
        if name_similarity > 0.3:
            match_reasons.append("Tên khóa học tương tự")
        if desc_similarity > 0.3:
            match_reasons.append("Mô tả khóa học tương tự")
        if keyword_score > 0.2:
            match_reasons.append("Từ khóa phù hợp")
        if category_match_score > 0:
            match_reasons.append("Danh mục liên quan")
        if chapter_match_score > 0:
            match_reasons.append("Nội dung chương liên quan")
        
        # Only include courses with reasonable similarity
        if overall_score > 0.1:
            # Get Gemini evaluation for this course
            course_info = {
                "name": course.CourseName,
                "description": course.CourseDescription,
                "categories": categories,
                "chapters": [ch.strip() for ch in chapters if ch.strip()]
            }
            
            gemini_eval = gemini_service.evaluate_course_relevance(course_info, user_analysis)
            
            # Combine traditional score with Gemini score
            gemini_relevance = gemini_eval.get("relevance_score", 0.5)
            final_score = (overall_score * 0.7) + (gemini_relevance * 0.3)
            
            # Combine match reasons
            enhanced_match_reasons = match_reasons + gemini_eval.get("reasons", [])
            
            suggestions.append(CourseResponse(
                course_id=course.CourseId,
                course_name=course.CourseName,
                course_description=course.CourseDescription,
                author_name=course.AuthorName or course.AuthorUsername,
                categories=categories,
                chapters=[ch.strip() for ch in chapters if ch.strip()],
                created_at=course.CourseCreatedAt,
                similarity_score=round(final_score, 3),
                match_reasons=enhanced_match_reasons,
                gemini_relevance_score=round(gemini_relevance, 3),
                gemini_match_points=gemini_eval.get("match_points", [])
            ))
    
    # Sort by similarity score and return top 10
    suggestions.sort(key=lambda x: x.similarity_score, reverse=True)
    return suggestions[:10]

@app.get("/")
async def root():
    return {"message": "Course Suggestion AI Service is running!"}

@app.post("/suggest-courses", response_model=List[CourseResponse])
async def suggest_courses(request: CourseRequest, db: Session = Depends(get_db)):
    """
    Suggest courses based on user description (Legacy endpoint for backward compatibility)
    """
    if not request.description or len(request.description.strip()) < 3:
        raise HTTPException(status_code=400, detail="Description must be at least 3 characters long")
    
    try:
        suggestions = find_matching_courses(request.description, db)
        return suggestions
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Error processing request: {str(e)}")

@app.post("/suggest-courses-enhanced", response_model=EnhancedSuggestionResponse)
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

@app.get("/health")
async def health_check():
    """Health check endpoint"""
    return {
        "status": "healthy", 
        "timestamp": datetime.now(),
        "gemini_enabled": gemini_service.enabled,
        "gemini_model": settings.GEMINI_MODEL if gemini_service.enabled else None
    }

@app.get("/gemini-status")
async def gemini_status():
    """Check Gemini AI service status"""
    return {
        "enabled": gemini_service.enabled,
        "model": settings.GEMINI_MODEL if gemini_service.enabled else None,
        "api_key_configured": bool(settings.GEMINI_API_KEY),
        "use_gemini_setting": settings.USE_GEMINI
    }

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host=settings.HOST, port=settings.PORT) 