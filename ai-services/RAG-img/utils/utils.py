import os
import uuid
import hashlib
from pathlib import Path
from typing import List, Dict, Any, Optional, Tuple
from PIL import Image
import cv2
import numpy as np
from datetime import datetime
import json
import logging
import aiofiles
from fastapi import UploadFile

# Setup logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class FileUtils:
    """Utility class for file operations"""
    
    @staticmethod
    def generate_unique_filename(original_filename: str) -> str:
        """Generate unique filename using timestamp and UUID"""
        file_ext = Path(original_filename).suffix
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        unique_id = str(uuid.uuid4())[:8]
        return f"{timestamp}_{unique_id}{file_ext}"
    
    @staticmethod
    def get_file_hash(file_path: str) -> str:
        """Calculate MD5 hash of file"""
        hash_md5 = hashlib.md5()
        with open(file_path, "rb") as f:
            for chunk in iter(lambda: f.read(4096), b""):
                hash_md5.update(chunk)
        return hash_md5.hexdigest()
    
    @staticmethod
    async def save_upload_file(upload_file: UploadFile, save_path: str) -> Dict[str, Any]:
        """Save uploaded file and return metadata"""
        try:
            # Create directory if it doesn't exist
            os.makedirs(os.path.dirname(save_path), exist_ok=True)
            
            # Save file
            async with aiofiles.open(save_path, 'wb') as f:
                content = await upload_file.read()
                await f.write(content)
            
            # Get file info
            file_size = os.path.getsize(save_path)
            file_hash = FileUtils.get_file_hash(save_path)
            
            return {
                "success": True,
                "file_path": save_path,
                "file_size": file_size,
                "file_hash": file_hash,
                "content_type": upload_file.content_type
            }
        except Exception as e:
            logger.error(f"Error saving file: {str(e)}")
            return {
                "success": False,
                "error": str(e)
            }
    
    @staticmethod
    def delete_file(file_path: str) -> bool:
        """Delete file safely"""
        try:
            if os.path.exists(file_path):
                os.remove(file_path)
                return True
            return False
        except Exception as e:
            logger.error(f"Error deleting file {file_path}: {str(e)}")
            return False


class ImageUtils:
    """Utility class for image processing"""
    
    @staticmethod
    def get_image_dimensions(image_path: str) -> Tuple[int, int]:
        """Get image dimensions (width, height)"""
        try:
            with Image.open(image_path) as img:
                return img.size
        except Exception as e:
            logger.error(f"Error getting image dimensions: {str(e)}")
            return (0, 0)
    
    @staticmethod
    def validate_image_format(image_path: str) -> bool:
        """Validate if file is a supported image format"""
        try:
            supported_formats = {'.jpg', '.jpeg', '.png', '.bmp', '.gif', '.tiff', '.webp'}
            file_ext = Path(image_path).suffix.lower()
            return file_ext in supported_formats
        except Exception as e:
            logger.error(f"Error validating image format: {str(e)}")
            return False
    
    @staticmethod
    def preprocess_image_for_ocr(image_path: str) -> np.ndarray:
        """Preprocess image for better OCR results"""
        try:
            # Read image
            image = cv2.imread(image_path)
            if image is None:
                raise ValueError(f"Could not read image: {image_path}")
            
            # Convert to grayscale
            gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
            
            # Apply Gaussian blur to reduce noise
            blurred = cv2.GaussianBlur(gray, (5, 5), 0)
            
            # Apply adaptive thresholding
            thresh = cv2.adaptiveThreshold(
                blurred, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C, 
                cv2.THRESH_BINARY, 11, 2
            )
            
            # Morphological operations to clean up
            kernel = np.ones((2, 2), np.uint8)
            cleaned = cv2.morphologyEx(thresh, cv2.MORPH_CLOSE, kernel)
            
            return cleaned
        except Exception as e:
            logger.error(f"Error preprocessing image: {str(e)}")
            # Return original image if preprocessing fails
            return cv2.imread(image_path, cv2.IMREAD_GRAYSCALE)
    
    @staticmethod
    def resize_image_if_needed(image_path: str, max_size: int = 2048) -> str:
        """Resize image if it's too large"""
        try:
            with Image.open(image_path) as img:
                width, height = img.size
                
                # Check if resize is needed
                if max(width, height) > max_size:
                    # Calculate new dimensions
                    if width > height:
                        new_width = max_size
                        new_height = int(height * max_size / width)
                    else:
                        new_height = max_size
                        new_width = int(width * max_size / height)
                    
                    # Resize image
                    resized = img.resize((new_width, new_height), Image.LANCZOS)
                    resized.save(image_path, quality=95)
                    logger.info(f"Resized image from {width}x{height} to {new_width}x{new_height}")
                
                return image_path
        except Exception as e:
            logger.error(f"Error resizing image: {str(e)}")
            return image_path


class TextUtils:
    """Utility class for text processing"""
    
    @staticmethod
    def clean_ocr_text(text: str) -> str:
        """Clean and normalize OCR text"""
        if not text:
            return ""
        
        # Remove extra whitespace
        text = ' '.join(text.split())
        
        # Remove common OCR artifacts
        text = text.replace('|', 'I')
        text = text.replace('0', 'O')  # Common OCR confusion
        
        # Remove non-printable characters
        text = ''.join(char for char in text if char.isprintable())
        
        return text.strip()
    
    @staticmethod
    def split_text_into_chunks(text: str, chunk_size: int = 500, overlap: int = 50) -> List[str]:
        """Split text into overlapping chunks for better processing"""
        if not text:
            return []
        
        words = text.split()
        chunks = []
        
        for i in range(0, len(words), chunk_size - overlap):
            chunk = ' '.join(words[i:i + chunk_size])
            chunks.append(chunk)
            
            # Break if we've reached the end
            if i + chunk_size >= len(words):
                break
        
        return chunks
    
    @staticmethod
    def extract_keywords(text: str, top_k: int = 10) -> List[str]:
        """Extract keywords from text (simple frequency-based)"""
        if not text:
            return []
        
        # Simple word frequency approach
        import re
        from collections import Counter
        
        # Remove punctuation and convert to lowercase
        words = re.findall(r'\b[a-zA-Z]{3,}\b', text.lower())
        
        # Common stopwords to exclude
        stopwords = {'the', 'and', 'for', 'are', 'but', 'not', 'you', 'all', 'can', 'had', 'her', 'was', 'one', 'our', 'out', 'day', 'get', 'has', 'him', 'his', 'how', 'its', 'may', 'new', 'now', 'old', 'see', 'two', 'who', 'boy', 'did', 'she', 'use', 'way', 'oil', 'sit', 'set', 'say', 'run', 'eat'}
        
        # Filter out stopwords
        filtered_words = [word for word in words if word not in stopwords]
        
        # Count frequency
        word_freq = Counter(filtered_words)
        
        # Return top keywords
        return [word for word, count in word_freq.most_common(top_k)]


class ConfigUtils:
    """Utility class for configuration management"""
    
    @staticmethod
    def load_config(config_path: str = "config.json") -> Dict[str, Any]:
        """Load configuration from file"""
        try:
            if os.path.exists(config_path):
                with open(config_path, 'r') as f:
                    return json.load(f)
            else:
                return ConfigUtils.get_default_config()
        except Exception as e:
            logger.error(f"Error loading config: {str(e)}")
            return ConfigUtils.get_default_config()
    
    @staticmethod
    def get_default_config() -> Dict[str, Any]:
        """Get default configuration"""
        return {
            "upload_dir": "uploads",
            "max_file_size": 10 * 1024 * 1024,  # 10MB
            "supported_formats": [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp"],
            "ocr_languages": ["vi", "en"],
            "embedding_model": "sentence-transformers/all-MiniLM-L6-v2",
            "vector_db_path": "vector_db",
            "chunk_size": 500,
            "chunk_overlap": 50,
            "similarity_threshold": 0.7,
            "max_results": 10
        }
    
    @staticmethod
    def save_config(config: Dict[str, Any], config_path: str = "config.json") -> bool:
        """Save configuration to file"""
        try:
            with open(config_path, 'w') as f:
                json.dump(config, f, indent=2)
            return True
        except Exception as e:
            logger.error(f"Error saving config: {str(e)}")
            return False


class ValidationUtils:
    """Utility class for validation"""
    
    @staticmethod
    def validate_file_size(file_size: int, max_size: int = 10 * 1024 * 1024) -> bool:
        """Validate file size"""
        return file_size <= max_size
    
    @staticmethod
    def validate_image_file(file: UploadFile) -> Dict[str, Any]:
        """Validate uploaded image file"""
        errors = []
        
        # Check content type
        if not file.content_type or not file.content_type.startswith('image/'):
            errors.append("File must be an image")
        
        # Check file extension
        file_ext = Path(file.filename).suffix.lower()
        supported_formats = {'.jpg', '.jpeg', '.png', '.bmp', '.gif', '.tiff', '.webp'}
        if file_ext not in supported_formats:
            errors.append(f"Unsupported file format: {file_ext}")
        
        return {
            "is_valid": len(errors) == 0,
            "errors": errors
        } 