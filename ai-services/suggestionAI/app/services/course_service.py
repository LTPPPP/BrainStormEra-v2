"""
Course service for handling course matching and suggestion logic
"""

from typing import List
from sqlalchemy.orm import Session
from sqlalchemy import text

from ..models.response import CourseResponse
from .analysis_service import extract_keywords, calculate_similarity
from .gemini_service import gemini_service


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