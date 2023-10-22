using Phantom.Agent.Services.Instances.Procedures;
using Phantom.Agent.Services.Instances.States;
using Phantom.Common.Data.Instance;
using Serilog;

namespace Phantom.Agent.Services.Instances;

interface IInstanceContext {
	string ShortName { get; }
	ILogger Logger { get; }
	
	InstanceServices Services { get; }
	IInstanceState CurrentState { get; }

	void SetStatus(IInstanceStatus newStatus);
	void ReportEvent(IInstanceEvent instanceEvent);
	void EnqueueProcedure(IInstanceProcedure procedure, bool immediate = false);
}

static class InstanceContextExtensions {
	public static void SetLaunchFailedStatusAndReportEvent(this IInstanceContext context, InstanceLaunchFailReason reason) {
		context.SetStatus(InstanceStatus.Failed(reason));
		context.ReportEvent(new InstanceLaunchFailedEvent(reason));
	}
}
