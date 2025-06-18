# URL Hash Service Migration Guide - SHA256 to AES

## Tổng quan

Hệ thống đã được nâng cấp từ SHA256 (one-way hash) sang AES-256-CBC (two-way encryption) để có thể mã hóa và giải mã ngược lại các ID trong URL.

## Thay đổi chính

### 1. **UrlHashServiceImproved** - AES Encryption Service

```csharp
// OLD: SHA256 (không thể giải mã ngược)
string hash = sha256.ComputeHash(input); // Không thể lấy lại input

// NEW: AES-256-CBC (có thể giải mã ngược)
string encrypted = aes.Encrypt(realId);   // Có thể lấy lại realId
string decrypted = aes.Decrypt(encrypted); // realId == decrypted
```

### 2. **QueryHashService** - Smart Query Helper

Service mới giúp thực hiện các truy vấn một cách thông minh với ID được mã hóa.

## Cách sử dụng

### 1. Cấu hình appsettings.json

```json
{
  "UrlHashSettings": {
    "SecretKey": "BrainStormEra-ProductionSecretKey-2024",
    "Salt": "BrainStorm-ProductionSalt-2024"
  }
}
```

### 2. Dependency Injection

```csharp
// Trong Program.cs
builder.Services.AddScoped<IUrlHashService, UrlHashServiceImproved>();
builder.Services.AddScoped<QueryHashService>();
```

### 3. Sử dụng trong Controller

```csharp
public class CourseController : BaseController
{
    private readonly QueryHashService _queryHashService;
    private readonly ICourseService _courseService;

    public async Task<IActionResult> Details(string id)
    {
        // Tự động xử lý cả ID thật và hash
        var course = await _queryHashService.ExecuteQueryWithId(id, async (realId) =>
        {
            return await _courseService.GetByIdAsync(realId);
        });

        if (course == null)
            return NotFound();

        return View(course);
    }
}
```

### 4. Tạo URL với hash

```csharp
// Trong View hoặc Controller
var encryptedId = _urlHashService.EncodeId(course.CourseId);
var url = Url.Action("Details", "Course", new { id = encryptedId });

// Hoặc sử dụng extension method
var url = Url.ActionWithHash("Details", "Course", course.CourseId);
```

### 5. Validation và Error Handling

```csharp
// Kiểm tra tính hợp lệ
var validation = _queryHashService.ValidateIdForQuery(idOrHash);
if (!validation.IsValid)
{
    // Xử lý lỗi
    return BadRequest(validation.ErrorMessage);
}

// Phân tích ID để debug
var analysis = _queryHashService.AnalyzeId(idOrHash);
Console.WriteLine($"Type: {analysis.EncryptionMethod}, Valid: {analysis.IsValid}");
```

## Format của Hash

### Encrypted Hash (AES)
- **Prefix**: `h` 
- **Example**: `hABCDEF123456...`
- **Có thể giải mã**: ✅ Có

### Fallback Hash (SHA256)
- **Prefix**: `s`
- **Example**: `s789GHI456...`
- **Có thể giải mã**: ❌ Không (chỉ dùng khi AES lỗi)

## Best Practices

### 1. Luôn validate input
```csharp
var validation = _queryHashService.ValidateIdForQuery(input);
if (!validation.IsValid)
{
    // Handle error
}
```

### 2. Sử dụng smart query methods
```csharp
// Tốt ✅
var result = await _queryHashService.ExecuteQueryWithId(idOrHash, queryFunc);

// Tránh ❌ - Manual conversion
var realId = _urlHashService.DecodeId(hash);
var result = await _repo.GetByIdAsync(realId);
```

### 3. Prepare data cho display
```csharp
// Chuyển đổi real IDs thành encrypted IDs cho URLs
var displayIds = _queryHashService.PrepareIdsForDisplay(realIds);
```

### 4. Bulk operations
```csharp
// Xử lý multiple IDs cùng lúc
var results = await _queryHashService.ExecuteQueryWithIds(idsOrHashes, async (realIds) =>
{
    return await _repo.GetByIdsAsync(realIds);
});
```

## Migration Steps

1. **Cập nhật appsettings.json** với SecretKey và Salt mạnh
2. **Đảm bảo** `UrlHashServiceImproved` được đăng ký thay vì `UrlHashService`
3. **Thêm** `QueryHashService` vào DI container
4. **Cập nhật** controllers để sử dụng `QueryHashService`
5. **Test** các chức năng với cả ID thật và encrypted hash

## Bảo mật

- **AES-256-CBC**: Encryption mạnh với key derivation qua PBKDF2
- **Random IV**: Mỗi lần encrypt đều tạo IV mới
- **URL-safe encoding**: Base64 được chuyển đổi để an toàn cho URL
- **Fallback mechanism**: Tự động chuyển về SHA256 nếu AES lỗi

## Testing

```csharp
// Test basic encryption/decryption
var originalId = "123";
var encrypted = _urlHashService.EncodeId(originalId);
var decrypted = _urlHashService.DecodeId(encrypted);
Assert.Equal(originalId, decrypted);

// Test validation
var isValid = _urlHashService.ValidateHash(encrypted);
Assert.True(isValid);

// Test smart query
var result = await _queryHashService.ExecuteQueryWithId(encrypted, async (realId) =>
{
    Assert.Equal(originalId, realId);
    return await GetTestDataAsync(realId);
});
```

## Troubleshooting

### Hash không thể giải mã
- Kiểm tra SecretKey và Salt trong config
- Đảm bảo hash format đúng (bắt đầu với 'h')
- Check logs để xem lỗi cụ thể

### Performance issues
- Cache các encrypted IDs nếu cần
- Sử dụng bulk operations cho multiple IDs
- Consider async methods cho database queries

### Compatibility
- Service tự động detect và handle cả real IDs và encrypted hashes
- Fallback mechanism đảm bảo backward compatibility 