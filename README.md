# BrainStormEra - Advanced E-Learning Platform 🧠

<div align="center">
  <img src="Main_Logo.jpg" alt="BrainStormEra Logo" width="100"/>
  
  <p align="center">
    <strong>A Comprehensive Web-Based Educational Platform with Interactive Learning Experience & AI-Powered Features</strong>
  </p>
  
  <p align="center">
    <img src="https://img.shields.io/badge/.NET-6.0+-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 6.0+"/>
    <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" alt="C#"/>
    <img src="https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white" alt="SQL Server"/>
    <img src="https://img.shields.io/badge/Python-3776AB?style=for-the-badge&logo=python&logoColor=white" alt="Python"/>
    <img src="https://img.shields.io/badge/FastAPI-009688?style=for-the-badge&logo=fastapi&logoColor=white" alt="FastAPI"/>
    <img src="https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white" alt="Docker"/>
    <img src="https://img.shields.io/badge/SignalR-000000?style=for-the-badge&logo=microsoft&logoColor=white" alt="SignalR"/>
    <img src="https://img.shields.io/badge/Entity%20Framework-1BA5E1?style=for-the-badge&logo=microsoft&logoColor=white" alt="Entity Framework"/>
  </p>
  
  <p align="center">
    <img src="https://img.shields.io/badge/Status-Active%20Development-green?style=flat-square" alt="Status"/>
    <img src="https://img.shields.io/badge/License-MIT-blue?style=flat-square" alt="License"/>
    <img src="https://img.shields.io/badge/Version-2.0.0-orange?style=flat-square" alt="Version"/>
  </p>
</div>

## 📋 Project Overview

**BrainStormEra** is a sophisticated web-based educational platform developed using modern .NET and Python technologies, designed to deliver comprehensive learning experiences for students, instructors, and administrators. The system implements a robust multi-layered architecture pattern, incorporating ASP.NET Core MVC, Razor Pages, Entity Framework Core, SQL Server, and AI-powered microservices to provide scalable and maintainable e-learning solutions.

### 🎯 Project Objectives

- **Educational Excellence**: Develop a modern, user-centric online learning platform that enhances educational accessibility and engagement
- **Scalable Architecture**: Implement enterprise-level software architecture patterns ensuring maintainability, testability, and scalability
- **Interactive Learning**: Provide dynamic course management, interactive lessons, and real-time assessment capabilities
- **AI Integration**: Incorporate intelligent chatbot assistance, course suggestions, and multimedia content analysis for 24/7 student support
- **Achievement System**: Design comprehensive progress tracking, certification management, and gamification elements
- **Real-time Communication**: Enable live notifications and interactive features using SignalR technology
- **Payment Processing**: Implement secure payment gateway integration with VNPay and points-based system
- **Multimedia Intelligence**: Provide OCR, video processing, and intelligent content analysis capabilities

### 🏗️ Academic Context

This project serves as a comprehensive demonstration of modern web application development principles, showcasing:

- **Software Engineering Best Practices**: Clean code architecture, SOLID principles, and design patterns
- **Full-Stack Development**: Integration of frontend and backend technologies in a cohesive system
- **Microservices Architecture**: Python FastAPI services for AI-powered features
- **Database Design**: Normalized relational database schema with Entity Framework Code-First approach
- **API Development**: RESTful services and real-time communication protocols
- **DevOps Practices**: Containerization, automated deployment, and continuous integration workflows
- **AI/ML Integration**: Natural language processing, computer vision, and intelligent recommendations

## ✨ Core Features & Functionality

### 🎓 **Learning Management System (LMS)**

- **Advanced Course Management**: CRUD operations for courses with metadata, prerequisites, and scheduling
- **Interactive Content Delivery**: Multi-media lesson content supporting video, audio, documents, and interactive elements
- **Structured Learning Paths**: Hierarchical chapter-lesson organization with dependency management
- **Assessment Engine**: Comprehensive quiz system with multiple question types, auto-grading, and detailed analytics
- **Progress Tracking**: Real-time learning analytics with completion rates and performance metrics
- **Sequential Learning**: Enforce lesson prerequisites and sequential access control
- **Course Approval Workflow**: Admin review and approval system for course publication

### 👥 **User Management & Authentication**

- **Secure Authentication**: JWT-based authentication with role-based authorization (RBAC)
- **Multi-Role Architecture**: Distinct user roles (Administrator, Instructor, Student) with granular permissions
- **Profile Management**: Comprehensive user profiles with learning history and preferences
- **Real-time Notifications**: SignalR-powered instant messaging and system notifications
- **Session Management**: Secure session handling with automatic timeout and refresh mechanisms
- **Points System**: Real-time points tracking with automatic refresh and transaction management
- **Account Security**: Brute force protection, password policies, and account lockout mechanisms

### 🤖 **AI-Powered Services & Virtual Assistant**

#### **Intelligent Chatbot Service** (FastAPI - Port 8000)
- **Google AI Integration**: Powered by Gemini Pro for natural language processing
- **Real-time Chat**: WebSocket-based communication with typing indicators and read receipts
- **Context-Aware Responses**: AI understands current page, course context, and user history
- **Conversation History**: Persistent chat history with feedback collection
- **Educational Optimization**: Tailored responses for learning assistance and course guidance
- **Multi-language Support**: Vietnamese and English language processing

#### **Course Suggestion AI** (FastAPI - Port 7000)
- **Gemini AI Analysis**: Deep user intent analysis and learning goal extraction
- **Smart Query Generation**: AI-enhanced keyword processing and search optimization
- **Dual Scoring System**: Combines traditional similarity + AI relevance scoring
- **Personalized Recommendations**: Skill level detection and technology mapping
- **Enhanced Analytics**: Detailed user analysis and course matching insights

#### **RAG Image Processing** (FastAPI - Port 8000)
- **OCR Text Extraction**: Vietnamese and English text recognition from images
- **Intelligent Q&A**: Ask questions about image content using natural language
- **Vector Search**: ChromaDB-powered similarity search for image content
- **Multi-format Support**: JPG, PNG, BMP, GIF, TIFF, WebP image processing
- **Semantic Understanding**: Sentence Transformers for intelligent content analysis

#### **RAG Video Processing** (FastAPI - Port 6767)
- **Local Video Processing**: Download and process YouTube videos locally
- **Whisper Integration**: OpenAI Whisper for high-accuracy transcription
- **Multi-language Support**: Vietnamese, English, and 50+ languages
- **Intelligent Q&A**: Ask questions about video content with source references
- **Storage Management**: Automatic cleanup and disk space monitoring
- **Processing Modes**: Local processing vs. transcript API fallback

### 💳 **Payment & E-commerce System**

- **VNPay Integration**: Secure payment gateway with PCI DSS compliance
- **Points-Based System**: Internal currency system for course enrollment
- **Transaction Management**: Comprehensive payment tracking and history
- **Top-up Functionality**: Account balance management with payment processing
- **Refund Processing**: Automated refund handling and transaction reversal
- **Payment Analytics**: Revenue tracking, transaction reporting, and financial insights
- **Multiple Payment Types**: Course enrollment, account top-up, and point purchases

### 🏆 **Achievement & Certification System**

- **Gamification Elements**: Badge system for learning milestones and engagement
- **Digital Certificates**: Automated certificate generation upon course completion
- **Progress Dashboard**: Comprehensive analytics dashboard with visual progress indicators
- **Performance Analytics**: Detailed reporting on learning outcomes and skill development
- **Streak Tracking**: Continuous learning rewards and motivation system
- **Achievement Sharing**: Social media integration for achievement sharing
- **Custom Achievement Creation**: Admin-configurable achievement criteria

### 📊 **Administrative Features**

- **Content Management**: Advanced CMS for course materials and platform content
- **User Analytics**: Comprehensive reporting on user engagement and platform usage
- **System Configuration**: Flexible platform settings and customization options
- **Data Export**: Reporting capabilities with multiple export formats (PDF, Excel, CSV)
- **Security Dashboard**: Real-time security monitoring and threat detection
- **Course Moderation**: Content review, approval, and quality control
- **Financial Management**: Revenue tracking, payout processing, and financial reporting

### 🔄 **Real-time Communication & Collaboration**

- **SignalR Hubs**: Real-time messaging, notifications, and live updates
- **Chat System**: User-to-user messaging with file sharing capabilities
- **Course Chat Rooms**: Course-specific discussion forums and Q&A sessions
- **Notification System**: Multi-channel notifications (in-app, email, real-time)
- **Online Status**: Real-time user presence and activity indicators
- **Message History**: Persistent chat history with search functionality
- **Moderation Tools**: Content filtering and user management features

### 📈 **Analytics & Reporting**

- **Learning Analytics**: Detailed progress tracking and performance metrics
- **User Engagement**: Comprehensive user behavior analysis and insights
- **Course Performance**: Instructor analytics and course effectiveness metrics
- **Financial Reports**: Revenue analysis, transaction reporting, and financial insights
- **System Monitoring**: Performance metrics, error tracking, and health monitoring
- **Custom Dashboards**: Role-specific analytics and reporting interfaces

## 🛠️ Technology Stack & Architecture

### **Backend Technologies**

- **ASP.NET Core 6.0**: Cross-platform web framework with high-performance HTTP pipeline
- **C# 10.0**: Primary programming language with modern language features
- **Entity Framework Core 6.0**: Object-Relational Mapping (ORM) with Code-First migrations
- **SignalR**: Real-time web functionality enabling bi-directional communication
- **AutoMapper**: Object-to-object mapping for clean data transformation
- **FluentValidation**: Business rule validation with fluent interface
- **Serilog**: Structured logging with multiple sinks and enrichers

### **AI Services (Python FastAPI)**

- **FastAPI**: High-performance Python web framework for AI services
- **Google AI (Gemini)**: Advanced language model for natural language processing
- **OpenAI Whisper**: Speech-to-text transcription for video processing
- **EasyOCR**: Optical Character Recognition for image text extraction
- **Sentence Transformers**: Semantic text embedding and similarity search
- **ChromaDB**: Vector database for similarity search and content retrieval
- **PyTorch**: Deep learning framework for AI model inference

### **Frontend Technologies**

- **Razor Pages & MVC Views**: Server-side rendering with strongly-typed models
- **HTML5/CSS3**: Modern web standards with semantic markup
- **JavaScript ES6+**: Modern JavaScript with async/await patterns
- **jQuery 3.6**: DOM manipulation and AJAX communication
- **Bootstrap 5**: Mobile-first responsive CSS framework
- **Chart.js**: Interactive data visualization and analytics charts
- **TinyMCE**: Rich text editor for content creation

### **Database & Data Access**

- **Microsoft SQL Server 2019**: Enterprise-grade relational database
- **Entity Framework Core**: Code-First approach with automatic migrations
- **LINQ**: Language-Integrated Query for type-safe data access
- **SQL Server Express LocalDB**: Development database instance

### **Security & Authentication**

- **ASP.NET Core Identity**: Comprehensive authentication and authorization system
- **JWT Tokens**: Stateless authentication for API endpoints
- **Role-Based Access Control (RBAC)**: Granular permission system
- **Data Protection API**: Secure data encryption and key management
- **HTTPS/TLS**: Encrypted communication protocols
- **VNPay Security**: PCI DSS compliant payment processing

### **DevOps & Deployment**

- **Docker & Docker Compose**: Containerized application deployment
- **GitHub Actions**: Continuous Integration/Continuous Deployment (CI/CD)
- **Azure DevOps**: Project management and release pipelines
- **IIS**: Internet Information Services for production deployment

### **Architecture Patterns & Principles**

- **Clean Architecture**: Dependency rule with domain-centric design
- **Repository Pattern**: Data access abstraction with unit of work
- **Dependency Injection**: IoC container for loose coupling
- **CQRS Pattern**: Command Query Responsibility Segregation for complex operations
- **Builder Pattern**: Complex object construction with fluent interface
- **Factory Pattern**: Object creation with abstraction layer
- **Microservices**: Decoupled AI services for scalability and maintainability

## 📁 Project Architecture & Structure

### **System Architecture Overview**

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
├─────────────────────┬───────────────────────────────────────┤
│   MVC Application   │         Razor Pages App               │
│   (Port 5216)       │         (Port 5274)                   │
└─────────────────────┴───────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                 Business Logic Layer                        │
├─────────────────────────────────────────────────────────────┤
│  Services │ Validators │ Managers │ Processors │ Handlers   │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                 Data Access Layer                           │
├─────────────────────────────────────────────────────────────┤
│  Repositories │ DbContext │ Entities │ Migrations │ UoW     │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                 Database Layer                              │
├─────────────────────────────────────────────────────────────┤
│              SQL Server 2019 (Port 1433)                    │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    AI Services Layer                        │
├─────────────────────┬───────────────────────────────────────┤
│   Chat Service      │      Suggestion AI Service            │
│   (Port 8000)       │         (Port 7000)                   │
├─────────────────────┼───────────────────────────────────────┤
│   RAG Image Service │      RAG Video Service                │
│   (Port 8000)       │         (Port 6767)                   │
└─────────────────────┴───────────────────────────────────────┘
```

### **Directory Structure**

```
BrainStormEra/
├── 📂 BrainStormEra-MVC/              # Primary MVC Web Application
│   ├── 📂 Controllers/                # MVC Controllers with action methods
│   │   ├── 🎯 HomeController.cs       # Landing page and navigation
│   │   ├── 🔐 AuthController.cs       # Authentication & authorization
│   │   ├── 📚 CourseController.cs     # Course management operations
│   │   ├── 📖 LessonController.cs     # Lesson content delivery
│   │   ├── ❓ QuizController.cs       # Assessment and evaluation
│   │   ├── 👤 ProfileController.cs    # User profile management
│   │   ├── 🤖 ChatbotController.cs    # AI assistant integration
│   │   ├── 💳 PaymentController.cs    # Payment processing
│   │   ├── 🏆 AchievementController.cs # Achievement system
│   │   └── 📊 AdminController.cs      # Administrative functions
│   ├── 📂 Models/                     # ViewModels and DTOs
│   ├── 📂 Views/                      # Razor view templates
│   ├── 📂 Filters/                    # Action and authorization filters
│   ├── 📂 Middlewares/                # Custom middleware components
│   └── 📂 wwwroot/                    # Static assets (CSS, JS, images)
├── 📂 BrainStormEra-Razor/            # Alternative Razor Pages Implementation
├── 📂 BusinessLogicLayer/             # Domain Logic and Business Rules
│   ├── 📂 Services/                   # Business service implementations
│   │   ├── 📂 Implementations/        # Service implementations
│   │   └── 📂 Interfaces/             # Service contracts and abstractions
│   ├── 📂 Validators/                 # Business rule validation
│   ├── 📂 DTOs/                       # Data Transfer Objects
│   ├── 📂 Hubs/                       # SignalR hubs for real-time communication
│   └── 📂 Utilities/                  # Helper classes and utilities
├── 📂 DataAccessLayer/                # Data Persistence Layer
│   ├── 📂 Models/                     # Entity Framework models
│   ├── 📂 Data/                       # Database context configurations
│   ├── 📂 Repositories/               # Data access repositories
│   │   ├── 📂 Interfaces/             # Repository contracts
│   │   └── 📂 Implementations/        # Repository implementations
│   └── 📂 Migrations/                 # Database schema migrations
├── 📂 ai-services/                    # AI-Powered Microservices
│   ├── 📂 chat/                       # AI Chatbot Service (FastAPI)
│   │   ├── 📂 app/                    # FastAPI application structure
│   │   ├── 📂 services/               # Chat and chatbot services
│   │   ├── 📂 websockets/             # Real-time communication
│   │   └── 📂 models/                 # Pydantic models
│   ├── 📂 suggestionAI/               # Course Suggestion AI (FastAPI)
│   │   ├── 📂 app/                    # FastAPI application
│   │   ├── 📂 services/               # AI suggestion services
│   │   └── 📂 models/                 # Data models
│   ├── 📂 RAG-img/                    # Image Processing RAG (FastAPI)
│   │   ├── 📂 services/               # OCR and embedding services
│   │   ├── 📂 utils/                  # Utility functions
│   │   └── 📂 uploads/                # Image storage
│   └── 📂 RAG-vid/                    # Video Processing RAG (FastAPI)
│       ├── 📂 services/               # Video processing services
│       ├── 📂 downloads/              # Video storage
│       └── 📂 data/                   # Processed data
├── 📂 init_script/                    # Database initialization scripts
├── 📂 docs/                           # Project documentation
├── 📂 SharedMedia/                    # Shared media assets
├── 📄 docker-compose.yml             # Multi-container orchestration
├── 📄 BrainStormEra.sln              # Visual Studio solution file
└── 📄 README.md                      # Project documentation
```

## 🚀 Installation & Setup Guide

### **System Requirements**

- **Operating System**: Windows 10/11, macOS 10.15+, or Linux Ubuntu 18.04+
- **Docker Desktop**: Latest stable version (4.0+)
- **.NET 6.0 SDK**: Long-term support version
- **Python 3.8+**: For AI services
- **SQL Server**: 2019 or later (Express/Developer/Standard editions)
- **IDE**: Visual Studio 2022, JetBrains Rider, or Visual Studio Code
- **Memory**: Minimum 8GB RAM (16GB recommended for optimal performance)
- **Storage**: At least 10GB free disk space (for video processing)
- **Network**: Stable internet connection for AI model downloads

### **Development Environment Setup**

#### **1. Repository Clone & Initial Setup**

```bash
# Clone the repository
git clone https://github.com/LTPPPP/BrainStormEra-v2.git
cd BrainStormEra

# Verify .NET SDK installation
dotnet --version

# Verify Python installation
python --version

# Restore NuGet packages for all projects
dotnet restore BrainStormEra.sln
```

#### **2. Docker-based Deployment (Recommended)**

```bash
# Build and start all services in development mode
docker-compose up --build

# Run in detached mode (background)
docker-compose up -d --build

# View logs for specific service
docker-compose logs -f mvc
docker-compose logs -f chat-service
docker-compose logs -f suggestion-service
docker-compose logs -f rag-img-service
docker-compose logs -f rag-vid-service

# Stop all services
docker-compose down

# Remove volumes (reset database)
docker-compose down -v
```

#### **3. Local Development Setup (Without Docker)**

```bash
# Install Entity Framework CLI tools
dotnet tool install --global dotnet-ef

# Navigate to DataAccessLayer
cd DataAccessLayer

# Create initial migration (if not exists)
dotnet ef migrations add InitialCreate

# Update database schema
dotnet ef database update

# Return to solution root
cd ..

# Start MVC application
cd BrainStormEra-MVC
dotnet run --urls="https://localhost:5001;http://localhost:5000"

# In a new terminal, start Razor application
cd BrainStormEra-Razor
dotnet run --urls="https://localhost:5003;http://localhost:5002"

# Start AI Services (in separate terminals)
cd ai-services/chat
python main.py

cd ai-services/suggestionAI
python run.py

cd ai-services/RAG-img
python main.py

cd ai-services/RAG-vid
python main.py
```

### **Application Access Points**

- **🌐 MVC Application**: [https://localhost:5216](https://localhost:5216) (HTTPS) | [http://localhost:5000](http://localhost:5000) (HTTP)
- **🌐 Razor Application**: [https://localhost:5274](https://localhost:5274) (HTTPS) | [http://localhost:5002](http://localhost:5002) (HTTP)
- **🤖 AI Chat Service**: [http://localhost:8000](http://localhost:8000) | [http://localhost:8000/docs](http://localhost:8000/docs) (Swagger)
- **🎯 Suggestion AI Service**: [http://localhost:7000](http://localhost:7000) | [http://localhost:7000/docs](http://localhost:7000/docs) (Swagger)
- **🖼️ RAG Image Service**: [http://localhost:8000](http://localhost:8000) | [http://localhost:8000/docs](http://localhost:8000/docs) (Swagger)
- **🎥 RAG Video Service**: [http://localhost:6767](http://localhost:6767) | [http://localhost:6767/docs](http://localhost:6767/docs) (Swagger)
- **🗄️ SQL Server**: `localhost:1433` (Docker) | `(localdb)\MSSQLLocalDB` (Local)
- **📊 Database Management**: Use SQL Server Management Studio (SSMS) or Azure Data Studio

### **Database Configuration**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BrainStormEra;User Id=sa;Password=YourStrong!Password;TrustServerCertificate=true;",
    "LocalConnection": "Server=(localdb)\\MSSQLLocalDB;Database=BrainStormEra;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

## ⚙️ Configuration & Environment Setup

### **Environment Variables**

```env
# Application Environment
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=https://localhost:5001;http://localhost:5000

# Database Configuration
SA_PASSWORD=YourStrong!Password
ACCEPT_EULA=Y
DATABASE_CONNECTION_STRING=Server=localhost,1433;Database=BrainStormEra;User Id=sa;Password=YourStrong!Password;TrustServerCertificate=true;

# Security Settings
JWT_SECRET_KEY=your-super-secure-secret-key-here-with-at-least-32-characters
JWT_EXPIRE_HOURS=24
ENCRYPTION_KEY=your-encryption-key-for-sensitive-data

# Payment Gateway (VNPay)
VNPAY_TMN_CODE=your_vnpay_tmn_code
VNPAY_HASH_SECRET=your_vnpay_hash_secret
VNPAY_BASE_URL=https://sandbox.vnpayment.vn/paymentv2/vpcpay.html

# AI Services Configuration
GOOGLE_AI_API_KEY=your_google_ai_api_key_here
GEMINI_API_KEY=your_gemini_api_key_here

# External Services
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-password
```

### **AI Services Configuration**

#### **Chat Service Configuration**
```python
# ai-services/chat/config.py
DATABASE_URL = "mssql+aiodbc://sa:YourPassword@localhost/BrainStormEra?driver=ODBC+Driver+17+for+SQL+Server"
GOOGLE_AI_API_KEY = "your_google_ai_api_key_here"
SECRET_KEY = "your_secret_key_here"
```

#### **Suggestion AI Configuration**
```python
# ai-services/suggestionAI/.env
DATABASE_URL=mssql+pyodbc://sa:YourPassword@localhost/BrainStormEra?driver=ODBC+Driver+17+for+SQL+Server
GEMINI_API_KEY=your_gemini_api_key_here
USE_GEMINI=true
```

#### **RAG Services Configuration**
```json
// ai-services/RAG-img/config.json
{
  "upload_dir": "uploads",
  "max_file_size": 10485760,
  "supported_formats": [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp"],
  "ocr_languages": ["vi", "en"],
  "embedding_model": "sentence-transformers/all-MiniLM-L6-v2"
}
```

### **Configuration Files Structure**

```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=BrainStormEra;User Id=sa;Password=YourStrong!Password;TrustServerCertificate=true;",
    "LocalConnection": "Server=(localdb)\\MSSQLLocalDB;Database=BrainStormEra;Trusted_Connection=true;"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key",
    "Issuer": "BrainStormEra",
    "Audience": "BrainStormEra-Users",
    "ExpiryInHours": 24
  },
  "VNPaySettings": {
    "TmnCode": "your_vnpay_tmn_code",
    "HashSecret": "your_vnpay_hash_secret",
    "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

## 👥 User Guide & Workflow Documentation

### **🎓 Student Workflow**

1. **Account Registration**:
   - Complete profile setup with academic information
   - Email verification and account activation
   - Initial skills assessment and learning preferences
2. **Course Discovery**:
   - Browse categorized course catalog with filtering options
   - Use AI-powered course suggestions based on interests
   - View course previews, syllabi, and instructor profiles
   - Read reviews and ratings from other students
3. **Enrollment Process**:
   - Select desired courses and add to learning path
   - Configure learning schedule and notification preferences
   - Process payment using VNPay or points system
   - Access course materials and supplementary resources
4. **Learning Experience**:
   - Progress through structured lessons and chapters
   - Participate in interactive content and multimedia lessons
   - Use AI chatbot for real-time learning assistance
   - Track learning analytics and performance metrics
5. **Assessment & Evaluation**:
   - Complete chapter quizzes and comprehensive exams
   - Receive immediate feedback and detailed explanations
   - Retake assessments to improve understanding
6. **Certification & Achievement**:
   - Earn digital badges for completed milestones
   - Generate completion certificates with verification codes
   - Build comprehensive learning portfolio
   - Share achievements on social media

### **🧑‍🏫 Instructor Workflow**

1. **Professional Onboarding**:
   - Submit credentials and teaching experience verification
   - Complete instructor training and platform orientation
   - Set up instructor profile and teaching preferences
2. **Course Development**:
   - Create comprehensive course outlines and learning objectives
   - Develop multimedia content using integrated authoring tools
   - Organize content into logical chapters and learning modules
   - Use AI tools for content optimization and suggestions
3. **Content Management**:
   - Upload and organize diverse learning materials (videos, documents, presentations)
   - Create interactive elements and knowledge check points
   - Configure course prerequisites and learning paths
   - Use RAG services for content analysis and enhancement
4. **Assessment Creation**:
   - Design varied assessment types (multiple choice, essay, practical)
   - Set up automated grading rules and rubrics
   - Create personalized feedback templates
5. **Student Interaction**:
   - Monitor student progress through analytics dashboard
   - Provide personalized feedback and guidance
   - Conduct virtual office hours and Q&A sessions
   - Use AI chatbot for automated student support
6. **Performance Analysis**:
   - Analyze course effectiveness through detailed metrics
   - Generate reports on student outcomes and engagement
   - Continuously improve content based on feedback
   - Track revenue and financial performance

### **🔧 Administrator Workflow**

1. **System Administration**:
   - Manage user accounts, roles, and permissions
   - Configure system-wide settings and policies
   - Monitor platform performance and resource utilization
   - Manage AI services and external integrations
2. **Content Governance**:
   - Review and approve course content before publication
   - Ensure quality standards and educational guidelines compliance
   - Manage content libraries and shared resources
   - Monitor AI-generated content and suggestions
3. **Analytics & Reporting**:
   - Generate comprehensive platform usage reports
   - Analyze user engagement and learning outcomes
   - Monitor financial metrics and enrollment trends
   - Track AI service performance and usage
4. **Technical Maintenance**:
   - Perform regular system backups and data integrity checks
   - Manage database optimization and performance tuning
   - Coordinate with development team for updates and enhancements
   - Monitor AI model performance and updates
5. **Support & Communication**:
   - Handle escalated user support requests
   - Communicate platform updates and announcements
   - Manage stakeholder relationships and feedback integration
   - Oversee AI chatbot training and optimization

### **🤖 AI Services Usage**

#### **Chatbot Interaction**
- **24/7 Support**: Access AI assistant anytime for learning help
- **Context Awareness**: AI understands current course and page context
- **Natural Language**: Ask questions in natural language
- **Learning Guidance**: Get personalized study recommendations
- **Feedback System**: Rate AI responses to improve accuracy

#### **Course Suggestions**
- **Smart Recommendations**: AI analyzes user interests and goals
- **Skill Level Detection**: Automatic identification of user expertise level
- **Technology Mapping**: AI suggests relevant technologies and frameworks
- **Enhanced Search**: Improved course discovery with AI-powered keywords

#### **Content Analysis**
- **Image Processing**: Upload images and ask questions about content
- **Video Analysis**: Process YouTube videos and extract key information
- **Document Understanding**: OCR and text extraction from various formats
- **Semantic Search**: Find relevant content using natural language queries

## 🔧 Development Guide & Best Practices

### **Database Management**

```bash
# Entity Framework Commands
dotnet ef migrations add MigrationName --project DataAccessLayer --startup-project BrainStormEra-MVC
dotnet ef database update --project DataAccessLayer --startup-project BrainStormEra-MVC
dotnet ef migrations remove --project DataAccessLayer --startup-project BrainStormEra-MVC

# Database Seeding
dotnet run --project BrainStormEra-MVC --seed-data

# Generate SQL Scripts
dotnet ef migrations script --project DataAccessLayer --startup-project BrainStormEra-MVC
```

### **AI Services Development**

```bash
# Chat Service Development
cd ai-services/chat
pip install -r requirements.txt
python main.py

# Suggestion AI Development
cd ai-services/suggestionAI
pip install -r requirements.txt
python run.py

# RAG Image Service Development
cd ai-services/RAG-img
pip install -r requirements.txt
python main.py

# RAG Video Service Development
cd ai-services/RAG-vid
pip install -r requirements.txt
python main.py
```

### **Build & Testing Commands**

```bash
# Clean and rebuild solution
dotnet clean BrainStormEra.sln
dotnet build BrainStormEra.sln --configuration Release

# Run unit tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory TestResults/

# Package for deployment
dotnet publish BrainStormEra-MVC -c Release -o ./publish
```

### **Code Quality & Analysis**

```bash
# Code formatting
dotnet format BrainStormEra.sln

# Security analysis
dotnet list package --vulnerable
dotnet list package --outdated

# Performance profiling
dotnet run --project BrainStormEra-MVC --configuration Release --no-build
```

## 🤝 Contributing Guidelines

### **Development Workflow**

1. **Fork Repository**: Create a personal fork of the main repository
2. **Create Feature Branch**: `git checkout -b feature/descriptive-feature-name`
3. **Implement Changes**: Follow coding standards and write comprehensive tests
4. **Commit Guidelines**: Use conventional commits (feat:, fix:, docs:, style:, refactor:, test:, chore:)
5. **Pull Request**: Submit PR with detailed description and reference to issues
6. **Code Review**: Address feedback and ensure CI/CD pipeline passes

### **Coding Standards & Best Practices**

- **C# Conventions**: Follow Microsoft C# Coding Conventions and StyleCop rules
- **Python Conventions**: Follow PEP 8 style guide for Python code
- **Naming Standards**: Use PascalCase for public members, camelCase for private fields
- **Documentation**: XML documentation comments for public APIs
- **Testing**: Minimum 80% code coverage with unit and integration tests
- **Security**: Follow OWASP security guidelines and dependency scanning
- **Performance**: Implement async/await patterns for I/O operations
- **Architecture**: Maintain separation of concerns and SOLID principles

### **Project Requirements**

- All new features must include comprehensive unit tests
- Integration tests for controller actions and business logic
- Database migrations with both up and down methods
- API documentation using Swagger/OpenAPI specifications
- Performance benchmarks for critical path operations
- AI service integration testing and validation

## 📄 License & Legal Information

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for complete details.

### **Third-Party Libraries**

- ASP.NET Core (Apache 2.0)
- Entity Framework Core (Apache 2.0)
- FastAPI (MIT)
- Google AI (Gemini) (Google Terms of Service)
- OpenAI Whisper (MIT)
- Bootstrap (MIT)
- jQuery (MIT)
- Chart.js (MIT)

### **Academic Use Policy**

This project is developed for educational purposes and academic research. Commercial use requires explicit permission from the development team.

## 📞 Support & Community

### **Development Team**

- **Project Lead**: [GitHub Profile](https://github.com/LTPPPP)
- **Email Support**: brainstormera.pro@gmail.com
- **Repository**: [BrainStormEra-v2](https://github.com/LTPPPP/BrainStormEra-v2)

### **Issue Reporting & Feature Requests**

- **Bug Reports**: Use GitHub Issues with bug report template
- **Feature Requests**: Submit enhancement proposals with detailed specifications
- **Security Issues**: Report privately to brainstormera.security@gmail.com
- **Documentation**: Contribute to wiki and documentation improvements

### **Community Engagement**

- **Discussions**: GitHub Discussions for general questions and ideas
- **Code of Conduct**: Please read and follow our community guidelines
- **Contributing Guide**: Detailed contribution instructions in CONTRIBUTING.md

### **Academic Collaboration**

- **Research Partnerships**: Open to academic collaboration and research projects
- **Student Projects**: Welcome contributions from computer science students
- **Thesis Support**: Available for undergraduate and graduate thesis projects

---

<div align="center">
  <h3>🎓 Academic Project - BrainStormEra E-Learning Platform</h3>
  <p><strong>Demonstrating Modern Web Development Practices & Educational Technology Innovation with AI Integration</strong></p>
  
<p>
   <a href="https://github.com/LTPPPP/BrainStormEra-v2/stargazers">
      <img src="https://img.shields.io/github/stars/LTPPPP/BrainStormEra-v2?style=social" alt="GitHub Stars"/>
   </a>
   <a href="https://github.com/LTPPPP/BrainStormEra-v2/network/members">
      <img src="https://img.shields.io/github/forks/LTPPPP/BrainStormEra-v2?style=social" alt="GitHub Forks"/>
   </a>
   <a href="https://github.com/LTPPPP/BrainStormEra-v2/watchers">
      <img src="https://img.shields.io/github/watchers/LTPPPP/BrainStormEra-v2?style=social" alt="GitHub Watchers"/>
   </a>
</p>
  <p>🌟 <strong>If this project helps your learning journey, please give us a star!</strong> 🌟</p>
  <p>Built with ❤️ by Computer Science Students | Powered by .NET 6.0, Python FastAPI & Modern AI Technologies</p>
</div>
