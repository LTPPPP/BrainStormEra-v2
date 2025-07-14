import asyncio
import logging
import os
import json
import pickle
from typing import Dict, List, Optional, Any
from datetime import datetime
import numpy as np
from sentence_transformers import SentenceTransformer
import faiss
import google.generativeai as genai
from transformers import pipeline
import re
from pathlib import Path

logger = logging.getLogger(__name__)

class RAGService:
    def __init__(self):
        self.embedding_model = None
        self.qa_pipeline = None
        self.gemini_model = None
        self.index = None
        self.video_data = {}
        self.chunk_data = {}
        self.data_dir = Path("data")
        self.data_dir.mkdir(exist_ok=True)
        
        # Initialize Gemini if API key is available
        gemini_api_key = os.getenv("GEMINI_API_KEY")
        if gemini_api_key:
            genai.configure(api_key=gemini_api_key)
            self.gemini_model = genai.GenerativeModel('gemini-1.5-flash')
        
    async def initialize(self):
        """Initialize the RAG service with models"""
        try:
            logger.info("Initializing RAG service...")
            
            # Initialize embedding model
            logger.info("Loading embedding model...")
            self.embedding_model = SentenceTransformer('all-MiniLM-L6-v2')
            
            # Initialize QA pipeline
            logger.info("Loading QA pipeline...")
            self.qa_pipeline = pipeline(
                "question-answering",
                model="distilbert-base-uncased-distilled-squad",
                device=-1  # Use CPU
            )
            
            # Initialize FAISS index
            self.index = faiss.IndexFlatIP(384)  # all-MiniLM-L6-v2 has 384 dimensions
            
            # Load existing data if available
            await self._load_existing_data()
            
            logger.info("RAG service initialized successfully")
            
        except Exception as e:
            logger.error(f"Error initializing RAG service: {e}")
            raise
    
    async def _load_existing_data(self):
        """Load existing video data and index"""
        try:
            video_data_file = self.data_dir / "video_data.json"
            chunk_data_file = self.data_dir / "chunk_data.json"
            index_file = self.data_dir / "index.faiss"
            
            if video_data_file.exists():
                with open(video_data_file, 'r', encoding='utf-8') as f:
                    self.video_data = json.load(f)
                logger.info(f"Loaded data for {len(self.video_data)} videos")
            
            if chunk_data_file.exists():
                with open(chunk_data_file, 'r', encoding='utf-8') as f:
                    self.chunk_data = json.load(f)
                logger.info(f"Loaded {len(self.chunk_data)} chunks")
            
            if index_file.exists():
                self.index = faiss.read_index(str(index_file))
                logger.info(f"Loaded FAISS index with {self.index.ntotal} vectors")
                
        except Exception as e:
            logger.error(f"Error loading existing data: {e}")
    
    async def _save_data(self):
        """Save video data and index to disk"""
        try:
            video_data_file = self.data_dir / "video_data.json"
            chunk_data_file = self.data_dir / "chunk_data.json"
            index_file = self.data_dir / "index.faiss"
            
            with open(video_data_file, 'w', encoding='utf-8') as f:
                json.dump(self.video_data, f, ensure_ascii=False, indent=2)
            
            with open(chunk_data_file, 'w', encoding='utf-8') as f:
                json.dump(self.chunk_data, f, ensure_ascii=False, indent=2)
            
            faiss.write_index(self.index, str(index_file))
            
        except Exception as e:
            logger.error(f"Error saving data: {e}")
    
    def _chunk_text(self, text: str, chunk_size: int = 1000, overlap: int = 200) -> List[str]:
        """Split text into overlapping chunks"""
        chunks = []
        start = 0
        
        while start < len(text):
            end = start + chunk_size
            chunk = text[start:end]
            
            # Try to break at sentence boundaries
            if end < len(text):
                last_period = chunk.rfind('.')
                last_newline = chunk.rfind('\n')
                last_break = max(last_period, last_newline)
                
                if last_break > start + chunk_size // 2:
                    chunk = text[start:start + last_break + 1]
                    end = start + last_break + 1
            
            chunks.append(chunk.strip())
            start = end - overlap
            
            if start >= len(text):
                break
                
        return chunks
    
    async def process_transcript(self, video_id: str, transcript: str, title: str):
        """Process transcript and create embeddings"""
        try:
            logger.info(f"Processing transcript for video {video_id}")
            
            # Chunk the transcript
            chunks = self._chunk_text(transcript)
            
            # Create embeddings for chunks
            embeddings = []
            chunk_ids = []
            
            for i, chunk in enumerate(chunks):
                embedding = self.embedding_model.encode(chunk)
                embeddings.append(embedding)
                
                chunk_id = f"{video_id}_{i}"
                chunk_ids.append(chunk_id)
                
                # Store chunk data
                self.chunk_data[chunk_id] = {
                    'video_id': video_id,
                    'text': chunk,
                    'chunk_index': i,
                    'embedding_index': len(self.chunk_data) + i
                }
            
            # Add embeddings to FAISS index
            embeddings_array = np.array(embeddings).astype('float32')
            faiss.normalize_L2(embeddings_array)
            self.index.add(embeddings_array)
            
            # Store video data
            self.video_data[video_id] = {
                'title': title,
                'transcript': transcript,
                'chunk_count': len(chunks),
                'processed_at': datetime.now().isoformat(),
                'chunk_ids': chunk_ids
            }
            
            # Save data
            await self._save_data()
            
            logger.info(f"Processed {len(chunks)} chunks for video {video_id}")
            
        except Exception as e:
            logger.error(f"Error processing transcript for video {video_id}: {e}")
            raise
    
    async def answer_question(self, video_id: str, question: str, model_preference: str = "huggingface") -> Dict:
        """Answer a question using RAG"""
        try:
            logger.info(f"Answering question for video {video_id} using {model_preference}")
            
            # Get relevant chunks
            relevant_chunks = await self._get_relevant_chunks(video_id, question, top_k=5)
            
            if not relevant_chunks:
                return {
                    'answer': "I couldn't find relevant information in the video to answer your question.",
                    'confidence': 0.0,
                    'sources': []
                }
            
            # Prepare context
            context = "\n\n".join([chunk['text'] for chunk in relevant_chunks])
            
            # Generate answer based on model preference
            if model_preference == "gemini" and self.gemini_model:
                answer = await self._answer_with_gemini(question, context)
            else:
                answer = await self._answer_with_huggingface(question, context)
            
            return {
                'answer': answer['answer'],
                'confidence': answer['confidence'],
                'sources': relevant_chunks
            }
            
        except Exception as e:
            logger.error(f"Error answering question: {e}")
            raise
    
    async def _get_relevant_chunks(self, video_id: str, query: str, top_k: int = 5) -> List[Dict]:
        """Get relevant chunks for a query"""
        try:
            # Get video chunk IDs
            if video_id not in self.video_data:
                return []
            
            video_chunk_ids = self.video_data[video_id]['chunk_ids']
            
            # Create query embedding
            query_embedding = self.embedding_model.encode([query])
            query_embedding = query_embedding.astype('float32')
            faiss.normalize_L2(query_embedding)
            
            # Search in FAISS index
            scores, indices = self.index.search(query_embedding, min(top_k * 2, self.index.ntotal))
            
            # Filter results to only include chunks from the specified video
            relevant_chunks = []
            for score, idx in zip(scores[0], indices[0]):
                # Find chunk by embedding index
                chunk_id = None
                for cid, chunk_data in self.chunk_data.items():
                    if chunk_data.get('embedding_index') == idx and chunk_data['video_id'] == video_id:
                        chunk_id = cid
                        break
                
                if chunk_id and len(relevant_chunks) < top_k:
                    relevant_chunks.append({
                        'text': self.chunk_data[chunk_id]['text'],
                        'similarity': float(score),
                        'chunk_index': self.chunk_data[chunk_id]['chunk_index']
                    })
            
            return relevant_chunks
            
        except Exception as e:
            logger.error(f"Error getting relevant chunks: {e}")
            return []
    
    async def _answer_with_huggingface(self, question: str, context: str) -> Dict:
        """Answer question using HuggingFace model"""
        try:
            # Truncate context if too long
            max_context_length = 2000
            if len(context) > max_context_length:
                context = context[:max_context_length] + "..."
            
            # Run in thread pool to avoid blocking
            loop = asyncio.get_event_loop()
            result = await loop.run_in_executor(
                None,
                lambda: self.qa_pipeline(question=question, context=context)
            )
            
            return {
                'answer': result['answer'],
                'confidence': result['score']
            }
            
        except Exception as e:
            logger.error(f"Error answering with HuggingFace: {e}")
            return {
                'answer': "I'm sorry, I encountered an error while processing your question.",
                'confidence': 0.0
            }
    
    async def _answer_with_gemini(self, question: str, context: str) -> Dict:
        """Answer question using Gemini model"""
        try:
            prompt = f"""Based on the following context from a video transcript, please answer the question.

Context:
{context}

Question: {question}

Please provide a clear and concise answer based only on the information provided in the context. If the context doesn't contain enough information to answer the question, please say so.

Answer:"""
            
            # Run in thread pool to avoid blocking
            loop = asyncio.get_event_loop()
            response = await loop.run_in_executor(
                None,
                lambda: self.gemini_model.generate_content(prompt)
            )
            
            return {
                'answer': response.text,
                'confidence': 0.8  # Gemini doesn't provide confidence scores
            }
            
        except Exception as e:
            logger.error(f"Error answering with Gemini: {e}")
            return {
                'answer': "I'm sorry, I encountered an error while processing your question with Gemini.",
                'confidence': 0.0
            }
    
    async def is_video_processed(self, video_id: str) -> bool:
        """Check if video is processed"""
        return video_id in self.video_data
    
    async def list_processed_videos(self) -> List[Dict]:
        """List all processed videos"""
        videos = []
        for video_id, data in self.video_data.items():
            videos.append({
                'video_id': video_id,
                'title': data['title'],
                'processed_at': data['processed_at'],
                'chunk_count': data['chunk_count']
            })
        return videos
    
    async def delete_video(self, video_id: str):
        """Delete a processed video and its data"""
        if video_id not in self.video_data:
            raise ValueError(f"Video {video_id} not found")
        
        # Get chunk IDs to remove
        chunk_ids = self.video_data[video_id]['chunk_ids']
        
        # Remove chunks from chunk_data
        for chunk_id in chunk_ids:
            if chunk_id in self.chunk_data:
                del self.chunk_data[chunk_id]
        
        # Remove video data
        del self.video_data[video_id]
        
        # Note: We don't remove from FAISS index as it's complex to remove specific vectors
        # In production, you might want to rebuild the index periodically
        
        # Save data
        await self._save_data()
        
        logger.info(f"Deleted video {video_id} and its chunks") 