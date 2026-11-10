# syntax=docker/dockerfile:1
# =====================================================================
# RaceDay.Web (Part 3 MVC front end) - multi-stage production image
#   build : docker build -t raceday-web .
#   run   : see README.md ("Run with Docker")
# =====================================================================

# ---------- 1. Build stage: full .NET SDK ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first, using only the project file, so this layer is cached until dependencies change.
COPY src/RaceDay.Web/RaceDay.Web.csproj src/RaceDay.Web/
RUN dotnet restore src/RaceDay.Web/RaceDay.Web.csproj

# Then copy the source and publish a Release build.
COPY src/RaceDay.Web/ src/RaceDay.Web/
RUN dotnet publish src/RaceDay.Web/RaceDay.Web.csproj \
        --configuration Release \
        --no-restore \
        --output /app/publish \
        /p:UseAppHost=false

# ---------- 2. Runtime stage: small ASP.NET Core runtime only ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Listen on 8080 inside the container. Api__BaseUrl points at the Part 2 API running on the host
# (override it with -e Api__BaseUrl=... if your API runs somewhere else).
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    Api__BaseUrl=http://host.docker.internal:5080/

EXPOSE 8080

COPY --from=build /app/publish .

# Run as the built-in non-root user supplied by the .NET 8 images.
USER $APP_UID

ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
