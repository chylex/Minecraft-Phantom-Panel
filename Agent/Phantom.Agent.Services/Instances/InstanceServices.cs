using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Rpc;
using Phantom.Agent.Services.Backups;

namespace Phantom.Agent.Services.Instances;

sealed record InstanceServices(ControllerConnection ControllerConnection, BackupManager BackupManager, LaunchServices LaunchServices);
