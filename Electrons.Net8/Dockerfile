FROM mcr.microsoft.com/dotnet/aspnet:8.0-nanoserver-ltsc2022 AS base
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0-nanoserver-ltsc2022 AS build
ARG configuration=Release
WORKDIR /src
COPY ["Electrons.Net8/Electrons.Net8.csproj", "Electrons.Net8/"]
RUN dotnet restore "Electrons.Net8\Electrons.Net8.csproj"
COPY . .
WORKDIR "/src/Electrons.Net8"
RUN dotnet build "Electrons.Net8.csproj" -c $configuration -o /app/build

FROM build AS publish
ARG configuration=Release
RUN dotnet publish "Electrons.Net8.csproj" -c $configuration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Electrons.Net8.dll"]
