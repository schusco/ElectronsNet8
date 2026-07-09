# 1. Build Stage
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /src

# Copy csproj and restore dependencies (Optimizes caching)
COPY ["Electrons.Net8/Electrons.Net8.csproj", "./"]
RUN dotnet restore -a ${TARGETARCH}

# Copy everything else and build
COPY . .
RUN dotnet publish "Electrons.Net8/Electrons.Net8.csproj" -c Release -o /app -a %{TARGETARCH} --self-contained false 

# 2. Runtime Stage (The final tiny image)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# .NET 8 defaults to port 8080 (non-root user for security)
EXPOSE 8080
ENTRYPOINT ["dotnet", "Electrons.Net8.dll"]
