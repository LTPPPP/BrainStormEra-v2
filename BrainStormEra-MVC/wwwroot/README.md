# Cấu Trúc Thư Mục Dự Án BrainStormEra

## Tổng Quan

Dự án BrainStormEra được tổ chức theo mô hình MVC (Model-View-Controller) với cấu trúc thư mục được phân chia rõ ràng để dễ dàng quản lý và bảo trì.

## Cấu Trúc Thư Mục wwwroot

### CSS

- **components**: Chứa các file CSS cho các thành phần UI tái sử dụng
  - `header.css`: Style cho header
  - `loader.css`: Style cho loading animation
  - `placeholders.css`: Style cho các placeholder
- **layouts**: Chứa các file CSS cho layout chung
  - `base.css`: Style cơ bản cho toàn bộ trang web
  - `site.css`: Style cho layout chính
- **pages**: Chứa CSS cho từng trang cụ thể, được phân chia theo controller
  - **Home**: CSS cho trang chủ
    - `enhanced-home.css`: Style nâng cao cho trang chủ
    - `homePage.css`: Style cơ bản cho trang chủ
    - `landing_page.css`: Style cho landing page
  - **Course**: CSS cho trang khóa học
  - **Payment**: CSS cho trang thanh toán

### JavaScript

- **components**: Chứa JS cho các thành phần UI tái sử dụng
  - `header.js`: Logic cho header
  - `loader.js`: Logic cho loading animation
- **pages**: Chứa JS cho từng trang cụ thể
  - **Home**: JS cho trang chủ
  - **Course**: JS cho trang khóa học
  - **Payment**: JS cho trang thanh toán
- **utils**: Chứa các utility functions
  - `site.js`: JS chung cho toàn bộ trang web

### Hình Ảnh

- **logo**: Chứa logo của trang web
  - `Main_Logo.jpg`: Logo chính của BrainStormEra
- **banners**: Chứa banner cho trang web
- **courses**: Chứa hình ảnh liên quan đến khóa học
- **avatars**: Chứa avatar người dùng
- **icons**: Chứa các icon sử dụng trong trang web

## Cách Sử Dụng

Khi thêm file mới, hãy đảm bảo đặt chúng vào đúng thư mục theo cấu trúc đã được phân chia. Điều này giúp dự án được tổ chức tốt và dễ dàng bảo trì.

## Quy Ước Đặt Tên

- Sử dụng `kebab-case` cho tên file (ví dụ: `header-dropdown.css`)
- Sử dụng tên có ý nghĩa mô tả rõ chức năng của file
- Prefix CSS component với tên component (ví dụ: `header-dropdown.css`)
- Prefix JS pages với tên controller (ví dụ: `home-slider.js`)
