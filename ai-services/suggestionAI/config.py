import os
from dotenv import load_dotenv

# Load environment variables from .env file
load_dotenv()

class Settings:
    """Application settings"""
    
    # Database configuration
    DATABASE_URL: str = os.getenv(
        "DATABASE_URL", 
        "ok"
    )
    
    # Server configuration
    HOST: str = os.getenv("HOST", "0.0.0.0")
    PORT: int = int(os.getenv("PORT", "7000"))
    
    # AI Configuration
    MAX_SUGGESTIONS: int = int(os.getenv("MAX_SUGGESTIONS", "10"))
    MIN_SIMILARITY_SCORE: float = float(os.getenv("MIN_SIMILARITY_SCORE", "0.1"))
    
    # Gemini AI Configuration
    GEMINI_API_KEY: str = os.getenv("GEMINI_API_KEY", "")
    GEMINI_MODEL: str = os.getenv("GEMINI_MODEL", "gemini-pro")
    USE_GEMINI: bool = os.getenv("USE_GEMINI", "true").lower() == "true"

settings = Settings()

# Example database URLs for different environments:
# Local SQL Server: mssql+pyodbc://sa:yourpassword@localhost/BrainStormEra?driver=ODBC+Driver+17+for+SQL+Server&TrustServerCertificate=yes
# Remote SQL Server: mssql+pyodbc://username:password@server.database.windows.net/BrainStormEra?driver=ODBC+Driver+17+for+SQL+Server&TrustServerCertificate=yes 