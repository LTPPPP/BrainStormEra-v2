# BrainStormEra CSS Design System

## Tổng quan
Design system này cung cấp một bộ CSS Variables thống nhất và responsive design cho toàn bộ dự án BrainStormEra.

## Cấu trúc Files
- `global.css` - CSS Variables, reset styles, và responsive utilities
- `admin-dashboard.css` - Styles cho admin dashboard
- `admin-login.css` - Styles cho trang đăng nhập admin
- `admin-profile.css` - Styles cho trang profile admin
- `admin-courses.css` - Styles cho quản lý khóa học
- `site.css` - Global site styles

## CSS Variables

### Colors
```css
/* Primary Colors */
--primary-blue: #667eea
--primary-purple: #764ba2
--primary-gradient: linear-gradient(135deg, var(--primary-blue) 0%, var(--primary-purple) 100%)

/* Background Colors */
--bg-primary: #ffffff (white)
--bg-secondary: #f7fafc (light gray)
--bg-tertiary: #edf2f7 (lighter gray)

/* Text Colors */
--text-primary: #2d3748 (dark gray)
--text-secondary: #718096 (medium gray)
--text-tertiary: #a0aec0 (light gray)

/* Status Colors */
--success: #48bb78 (green)
--warning: #f6ad55 (orange)
--danger: #f56565 (red)
--info: #4299e1 (blue)
```

### Spacing
```css
--spacing-xs: 0.25rem (4px)
--spacing-sm: 0.5rem (8px)
--spacing-md: 1rem (16px)
--spacing-lg: 1.5rem (24px)
--spacing-xl: 2rem (32px)
--spacing-2xl: 3rem (48px)
--spacing-3xl: 4rem (64px)
```

### Border Radius
```css
--radius-sm: 4px
--radius-md: 8px
--radius-lg: 12px
--radius-xl: 16px
--radius-2xl: 20px
--radius-3xl: 24px
--radius-full: 50%
```

### Shadows
```css
--shadow-sm: 0 1px 3px rgba(0, 0, 0, 0.1)
--shadow-md: 0 4px 6px rgba(0, 0, 0, 0.1)
--shadow-lg: 0 10px 15px rgba(0, 0, 0, 0.1)
--shadow-xl: 0 20px 25px rgba(0, 0, 0, 0.15)
--shadow-primary: 0 8px 20px rgba(102, 126, 234, 0.3)
```

## Responsive Breakpoints
```css
--breakpoint-sm: 576px
--breakpoint-md: 768px
--breakpoint-lg: 992px
--breakpoint-xl: 1200px
--breakpoint-2xl: 1400px
```

## Grid System
Responsive grid system tương tự Bootstrap với 12 columns:

```html
<div class="row">
    <div class="col-12 col-md-6 col-lg-4">
        <!-- Content -->
    </div>
</div>
```

## Utility Classes

### Display
- `.d-none`, `.d-block`, `.d-flex`, `.d-grid`
- `.d-sm-none`, `.d-md-block`, `.d-lg-flex` (responsive)

### Text Alignment
- `.text-left`, `.text-center`, `.text-right`

### Flexbox
- `.justify-content-center`, `.justify-content-between`
- `.align-items-center`, `.align-items-start`
- `.flex-column`, `.flex-wrap`, `.flex-1`

### Spacing
- `.m-0` to `.m-5` (margin)
- `.mt-0` to `.mt-5` (margin-top)
- `.mb-0` to `.mb-5` (margin-bottom)
- `.p-0` to `.p-5` (padding)

### Colors
- `.text-primary`, `.text-success`, `.text-warning`, `.text-danger`
- `.bg-primary`, `.bg-success`, `.bg-warning`, `.bg-danger`

### Borders & Shadows
- `.border`, `.border-0`
- `.rounded`, `.rounded-sm`, `.rounded-lg`
- `.shadow`, `.shadow-sm`, `.shadow-lg`

## Components

### Buttons
```html
<button class="btn btn-primary">Primary Button</button>
<button class="btn btn-success">Success Button</button>
```

### Cards
```html
<div class="card">
    <div class="card-header">
        <h5>Card Title</h5>
    </div>
    <div class="card-body">
        <p>Card content</p>
    </div>
</div>
```

### Forms
```html
<div class="form-group">
    <label class="form-label">Label</label>
    <input type="text" class="form-control" placeholder="Input">
</div>
```

## Responsive Design

### Mobile First
Design system sử dụng mobile-first approach:
- Base styles áp dụng cho mobile
- Media queries thêm styles cho tablet và desktop

### Breakpoints
- **XS (< 576px)**: Mobile phones
- **SM (576px+)**: Large phones
- **MD (768px+)**: Tablets
- **LG (992px+)**: Desktops
- **XL (1200px+)**: Large desktops

### Container
```html
<div class="container-responsive">
    <!-- Content tự động responsive -->
</div>
```

## Dark Mode Support
CSS Variables hỗ trợ dark mode tự động:
```css
@media (prefers-color-scheme: dark) {
    :root {
        --bg-primary: #1a202c;
        --text-primary: #ffffff;
        /* ... other dark colors */
    }
}
```

## Accessibility
- Focus states được định nghĩa rõ ràng
- High contrast mode support
- Reduced motion support cho accessibility

## Cách sử dụng

### 1. Import trong Layout
```html
<link href="~/css/global.css" rel="stylesheet">
```

### 2. Sử dụng CSS Variables
```css
.my-component {
    background: var(--primary-gradient);
    padding: var(--spacing-lg);
    border-radius: var(--radius-lg);
    box-shadow: var(--shadow-md);
}
```

### 3. Sử dụng Utility Classes
```html
<div class="bg-primary text-light p-3 rounded shadow">
    Content với background primary
</div>
```

## Performance
- CSS Variables giảm file size và tăng performance
- Minimal media queries
- Efficient utility classes

## Browser Support
- Modern browsers (Chrome, Firefox, Safari, Edge)
- CSS Variables support (IE 11+ với fallbacks)

## Demo
Xem file `demo-responsive.html` để test toàn bộ design system. 