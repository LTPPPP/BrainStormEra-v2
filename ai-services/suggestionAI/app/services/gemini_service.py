import google.generativeai as genai
import json
import re
from typing import List, Dict, Optional
from ..core.config import settings
import logging

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class GeminiService:
    """Service for handling Gemini AI natural language processing"""
    
    def __init__(self):
        if settings.GEMINI_API_KEY:
            genai.configure(api_key=settings.GEMINI_API_KEY)
            self.model = genai.GenerativeModel(settings.GEMINI_MODEL)
            self.enabled = settings.USE_GEMINI
        else:
            self.model = None
            self.enabled = False
            logger.warning("Gemini API key not provided. Gemini features disabled.")
    
    def _clean_json_response(self, response_text: str) -> str:
        """Clean Gemini response to extract pure JSON"""
        # Remove markdown code blocks if present
        response_text = re.sub(r'```json\s*', '', response_text)
        response_text = re.sub(r'```\s*$', '', response_text)
        response_text = re.sub(r'```.*$', '', response_text, flags=re.MULTILINE)
        
        # Find JSON object in response
        json_match = re.search(r'\{.*\}', response_text, re.DOTALL)
        if json_match:
            return json_match.group(0)
        
        # Find JSON array in response
        array_match = re.search(r'\[.*\]', response_text, re.DOTALL)
        if array_match:
            return array_match.group(0)
        
        return response_text.strip()
    
    def analyze_user_intent(self, description: str) -> Dict:
        """
        Analyze user description to extract learning intent and preferences
        """
        if not self.enabled or not self.model:
            return self._fallback_analysis(description)
        
        try:
            prompt = f"""
            Phân tích mô tả sau của người dùng về nhu cầu học tập và trích xuất thông tin có cấu trúc:

            Mô tả: "{description}"

            Hãy trả về JSON với cấu trúc sau:
            {{
                "main_subjects": ["chủ đề chính 1", "chủ đề chính 2"],
                "skill_level": "beginner",
                "learning_goals": ["mục tiêu 1", "mục tiêu 2"],
                "keywords": ["từ khóa 1", "từ khóa 2", "từ khóa 3"],
                "course_type_preference": "practical",
                "technologies": ["công nghệ 1", "công nghệ 2"],
                "career_focus": "development",
                "urgency": "medium"
            }}

            Chỉ trả về JSON thuần túy, không có text hay markdown khác.
            """
            
            response = self.model.generate_content(prompt)
            cleaned_response = self._clean_json_response(response.text)
            
            # Parse JSON response
            try:
                analysis = json.loads(cleaned_response)
                logger.info("Successfully parsed Gemini analysis response")
                return analysis
            except json.JSONDecodeError as e:
                logger.error(f"Failed to parse Gemini JSON response: {e}")
                logger.error(f"Response text: {response.text[:200]}...")
                return self._fallback_analysis(description)
                
        except Exception as e:
            logger.error(f"Gemini analysis failed: {e}")
            return self._fallback_analysis(description)
    
    def enhance_keywords(self, description: str, basic_keywords: List[str]) -> List[str]:
        """
        Use Gemini to enhance and expand keywords from basic extraction
        """
        if not self.enabled or not self.model:
            return basic_keywords
        
        try:
            prompt = f"""
            Dựa vào mô tả học tập và từ khóa cơ bản, hãy mở rộng và cải thiện danh sách từ khóa:

            Mô tả: "{description}"
            Từ khóa cơ bản: {basic_keywords}

            Hãy trả về danh sách từ khóa JSON mở rộng bao gồm:
            - Từ khóa gốc
            - Từ đồng nghĩa
            - Thuật ngữ kỹ thuật liên quan
            - Tên công cụ/framework
            - Kỹ năng liên quan

            Format: ["keyword1", "keyword2", "keyword3"]
            
            Chỉ trả về JSON array, tối đa 15 từ khóa.
            """
            
            response = self.model.generate_content(prompt)
            cleaned_response = self._clean_json_response(response.text)
            
            try:
                enhanced_keywords = json.loads(cleaned_response)
                if isinstance(enhanced_keywords, list):
                    # Combine original and enhanced keywords, remove duplicates
                    all_keywords = list(set(basic_keywords + enhanced_keywords))
                    logger.info(f"Enhanced keywords: {len(all_keywords)} total")
                    return all_keywords[:15]  # Limit to 15 keywords
                else:
                    logger.warning("Enhanced keywords response is not a list")
                    return basic_keywords
            except json.JSONDecodeError as e:
                logger.error(f"Failed to parse enhanced keywords: {e}")
                return basic_keywords
                
        except Exception as e:
            logger.error(f"Keyword enhancement failed: {e}")
            return basic_keywords
    
    def generate_smart_query(self, description: str, analysis: Dict) -> str:
        """
        Generate optimized database query based on user intent analysis
        """
        if not self.enabled or not self.model:
            return description
        
        try:
            prompt = f"""
            Dựa vào phân tích nhu cầu học tập, tạo một câu query tối ưu để tìm kiếm khóa học phù hợp:

            Mô tả gốc: "{description}"
            
            Phân tích:
            - Chủ đề chính: {analysis.get('main_subjects', [])}
            - Cấp độ: {analysis.get('skill_level', 'unknown')}
            - Mục tiêu: {analysis.get('learning_goals', [])}
            - Công nghệ: {analysis.get('technologies', [])}
            
            Tạo một câu query ngắn gọn, chính xác để tìm kiếm trong database khóa học.
            Query này sẽ được dùng để so sánh với tên khóa học và mô tả khóa học.
            
            Chỉ trả về câu query, không giải thích hay markdown.
            """
            
            response = self.model.generate_content(prompt)
            smart_query = response.text.strip().replace('"', '').replace('`', '')
            
            # Remove any markdown or extra formatting
            smart_query = re.sub(r'\*\*.*?\*\*', '', smart_query)
            smart_query = re.sub(r'\*.*?\*', '', smart_query)
            smart_query = smart_query.strip()
            
            logger.info(f"Generated smart query: {smart_query}")
            return smart_query if smart_query else description
            
        except Exception as e:
            logger.error(f"Smart query generation failed: {e}")
            return description
    
    def evaluate_course_relevance(self, course_info: Dict, user_analysis: Dict) -> Dict:
        """
        Use Gemini to evaluate how relevant a course is to user needs
        """
        if not self.enabled or not self.model:
            return {"relevance_score": 0.5, "reasons": ["Đánh giá cơ bản"]}
        
        try:
            prompt = f"""
            Đánh giá mức độ phù hợp của khóa học với nhu cầu người dùng:

            KHÓA HỌC:
            - Tên: {course_info.get('name', '')}
            - Mô tả: {course_info.get('description', '')}
            - Danh mục: {course_info.get('categories', [])}
            - Chương: {course_info.get('chapters', [])}

            NHU CẦU NGƯỜI DÙNG:
            - Chủ đề quan tâm: {user_analysis.get('main_subjects', [])}
            - Cấp độ: {user_analysis.get('skill_level', 'unknown')}
            - Mục tiêu: {user_analysis.get('learning_goals', [])}
            - Công nghệ: {user_analysis.get('technologies', [])}

            Trả về JSON:
            {{
                "relevance_score": 0.85,
                "reasons": ["lý do 1", "lý do 2"],
                "match_points": ["điểm phù hợp 1", "điểm phù hợp 2"]
            }}

            relevance_score từ 0.0 đến 1.0. Chỉ trả về JSON thuần túy.
            """
            
            response = self.model.generate_content(prompt)
            cleaned_response = self._clean_json_response(response.text)
            
            try:
                evaluation = json.loads(cleaned_response)
                logger.info("Successfully evaluated course relevance")
                return evaluation
            except json.JSONDecodeError as e:
                logger.error(f"Failed to parse course evaluation: {e}")
                return {"relevance_score": 0.5, "reasons": ["Lỗi đánh giá"], "match_points": ["Không thể phân tích"]}
                
        except Exception as e:
            logger.error(f"Course evaluation failed: {e}")
            return {"relevance_score": 0.5, "reasons": ["Lỗi đánh giá"], "match_points": ["Không thể phân tích"]}
    
    def _fallback_analysis(self, description: str) -> Dict:
        """
        Fallback analysis when Gemini is not available
        """
        # Simple keyword-based analysis
        text_lower = description.lower()
        
        # Detect skill level
        skill_level = "beginner"
        if any(word in text_lower for word in ["nâng cao", "advanced", "expert", "chuyên sâu"]):
            skill_level = "advanced"
        elif any(word in text_lower for word in ["trung bình", "intermediate", "cơ bản"]):
            skill_level = "intermediate"
        
        # Extract basic subjects
        subjects = []
        tech_keywords = {
            "python": "Python", "javascript": "JavaScript", "react": "React",
            "node.js": "Node.js", "html": "HTML", "css": "CSS",
            "machine learning": "Machine Learning", "ai": "AI",
            "web": "Web Development", "mobile": "Mobile Development"
        }
        
        for keyword, subject in tech_keywords.items():
            if keyword in text_lower:
                subjects.append(subject)
        
        return {
            "main_subjects": subjects,
            "skill_level": skill_level,
            "learning_goals": ["Học kỹ năng mới"],
            "keywords": description.split()[:5],
            "course_type_preference": "mixed",
            "technologies": subjects,
            "career_focus": "development",
            "urgency": "medium"
        }

# Global instance
gemini_service = GeminiService() 