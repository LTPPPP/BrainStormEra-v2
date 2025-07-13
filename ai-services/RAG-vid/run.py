#!/usr/bin/env python3
"""
Simple run script for the RAG Video Service
"""

import os
import sys
import subprocess
import logging
from pathlib import Path

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

def check_dependencies():
    """Check if required dependencies are installed"""
    try:
        import fastapi
        import uvicorn
        import sentence_transformers
        import youtube_transcript_api
        import faiss
        logger.info("✅ All dependencies are installed")
        return True
    except ImportError as e:
        logger.error(f"❌ Missing dependency: {e}")
        logger.info("Please install dependencies with: pip install -r requirements.txt")
        return False

def create_data_directory():
    """Create data directory if it doesn't exist"""
    data_dir = Path("data")
    data_dir.mkdir(exist_ok=True)
    logger.info(f"📁 Data directory ready: {data_dir.absolute()}")

def check_optional_configs():
    """Check for optional configurations"""
    gemini_key = os.getenv("GEMINI_API_KEY")
    if gemini_key:
        logger.info("🔑 Gemini API key found - Gemini model will be available")
    else:
        logger.info("ℹ️  No Gemini API key found - Only HuggingFace models will be available")
        logger.info("   Set GEMINI_API_KEY environment variable to use Gemini models")

def main():
    """Main function to start the service"""
    print("🚀 Starting RAG Video Service")
    print("=" * 50)
    
    # Check dependencies
    if not check_dependencies():
        sys.exit(1)
    
    # Create data directory
    create_data_directory()
    
    # Check optional configurations
    check_optional_configs()
    
    # Set environment variables
    os.environ.setdefault("HOST", "0.0.0.0")
    os.environ.setdefault("PORT", "6767")
    
    host = os.getenv("HOST", "0.0.0.0")
    port = int(os.getenv("PORT", 6767))
    
    print(f"🌐 Service will be available at: http://{host}:{port}")
    print(f"📖 API Documentation: http://{host}:{port}/docs")
    print(f"🔍 Interactive API: http://{host}:{port}/redoc")
    print("=" * 50)
    
    # Import and run the app
    try:
        from main import app
        import uvicorn
        
        logger.info("Starting FastAPI server...")
        uvicorn.run(
            "main:app",
            host=host,
            port=port,
            reload=True,
            log_level="info"
        )
    except KeyboardInterrupt:
        logger.info("🛑 Service stopped by user")
    except Exception as e:
        logger.error(f"❌ Error starting service: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main() 