# +---------------------------+
# | Prepare build environment |
# +---------------------------+
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/nightly/sdk:8.0-preview AS phantom-base-builder
ARG TARGETARCH

ADD . /app
WORKDIR /app

RUN mkdir /data && chmod 777 /data
RUN dotnet restore --arch "$TARGETARCH"


# +---------------------+
# | Build Phantom Agent |
# +---------------------+
FROM phantom-base-builder AS phantom-agent-builder

RUN dotnet publish Agent/Phantom.Agent/Phantom.Agent.csproj \
    /p:DebugType=None                                       \
    /p:DebugSymbols=false                                   \
    --no-restore                                            \
    --arch "$TARGETARCH"                                    \
    --configuration Release                                 \
    --output /app/out


# +--------------------------+
# | Build Phantom Controller |
# +--------------------------+
FROM phantom-base-builder AS phantom-controller-builder

RUN dotnet publish Controller/Phantom.Controller/Phantom.Controller.csproj \
    /p:DebugType=None                                                      \
    /p:DebugSymbols=false                                                  \
    --no-restore                                                           \
    --arch "$TARGETARCH"                                                   \
    --configuration Release                                                \
    --output /app/out


# +-------------------+
# | Build Phantom Web |
# +-------------------+
FROM phantom-base-builder AS phantom-controller-builder

RUN dotnet publish Web/Phantom.Web/Phantom.Web.csproj \
    /p:DebugType=None                                 \
    /p:DebugSymbols=false                             \
    --no-restore                                      \
    --arch "$TARGETARCH"                              \
    --configuration Release                           \
    --output /app/out


# +------------------------------+
# | Finalize Phantom Agent image |
# +------------------------------+
FROM mcr.microsoft.com/dotnet/nightly/runtime:8.0-preview AS phantom-agent

WORKDIR /data

COPY --from=eclipse-temurin:8-jre  /opt/java/openjdk /opt/java/8
COPY --from=eclipse-temurin:16-jdk /opt/java/openjdk /opt/java/16
COPY --from=eclipse-temurin:17-jre /opt/java/openjdk /opt/java/17
COPY --from=eclipse-temurin:20-jre /opt/java/openjdk /opt/java/20

ARG DEBIAN_FRONTEND=noninteractive

RUN --mount=target=/var/lib/apt/lists,type=cache,sharing=locked \
    --mount=target=/var/cache/apt,type=cache,sharing=locked     \
    rm -f /etc/apt/apt.conf.d/docker-clean                   && \
    apt-get update                                           && \
    apt-get install -y                                          \
    zstd

COPY --from=phantom-agent-builder --chmod=755 /app/out /app

ENTRYPOINT ["dotnet", "/app/Phantom.Agent.dll"]


# +-----------------------------------+
# | Finalize Phantom Controller image |
# +-----------------------------------+
FROM mcr.microsoft.com/dotnet/nightly/runtime:8.0-preview AS phantom-controller

WORKDIR /data

COPY --from=phantom-controller-builder --chmod=755 /app/out /app

ENTRYPOINT ["dotnet", "/app/Phantom.Controller.dll"]


# +----------------------------+
# | Finalize Phantom Web image |
# +----------------------------+
FROM mcr.microsoft.com/dotnet/nightly/aspnet:8.0-preview AS phantom-web

WORKDIR /data

COPY --from=phantom-web-builder --chmod=755 /app/out /app

ENTRYPOINT ["dotnet", "/app/Phantom.Web.dll"]
