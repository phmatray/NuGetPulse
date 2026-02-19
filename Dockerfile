FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution manifest + all .csproj files first for optimal layer caching.
# Each project that NuGetPulse.Web depends on must be listed here so that
# `dotnet restore` can resolve the full dependency graph before the source copy.
COPY global.json Directory.Packages.props NuGetPulse.slnx ./
COPY src/NuGetPulse.Core/NuGetPulse.Core.csproj src/NuGetPulse.Core/
COPY src/NuGetPulse.Scanner/NuGetPulse.Scanner.csproj src/NuGetPulse.Scanner/
COPY src/NuGetPulse.Security/NuGetPulse.Security.csproj src/NuGetPulse.Security/
COPY src/NuGetPulse.Server/NuGetPulse.Server.csproj src/NuGetPulse.Server/
COPY src/NuGetPulse.Web/NuGetPulse.Web.csproj src/NuGetPulse.Web/
RUN dotnet restore src/NuGetPulse.Web/NuGetPulse.Web.csproj

# Copy everything and publish
COPY src/ src/
RUN dotnet publish src/NuGetPulse.Web/NuGetPulse.Web.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "NuGetPulse.Web.dll"]
