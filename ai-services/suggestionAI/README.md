# Course Suggestion AI Service

Dịch vụ AI gợi ý khóa học dựa trên mô tả của người dùng sử dụng FastAPI.

## Tính năng

- **🧠 Gemini AI Integration**: Tích hợp Google Gemini AI cho xử lý ngôn ngữ tự nhiên
- **📊 Phân tích ý định người dùng**: AI phân tích sâu nhu cầu học tập và mục tiêu
- **🔍 Trích xuất từ khóa nâng cao**: Gemini mở rộng và cải thiện từ khóa tìm kiếm
- **🎯 Smart Query Generation**: Tạo câu query tối ưu dựa trên phân tích AI
- **⚖️ Dual Scoring System**: Kết hợp traditional similarity + Gemini relevance
- **💡 Gợi ý thông minh**: Đánh giá độ phù hợp dựa trên nhiều yếu tố AI
- **🔄 Fallback Support**: Hoạt động ngay cả khi Gemini không khả dụng
- **📈 Enhanced Analytics**: Cung cấp insights chi tiết về người dùng và khóa học

## Cài đặt

### 🚀 Quick Setup (Khuyến nghị)

```bash
cd ai-services/suggestionAI
python setup.py
```

Script sẽ tự động:
- ✅ Tạo file `.env` từ template
- ✅ Kiểm tra dependencies
- ✅ Hướng dẫn setup Gemini API key

### 📖 Manual Setup

1. **Cài đặt Dependencies:**
```bash
cd ai-services/suggestionAI
pip install -r requirements.txt
```

2. **Tạo Environment file:**
```bash
cp .env.template .env
```

3. **Cấu hình Database và AI:**
Edit file `.env`:
```env
DATABASE_URL=mssql+pyodbc://sa:01654460072ltp@LTPP/BrainStormEra?driver=ODBC+Driver+17+for+SQL+Server&TrustServerCertificate=yes
HOST=0.0.0.0
PORT=7000
MAX_SUGGESTIONS=10
MIN_SIMILARITY_SCORE=0.1

# Gemini AI Configuration
GEMINI_API_KEY=your_gemini_api_key_here
GEMINI_MODEL=gemini-pro
USE_GEMINI=true
```

📖 **Xem [SETUP.md](SETUP.md) để biết hướng dẫn chi tiết và troubleshooting.**

### 3. Cài đặt ODBC Driver

**Windows:**
- Tải và cài đặt [Microsoft ODBC Driver 17 for SQL Server](https://docs.microsoft.com/en-us/sql/connect/odbc/download-odbc-driver-for-sql-server)

**Linux:**
```bash
curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
curl https://packages.microsoft.com/config/ubuntu/20.04/prod.list | sudo tee /etc/apt/sources.list.d/mssql-release.list
sudo apt-get update
sudo ACCEPT_EULA=Y apt-get install -y msodbcsql17
```

### 4. Cấu hình Gemini AI

**Lấy API Key:**
1. Truy cập [Google AI Studio](https://makersuite.google.com/)
2. Đăng nhập với tài khoản Google
3. Tạo API key mới
4. Copy API key vào file `.env`

**Hoặc sử dụng Google Cloud:**
1. Tạo project tại [Google Cloud Console](https://console.cloud.google.com/)
2. Enable Generative AI API
3. Tạo service account và tải credentials
4. Set environment variable `GOOGLE_APPLICATION_CREDENTIALS`

## Chạy Service

```bash
cd ai-services/suggestionAI
python run.py
```

Hoặc sử dụng uvicorn:

```bash
uvicorn app.main:app --host 0.0.0.0 --port 7000 --reload
```

## API Endpoints

### 1. Health Check
```
GET /health
```
Response bao gồm thông tin về Gemini AI status.

### 2. Gemini Status
```
GET /gemini-status
```
Kiểm tra trạng thái hoạt động của Gemini AI.

### 3. Gợi ý khóa học (Legacy)
```
POST /suggest-courses
Content-Type: application/json

{
    "description": "Tôi muốn học lập trình Python cho người mới bắt đầu"
}
```

### 4. Gợi ý khóa học nâng cao (Gemini AI)
```
POST /suggest-courses-enhanced
Content-Type: application/json

{
    "description": "Tôi muốn học lập trình Python cho người mới bắt đầu để làm web development"
}
```

**Enhanced Response:**
```json
{
    "user_analysis": {
        "main_subjects": ["Python", "Web Development"],
        "skill_level": "beginner",
        "learning_goals": ["Học lập trình cơ bản", "Phát triển web"],
        "keywords": ["python", "lập trình", "web development", "người mới"],
        "course_type_preference": "practical",
        "technologies": ["Python", "HTML", "CSS"],
        "career_focus": "development",
        "urgency": "medium"
    },
    "enhanced_keywords": ["python", "web", "development", "flask", "django", "backend"],
    "smart_query": "Python web development cơ bản",
    "suggestions": [
        {
            "course_id": "course-123",
            "course_name": "Python Web Development cơ bản",
            "course_description": "Học lập trình web với Python...",
            "author_name": "Nguyễn Văn A",
            "categories": ["Lập trình", "Python", "Web"],
            "chapters": ["Python cơ bản", "Flask framework", "Database"],
            "created_at": "2024-01-15T10:30:00",
            "similarity_score": 0.92,
            "match_reasons": ["Phù hợp skill level", "Chủ đề Python"],
            "gemini_relevance_score": 0.88,
            "gemini_match_points": ["Phù hợp mục tiêu học web", "Cấp độ beginner"]
        }
    ]
}
```

## Thuật toán AI với Gemini

Service sử dụng hybrid AI system kết hợp traditional NLP và Gemini AI:

### 🧠 Phase 1: Gemini Intent Analysis
1. **User Intent Recognition**: Gemini phân tích mô tả để hiểu ý định học tập
2. **Skill Level Detection**: Tự động nhận diện level (beginner/intermediate/advanced)
3. **Goal Extraction**: Trích xuất mục tiêu học tập cụ thể
4. **Technology Mapping**: Xác định công nghệ và framework liên quan

### 🔍 Phase 2: Enhanced Keyword Processing
1. **Basic Extraction**: Loại bỏ stop words, trích xuất từ khóa cơ bản
2. **Gemini Enhancement**: Mở rộng từ khóa với synonyms và technical terms
3. **Smart Query Generation**: Tạo optimized search query từ analysis

### ⚖️ Phase 3: Dual Scoring System
**Traditional Similarity (70%)**:
- Tên khóa học (30%)
- Mô tả khóa học (40%) 
- Keyword matching (20%)
- Category matching (5%)
- Chapter matching (5%)

**Gemini Relevance (30%)**:
- Intent alignment score
- Skill level compatibility
- Goal-course mapping
- Technology stack matching

### 🎯 Phase 4: Final Ranking
1. **Score Combination**: Kết hợp traditional + Gemini scores
2. **Smart Filtering**: Loại bỏ courses không phù hợp
3. **Contextual Ranking**: Sắp xếp theo relevance tổng thể
4. **Top-K Selection**: Trả về 10 kết quả tốt nhất

## Ví dụ sử dụng

### cURL

**Legacy endpoint:**
```bash
curl -X POST "http://localhost:7000/suggest-courses" \
-H "Content-Type: application/json" \
-d '{"description": "Tôi muốn học thiết kế web responsive với HTML CSS"}'
```

**Enhanced endpoint với Gemini:**
```bash
curl -X POST "http://localhost:7000/suggest-courses-enhanced" \
-H "Content-Type: application/json" \
-d '{"description": "Tôi muốn học lập trình Python cho người mới bắt đầu để làm web development"}'
```

### Python

**Enhanced suggestions:**
```python
import requests

response = requests.post(
    "http://localhost:7000/suggest-courses-enhanced",
    json={"description": "Tôi muốn học machine learning với Python"}
)

data = response.json()

# Display user analysis
analysis = data['user_analysis']
print(f"Skill Level: {analysis['skill_level']}")
print(f"Main Subjects: {analysis['main_subjects']}")
print(f"Technologies: {analysis['technologies']}")
print(f"Enhanced Keywords: {data['enhanced_keywords']}")
print(f"Smart Query: {data['smart_query']}")
print("---")

# Display suggestions
for course in data['suggestions']:
    print(f"Khóa học: {course['course_name']}")
    print(f"Traditional Score: {course['similarity_score']}")
    if course.get('gemini_relevance_score'):
        print(f"Gemini Relevance: {course['gemini_relevance_score']}")
    print(f"Match Reasons: {', '.join(course['match_reasons'])}")
    if course.get('gemini_match_points'):
        print(f"Gemini Points: {', '.join(course['gemini_match_points'])}")
    print("---")
```

### JavaScript

**Enhanced suggestions:**
```javascript
fetch('http://localhost:7000/suggest-courses-enhanced', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
    },
    body: JSON.stringify({
        description: 'Tôi muốn học React.js và Node.js để làm full-stack developer'
    })
})
.then(response => response.json())
.then(data => {
    // Display user analysis
    const analysis = data.user_analysis;
    console.log(`Skill Level: ${analysis.skill_level}`);
    console.log(`Main Subjects: ${analysis.main_subjects.join(', ')}`);
    console.log(`Enhanced Keywords: ${data.enhanced_keywords.join(', ')}`);
    console.log(`Smart Query: ${data.smart_query}`);
    console.log('---');
    
    // Display suggestions
    data.suggestions.forEach(course => {
        console.log(`Khóa học: ${course.course_name}`);
        console.log(`Traditional Score: ${course.similarity_score}`);
        if (course.gemini_relevance_score) {
            console.log(`Gemini Relevance: ${course.gemini_relevance_score}`);
        }
        console.log(`Match Reasons: ${course.match_reasons.join(', ')}`);
        if (course.gemini_match_points) {
            console.log(`Gemini Points: ${course.gemini_match_points.join(', ')}`);
        }
        console.log('---');
    });
});
```

## Swagger Documentation

Khi service đang chạy, truy cập:
- Swagger UI: http://localhost:7000/docs
- ReDoc: http://localhost:7000/redoc

## Lưu ý

1. **Database Connection**: Đảm bảo connection string đúng và có quyền truy cập database
2. **Encoding**: Service hỗ trợ tiếng Việt có dấu
3. **Performance**: Service có thể cache kết quả để tăng tốc độ
4. **Security**: Chỉ đọc dữ liệu, không có quyền ghi
5. **Error Handling**: Service có fallback query khi main query lỗi

## Troubleshooting

### Lỗi kết nối database
- Kiểm tra connection string trong `.env`
- Đảm bảo SQL Server đang chạy
- Kiểm tra firewall và network

### Lỗi ODBC Driver
- Cài đặt đúng version ODBC Driver 17
- Kiểm tra driver có trong system PATH

### Lỗi encoding
- Đảm bảo database collation hỗ trợ Unicode
- Kiểm tra encoding của text input

## Tích hợp với MVC

Service có thể được gọi từ MVC application:

### Enhanced Suggestions với Gemini:
```csharp
public async Task<EnhancedSuggestionResponse> GetEnhancedCourseSuggestions(string description)
{
    using var client = new HttpClient();
    var request = new { description = description };
    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    
    var response = await client.PostAsync("http://localhost:7000/suggest-courses-enhanced", content);
    var result = await response.Content.ReadAsStringAsync();
    
    return JsonSerializer.Deserialize<EnhancedSuggestionResponse>(result);
}
```

### Legacy Support:
```csharp
public async Task<List<CourseSuggestion>> GetCourseSuggestions(string description)
{
    using var client = new HttpClient();
    var request = new { description = description };
    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    
    var response = await client.PostAsync("http://localhost:7000/suggest-courses", content);
    var result = await response.Content.ReadAsStringAsync();
    
    return JsonSerializer.Deserialize<List<CourseSuggestion>>(result);
}
``` 