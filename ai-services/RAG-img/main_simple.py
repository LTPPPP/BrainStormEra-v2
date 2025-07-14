from fastapi import FastAPI, File, UploadFile, HTTPException, status, Form
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware
import uvicorn
import os
import sys
from typing import Optional
import logging
from datetime import datetime
import uuid

# Add current directory to path
sys.path.append(os.path.dirname(os.path.abspath(__file__)))

# Simple response models
class SimpleUploadResponse:
    def __init__(self, success: bool, image_id: str, filename: str, extracted_text: str, message: str):
        self.success = success
        self.image_id = image_id
        self.filename = filename
        self.extracted_text = extracted_text
        self.message = message
        self.timestamp = datetime.now()

class SimpleQuestionResponse:
    def __init__(self, answer: str, confidence: float, relevant_images: list, sources: list):
        self.answer = answer
        self.confidence = confidence
        self.relevant_images = relevant_images
        self.sources = sources
        self.timestamp = datetime.now()

# Setup logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Initialize FastAPI app
app = FastAPI(
    title="RAG-img API (Simple Version)",
    description="Simple version for testing - OCR and vector search disabled",
    version="1.0.0-simple",
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

# Create upload directory
os.makedirs("uploads", exist_ok=True)

# Simple in-memory storage
uploaded_images = {}

@app.get("/")
async def root():
    return {
        "message": "RAG-img API (Simple Version)", 
        "version": "1.0.0-simple", 
        "docs": "/docs",
        "note": "This is a simplified version for testing. OCR and vector search are disabled."
    }

@app.post("/upload")
async def upload_image(file: UploadFile = File(...), description: Optional[str] = Form(None)):
    try:
        logger.info(f"Processing uploaded file: {file.filename}")
        
        # Generate unique ID
        image_id = str(uuid.uuid4())
        
        # Save file (basic)
        file_path = f"uploads/{image_id}_{file.filename}"
        content = await file.read()
        with open(file_path, "wb") as f:
            f.write(content)
        
        # Mock OCR result
        mock_text = f"Mock extracted text from {file.filename}. In a real system, this would be extracted using OCR."
        
        # Store metadata
        uploaded_images[image_id] = {
            "filename": file.filename,
            "file_path": file_path,
            "extracted_text": mock_text,
            "description": description,
            "upload_time": datetime.now().isoformat()
        }
        
        return {
            "success": True,
            "image_id": image_id,
            "filename": file.filename,
            "extracted_text": mock_text,
            "message": "Image uploaded successfully (mock OCR)",
            "timestamp": datetime.now().isoformat()
        }
        
    except Exception as e:
        logger.error(f"Error uploading image: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/question")
async def ask_question(request: dict):
    try:
        question = request.get("question", "")
        image_id = request.get("image_id")
        
        logger.info(f"Processing question: {question}")
        
        # Mock answer
        if image_id and image_id in uploaded_images:
            image_data = uploaded_images[image_id]
            answer = f"Based on the uploaded image '{image_data['filename']}', I found: {image_data['extracted_text']}. Note: This is a mock response."
            relevant_images = [image_id]
        else:
            # Search all images (mock)
            relevant_images = list(uploaded_images.keys())
            answer = f"Based on {len(relevant_images)} uploaded images, I found relevant content. Note: This is a mock response to '{question}'"
        
        return {
            "answer": answer,
            "confidence": 0.8,
            "relevant_images": relevant_images,
            "sources": [{"image_id": img_id, "filename": uploaded_images[img_id]["filename"]} for img_id in relevant_images],
            "timestamp": datetime.now().isoformat()
        }
        
    except Exception as e:
        logger.error(f"Error answering question: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/search")
async def search_images(request: dict):
    try:
        query = request.get("query", "")
        
        logger.info(f"Searching for: {query}")
        
        # Mock search results
        results = []
        for image_id, data in uploaded_images.items():
            if query.lower() in data["extracted_text"].lower() or query.lower() in data["filename"].lower():
                results.append({
                    "image_id": image_id,
                    "filename": data["filename"],
                    "similarity": 0.8,
                    "matched_text": data["extracted_text"][:100] + "...",
                    "ocr_confidence": 0.9,
                    "detected_language": "en",
                    "upload_timestamp": data["upload_time"]
                })
        
        return {
            "results": results,
            "total_results": len(results),
            "query": query,
            "timestamp": datetime.now().isoformat()
        }
        
    except Exception as e:
        logger.error(f"Error searching images: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/images")
async def list_images():
    try:
        return {
            "images": list(uploaded_images.values()),
            "total_count": len(uploaded_images)
        }
    except Exception as e:
        logger.error(f"Error listing images: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/images/{image_id}")
async def get_image_info(image_id: str):
    try:
        if image_id not in uploaded_images:
            raise HTTPException(status_code=404, detail="Image not found")
        return uploaded_images[image_id]
    except Exception as e:
        logger.error(f"Error getting image info: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.delete("/images/{image_id}")
async def delete_image(image_id: str):
    try:
        if image_id not in uploaded_images:
            raise HTTPException(status_code=404, detail="Image not found")
        
        # Delete file
        image_data = uploaded_images[image_id]
        if os.path.exists(image_data["file_path"]):
            os.remove(image_data["file_path"])
        
        # Remove from memory
        del uploaded_images[image_id]
        
        return {
            "success": True,
            "message": f"Image {image_id} deleted successfully"
        }
    except Exception as e:
        logger.error(f"Error deleting image: {str(e)}")
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/health")
async def health_check():
    return {
        "status": "healthy",
        "timestamp": datetime.now().isoformat(),
        "services": {
            "api": "healthy",
            "storage": "healthy",
            "ocr": "disabled (mock)",
            "embedding": "disabled (mock)",
            "vector_db": "disabled (mock)"
        },
        "version": "1.0.0-simple"
    }

if __name__ == "__main__":
    uvicorn.run("main_simple:app", host="0.0.0.0", port=8000, reload=True) 