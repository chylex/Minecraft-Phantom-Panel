FROM ubuntu:focal AS java-legacy

ARG DEBIAN_FRONTEND=noninteractive

RUN apt-get update
RUN apt-get install -y      \
    openjdk-8-jre-headless  \
    openjdk-16-jre-headless \
    openjdk-17-jre-headless

FROM mcr.microsoft.com/dotnet/aspnet:7.0-jammy

COPY --from=java-legacy /usr/lib/jvm/java-8-openjdk-amd64 /usr/lib/jvm/java-8-openjdk-amd64
COPY --from=java-legacy /usr/lib/jvm/java-16-openjdk-amd64 /usr/lib/jvm/java-16-openjdk-amd64
COPY --from=java-legacy /usr/lib/jvm/java-17-openjdk-amd64 /usr/lib/jvm/java-17-openjdk-amd64

ARG DEBIAN_FRONTEND=noninteractive

RUN apt-get update
RUN apt-get install -y \
    openjdk-18-jre-headless

WORKDIR /app
# TODO
ENTRYPOINT ["dotnet", "aspnetapp.dll"]
