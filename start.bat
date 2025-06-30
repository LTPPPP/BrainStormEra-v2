@echo off
echo Starting BrainStormEra Docker Services...
echo.

echo Building and starting all services...
docker-compose up -d --build

echo.
echo Waiting for services to be ready...
timeout /t 10 /nobreak > nul

echo.
echo Services status:
docker-compose ps

echo.
echo Applications are now available at:
echo - MVC Application: http://localhost:5000
echo - Razor Application: http://localhost:5001
echo.
echo To stop the services, run: docker-compose down
echo To view logs, run: docker-compose logs -f
echo.
pause 