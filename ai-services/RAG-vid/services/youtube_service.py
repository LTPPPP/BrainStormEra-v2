import re
import asyncio
import logging
from typing import Optional, Dict, List
from youtube_transcript_api import YouTubeTranscriptApi
from youtube_transcript_api.formatters import TextFormatter
from youtube_transcript_api._errors import (
    TranscriptsDisabled, 
    NoTranscriptFound, 
    VideoUnavailable,
    NotTranslatable,
    TranslationLanguageNotAvailable
)
import yt_dlp
from urllib.parse import urlparse, parse_qs
from .video_processor_service import VideoProcessorService

logger = logging.getLogger(__name__)

class YouTubeService:
    def __init__(self):
        self.formatter = TextFormatter()
        self.video_processor = VideoProcessorService()
        
    async def initialize(self):
        """Initialize the YouTube service and video processor"""
        try:
            await self.video_processor.initialize_whisper()
            logger.info("YouTube service initialized successfully")
        except Exception as e:
            logger.error(f"Error initializing YouTube service: {e}")
            raise
        
    def extract_video_id(self, url: str) -> Optional[str]:
        """
        Extract video ID from YouTube URL
        """
        return self.video_processor.extract_video_id(url)
    
    async def get_video_info(self, url: str) -> Dict:
        """
        Get video information using video processor
        """
        return await self.video_processor.get_video_info(url)
    
    async def check_video_availability(self, url: str) -> Dict:
        """
        Check if video is available for processing
        """
        try:
            video_info = await self.get_video_info(url)
            return {
                'available': True,
                'video_info': video_info,
                'message': 'Video is available for processing'
            }
        except Exception as e:
            return {
                'available': False,
                'video_info': None,
                'message': f'Video is not available: {str(e)}'
            }
    
    async def get_transcript(self, url: str, languages: List[str] = None, use_local_processing: bool = True) -> Optional[str]:
        """
        Get transcript - can use local processing or fallback to transcript API
        """
        if use_local_processing:
            try:
                # Use local video processing
                logger.info(f"Attempting local video processing for: {url}")
                result = await self.video_processor.process_video_full(url)
                transcript_data = result['transcript']
                logger.info(f"Local processing successful for: {url}")
                return transcript_data['text']
            except Exception as e:
                logger.warning(f"Local processing failed for {url}: {e}")
                
                # Check if it's a bot detection error
                if "Sign in to confirm you're not a bot" in str(e) or "bot" in str(e).lower():
                    logger.info(f"Bot detection encountered, falling back to transcript API for: {url}")
                else:
                    logger.info(f"Local processing error, falling back to transcript API for: {url}")
                
                # Fallback to transcript API
                try:
                    return await self._get_transcript_from_api(url, languages)
                except Exception as api_error:
                    logger.error(f"Both local and API processing failed for {url}. Local error: {e}, API error: {api_error}")
                    raise Exception(f"Failed to get transcript using both methods. Local processing failed due to: {str(e)}, API processing failed due to: {str(api_error)}")
        else:
            # Use transcript API directly
            return await self._get_transcript_from_api(url, languages)
    
    async def _try_multiple_transcript_languages(self, transcript_list, video_id: str) -> Optional[object]:
        """Try to get transcript in multiple languages with systematic approach"""
        
        # Priority order for languages
        language_priorities = [
            # Vietnamese variants
            ['vi', 'vi-VN'],
            # English variants
            ['en', 'en-US', 'en-GB', 'en-CA', 'en-AU'],
            # Other common languages
            ['zh', 'zh-CN', 'zh-TW'],
            ['ja', 'ja-JP'],
            ['ko', 'ko-KR'],
            ['es', 'es-ES'],
            ['fr', 'fr-FR'],
            ['de', 'de-DE'],
            ['it', 'it-IT'],
            ['pt', 'pt-BR', 'pt-PT'],
            ['ru', 'ru-RU'],
        ]
        
        # First try specific language groups
        for lang_group in language_priorities:
            try:
                transcript = transcript_list.find_transcript(lang_group)
                logger.info(f"Found transcript in language group: {lang_group} -> {transcript.language}")
                return transcript
            except NoTranscriptFound:
                continue
            except Exception as e:
                logger.debug(f"Error with language group {lang_group}: {e}")
                continue
        
        # If no specific language worked, try any available transcript
        try:
            available_transcripts = list(transcript_list)
            if available_transcripts:
                transcript = available_transcripts[0]
                logger.info(f"Using first available transcript: {transcript.language}")
                return transcript
            else:
                logger.warning(f"No transcripts available for video {video_id}")
                return None
        except Exception as e:
            logger.error(f"Error getting available transcripts: {e}")
            return None

    async def _get_transcript_from_api(self, url: str, languages: List[str] = None) -> Optional[str]:
        """
        Get transcript from YouTube API (fallback method)
        """
        try:
            video_id = self.extract_video_id(url)
            if not video_id:
                raise ValueError("Invalid YouTube URL")
            
            if languages is None:
                languages = ['vi', 'en', 'en-US', 'en-GB']
            
            logger.info(f"Attempting to get transcript for video ID: {video_id}")
            
            # Run in thread pool to avoid blocking
            loop = asyncio.get_event_loop()
            
            # First, try to get the transcript list
            try:
                transcript_list = await loop.run_in_executor(
                    None,
                    lambda: YouTubeTranscriptApi.list_transcripts(video_id)
                )
            except TranscriptsDisabled:
                logger.warning(f"Transcripts are disabled for video {video_id}")
                return None
            except VideoUnavailable:
                logger.warning(f"Video {video_id} is unavailable")
                return None
            except Exception as e:
                logger.error(f"Error getting transcript list for video {video_id}: {e}")
                return None
            
            transcript = None
            
            # Use systematic language selection
            transcript = await self._try_multiple_transcript_languages(transcript_list, video_id)
            
            if not transcript:
                logger.warning(f"No suitable transcript found for video {video_id}")
                return None
            
            # Fetch transcript data with improved error handling
            try:
                transcript_data = await loop.run_in_executor(
                    None,
                    transcript.fetch
                )
                
                # Check if transcript_data is valid
                if not transcript_data:
                    logger.warning(f"Empty transcript data for video {video_id}")
                    return None
                
                if not isinstance(transcript_data, list):
                    logger.warning(f"Invalid transcript data type for video {video_id}: {type(transcript_data)}")
                    return None
                
                logger.info(f"Successfully fetched {len(transcript_data)} transcript segments")
                
            except Exception as e:
                error_msg = str(e)
                logger.error(f"Error fetching transcript data: {error_msg}")
                
                # Handle specific XML parsing errors
                if "no element found" in error_msg:
                    logger.warning(f"XML parsing error for video {video_id} - transcript may be corrupted or empty")
                    return None
                elif "not well-formed" in error_msg:
                    logger.warning(f"Malformed XML in transcript for video {video_id}")
                    return None
                else:
                    logger.warning(f"Unexpected error fetching transcript: {error_msg}")
                    return None
            
            # Format transcript with additional validation
            try:
                if not transcript_data:
                    logger.warning("No transcript data to format")
                    return None
                
                formatted_transcript = self.formatter.format_transcript(transcript_data)
                
                if not formatted_transcript or len(formatted_transcript.strip()) == 0:
                    logger.warning(f"Empty formatted transcript for video {video_id}")
                    return None
                
                logger.info(f"Successfully formatted transcript, length: {len(formatted_transcript)}")
                return formatted_transcript
                
            except Exception as e:
                logger.error(f"Error formatting transcript: {e}")
                return None
            
        except Exception as e:
            logger.error(f"Unexpected error getting transcript for video {url}: {e}")
            return None
    
    async def get_transcript_with_timestamps(self, url: str, languages: List[str] = None, use_local_processing: bool = True) -> Optional[List[Dict]]:
        """
        Get transcript with timestamps
        """
        if use_local_processing:
            try:
                # Use local video processing
                result = await self.video_processor.process_video_full(url)
                transcript_data = result['transcript']
                return transcript_data['segments']
            except Exception as e:
                logger.error(f"Local processing failed: {e}, falling back to transcript API")
                # Fallback to transcript API
                return await self._get_transcript_with_timestamps_from_api(url, languages)
        else:
            # Use transcript API directly
            return await self._get_transcript_with_timestamps_from_api(url, languages)
    
    async def _get_transcript_with_timestamps_from_api(self, url: str, languages: List[str] = None) -> Optional[List[Dict]]:
        """
        Get transcript with timestamps from YouTube API (fallback method)
        """
        try:
            video_id = self.extract_video_id(url)
            if not video_id:
                raise ValueError("Invalid YouTube URL")
            
            if languages is None:
                languages = ['vi', 'en', 'en-US', 'en-GB']
            
            # Run in thread pool to avoid blocking
            loop = asyncio.get_event_loop()
            
            try:
                transcript_list = await loop.run_in_executor(
                    None,
                    lambda: YouTubeTranscriptApi.list_transcripts(video_id)
                )
            except TranscriptsDisabled:
                logger.warning(f"Transcripts are disabled for video {video_id}")
                return None
            except VideoUnavailable:
                logger.warning(f"Video {video_id} is unavailable")
                return None
            except Exception as e:
                logger.error(f"Error getting transcript list for video {video_id}: {e}")
                return None
            
            transcript = None
            
            # Try to get transcript in preferred languages
            for lang in languages:
                try:
                    transcript = transcript_list.find_transcript([lang])
                    break
                except NoTranscriptFound:
                    continue
                except Exception:
                    continue
            
            # If no transcript found in preferred languages, try any available
            if not transcript:
                try:
                    transcript = transcript_list.find_transcript(['en'])
                except NoTranscriptFound:
                    # Try to get any available transcript
                    available_transcripts = list(transcript_list)
                    if available_transcripts:
                        transcript = available_transcripts[0]
                    else:
                        return None
                except Exception:
                    return None
            
            # Fetch transcript data
            try:
                transcript_data = await loop.run_in_executor(
                    None,
                    transcript.fetch
                )
            except Exception as e:
                logger.error(f"Error fetching transcript data: {e}")
                return None
            
            # Format transcript with timestamps
            try:
                formatted_segments = []
                for segment in transcript_data:
                    formatted_segments.append({
                        'start': segment['start'],
                        'end': segment['start'] + segment['duration'],
                        'text': segment['text'],
                        'duration': segment['duration']
                    })
                
                return formatted_segments
            except Exception as e:
                logger.error(f"Error formatting timestamped transcript: {e}")
                return None
            
        except Exception as e:
            logger.error(f"Unexpected error getting timestamped transcript: {e}")
            return None
    
    def _seconds_to_timestamp(self, seconds: float) -> str:
        """Convert seconds to HH:MM:SS format"""
        hours = int(seconds // 3600)
        minutes = int((seconds % 3600) // 60)
        seconds = int(seconds % 60)
        return f"{hours:02d}:{minutes:02d}:{seconds:02d}"
    
    async def process_video_locally(self, url: str, language: str = None) -> Dict:
        """
        Process video using local download and transcription
        """
        try:
            logger.info(f"Processing video locally: {url}")
            
            result = await self.video_processor.process_video_full(url, language)
            
            # Format result for compatibility
            video_info = result['video_info']
            transcript_data = result['transcript']
            
            return {
                'video_id': video_info['video_id'],
                'title': video_info['title'],
                'duration': video_info['duration'],
                'transcript': transcript_data['text'],
                'transcript_segments': transcript_data['segments'],
                'language': transcript_data['language'],
                'processing_method': 'local_whisper',
                'file_path': video_info['file_path'],
                'file_size': video_info['file_size']
            }
            
        except Exception as e:
            logger.error(f"Error processing video locally: {e}")
            raise Exception(f"Failed to process video locally: {str(e)}")
    
    async def cleanup_video_files(self, video_id: str):
        """Clean up downloaded video files for a specific video"""
        try:
            video_files = list(self.video_processor.downloads_dir.glob(f"{video_id}.*"))
            for file_path in video_files:
                await self.video_processor.cleanup_video(str(file_path))
            logger.info(f"Cleaned up files for video: {video_id}")
        except Exception as e:
            logger.error(f"Error cleaning up files for video {video_id}: {e}")
    
    async def get_storage_info(self) -> Dict:
        """Get storage information"""
        return await self.video_processor.get_storage_info()
    
    async def cleanup_all_downloads(self):
        """Clean up all downloaded files"""
        await self.video_processor.cleanup_all_downloads() 