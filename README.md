# BrainStormEra

BrainStormEra is a modern e-learning platform designed to facilitate online courses, user progress tracking, achievements, and interactive chatbot conversations. Built with ASP.NET Core and Entity Framework Core, it provides a robust backend for managing users, courses, enrollments, and more.

## Features

- User authentication and role management
- Course creation, categorization, and enrollment
- Chapter and lesson management with ordering
- User progress tracking and achievements
- Feedback and rating system
- Chatbot conversation logging
- Notification and transaction support

## Tech Stack

- **Backend:** ASP.NET Core 8.0
- **ORM:** Entity Framework Core (SQL Server)
- **Frontend:** Razor Views (MVC)
- **Database:** Microsoft SQL Server
- **Other:** Bootstrap, jQuery

## Getting Started

1. Clone the repository.
2. Configure your database connection in `appsettings.json`.
3. Run database migrations to set up the schema (see `sql.txt`).
4. Build and run the project:
   ```powershell
   dotnet build
   dotnet run
   ```
5. Access the app at `https://localhost:5001` (or as configured).

## Project Structure

- `Controllers/` - MVC controllers
- `Models/` - Entity models and DbContext
- `Views/` - Razor views
- `wwwroot/` - Static files (CSS, JS, images)

## License

See [LICENSE](LICENSE) for details.
