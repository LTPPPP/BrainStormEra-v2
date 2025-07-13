# RAG-vid Quick Start Guide

## 🚀 Quick Test (5 minutes)

### 1. Install Dependencies
```bash
pip install -r requirements.txt
```

### 2. Test the Service
```bash
python quick_test.py
```

This will test the improved fallback mechanism with different video types.

### 3. Run the API Server
```bash
python main.py
```

### 4. Test with Browser
Go to `http://localhost:6767/docs` and try the new `/test-processing` endpoint.

## 🔧 What's New

### ✅ Improved Features
- **Automatic Fallback**: Local processing → Transcript API fallback
- **Better Error Handling**: Clear error messages for different failure types
- **Bot Detection Bypass**: Improved yt-dlp configuration
- **Test Endpoints**: `/test-processing` to debug issues

### 🛠️ Key Endpoints

1. **Process Video** (with fallback):
   ```
   POST /process-video
   {"url": "https://youtube.com/watch?v=VIDEO_ID"}
   ```

2. **Test Both Methods**:
   ```
   POST /test-processing
   {"url": "https://youtube.com/watch?v=VIDEO_ID"}
   ```

3. **Advanced Processing**:
   ```
   POST /process-video-advanced
   {
     "url": "https://youtube.com/watch?v=VIDEO_ID",
     "mode": "local",
     "language": "vi"
   }
   ```

## 🐛 Troubleshooting

### Bot Detection Error
```
YouTube bot detection triggered...
```
**Solution**: The service will automatically fallback to transcript API.

### No Transcript Available
```
No transcript available from API
```
**Solution**: Video has no captions. Try a different video or wait for local processing to work.

### FFmpeg Not Found
```
ffmpeg not found
```
**Solution**: Install FFmpeg:
- Windows: `choco install ffmpeg`
- macOS: `brew install ffmpeg`
- Linux: `sudo apt install ffmpeg`

## 📊 Processing Methods

| Method | Works With | Speed | Accuracy |
|--------|------------|-------|----------|
| Local Processing | Any video | Slow | High |
| Transcript API | Videos with captions | Fast | Medium |
| **Auto Fallback** | **Most videos** | **Smart** | **Best** |

## 🎯 Current Status

The service now handles YouTube's bot detection much better:

1. **Tries local processing first** (downloads video)
2. **Falls back to transcript API** if blocked
3. **Provides clear error messages** for debugging
4. **Cleans up files automatically**

## 🔍 Test Results

Use the `/test-processing` endpoint to see both methods side-by-side:

```json
{
  "video_info": {...},
  "local_processing": {
    "success": false,
    "error": "YouTube bot detection triggered..."
  },
  "transcript_api": {
    "success": true,
    "transcript_length": 1234
  }
}
```

## 📞 Support

If you encounter issues:
1. Check the logs for specific error messages
2. Try the `/test-processing` endpoint
3. Verify FFmpeg is installed correctly
4. Consider using transcript API mode for quick testing 