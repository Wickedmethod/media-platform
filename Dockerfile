FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY Directory.Build.props .
COPY NuGet.config .
COPY src/MediaPlatform.Domain/MediaPlatform.Domain.csproj src/MediaPlatform.Domain/
COPY src/MediaPlatform.Application/MediaPlatform.Application.csproj src/MediaPlatform.Application/
COPY src/MediaPlatform.Infrastructure/MediaPlatform.Infrastructure.csproj src/MediaPlatform.Infrastructure/
COPY src/MediaPlatform.Api/MediaPlatform.Api.csproj src/MediaPlatform.Api/
RUN dotnet restore src/MediaPlatform.Api/MediaPlatform.Api.csproj

COPY src/ src/
RUN dotnet publish src/MediaPlatform.Api/MediaPlatform.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "MediaPlatform.Api.dll"]
