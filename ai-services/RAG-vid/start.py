#!/usr/bin/env python3
"""
Quick start script for RAG Video Service
This script will install dependencies and start the service
"""

import os
import sys
import subprocess
import time
from pathlib import Path

def print_header():
    """Print service header"""
    print("🎥 RAG Video Service")
    print("=" * 50)
    print("A FastAPI service for YouTube video Q&A using RAG")
    print("=" * 50)

def install_dependencies():
    """Install required dependencies"""
    print("📦 Installing dependencies...")
    
    # Check if pip is available
    try:
        subprocess.run([sys.executable, "-m", "pip", "--version"], 
                      check=True, capture_output=True)
    except subprocess.CalledProcessError:
        print("❌ pip is not available. Please install pip first.")
        return False
    
    # Install requirements
    try:
        subprocess.run([sys.executable, "-m", "pip", "install", "-r", "requirements.txt"], 
                      check=True)
        print("✅ Dependencies installed successfully")
        return True
    except subprocess.CalledProcessError as e:
        print(f"❌ Failed to install dependencies: {e}")
        return False

def check_python_version():
    """Check Python version"""
    if sys.version_info < (3, 8):
        print("❌ Python 3.8 or higher is required")
        print(f"   Current version: {sys.version}")
        return False
    print(f"✅ Python version: {sys.version}")
    return True

def main():
    """Main function"""
    print_header()
    
    # Check Python version
    if not check_python_version():
        return
    
    # Check if requirements.txt exists
    if not Path("requirements.txt").exists():
        print("❌ requirements.txt not found")
        print("   Please make sure you're in the RAG-vid directory")
        return
    
    # Install dependencies
    if not install_dependencies():
        return
    
    print("\n" + "=" * 50)
    print("🚀 Starting the service...")
    print("=" * 50)
    
    # Give some time for installation to complete
    time.sleep(2)
    
    # Start the service
    try:
        # Use run.py if it exists, otherwise use main.py
        if Path("run.py").exists():
            subprocess.run([sys.executable, "run.py"])
        else:
            subprocess.run([sys.executable, "main.py"])
    except KeyboardInterrupt:
        print("\n🛑 Service stopped")
    except Exception as e:
        print(f"\n❌ Error starting service: {e}")

if __name__ == "__main__":
    main() 