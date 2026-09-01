# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY SecureConfigApi.sln .
COPY src/SecureConfigApi/SecureConfigApi.csproj src/SecureConfigApi/
COPY tests/SecureConfigApi.Tests/SecureConfigApi.Tests.csproj tests/SecureConfigApi.Tests/
RUN dotnet restore SecureConfigApi.sln

COPY . .
RUN dotnet publish src/SecureConfigApi/SecureConfigApi.csproj -c Release -o /app/publish --no-restore

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SecureConfigApi.dll"]
