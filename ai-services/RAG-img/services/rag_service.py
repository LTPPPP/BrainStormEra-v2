import os
import sys
import uuid
import time
import json
import logging
from typing import List, Dict, Any, Optional, Tuple
from datetime import datetime
from pathlib import Path
import asyncio
from dataclasses import dataclass
from fastapi import UploadFile

# Add parent directory to path
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from services.ocr_service import OCRService
from services.embedding_service import EmbeddingService
from services.vector_db_service import VectorDBService
from utils.utils import FileUtils, ImageUtils, TextUtils, ValidationUtils

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


@dataclass
class ImageProcessingResult:
    """Result of image processing"""
    image_id: str
    filename: str
    file_path: str
    file_size: int
    dimensions: Tuple[int, int]
    extracted_text: str
    ocr_confidence: float
    detected_language: str
    processing_time: float
    success: bool
    error_message: Optional[str] = None


@dataclass
class QuestionAnswerResult:
    """Result of question answering"""
    question: str
    answer: str
    confidence: float
    relevant_images: List[str]
    sources: List[Dict[str, Any]]
    processing_time: float
    success: bool
    error_message: Optional[str] = None


class RAGService:
    """Main RAG service that orchestrates all components"""
    
    def __init__(self, upload_dir: str = "uploads", db_path: str = "chroma_db"):
        """
        Initialize RAG service
        
        Args:
            upload_dir: Directory to store uploaded images
            db_path: Path to vector database
        """
        self.upload_dir = upload_dir
        self.db_path = db_path
        
        # Initialize services
        self.ocr_service = OCRService(languages=['vi', 'en'])
        self.embedding_service = EmbeddingService()
        self.vector_db_service = VectorDBService(db_path=db_path)
        
        # Create upload directory
        os.makedirs(upload_dir, exist_ok=True)
        
        # In-memory storage for image metadata
        self.image_metadata: Dict[str, Dict[str, Any]] = {}
        
        logger.info("RAG service initialized successfully")
    
    async def process_image(self, file: UploadFile, description: Optional[str] = None) -> ImageProcessingResult:
        """
        Process uploaded image: save, OCR, embed, and store in vector DB
        
        Args:
            file: Uploaded image file
            description: Optional description of the image
            
        Returns:
            ImageProcessingResult object
        """
        start_time = time.time()
        image_id = str(uuid.uuid4())
        
        try:
            # Validate file
            validation_result = ValidationUtils.validate_image_file(file)
            if not validation_result["is_valid"]:
                return ImageProcessingResult(
                    image_id=image_id,
                    filename=file.filename,
                    file_path="",
                    file_size=0,
                    dimensions=(0, 0),
                    extracted_text="",
                    ocr_confidence=0.0,
                    detected_language="",
                    processing_time=time.time() - start_time,
                    success=False,
                    error_message="; ".join(validation_result["errors"])
                )
            
            # Generate unique filename and save path
            unique_filename = FileUtils.generate_unique_filename(file.filename)
            save_path = os.path.join(self.upload_dir, unique_filename)
            
            # Save uploaded file
            save_result = await FileUtils.save_upload_file(file, save_path)
            if not save_result["success"]:
                return ImageProcessingResult(
                    image_id=image_id,
                    filename=file.filename,
                    file_path="",
                    file_size=0,
                    dimensions=(0, 0),
                    extracted_text="",
                    ocr_confidence=0.0,
                    detected_language="",
                    processing_time=time.time() - start_time,
                    success=False,
                    error_message=save_result["error"]
                )
            
            # Get image dimensions
            dimensions = ImageUtils.get_image_dimensions(save_path)
            
            # Resize image if needed
            ImageUtils.resize_image_if_needed(save_path)
            
            # Extract text using OCR
            ocr_result = await self.ocr_service.extract_text(save_path)
            if not ocr_result["success"]:
                return ImageProcessingResult(
                    image_id=image_id,
                    filename=file.filename,
                    file_path=save_path,
                    file_size=save_result["file_size"],
                    dimensions=dimensions,
                    extracted_text="",
                    ocr_confidence=0.0,
                    detected_language="",
                    processing_time=time.time() - start_time,
                    success=False,
                    error_message=ocr_result["error"]
                )
            
            extracted_text = ocr_result["extracted_text"]
            
            # Skip embedding if no text extracted
            if not extracted_text or not extracted_text.strip():
                logger.warning(f"No text extracted from image {image_id}")
                extracted_text = "No text found in image"
            
            # Create embedding
            embedding = await self.embedding_service.encode_text(extracted_text)
            
            # Store in vector database
            metadata = {
                "filename": file.filename,
                "unique_filename": unique_filename,
                "file_path": save_path,
                "file_size": save_result["file_size"],
                "dimensions": dimensions,
                "description": description,
                "ocr_confidence": ocr_result["confidence"],
                "detected_language": ocr_result["detected_language"],
                "upload_timestamp": datetime.now().isoformat()
            }
            
            vector_result = await self.vector_db_service.add_image_text(
                image_id=image_id,
                text=extracted_text,
                embedding=embedding[0],  # embedding service returns array
                metadata=metadata
            )
            
            if not vector_result["success"]:
                logger.error(f"Failed to store in vector DB: {vector_result['error']}")
            
            # Store metadata in memory
            self.image_metadata[image_id] = {
                "filename": file.filename,
                "unique_filename": unique_filename,
                "file_path": save_path,
                "file_size": save_result["file_size"],
                "dimensions": dimensions,
                "extracted_text": extracted_text,
                "ocr_confidence": ocr_result["confidence"],
                "detected_language": ocr_result["detected_language"],
                "description": description,
                "upload_timestamp": datetime.now().isoformat()
            }
            
            processing_time = time.time() - start_time
            
            return ImageProcessingResult(
                image_id=image_id,
                filename=file.filename,
                file_path=save_path,
                file_size=save_result["file_size"],
                dimensions=dimensions,
                extracted_text=extracted_text,
                ocr_confidence=ocr_result["confidence"],
                detected_language=ocr_result["detected_language"],
                processing_time=processing_time,
                success=True
            )
            
        except Exception as e:
            logger.error(f"Error processing image: {str(e)}")
            return ImageProcessingResult(
                image_id=image_id,
                filename=file.filename if file else "",
                file_path="",
                file_size=0,
                dimensions=(0, 0),
                extracted_text="",
                ocr_confidence=0.0,
                detected_language="",
                processing_time=time.time() - start_time,
                success=False,
                error_message=str(e)
            )
    
    async def answer_question(self, question: str, image_id: Optional[str] = None, 
                             top_k: int = 5) -> QuestionAnswerResult:
        """
        Answer question about images using RAG
        
        Args:
            question: Question to answer
            image_id: Optional specific image ID to query
            top_k: Number of top results to retrieve
            
        Returns:
            QuestionAnswerResult object
        """
        start_time = time.time()
        
        try:
            # Create question embedding
            question_embedding = await self.embedding_service.encode_text(question)
            
            # Search for relevant images
            if image_id:
                # Search for specific image
                search_result = await self.vector_db_service.search_vectors(
                    query_embedding=question_embedding[0],
                    n_results=1,
                    where={"image_id": image_id}
                )
            else:
                # Search all images
                search_result = await self.vector_db_service.search_similar_images(
                    query_embedding=question_embedding[0],
                    n_results=top_k,
                    similarity_threshold=0.3
                )
            
            if not search_result["success"] or not search_result["results"]:
                return QuestionAnswerResult(
                    question=question,
                    answer="I couldn't find any relevant images to answer your question.",
                    confidence=0.0,
                    relevant_images=[],
                    sources=[],
                    processing_time=time.time() - start_time,
                    success=False,
                    error_message="No relevant images found"
                )
            
            # Extract relevant text and generate answer
            relevant_texts = []
            relevant_images = []
            sources = []
            
            for result in search_result["results"]:
                relevant_texts.append(result["document"])
                img_id = result["metadata"].get("image_id", "")
                relevant_images.append(img_id)
                
                sources.append({
                    "image_id": img_id,
                    "filename": result["metadata"].get("filename", ""),
                    "text": result["document"],
                    "similarity": result["similarity"],
                    "confidence": result["metadata"].get("ocr_confidence", 0.0)
                })
            
            # Simple answer generation based on retrieved context
            answer = await self._generate_answer(question, relevant_texts, sources)
            
            # Calculate overall confidence
            avg_similarity = sum(s["similarity"] for s in sources) / len(sources)
            avg_ocr_confidence = sum(s["confidence"] for s in sources) / len(sources)
            overall_confidence = (avg_similarity + avg_ocr_confidence) / 2
            
            processing_time = time.time() - start_time
            
            return QuestionAnswerResult(
                question=question,
                answer=answer,
                confidence=overall_confidence,
                relevant_images=relevant_images,
                sources=sources,
                processing_time=processing_time,
                success=True
            )
            
        except Exception as e:
            logger.error(f"Error answering question: {str(e)}")
            return QuestionAnswerResult(
                question=question,
                answer="I encountered an error while processing your question.",
                confidence=0.0,
                relevant_images=[],
                sources=[],
                processing_time=time.time() - start_time,
                success=False,
                error_message=str(e)
            )
    
    async def _generate_answer(self, question: str, relevant_texts: List[str], 
                             sources: List[Dict[str, Any]]) -> str:
        """
        Generate answer based on retrieved context
        
        Args:
            question: The question
            relevant_texts: List of relevant texts
            sources: List of source information
            
        Returns:
            Generated answer
        """
        try:
            # Combine all relevant texts
            combined_text = " ".join(relevant_texts)
            
            # Simple keyword-based answering
            question_lower = question.lower()
            
            # Check if question is about specific content
            if any(keyword in question_lower for keyword in ["what", "gì", "nào"]):
                if combined_text.strip():
                    return f"Based on the images, I found the following content: {combined_text}"
                else:
                    return "I couldn't find any readable text in the images."
            
            # Check if question is about presence of something
            elif any(keyword in question_lower for keyword in ["có", "is there", "does", "have"]):
                # Extract potential search terms from question
                search_terms = [word for word in question_lower.split() 
                              if len(word) > 2 and word not in ["what", "does", "have", "there", "this"]]
                
                found_terms = []
                for term in search_terms:
                    if term in combined_text.lower():
                        found_terms.append(term)
                
                if found_terms:
                    return f"Yes, I found mentions of: {', '.join(found_terms)} in the images."
                else:
                    return "I couldn't find the specific content you're asking about in the images."
            
            # Check if question is about quantity
            elif any(keyword in question_lower for keyword in ["how many", "bao nhiêu", "count"]):
                numbers = [word for word in combined_text.split() if word.isdigit()]
                if numbers:
                    return f"I found these numbers in the images: {', '.join(numbers)}"
                else:
                    return "I couldn't find any specific numbers in the images."
            
            # Default response
            else:
                if combined_text.strip():
                    return f"Based on the content I found in the images: {combined_text}. Please let me know if you need more specific information."
                else:
                    return "I couldn't extract meaningful text from the images to answer your question."
                    
        except Exception as e:
            logger.error(f"Error generating answer: {str(e)}")
            return "I encountered an error while generating the answer."
    
    async def search_images(self, query: str, top_k: int = 10, 
                           similarity_threshold: float = 0.5) -> Dict[str, Any]:
        """
        Search for images based on text query
        
        Args:
            query: Search query
            top_k: Number of results to return
            similarity_threshold: Minimum similarity threshold
            
        Returns:
            Dictionary with search results
        """
        try:
            # Create query embedding
            query_embedding = await self.embedding_service.encode_text(query)
            
            # Search in vector database
            search_result = await self.vector_db_service.search_similar_images(
                query_embedding=query_embedding[0],
                n_results=top_k,
                similarity_threshold=similarity_threshold
            )
            
            if not search_result["success"]:
                return search_result
            
            # Enhance results with metadata
            enhanced_results = []
            for result in search_result["results"]:
                img_id = result["metadata"].get("image_id", "")
                enhanced_result = {
                    "image_id": img_id,
                    "filename": result["metadata"].get("filename", ""),
                    "similarity": result["similarity"],
                    "matched_text": result["document"],
                    "ocr_confidence": result["metadata"].get("ocr_confidence", 0.0),
                    "detected_language": result["metadata"].get("detected_language", ""),
                    "upload_timestamp": result["metadata"].get("upload_timestamp", "")
                }
                enhanced_results.append(enhanced_result)
            
            return {
                "success": True,
                "results": enhanced_results,
                "total_results": len(enhanced_results),
                "query": query
            }
            
        except Exception as e:
            logger.error(f"Error searching images: {str(e)}")
            return {
                "success": False,
                "error": str(e),
                "results": [],
                "total_results": 0
            }
    
    async def get_image_info(self, image_id: str) -> Optional[Dict[str, Any]]:
        """
        Get information about a specific image
        
        Args:
            image_id: Image ID
            
        Returns:
            Dictionary with image information or None if not found
        """
        try:
            if image_id in self.image_metadata:
                return self.image_metadata[image_id]
            else:
                return None
        except Exception as e:
            logger.error(f"Error getting image info: {str(e)}")
            return None
    
    async def list_images(self) -> List[Dict[str, Any]]:
        """
        List all uploaded images
        
        Returns:
            List of image metadata
        """
        try:
            return list(self.image_metadata.values())
        except Exception as e:
            logger.error(f"Error listing images: {str(e)}")
            return []
    
    async def delete_image(self, image_id: str) -> Dict[str, Any]:
        """
        Delete an image and its associated data
        
        Args:
            image_id: Image ID to delete
            
        Returns:
            Dictionary with deletion results
        """
        try:
            # Get image info
            image_info = await self.get_image_info(image_id)
            if not image_info:
                return {
                    "success": False,
                    "error": "Image not found"
                }
            
            # Delete file
            file_path = image_info.get("file_path", "")
            if file_path and os.path.exists(file_path):
                FileUtils.delete_file(file_path)
            
            # Delete from vector database
            await self.vector_db_service.delete_vectors([f"img_{image_id}"])
            
            # Remove from memory
            if image_id in self.image_metadata:
                del self.image_metadata[image_id]
            
            return {
                "success": True,
                "message": f"Image {image_id} deleted successfully"
            }
            
        except Exception as e:
            logger.error(f"Error deleting image: {str(e)}")
            return {
                "success": False,
                "error": str(e)
            }
    
    async def health_check(self) -> Dict[str, Any]:
        """Perform health check on all services"""
        try:
            start_time = time.time()
            
            # Check individual services
            ocr_health = await self.ocr_service.health_check()
            embedding_health = await self.embedding_service.health_check()
            vector_db_health = await self.vector_db_service.health_check()
            
            # Check file system
            upload_dir_exists = os.path.exists(self.upload_dir)
            upload_dir_writable = os.access(self.upload_dir, os.W_OK)
            
            # Overall health
            all_healthy = (
                ocr_health["status"] == "healthy" and
                embedding_health["status"] == "healthy" and
                vector_db_health["status"] == "healthy" and
                upload_dir_exists and
                upload_dir_writable
            )
            
            health_time = time.time() - start_time
            
            return {
                "status": "healthy" if all_healthy else "unhealthy",
                "check_time": health_time,
                "upload_dir": self.upload_dir,
                "upload_dir_exists": upload_dir_exists,
                "upload_dir_writable": upload_dir_writable,
                "total_images": len(self.image_metadata),
                "services": {
                    "ocr": ocr_health,
                    "embedding": embedding_health,
                    "vector_db": vector_db_health
                }
            }
            
        except Exception as e:
            logger.error(f"Health check failed: {str(e)}")
            return {
                "status": "unhealthy",
                "error": str(e),
                "upload_dir": self.upload_dir,
                "total_images": len(self.image_metadata)
            } 