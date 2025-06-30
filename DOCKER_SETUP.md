# BrainStormEra Docker Setup

Hướng dẫn chạy toàn bộ hệ thống BrainStormEra bao gồm cả MVC và Razor Pages ứng dụng với Docker Compose.

## Yêu cầu hệ thống

- Docker Desktop
- Docker Compose
- Ít nhất 4GB RAM khả dụng

## Cách chạy

### 1. Khởi động toàn bộ hệ thống

```bash
docker-compose up -d
```

### 2. Kiểm tra trạng thái services

```bash
docker-compose ps
```

### 3. Xem logs

```bash
# Xem logs tất cả services
docker-compose logs -f

# Xem logs của service cụ thể
docker-compose logs -f mvc
docker-compose logs -f razor
docker-compose logs -f database
```

## Truy cập ứng dụng

- **MVC Application**: http://localhost:5000
- **Razor Pages Application**: http://localhost:5001
- **SQL Server**: localhost:1433
  - Username: `sa`
  - Password: `YourStrong@Passw0rd`
  - Database: `BrainStormEra`

## Dừng hệ thống

```bash
# Dừng services nhưng giữ lại data
docker-compose down

# Dừng services và xóa volumes (mất hết data)
docker-compose down -v
```

## Rebuild ứng dụng

Khi có thay đổi code:

```bash
# Rebuild và restart
docker-compose up -d --build

# Hoặc rebuild service cụ thể
docker-compose build mvc
docker-compose build razor
```

## Troubleshooting

### Database connection issues
- Đảm bảo SQL Server container đã startup hoàn tất (khoảng 30-60 giây)
- Kiểm tra logs: `docker-compose logs database`

### Port conflicts
- Nếu port 5000, 5001, hoặc 1433 đã được sử dụng, tạm dừng service đang chạy trên port đó

### Memory issues
- Đảm bảo Docker Desktop có đủ memory allocation (ít nhất 4GB)

## Cấu trúc Services

- **database**: SQL Server 2022 Express
- **mvc**: BrainStormEra MVC Application (Port 5000)  
- **razor**: BrainStormEra Razor Pages Application (Port 5001)

Tất cả services được kết nối qua network `brainstormera-network` và có thể communicate với nhau. 