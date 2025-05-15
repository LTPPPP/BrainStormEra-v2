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

## Running with Docker

You can run BrainStormEra and its SQL Server database using Docker Compose for easy local development:

1. Ensure you have [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed.
2. Build and start the containers:
   ```powershell
   docker-compose up -d
   ```
3. The web app will be available at `http://localhost:5000`.
4. The SQL Server database will be accessible at `localhost:1433` (see `docker-compose.yml` for credentials).

### Environment Variables

- The database connection string is set via the `ConnectionStrings__DefaultConnection` environment variable in `docker-compose.yml`.
- Update the `SA_PASSWORD` in `docker-compose.yml` as needed for security.

### Stopping the Containers

To stop the containers, press `Ctrl+C` in the terminal or run:

```powershell
docker-compose down
```

## Project Structure

- `Controllers/` - MVC controllers
- `Models/` - Entity models and DbContext
- `Views/` - Razor views
- `wwwroot/` - Static files (CSS, JS, images)

## Commit Message Guidelines

To keep the project history clean and meaningful, use the following commit message types:

- **feat:** Add a new feature to the project.
  - _Example:_ `feat: add user registration functionality`
- **fix:** Fix a minor bug, logic error, or make a small update that does not significantly affect the system.
  - _Example:_ `fix: correct user name display bug`
- **bug:** Fix a critical bug that affects core functionality.
  - _Example:_ `bug: resolve login failure issue`
- **hot-fix:** Apply an urgent fix, usually for production issues.
  - _Example:_ `hot-fix: patch login security vulnerability`
- **refactor:** Refactor code without changing its external behavior.
  - _Example:_ `refactor: optimize controller structure`
- **docs:** Update documentation, README, or usage guides.
  - _Example:_ `docs: update installation instructions`
- **style:** Code style changes (formatting, missing semi-colons, etc.) that do not affect logic.
  - _Example:_ `style: standardize code style in models`
- **test:** Add or update tests, unit tests, or test infrastructure.
  - _Example:_ `test: add tests for login functionality`
- **chore:** Maintenance tasks, dependency updates, configuration changes, not affecting main source code.
  - _Example:_ `chore: update EntityFramework package`

## Contributing

We welcome contributions from the community! To contribute:

1. Fork this repository.
2. Create a new branch for your feature or fix.
3. Make your changes and commit them using the commit message guidelines above.
4. Push your branch and open a Pull Request (PR) with a clear description of your changes.
5. Wait for review and feedback.

Please ensure your code follows the project's coding standards and passes all tests before submitting a PR.

## License

See [LICENSE](LICENSE) for details.
