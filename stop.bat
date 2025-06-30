@echo off
echo Stopping BrainStormEra Docker Services...
echo.

docker-compose down

echo.
echo All services have been stopped.
echo.
echo To remove all data (including database), run: docker-compose down -v
echo.
pause 