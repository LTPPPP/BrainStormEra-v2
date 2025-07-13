from fastapi import FastAPI, File, UploadFile, HTTPException, status, Form
from fastapi.responses import JSONResponse, FileResponse
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
import uvicorn
import os
import sys
from typing import Optional
import logging

# Add current directory to path
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

from models.schemas import (
    ImageUploadResponse, QuestionRequest, QuestionResponse, 
    SearchRequest, SearchResponse, SearchResult, HealthCheckResponse, ErrorResponse
)
from services.rag_service import RAGService
from utils.utils import ConfigUtils

# Setup logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Load configuration
config = ConfigUtils.load_config()

# Initialize FastAPI app
app = FastAPI(
    title="RAG-img API",
    description="RAG system for image text extraction and question answering",
    version="1.0.0",
    docs_url="/docs",
    redoc_url="/redoc"
)

# CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Initialize RAG service
rag_service = RAGService(
    upload_dir=config.get("upload_dir", "uploads"),
    db_path=config.get("vector_db_path", "chroma_db")
)

# Mount static files
if os.path.exists(config.get("upload_dir", "uploads")):
    app.mount("/uploads", StaticFiles(directory=config.get("upload_dir", "uploads")), name="uploads")


@app.get("/")
async def root():
    return {"message": "RAG-img API", "version": "1.0.0", "docs": "/docs"}


@app.post("/upload", response_model=ImageUploadResponse)
async def upload_image(file: UploadFile = File(...), description: Optional[str] = Form(None)):
    try:
        result = await rag_service.process_image(file, description)
        
        if not result.success:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail=result.error_message or "Failed to process image"
            )
        
        return ImageUploadResponse(
            success=True,
            image_id=result.image_id,
            filename=result.filename,
            extracted_text=result.extracted_text,
            message="Image uploaded successfully"
        )
    except Exception as e:
        logger.error(f"Upload error: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/question", response_model=QuestionResponse)
async def ask_question(request: QuestionRequest):
    try:
        result = await rag_service.answer_question(
            question=request.question,
            image_id=request.image_id,
            top_k=request.top_k
        )
        
        if not result.success:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail=result.error_message or "Failed to answer question"
            )
        
        return QuestionResponse(
            answer=result.answer,
            confidence=result.confidence,
            relevant_images=result.relevant_images,
            sources=result.sources
        )
    except Exception as e:
        logger.error(f"Question error: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.post("/search", response_model=SearchResponse)
async def search_images(request: SearchRequest):
    try:
        result = await rag_service.search_images(
            query=request.query,
            top_k=request.top_k,
            similarity_threshold=request.threshold
        )
        
        if not result["success"]:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail=result.get("error", "Search failed")
            )
        
        # Convert results to match SearchResult schema
        search_results = []
        for res in result["results"]:
            search_results.append(SearchResult(
                image_id=res["image_id"],
                filename=res["filename"],
                similarity=res["similarity"],
                matched_text=res["matched_text"],
                ocr_confidence=res.get("ocr_confidence", 0.0),
                detected_language=res.get("detected_language", ""),
                upload_timestamp=res.get("upload_timestamp", "")
            ))
        
        return SearchResponse(
            results=search_results,
            total_results=result["total_results"],
            query=request.query
        )
    except Exception as e:
        logger.error(f"Search error: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.get("/images")
async def list_images():
    try:
        images = await rag_service.list_images()
        return {"images": images, "total_count": len(images)}
    except Exception as e:
        logger.error(f"List images error: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.get("/images/{image_id}")
async def get_image_info(image_id: str):
    try:
        image_info = await rag_service.get_image_info(image_id)
        if not image_info:
            raise HTTPException(status_code=404, detail="Image not found")
        return image_info
    except Exception as e:
        logger.error(f"Get image info error: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.delete("/images/{image_id}")
async def delete_image(image_id: str):
    try:
        result = await rag_service.delete_image(image_id)
        if not result["success"]:
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail=result.get("error", "Failed to delete image")
            )
        return {"success": True, "message": result["message"]}
    except Exception as e:
        logger.error(f"Delete image error: {e}")
        raise HTTPException(status_code=500, detail=str(e))


@app.get("/health", response_model=HealthCheckResponse)
async def health_check():
    try:
        health_result = await rag_service.health_check()
        return HealthCheckResponse(
            status=health_result["status"],
            services={
                "ocr": health_result["services"]["ocr"]["status"],
                "embedding": health_result["services"]["embedding"]["status"],
                "vector_db": health_result["services"]["vector_db"]["status"]
            },
            version="1.0.0"
        )
    except Exception as e:
        logger.error(f"Health check error: {e}")
        return HealthCheckResponse(status="unhealthy", services={"error": str(e)}, version="1.0.0")


@app.exception_handler(HTTPException)
async def http_exception_handler(request, exc):
    return JSONResponse(
        status_code=exc.status_code,
        content={"error": exc.detail, "detail": str(exc.detail)}
    )


if __name__ == "__main__":
    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True) 