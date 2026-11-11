# ── Stage 1: Build & Publish ─────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for layer caching
COPY AdmissionSystem/AdmissionSystem.sln AdmissionSystem/
COPY AdmissionSystem/AdmissionSystem.Api/AdmissionSystem.Api.csproj AdmissionSystem/AdmissionSystem.Api/

# Restore dependencies
RUN dotnet restore AdmissionSystem/AdmissionSystem.Api/AdmissionSystem.Api.csproj

# Copy all source code
COPY AdmissionSystem/ AdmissionSystem/

# Build and publish release binaries
WORKDIR /src/AdmissionSystem/AdmissionSystem.Api
RUN dotnet publish AdmissionSystem.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ── Stage 2: Runtime Image ───────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Configure default runtime environment
ENV ASPNETCORE_URLS=http://+:8080
ENV HOLD_DURATION_SECONDS=600

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AdmissionSystem.Api.dll"]
