# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["Custom-Mcp.csproj", "./"]
RUN dotnet restore "Custom-Mcp.csproj"

# Copy source code and build
COPY . .
RUN dotnet build "Custom-Mcp.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Custom-Mcp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY credentials/serviceAccount.json credentials/serviceAccount.json
# MCP servers communicate via stdio, not HTTP
ENTRYPOINT ["dotnet", "Custom-Mcp.dll"]