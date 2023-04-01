# +---------------------------+
# | Prepare build environment |
# +---------------------------+
FROM mcr.microsoft.com/dotnet/nightly/sdk:8.0-preview AS phantom-base-builder

ADD . /app
WORKDIR /app

RUN dotnet restore


# +---------------------+
# | Build Phantom Agent |
# +---------------------+
FROM phantom-base-builder AS phantom-agent-builder

RUN dotnet publish Agent/Phantom.Agent/Phantom.Agent.csproj -c Release -o /app/out


# +----------------------+
# | Build Phantom Server |
# +----------------------+
FROM phantom-base-builder AS phantom-server-builder

RUN dotnet publish Server/Phantom.Server.Web/Phantom.Server.Web.csproj -c Release -o /app/out
RUN dotnet publish Server/Phantom.Server/Phantom.Server.csproj -c Release -o /app/out


# +------------------------------+
# | Finalize Phantom Agent image |
# +------------------------------+
FROM mcr.microsoft.com/dotnet/nightly/runtime:8.0-preview AS phantom-agent

RUN mkdir /data && chmod 777 /data
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


# +-------------------------------+
# | Finalize Phantom Server image |
# +-------------------------------+
FROM mcr.microsoft.com/dotnet/nightly/aspnet:8.0-preview AS phantom-server

RUN mkdir /data && chmod 777 /data
WORKDIR /data

COPY --from=phantom-server-builder --chmod=755 /app/out /app

ENTRYPOINT ["dotnet", "/app/Phantom.Server.dll"]
