# Use the official .NET SDK image as a build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file and project files
COPY BrainStormEra.sln ./
COPY BrainStormEra-MVC/*.csproj ./BrainStormEra-MVC/
COPY DataAccessLayer/*.csproj ./DataAccessLayer/
COPY BusinessLogicLayer/*.csproj ./BusinessLogicLayer/

# Restore dependencies
RUN dotnet restore BrainStormEra-MVC/BrainStormEra-MVC.csproj

# Copy all source code
COPY BrainStormEra-MVC/ ./BrainStormEra-MVC/
COPY DataAccessLayer/ ./DataAccessLayer/
COPY BusinessLogicLayer/ ./BusinessLogicLayer/

# Build and publish the application
WORKDIR /src/BrainStormEra-MVC
RUN dotnet publish -c Release -o /app/publish --no-restore

# Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Expose the port and set environment variables
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "BrainStormEra-MVC.dll"]
