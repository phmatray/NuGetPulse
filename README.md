# 📦 NuGetPulse

[![Build](https://github.com/phmatray/NuGetPulse/actions/workflows/ci.yml/badge.svg)](https://github.com/phmatray/NuGetPulse/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com)

**NuGetPulse** is the all-in-one NuGet toolkit for .NET teams.  
It consolidates four previously separate tools into a single, cohesive product:

| Absorbed repo | What it contributed |
|---|---|
| `NuGetPulse` (original) | Blazor dashboard, health scoring |
| `NugetManager` | Package scanning, dependency graph, Git/GitHub integration |
| `NugetOSV` | OSV vulnerability scanning (concept → real implementation) |
| `NugetServer` | Self-hosted NuGet gRPC server |

> 🌐 **Live demo:** [https://nugetpulse.garry-ai.cloud](https://nugetpulse.garry-ai.cloud)

---

## Features

### 🔍 Dashboard (`NuGetPulse.Web`)
- Real-time NuGet API integration — live metadata from api.nuget.org
- **Health Score (0–100)** — composite metric: downloads + freshness + vulnerability + deprecation
- Dark mode, Blazor Server, SignalR-live updates
- Docker + Kubernetes ready, .NET 10

### 🔎 Package Scanner (`NuGetPulse.Scanner`)
- Parse `.csproj`, `.fsproj`, `packages.config`, `Directory.Packages.props`
- Recursive directory scanning
- Central Package Management (CPM) support
- Ported and modernised from [NugetManager](https://github.com/phmatray/NugetManager)

### 🛡️ Security / OSV (`NuGetPulse.Security`)
- Scan any NuGet package/version against the **OSV** (Open Source Vulnerabilities) database
- Batch scanning for entire project portfolios
- Severity mapping (Critical / High / Medium / Low)
- Real implementation of the [NugetOSV](https://github.com/phmatray/NugetOSV) concept

### 🗄️ Self-hosted Server (`NuGetPulse.Server`)
- File-system backed NuGet package store
- Push / List / Download .nupkg packages
- Clean architecture (ports & adapters)
- Inspired by [NugetServer](https://github.com/phmatray/NugetServer) gRPC design

---

## Project Structure

```
NuGetPulse/
├── src/
│   ├── NuGetPulse.Web/        # Blazor Server dashboard (net10.0)
│   ├── NuGetPulse.Core/       # Shared models, interfaces, health scoring
│   ├── NuGetPulse.Scanner/    # Project-file package scanner
│   ├── NuGetPulse.Security/   # OSV vulnerability scanner
│   └── NuGetPulse.Server/     # Self-hosted NuGet package store
├── k8s/                       # Kubernetes manifests
├── Dockerfile
├── global.json                # SDK 10.0.103 pinned
└── Directory.Packages.props   # Central Package Management
```

---

## Quick Start

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

### Use the scanner in your project
```csharp
// Register
services.AddNuGetPulseScanner();

// Inject and use
var packages = await scanner.ScanDirectoryAsync("/path/to/your/repo");
```

### Scan for vulnerabilities (OSV)
```csharp
// Register
services.AddNuGetPulseSecurity();

// Inject and use
var report = await vulnScanner.ScanAsync("Newtonsoft.Json", "12.0.1");
Console.WriteLine($"Found {report.Count} vulnerabilities");
```

### Self-hosted NuGet server
```csharp
// Register
services.AddNuGetPulseServer(opts => opts.RootPath = "/srv/nuget-packages");

// Push a package
await store.PushAsync(nupkgStream);

// List packages
var list = await store.ListAsync();
```

---

## How the Health Score Works

| Factor | Weight | Description |
|--------|--------|-------------|
| Downloads | 30% | Log-normalised total download count |
| Freshness | 30% | Days since last publish (< 30 days = 100) |
| Vulnerabilities | 25% | 0 vulns = 100, each vuln −25 |
| Deprecation | 15% | Not deprecated = 100, deprecated = 0 |

**Score ≥ 80** → 🟢 Healthy  
**Score 60–79** → 🟡 Warning  
**Score < 60** → 🔴 Critical

---

## Deprecated Repositories

The following repositories have been absorbed into NuGetPulse and are now archived:

- ~~[phmatray/NugetOSV](https://github.com/phmatray/NugetOSV)~~ → `NuGetPulse.Security`
- ~~[phmatray/NugetManager](https://github.com/phmatray/NugetManager)~~ → `NuGetPulse.Scanner` + `NuGetPulse.Web`
- ~~[phmatray/NugetServer](https://github.com/phmatray/NugetServer)~~ → `NuGetPulse.Server`

---

## Roadmap

- [ ] Blazor UI for scanner results (visualise project dependencies)
- [ ] OSV vulnerability badges in the dashboard
- [ ] Self-hosted server UI tab in dashboard
- [ ] Email/Slack alerts on score drops
- [ ] Historical trend charts
- [ ] GitHub Actions integration (block PRs on unhealthy deps)

---

## Contributing

Pull requests are welcome. For major changes, please open an issue first.

---

## License

[MIT](LICENSE) © 2026 [Philippe Matray](https://github.com/phmatray) / [Atypical Consulting](https://atypical.garry-ai.cloud)
