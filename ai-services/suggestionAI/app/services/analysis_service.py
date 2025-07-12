"""
Analysis service for text processing and similarity calculations
"""

import re
from typing import List
from difflib import SequenceMatcher


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