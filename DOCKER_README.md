# BrainStormEra Docker Setup

This project includes comprehensive Docker configuration for easy development and deployment.

## 🐳 Docker Files Overview

- **`docker-compose.yml`** - Main compose file with base configuration
- **`docker-compose.override.yml`** - Development overrides (automatically used with `docker-compose up`)
- **`docker-compose.prod.yml`** - Production configuration
- **`Dockerfile`** (in MVC/Razor folders) - Application container definitions
- **`.dockerignore`** - Excludes unnecessary files from build context

## 🚀 Quick Start

### Development Environment

```bash
# Start all services in development mode
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

### Production Environment

```bash
# Start with production configuration
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Scale services (production only)
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d --scale mvc=3 --scale razor=2
```

## 🏗️ Architecture

### Services

1. **mvc** (Port 5000)
   - BrainStormEra MVC application
   - .NET 8.0 ASP.NET Core
   - Depends on SQL Server

2. **razor** (Port 5001)
   - BrainStormEra Razor Pages application
   - .NET 8.0 ASP.NET Core
   - Depends on SQL Server

3. **database** (Port 1433)
   - SQL Server 2022 Express (dev) / Standard (prod)
   - Persistent data volume
   - Health checks enabled

### Network

All services communicate through a custom bridge network `brainstormera-network`.

## 🔧 Configuration

### Environment Variables

The applications are configured with:
- `ASPNETCORE_ENVIRONMENT` - Development/Production
- `ASPNETCORE_URLS` - Binding URLs
- `ConnectionStrings__DefaultConnection` - Database connection

### Database Connection

```
Server=database,1433;Database=BrainStormEra;User Id=sa;Password=YourStrong!Password123;TrustServerCertificate=true;MultipleActiveResultSets=true
```

## 📝 Common Commands

### Building Images

```bash
# Build all images
docker-compose build

# Build specific service
docker-compose build mvc

# Build with no cache
docker-compose build --no-cache
```

### Managing Services

```bash
# Start specific service
docker-compose up mvc

# Restart service
docker-compose restart mvc

# View service logs
docker-compose logs mvc

# Execute command in running container
docker-compose exec mvc bash
```

### Database Management

```bash
# Connect to SQL Server
docker-compose exec database /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong!Password123

# Backup database volume
docker run --rm -v brainstormera_sql_data:/data -v $(pwd):/backup alpine tar czf /backup/database-backup.tar.gz -C /data .

# Restore database volume
docker run --rm -v brainstormera_sql_data:/data -v $(pwd):/backup alpine tar xzf /backup/database-backup.tar.gz -C /data
```

## 🐛 Troubleshooting

### Common Issues

1. **Port conflicts**
   ```bash
   # Check what's using the port
   netstat -tlnp | grep :5000
   
   # Change ports in docker-compose.override.yml
   ```

2. **Database connection issues**
   ```bash
   # Check database health
   docker-compose ps database
   
   # Check database logs
   docker-compose logs database
   ```

3. **Build failures**
   ```bash
   # Clean and rebuild
   docker-compose down
   docker system prune -f
   docker-compose build --no-cache
   ```

### Performance Optimization

1. **Use multi-stage builds** ✅ (Already implemented)
2. **Layer caching** ✅ (Copy package files first)
3. **Minimal base images** ✅ (Using aspnet runtime)
4. **.dockerignore** ✅ (Excludes unnecessary files)

## 🔒 Security Considerations

### Development
- Default SA password (change in production)
- Development certificates
- Open ports for debugging

### Production
- Use secrets management for passwords
- Enable HTTPS with proper certificates
- Restrict network access
- Use non-root users in containers
- Regular security updates

## 📊 Monitoring

### Health Checks

- Database service includes SQL Server health check
- Web applications can be monitored via HTTP endpoints

### Logging

```bash
# Follow all logs
docker-compose logs -f

# Filter logs by service
docker-compose logs -f mvc

# Export logs
docker-compose logs --no-color > application.log
```

## 🚀 Deployment

### Docker Swarm

```bash
# Initialize swarm
docker swarm init

# Deploy stack
docker stack deploy -c docker-compose.yml -c docker-compose.prod.yml brainstormera

# Monitor stack
docker stack services brainstormera
```

### Environment-Specific Configs

Create environment-specific compose files:
- `docker-compose.staging.yml`
- `docker-compose.prod.yml`
- `docker-compose.test.yml`

## 📋 Maintenance

### Regular Tasks

```bash
# Update images
docker-compose pull

# Clean unused resources
docker system prune -f

# Update and restart services
docker-compose pull && docker-compose up -d

# Backup volumes
docker run --rm -v brainstormera_sql_data:/data -v $(pwd):/backup alpine tar czf /backup/backup-$(date +%Y%m%d).tar.gz -C /data .
```

## 🤝 Contributing

When making changes to Docker configuration:

1. Test with development compose first
2. Verify production compatibility
3. Update this README if needed
4. Test build performance impact

---

For more information about the application itself, see the main [README.md](README.md). 