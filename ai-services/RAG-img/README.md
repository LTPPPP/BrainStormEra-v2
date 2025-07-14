# RAG-img - Image Text Extraction and Question Answering System

A free, open-source RAG (Retrieval-Augmented Generation) system for processing images, extracting text using OCR, and answering questions about image content.

## Features

- 🖼️ **Image Upload & Processing**: Support for multiple image formats (JPG, PNG, BMP, GIF, TIFF, WebP)
- 🔍 **OCR Text Extraction**: Using EasyOCR for Vietnamese and English text recognition
- 🧠 **Text Embedding**: Using Sentence Transformers for semantic text representation
- 🔎 **Vector Search**: ChromaDB for fast similarity search
- ❓ **Question Answering**: Ask questions about image content in natural language
- 🌐 **REST API**: Full-featured FastAPI with automatic documentation
- 🐳 **Docker Support**: Easy deployment with Docker and Docker Compose

## Technology Stack

- **Backend**: FastAPI (Python)
- **OCR**: EasyOCR (free, supports Vietnamese & English)
- **Embeddings**: Sentence Transformers (free, lightweight)
- **Vector Database**: ChromaDB (free, embedded)
- **Image Processing**: OpenCV, PIL
- **Deployment**: Docker, Docker Compose

## Quick Start

### Option 1: Using Docker Compose (Recommended)

1. **Clone or download the project**
2. **Navigate to the RAG-img directory**
3. **Run with Docker Compose:**
   ```bash
   docker-compose up --build
   ```
4. **Access the API:**
   - API: http://localhost:8000
   - Documentation: http://localhost:8000/docs
   - Alternative docs: http://localhost:8000/redoc

### Option 2: Manual Setup

1. **Install dependencies:**
   ```bash
   pip install -r requirements.txt
   ```

2. **Install system dependencies (Ubuntu/Debian):**
   ```bash
   sudo apt-get update
   sudo apt-get install tesseract-ocr tesseract-ocr-vie tesseract-ocr-eng
   ```

3. **Run the application:**
   ```bash
   python main.py
   ```

## API Endpoints

### 1. Upload Image
```bash
POST /upload
```
Upload and process an image with OCR text extraction.

**Example:**
```bash
curl -X POST "http://localhost:8000/upload" \
  -F "file=@your_image.jpg" \
  -F "description=Optional description"
```

### 2. Ask Question
```bash
POST /question
```
Ask questions about uploaded images.

**Example:**
```bash
curl -X POST "http://localhost:8000/question" \
  -H "Content-Type: application/json" \
  -d '{
    "question": "What text is in the image?",
    "image_id": "optional-specific-image-id",
    "top_k": 5
  }'
```

### 3. Search Images
```bash
POST /search
```
Search for images based on text content.

**Example:**
```bash
curl -X POST "http://localhost:8000/search" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "invoice",
    "top_k": 10,
    "threshold": 0.5
  }'
```

### 4. List Images
```bash
GET /images
```
Get list of all uploaded images.

### 5. Get Image Info
```bash
GET /images/{image_id}
```
Get detailed information about a specific image.

### 6. Delete Image
```bash
DELETE /images/{image_id}
```
Delete an image and its associated data.

### 7. Health Check
```bash
GET /health
```
Check the health status of all services.

## Configuration

The system uses `config.json` for configuration. Key settings:

```json
{
  "upload_dir": "uploads",
  "max_file_size": 10485760,
  "supported_formats": [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp"],
  "ocr_languages": ["vi", "en"],
  "embedding_model": "sentence-transformers/all-MiniLM-L6-v2",
  "vector_db_path": "chroma_db",
  "chunk_size": 500,
  "chunk_overlap": 50,
  "similarity_threshold": 0.7,
  "max_results": 10
}
```

## Usage Examples

### Python Client Example

```python
import requests
import json

# Upload image
with open('image.jpg', 'rb') as f:
    response = requests.post('http://localhost:8000/upload', 
                           files={'file': f},
                           data={'description': 'My document'})
    result = response.json()
    image_id = result['image_id']

# Ask question
question_data = {
    "question": "What is the total amount?",
    "image_id": image_id,
    "top_k": 5
}
response = requests.post('http://localhost:8000/question', 
                        json=question_data)
answer = response.json()
print(f"Answer: {answer['answer']}")
```

### JavaScript Client Example

```javascript
// Upload image
const formData = new FormData();
formData.append('file', fileInput.files[0]);
formData.append('description', 'My document');

fetch('http://localhost:8000/upload', {
    method: 'POST',
    body: formData
})
.then(response => response.json())
.then(data => {
    console.log('Image uploaded:', data);
    
    // Ask question
    return fetch('http://localhost:8000/question', {
        method: 'POST',
        headers: {'Content-Type': 'application/json'},
        body: JSON.stringify({
            question: 'What text is in this image?',
            image_id: data.image_id,
            top_k: 5
        })
    });
})
.then(response => response.json())
.then(answer => {
    console.log('Answer:', answer.answer);
});
```

## Supported Image Formats

- JPEG (.jpg, .jpeg)
- PNG (.png)
- BMP (.bmp)
- GIF (.gif)
- TIFF (.tiff)
- WebP (.webp)

## Language Support

The OCR system supports:
- **Vietnamese** (vi)
- **English** (en)

## Performance Notes

- **First run**: May take longer due to model downloads
- **OCR processing**: Depends on image size and text complexity
- **Memory usage**: Approximately 2-4GB RAM for optimal performance
- **Storage**: Vector database grows with number of processed images

## Troubleshooting

### Common Issues

1. **OCR not working**: Ensure Tesseract is installed
2. **Out of memory**: Reduce image size or increase system memory
3. **Slow performance**: Use GPU acceleration if available
4. **Dependencies error**: Check Python version (3.8+ required)

### Health Check

Visit `/health` endpoint to verify all services are running correctly.

## Development

### Project Structure

```
RAG-img/
├── main.py                 # FastAPI application
├── models/
│   └── schemas.py         # Pydantic models
├── services/
│   ├── ocr_service.py     # OCR functionality
│   ├── embedding_service.py # Text embedding
│   ├── vector_db_service.py # Vector database
│   └── rag_service.py     # Main RAG service
├── utils/
│   └── utils.py           # Utility functions
├── requirements.txt       # Python dependencies
├── Dockerfile            # Docker configuration
├── docker-compose.yml    # Docker Compose setup
└── config.json          # Configuration file
```

### Adding New Features

1. **New OCR language**: Add to `ocr_languages` in config
2. **New embedding model**: Change `embedding_model` in config
3. **Custom preprocessing**: Modify `ImageUtils` in utils
4. **New endpoints**: Add to `main.py`

## License

This project is free and open-source. Feel free to use, modify, and distribute.

## Support

For questions or issues:
1. Check the `/health` endpoint
2. Review the logs for error messages
3. Ensure all dependencies are installed correctly

## Contributing

Contributions are welcome! Please feel free to submit issues, feature requests, or pull requests. 