import numpy as np
from sentence_transformers import SentenceTransformer
from typing import List, Dict, Any, Optional, Union
import logging
import asyncio
from concurrent.futures import ThreadPoolExecutor
import time
import os
import sys
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from utils.utils import TextUtils

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class EmbeddingService:
    """Service for generating text embeddings using sentence-transformers"""
    
    def __init__(self, model_name: str = "sentence-transformers/all-MiniLM-L6-v2"):
        """
        Initialize embedding service
        
        Args:
            model_name: Name of the sentence-transformers model to use
        """
        self.model_name = model_name
        self.model = None
        self.executor = ThreadPoolExecutor(max_workers=2)
        self.embedding_dim = None
        
        # Initialize model
        self._initialize_model()
    
    def _initialize_model(self):
        """Initialize sentence-transformers model"""
        try:
            logger.info(f"Initializing sentence-transformers model: {self.model_name}")
            self.model = SentenceTransformer(self.model_name)
            
            # Get embedding dimension
            test_embedding = self.model.encode(["test"])
            self.embedding_dim = test_embedding.shape[1]
            
            logger.info(f"Model initialized successfully. Embedding dimension: {self.embedding_dim}")
        except Exception as e:
            logger.error(f"Failed to initialize embedding model: {str(e)}")
            raise
    
    def _encode_text_sync(self, texts: Union[str, List[str]], normalize: bool = True) -> np.ndarray:
        """
        Synchronous text encoding
        
        Args:
            texts: Single text or list of texts to encode
            normalize: Whether to normalize embeddings
            
        Returns:
            Numpy array of embeddings
        """
        try:
            if isinstance(texts, str):
                texts = [texts]
            
            # Clean texts
            cleaned_texts = [TextUtils.clean_ocr_text(text) for text in texts]
            
            # Filter out empty texts
            non_empty_texts = [text for text in cleaned_texts if text.strip()]
            
            if not non_empty_texts:
                # Return zero embeddings for empty texts
                return np.zeros((len(texts), self.embedding_dim))
            
            # Generate embeddings
            embeddings = self.model.encode(
                non_empty_texts,
                normalize_embeddings=normalize,
                show_progress_bar=False
            )
            
            # Handle case where some texts were empty
            if len(non_empty_texts) != len(texts):
                full_embeddings = np.zeros((len(texts), self.embedding_dim))
                non_empty_idx = 0
                
                for i, text in enumerate(cleaned_texts):
                    if text.strip():
                        full_embeddings[i] = embeddings[non_empty_idx]
                        non_empty_idx += 1
                
                return full_embeddings
            
            return embeddings
            
        except Exception as e:
            logger.error(f"Error encoding texts: {str(e)}")
            # Return zero embeddings as fallback
            return np.zeros((len(texts) if isinstance(texts, list) else 1, self.embedding_dim))
    
    async def encode_text(self, texts: Union[str, List[str]], normalize: bool = True) -> np.ndarray:
        """
        Asynchronous text encoding
        
        Args:
            texts: Single text or list of texts to encode
            normalize: Whether to normalize embeddings
            
        Returns:
            Numpy array of embeddings
        """
        try:
            loop = asyncio.get_event_loop()
            embeddings = await loop.run_in_executor(
                self.executor,
                self._encode_text_sync,
                texts,
                normalize
            )
            return embeddings
        except Exception as e:
            logger.error(f"Error in async text encoding: {str(e)}")
            text_count = len(texts) if isinstance(texts, list) else 1
            return np.zeros((text_count, self.embedding_dim))
    
    def _chunk_and_encode_sync(self, text: str, chunk_size: int = 500, overlap: int = 50) -> List[Dict[str, Any]]:
        """
        Synchronous text chunking and encoding
        
        Args:
            text: Input text to chunk and encode
            chunk_size: Size of each chunk
            overlap: Overlap between chunks
            
        Returns:
            List of dictionaries containing chunk text and embeddings
        """
        try:
            if not text or not text.strip():
                return []
            
            # Split text into chunks
            chunks = TextUtils.split_text_into_chunks(text, chunk_size, overlap)
            
            if not chunks:
                return []
            
            # Encode all chunks
            embeddings = self._encode_text_sync(chunks)
            
            # Create result list
            result = []
            for i, (chunk, embedding) in enumerate(zip(chunks, embeddings)):
                result.append({
                    "chunk_id": i,
                    "text": chunk,
                    "embedding": embedding,
                    "start_pos": i * (chunk_size - overlap),
                    "end_pos": min((i + 1) * (chunk_size - overlap), len(text.split()))
                })
            
            return result
            
        except Exception as e:
            logger.error(f"Error in chunking and encoding: {str(e)}")
            return []
    
    async def chunk_and_encode(self, text: str, chunk_size: int = 500, overlap: int = 50) -> List[Dict[str, Any]]:
        """
        Asynchronous text chunking and encoding
        
        Args:
            text: Input text to chunk and encode
            chunk_size: Size of each chunk
            overlap: Overlap between chunks
            
        Returns:
            List of dictionaries containing chunk text and embeddings
        """
        try:
            loop = asyncio.get_event_loop()
            result = await loop.run_in_executor(
                self.executor,
                self._chunk_and_encode_sync,
                text,
                chunk_size,
                overlap
            )
            return result
        except Exception as e:
            logger.error(f"Error in async chunking and encoding: {str(e)}")
            return []
    
    def calculate_similarity(self, embedding1: np.ndarray, embedding2: np.ndarray) -> float:
        """
        Calculate cosine similarity between two embeddings
        
        Args:
            embedding1: First embedding
            embedding2: Second embedding
            
        Returns:
            Cosine similarity score
        """
        try:
            # Normalize embeddings
            norm1 = np.linalg.norm(embedding1)
            norm2 = np.linalg.norm(embedding2)
            
            if norm1 == 0 or norm2 == 0:
                return 0.0
            
            normalized1 = embedding1 / norm1
            normalized2 = embedding2 / norm2
            
            # Calculate cosine similarity
            similarity = np.dot(normalized1, normalized2)
            
            return float(similarity)
            
        except Exception as e:
            logger.error(f"Error calculating similarity: {str(e)}")
            return 0.0
    
    def find_most_similar(self, query_embedding: np.ndarray, candidate_embeddings: List[np.ndarray], 
                          top_k: int = 5) -> List[Dict[str, Any]]:
        """
        Find most similar embeddings to query
        
        Args:
            query_embedding: Query embedding
            candidate_embeddings: List of candidate embeddings
            top_k: Number of top results to return
            
        Returns:
            List of similarity results
        """
        try:
            if not candidate_embeddings:
                return []
            
            # Calculate similarities
            similarities = []
            for i, candidate in enumerate(candidate_embeddings):
                similarity = self.calculate_similarity(query_embedding, candidate)
                similarities.append({
                    "index": i,
                    "similarity": similarity
                })
            
            # Sort by similarity (descending)
            similarities.sort(key=lambda x: x["similarity"], reverse=True)
            
            # Return top k results
            return similarities[:top_k]
            
        except Exception as e:
            logger.error(f"Error finding most similar embeddings: {str(e)}")
            return []
    
    async def batch_encode(self, texts: List[str], batch_size: int = 32) -> List[np.ndarray]:
        """
        Batch encode multiple texts
        
        Args:
            texts: List of texts to encode
            batch_size: Batch size for processing
            
        Returns:
            List of embeddings
        """
        try:
            if not texts:
                return []
            
            # Process in batches
            results = []
            for i in range(0, len(texts), batch_size):
                batch = texts[i:i + batch_size]
                batch_embeddings = await self.encode_text(batch)
                results.extend(batch_embeddings)
            
            return results
            
        except Exception as e:
            logger.error(f"Error in batch encoding: {str(e)}")
            return [np.zeros(self.embedding_dim) for _ in texts]
    
    def get_model_info(self) -> Dict[str, Any]:
        """Get information about the embedding model"""
        return {
            "model_name": self.model_name,
            "embedding_dim": self.embedding_dim,
            "max_seq_length": getattr(self.model, 'max_seq_length', 512),
            "model_initialized": self.model is not None
        }
    
    async def health_check(self) -> Dict[str, Any]:
        """Perform health check on embedding service"""
        try:
            start_time = time.time()
            
            # Test encoding
            test_texts = ["This is a test sentence.", "Đây là câu thử nghiệm."]
            embeddings = await self.encode_text(test_texts)
            
            # Test similarity
            similarity = self.calculate_similarity(embeddings[0], embeddings[1])
            
            test_time = time.time() - start_time
            
            return {
                "status": "healthy",
                "test_time": test_time,
                "model_name": self.model_name,
                "embedding_dim": self.embedding_dim,
                "test_similarity": similarity,
                "test_passed": embeddings is not None and len(embeddings) == 2
            }
            
        except Exception as e:
            logger.error(f"Health check failed: {str(e)}")
            return {
                "status": "unhealthy",
                "error": str(e),
                "model_name": self.model_name,
                "embedding_dim": self.embedding_dim
            }
    
    def __del__(self):
        """Cleanup when service is destroyed"""
        try:
            if hasattr(self, 'executor'):
                self.executor.shutdown(wait=True)
        except:
            pass 