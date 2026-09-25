# Multi-stage Dockerfile for TaskForge .NET 10 Web API

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy central configuration files
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]

# Copy project files for restore caching
COPY ["src/TaskForge.Shared/TaskForge.Shared.csproj", "src/TaskForge.Shared/"]
COPY ["src/TaskForge.Domain/TaskForge.Domain.csproj", "src/TaskForge.Domain/"]
COPY ["src/TaskForge.Application/TaskForge.Application.csproj", "src/TaskForge.Application/"]
COPY ["src/TaskForge.Infrastructure/TaskForge.Infrastructure.csproj", "src/TaskForge.Infrastructure/"]
COPY ["src/TaskForge.Api/TaskForge.Api.csproj", "src/TaskForge.Api/"]

RUN dotnet restore "src/TaskForge.Api/TaskForge.Api.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/src/TaskForge.Api"
RUN dotnet build "TaskForge.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TaskForge.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TaskForge.Api.dll"]
