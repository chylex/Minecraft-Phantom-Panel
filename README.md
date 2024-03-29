﻿# Phantom Panel

Phantom Panel is a **work-in-progress** web interface for managing Minecraft servers.

# Architecture

Phantom Panel has 3 types of services:

* The **Web** provides a web interface for the **Controller**.
* The **Controller** manages all state and persists it in a database, and communicates with **Agents**.
* One or more **Agents** receive commands from the **Controller**, manage the Minecraft server processes, and report on their status.

This architecture has several goals and benefits:

1. The services can run on separate computers, in separate containers, or a mixture of both.
2. The services can be updated independently.
   - The Controller or Web can receive new features, bug fixes, and security updates without the need to shutdown every Minecraft server.
   - Agent updates can be staggered or delayed. For example, if you have Agents in different geographical locations, you could schedule around timezones and update them at times when people are unlikely to be online.
3. Agents are lightweight processes which should have minimal impact on the performance of Minecraft servers.

When an official Controller update is released, it will work with older versions of Agents. There is no guarantee it will also work in reverse (updated Agents and an older Controller), but if there is an Agent update that is compatible with an older Controller, it will be mentioned in the release notes.

Note that compatibility is only guaranteed when using official releases. If you build the project from a version of the source between two official releases, you have to understand which changes break compatibility.

# Usage

This project is **work-in-progress**, and currently has no official releases. Feel free to try it and experiment, but there will be missing features, bugs, and breaking changes.

For a quick start, I recommend using [Docker](https://www.docker.com/) or another containerization platform. The `Dockerfile` in the root of the repository can build three target images: `phantom-web`, `phantom-controller`, and `phantom-agent`.

All images put the built application into the `/app` folder. The Agent image also installs Java 8, 16, 17, and 18.

Files are stored relative to the working directory. In the provided images, the working directory is set to `/data`.

## Controller

The Controller comprises 3 key areas:

* **Agent RPC server** that Agents connect to.
* **Web RPC server** that Web connects to.
* **PostgreSQL database connection** to persist data.

The configuration for these is set via environment variables.

### Agent & Web Keys

When the Controller starts for the first time, it will generate two pairs of key files. Each pair consists of a **common** and a **secret** key file. One pair is generated for **Agents**, and one for the **Web**.

The **common keys** contain encryption certificates and authorization tokens, which are needed to connect to the Controller. Both the Controller and the connecting Agent or Web must have access to the appropriate **common key**.

The **secret keys** contain information the Controller needs to establish an encrypted communication channel. These files should only be accessible by the Controller itself.

The **common keys** have two forms:

* A binary file `/data/secrets/agent.key` or `/data/secrets/web.key` that can be distributed to the other services.
* A plaintext-encoded version printed into the logs on every startup, that can be passed to the other services in an environment variable.

The **secret keys** are stored as binary files `/data/secrets/agent.secret` and `/data/secrets/web.secret`.

### Storage

Use volumes to persist the whole `/data` folder.

### Environment variables

* **Agent RPC Server**
  - `AGENT_RPC_SERVER_HOST` is the host. Default: `0.0.0.0`
  - `AGENT_RPC_SERVER_PORT` is the port. Default: `9401`
* **Web RPC Server**
  - `WEB_RPC_SERVER_HOST` is the host. Default: `0.0.0.0`
  - `WEB_RPC_SERVER_PORT` is the port. Default: `9402`
* **PostgreSQL Database Connection**
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

### Environment variables

* **Controller Communication**
  - `CONTROLLER_HOST` is the hostname of the Controller.
  - `CONTROLLER_PORT` is the Agent RPC port of the Controller. Default: `9401`
  - `AGENT_NAME` is the display name of the Agent. Emoji are allowed.
  - `AGENT_KEY` is the plaintext-encoded version of [Agent Key](#agent--web-keys).
  - `AGENT_KEY_FILE` is a path to the [Agent Key](#agent--web-keys) binary file.
* **Agent Configuration**
  - `MAX_INSTANCES` is the number of instances that can be created.
  - `MAX_MEMORY` is the maximum amount of RAM that can be distributed among all instances. Use a positive integer with an optional suffix 'M' for MB, or 'G' for GB. Examples: `4096M`, `16G`
  - `MAX_CONCURRENT_BACKUP_COMPRESSION_TASKS` is how many backup compression tasks can run at the same time. Limiting concurrent compression tasks limits memory usage of compression, but it increases time between backups because the next backup is only scheduled once the current one completes. Default: `1`
* **Minecraft Configuration**
  - `JAVA_SEARCH_PATH` is a path to a folder which will be searched for Java runtime installations. Linux default: `/usr/lib/jvm`
  - `ALLOWED_SERVER_PORTS` is a comma-separated list of ports and port ranges that can be used as Minecraft Server ports. Example: `25565,25900,26000-27000`
  - `ALLOWED_RCON_PORTS` is a comma-separated list of ports and port ranges that can be used as Minecraft RCON ports. Example: `25575,25901,36000-37000`

## Web

### Storage

Use volumes to persist the whole `/data` folder.

### Environment variables

* **Controller Communication**
  - `CONTROLLER_HOST` is the hostname of the Controller.
  - `CONTROLLER_PORT` is the Web RPC port of the Controller. Default: `9402`
  - `WEB_KEY` is the plaintext-encoded version of [Web Key](#agent--web-keys).
  - `WEB_KEY_FILE` is a path to the [Web Key](#agent--web-keys) binary file.
* **Web Server**
  - `WEB_SERVER_HOST` is the host. Default: `0.0.0.0`
  - `WEB_SERVER_PORT` is the port. Default: `9400`
  - `WEB_BASE_PATH` is the base path of every URL. Must begin with a slash. Default: `/`

## Logging

All services support a `LOG_LEVEL` environment variable to set the minimum log level. Possible values:

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
   - Port: `9403`
   - User: `postgres`
   - Password: `development`
   - Database: `postgres`
2. Install one or more Java versions into the `~/.jdks` folder (`%USERPROFILE%\.jdks` on Windows).
3. Open the project in [Rider](https://www.jetbrains.com/rider/) and use one of the provided run configurations:
   - `Controller` starts the Controller.
   - `Web` starts the Web server.
   - `Agent 1`, `Agent 2`, `Agent 3` start one of the Agents.
   - `Controller + Web + Agent` starts the Controller and Agent 1.
   - `Controller + Web + Agent x3` starts the Controller and Agent 1, 2, and 3.

## Bootstrap

The project uses [Bootstrap 5](https://getbootstrap.com/docs/5.2) with a custom theme and several other customizations. The sources are in the `Phantom.Server.Web.Bootstrap` project.

If you make any changes to the sources, you will need to use the `Compile Bootstrap` run configuration, then restart the Server to load the new version. This is not done automatically, and it requires [Node](https://nodejs.org/en/) and [npm](https://www.npmjs.com/).
