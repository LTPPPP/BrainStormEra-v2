# Points System Documentation

## Tổng quan

Hệ thống Points trong BrainStormEra được thiết kế để tự động cập nhật và hiển thị chính xác số points của user trong thời gian thực.

## Các thành phần chính

### 1. PointsService
- **Interface**: `IPointsService`
- **Implementation**: `PointsService`
- **Chức năng**:
  - Lấy points hiện tại từ database
  - Cập nhật points trong database
  - Refresh claims authentication với points mới

### 2. PointsController
- **Route**: `/api/points`
- **Endpoints**:
  - `GET /api/points/current` - Lấy points hiện tại
  - `POST /api/points/refresh` - Refresh points claim

### 3. PointsRefreshMiddleware
- **Chức năng**: Tự động refresh points claims mỗi 5 phút
- **Vị trí**: Được đăng ký trong pipeline sau SecurityMiddleware

### 4. Points Updater JavaScript
- **File**: `points-updater.js`
- **Chức năng**:
  - Tự động cập nhật points hiển thị mỗi 5 phút
  - Cập nhật khi user quay lại tab
  - Animation khi points thay đổi

## Cách hoạt động

### 1. Khi user đăng nhập
- Points được lưu trong claims authentication
- Thêm claim `PointsLastRefresh` để theo dõi thời gian refresh cuối

### 2. Tự động refresh (5 phút/lần)
- Middleware kiểm tra thời gian refresh cuối
- Nếu đã qua 5 phút, tự động refresh points từ database
- Cập nhật claims với points mới

### 3. JavaScript auto-update
- Script chạy mỗi 5 phút để cập nhật UI
- Gọi API `/api/points/current` để lấy points mới
- Hiển thị với animation đẹp mắt

### 4. Khi points thay đổi
- PaymentService sử dụng PointsService để cập nhật
- AdminService sử dụng PointsService để quản lý points
- CourseService sử dụng PointsService khi user đăng ký khóa học

## Cấu hình

### 1. Đăng ký services trong Program.cs
```csharp
builder.Services.AddScoped<IPointsService, PointsService>();
```

### 2. Đăng ký middleware
```csharp
app.UsePointsRefresh();
```

### 3. Thêm CSS và JavaScript
- `points-display.css` - Styling cho points display
- `points-updater.js` - Auto-update functionality

## API Endpoints

### GET /api/points/current
Lấy points hiện tại của user đã đăng nhập.

**Response:**
```json
{
  "success": true,
  "points": 1500
}
```

### POST /api/points/refresh
Refresh points claim và trả về points mới.

**Response:**
```json
{
  "success": true,
  "points": 1500,
  "message": "Points refreshed successfully"
}
```

## Claims được sử dụng

- `PaymentPoint` - Số points hiện tại
- `PointsLastRefresh` - Thời gian refresh cuối (format: yyyy-MM-dd HH:mm:ss)

## Lưu ý

1. **Performance**: Hệ thống được tối ưu để không ảnh hưởng performance
2. **Security**: Chỉ user đã đăng nhập mới có thể truy cập API
3. **Real-time**: Points được cập nhật real-time từ database
4. **Fallback**: Nếu có lỗi, hệ thống vẫn hoạt động bình thường

## Troubleshooting

### Points không cập nhật
1. Kiểm tra console browser có lỗi JavaScript không
2. Kiểm tra network tab xem API calls có thành công không
3. Kiểm tra logs server có lỗi middleware không

### Points hiển thị sai
1. Kiểm tra database có đúng points không
2. Thử refresh page để reset claims
3. Kiểm tra middleware có chạy đúng không 