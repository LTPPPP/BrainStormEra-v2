# Sequence Diagram - BrainStormEra E-Learning Platform

## Overview
This document contains multiple sequence diagrams showing detailed interactions between system components, actors, and external services in the BrainStormEra e-learning platform.

## Sequence Diagrams

### 1. User Authentication and Authorization Flow

```mermaid
sequenceDiagram
    participant U as User
    participant Browser as Browser
    participant AuthController as AuthController
    participant AuthService as AuthServiceImpl
    participant AuthRepo as AuthRepo
    participant Database as SQL Server
    participant SecurityService as SecurityService
    participant PasswordHasher as PasswordHasher
    
    U->>Browser: Enter login credentials
    Browser->>AuthController: POST /Auth/Login
    AuthController->>AuthService: AuthenticateUserAsync(context, model)
    
    AuthService->>AuthRepo: GetUserByUsernameAsync(username)
    AuthRepo->>Database: SELECT * FROM account WHERE username=?
    Database-->>AuthRepo: User record
    AuthRepo-->>AuthService: Account object
    
    AuthService->>AuthService: Check if user is banned
    alt User is banned
        AuthService-->>AuthController: LoginResult(Success=false, ErrorMessage="Account suspended")
        AuthController-->>Browser: Error view with message
        Browser-->>U: Display error message
    end
    
    AuthService->>PasswordHasher: VerifyPasswordAsync(password, hashedPassword)
    PasswordHasher-->>AuthService: Password verification result
    
    alt Password is invalid
        AuthService->>SecurityService: LogLoginAttemptAsync(failed attempt)
        SecurityService->>Database: INSERT INTO login_attempts
        AuthService-->>AuthController: LoginResult(Success=false)
        AuthController-->>Browser: Error view
        Browser-->>U: Display login error
    end
    
    AuthService->>AuthService: Create authentication claims
    AuthService->>Browser: SignInAsync with cookie
    AuthService->>SecurityService: LogLoginAttemptAsync(successful attempt)
    SecurityService->>Database: INSERT INTO login_attempts
    AuthService->>AuthRepo: UpdateLastLoginAsync(userId)
    AuthRepo->>Database: UPDATE account SET last_login
    
    AuthService-->>AuthController: LoginResult(Success=true, UserRole, RedirectAction)
    AuthController->>AuthController: GetPostLoginRedirect(userRole)
    alt User is Learner
        AuthController-->>Browser: Redirect to LearnerDashboard
    else User is Instructor
        AuthController-->>Browser: Redirect to InstructorDashboard
    else User is Admin
        AuthController-->>Browser: Redirect to AdminDashboard
    end
    Browser-->>U: Display appropriate dashboard
```

### 2. Course Enrollment and Payment Processing Flow

```mermaid
sequenceDiagram
    participant L as Learner
    participant Browser as Browser
    participant CourseController as CourseController
    participant CourseServiceImpl as CourseServiceImpl
    participant CourseService as CourseService
    participant UserRepo as UserRepo
    participant PaymentService as PaymentService
    participant Database as SQL Server
    participant NotificationHub as NotificationHub
    
    L->>Browser: Click "Enroll in Course"
    Browser->>CourseController: POST /Course/Enroll
    CourseController->>CourseServiceImpl: EnrollInCourseAsync(user, courseId)
    
    CourseServiceImpl->>CourseService: IsUserEnrolledAsync(userId, courseId)
    CourseService->>Database: SELECT FROM enrollments WHERE user_id AND course_id
    Database-->>CourseService: Enrollment check result
    CourseService-->>CourseServiceImpl: false (not enrolled)
    
    CourseServiceImpl->>CourseService: GetCourseByIdAsync(courseId)
    CourseService->>Database: SELECT FROM course WHERE course_id
    Database-->>CourseService: Course details
    CourseService-->>CourseServiceImpl: Course object with price
    
    alt Course is free (price = 0)
        CourseServiceImpl->>CourseService: EnrollUserAsync(userId, courseId)
        CourseService->>Database: INSERT INTO enrollments
        Database-->>CourseService: Enrollment created
        CourseServiceImpl-->>CourseController: EnrollmentResult(Success=true)
    else Course requires payment
        CourseServiceImpl->>UserRepo: GetUserWithPaymentPointAsync(userId)
        UserRepo->>Database: SELECT payment_point FROM account WHERE user_id
        Database-->>UserRepo: User payment points
        UserRepo-->>CourseServiceImpl: User with points
        
        alt Insufficient points
            CourseServiceImpl-->>CourseController: EnrollmentResult(Success=false, "Insufficient points")
        else Sufficient points
            CourseServiceImpl->>Database: BEGIN TRANSACTION
            CourseServiceImpl->>Database: UPDATE account SET payment_point = payment_point - course_price
            CourseServiceImpl->>Database: INSERT INTO enrollments
            CourseServiceImpl->>Database: COMMIT TRANSACTION
            CourseServiceImpl-->>CourseController: EnrollmentResult(Success=true)
        end
    end
    
    CourseController->>NotificationHub: Send enrollment notification
    NotificationHub->>L: Real-time notification
    CourseController-->>Browser: JSON response with result
    Browser-->>L: Display enrollment confirmation
```

### 3. Quiz Taking and Auto-Grading Flow

```mermaid
sequenceDiagram
    participant L as Learner
    participant Browser as Browser
    participant QuizController as QuizController
    participant QuizServiceImpl as QuizServiceImpl
    participant QuizRepo as QuizRepo
    participant AchievementService as AchievementUnlockService
    participant Database as SQL Server
    participant NotificationHub as NotificationHub
    
    L->>Browser: Start Quiz
    Browser->>QuizController: GET /Quiz/Take/{id}
    QuizController->>QuizServiceImpl: GetQuizTakeAsync(user, quizId)
    
    QuizServiceImpl->>QuizRepo: CanUserRetakeQuizAsync(userId, quizId)
    QuizRepo->>Database: SELECT COUNT FROM quiz_attempts WHERE user_id AND quiz_id
    Database-->>QuizRepo: Attempt count
    QuizRepo-->>QuizServiceImpl: Can retake = true/false
    
    QuizServiceImpl->>QuizRepo: GetQuizWithQuestionsAsync(quizId)
    QuizRepo->>Database: SELECT quiz with questions and options
    Database-->>QuizRepo: Quiz with questions
    QuizRepo-->>QuizServiceImpl: QuizTakeViewModel
    
    QuizServiceImpl->>QuizRepo: StartQuizAttemptAsync(userId, quizId)
    QuizRepo->>Database: INSERT INTO quiz_attempts
    Database-->>QuizRepo: New attempt record
    QuizRepo-->>QuizServiceImpl: QuizAttempt object
    
    QuizServiceImpl-->>QuizController: QuizTakeResult with ViewModel
    QuizController-->>Browser: Quiz page with questions
    Browser-->>L: Display quiz questions
    
    L->>Browser: Answer questions and submit
    Browser->>QuizController: POST /Quiz/Submit
    QuizController->>QuizServiceImpl: SubmitQuizAsync(user, model, modelState)
    
    QuizServiceImpl->>Database: Get quiz attempt with questions
    QuizServiceImpl->>QuizServiceImpl: Check time limit
    
    loop For each question
        QuizServiceImpl->>QuizServiceImpl: Validate user answer
        QuizServiceImpl->>QuizServiceImpl: Calculate points earned
        QuizServiceImpl->>Database: INSERT INTO user_answers
    end
    
    QuizServiceImpl->>QuizServiceImpl: Calculate final score and percentage
    QuizServiceImpl->>Database: UPDATE quiz_attempts SET score, percentage_score, is_passed
    
    QuizServiceImpl->>AchievementService: CheckQuizAchievementsAsync(userId, quizId, score, isPassed)
    AchievementService->>Database: Check achievement criteria
    
    loop For each unlocked achievement
        AchievementService->>Database: INSERT INTO user_achievements
        AchievementService->>NotificationHub: Send achievement notification
        NotificationHub->>L: Real-time achievement notification
    end
    
    AchievementService-->>QuizServiceImpl: List of unlocked achievements
    QuizServiceImpl-->>QuizController: QuizSubmitResult(Success=true, AttemptId)
    QuizController-->>Browser: Redirect to results page
    Browser-->>L: Display quiz results and achievements
```

### 4. Real-time Chat Communication Flow

```mermaid
sequenceDiagram
    participant U1 as User1 (Sender)
    participant Browser1 as Browser1
    participant U2 as User2 (Receiver)
    participant Browser2 as Browser2
    participant ChatHub as ChatHub (SignalR)
    participant ChatService as ChatService
    participant Database as SQL Server
    participant NotificationHub as NotificationHub
    
    U1->>Browser1: Open chat with User2
    Browser1->>ChatHub: SignalR connection established
    ChatHub->>ChatService: SetUserOnlineStatusAsync(user1Id, true)
    ChatService->>Database: UPDATE account SET online_status
    
    U2->>Browser2: Open chat application
    Browser2->>ChatHub: SignalR connection established
    ChatHub->>ChatService: SetUserOnlineStatusAsync(user2Id, true)
    
    U1->>Browser1: Type message
    Browser1->>ChatHub: StartTyping(receiverId=user2Id)
    ChatHub->>Browser2: UserStartedTyping(senderId=user1Id)
    Browser2-->>U2: Show typing indicator
    
    U1->>Browser1: Send message "Hello!"
    Browser1->>ChatHub: SendMessage(receiverId=user2Id, message="Hello!")
    
    ChatHub->>ChatService: SendMessageAsync(user1Id, user2Id, "Hello!", null)
    ChatService->>Database: INSERT INTO message_entities
    Database-->>ChatService: Message record created
    ChatService-->>ChatHub: MessageEntity object
    
    ChatHub->>Browser2: ReceiveMessage(messageData)
    Browser2-->>U2: Display new message with notification sound
    
    ChatHub->>Browser1: MessageSent(confirmation)
    Browser1-->>U1: Show message as sent
    
    Browser2->>ChatHub: MarkMessageAsRead(messageId)
    ChatHub->>ChatService: MarkMessageAsReadAsync(messageId, user2Id)
    ChatService->>Database: UPDATE message_entities SET is_read=true
    
    ChatHub->>Browser1: MessageRead(messageId, readerId=user2Id)
    Browser1-->>U1: Show message as read (✓✓)
    
    U2->>Browser2: Reply "Hi there!"
    Browser2->>ChatHub: SendMessage(receiverId=user1Id, message="Hi there!")
    Note over ChatHub,Database: Same message sending process
    
    ChatHub->>Browser1: ReceiveMessage(replyData)
    Browser1-->>U1: Display reply message
    
    Browser1->>ChatHub: Connection lost (network issue)
    ChatHub->>ChatService: SetUserOnlineStatusAsync(user1Id, false)
    
    U2->>Browser2: Send "Are you there?"
    Browser2->>ChatHub: SendMessage(message="Are you there?")
    ChatHub->>ChatService: SendMessageAsync(user2Id, user1Id, "Are you there?")
    ChatService->>Database: Store message for offline user
    
    Browser1->>ChatHub: Reconnect established
    ChatHub->>ChatService: SetUserOnlineStatusAsync(user1Id, true)
    ChatService->>Database: Get unread messages for user1Id
    ChatHub->>Browser1: Deliver offline messages
    Browser1-->>U1: Display missed messages
```

### 5. AI Chatbot Interaction Flow

```mermaid
sequenceDiagram
    participant L as Learner
    participant Browser as Browser
    participant ChatbotController as ChatbotController
    participant ChatbotServiceImpl as ChatbotServiceImpl
    participant AIService as AI Service (FastAPI)
    participant GoogleAI as Google AI API
    participant Database as SQL Server
    
    L->>Browser: Open chatbot interface
    Browser->>Browser: Load chatbot widget
    
    L->>Browser: Type question "How do I complete this course?"
    Browser->>ChatbotController: POST /Chatbot/Ask
    ChatbotController->>ChatbotServiceImpl: ProcessQuestionAsync(user, request)
    
    ChatbotServiceImpl->>ChatbotServiceImpl: Extract context (current page, course)
    ChatbotServiceImpl->>ChatbotServiceImpl: Validate and sanitize input
    
    ChatbotServiceImpl->>AIService: POST /chatbot/ask
    Note over ChatbotServiceImpl,AIService: Send question with context:<br/>- User ID<br/>- Current course<br/>- Learning progress<br/>- Question text
    
    AIService->>AIService: Process context and question
    AIService->>GoogleAI: Generate response using Gemini Pro
    GoogleAI-->>AIService: AI-generated response
    AIService-->>ChatbotServiceImpl: Contextualized response
    
    ChatbotServiceImpl->>Database: INSERT INTO chatbot_conversations
    ChatbotServiceImpl-->>ChatbotController: ChatbotResponse(success=true, response)
    ChatbotController-->>Browser: JSON response with answer
    Browser-->>L: Display AI response
    
    L->>Browser: Rate response (4/5 stars)
    Browser->>ChatbotController: POST /Chatbot/Feedback
    ChatbotController->>ChatbotServiceImpl: SubmitFeedbackAsync(conversationId, rating=4)
    
    ChatbotServiceImpl->>AIService: POST /chatbot/feedback
    AIService->>Database: UPDATE chatbot_conversations SET feedback_rating=4
    ChatbotServiceImpl-->>ChatbotController: FeedbackResult(success=true)
    ChatbotController-->>Browser: Feedback confirmation
    Browser-->>L: Show "Thank you for feedback"
    
    L->>Browser: Ask follow-up "What are the quiz requirements?"
    Note over L,Database: Same process repeats with conversation history
    
    Browser->>ChatbotController: POST /Chatbot/Ask (follow-up)
    ChatbotController->>ChatbotServiceImpl: ProcessQuestionAsync(with conversation history)
    ChatbotServiceImpl->>AIService: POST /chatbot/ask (with context + history)
    AIService->>GoogleAI: Generate contextual follow-up response
    GoogleAI-->>AIService: Relevant response about quiz requirements
    AIService-->>ChatbotServiceImpl: Response with quiz details
    ChatbotServiceImpl-->>ChatbotController: Detailed quiz requirements
    ChatbotController-->>Browser: Response about quiz passing scores, attempts, etc.
    Browser-->>L: Display comprehensive quiz information
```

### 6. Achievement Unlocking and Notification Flow

```mermaid
sequenceDiagram
    participant L as Learner
    participant CourseSystem as Course System
    participant AchievementUnlockService as AchievementUnlockService
    participant AchievementRepo as AchievementRepo
    participant NotificationService as NotificationService
    participant NotificationHub as NotificationHub (SignalR)
    participant Database as SQL Server
    participant Browser as Browser
    
    L->>CourseSystem: Complete course
    CourseSystem->>AchievementUnlockService: CheckCourseCompletionAchievementsAsync(userId, courseId)
    
    AchievementUnlockService->>AchievementRepo: GetAchievementsByTypeAsync("course_completion")
    AchievementRepo->>Database: SELECT FROM achievements WHERE type='course_completion'
    Database-->>AchievementRepo: Course completion achievements
    AchievementRepo-->>AchievementUnlockService: List of achievements
    
    AchievementUnlockService->>Database: SELECT COUNT FROM enrollments WHERE user_id AND certificate_issued
    Database-->>AchievementUnlockService: Completed courses count
    
    loop For each achievement
        AchievementUnlockService->>AchievementRepo: HasUserAchievementAsync(userId, achievementId)
        AchievementRepo->>Database: SELECT FROM user_achievements
        Database-->>AchievementRepo: Achievement status
        
        alt Achievement not yet unlocked AND criteria met
            AchievementUnlockService->>AchievementUnlockService: ProcessAchievementUnlockAsync(userId, achievementId)
            AchievementUnlockService->>Database: INSERT INTO user_achievements
            
            AchievementUnlockService->>NotificationService: SendAchievementNotificationAsync(userId, achievement)
            NotificationService->>Database: INSERT INTO notifications
            NotificationService->>NotificationHub: Send real-time notification
            NotificationHub->>Browser: ReceiveNotification(achievement unlocked)
            Browser-->>L: Show achievement popup notification
        end
    end
    
    AchievementUnlockService-->>CourseSystem: List of newly unlocked achievements
    
    L->>L: Continue learning activities
    Note over L,AchievementUnlockService: Achievements are checked automatically for:<br/>- Quiz completions<br/>- Learning streaks<br/>- Course enrollments<br/>- Engagement activities
    
    L->>CourseSystem: Complete quiz with high score
    CourseSystem->>AchievementUnlockService: CheckQuizAchievementsAsync(userId, quizId, score, isPassed)
    
    AchievementUnlockService->>Database: Get user quiz statistics
    AchievementUnlockService->>AchievementRepo: GetAchievementsByTypeAsync("quiz_master")
    
    loop Check quiz master achievements
        alt Score >= 90% AND achievement not unlocked
            AchievementUnlockService->>AchievementUnlockService: ProcessAchievementUnlockAsync(userId, "quiz_master_90")
            AchievementUnlockService->>NotificationService: SendAchievementNotificationAsync
            NotificationService->>NotificationHub: Send notification
            NotificationHub->>Browser: Achievement notification
            Browser-->>L: "Quiz Master - 90% Score!" achievement popup
        end
    end
    
    AchievementUnlockService-->>CourseSystem: Newly unlocked quiz achievements
```

## Sequence Flow Descriptions

### Authentication Flow
1. **Credential Validation**: Multi-step verification including user existence, ban status, and password verification
2. **Security Logging**: All login attempts are logged for security monitoring
3. **Claims Creation**: Comprehensive user information stored in authentication claims
4. **Role-based Redirection**: Users directed to appropriate dashboards based on their roles

### Course Enrollment Flow
1. **Enrollment Validation**: Check for existing enrollments to prevent duplicates
2. **Payment Processing**: Point-based system with transaction management
3. **Database Transactions**: Atomic operations ensure data consistency
4. **Real-time Notifications**: Immediate feedback via SignalR

### Quiz Assessment Flow
1. **Attempt Management**: Retry limits and time restrictions enforced
2. **Auto-grading**: Immediate scoring with detailed feedback
3. **Achievement Integration**: Automatic achievement checking upon completion
4. **Progress Tracking**: Course progression updated based on quiz results

### Real-time Communication
1. **Connection Management**: User online status tracking
2. **Message Delivery**: Real-time for online users, stored for offline users
3. **Read Receipts**: Message status tracking with visual indicators
4. **Typing Indicators**: Live interaction feedback

### AI Chatbot Integration
1. **Context Awareness**: Current course and progress considered in responses
2. **External AI Service**: Integration with Google AI via FastAPI service
3. **Conversation History**: Maintained for better context in follow-up questions
4. **Feedback Loop**: User ratings improve AI response quality

### Achievement System
1. **Automatic Monitoring**: Continuous checking of achievement criteria
2. **Multi-trigger Events**: Achievements can be unlocked by various activities
3. **Real-time Notifications**: Immediate notification delivery via SignalR
4. **Comprehensive Tracking**: Detailed statistics for achievement calculation

## Technical Implementation Details

- **SignalR**: Enables real-time bidirectional communication
- **Entity Framework**: ORM for database operations with transaction support
- **JWT Authentication**: Secure, stateless authentication with claims
- **Async/Await**: Non-blocking operations for better performance
- **Repository Pattern**: Data access abstraction for maintainability
- **Service Layer**: Business logic separation and reusability 