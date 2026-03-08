# Contributing to NuGetPulse

Thank you for your interest in contributing to NuGetPulse! 🎉

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (version 10.0.103 or later)
- Git
- A code editor (Visual Studio 2022, Rider, or VS Code)

### Development Setup

1. **Fork and clone the repository**
   ```bash
   git clone https://github.com/YOUR-USERNAME/NuGetPulse.git
   cd NuGetPulse
   ```

2. **Configure git identity**
   ```bash
   git config user.name "Your Name"
   git config user.email "your.email@example.com"
   ```

3. **Restore dependencies**
   ```bash
   dotnet restore
   ```

4. **Build the solution**
   ```bash
   dotnet build --configuration Release
   ```

5. **Run tests**
   ```bash
   dotnet test --configuration Release
   ```

6. **Run the web app**
   ```bash
   dotnet run --project src/NuGetPulse.Web
   ```
   Navigate to [http://localhost:5000](http://localhost:5000)

## Development Workflow

### Branching Strategy
- `main` - production-ready code, protected branch
- `feature/feature-name` - new features
- `fix/bug-description` - bug fixes
- `chore/task-description` - maintenance tasks, dependency updates
- `docs/improvement-description` - documentation improvements

### Making Changes

1. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make your changes**
   - Write tests first (TDD approach)
   - Implement the feature
   - Ensure all tests pass
   - Update documentation if needed

3. **Commit your changes**
   We follow [Conventional Commits](https://www.conventionalcommits.org/):
   ```bash
   git commit -m "feat(scanner): add support for .fsproj files"
   git commit -m "fix(security): handle null OSV responses"
   git commit -m "docs(readme): add deployment guide"
   git commit -m "chore(deps): update FluentAssertions to 8.8.0"
   ```

   **Commit types:**
   - `feat:` - new feature
   - `fix:` - bug fix
   - `docs:` - documentation only
   - `chore:` - maintenance, dependencies
   - `test:` - adding or updating tests
   - `refactor:` - code restructuring
   - `perf:` - performance improvements
   - `ci:` - CI/CD changes

4. **Push and create a Pull Request**
   ```bash
   git push origin feature/your-feature-name
   ```
   Then open a PR on GitHub with:
   - Clear description of what changed
   - Reference any related issues
   - Screenshots if UI changes

## Code Standards

### Architecture
NuGetPulse follows **Clean Architecture** principles:
- **Core** - Domain models, interfaces (no dependencies)
- **Scanner/Security/Graph** - Business logic, domain services
- **Persistence** - EF Core, SQLite (infrastructure)
- **Web** - Blazor UI (presentation)

### .NET Conventions
- Target `net10.0`
- Use **Central Package Management** (`Directory.Packages.props`)
- Always use `dotnet restore` → `dotnet build` → `dotnet test` (never `--no-build` in Dockerfiles)
- Use `Result<T>` for expected errors, exceptions for unexpected failures
- Prefer immutability where possible
- Use nullable reference types (`<Nullable>enable</Nullable>`)

### Testing
- Write unit tests for business logic
- Use xUnit, FluentAssertions, NSubstitute
- Aim for >80% code coverage on Core/Scanner/Security/Graph
- Test file: `ClassName.Tests.cs` in `NuGetPulse.Tests`
- Test method naming: `MethodName_Scenario_ExpectedBehavior`

Example:
```csharp
public class HealthScoreCalculatorTests
{
    [Fact]
    public void Calculate_WithHighDownloads_ReturnsHighScore()
    {
        // Arrange
        var calculator = new HealthScoreCalculator();
        var package = new PackageMetadata { Downloads = 10_000_000 };

        // Act
        var score = calculator.Calculate(package);

        // Assert
        score.Should().BeGreaterThan(80);
    }
}
```

### Code Style
- Use meaningful variable names
- Keep methods focused (single responsibility)
- Add XML documentation for public APIs
- Use `var` for local variables when type is obvious
- Place `using` directives inside namespace

## Pull Request Process

1. **Ensure all tests pass**
   ```bash
   dotnet test --configuration Release
   ```

2. **Update documentation**
   - If you added a feature, document it in README.md
   - If you changed APIs, update XML comments
   - If you changed behavior, update relevant docs

3. **Create the PR**
   - Title: Follow conventional commits format
   - Description: Explain what and why
   - Reference issues: `Closes #123` or `Fixes #456`

4. **Code Review**
   - Address review feedback promptly
   - Keep discussions focused and respectful
   - CI must pass before merge

5. **Merge**
   - Maintainers will merge using **Squash and Merge**
   - Your commits will be squashed into one with a clean message

## Areas to Contribute

### 🐛 Bug Fixes
Check [open issues labeled "bug"](https://github.com/phmatray/NuGetPulse/labels/bug)

### ✨ New Features
- **Package analytics** - trends, popularity metrics
- **Dependency graph visualization** - interactive graph of dependencies
- **Security alerts** - email notifications for new vulnerabilities
- **Multi-solution scanning** - scan entire solution directories
- **Export formats** - PDF, Excel, HTML reports
- **API endpoints** - REST API for programmatic access

### 📚 Documentation
- Improve README examples
- Add architecture diagrams
- Write deployment guides (Docker, Kubernetes, Azure)
- Create video tutorials

### 🧪 Testing
- Increase code coverage
- Add integration tests
- Add performance tests

### 🏗️ Infrastructure
- Improve CI/CD pipeline
- Add Docker Compose for local dev
- Kubernetes Helm charts

## Questions?

- **Open an issue** for bugs or feature requests
- **Start a discussion** for questions or ideas
- **Check existing issues** before creating duplicates

## Code of Conduct

Be respectful, inclusive, and professional. We're all here to build something great together.

---

**Thank you for contributing to NuGetPulse!** 🚀
