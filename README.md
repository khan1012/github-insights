# GitHub Insights Dashboard

> **Enterprise-grade analytics platform for GitHub organizations** - Built with ASP.NET Core 10.0

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Docker](https://img.shields.io/badge/docker-ready-2496ED)](https://www.docker.com/)

Comprehensive analytics dashboard that provides deep insights into your GitHub organization's repositories, contributors, health metrics, and community reach.

![GitHub Insights Dashboard](docs/screenshot.png)

---

## ✨ Features

### 📊 **Repository Analytics**
- Real-time statistics (stars, forks, watchers, issues)
- Language distribution analysis
- Repository health scoring
- Activity trends and engagement metrics
- Top 10 most active repositories

### 👥 **Contributor Insights**
- Internal vs external contributor breakdown
- Top contributors with detailed metrics
- Social reach analysis (follower counts)
- Contribution patterns across repositories

### 🏥 **Health Monitoring**
- Repository health classification (healthy/needs attention/at risk)
- Stale repository detection
- Issue density analysis
- Update frequency tracking

### 🔗 **Dependency Analysis**
- Dependent package detection
- Repository impact assessment
- Package ecosystem insights

### ⚡ **Performance & Reliability**
- Smart caching (5-minute default TTL)
- Parallel API processing with concurrency control
- Retry logic with exponential backoff
- Circuit breaker pattern for fault tolerance
- Configurable rate limiting

### 🔒 **Security & Production-Ready**
- Security headers (HSTS, CSP, X-Frame-Options)
- API rate limiting (60/min, 1000/hour)
- Structured logging with Serilog
- Health check endpoints
- Docker containerization
- CI/CD pipeline included

---

## 🚀 Quick Start

### Prerequisites

**Option 1: .NET SDK**
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- macOS, Linux, or Windows

**Option 2: Docker** (Recommended)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) or Docker Engine

### 🎯 Run with Script (Easiest)

```bash
./run.sh
```

The script will guide you through setup and automatically choose the best deployment method.

### 🐳 Run with Docker

```bash
# Start application
docker-compose up

# Or build manually
docker build -t github-insights .
docker run -p 5281:8080 -e GitHub__Organization=microsoft github-insights
```

### 💻 Run with .NET SDK

```bash
cd src
dotnet restore
dotnet run
```

### 🌐 Access the Application

- **Dashboard**: http://localhost:5281
- **API Docs (Scalar)**: http://localhost:5281/scalar/v1
- **Health Check**: http://localhost:5281/health

---

## ⚙️ Configuration

Edit `src/appsettings.json`:

```json
{
  "GitHub": {
    "Organization": "microsoft",     // Your GitHub org name
    "Token": "",                      // Optional: GitHub PAT (ghp_xxx)
    "CacheDurationMinutes": 5,        // API cache duration
    "MaxRepositories": 100,           // Limit repos fetched
    "MaxRepositoriesForContributorAnalysis": 20
  },
  "Performance": {
    "MaxConcurrentRequests": 10,      // Parallel API calls
    "HealthCheckStaleDays": 180,      // Repo staleness threshold
    "HealthCheckAttentionDays": 30
  },
  "Resilience": {
    "MaxRetries": 3,                  // API retry attempts
    "CircuitBreakerThreshold": 5,     // Failures before circuit opens
    "TimeoutSeconds": 30              // Request timeout
  }
}
```

### 🔑 GitHub Token (Recommended)

**Why?** Increases rate limit from 60 to 5,000 requests/hour

1. Create token: https://github.com/settings/tokens/new
2. Select scopes: `read:org` and `repo`
3. Copy token (starts with `ghp_`)
4. Add to `appsettings.json`: `"Token": "ghp_xxxxx"`

⚠️ **Never commit tokens to git!** They're already in `.gitignore`.

---

## 📡 API Endpoints

### Repository Endpoints
- `GET /api/github/repos/count` - Total repository count
- `GET /api/github/repos/details` - Detailed statistics
- `GET /api/github/repos/basic` - Fast basic stats
- `GET /api/github/insights/detailed` - Comprehensive insights

### Contributor Endpoints
- `GET /api/github/repos/contributors` - Contributor analysis
- `GET /api/github/repos/topcontributors` - Top contributors
- `GET /api/github/repos/followerreach` - Social reach metrics

### Analysis Endpoints
- `GET /api/github/repos/dependents` - Dependency analysis
- `GET /health` - Health check with details
- `GET /health/ready` - Readiness probe

Full API documentation available at `/api-docs` when running.

---

## 🏗️ Architecture

```
Frontend (Static) → API Controllers → Domain Services → Analyzers → GitHub API
                           ↓
                    Middleware Pipeline
                    (Security, Rate Limiting, Caching, Logging)
```

**Key Patterns:**
- Dependency Injection (DI) for all services
- Repository pattern for data access
- Circuit Breaker for fault tolerance
- Retry with exponential backoff
- Cache-aside pattern
- Structured logging

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed technical documentation.

---

## 🧪 Testing

```bash
# Run all tests
dotnet test

# With coverage
dotnet test /p:CollectCoverage=true

# Run specific test class
dotnet test --filter "FullyQualifiedName~ConfigurationOptionsTests"
```

---

## 📦 Deployment

### Docker Deployment

```bash
docker-compose up -d
```

### Kubernetes

```bash
kubectl apply -f k8s/deployment.yml
```

### Manual Deployment

```bash
dotnet publish -c Release -o ./publish
cd publish
dotnet GitHubInsights.dll
```

See [DEPLOYMENT.md](DEPLOYMENT.md) for comprehensive deployment guide including:
- Environment configuration
- Production checklist
- Kubernetes manifests
- Scaling strategies

---

## 🔧 Development

### Project Structure

```
github-insights/
├── src/
│   ├── Configuration/      # Strongly-typed config classes
│   ├── Controllers/         # API endpoints
│   ├── Services/            # Business logic & analyzers
│   ├── Middleware/          # Request pipeline components
│   ├── Models/              # Data models & DTOs
│   └── wwwroot/             # Static frontend files
├── test/
│   ├── Configuration/       # Config tests
│   ├── Services/            # Service tests
│   └── Controllers/         # API tests
├── Dockerfile               # Container definition
├── docker-compose.yml       # Local development setup
└── .github/workflows/       # CI/CD pipelines
```

### Technology Stack

- **Backend**: ASP.NET Core 10.0
- **Logging**: Serilog (console + file)
- **Resilience**: Polly (retry + circuit breaker)
- **Documentation**: Swagger/OpenAPI
- **Testing**: xUnit, Moq, FluentAssertions
- **Caching**: In-memory with configurable TTL
- **Rate Limiting**: AspNetCoreRateLimit

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🐛 Troubleshooting

### Common Issues

**Port already in use**
```bash
# Find process
lsof -i :5281
# Kill process
kill -9 <PID>
```

**GitHub rate limit exceeded**
- Add GitHub token to configuration
- Increase `CacheDurationMinutes`
- Reduce `MaxRepositories`

**Docker build fails**
```bash
docker system prune -a
docker build --no-cache -t github-insights .
```

**Logs not appearing**
- Check `logs/` directory exists
- Verify permissions on logs folder
- Check Serilog configuration in `Program.cs`

For more troubleshooting, see [DEPLOYMENT.md](DEPLOYMENT.md#troubleshooting).

---

## 📊 Metrics & Observability

- **Logs**: Rolling file logs in `logs/` (7-day retention)
- **Health Checks**: `/health` and `/health/ready`
- **Structured Logging**: JSON-formatted logs with correlation IDs
- **Performance**: Cache hit rates logged at debug level

---

## 🗺️ Roadmap

- [ ] Real-time updates via SignalR
- [ ] Historical trend analysis
- [ ] Custom dashboard builder
- [ ] Multi-organization support
- [ ] GraphQL API support
- [ ] Redis distributed caching
- [ ] Prometheus metrics export

---

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/your-repo/issues)
- **Documentation**: See [DEPLOYMENT.md](DEPLOYMENT.md) and [ARCHITECTURE.md](ARCHITECTURE.md)
- **API Docs**: http://localhost:5281/api-docs (when running)

---

**Made with ❤️ using ASP.NET Core**
