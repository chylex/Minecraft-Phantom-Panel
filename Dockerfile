# +---------------------------+
# | Prepare build environment |
# +---------------------------+
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS phantom-base-builder

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
# | Download older Java versions |
# +------------------------------+
FROM ubuntu:focal AS java-legacy

ARG DEBIAN_FRONTEND=noninteractive

RUN --mount=target=/var/lib/apt/lists,type=cache,sharing=locked \
    --mount=target=/var/cache/apt,type=cache,sharing=locked     \
    rm -f /etc/apt/apt.conf.d/docker-clean                   && \
    apt-get update                                           && \
    apt-get install -y                                          \
    openjdk-8-jre-headless                                      \
    openjdk-16-jre-headless                                     \
    openjdk-17-jre-headless


# +------------------------------+
# | Finalize Phantom Agent image |
# +------------------------------+
FROM mcr.microsoft.com/dotnet/runtime:7.0-jammy AS phantom-agent

COPY --from=java-legacy /usr/lib/jvm/java-8-openjdk-amd64 /usr/lib/jvm/java-8-openjdk-amd64
COPY --from=java-legacy /usr/lib/jvm/java-16-openjdk-amd64 /usr/lib/jvm/java-16-openjdk-amd64
COPY --from=java-legacy /usr/lib/jvm/java-17-openjdk-amd64 /usr/lib/jvm/java-17-openjdk-amd64

ARG DEBIAN_FRONTEND=noninteractive

RUN --mount=target=/var/lib/apt/lists,type=cache,sharing=locked \
    --mount=target=/var/cache/apt,type=cache,sharing=locked     \
    rm -f /etc/apt/apt.conf.d/docker-clean                   && \
    apt-get update                                           && \
    apt-get install -y                                          \
    openjdk-18-jre-headless

RUN mkdir /data && chmod 777 /data
WORKDIR /data

COPY --from=phantom-agent-builder --chmod=755 /app/out /app

ENTRYPOINT ["dotnet", "/app/Phantom.Agent.dll"]


# +-------------------------------+
# | Finalize Phantom Server image |
# +-------------------------------+
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS phantom-server

RUN mkdir /data && chmod 777 /data
WORKDIR /data

COPY --from=phantom-server-builder --chmod=755 /app/out /app

ENTRYPOINT ["dotnet", "/app/Phantom.Server.dll"]
