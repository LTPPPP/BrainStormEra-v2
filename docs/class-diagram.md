# Class Diagram - BrainStormEra E-Learning Platform

## Overview
This Class Diagram illustrates the object-oriented design of the BrainStormEra e-learning platform, showing the main classes, their attributes, methods, inheritance relationships, and associations.

## Domain Model Class Diagram

```mermaid
classDiagram
    %% Core User Management Classes
    class Account {
        -string UserId
        -string UserRole
        -string Username
        -string PasswordHash
        -string UserEmail
        -string FullName
        -decimal PaymentPoint
        -DateOnly DateOfBirth
        -short Gender
        -string PhoneNumber
        -string UserAddress
        -string UserImage
        -bool IsBanned
        -DateTime AccountCreatedAt
        -DateTime AccountUpdatedAt
        +ValidatePassword(password: string) bool
        +UpdateProfile(profile: UserProfile) bool
        +AddPaymentPoints(amount: decimal) void
        +DeductPaymentPoints(amount: decimal) bool
        +BanAccount(reason: string) void
        +UnbanAccount() void
    }

    class UserProfile {
        -string FullName
        -string Email
        -string PhoneNumber
        -string Address
        -DateOnly DateOfBirth
        -short Gender
        -string ImagePath
        +Validate() ValidationResult
        +ToAccount() Account
    }

    %% Course Management Classes
    class Course {
        -string CourseId
        -string AuthorId
        -string CourseName
        -string CourseDescription
        -int CourseStatus
        -string CourseImage
        -decimal Price
        -int EstimatedDuration
        -byte DifficultyLevel
        -bool IsFeatured
        -string ApprovalStatus
        -DateTime CourseCreatedAt
        +AddChapter(chapter: Chapter) void
        +RemoveChapter(chapterId: string) bool
        +UpdateStatus(status: int) void
        +CalculateTotalDuration() int
        +GetPrerequisites() List~Course~
        +IsAccessibleToUser(userId: string) bool
        +GetEnrollmentCount() int
    }

    class Chapter {
        -string ChapterId
        -string CourseId
        -string ChapterName
        -string ChapterDescription
        -int ChapterOrder
        -bool IsLocked
        -DateTime ChapterCreatedAt
        +AddLesson(lesson: Lesson) void
        +RemoveLesson(lessonId: string) bool
        +ReorderLessons() void
        +IsUnlockedForUser(userId: string) bool
        +GetTotalDuration() int
        +GetCompletionPercentage(userId: string) decimal
    }

    class Lesson {
        -string LessonId
        -string ChapterId
        -string LessonName
        -string LessonDescription
        -string LessonContent
        -int LessonOrder
        -int LessonTypeId
        -bool IsLocked
        -bool IsMandatory
        -DateTime LessonCreatedAt
        +MarkAsCompleted(userId: string) bool
        +UpdateProgress(userId: string, percentage: decimal) void
        +IsAccessibleToUser(userId: string) bool
        +GetPrerequisites() List~Lesson~
        +ValidateCompletion(userId: string) bool
    }

    class LessonType {
        -int LessonTypeId
        -string LessonTypeName
        +GetByName(name: string) LessonType
        +GetAll() List~LessonType~
    }

    %% Assessment System Classes
    class Quiz {
        -string QuizId
        -string LessonId
        -string CourseId
        -string QuizName
        -string QuizDescription
        -int TimeLimit
        -decimal PassingScore
        -int MaxAttempts
        -bool IsFinalQuiz
        -DateTime QuizCreatedAt
        +AddQuestion(question: Question) void
        +RemoveQuestion(questionId: string) bool
        +CalculateScore(answers: List~UserAnswer~) decimal
        +IsPassingScore(score: decimal) bool
        +GetRemainingAttempts(userId: string) int
        +StartAttempt(userId: string) QuizAttempt
    }

    class Question {
        -string QuestionId
        -string QuizId
        -string QuestionText
        -string QuestionType
        -decimal Points
        -int QuestionOrder
        -string Explanation
        +AddAnswerOption(option: AnswerOption) void
        +ValidateAnswer(userAnswer: string) bool
        +GetCorrectAnswers() List~AnswerOption~
        +CalculatePoints(userAnswer: UserAnswer) decimal
    }

    class AnswerOption {
        -string OptionId
        -string QuestionId
        -string OptionText
        -bool IsCorrect
        -int OptionOrder
        +Validate() bool
        +ToString() string
    }

    class QuizAttempt {
        -string AttemptId
        -string UserId
        -string QuizId
        -DateTime StartTime
        -DateTime EndTime
        -decimal Score
        -bool IsPassed
        -bool IsCompleted
        -int AttemptNumber
        +StartAttempt() void
        +SubmitAttempt(answers: List~UserAnswer~) void
        +CalculateFinalScore() decimal
        +IsTimeExpired() bool
        +GetTimeRemaining() TimeSpan
    }

    class UserAnswer {
        -string UserId
        -string QuestionId
        -string AttemptId
        -string SelectedOptionId
        -string AnswerText
        -bool IsCorrect
        -decimal PointsEarned
        +ValidateAnswer() bool
        +CalculatePoints() decimal
    }

    %% Enrollment and Progress Classes
    class Enrollment {
        -string EnrollmentId
        -string UserId
        -string CourseId
        -int EnrollmentStatus
        -bool Approved
        -decimal ProgressPercentage
        -DateOnly CertificateIssuedDate
        -DateTime EnrollmentCreatedAt
        +UpdateProgress() void
        +CalculateProgressPercentage() decimal
        +IsCompleted() bool
        +GenerateCertificate() Certificate
        +Approve() void
        +Reject(reason: string) void
    }

    class UserProgress {
        -string UserId
        -string LessonId
        -bool IsCompleted
        -decimal ProgressPercentage
        -int TimeSpent
        -DateTime LastAccessedAt
        -DateTime CompletedAt
        -bool IsUnlocked
        +UpdateProgress(percentage: decimal) void
        +MarkCompleted() void
        +AddTimeSpent(seconds: int) void
        +UnlockLesson() void
        +MeetsRequirements() bool
    }

    %% Achievement System Classes
    class Achievement {
        -string AchievementId
        -string AchievementName
        -string AchievementDescription
        -string AchievementIcon
        -string UnlockCriteria
        -int PointsReward
        -string AchievementType
        -bool IsActive
        +EvaluateCriteria(user: Account) bool
        +UnlockForUser(userId: string) UserAchievement
        +GetProgress(userId: string) decimal
    }

    class UserAchievement {
        -string UserAchievementId
        -string UserId
        -string AchievementId
        -string CourseId
        -DateTime UnlockedAt
        -bool IsNotified
        -string UnlockContext
        +NotifyUser() void
        +GetAchievementDetails() Achievement
    }

    class Certificate {
        -string CertificateId
        -string UserId
        -string CourseId
        -string CertificateName
        -DateTime IssueDate
        -string CertificateUrl
        -bool IsVerified
        -string VerificationCode
        +GenerateCertificate() void
        +VerifyCertificate(code: string) bool
        +GetDownloadUrl() string
        +SendByEmail() void
    }

    %% Communication System Classes
    class Conversation {
        -string ConversationId
        -string ConversationType
        -string CreatedBy
        -bool IsActive
        -DateTime LastMessageAt
        +AddParticipant(userId: string) void
        +RemoveParticipant(userId: string) void
        +SendMessage(message: MessageEntity) void
        +GetMessages(page: int, size: int) List~MessageEntity~
        +MarkAsRead(userId: string) void
    }

    class MessageEntity {
        -string MessageId
        -string SenderId
        -string ReceiverId
        -string ConversationId
        -string MessageContent
        -string MessageType
        -bool IsRead
        -DateTime MessageCreatedAt
        +MarkAsRead() void
        +EditMessage(newContent: string) void
        +DeleteMessage() void
        +AddAttachment(url: string, name: string) void
    }

    class ConversationParticipant {
        -string ConversationId
        -string UserId
        -string ParticipantRole
        -DateTime JoinedAt
        -bool IsActive
        -bool IsMuted
        +MuteConversation() void
        +UnmuteConversation() void
        +LeaveConversation() void
        +UpdateRole(role: string) void
    }

    %% AI Chatbot Classes
    class ChatbotConversation {
        -string ConversationId
        -string UserId
        -string SessionId
        -string ContextPage
        -string ContextCourseId
        -DateTime ConversationStartedAt
        -bool IsActive
        +StartSession() void
        +EndSession() void
        +UpdateContext(page: string, courseId: string) void
        +GetConversationHistory() List~string~
    }

    %% Notification System Classes
    class Notification {
        -string NotificationId
        -string UserId
        -string NotificationType
        -string Title
        -string Message
        -bool IsRead
        -DateTime CreatedAt
        -string ActionUrl
        +MarkAsRead() void
        +Send() void
        +GetActionUrl() string
        +CreateFromTemplate(template: string, data: object) Notification
    }

    %% Payment System Classes
    class PaymentTransaction {
        -string TransactionId
        -string UserId
        -string CourseId
        -decimal Amount
        -string Currency
        -string TransactionType
        -string Status
        -DateTime TransactionDate
        +ProcessPayment() bool
        +RefundPayment() bool
        +UpdateStatus(status: string) void
        +GetPaymentDetails() object
        +ValidateAmount() bool
    }

    class PaymentMethod {
        -int PaymentMethodId
        -string MethodName
        -bool IsActive
        +ProcessPayment(amount: decimal) bool
        +ValidatePaymentData(data: object) bool
    }

    %% Service Layer Classes
    class AuthServiceImpl {
        -IAuthRepo authRepo
        -ISecurityService securityService
        +AuthenticateUserAsync(model: LoginModel) Task~LoginResult~
        +RegisterUserAsync(model: RegisterModel) Task~bool~
        +ResetPasswordAsync(email: string) Task~bool~
        +ChangePasswordAsync(userId: string, oldPassword: string, newPassword: string) Task~bool~
        +ValidateUserSession(userId: string) Task~bool~
    }

    class CourseServiceImpl {
        -ICourseRepo courseRepo
        -IChapterRepo chapterRepo
        -ILessonRepo lessonRepo
        +CreateCourseAsync(model: CreateCourseModel) Task~Course~
        +UpdateCourseAsync(courseId: string, model: UpdateCourseModel) Task~bool~
        +ApproveCourseAsync(courseId: string, adminId: string) Task~bool~
        +GetCoursesAsync(filter: CourseFilterModel) Task~List~Course~~
        +EnrollUserAsync(userId: string, courseId: string) Task~bool~
    }

    class QuizServiceImpl {
        -IQuizRepo quizRepo
        -IQuestionRepo questionRepo
        -IQuizAttemptRepo attemptRepo
        +CreateQuizAsync(model: CreateQuizModel) Task~Quiz~
        +StartQuizAttemptAsync(userId: string, quizId: string) Task~QuizAttempt~
        +SubmitQuizAsync(attemptId: string, answers: List~UserAnswer~) Task~decimal~
        +GetQuizResultsAsync(attemptId: string) Task~QuizResult~
    }

    %% Repository Classes (Interfaces)
    class IAuthRepo {
        <<interface>>
        +GetUserByUsernameAsync(username: string) Task~Account~
        +GetUserByEmailAsync(email: string) Task~Account~
        +CreateUserAsync(account: Account) Task~bool~
        +UpdateUserAsync(account: Account) Task~bool~
        +ValidatePasswordAsync(userId: string, password: string) Task~bool~
    }

    class ICourseRepo {
        <<interface>>
        +GetByIdAsync(courseId: string) Task~Course~
        +GetAllAsync() Task~List~Course~~
        +CreateAsync(course: Course) Task~Course~
        +UpdateAsync(course: Course) Task~bool~
        +DeleteAsync(courseId: string) Task~bool~
        +GetByAuthorAsync(authorId: string) Task~List~Course~~
    }

    %% Hub Classes
    class ChatHub {
        -IChatService chatService
        -ILogger logger
        +SendMessage(receiverId: string, message: string) Task
        +JoinGroup(groupName: string) Task
        +LeaveGroup(groupName: string) Task
        +MarkMessageAsRead(messageId: string) Task
        +GetOnlineUsers() Task~List~string~~
    }

    class NotificationHub {
        -INotificationService notificationService
        +SendNotification(userId: string, notification: Notification) Task
        +JoinUserGroup(userId: string) Task
        +LeaveUserGroup(userId: string) Task
        +BroadcastToRole(role: string, notification: Notification) Task
    }

    %% Base Classes
    class BaseController {
        #string CurrentUserId
        #string CurrentUsername
        #string CurrentUserRole
        #string CurrentUserEmail
        +GetCurrentUser() Account
        +IsAuthenticated() bool
        +HasRole(role: string) bool
        +LogUserAction(action: string) void
    }

    %% Relationships
    Account ||--o{ Course : "creates"
    Account ||--o{ Enrollment : "enrolls in"
    Account ||--o{ UserProgress : "tracks progress"
    Account ||--o{ UserAchievement : "earns"
    Account ||--o{ QuizAttempt : "attempts"
    Account ||--o{ UserAnswer : "provides answers"
    Account ||--o{ MessageEntity : "sends/receives"
    Account ||--o{ Notification : "receives"
    Account ||--o{ PaymentTransaction : "makes payments"
    Account ||--o{ Certificate : "receives"

    Course ||--o{ Chapter : "contains"
    Course ||--o{ Enrollment : "enrolled by users"
    Course ||--o{ Quiz : "includes"
    Course ||--o{ Certificate : "issues"

    Chapter ||--o{ Lesson : "contains"
    Lesson ||--o{ Quiz : "may have"
    Lesson ||--o{ UserProgress : "tracked by"
    LessonType ||--o{ Lesson : "categorizes"

    Quiz ||--o{ Question : "contains"
    Quiz ||--o{ QuizAttempt : "attempted by users"
    Question ||--o{ AnswerOption : "has options"
    Question ||--o{ UserAnswer : "answered by users"
    QuizAttempt ||--o{ UserAnswer : "includes"

    Achievement ||--o{ UserAchievement : "unlocked as"
    Enrollment ||--o{ Certificate : "generates"
    Enrollment ||--o{ UserAchievement : "may unlock"

    Conversation ||--o{ MessageEntity : "contains"
    Conversation ||--o{ ConversationParticipant : "includes"

    PaymentMethod ||--o{ PaymentTransaction : "processed by"

    %% Service Dependencies
    AuthServiceImpl --> IAuthRepo : "uses"
    CourseServiceImpl --> ICourseRepo : "uses"
    QuizServiceImpl --> IQuizRepo : "uses"

    %% Inheritance
    BaseController <|-- AuthController
    BaseController <|-- CourseController
    BaseController <|-- QuizController
    BaseController <|-- ProfileController
```

## Class Categories

### Domain Model Classes
Core business entities representing the problem domain:

**User Management**
- `Account`: Central user entity with authentication and profile data
- `UserProfile`: Data transfer object for profile updates

**Educational Content**
- `Course`: Main educational container
- `Chapter`: Course subdivision for organization
- `Lesson`: Individual learning unit
- `LessonType`: Classification of lesson content types

**Assessment System**
- `Quiz`: Assessment container
- `Question`: Individual quiz questions
- `AnswerOption`: Multiple choice options
- `QuizAttempt`: User's quiz submission
- `UserAnswer`: User's answer to specific questions

### Progress and Achievement Classes
Track user learning progress and gamification:

**Progress Tracking**
- `Enrollment`: User's course enrollment status
- `UserProgress`: Detailed lesson-level progress
- `Certificate`: Completion certificates

**Achievement System**
- `Achievement`: Available achievements/badges
- `UserAchievement`: User's unlocked achievements

### Communication Classes
Handle messaging and notifications:

**Messaging System**
- `Conversation`: Chat conversations
- `MessageEntity`: Individual messages
- `ConversationParticipant`: Conversation membership
- `ChatbotConversation`: AI chatbot sessions

**Notification System**
- `Notification`: System notifications and alerts

### Service Layer Classes
Business logic implementation:

**Service Implementations**
- `AuthServiceImpl`: Authentication business logic
- `CourseServiceImpl`: Course management operations
- `QuizServiceImpl`: Assessment processing

**Repository Interfaces**
- `IAuthRepo`: User data access interface
- `ICourseRepo`: Course data access interface
- Additional repository interfaces for each entity

### Infrastructure Classes
Technical framework classes:

**SignalR Hubs**
- `ChatHub`: Real-time messaging hub
- `NotificationHub`: Live notification delivery

**Base Classes**
- `BaseController`: Common controller functionality

## Design Patterns Implemented

### Repository Pattern
```csharp
public interface IBaseRepo<T> where T : class
{
    Task<T> GetByIdAsync(object id);
    Task<List<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task<bool> UpdateAsync(T entity);
    Task<bool> DeleteAsync(object id);
}
```

### Service Layer Pattern
```csharp
public class CourseServiceImpl : ICourseService
{
    private readonly ICourseRepo _courseRepo;
    private readonly IChapterRepo _chapterRepo;
    
    public async Task<Course> CreateCourseAsync(CreateCourseModel model)
    {
        // Business logic implementation
        var course = new Course
        {
            CourseId = Guid.NewGuid().ToString(),
            CourseName = model.CourseName,
            // ... other properties
        };
        
        return await _courseRepo.CreateAsync(course);
    }
}
```

### Factory Pattern
```csharp
public class NotificationFactory
{
    public static Notification CreateAchievementNotification(
        string userId, Achievement achievement)
    {
        return new Notification
        {
            UserId = userId,
            NotificationType = "achievement",
            Title = "Achievement Unlocked!",
            Message = $"You've earned: {achievement.AchievementName}"
        };
    }
}
```

### Builder Pattern
```csharp
public class QuizBuilder
{
    private Quiz _quiz = new Quiz();
    
    public QuizBuilder SetName(string name)
    {
        _quiz.QuizName = name;
        return this;
    }
    
    public QuizBuilder SetTimeLimit(int minutes)
    {
        _quiz.TimeLimit = minutes;
        return this;
    }
    
    public Quiz Build() => _quiz;
}
```

## Key Object Relationships

### Composition Relationships
- **Course** contains **Chapters** (strong ownership)
- **Chapter** contains **Lessons** (strong ownership)
- **Quiz** contains **Questions** (strong ownership)
- **Question** contains **AnswerOptions** (strong ownership)

### Association Relationships
- **Account** enrolls in **Course** through **Enrollment**
- **Account** attempts **Quiz** through **QuizAttempt**
- **Account** earns **Achievement** through **UserAchievement**

### Aggregation Relationships
- **Conversation** includes **ConversationParticipants**
- **QuizAttempt** aggregates **UserAnswers**

### Dependency Relationships
- **Service Layer** depends on **Repository Interfaces**
- **Controllers** depend on **Service Implementations**
- **Hubs** depend on **Service Layer**

## Business Logic Encapsulation

### Domain Rules
1. **Course Prerequisites**: Users must complete prerequisite courses before enrollment
2. **Sequential Learning**: Lessons must be completed in order (configurable)
3. **Quiz Attempts**: Limited number of attempts per quiz
4. **Achievement Unlocking**: Automatic based on defined criteria
5. **Payment Processing**: Secure transaction handling with rollback capabilities

### Validation Logic
- **Input Validation**: Data annotation attributes and FluentValidation
- **Business Rule Validation**: Custom validation in service layer
- **Security Validation**: Authentication and authorization checks

This class diagram provides a comprehensive view of the BrainStormEra object-oriented design, ensuring maintainable, extensible, and well-structured code architecture. 