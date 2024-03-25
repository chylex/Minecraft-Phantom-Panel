using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Rpc;
using Phantom.Agent.Services.Backups;
using Phantom.Utils.Tasks;

namespace Phantom.Agent.Services.Instances;

sealed record InstanceServices(ControllerConnection ControllerConnection, TaskManager TaskManager, BackupManager BackupManager, LaunchServices LaunchServices);
