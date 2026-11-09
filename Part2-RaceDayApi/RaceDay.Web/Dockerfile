# ---------------------------------------------------------------
# RaceDay MVC web app - multi-stage Docker build
#   Stage 1 (build):   uses the full .NET SDK to restore and publish
#   Stage 2 (runtime): copies only the published output into the small
#                      ASP.NET runtime image and runs as a non-root user
# Build:  docker build -t raceday-web ./RaceDay.Web
# ---------------------------------------------------------------

# ---------- Stage 1: build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file first and restore, so this layer is cached
# and packages are only re-downloaded when the .csproj changes
COPY RaceDay.Web.csproj ./
RUN dotnet restore RaceDay.Web.csproj

# Copy the rest of the source and publish a Release build
COPY . ./
RUN dotnet publish RaceDay.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Stage 2: runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Listen on port 8080 inside the container; Production = friendly error pages
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ApiBaseUrl=http://host.docker.internal:5180/

EXPOSE 8080

COPY --from=build /app/publish ./

# Don't run as root (the .NET 8 images include a built-in "app" user)
USER $APP_UID

ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
