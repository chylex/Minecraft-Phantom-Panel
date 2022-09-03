FROM mcr.microsoft.com/dotnet/aspnet:6.0

RUN apt-get update
RUN apt-get install -y      \
    openjdk-8-jdk-headless  \
    openjdk-16-jdk-headless \
    openjdk-17-jdk-headless

WORKDIR /app
# TODO
ENTRYPOINT ["dotnet", "aspnetapp.dll"]
