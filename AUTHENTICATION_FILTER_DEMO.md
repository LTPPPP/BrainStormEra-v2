# Authentication Filter Implementation

## Overview

This project implements an authentication filter to check if users are logged in before accessing ViewDetail pages. If users are not authenticated, they will see a notification and be redirected to the login page.

## Implementation

### 1. RequireAuthenticationAttribute Filter

- **File**: `BrainStormEra-MVC/Filters/RequireAuthenticationAttribute.cs`
- **Function**: Check authentication before accessing ViewDetail pages
- **Messages**: Display customizable English messages for each page type

### 2. Applied to Controllers

#### CourseController

- **Action**: `Details` - View course details
- **Action**: `CourseDetail` - View course details from JavaScript
- **Message**: "You need to login to view course details. Please login to continue."

#### QuizController

- **Action**: `Details` - View quiz details
- **Message**: "You need to login to view quiz details. Please login to continue."

#### CertificateController

- **Action**: `Details` - View certificate details
- **Message**: "You need to login to view certificate details. Please login to continue."

#### UserController

- **Action**: `Detail` - View user details
- **Message**: "You need to login to view user details. Please login to continue."

#### AchievementController

- **Action**: `GetAchievementDetails` - View achievement details
- **Message**: "You need to login to view achievement details. Please login to continue."

## How It Works

1. **Authentication Check**: Filter checks `User.Identity.IsAuthenticated`
2. **Show Notification**: If not authenticated, shows message in `TempData["ErrorMessage"]`
3. **Save Return URL**: Saves current URL to redirect back after login
4. **Redirect to Login**: Redirects to login page with returnUrl

## Test Cases

### Test 1: Access course details page when not logged in

- **URL**: `/Course/Details/courseId`
- **Expected Result**:
  - Redirect to `/Auth/Login?returnUrl=/Course/Details/courseId`
  - Show message: "You need to login to view course details..."

### Test 2: Access quiz details page when not logged in

- **URL**: `/Quiz/Details/quizId`
- **Expected Result**:
  - Redirect to `/Auth/Login?returnUrl=/Quiz/Details/quizId`
  - Show message: "You need to login to view quiz details..."

### Test 3: Click "View Details" button from homepage when not logged in

- **Action**: Click Details button on course card
- **JavaScript**: Calls `viewDetailCourse(courseId)`
- **Expected Result**:
  - Redirect to login page
  - Show appropriate message

### Test 4: After successful login

- **Expected Result**:
  - Automatically redirect back to original detail page
  - Display detail content normally

## Implementation Notes

1. **Session & Cookie**: Filter checks authentication through ASP.NET Core Identity
2. **Return URL**: Used to redirect back to original page after login
3. **TempData**: Used to display error messages
4. **Customizable Messages**: Each controller can have specific messages
5. **JavaScript Integration**: Supports both direct access and JavaScript calls

## Modified Files

1. `BrainStormEra-MVC/Filters/RequireAuthenticationAttribute.cs` (Created new)
2. `BrainStormEra-MVC/Controllers/CourseController.cs` (Added filter + using)
3. `BrainStormEra-MVC/Controllers/QuizController.cs` (Added filter + using)
4. `BrainStormEra-MVC/Controllers/CertificateController.cs` (Added filter + using)
5. `BrainStormEra-MVC/Controllers/UserController.cs` (Added filter + using)
6. `BrainStormEra-MVC/Controllers/AchievementController.cs` (Added filter + using)
