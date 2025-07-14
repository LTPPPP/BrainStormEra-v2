from fastapi import FastAPI, HTTPException, BackgroundTasks
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, HttpUrl
from typing import List, Optional
import uvicorn
import logging
import os
from contextlib import asynccontextmanager
from dotenv import load_dotenv

# Load environment variables
load_dotenv()

from services.youtube_service import YouTubeService
from services.rag_service import RAGService
from models.schemas import VideoProcessRequest, QuestionRequest, VideoInfo, QuestionResponse

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Global services
youtube_service = None
rag_service = None

# Additional request models
class VideoProcessingMode(BaseModel):
    url: HttpUrl
    mode: str = "local"  # "local" or "transcript_api"
    language: Optional[str] = None
    whisper_model: str = "base"  # "tiny", "base", "small", "medium", "large"

class CleanupRequest(BaseModel):
    video_id: Optional[str] = None
    cleanup_all: bool = False

@asynccontextmanager
async def lifespan(app: FastAPI):
    # Startup
    global youtube_service, rag_service
    logger.info("Initializing services...")
    
    try:
        youtube_service = YouTubeService()
        await youtube_service.initialize()
        
        rag_service = RAGService()
        await rag_service.initialize()
        logger.info("Services initialized successfully")
    except Exception as e:
        logger.error(f"Failed to initialize services: {e}")
        raise
    
    yield
    
    # Shutdown
    logger.info("Shutting down services...")

app = FastAPI(
    title="RAG Video Service",
    description="A FastAPI service for extracting data from YouTube videos using local processing and answering questions using RAG",
    version="2.0.0",
    lifespan=lifespan
)

# Add CORS middleware
allowed_origins = os.getenv("ALLOWED_ORIGINS", "*").split(",")
app.add_middleware(
    CORSMiddleware,
    allow_origins=allowed_origins,
    allow_credentials=True,
    allow_methods=os.getenv("ALLOWED_METHODS", "*").split(","),
    allow_headers=os.getenv("ALLOWED_HEADERS", "*").split(","),
)

@app.get("/")
async def root():
    return {"message": "RAG Video Service with Local Processing is running!", "version": "2.0.0"}

@app.get("/health")
async def health_check():
    return {"status": "healthy", "services": {
        "youtube": youtube_service is not None,
        "rag": rag_service is not None
    }}

@app.post("/check-video-availability")
async def check_video_availability(request: VideoProcessRequest):
    """
    Check if a video is available for processing
    """
    try:
        logger.info(f"Checking availability for video: {request.url}")
        
        result = await youtube_service.check_video_availability(str(request.url))
        return result
    
    except Exception as e:
        logger.error(f"Error checking video availability: {e}")
        raise HTTPException(status_code=500, detail=f"Error checking video availability: {str(e)}")

@app.post("/check-transcript", response_model=dict)
async def check_transcript_availability(request: VideoProcessRequest):
    """
    Check if transcript is available for a YouTube video (using transcript API)
    """
    try:
        logger.info(f"Checking transcript availability for video: {request.url}")
        
        video_id = youtube_service.extract_video_id(str(request.url))
        if not video_id:
            raise HTTPException(status_code=400, detail="Invalid YouTube URL")
        
        # Try to get video info first
        try:
            video_info = await youtube_service.get_video_info(str(request.url))
        except Exception as e:
            logger.error(f"Error getting video info: {e}")
            raise HTTPException(status_code=400, detail=f"Cannot access video: {str(e)}")
        
        # Check transcript availability using API
        transcript = await youtube_service.get_transcript(str(request.url), use_local_processing=False)
        
        if transcript:
            return {
                "video_id": video_id,
                "title": video_info["title"],
                "transcript_available": True,
                "transcript_length": len(transcript),
                "message": "Transcript is available for this video"
            }
        else:
            return {
                "video_id": video_id,
                "title": video_info["title"],
                "transcript_available": False,
                "transcript_length": 0,
                "message": "No transcript available. This video may not have captions, or transcripts may be disabled."
            }
    
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error checking transcript availability: {e}")
        raise HTTPException(status_code=500, detail=f"Error checking transcript: {str(e)}")

@app.post("/process-video", response_model=VideoInfo)
async def process_video(request: VideoProcessRequest, background_tasks: BackgroundTasks):
    """
    Process a YouTube video using local processing with automatic fallback to transcript API
    """
    try:
        logger.info(f"Processing video: {request.url}")
        
        # Get video info first
        video_info = await youtube_service.get_video_info(str(request.url))
        logger.info(f"Video info obtained: {video_info['title']} ({video_info['duration']}s)")
        
        # Try to get transcript with fallback mechanism
        transcript = await youtube_service.get_transcript(str(request.url), use_local_processing=True)
        
        if not transcript:
            # Provide detailed error message
            error_msg = f"Could not extract transcript from video '{video_info['title']}' (ID: {video_info['video_id']}) using any available method. "
            error_msg += "This can happen when: "
            error_msg += "1) The video has no captions/subtitles, "
            error_msg += "2) The video is private or age-restricted, "
            error_msg += "3) YouTube's bot detection is blocking downloads, "
            error_msg += "4) The video has unusual encoding or format restrictions, "
            error_msg += "5) The transcript data is corrupted or unavailable. "
            error_msg += "Please try a different video or contact support if this issue persists."
            
            logger.error(error_msg)
            raise HTTPException(status_code=400, detail=error_msg)
        
        logger.info(f"Transcript obtained successfully, length: {len(transcript)} characters")
        
        # Process transcript in background for RAG
        background_tasks.add_task(
            rag_service.process_transcript,
            video_info["video_id"],
            transcript,
            video_info["title"]
        )
        
        return VideoInfo(
            video_id=video_info["video_id"],
            title=video_info["title"],
            duration=video_info["duration"],
            transcript_length=len(transcript),
            status="processing"
        )
    
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error processing video: {e}")
        
        # Provide more user-friendly error messages
        error_msg = str(e)
        if "Invalid YouTube URL" in error_msg:
            raise HTTPException(status_code=400, detail="Invalid YouTube URL format. Please provide a valid YouTube video URL.")
        elif "Video unavailable" in error_msg:
            raise HTTPException(status_code=400, detail="This video is unavailable (may be deleted, private, or region-restricted).")
        elif "Failed to get video information" in error_msg:
            raise HTTPException(status_code=400, detail="Could not access video information. The video may be private, deleted, or have restricted access.")
        else:
            raise HTTPException(status_code=500, detail=f"An error occurred while processing the video: {error_msg}")

@app.post("/process-video-advanced")
async def process_video_advanced(request: VideoProcessingMode, background_tasks: BackgroundTasks):
    """
    Process a YouTube video with advanced options
    """
    try:
        logger.info(f"Processing video with mode {request.mode}: {request.url}")
        
        if request.mode == "local":
            # Use local processing
            result = await youtube_service.process_video_locally(str(request.url), request.language)
            
            # Process transcript in background for RAG
            background_tasks.add_task(
                rag_service.process_transcript,
                result["video_id"],
                result["transcript"],
                result["title"]
            )
            
            return {
                "video_id": result["video_id"],
                "title": result["title"],
                "duration": result["duration"],
                "transcript_length": len(result["transcript"]),
                "processing_method": result["processing_method"],
                "language": result["language"],
                "file_size": result["file_size"],
                "status": "processing"
            }
        
        elif request.mode == "transcript_api":
            # Use transcript API
            video_info = await youtube_service.get_video_info(str(request.url))
            transcript = await youtube_service.get_transcript(str(request.url), use_local_processing=False)
            
            if not transcript:
                error_msg = f"Could not extract transcript from video {video_info['video_id']} using transcript API."
                logger.warning(error_msg)
                raise HTTPException(status_code=400, detail=error_msg)
            
            # Process transcript in background for RAG
            background_tasks.add_task(
                rag_service.process_transcript,
                video_info["video_id"],
                transcript,
                video_info["title"]
            )
            
            return {
                "video_id": video_info["video_id"],
                "title": video_info["title"],
                "duration": video_info["duration"],
                "transcript_length": len(transcript),
                "processing_method": "transcript_api",
                "language": "unknown",
                "file_size": 0,
                "status": "processing"
            }
        
        else:
            raise HTTPException(status_code=400, detail="Invalid processing mode. Use 'local' or 'transcript_api'")
    
    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error processing video: {e}")
        raise HTTPException(status_code=500, detail=f"Error processing video: {str(e)}")

@app.post("/ask-question", response_model=QuestionResponse)
async def ask_question(request: QuestionRequest):
    """
    Ask a question about a processed video
    """
    try:
        logger.info(f"Answering question for video {request.video_id}: {request.question}")
        
        # Check if video is processed
        if not await rag_service.is_video_processed(request.video_id):
            raise HTTPException(status_code=400, detail="Video not found or not processed yet")
        
        # Get answer using RAG
        answer = await rag_service.answer_question(
            request.video_id,
            request.question,
            request.model_preference
        )
        
        return QuestionResponse(
            question=request.question,
            answer=answer["answer"],
            confidence=answer["confidence"],
            sources=answer["sources"]
        )
    
    except Exception as e:
        logger.error(f"Error answering question: {e}")
        raise HTTPException(status_code=500, detail=f"Error answering question: {str(e)}")

@app.get("/videos")
async def list_processed_videos():
    """
    List all processed videos
    """
    try:
        videos = await rag_service.list_processed_videos()
        return {"videos": videos}
    except Exception as e:
        logger.error(f"Error listing videos: {e}")
        raise HTTPException(status_code=500, detail=f"Error listing videos: {str(e)}")

@app.delete("/videos/{video_id}")
async def delete_video(video_id: str):
    """
    Delete a processed video and its data
    """
    try:
        await rag_service.delete_video(video_id)
        # Also cleanup video files
        await youtube_service.cleanup_video_files(video_id)
        return {"message": f"Video {video_id} deleted successfully"}
    except Exception as e:
        logger.error(f"Error deleting video: {e}")
        raise HTTPException(status_code=500, detail=f"Error deleting video: {str(e)}")

@app.get("/storage-info")
async def get_storage_info():
    """
    Get storage information for downloaded videos
    """
    try:
        storage_info = await youtube_service.get_storage_info()
        return storage_info
    except Exception as e:
        logger.error(f"Error getting storage info: {e}")
        raise HTTPException(status_code=500, detail=f"Error getting storage info: {str(e)}")

@app.post("/cleanup")
async def cleanup_files(request: CleanupRequest):
    """
    Clean up downloaded video files
    """
    try:
        if request.cleanup_all:
            await youtube_service.cleanup_all_downloads()
            return {"message": "All downloaded files cleaned up successfully"}
        elif request.video_id:
            await youtube_service.cleanup_video_files(request.video_id)
            return {"message": f"Files for video {request.video_id} cleaned up successfully"}
        else:
            raise HTTPException(status_code=400, detail="Specify either video_id or set cleanup_all=True")
    
    except Exception as e:
        logger.error(f"Error cleaning up files: {e}")
        raise HTTPException(status_code=500, detail=f"Error cleaning up files: {str(e)}")

@app.post("/quick-check")
async def quick_check(request: VideoProcessRequest):
    """
    Quick check to see if a video can be processed (has transcript or can be downloaded)
    """
    try:
        logger.info(f"Quick check for video: {request.url}")
        
        # Get video info
        video_info = await youtube_service.get_video_info(str(request.url))
        
        result = {
            "video_info": {
                "video_id": video_info["video_id"],
                "title": video_info["title"],
                "duration": video_info["duration"],
                "uploader": video_info["uploader"]
            },
            "can_process": False,
            "processing_methods": [],
            "recommendations": []
        }
        
        # Check transcript API availability
        try:
            transcript = await youtube_service.get_transcript(str(request.url), use_local_processing=False)
            if transcript:
                result["processing_methods"].append({
                    "method": "transcript_api",
                    "available": True,
                    "transcript_length": len(transcript)
                })
                result["can_process"] = True
        except Exception as e:
            result["processing_methods"].append({
                "method": "transcript_api",
                "available": False,
                "error": str(e)
            })
        
        # Check if video info is accessible (indicator for potential download)
        if video_info.get("duration", 0) > 0:
            result["processing_methods"].append({
                "method": "local_processing",
                "available": True,
                "note": "Video appears accessible for download (not guaranteed)"
            })
            result["can_process"] = True
        else:
            result["processing_methods"].append({
                "method": "local_processing",
                "available": False,
                "note": "Video may not be accessible for download"
            })
        
        # Provide recommendations
        if result["can_process"]:
            if any(method["method"] == "transcript_api" and method["available"] for method in result["processing_methods"]):
                result["recommendations"].append("Use /process-video for fast processing with transcript API fallback")
            else:
                result["recommendations"].append("Use /process-video for local processing (slower but works without captions)")
        else:
            result["recommendations"].append("This video may not be processable. Try a different video with captions.")
        
        return result
        
    except Exception as e:
        logger.error(f"Error in quick check: {e}")
        raise HTTPException(status_code=500, detail=f"Error checking video: {str(e)}")

@app.post("/test-processing")
async def test_processing(request: VideoProcessRequest):
    """
    Test both local processing and transcript API for a video
    """
    try:
        logger.info(f"Testing processing methods for video: {request.url}")
        
        # Get video info
        video_info = await youtube_service.get_video_info(str(request.url))
        
        results = {
            "video_info": video_info,
            "local_processing": {"success": False, "error": None, "transcript_length": 0},
            "transcript_api": {"success": False, "error": None, "transcript_length": 0}
        }
        
        # Test local processing
        try:
            logger.info("Testing local processing...")
            local_result = await youtube_service.process_video_locally(str(request.url))
            results["local_processing"]["success"] = True
            results["local_processing"]["transcript_length"] = len(local_result["transcript"])
            results["local_processing"]["method"] = local_result["processing_method"]
            results["local_processing"]["language"] = local_result["language"]
            results["local_processing"]["file_size"] = local_result["file_size"]
            logger.info("✅ Local processing successful")
        except Exception as e:
            results["local_processing"]["error"] = str(e)
            logger.warning(f"❌ Local processing failed: {e}")
        
        # Test transcript API
        try:
            logger.info("Testing transcript API...")
            transcript = await youtube_service.get_transcript(str(request.url), use_local_processing=False)
            if transcript:
                results["transcript_api"]["success"] = True
                results["transcript_api"]["transcript_length"] = len(transcript)
                logger.info("✅ Transcript API successful")
            else:
                results["transcript_api"]["error"] = "No transcript available"
                logger.warning("❌ No transcript available from API")
        except Exception as e:
            results["transcript_api"]["error"] = str(e)
            logger.warning(f"❌ Transcript API failed: {e}")
        
        # Clean up downloaded files
        try:
            await youtube_service.cleanup_video_files(video_info["video_id"])
        except:
            pass
        
        return results
        
    except Exception as e:
        logger.error(f"Error in test processing: {e}")
        raise HTTPException(status_code=500, detail=f"Error in test processing: {str(e)}")

@app.get("/processing-modes")
async def get_processing_modes():
    """
    Get available processing modes and options
    """
    return {
        "processing_modes": [
            {
                "mode": "local",
                "description": "Download video and process locally using Whisper",
                "pros": ["Works with any video", "High accuracy", "Supports multiple languages"],
                "cons": ["Requires more storage", "Takes longer", "Requires more computing power"]
            },
            {
                "mode": "transcript_api",
                "description": "Use YouTube's transcript API",
                "pros": ["Fast processing", "No storage required", "Low computing requirements"],
                "cons": ["Only works with videos that have captions", "Limited language support"]
            }
        ],
        "whisper_models": [
            {"name": "tiny", "description": "Fastest, lowest accuracy"},
            {"name": "base", "description": "Good balance of speed and accuracy (default)"},
            {"name": "small", "description": "Better accuracy, slower"},
            {"name": "medium", "description": "High accuracy, much slower"},
            {"name": "large", "description": "Highest accuracy, very slow"}
        ],
        "supported_languages": ["auto", "en", "vi", "zh", "ja", "ko", "es", "fr", "de", "it", "pt", "ru"]
    }

if __name__ == "__main__":
    host = os.getenv("HOST", "0.0.0.0")
    port = int(os.getenv("PORT", 6767))
    reload = os.getenv("RELOAD", "true").lower() == "true"
    
    uvicorn.run("main:app", host=host, port=port, reload=reload) 