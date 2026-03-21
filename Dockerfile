# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies (Optimizes caching)
COPY ["ElectronsNet8.csproj", "./"]
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app

# 2. Runtime Stage (The final tiny image)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# .NET 8 defaults to port 8080 (non-root user for security)
EXPOSE 8080
ENTRYPOINT ["dotnet", "ElectronsNet8.dll"]