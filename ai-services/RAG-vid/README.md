# RAG Video Service - Local Processing

A FastAPI service for processing YouTube videos locally and answering questions using Retrieval-Augmented Generation (RAG).

## 🚀 Features

### Video Processing
- **Local Video Processing**: Download videos and process them locally using Whisper
- **Transcript API Fallback**: Use YouTube's transcript API when local processing fails
- **Multiple Processing Modes**: Choose between local processing and transcript API
- **Multi-language Support**: Supports Vietnamese, English, and many other languages
- **High Accuracy**: Uses OpenAI's Whisper model for transcription

### RAG Q&A System
- **Intelligent Question Answering**: Ask questions about video content
- **Context-aware Responses**: Get relevant answers with source references
- **Multiple AI Models**: Support for HuggingFace models and Google Gemini
- **Semantic Search**: Find relevant content using vector embeddings

### Storage Management
- **Download Management**: Track and manage downloaded videos
- **Storage Monitoring**: Monitor disk usage and file sizes
- **Cleanup Tools**: Clean up downloaded files automatically

## 📦 Installation

### Prerequisites
- Python 3.8+
- FFmpeg (for video processing)
- At least 2GB free disk space for video downloads

### Install Dependencies
```bash
pip install -r requirements.txt
```

### Install FFmpeg
#### Windows
```bash
# Using chocolatey
choco install ffmpeg

# Or download from https://ffmpeg.org/download.html
```

#### macOS
```bash
brew install ffmpeg
```

#### Linux
```bash
sudo apt update
sudo apt install ffmpeg
```

## 🛠️ Setup

### Environment Variables
Create a `.env` file with the following variables:

```env
# API Configuration
HOST=0.0.0.0
PORT=6767
RELOAD=true

# CORS Configuration
ALLOWED_ORIGINS=*
ALLOWED_METHODS=*
ALLOWED_HEADERS=*

# AI Models
GEMINI_API_KEY=your_gemini_api_key_here

# Whisper Model (optional)
WHISPER_MODEL=base  # tiny, base, small, medium, large
```

### Initialize Data Directory
```bash
mkdir -p data downloads
```

## 🚀 Usage

### Start the Service
```bash
python main.py
```

The service will be available at `http://localhost:6767`

### API Documentation
Visit `http://localhost:6767/docs` for interactive API documentation.

## 📚 API Endpoints

### Video Processing

#### Check Video Availability
```http
POST /check-video-availability
```
Check if a video is available for processing.

#### Process Video (Local)
```http
POST /process-video
```
Process video using local download and Whisper transcription.

#### Process Video (Advanced)
```http
POST /process-video-advanced
```
Process video with advanced options:
- Processing mode (local/transcript_api)
- Language specification
- Whisper model selection

Example request:
```json
{
  "url": "https://youtube.com/watch?v=VIDEO_ID",
  "mode": "local",
  "language": "vi",
  "whisper_model": "base"
}
```

### Question Answering

#### Ask Question
```http
POST /ask-question
```
Ask a question about a processed video.

Example request:
```json
{
  "video_id": "VIDEO_ID",
  "question": "What is the main topic of this video?",
  "model_preference": "gemini"
}
```

### Management

#### List Videos
```http
GET /videos
```
List all processed videos.

#### Delete Video
```http
DELETE /videos/{video_id}
```
Delete a processed video and its files.

#### Storage Info
```http
GET /storage-info
```
Get storage information for downloaded videos.

#### Cleanup Files
```http
POST /cleanup
```
Clean up downloaded video files.

## 🔧 Processing Modes

### Local Processing
- **Pros**: Works with any video, high accuracy, supports multiple languages
- **Cons**: Requires more storage, takes longer, requires more computing power
- **Best for**: Videos without captions, non-English videos, maximum accuracy

### Transcript API
- **Pros**: Fast processing, no storage required, low computing requirements
- **Cons**: Only works with videos that have captions, limited language support
- **Best for**: English videos with existing captions, quick processing

## 📊 Whisper Models

| Model | Speed | Accuracy | Size |
|-------|-------|----------|------|
| tiny  | Fastest | Lowest | ~39MB |
| base  | Fast | Good | ~74MB |
| small | Medium | Better | ~244MB |
| medium | Slow | High | ~769MB |
| large | Very slow | Highest | ~1550MB |

## 🌍 Supported Languages

- Vietnamese (vi)
- English (en)
- Chinese (zh)
- Japanese (ja)
- Korean (ko)
- Spanish (es)
- French (fr)
- German (de)
- Italian (it)
- Portuguese (pt)
- Russian (ru)
- And many more...

## 📁 File Structure

```
RAG-vid/
├── main.py                 # FastAPI application
├── services/
│   ├── youtube_service.py  # Video processing service
│   ├── rag_service.py      # RAG Q&A service
│   └── video_processor_service.py  # Local video processing
├── models/
│   └── schemas.py          # Pydantic models
├── downloads/              # Downloaded videos (auto-created)
├── data/                   # Processed data (auto-created)
├── requirements.txt        # Dependencies
├── .env                    # Environment variables
└── README.md              # This file
```

## 🚨 Important Notes

### Storage Requirements
- Videos are temporarily downloaded during processing
- Audio files are extracted and then deleted
- Original video files can be cleaned up after processing
- Monitor disk space regularly

### Performance Considerations
- Local processing is CPU-intensive
- Whisper models require significant memory
- Consider using smaller models for faster processing
- Use transcript API for videos with existing captions

### Error Handling
- Automatic fallback to transcript API if local processing fails
- Comprehensive error messages for debugging
- Graceful handling of unavailable videos

## 🛡️ Security

- Input validation for all endpoints
- Rate limiting recommended for production
- File cleanup to prevent disk space issues
- Secure API key management

## 🔄 Updates from v1.0

### New Features
- Local video download and processing
- Whisper integration for transcription
- Advanced processing modes
- Storage management tools
- Cleanup utilities

### Breaking Changes
- Default processing mode changed to local
- New request/response formats for advanced features
- Additional dependencies required

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

For issues and questions:
1. Check the API documentation at `/docs`
2. Review the logs for error messages
3. Ensure all dependencies are properly installed
4. Verify FFmpeg is working correctly

## 🎯 Roadmap

- [ ] Support for more video platforms
- [ ] Batch processing capabilities
- [ ] Advanced video analysis features
- [ ] Real-time processing for live streams
- [ ] Multi-modal analysis (video + audio + text)
- [ ] Performance optimizations
- [ ] Docker containerization 
This project is licensed under the MIT License. 