#!/usr/bin/env python3
"""
Setup script for Course Suggestion AI Service
"""

import os
import shutil
import sys

def create_env_file():
    """Create .env file from template"""
    template_file = ".env.template"
    env_file = ".env"
    
    if os.path.exists(env_file):
        response = input(f"{env_file} already exists. Overwrite? (y/N): ")
        if response.lower() != 'y':
            print("Skipping .env file creation.")
            return False
    
    if not os.path.exists(template_file):
        print(f"Error: {template_file} not found!")
        return False
    
    try:
        shutil.copy(template_file, env_file)
        print(f"✅ Created {env_file} from template")
        return True
    except Exception as e:
        print(f"❌ Error creating {env_file}: {e}")
        return False

def check_dependencies():
    """Check if required packages are installed"""
    required_packages = [
        'fastapi',
        'uvicorn',
        'sqlalchemy',
        'pyodbc',
        'pydantic',
        'python-dotenv',
        'requests',
        'google-generativeai'
    ]
    
    missing_packages = []
    
    for package in required_packages:
        try:
            __import__(package.replace('-', '_'))
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

def display_next_steps():
    """Display next steps for user"""
    print("\n🎉 Setup completed!")
    print("\n📝 Next steps:")
    print("1. Edit .env file and add your Gemini API key:")
    print("   GEMINI_API_KEY=your_actual_api_key_here")
    print("\n2. Get Gemini API key from:")
    print("   https://makersuite.google.com/")
    print("\n3. Install dependencies (if not already done):")
    print("   pip install -r requirements.txt")
    print("\n4. Run the service:")
    print("   python run.py")
    print("\n5. Alternative run method:")
    print("   uvicorn app.main:app --host 0.0.0.0 --port 7000 --reload")

def main():
    """Main setup function"""
    print("🚀 Setting up Course Suggestion AI Service")
    print("=" * 50)
    
    # Create .env file
    env_created = create_env_file()
    
    # Check dependencies
    deps_ok = check_dependencies()
    
    # Display results
    print("\n" + "=" * 50)
    if env_created:
        print("✅ Environment file created")
    else:
        print("⚠️  Environment file not created")
    
    if deps_ok:
        print("✅ Dependencies satisfied")
    else:
        print("❌ Missing dependencies")
    
    # Show next steps
    display_next_steps()

if __name__ == "__main__":
    main() 