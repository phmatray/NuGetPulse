# 📦 NuGetPulse

[![Build](https://github.com/phmatray/NuGetPulse/actions/workflows/ci.yml/badge.svg)](https://github.com/phmatray/NuGetPulse/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com)
[![Tests](https://img.shields.io/badge/tests-63%20passing-brightgreen.svg)](#)

**NuGetPulse** is the all-in-one NuGet health platform for .NET teams.  
Search packages, scan your projects for vulnerabilities and version conflicts, track history, and export reports — all in one dark-mode Blazor dashboard.

> 🌐 **Live demo:** [https://nugetpulse.garry-ai.cloud](https://nugetpulse.garry-ai.cloud)

---

## ✨ Features

### 🔍 Package Search & Health Score
- Real-time search against **api.nuget.org** with download counts, versions, authors
- **Health Score (0–100)** per package — composite metric: downloads + freshness + OSV vulnerabilities + deprecation
- Click any package for a detailed dashboard: metadata, TFM support matrix, vulnerability report

### 🔎 Project Scanner
- Paste a directory path and scan all `.csproj`, `.fsproj`, `packages.config`, `Directory.Packages.props` files
- **Central Package Management (CPM)** support
- **Version conflict detection** — highlights packages with multiple versions across projects, with severity (Major / Minor / Patch) and a suggested fix
- Results show per-package vulnerability status inline (Critical / High / Medium / Low / Safe)
- Export results to **CSV** or **JSON** with one click

### 🛡️ OSV Vulnerability Scanning
- Every scanned package is checked against the [OSV database](https://osv.dev) (api.osv.dev) **asynchronously** — the UI updates live while scanning
- Severity levels: **Critical · High · Medium · Low · Unknown**
- Full vulnerability detail panel: ID, CVE aliases, summary, reference link
- Scan-level **Health Score** computed from real conflict + vulnerability data

### 📜 Scan History
- Every scan is persisted in a local SQLite database (EF Core)
- Dedicated `/history` page — see all past scans, package counts, vulnerability totals, duration, status
- Purge scans older than 30 days with one click

### 📦 Self-hosted NuGet Server
- File-system backed NuGet package store (`NuGetPulse.Server`)
- Push, list, download `.nupkg` packages
- Clean ports-and-adapters architecture

---

## 🏗️ Project Structure

```
NuGetPulse/
├── src/
│   ├── NuGetPulse.Web/          # Blazor Server dashboard (net10.0)
│   ├── NuGetPulse.Core/         # Shared models, interfaces, health scoring
│   ├── NuGetPulse.Scanner/      # .csproj / .sln package scanner
│   ├── NuGetPulse.Security/     # OSV vulnerability scanner
│   ├── NuGetPulse.Graph/        # Dependency graph + conflict detection
│   ├── NuGetPulse.Persistence/  # EF Core SQLite (scan history)
│   ├── NuGetPulse.Export/       # CSV / JSON export
│   └── NuGetPulse.Server/       # Self-hosted NuGet package store
├── tests/
│   └── NuGetPulse.Tests/        # 63 unit + integration tests
├── k8s/                         # Kubernetes manifests
├── Dockerfile
├── global.json                  # SDK 10.0.103 pinned
└── Directory.Packages.props     # Central Package Management
```

---

## 🚀 Quick Start

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Git

### Run the dashboard locally

```bash
git clone https://github.com/phmatray/NuGetPulse.git
cd NuGetPulse
dotnet restore
dotnet run --project src/NuGetPulse.Web
```

Open [http://localhost:5000](http://localhost:5000).

### Run via Docker

```bash
docker build -t nugetpulse .
docker run -p 8080:8080 nugetpulse
```

### Run tests

```bash
dotnet test
# 63 tests, 0 failures
```

---

## 📖 Usage

### Search a package
Navigate to `/` (Home), type any package name (e.g. `Newtonsoft.Json`), and click Search.  
Click a result to open the full health dashboard for that package.

### Scan your project
1. Navigate to **Scan Projects** in the sidebar (or `/scan`)
2. Enter a name and the absolute path to your solution directory
3. Click **Start Scan**
4. NuGetPulse will:
   - Discover all `.csproj`, `.sln`, `packages.config`, `Directory.Packages.props` files
   - Build a dependency graph and detect version conflicts
   - Run an async OSV vulnerability scan for every unique package/version
   - Display results with inline severity badges
   - Save the scan to history

### View scan history
Navigate to **Scan History** (`/history`) to see all past scans with package counts, vulnerability totals, and status.

### Export results
After a scan, use the **Export CSV** or **Export JSON** buttons to download the package list.

---

## 🧮 Health Score Algorithm

| Factor | Weight | Scoring |
|--------|--------|---------|
| Downloads | 30% | Log-scale normalised (10M+ → 100) |
| Freshness | 30% | Days since last publish (≤30 days → 100) |
| Vulnerabilities | 25% | 0 vulns = 100; each vuln −25 (min 0) |
| Deprecation | 15% | Not deprecated = 100; deprecated = 0 |

| Score | Status |
|-------|--------|
| ≥ 80 | 🟢 Healthy |
| 60–79 | 🟡 Warning |
| < 60 | 🔴 Critical |

---

## 🧪 Tests

NuGetPulse has **63 unit and integration tests** across all layers:

| Module | Tests | Coverage |
|--------|-------|----------|
| `NuGetPulse.Core` | HealthScore computation | Scores, status thresholds, edge cases |
| `NuGetPulse.Scanner` | PackageScanner | `.csproj`, `packages.config`, CPM, directory scan, error handling |
| `NuGetPulse.Security` | OsvClient | HTTP mock, severity parsing, batch scan, error resilience |
| `NuGetPulse.Graph` | DependencyGraphBuilder | Conflicts, severity, node deduplication |
| `NuGetPulse.Export` | PackageExportService | CSV output, JSON structure, file names |
| `NuGetPulse.Persistence` | ScanHistoryRepository | Save, query, purge, history (SQLite in-memory) |

---

## 🗺️ Roadmap

- [x] Blazor scanner UI with vulnerability display
- [x] OSV async scanning in the dashboard
- [x] Version conflict detection with severity
- [x] Scan history persistence (EF Core SQLite)
- [x] CSV / JSON export
- [ ] GitHub Actions integration — block PRs on unhealthy deps
- [ ] Email / Slack alerts on score drops
- [ ] Historical trend charts per package
- [ ] Git repository scanning via URL
- [ ] NuGet lock file support

---

## 🧩 Absorbed Repositories

NuGetPulse consolidates four previously separate tools:

| Repo | What it contributed |
|------|---------------------|
| `NuGetPulse` (original) | Blazor dashboard, health scoring |
| `NugetManager` | Package scanning, dependency graph |
| `NugetOSV` | OSV vulnerability scanning |
| `NugetServer` | Self-hosted NuGet server |

---

## 🚢 Deployment

### Docker

The simplest way to deploy NuGetPulse is via Docker:

```bash
# Build the image
docker build -t nugetpulse:latest .

# Run the container
docker run -d \
  -p 8080:8080 \
  -v /path/to/data:/app/data \
  --name nugetpulse \
  nugetpulse:latest
```

The SQLite database and scan history will be persisted in `/app/data`.

### Kubernetes

NuGetPulse includes Kubernetes manifests in the `k8s/` directory:

```bash
# Create namespace
kubectl create namespace nugetpulse

# Apply manifests
kubectl apply -f k8s/ -n nugetpulse

# Check deployment
kubectl get pods -n nugetpulse
kubectl get svc -n nugetpulse
```

**Persistent Storage:**
Ensure you configure a PersistentVolumeClaim for `/app/data` to persist scan history across pod restarts.

### Docker Compose (Development)

For local development with hot-reload:

```yaml
version: '3.8'
services:
  nugetpulse:
    build: .
    ports:
      - "5000:8080"
    volumes:
      - ./data:/app/data
      - ./src:/app/src:ro
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
```

Save as `docker-compose.yml` and run:
```bash
docker-compose up
```

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_URLS` | `http://+:8080` | Listening address |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Environment (`Development`, `Staging`, `Production`) |
| `ConnectionStrings__DefaultConnection` | `Data Source=data/nugetpulse.db` | SQLite database path |

### Production Best Practices

1. **HTTPS**: Use a reverse proxy (nginx, Traefik, Azure App Service) with TLS
2. **Persistent Storage**: Mount a volume for `/app/data` to persist SQLite database
3. **Resource Limits**: Set CPU/memory limits in Kubernetes to prevent resource exhaustion during large scans
4. **Monitoring**: Check logs via `kubectl logs` or Docker logs
5. **Backups**: Regularly backup the SQLite database (`/app/data/nugetpulse.db`)

---

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on:
- Development setup and workflow
- Code standards and architecture
- Testing requirements
- Pull request process

Quick start for contributors:

```bash
# Fork and clone
git clone https://github.com/YOUR-USERNAME/NuGetPulse.git
cd NuGetPulse

# Create a feature branch
git checkout -b feat/my-improvement

# Make changes, add tests
dotnet test  # must pass before PR

# Commit with conventional commits
git commit -m "feat(scanner): add support for paket.dependencies"

# Push and create PR
git push origin feat/my-improvement
```

For bug reports and feature requests, please [open an issue](https://github.com/phmatray/NuGetPulse/issues/new).

---

## License

[MIT](LICENSE) © 2026 [Philippe Matray](https://github.com/phmatray) / [Atypical Consulting](https://atypical.garry-ai.cloud)
