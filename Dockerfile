# syntax=docker/dockerfile:1

# The build stage runs on the build machine's own architecture. The bot is published as portable IL (no runtime identifier),
# so the same output runs on every architecture the runtime image supports, and restore matches packages.lock.json exactly.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-noble@sha256:0d99ebf1ccad15037b5c98c9fe8979d6b8dbac23812feae2277a0b277b5c5ffa AS build
WORKDIR /src

COPY Cloudy-Canvas/Cloudy-Canvas.csproj Cloudy-Canvas/packages.lock.json Cloudy-Canvas/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore Cloudy-Canvas/Cloudy-Canvas.csproj --locked-mode

COPY Cloudy-Canvas/ Cloudy-Canvas/
# VERSION ends up in the Manebooru User-Agent. The release workflow passes the tag without its leading "v".
ARG VERSION=0.0.0-dev
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish Cloudy-Canvas/Cloudy-Canvas.csproj \
        --configuration Release \
        --no-restore \
        --output /app \
        -p:UseAppHost=false \
        -p:Version=$VERSION \
    && rm -f /app/appsettings*.json

# Chiseled image: no shell, no package manager, runs as the non-root "app" user (UID/GID 1654).
# The -extra variant includes ICU and tzdata, which the bot needs because it does not run in invariant globalization mode.
FROM mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled-extra@sha256:11f4c921d75b7a1f19d4eb5b7069f9e92a9d1b8412a04b3475a664b62a9b88cb

LABEL org.opencontainers.image.title="Cloudy Canvas" \
      org.opencontainers.image.description="Discord bot for the Manebooru imageboard" \
      org.opencontainers.image.source="https://github.com/romulus4444/Cloudy-Canvas" \
      org.opencontainers.image.licenses="MIT"

WORKDIR /app
# Owned by root so the bot cannot change its own program files.
COPY --from=build --chown=0:0 /app .

# Settings and audit logs are written to /data, which must be a writable volume.
# EnableDiagnostics=0 turns off the debugger/profiler IPC socket, which also means nothing needs to write to /tmp at startup.
ENV Storage__RootPath=/data \
    DOTNET_EnableDiagnostics=0

USER 1654:1654
ENTRYPOINT ["dotnet", "Cloudy-Canvas.dll"]
