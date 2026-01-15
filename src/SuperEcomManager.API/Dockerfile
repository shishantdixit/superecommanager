# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["SuperEcomManager.sln", "./"]
COPY ["src/SuperEcomManager.API/SuperEcomManager.API.csproj", "src/SuperEcomManager.API/"]
COPY ["src/SuperEcomManager.Application/SuperEcomManager.Application.csproj", "src/SuperEcomManager.Application/"]
COPY ["src/SuperEcomManager.Domain/SuperEcomManager.Domain.csproj", "src/SuperEcomManager.Domain/"]
COPY ["src/SuperEcomManager.Infrastructure/SuperEcomManager.Infrastructure.csproj", "src/SuperEcomManager.Infrastructure/"]
COPY ["src/SuperEcomManager.Integrations/SuperEcomManager.Integrations.csproj", "src/SuperEcomManager.Integrations/"]

# Restore dependencies
RUN dotnet restore "src/SuperEcomManager.API/SuperEcomManager.API.csproj"

# Copy remaining source code
COPY . .

# Build and publish
WORKDIR "/src/src/SuperEcomManager.API"
RUN dotnet build "SuperEcomManager.API.csproj" -c Release -o /app/build
RUN dotnet publish "SuperEcomManager.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser

# Copy published application
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start the application
ENTRYPOINT ["dotnet", "SuperEcomManager.API.dll"]
