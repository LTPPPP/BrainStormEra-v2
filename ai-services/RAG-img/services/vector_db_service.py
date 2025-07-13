import chromadb
from chromadb.config import Settings
import numpy as np
from typing import List, Dict, Any, Optional, Union
import logging
import asyncio
from concurrent.futures import ThreadPoolExecutor
import time
import os
import json
import uuid
import sys
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from utils.utils import TextUtils

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class VectorDBService:
    """Service for vector database operations using ChromaDB"""
    
    def __init__(self, db_path: str = "chroma_db", collection_name: str = "image_texts"):
        """
        Initialize vector database service
        
        Args:
            db_path: Path to ChromaDB database
            collection_name: Name of the collection to use
        """
        self.db_path = db_path
        self.collection_name = collection_name
        self.client = None
        self.collection = None
        self.executor = ThreadPoolExecutor(max_workers=2)
        
        # Initialize ChromaDB
        self._initialize_db()
    
    def _initialize_db(self):
        """Initialize ChromaDB client and collection"""
        try:
            logger.info(f"Initializing ChromaDB at path: {self.db_path}")
            
            # Create directory if it doesn't exist
            os.makedirs(self.db_path, exist_ok=True)
            
            # Initialize ChromaDB client
            self.client = chromadb.PersistentClient(
                path=self.db_path,
                settings=Settings(
                    anonymized_telemetry=False,
                    allow_reset=True
                )
            )
            
            # Get or create collection
            self.collection = self.client.get_or_create_collection(
                name=self.collection_name,
                metadata={"hnsw:space": "cosine"}
            )
            
            logger.info(f"ChromaDB initialized successfully. Collection: {self.collection_name}")
        except Exception as e:
            logger.error(f"Failed to initialize ChromaDB: {str(e)}")
            raise
    
    def _add_vectors_sync(self, texts: List[str], embeddings: List[np.ndarray], 
                          metadatas: List[Dict[str, Any]], ids: Optional[List[str]] = None) -> Dict[str, Any]:
        """
        Synchronous vector addition
        
        Args:
            texts: List of texts
            embeddings: List of embeddings
            metadatas: List of metadata dictionaries
            ids: Optional list of IDs (will be generated if not provided)
            
        Returns:
            Dictionary with operation results
        """
        try:
            if not texts or not embeddings:
                return {"success": False, "error": "Empty texts or embeddings"}
            
            # Generate IDs if not provided
            if ids is None:
                ids = [str(uuid.uuid4()) for _ in range(len(texts))]
            
            # Convert embeddings to list format
            embeddings_list = [embedding.tolist() for embedding in embeddings]
            
            # Add to collection
            self.collection.add(
                documents=texts,
                embeddings=embeddings_list,
                metadatas=metadatas,
                ids=ids
            )
            
            return {
                "success": True,
                "added_count": len(texts),
                "ids": ids
            }
            
        except Exception as e:
            logger.error(f"Error adding vectors: {str(e)}")
            return {
                "success": False,
                "error": str(e),
                "added_count": 0
            }
    
    async def add_vectors(self, texts: List[str], embeddings: List[np.ndarray], 
                          metadatas: List[Dict[str, Any]], ids: Optional[List[str]] = None) -> Dict[str, Any]:
        """
        Asynchronous vector addition
        
        Args:
            texts: List of texts
            embeddings: List of embeddings
            metadatas: List of metadata dictionaries
            ids: Optional list of IDs
            
        Returns:
            Dictionary with operation results
        """
        try:
            loop = asyncio.get_event_loop()
            result = await loop.run_in_executor(
                self.executor,
                self._add_vectors_sync,
                texts,
                embeddings,
                metadatas,
                ids
            )
            return result
        except Exception as e:
            logger.error(f"Error in async vector addition: {str(e)}")
            return {
                "success": False,
                "error": str(e),
                "added_count": 0
            }
    
    def _search_vectors_sync(self, query_embedding: np.ndarray, n_results: int = 10, 
                            where: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        """
        Synchronous vector search
        
        Args:
            query_embedding: Query embedding
            n_results: Number of results to return
            where: Optional filter conditions
            
        Returns:
            Dictionary with search results
        """
        try:
            # Convert embedding to list format
            query_embedding_list = query_embedding.tolist()
            
            # Search in collection
            results = self.collection.query(
                query_embeddings=[query_embedding_list],
                n_results=n_results,
                where=where,
                include=['documents', 'metadatas', 'distances']
            )
            
            # Process results
            documents = results.get('documents', [[]])[0]
            metadatas = results.get('metadatas', [[]])[0]
            distances = results.get('distances', [[]])[0]
            ids = results.get('ids', [[]])[0]
            
            # Convert distances to similarities (ChromaDB uses distance, we want similarity)
            similarities = [1 - distance for distance in distances]
            
            # Format results
            formatted_results = []
            for i, (doc, metadata, similarity, doc_id) in enumerate(zip(documents, metadatas, similarities, ids)):
                formatted_results.append({
                    "id": doc_id,
                    "document": doc,
                    "metadata": metadata,
                    "similarity": similarity,
                    "rank": i + 1
                })
            
            return {
                "success": True,
                "results": formatted_results,
                "total_results": len(formatted_results)
            }
            
        except Exception as e:
            logger.error(f"Error searching vectors: {str(e)}")
            return {
                "success": False,
                "error": str(e),
                "results": [],
                "total_results": 0
            }
    
    async def search_vectors(self, query_embedding: np.ndarray, n_results: int = 10, 
                            where: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        """
        Asynchronous vector search
        
        Args:
            query_embedding: Query embedding
            n_results: Number of results to return
            where: Optional filter conditions
            
        Returns:
            Dictionary with search results
        """
        try:
            loop = asyncio.get_event_loop()
            result = await loop.run_in_executor(
                self.executor,
                self._search_vectors_sync,
                query_embedding,
                n_results,
                where
            )
            return result
        except Exception as e:
            logger.error(f"Error in async vector search: {str(e)}")
            return {
                "success": False,
                "error": str(e),
                "results": [],
                "total_results": 0
            }
    
    def _delete_vectors_sync(self, ids: List[str]) -> Dict[str, Any]:
        """
        Synchronous vector deletion
        
        Args:
            ids: List of IDs to delete
            
        Returns:
            Dictionary with operation results
        """
        try:
            if not ids:
                return {"success": False, "error": "No IDs provided"}
            
            # Delete from collection
            self.collection.delete(ids=ids)
            
            return {
                "success": True,
                "deleted_count": len(ids),
                "deleted_ids": ids
            }
            
        except Exception as e:
            logger.error(f"Error deleting vectors: {str(e)}")
            return {
                "success": False,
                "error": str(e),
                "deleted_count": 0
            }
    
    async def delete_vectors(self, ids: List[str]) -> Dict[str, Any]:
        """
        Asynchronous vector deletion
        
        Args:
            ids: List of IDs to delete
            
        Returns:
            Dictionary with operation results
        """
        try:
            loop = asyncio.get_event_loop()
            result = await loop.run_in_executor(
                self.executor,
                self._delete_vectors_sync,
                ids
            )
            return result
        except Exception as e:
            logger.error(f"Error in async vector deletion: {str(e)}")
            return {
                "success": False,
                "error": str(e),
                "deleted_count": 0
            }
    
    def _get_collection_info_sync(self) -> Dict[str, Any]:
        """
        Synchronous collection information retrieval
        
        Returns:
            Dictionary with collection information
        """
        try:
            # Get collection count
            count = self.collection.count()
            
            # Get collection metadata
            metadata = self.collection.metadata
            
            return {
                "success": True,
                "collection_name": self.collection_name,
                "total_vectors": count,
                "metadata": metadata
            }
            
        except Exception as e:
            logger.error(f"Error getting collection info: {str(e)}")
            return {
                "success": False,
                "error": str(e),
                "total_vectors": 0
            }
    
    async def get_collection_info(self) -> Dict[str, Any]:
        """
        Asynchronous collection information retrieval
        
        Returns:
            Dictionary with collection information
        """
        try:
            loop = asyncio.get_event_loop()
            result = await loop.run_in_executor(
                self.executor,
                self._get_collection_info_sync
            )
            return result
        except Exception as e:
            logger.error(f"Error in async collection info retrieval: {str(e)}")
            return {
                "success": False,
                "error": str(e),
                "total_vectors": 0
            }
    
    async def add_image_text(self, image_id: str, text: str, embedding: np.ndarray, 
                            metadata: Dict[str, Any]) -> Dict[str, Any]:
        """
        Add image text and embedding to vector database
        
        Args:
            image_id: Unique image identifier
            text: Extracted text from image
            embedding: Text embedding
            metadata: Additional metadata
            
        Returns:
            Dictionary with operation results
        """
        try:
            # Prepare metadata
            full_metadata = {
                "image_id": image_id,
                "text_length": len(text),
                "timestamp": time.time(),
                **metadata
            }
            
            # Add to vector database
            result = await self.add_vectors(
                texts=[text],
                embeddings=[embedding],
                metadatas=[full_metadata],
                ids=[f"img_{image_id}"]
            )
            
            return result
            
        except Exception as e:
            logger.error(f"Error adding image text: {str(e)}")
            return {
                "success": False,
                "error": str(e),
                "added_count": 0
            }
    
    async def search_similar_images(self, query_embedding: np.ndarray, n_results: int = 10, 
                                   similarity_threshold: float = 0.7) -> Dict[str, Any]:
        """
        Search for similar images based on text content
        
        Args:
            query_embedding: Query embedding
            n_results: Number of results to return
            similarity_threshold: Minimum similarity threshold
            
        Returns:
            Dictionary with search results
        """
        try:
            # Search vectors
            search_result = await self.search_vectors(query_embedding, n_results)
            
            if not search_result["success"]:
                return search_result
            
            # Filter by similarity threshold
            filtered_results = []
            for result in search_result["results"]:
                if result["similarity"] >= similarity_threshold:
                    filtered_results.append(result)
            
            return {
                "success": True,
                "results": filtered_results,
                "total_results": len(filtered_results),
                "similarity_threshold": similarity_threshold
            }
            
        except Exception as e:
            logger.error(f"Error searching similar images: {str(e)}")
            return {
                "success": False,
                "error": str(e),
                "results": [],
                "total_results": 0
            }
    
    async def health_check(self) -> Dict[str, Any]:
        """Perform health check on vector database service"""
        try:
            start_time = time.time()
            
            # Test basic operations
            info = await self.get_collection_info()
            
            # Test vector addition and search
            test_text = "This is a test document"
            test_embedding = np.random.rand(384)  # Common embedding dimension
            test_metadata = {"test": True}
            
            # Add test vector
            add_result = await self.add_vectors(
                texts=[test_text],
                embeddings=[test_embedding],
                metadatas=[test_metadata],
                ids=["test_vector"]
            )
            
            # Search test vector
            search_result = await self.search_vectors(test_embedding, n_results=1)
            
            # Clean up test vector
            await self.delete_vectors(["test_vector"])
            
            test_time = time.time() - start_time
            
            return {
                "status": "healthy",
                "test_time": test_time,
                "collection_name": self.collection_name,
                "total_vectors": info.get("total_vectors", 0),
                "add_test_passed": add_result["success"],
                "search_test_passed": search_result["success"],
                "db_path": self.db_path
            }
            
        except Exception as e:
            logger.error(f"Health check failed: {str(e)}")
            return {
                "status": "unhealthy",
                "error": str(e),
                "collection_name": self.collection_name,
                "db_path": self.db_path
            }
    
    def __del__(self):
        """Cleanup when service is destroyed"""
        try:
            if hasattr(self, 'executor'):
                self.executor.shutdown(wait=True)
        except:
            pass 