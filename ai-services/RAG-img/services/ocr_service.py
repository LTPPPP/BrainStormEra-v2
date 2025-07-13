import easyocr
import cv2
import numpy as np
from typing import List, Dict, Any, Optional
import logging
import asyncio
from concurrent.futures import ThreadPoolExecutor
import time
import os
import sys
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from utils.utils import ImageUtils, TextUtils

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class OCRService:
    """Service for Optical Character Recognition using EasyOCR"""
    
    def __init__(self, languages: List[str] = ['vi', 'en'], gpu: bool = False):
        self.languages = languages
        self.gpu = gpu
        self.reader = None
        self.executor = ThreadPoolExecutor(max_workers=2)
        self._initialize_reader()
    
    def _initialize_reader(self):
        """Initialize EasyOCR reader"""
        try:
            logger.info(f"Initializing EasyOCR reader for languages: {self.languages}")
            self.reader = easyocr.Reader(self.languages, gpu=self.gpu)
            logger.info("EasyOCR reader initialized successfully")
        except Exception as e:
            logger.error(f"Failed to initialize EasyOCR reader: {str(e)}")
            raise
    
    def _preprocess_image(self, image_path: str) -> np.ndarray:
        """Preprocess image for better OCR results"""
        try:
            processed_image = ImageUtils.preprocess_image_for_ocr(image_path)
            return processed_image
        except Exception as e:
            logger.error(f"Error preprocessing image: {str(e)}")
            return cv2.imread(image_path)
    
    def _extract_text_sync(self, image_path: str, detail: int = 0) -> Dict[str, Any]:
        """Synchronous text extraction from image"""
        try:
            start_time = time.time()
            
            if not ImageUtils.validate_image_format(image_path):
                raise ValueError(f"Unsupported image format: {image_path}")
            
            processed_image = self._preprocess_image(image_path)
            results = self.reader.readtext(processed_image, detail=detail)
            
            extracted_text = ""
            text_blocks = []
            confidence_scores = []
            
            for result in results:
                if detail == 0:
                    text = result
                    extracted_text += text + " "
                else:
                    bbox, text, confidence = result
                    cleaned_text = TextUtils.clean_ocr_text(text)
                    if cleaned_text:
                        extracted_text += cleaned_text + " "
                        text_blocks.append({
                            "text": cleaned_text,
                            "bbox": bbox,
                            "confidence": confidence
                        })
                        confidence_scores.append(confidence)
            
            extracted_text = TextUtils.clean_ocr_text(extracted_text)
            avg_confidence = sum(confidence_scores) / len(confidence_scores) if confidence_scores else 0.0
            detected_language = self._detect_language(extracted_text)
            processing_time = time.time() - start_time
            
            return {
                "success": True,
                "extracted_text": extracted_text,
                "text_blocks": text_blocks,
                "confidence": avg_confidence,
                "detected_language": detected_language,
                "processing_time": processing_time,
                "total_blocks": len(text_blocks)
            }
            
        except Exception as e:
            logger.error(f"Error extracting text from {image_path}: {str(e)}")
            return {
                "success": False,
                "error": str(e),
                "extracted_text": "",
                "confidence": 0.0,
                "processing_time": 0.0
            }
    
    async def extract_text(self, image_path: str, detail: int = 1) -> Dict[str, Any]:
        """Asynchronous text extraction from image"""
        try:
            loop = asyncio.get_event_loop()
            result = await loop.run_in_executor(
                self.executor, 
                self._extract_text_sync, 
                image_path, 
                detail
            )
            return result
        except Exception as e:
            logger.error(f"Error in async text extraction: {str(e)}")
            return {
                "success": False,
                "error": str(e),
                "extracted_text": "",
                "confidence": 0.0,
                "processing_time": 0.0
            }
    
    def _detect_language(self, text: str) -> str:
        """Simple language detection based on character patterns"""
        if not text:
            return "unknown"
        
        vietnamese_chars = "àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ"
        vietnamese_count = sum(1 for char in text.lower() if char in vietnamese_chars)
        
        if len(text) > 0 and vietnamese_count / len(text) > 0.05:
            return "vi"
        else:
            return "en"
    
    async def health_check(self) -> Dict[str, Any]:
        """Perform health check on OCR service"""
        try:
            test_image = np.ones((100, 300, 3), dtype=np.uint8) * 255
            cv2.putText(test_image, "Test OCR", (50, 50), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 0), 2)
            
            test_path = "temp_test_image.jpg"
            cv2.imwrite(test_path, test_image)
            
            start_time = time.time()
            result = await self.extract_text(test_path, detail=0)
            test_time = time.time() - start_time
            
            if os.path.exists(test_path):
                os.remove(test_path)
            
            return {
                "status": "healthy" if result["success"] else "unhealthy",
                "test_time": test_time,
                "languages": self.languages,
                "gpu_enabled": self.gpu,
                "test_result": result["success"]
            }
            
        except Exception as e:
            logger.error(f"Health check failed: {str(e)}")
            return {
                "status": "unhealthy",
                "error": str(e),
                "languages": self.languages,
                "gpu_enabled": self.gpu
            }
    
    def __del__(self):
        """Cleanup when service is destroyed"""
        try:
            if hasattr(self, 'executor'):
                self.executor.shutdown(wait=True)
        except:
            pass 