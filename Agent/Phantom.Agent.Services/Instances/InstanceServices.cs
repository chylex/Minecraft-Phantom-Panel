using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Services.Backups;
using Phantom.Agent.Services.Rpc;

namespace Phantom.Agent.Services.Instances;

sealed record InstanceServices(ControllerConnection ControllerConnection, BackupManager BackupManager, LaunchServices LaunchServices);
