# BrainStormEra 🚀

BrainStormEra is a comprehensive educational platform designed to provide interactive learning experiences. It includes multiple components such as MVC and Razor projects, a Business Logic Layer, a Data Access Layer, and a SQL Server database.

## Features ✨

- **MVC Application**: A web application built using ASP.NET Core MVC.
- **Razor Pages**: A lightweight web application using Razor Pages.
- **Business Logic Layer**: Encapsulates the core business logic of the application.
- **Data Access Layer**: Handles database interactions.
- **SQL Server Database**: Stores application data.

## Tech Stack 🛠️

- **Frontend**: Razor Pages, HTML, CSS, JavaScript
- **Backend**: ASP.NET Core MVC, C#
- **Database**: SQL Server
- **Containerization**: Docker, Docker Compose
- **Version Control**: Git, GitHub

## Prerequisites ✅

- Docker and Docker Compose installed on your system.
- .NET 6.0 SDK installed.

## Getting Started 🏁

### Clone the Repository

```bash
git clone https://github.com/LTPPPP/BrainStormEra-v2.git
cd BrainStormEra
```

### Build and Run with Docker Compose

1. Ensure Docker is running on your system.
2. Run the following command to build and start all services:
   ```bash
   docker-compose up --build
   ```
3. Access the applications:
   - MVC Application: [http://localhost:5000](http://localhost:5000)
   - Razor Application: [http://localhost:5001](http://localhost:5001)

### Project Structure 📂

- **BrainStormEra-MVC/**: Contains the MVC application.
- **BrainStormEra-Razor/**: Contains the Razor Pages application.
- **BusinessLogicLayer/**: Contains the business logic.
- **DataAccessLayer/**: Contains the data access logic.
- **docker-compose.yml**: Defines the services and their configurations.

### Environment Variables 🌐

- `ASPNETCORE_ENVIRONMENT`: Set to `Development` for local development.
- `SA_PASSWORD`: Password for the SQL Server database.
- `ACCEPT_EULA`: Set to `Y` to accept the SQL Server license agreement.

### Volumes 💾

- `sql_data`: Persists SQL Server data.

## Contributing 🤝

Contributions are welcome! Please fork the repository and submit a pull request.

## License 📜

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Contact 📧

For any inquiries, please contact [your-email@example.com].
