from pydantic import BaseModel, Field
from typing import List, Optional, Dict, Any
from datetime import datetime


class ImageUploadResponse(BaseModel):
    """Response model for image upload"""
    success: bool = Field(..., description="Whether upload was successful")
    image_id: str = Field(..., description="Unique identifier for the uploaded image")
    filename: str = Field(..., description="Name of the uploaded file")
    extracted_text: str = Field(..., description="Text extracted from the image using OCR")
    message: str = Field(..., description="Status message")
    timestamp: datetime = Field(default_factory=datetime.now, description="Upload timestamp")


class QuestionRequest(BaseModel):
    """Request model for asking questions about images"""
    question: str = Field(..., description="Question to ask about the image(s)")
    image_id: Optional[str] = Field(None, description="Specific image ID to query (optional)")
    top_k: int = Field(default=5, description="Number of top results to retrieve")


class QuestionResponse(BaseModel):
    """Response model for question answers"""
    answer: str = Field(..., description="Generated answer to the question")
    confidence: float = Field(..., description="Confidence score of the answer")
    relevant_images: List[str] = Field(..., description="List of relevant image IDs")
    sources: List[Dict[str, Any]] = Field(..., description="Source information for the answer")
    timestamp: datetime = Field(default_factory=datetime.now, description="Response timestamp")


class SearchRequest(BaseModel):
    """Request model for semantic search"""
    query: str = Field(..., description="Search query")
    top_k: int = Field(default=10, description="Number of top results to return")
    threshold: float = Field(default=0.5, description="Minimum similarity threshold")


class SearchResult(BaseModel):
    """Model for search result"""
    image_id: str = Field(..., description="Image ID")
    filename: str = Field(..., description="Image filename")
    similarity: float = Field(..., description="Similarity score")
    matched_text: str = Field(..., description="Matching text segment")
    ocr_confidence: float = Field(..., description="OCR confidence score")
    detected_language: str = Field(..., description="Detected language")
    upload_timestamp: str = Field(..., description="Upload timestamp")


class SearchResponse(BaseModel):
    """Response model for search results"""
    results: List[SearchResult] = Field(..., description="Search results")
    total_results: int = Field(..., description="Total number of results")
    query: str = Field(..., description="Original search query")
    timestamp: datetime = Field(default_factory=datetime.now, description="Search timestamp")


class ImageInfo(BaseModel):
    """Model for image information"""
    image_id: str = Field(..., description="Unique identifier for the image")
    filename: str = Field(..., description="Original filename")
    description: Optional[str] = Field(None, description="Image description")
    extracted_text: str = Field(..., description="Text extracted from the image")
    upload_timestamp: str = Field(..., description="When the image was uploaded")
    file_size: int = Field(..., description="File size in bytes")
    dimensions: Dict[str, int] = Field(..., description="Image dimensions (width, height)")


class ImageListResponse(BaseModel):
    """Response model for listing images"""
    images: List[ImageInfo] = Field(..., description="List of uploaded images")
    total_count: int = Field(..., description="Total number of images")


class DeleteImageResponse(BaseModel):
    """Response model for image deletion"""
    success: bool = Field(..., description="Whether deletion was successful")
    message: str = Field(..., description="Status message")
    image_id: str = Field(..., description="ID of the deleted image")


class HealthCheckResponse(BaseModel):
    """Response model for health check"""
    status: str = Field(..., description="Service status")
    timestamp: datetime = Field(default_factory=datetime.now, description="Health check timestamp")
    services: Dict[str, str] = Field(..., description="Status of individual services")
    version: str = Field(..., description="API version")


class ErrorResponse(BaseModel):
    """Response model for errors"""
    error: str = Field(..., description="Error message")
    detail: Optional[str] = Field(None, description="Detailed error information")
    timestamp: datetime = Field(default_factory=datetime.now, description="Error timestamp") 