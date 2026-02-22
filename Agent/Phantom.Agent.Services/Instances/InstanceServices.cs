using Phantom.Agent.Services.Backups;
using Phantom.Agent.Services.Downloads;
using Phantom.Agent.Services.Java;
using Phantom.Agent.Services.Rpc;

namespace Phantom.Agent.Services.Instances;

sealed record InstanceServices(
	ControllerConnection ControllerConnection,
	BackupManager BackupManager,
	FileDownloadManager DownloadManager,
	JavaRuntimeRepository JavaRuntimeRepository
);
