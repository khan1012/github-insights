# Deployment Guide - GitHub Insights

## Prerequisites

### Option 1: .NET SDK (Direct Run)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- macOS, Linux, or Windows

### Option 2: Docker (Recommended)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) or Docker Engine
- 4GB RAM minimum, 8GB recommended

---

## Quick Start with .NET SDK

### 1. Install .NET SDK
```bash
# macOS (via Homebrew)
brew install --cask dotnet-sdk

# Or download from: https://dotnet.microsoft.com/download/dotnet/10.0
```

### 2. Configure Application
Edit `src/appsettings.json`:
```json
{
  "GitHub": {
    "Organization": "microsoft",  // Change to your org
    "Token": ""  // Optional: Add GitHub PAT for higher rate limits
  }
}
```

### 3. Run Application
```bash
cd src
dotnet restore
dotnet run
```

### 4. Access Application
- **API**: http://localhost:5281
- **Swagger**: http://localhost:5281/api-docs
- **Health Check**: http://localhost:5281/health
- **Dashboard**: http://localhost:5281/index.html

---

## Quick Start with Docker

### 1. Start Docker
```bash
# macOS: Open Docker Desktop
# Linux: sudo systemctl start docker
```

### 2. Build and Run
```bash
# Simple run
docker build -t github-insights .
docker run -p 5281:8080 -e GitHub__Organization=microsoft github-insights

# Or use docker-compose (recommended)
docker-compose up
```

### 3. Access Application
- **API**: http://localhost:5281
- **Swagger**: http://localhost:5281/api-docs
- **Health Check**: http://localhost:5281/health

---

## Production Deployment

### Environment Variables
```bash
# Required
GITHUB_ORG=your-organization
GITHUB_TOKEN=ghp_your_token_here

# Optional Performance Tuning
GITHUB_CACHE_MINUTES=5
GITHUB_MAX_REPOS=100
PERFORMANCE_MAX_CONCURRENT=10
RESILIENCE_MAX_RETRIES=3
```

### Docker Compose (Production)
```yaml
services:
  github-insights:
    image: github-insights:latest
    ports:
      - "80:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - GitHub__Organization=${GITHUB_ORG}
      - GitHub__Token=${GITHUB_TOKEN}
    restart: always
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health/ready"]
      interval: 30s
      timeout: 10s
      retries: 3
```

### Kubernetes Deployment
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: github-insights
spec:
  replicas: 2
  selector:
    matchLabels:
      app: github-insights
  template:
    metadata:
      labels:
        app: github-insights
    spec:
      containers:
      - name: github-insights
        image: github-insights:latest
        ports:
        - containerPort: 8080
        env:
        - name: GitHub__Organization
          value: "microsoft"
        - name: GitHub__Token
          valueFrom:
            secretKeyRef:
              name: github-secrets
              key: token
        livenessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
```

---

## Configuration Options

### GitHub Settings (`appsettings.json`)
```json
{
  "GitHub": {
    "Organization": "microsoft",          // Required: GitHub organization name
    "Token": "",                          // Optional: GitHub PAT (ghp_xxx)
    "CacheDurationMinutes": 5,           // API response cache duration
    "MaxRepositories": 100,               // Limit repos fetched
    "MaxRepositoriesForContributorAnalysis": 20  // Limit deep analysis
  }
}
```

### Performance Tuning
```json
{
  "Performance": {
    "MaxConcurrentRequests": 10,         // Parallel API calls
    "HealthCheckStaleDays": 180,         // Repo staleness threshold
    "HealthCheckAttentionDays": 30,      // Repo attention threshold
    "ActivityScoreStarsWeight": 10,      // Star weight in scoring
    "ActivityScoreForksWeight": 5,       // Fork weight in scoring
    "ActivityScoreIssuesWeight": 2       // Issue weight in scoring
  }
}
```

### Resilience & Retry
```json
{
  "Resilience": {
    "MaxRetries": 3,                     // Max retry attempts
    "BaseDelaySeconds": 2,               // Exponential backoff base
    "CircuitBreakerThreshold": 5,        // Failures before circuit opens
    "CircuitBreakerDurationSeconds": 60, // Circuit break duration
    "TimeoutSeconds": 30                 // HTTP request timeout
  }
}
```

### Rate Limiting
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 60                      // 60 requests per minute
      },
      {
        "Endpoint": "*",
        "Period": "1h",
        "Limit": 1000                    // 1000 requests per hour
      }
    ]
  }
}
```

---

## Monitoring & Observability

### Health Checks
- **Detailed**: http://localhost:5281/health
- **Readiness**: http://localhost:5281/health/ready

### Logs
- **Console**: Real-time structured logs via Serilog
- **File**: `logs/github-insights-YYYY-MM-DD.log` (7-day retention)

### Metrics
Monitor these endpoints:
- `/health` - Application health status
- `/health/ready` - Kubernetes readiness probe

---

## Troubleshooting

### .NET SDK Not Found
```bash
# macOS
brew install --cask dotnet-sdk

# Linux (Ubuntu/Debian)
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0
```

### Docker Build Fails
```bash
# Clear Docker cache
docker system prune -a

# Rebuild without cache
docker build --no-cache -t github-insights .
```

### Port Already in Use
```bash
# Find process using port 5281
lsof -i :5281

# Kill process
kill -9 <PID>

# Or change port in docker-compose.yml
ports:
  - "8080:8080"  # Use different port
```

### GitHub Rate Limit Exceeded
1. Add GitHub Personal Access Token to configuration
2. Increase `CacheDurationMinutes` to reduce API calls
3. Reduce `MaxRepositories` and `MaxRepositoriesForContributorAnalysis`

---

## CI/CD Pipeline

The project includes GitHub Actions workflows:

### `.github/workflows/ci-cd.yml`
- Runs on every push/PR
- Builds, tests, and creates Docker image
- Runs security scans
- Publishes test results

### `.github/workflows/release.yml`
- Triggers on version tags (`v*.*.*`)
- Creates GitHub release with artifacts
- Publishes Docker image to GitHub Container Registry

---

## Architecture

### Technology Stack
- **Framework**: ASP.NET Core 10.0
- **Logging**: Serilog with file/console sinks
- **Resilience**: Polly (retry, circuit breaker)
- **Caching**: In-memory with configurable TTL
- **API Docs**: Swagger/OpenAPI
- **Rate Limiting**: AspNetCoreRateLimit
- **Testing**: xUnit, Moq, FluentAssertions

### Design Patterns
- **Dependency Injection**: All services registered with DI container
- **Repository Pattern**: Abstracted data fetching
- **Circuit Breaker**: Protects against cascading failures
- **Retry with Exponential Backoff**: Handles transient failures
- **Structured Logging**: Correlation IDs and context enrichment

---

## Security Best Practices

✅ **Implemented**:
- Security headers (HSTS, CSP, X-Frame-Options)
- API rate limiting
- Non-root Docker user
- Token validation and sanitization
- HTTPS enforcement in production

⚠️ **Additional Recommendations**:
- Store GitHub tokens in secrets manager (Azure Key Vault, AWS Secrets Manager)
- Enable authentication/authorization for production APIs
- Implement API key authentication
- Set up WAF (Web Application Firewall)
- Enable container image scanning in CI/CD

---

## Support & Documentation

- **API Documentation**: http://localhost:5281/api-docs
- **Health Dashboard**: http://localhost:5281/health
- **GitHub Repository**: [Your repo URL]
- **Issues**: [Your repo URL]/issues
