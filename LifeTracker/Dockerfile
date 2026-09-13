# --- Frontend ---
FROM node:22-bookworm-slim AS frontend
WORKDIR /web
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
ENV VITE_API_BASE=
ENV VITE_AZURE_API=
RUN npm run build

# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file from the subfolder context and restore
COPY ["LifeTracker/LifeTracker.csproj", "LifeTracker/"]
RUN dotnet restore "LifeTracker/LifeTracker.csproj"

# Copy the rest of the source code and build
COPY . .
WORKDIR /src/LifeTracker
RUN dotnet publish "LifeTracker.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install system dependencies required by PostgreSQL/Npgsql (like GSSAPI)
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
COPY --from=frontend /web/dist ./wwwroot

EXPOSE 8080
# EXPOSE 8081 

ENTRYPOINT ["dotnet", "LifeTracker.dll"]