# HashidsService Documentation

## Overview

HashidsService là một service trong BusinessLogicLayer giúp mã hóa và giải mã ID số thành các chuỗi hash ngắn, thân thiện với URL. Service này sử dụng thư viện Hashids.net để tạo ra các hash có thể đảo ngược.

## Cài đặt

Service đã được thêm vào BusinessLogicLayer với các thành phần sau:

- **IHashidsService**: Interface định nghĩa các phương thức
- **HashidsService**: Implementation của interface
- **HashidsServiceExtensions**: Extension để đăng ký service vào DI
- **HashidsHelper**: Utility helper với các phương thức safe
- **HashidsConstants**: Constants cho cấu hình

## Cấu hình

Thêm vào appsettings.json:

```json
{
  "Hashids": {
    "Salt": "YourCustomSalt_2024",
    "MinLength": 8,
    "Alphabet": "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890"
  }
}
```

## Đăng ký Service

Trong Program.cs hoặc Startup.cs:

```csharp
using BusinessLogicLayer.Extensions;

// Đăng ký HashidsService
builder.Services.AddHashidsServices();
```

## Sử dụng

### 1. Inject service vào controller/service

```csharp
public class CourseController : Controller
{
    private readonly IHashidsService _hashidsService;

    public CourseController(IHashidsService hashidsService)
    {
        _hashidsService = hashidsService;
    }
}
```

### 2. Mã hóa ID

```csharp
// Mã hóa ID đơn
int courseId = 123;
string hash = _hashidsService.Encode(courseId);
// Kết quả: "jR3k5N8m"

// Mã hóa nhiều ID
string hash = _hashidsService.Encode(123, 456, 789);

// Mã hóa long ID
long bigId = 9876543210;
string hash = _hashidsService.EncodeLong(bigId);
```

### 3. Giải mã hash

```csharp
// Giải mã thành ID đơn
string hash = "jR3k5N8m";
int id = _hashidsService.DecodeSingle(hash);
// Kết quả: 123

// Giải mã thành mảng ID
int[] ids = _hashidsService.Decode(hash);

// Giải mã thành long ID
long longId = _hashidsService.DecodeLong(hash);
```

### 4. Kiểm tra hash hợp lệ

```csharp
bool isValid = _hashidsService.IsValidHash("jR3k5N8m");
```

### 5. Sử dụng Helper Methods

```csharp
using BusinessLogicLayer.Utilities;

// Safe encode/decode với fallback
string hash = HashidsHelper.SafeEncode(_hashidsService, 123, "default");
int id = HashidsHelper.SafeDecode(_hashidsService, hash, 0);

// Tạo URL hash với prefix
string urlHash = HashidsHelper.CreateUrlHash(_hashidsService, 123, "course");
// Kết quả: "course-jR3k5N8m"

// Extract ID từ URL hash
int extractedId = HashidsHelper.ExtractIdFromUrlHash(_hashidsService, "course-jR3k5N8m", "course");

// Try decode pattern
if (HashidsHelper.TryDecode(_hashidsService, hash, out int decodedId))
{
    // Sử dụng decodedId
}
```

## Ví dụ thực tế

### Trong Controller

```csharp
[HttpGet("course/{hash}")]
public async Task<IActionResult> GetCourse(string hash)
{
    if (!_hashidsService.IsValidHash(hash))
        return BadRequest("Invalid course identifier");

    int courseId = _hashidsService.DecodeSingle(hash);
    if (courseId == 0)
        return NotFound();

    var course = await _courseService.GetByIdAsync(courseId);
    return View(course);
}

[HttpPost("enroll/{hash}")]
public async Task<IActionResult> EnrollCourse(string hash)
{
    int courseId = HashidsHelper.SafeDecode(_hashidsService, hash);
    if (courseId == 0)
        return BadRequest();

    // Logic enroll course
    return RedirectToAction("Index");
}
```

### Trong View

```html
<!-- Tạo link với hash -->
<a href="/course/@_hashidsService.Encode(course.Id)">
    @course.Title
</a>

<!-- Hoặc sử dụng Helper -->
<a href="/course/@HashidsHelper.CreateUrlHash(_hashidsService, course.Id, "course")">
    @course.Title
</a>
```

## Lợi ích

1. **Bảo mật**: Che giấu ID thực của database
2. **Thân thiện URL**: Hash ngắn, dễ đọc
3. **Khả năng đảo ngược**: Có thể decode về ID gốc
4. **Tránh enumeration attack**: Không thể đoán được ID tiếp theo
5. **Consistent**: Cùng một ID luôn tạo ra cùng một hash

## Lưu ý

- Salt nên được giữ bí mật và khác nhau cho mỗi environment
- MinLength ảnh hưởng đến độ dài hash tối thiểu
- Alphabet có thể customize để tránh các ký tự nhạy cảm
- Service được đăng ký dưới dạng Singleton để tối ưu performance
