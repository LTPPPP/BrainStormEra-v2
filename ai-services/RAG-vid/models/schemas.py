from pydantic import BaseModel, HttpUrl, Field
from typing import List, Optional, Dict, Any
from enum import Enum

class ModelPreference(str, Enum):
    HUGGINGFACE = "huggingface"
    GEMINI = "gemini"

class VideoProcessRequest(BaseModel):
    url: HttpUrl = Field(..., description="YouTube video URL")
    
    class Config:
        json_schema_extra = {
            "example": {
                "url": "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
            }
        }

class QuestionRequest(BaseModel):
    video_id: str = Field(..., description="YouTube video ID")
    question: str = Field(..., min_length=1, max_length=1000, description="Question about the video")
    model_preference: ModelPreference = Field(default=ModelPreference.HUGGINGFACE, description="Preferred model for answering")
    
    class Config:
        json_schema_extra = {
            "example": {
                "video_id": "dQw4w9WgXcQ",
                "question": "What is the main topic of this video?",
                "model_preference": "huggingface"
            }
        }

class VideoInfo(BaseModel):
    video_id: str = Field(..., description="YouTube video ID")
    title: str = Field(..., description="Video title")
    duration: int = Field(..., description="Video duration in seconds")
    transcript_length: int = Field(..., description="Length of transcript in characters")
    status: str = Field(..., description="Processing status")
    
    class Config:
        json_schema_extra = {
            "example": {
                "video_id": "dQw4w9WgXcQ",
                "title": "Sample Video",
                "duration": 180,
                "transcript_length": 5000,
                "status": "processing"
            }
        }

class QuestionResponse(BaseModel):
    question: str = Field(..., description="The original question")
    answer: str = Field(..., description="The generated answer")
    confidence: float = Field(..., ge=0.0, le=1.0, description="Confidence score of the answer")
    sources: List[Dict[str, Any]] = Field(default=[], description="Source chunks used for the answer")
    
    class Config:
        json_schema_extra = {
            "example": {
                "question": "What is the main topic of this video?",
                "answer": "The main topic of this video is...",
                "confidence": 0.85,
                "sources": [
                    {
                        "text": "This is the relevant text chunk...",
                        "timestamp": "00:02:30",
                        "similarity": 0.92
                    }
                ]
            }
        }

class ProcessedVideo(BaseModel):
    video_id: str
    title: str
    duration: int
    processed_at: str
    chunk_count: int

class ErrorResponse(BaseModel):
    error: str
    detail: Optional[str] = None
    code: Optional[int] = None 