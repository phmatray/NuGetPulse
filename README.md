# 📦 NuGetPulse

[![Build](https://github.com/phmatray/NuGetPulse/actions/workflows/ci.yml/badge.svg)](https://github.com/phmatray/NuGetPulse/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com)
[![Tests](https://img.shields.io/badge/tests-57%20passing-brightgreen.svg)](#)

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
│   └── NuGetPulse.Tests/        # 57 unit + integration tests
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
# 57 tests, 0 failures
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

NuGetPulse has **57 unit and integration tests** across all layers:

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

## Contributing

Pull requests are welcome. For major changes, open an issue first.

```bash
# Fork → branch → change → test → PR
git checkout -b feat/my-improvement
dotnet test  # must pass before PR
```

---

## License

[MIT](LICENSE) © 2026 [Philippe Matray](https://github.com/phmatray) / [Atypical Consulting](https://atypical.garry-ai.cloud)
