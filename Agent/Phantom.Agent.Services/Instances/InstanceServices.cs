using Phantom.Agent.Minecraft.Launcher;
using Phantom.Agent.Services.Backups;
using Phantom.Utils.Tasks;

namespace Phantom.Agent.Services.Instances; 

sealed record InstanceServices(TaskManager TaskManager, PortManager PortManager, BackupManager BackupManager, LaunchServices LaunchServices);
