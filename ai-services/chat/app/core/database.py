from sqlalchemy import create_engine
from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker
from sqlalchemy.orm import sessionmaker, Session
from sqlalchemy.pool import StaticPool
from app.core.config import settings
from app.models.database import Base
import asyncio
from concurrent.futures import ThreadPoolExecutor
import functools

# Sync engine for SQL Server (since aiodbc is not well supported)
sync_engine = create_engine(
    settings.database_url_sync,
    echo=settings.debug,
    future=True,
    pool_pre_ping=True,
    pool_recycle=300
)

# Sync session maker
SessionLocal = sessionmaker(
    bind=sync_engine,
    autocommit=False,
    autoflush=False,
)

# Thread pool for async wrapper
executor = ThreadPoolExecutor(max_workers=10)

def run_sync_in_async(func, *args, **kwargs):
    """Run sync function in async context"""
    loop = asyncio.get_event_loop()
    return loop.run_in_executor(executor, func, *args, **kwargs)

class AsyncSessionWrapper:
    """Async wrapper for sync SQL Server session"""
    
    def __init__(self):
        self._session = None
    
    async def __aenter__(self):
        self._session = SessionLocal()
        return self
    
    async def __aexit__(self, exc_type, exc_val, exc_tb):
        if self._session:
            if exc_type:
                await run_sync_in_async(self._session.rollback)
            else:
                try:
                    await run_sync_in_async(self._session.commit)
                except Exception:
                    await run_sync_in_async(self._session.rollback)
                    raise
            await run_sync_in_async(self._session.close)
    
    async def execute(self, statement):
        """Execute SQL statement async"""
        result = await run_sync_in_async(self._session.execute, statement)
        return AsyncResultWrapper(result)
    
    async def flush(self):
        """Flush session async"""
        await run_sync_in_async(self._session.flush)
    
    async def commit(self):
        """Commit session async"""
        await run_sync_in_async(self._session.commit)
    
    async def rollback(self):
        """Rollback session async"""
        await run_sync_in_async(self._session.rollback)
    
    async def close(self):
        """Close session async"""
        await run_sync_in_async(self._session.close)
    
    def add(self, instance):
        """Add instance to session"""
        self._session.add(instance)
    
    def delete(self, instance):
        """Delete instance from session"""
        self._session.delete(instance)

class AsyncResultWrapper:
    """Async wrapper for sync result"""
    
    def __init__(self, result):
        self._result = result
    
    def scalar_one_or_none(self):
        return self._result.scalar_one_or_none()
    
    def scalar(self):
        return self._result.scalar()
    
    def scalars(self):
        return self._result.scalars()
    
    def all(self):
        return self._result.all()
    
    def first(self):
        return self._result.first()

async def get_async_session():
    """Dependency to get async database session"""
    async with AsyncSessionWrapper() as session:
        try:
            yield session
        except Exception:
            await session.rollback()
            raise

def get_sync_session() -> Session:
    """Dependency to get sync database session"""
    session = SessionLocal()
    try:
        yield session
        session.commit()
    except Exception:
        session.rollback()
        raise
    finally:
        session.close()

# Database tables are managed by MVC application
# No need to create or drop tables from FastAPI service 