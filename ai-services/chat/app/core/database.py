from sqlalchemy import create_engine
from sqlalchemy.ext.asyncio import create_async_engine, AsyncSession
from sqlalchemy.orm import sessionmaker, Session
from sqlalchemy.pool import StaticPool
from app.core.config import settings
from app.models.database import Base


# Async engine for async operations
async_engine = create_async_engine(
    settings.database_url,
    echo=settings.debug,
    future=True,
    poolclass=StaticPool if "sqlite" in settings.database_url else None,
)

# Sync engine for migrations and sync operations
sync_engine = create_engine(
    settings.database_url_sync,
    echo=settings.debug,
    future=True,
    poolclass=StaticPool if "sqlite" in settings.database_url_sync else None,
)

# Async session maker
AsyncSessionLocal = sessionmaker(
    bind=async_engine,
    class_=AsyncSession,
    expire_on_commit=False,
    autocommit=False,
    autoflush=False,
)

# Sync session maker
SessionLocal = sessionmaker(
    bind=sync_engine,
    autocommit=False,
    autoflush=False,
)


async def get_async_session() -> AsyncSession:
    """Dependency to get async database session"""
    async with AsyncSessionLocal() as session:
        try:
            yield session
            await session.commit()
        except Exception:
            await session.rollback()
            raise
        finally:
            await session.close()


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


async def create_tables():
    """Create all tables"""
    async with async_engine.begin() as conn:
        await conn.run_sync(Base.metadata.create_all)


async def drop_tables():
    """Drop all tables"""
    async with async_engine.begin() as conn:
        await conn.run_sync(Base.metadata.drop_all) 