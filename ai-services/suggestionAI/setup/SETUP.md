# Quick Setup Guide

## 🚀 Quick Start

### 1. Tự động setup:
```bash
cd ai-services/suggestionAI
python setup.py
```

### 2. Manual setup:
```bash
# Copy environment template
cp env-template.txt .env

# Install dependencies
pip install -r requirements.txt

# Edit .env file và thêm Gemini API key
nano .env
```

## 🔑 Gemini API Key

1. Truy cập: https://makersuite.google.com/
2. Đăng nhập với Google account
3. Tạo API key mới
4. Copy vào file `.env`:
   ```
   GEMINI_API_KEY=your_actual_api_key_here
   ```

## ✅ Verify Setup

```bash
# Check dependencies và database
python run.py check

# Run development server
python run.py dev

# Test API
python test_service.py
```

## 📁 Files Created

- `.env` - Configuration file (sẽ được ignore bởi git)
- `.gitignore` - Git ignore rules
- `env-template.txt` - Template cho .env

## 🔧 Configuration

Edit `.env` file để customize:

```env
# Database
DATABASE_URL=your_database_connection_string

# Server
HOST=0.0.0.0
PORT=7000

# Gemini AI
GEMINI_API_KEY=your_api_key
USE_GEMINI=true

# Development
DEBUG=true
```

## 🌐 API Endpoints

- Health: `GET http://localhost:7000/health`
- Suggestions: `POST http://localhost:7000/suggest-courses-enhanced`
- Swagger: `http://localhost:7000/docs`

## 🆘 Troubleshooting

### 🔧 Database Connection Issues
**Common error: "Named Pipes Provider: Could not open a connection"**

**Quick fixes:**
```bash
# 1. Run database test script
python db_test.py

# 2. Try localhost instead of computer name in .env:
DATABASE_URL=mssql+pyodbc://sa:01654460072ltp@localhost/BrainStormEra?driver=ODBC+Driver+17+for+SQL+Server&TrustServerCertificate=yes
```

📖 **See [DATABASE_TROUBLESHOOTING.md](DATABASE_TROUBLESHOOTING.md) for complete database setup guide.**

### 🤖 Gemini API Issues
- Verify API key trong .env
- Check network connectivity
- Service vẫn hoạt động mà không có Gemini (fallback mode)

### 📦 Dependencies Issues
```bash
pip install -r requirements.txt
```

### 🔌 Port Conflicts
```bash
# Change port trong .env
PORT=8000
```

### 🚨 Getting Help
```bash
# Test everything
python db_test.py

# Check service health
python run.py check

# Test API
python test_service.py
``` 