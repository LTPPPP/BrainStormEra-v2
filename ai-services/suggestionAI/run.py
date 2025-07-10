#!/usr/bin/env python3
"""
Runner script for Course Suggestion AI Service
"""

import uvicorn
import argparse
import sys
import os
from config import settings

def run_development():
    """Run in development mode with auto-reload"""
    print("🔧 Starting Course Suggestion AI Service in DEVELOPMENT mode")
    print(f"📍 Server: http://{settings.HOST}:{settings.PORT}")
    print(f"📊 Swagger UI: http://{settings.HOST}:{settings.PORT}/docs")
    print(f"📋 ReDoc: http://{settings.HOST}:{settings.PORT}/redoc")
    print("-" * 50)
    
    uvicorn.run(
        "main:app",
        host=settings.HOST,
        port=settings.PORT,
        reload=True,
        log_level="info",
        access_log=True
    )

def run_production():
    """Run in production mode"""
    print("🚀 Starting Course Suggestion AI Service in PRODUCTION mode")
    print(f"📍 Server: http://{settings.HOST}:{settings.PORT}")
    print("-" * 50)
    
    uvicorn.run(
        "main:app",
        host=settings.HOST,
        port=settings.PORT,
        reload=False,
        log_level="warning",
        access_log=False,
        workers=1
    )

def run_test():
    """Run test server on different port"""
    test_port = settings.PORT + 1000  # e.g., 8000 -> 9000
    
    print("🧪 Starting Course Suggestion AI Service in TEST mode")
    print(f"📍 Test Server: http://{settings.HOST}:{test_port}")
    print(f"📊 Swagger UI: http://{settings.HOST}:{test_port}/docs")
    print("-" * 50)
    
    uvicorn.run(
        "main:app",
        host=settings.HOST,
        port=test_port,
        reload=True,
        log_level="debug",
        access_log=True
    )

def check_dependencies():
    """Check if all required dependencies are installed"""
    required_packages = [
        'fastapi',
        'uvicorn',
        'sqlalchemy',
        'pyodbc',
        'pydantic',
        'python-dotenv',
        'requests'
    ]
    
    missing_packages = []
    
    for package in required_packages:
        try:
            __import__(package)
        except ImportError:
            missing_packages.append(package)
    
    if missing_packages:
        print("❌ Missing required packages:")
        for package in missing_packages:
            print(f"   - {package}")
        print("\n💡 Install missing packages with:")
        print("   pip install -r requirements.txt")
        return False
    
    print("✅ All required packages are installed")
    return True

def check_database():
    """Check database connection"""
    try:
        from sqlalchemy import create_engine, text
        
        print("🔍 Checking database connection...")
        engine = create_engine(settings.DATABASE_URL, echo=False)
        
        with engine.connect() as conn:
            result = conn.execute(text("SELECT 1"))
            result.fetchone()
        
        print("✅ Database connection successful")
        return True
        
    except Exception as e:
        print(f"❌ Database connection failed: {e}")
        print("\n💡 Please check:")
        print("   - DATABASE_URL in .env file")
        print("   - SQL Server is running")
        print("   - Network connectivity")
        print("   - ODBC Driver is installed")
        return False

def main():
    """Main runner function"""
    parser = argparse.ArgumentParser(description="Course Suggestion AI Service Runner")
    parser.add_argument(
        "mode",
        choices=["dev", "prod", "test", "check"],
        help="Running mode: dev (development), prod (production), test (test server), check (check dependencies)"
    )
    parser.add_argument(
        "--skip-checks",
        action="store_true",
        help="Skip dependency and database checks"
    )
    
    args = parser.parse_args()
    
    print("🧠 Course Suggestion AI Service")
    print("=" * 50)
    
    # Run checks unless skipped
    if not args.skip_checks and args.mode != "check":
        if not check_dependencies():
            sys.exit(1)
        
        if not check_database():
            sys.exit(1)
        
        print("-" * 50)
    
    # Run based on mode
    if args.mode == "dev":
        run_development()
    elif args.mode == "prod":
        run_production()
    elif args.mode == "test":
        run_test()
    elif args.mode == "check":
        success = check_dependencies() and check_database()
        if success:
            print("\n🎉 All checks passed! Ready to run the service.")
        else:
            print("\n❌ Some checks failed. Please fix the issues above.")
            sys.exit(1)

if __name__ == "__main__":
    main() 