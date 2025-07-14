import os
import asyncio
import logging
import tempfile
import shutil
from pathlib import Path
from typing import Optional, Dict, List, Tuple
import yt_dlp
import whisper
import ffmpeg
from pydub import AudioSegment
from urllib.parse import urlparse, parse_qs
import re

logger = logging.getLogger(__name__)

class VideoProcessorService:
    def __init__(self):
        self.downloads_dir = Path("downloads")
        self.downloads_dir.mkdir(exist_ok=True)
        
        # Initialize Whisper model
        self.whisper_model = None
        
        # Configure yt-dlp options with bot detection bypass
        self.ydl_opts = {
            # More flexible format selection
            'format': '(best[height<=720]/best[height<=480]/best[height<=360]/best)',
            'outtmpl': str(self.downloads_dir / '%(id)s.%(ext)s'),
            'noplaylist': True,
            'extractaudio': False,
            'audioformat': 'mp3',
            'audioquality': '192',
            'quiet': True,
            'no_warnings': True,
            # Add headers to bypass bot detection
            'http_headers': {
                'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36',
                'Accept': 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
                'Accept-Language': 'en-us,en;q=0.5',
                'Accept-Encoding': 'gzip, deflate',
                'DNT': '1',
                'Connection': 'keep-alive',
                'Upgrade-Insecure-Requests': '1',
            },
            # Additional options to bypass restrictions
            'extractor_retries': 3,
            'fragment_retries': 3,
            'retries': 3,
            'sleep_interval': 1,
            'max_sleep_interval': 5,
            'cookiefile': None,  # Can be set to a cookies file if needed
            # Handle age-restricted content
            'age_limit': 99,
            'skip_download': False,
            # Additional bypass options
            'writesubtitles': False,
            'writeautomaticsub': False,
            'ignoreerrors': False,
            'extract_flat': False,
            # Network options
            'socket_timeout': 30,
            # Format fallback options
            'format_sort': ['res:720', 'ext:mp4:m4a', 'res', 'fps'],
            'prefer_free_formats': True,
        }
        
        # Alternative format options to try if main format fails
        self.fallback_formats = [
            'best[height<=720]',
            'best[height<=480]', 
            'best[height<=360]',
            'worst[height>=240]',
            'best',
            'worst'
        ]
    
    async def initialize_whisper(self, model_name: str = "base"):
        """Initialize Whisper model for transcription"""
        try:
            logger.info(f"Loading Whisper model: {model_name}")
            loop = asyncio.get_event_loop()
            self.whisper_model = await loop.run_in_executor(
                None, whisper.load_model, model_name
            )
            logger.info("Whisper model loaded successfully")
        except Exception as e:
            logger.error(f"Error loading Whisper model: {e}")
            raise
    
    def extract_video_id(self, url: str) -> Optional[str]:
        """Extract video ID from YouTube URL"""
        try:
            patterns = [
                r'(?:youtube\.com/watch\?v=|youtu\.be/|youtube\.com/embed/)([^&\n?#]+)',
                r'youtube\.com/v/([^&\n?#]+)',
                r'youtube\.com/watch\?.*v=([^&\n?#]+)'
            ]
            
            for pattern in patterns:
                match = re.search(pattern, url)
                if match:
                    return match.group(1)
            
            parsed_url = urlparse(url)
            if parsed_url.hostname in ['www.youtube.com', 'youtube.com']:
                return parse_qs(parsed_url.query).get('v', [None])[0]
            elif parsed_url.hostname == 'youtu.be':
                return parsed_url.path.lstrip('/')
                
            return None
        except Exception as e:
            logger.error(f"Error extracting video ID from URL {url}: {e}")
            return None
    
    async def get_video_info(self, url: str) -> Dict:
        """Get video information without downloading"""
        try:
            video_id = self.extract_video_id(url)
            if not video_id:
                raise ValueError("Invalid YouTube URL")
            
            with yt_dlp.YoutubeDL({'quiet': True, 'no_warnings': True}) as ydl:
                loop = asyncio.get_event_loop()
                info = await loop.run_in_executor(
                    None, 
                    lambda: ydl.extract_info(url, download=False)
                )
                
                return {
                    'video_id': video_id,
                    'title': info.get('title', 'Unknown Title'),
                    'duration': info.get('duration', 0),
                    'upload_date': info.get('upload_date', ''),
                    'uploader': info.get('uploader', 'Unknown'),
                    'view_count': info.get('view_count', 0),
                    'description': info.get('description', ''),
                    'thumbnail': info.get('thumbnail', ''),
                    'filesize': info.get('filesize', 0)
                }
        except Exception as e:
            logger.error(f"Error getting video info: {e}")
            raise Exception(f"Failed to get video information: {str(e)}")
    
    async def download_video(self, url: str) -> Tuple[str, Dict]:
        """Download video and return file path and info"""
        try:
            video_id = self.extract_video_id(url)
            if not video_id:
                raise ValueError("Invalid YouTube URL")
            
            logger.info(f"Downloading video: {video_id}")
            
            # Try main format first
            success = False
            video_info = None
            
            # Get video info first (this usually works even if download fails)
            try:
                with yt_dlp.YoutubeDL({'quiet': True, 'no_warnings': True}) as ydl:
                    loop = asyncio.get_event_loop()
                    info = await loop.run_in_executor(
                        None, 
                        lambda: ydl.extract_info(url, download=False)
                    )
                    video_info = info
            except Exception as e:
                logger.warning(f"Failed to get video info: {e}")
            
            # Try downloading with main format
            try:
                with yt_dlp.YoutubeDL(self.ydl_opts) as ydl:
                    loop = asyncio.get_event_loop()
                    await loop.run_in_executor(
                        None, 
                        lambda: ydl.download([url])
                    )
                success = True
                logger.info(f"Successfully downloaded with main format")
            except Exception as e:
                logger.warning(f"Main format failed: {e}")
                
                # Try fallback formats
                for format_option in self.fallback_formats:
                    try:
                        logger.info(f"Trying fallback format: {format_option}")
                        fallback_opts = self.ydl_opts.copy()
                        fallback_opts['format'] = format_option
                        
                        with yt_dlp.YoutubeDL(fallback_opts) as ydl:
                            await loop.run_in_executor(
                                None, 
                                lambda: ydl.download([url])
                            )
                        success = True
                        logger.info(f"Successfully downloaded with format: {format_option}")
                        break
                    except Exception as fallback_error:
                        logger.debug(f"Fallback format {format_option} failed: {fallback_error}")
                        continue
            
            if not success:
                raise Exception("All download formats failed")
            
            # Find downloaded file
            video_files = list(self.downloads_dir.glob(f"{video_id}.*"))
            if not video_files:
                raise Exception("Video download failed - file not found")
            
            video_path = str(video_files[0])
            
            # Use video_info if we got it, otherwise create basic info
            if video_info:
                result_info = {
                    'video_id': video_id,
                    'title': video_info.get('title', 'Unknown Title'),
                    'duration': video_info.get('duration', 0),
                    'upload_date': video_info.get('upload_date', ''),
                    'uploader': video_info.get('uploader', 'Unknown'),
                    'view_count': video_info.get('view_count', 0),
                    'description': video_info.get('description', ''),
                    'file_path': video_path,
                    'file_size': os.path.getsize(video_path)
                }
            else:
                result_info = {
                    'video_id': video_id,
                    'title': 'Unknown Title',
                    'duration': 0,
                    'upload_date': '',
                    'uploader': 'Unknown',
                    'view_count': 0,
                    'description': '',
                    'file_path': video_path,
                    'file_size': os.path.getsize(video_path)
                }
            
            logger.info(f"Video downloaded successfully: {video_path}")
            return video_path, result_info
            
        except Exception as e:
            error_msg = str(e)
            logger.error(f"Error downloading video: {error_msg}")
            
            # Provide more specific error messages
            if "Sign in to confirm you're not a bot" in error_msg:
                raise Exception(f"YouTube bot detection triggered. This can happen due to: 1) Too many requests from your IP, 2) YouTube's anti-bot measures, 3) Geographic restrictions. Try again later or use the transcript API instead.")
            elif "Private video" in error_msg:
                raise Exception("This video is private and cannot be downloaded.")
            elif "Video unavailable" in error_msg:
                raise Exception("This video is unavailable (may be deleted, private, or region-restricted).")
            elif "age-restricted" in error_msg.lower():
                raise Exception("This video is age-restricted and cannot be downloaded without authentication.")
            elif "Requested format is not available" in error_msg:
                raise Exception("No compatible video format available for download. This video may have restrictions or unusual encoding.")
            elif "All download formats failed" in error_msg:
                raise Exception("Could not download video in any available format. Video may be restricted or have compatibility issues.")
            else:
                raise Exception(f"Failed to download video: {error_msg}")
    
    async def extract_audio(self, video_path: str) -> str:
        """Extract audio from video file"""
        try:
            video_path = Path(video_path)
            audio_path = video_path.parent / f"{video_path.stem}.mp3"
            
            logger.info(f"Extracting audio from: {video_path}")
            
            # Use ffmpeg to extract audio
            loop = asyncio.get_event_loop()
            await loop.run_in_executor(
                None,
                lambda: (
                    ffmpeg
                    .input(str(video_path))
                    .output(str(audio_path), acodec='mp3', audio_bitrate='192k')
                    .overwrite_output()
                    .run(quiet=True)
                )
            )
            
            logger.info(f"Audio extracted successfully: {audio_path}")
            return str(audio_path)
            
        except Exception as e:
            logger.error(f"Error extracting audio: {e}")
            raise Exception(f"Failed to extract audio: {str(e)}")
    
    async def transcribe_audio(self, audio_path: str, language: str = None) -> Dict:
        """Transcribe audio using Whisper"""
        try:
            if not self.whisper_model:
                await self.initialize_whisper()
            
            logger.info(f"Transcribing audio: {audio_path}")
            
            # Transcribe audio
            loop = asyncio.get_event_loop()
            result = await loop.run_in_executor(
                None,
                lambda: self.whisper_model.transcribe(
                    audio_path,
                    language=language,
                    word_timestamps=True
                )
            )
            
            # Format transcript
            transcript_text = result['text']
            segments = result.get('segments', [])
            
            # Create timestamped segments
            timestamped_segments = []
            for segment in segments:
                timestamped_segments.append({
                    'start': segment['start'],
                    'end': segment['end'],
                    'text': segment['text'].strip(),
                    'words': segment.get('words', [])
                })
            
            transcript_data = {
                'text': transcript_text,
                'language': result.get('language', 'unknown'),
                'segments': timestamped_segments,
                'duration': segments[-1]['end'] if segments else 0
            }
            
            logger.info(f"Transcription completed. Language: {transcript_data['language']}, Duration: {transcript_data['duration']:.2f}s")
            
            return transcript_data
            
        except Exception as e:
            logger.error(f"Error transcribing audio: {e}")
            raise Exception(f"Failed to transcribe audio: {str(e)}")
    
    async def process_video_full(self, url: str, language: str = None) -> Dict:
        """Download video, extract audio, and transcribe - full pipeline"""
        try:
            logger.info(f"Starting full video processing for: {url}")
            
            # Download video
            video_path, video_info = await self.download_video(url)
            
            # Extract audio
            audio_path = await self.extract_audio(video_path)
            
            # Transcribe audio
            transcript_data = await self.transcribe_audio(audio_path, language)
            
            # Clean up audio file (optional)
            try:
                os.remove(audio_path)
                logger.info(f"Cleaned up audio file: {audio_path}")
            except:
                pass
            
            # Combine all data
            result = {
                'video_info': video_info,
                'transcript': transcript_data,
                'processing_completed': True
            }
            
            logger.info(f"Full video processing completed for: {video_info['video_id']}")
            return result
            
        except Exception as e:
            logger.error(f"Error in full video processing: {e}")
            raise Exception(f"Failed to process video: {str(e)}")
    
    async def cleanup_video(self, video_path: str):
        """Clean up downloaded video file"""
        try:
            if os.path.exists(video_path):
                os.remove(video_path)
                logger.info(f"Cleaned up video file: {video_path}")
        except Exception as e:
            logger.error(f"Error cleaning up video file: {e}")
    
    async def get_storage_info(self) -> Dict:
        """Get storage information for downloads directory"""
        try:
            total_size = 0
            file_count = 0
            
            for file_path in self.downloads_dir.rglob('*'):
                if file_path.is_file():
                    total_size += file_path.stat().st_size
                    file_count += 1
            
            return {
                'downloads_dir': str(self.downloads_dir),
                'total_files': file_count,
                'total_size_bytes': total_size,
                'total_size_mb': total_size / (1024 * 1024)
            }
        except Exception as e:
            logger.error(f"Error getting storage info: {e}")
            return {
                'downloads_dir': str(self.downloads_dir),
                'total_files': 0,
                'total_size_bytes': 0,
                'total_size_mb': 0
            }
    
    async def cleanup_all_downloads(self):
        """Clean up all downloaded files"""
        try:
            if self.downloads_dir.exists():
                shutil.rmtree(self.downloads_dir)
                self.downloads_dir.mkdir(exist_ok=True)
                logger.info("All downloads cleaned up")
        except Exception as e:
            logger.error(f"Error cleaning up downloads: {e}")
            raise Exception(f"Failed to cleanup downloads: {str(e)}") 