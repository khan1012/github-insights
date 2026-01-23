# Quick Start Guide

Get GitHub Insights Dashboard running in 3 minutes!

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- GitHub organization name

## Step 1: Clone and Navigate

```bash
git clone <repository-url>
cd github-insights/src
```

## Step 2: Configure

Edit `appsettings.json`:
```json
{
  "GitHub": {
    "Organization": "your-org-name",    // Change this!
    "Token": "",                         // required for higher rate limit on github api
    "CacheDurationMinutes": 5,          // Cache duration
    "MaxRepositories": 100,             // Max repos to fetch
    "MaxRepositoriesForContributorAnalysis": 20  // Max repos for deep analysis
  }
}
```

Replace `"your-org-name"` with your GitHub organization (e.g., "microsoft", "google").

**For large orgs (500+ repos):** Keep the default limits for faster loading, or increase if needed.

## Step 3: Run

```bash
dotnet run
```

## Step 4: Access

Open your browser:
- **HTTP**: http://localhost:5281

**That's it!** Your dashboard should be running. First load takes 5-15 seconds.

---

## Add GitHub Token (Recommended)

**Why?** Higher rate limits (5,000 vs 60 requests/hour)

1. Create token: https://github.com/settings/tokens/new
2. Name it: `GitHub Insights`
3. Select scopes: `read:org` and `repo`
4. Copy the token (starts with `ghp_`)
5. Add to `appsettings.json`: `"Token": "ghp_xxxxx"`
6. Restart: `dotnet run`

⚠️ **Never commit tokens to git!**

---

## Common Issues

**"Organization not found"** → Check spelling, verify at github.com/your-org

**"Rate limit exceeded"** → Add a GitHub token (see above)

**"Invalid token" or placeholder errors** → Replace `"Token": "${GITHUB_TOKEN}"` with actual token or leave empty `""`

**Port in use** → Change ports in `Properties/launchSettings.json` or kill the process

**No data showing** → Check internet connection and GitHub API status

**Loading too slow for large org?** → Reduce `MaxRepositories` to 100 and `MaxRepositoriesForContributorAnalysis` to 10
