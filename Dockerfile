# +---------------+
# | Prepare build |
# +---------------+
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/nightly/sdk:10.0 AS phantom-builder
ARG TARGETARCH

ADD . /app
WORKDIR /app

RUN dotnet publish PhantomPanel.sln \
    /p:DebugType=None               \
    /p:DebugSymbols=false           \
    --arch "$TARGETARCH"            \
    --configuration Release

RUN find .artifacts/publish/*/* -maxdepth 0 -execdir mv '{}' 'release' \;


# +---------------------+
# | Phantom Agent image |
# +---------------------+
FROM mcr.microsoft.com/dotnet/nightly/runtime:10.0 AS phantom-agent

RUN mkdir /data && chmod 777 /data
WORKDIR /data

COPY --from=eclipse-temurin:8-jre  /opt/java/openjdk /opt/java/8
COPY --from=eclipse-temurin:16-jdk /opt/java/openjdk /opt/java/16
COPY --from=eclipse-temurin:17-jre /opt/java/openjdk /opt/java/17
COPY --from=eclipse-temurin:21-jre /opt/java/openjdk /opt/java/21

ARG DEBIAN_FRONTEND=noninteractive

RUN --mount=target=/var/lib/apt/lists,type=cache,sharing=locked \
    --mount=target=/var/cache/apt,type=cache,sharing=locked     \
    rm -f /etc/apt/apt.conf.d/docker-clean                   && \
    apt-get update                                           && \
    apt-get install -y                                          \
    zstd

COPY --from=phantom-builder --chmod=755 /app/.artifacts/publish/Phantom.Agent/release /app

ENTRYPOINT ["dotnet", "/app/Phantom.Agent.dll"]


# +--------------------------+
# | Phantom Controller image |
# +--------------------------+
FROM mcr.microsoft.com/dotnet/nightly/runtime:10.0 AS phantom-controller

RUN mkdir /data && chmod 777 /data
WORKDIR /data

COPY --from=phantom-builder --chmod=755 /app/.artifacts/publish/Phantom.Controller/release /app

ENTRYPOINT ["dotnet", "/app/Phantom.Controller.dll"]


# +-------------------+
# | Phantom Web image |
# +-------------------+
FROM mcr.microsoft.com/dotnet/nightly/aspnet:9.0 AS phantom-web

RUN mkdir /data && chmod 777 /data
WORKDIR /data

COPY --from=phantom-builder --chmod=755 /app/.artifacts/publish/Phantom.Web/release /app

ENTRYPOINT ["dotnet", "/app/Phantom.Web.dll"]
