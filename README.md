# Phantom Panel

Phantom Panel is a **work-in-progress** web interface for managing Minecraft servers.

# Architecture

Phantom Panel is built on what I'm calling a **Server-Agent architecture**:

* The **Server** is provides a web interface, persists data in a database, and sends commands to the **Agents**.
* One or more **Agents** receive commands from the **Server**, manage the Minecraft server processes, and report on their status.

This architecture has several goals and benefits:

1. The Server and Agents can run on separate computers, in separate containers, or a mixture of both.
2. The Server and Agents can be updated independently.
   - The Server can receive new features, bug fixes, and security updates without the need to shutdown every Minecraft server.
   - Agent updates can be staggered or delayed. For example, if you have Agents in different geographical locations, you could schedule around timezones and update them at times when people are unlikely to be online.
3. Agents are lightweight processes which should have minimal impact on the performance of Minecraft servers.

When an official Server update is released, it will work with older versions of Agents. There is no guarantee it will also work in reverse (updated Agents and an older Server), but if there is an Agent update that is compatible with older Servers, it will be mentioned in the release notes.

Note that compatibility is only guaranteed when using official releases. If you build the project from a version of the source between two official releases, you have to understand which changes break compatibility.

# Usage

This project is **work-in-progress**, and currently has no official releases. Feel free to try it and experiment, but there will be missing features, bugs, and breaking changes.

For a quick start, I recommend using [Docker](https://www.docker.com/) or another containerization platform. The `Dockerfile` in the root of the repository can build two target images: `phantom-server` and `phantom-agent`.

Both images put the built application into the `/app` folder. The Agent image also installs Java 8, 16, 17, and 18.

Files are stored relative to the working directory. In the provided images, the working directory is set to `/data`.

## Server

The Server comprises 3 key areas:

* **Web server** that provides the web interface.
* **RPC server** that Agents connect to.
* **Database connection** that requires a PostgreSQL database server in order to persist data.

The configuration for these is set via environment variables.

### Agent Key

When the Server starts for the first time, it will generate and an **Agent Key**. The Agent Key contains an encryption certificate and an authorization token, which are needed for the Agents to connect to the Server.

The Agent Key has two forms:

* A binary file stored in `/data/secrets/agent.key` that the Agents can read.
* A plaintext-encoded version the Server outputs into the logs on every startup, that can be passed to the Agents in an environemnt variable.

### Storage

Use volumes to persist the whole `/data` folder.

### Environment variables

* **Web Server**
  - `WEB_SERVER_HOST` is the host. Default: `0.0.0.0`
  - `WEB_SERVER_PORT` is the port. Default: `9400`
  - `WEB_BASE_PATH` is the base path of every URL. Must begin with a slash. Default: `/`
* **RPC Server**
  - `RPC_SERVER_HOST` is the host. Default: `0.0.0.0`
  - `RPC_SERVER_PORT` is the port. Default: `9401`
* **PostgreSQL Database Server**
  - `PG_HOST` is the hostname.
  - `PG_PORT` is the port.
  - `PG_USER` is the username.
  - `PG_PASS` is the password.
  - `PG_DATABASE` is the database name.

## Agent

### Storage

The `/data` folder will contain two folders:

* `/data/data` for persistent files
* `/data/temp` for volatile files (such as downloaded Minecraft `.jar` files)

Use volumes to persist either the whole `/data` folder, or just `/data/data` if you don't want to persist the volatile files.

### Environment variables:

* **Server Communication**
  - `SERVER_HOST` is the hostname of the Server.
  - `SERVER_PORT` is the RPC port of the Server. Default: `9401`
  - `AGENT_NAME` is the display name of the Agent. Emoji are allowed.
  - `AGENT_KEY` is the plaintext-encoded version of [Agent Key](#agent-key).
  - `AGENT_KEY_FILE` is a path to the [Agent Key](#agent-key) binary file.
* **Agent Configuration**
  - `MAX_INSTANCES` is the number of instances that can be created.
  - `MAX_MEMORY` is the maximum amount of RAM that can be distributed among all instances. Use a positive integer with an optional suffix 'M' for MB, or 'G' for GB. Examples: `4096M`, `16G`
  - `MAX_CONCURRENT_BACKUP_COMPRESSION_TASKS` is how many backup compression tasks can run at the same time. Limiting concurrent compression tasks limits memory usage of compression, but it increases time between backups because the next backup is only scheduled once the current one completes. Default: `1`
* **Minecraft Configuration**
  - `JAVA_SEARCH_PATH` is a path to a folder which will be searched for Java runtime installations. Linux default: `/usr/lib/jvm`
  - `ALLOWED_SERVER_PORTS` is a comma-separated list of ports and port ranges that can be used as Minecraft Server ports. Example: `25565,25900,26000-27000`
  - `ALLOWED_RCON_PORTS` is a comma-separated list of ports and port ranges that can be used as Minecraft RCON ports. Example: `25575,25901,36000-37000`

## Logging

Both the Server and Agent support a `LOG_LEVEL` environment variable to set the minimum log level. Possible values:

* `VERBOSE`
* `DEBUG`
* `INFORMATION`
* `WARNING`
* `ERROR`

If the environment variable is omitted, the log level is set to `VERBOSE` for Debug builds and `INFORMATION` for Release builds.

# Development

The repository includes a [Rider](https://www.jetbrains.com/rider/) projects with several run configurations. The `.workdir` folder in the root of the repository is used for storage. Here's how to get started:

1. You will need a local PostgreSQL instance. If you have [Docker](https://www.docker.com/), you can enter the `Docker` folder in this repository, and run `docker compose up`. Otherwise, you will need to set it up manually with the following configuration:
   - Host: `localhost`
   - Port: `9402`
   - User: `postgres`
   - Password: `development`
   - Database: `postgres`
2. Install one or more Java versions into the `~/.jdks` folder (`%USERPROFILE%\.jdks` on Windows).
3. Open the project in [Rider](https://www.jetbrains.com/rider/) and use one of the provided run configurations:
   - `Server` starts the Server.
   - `Agent 1`, `Agent 2`, `Agent 3` start one of the Agents.
   - `Server + Agent` starts the Server and Agent 1.
   - `Server + Agent x3` starts the Server and Agent 1, 2, and 3.

## Bootstrap

The project uses [Bootstrap 5](https://getbootstrap.com/docs/5.2) with a custom theme and several other customizations. The sources are in the `Phantom.Server.Web.Bootstrap` project.

If you make any changes to the sources, you will need to use the `Compile Bootstrap` run configuration, then restart the Server to load the new version. This is not done automatically, and it requires [Node](https://nodejs.org/en/) and [npm](https://www.npmjs.com/).
