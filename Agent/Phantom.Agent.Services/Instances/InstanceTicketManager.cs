using Phantom.Agent.Rpc;
using Phantom.Common.Data;
using Phantom.Common.Data.Agent;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Utils.Tasks;

namespace Phantom.Agent.Services.Instances;

sealed class InstanceTicketManager {
	private readonly AgentInfo agentInfo;
	private readonly ControllerConnection controllerConnection;
	
	private readonly HashSet<Guid> runningInstanceGuids = new ();
	private readonly HashSet<ushort> usedPorts = new ();
	private RamAllocationUnits usedMemory = new ();

	public InstanceTicketManager(AgentInfo agentInfo, ControllerConnection controllerConnection) {
		this.agentInfo = agentInfo;
		this.controllerConnection = controllerConnection;
	}

	public Result<Ticket, LaunchInstanceResult> Reserve(Guid instanceGuid, InstanceConfiguration configuration) {
		var memoryAllocation = configuration.MemoryAllocation;
		var serverPort = configuration.ServerPort;
		var rconPort = configuration.RconPort;
		
		if (!agentInfo.AllowedServerPorts.Contains(serverPort)) {
			return LaunchInstanceResult.ServerPortNotAllowed;
		}

		if (!agentInfo.AllowedRconPorts.Contains(rconPort)) {
			return LaunchInstanceResult.RconPortNotAllowed;
		}
		
		lock (this) {
			if (runningInstanceGuids.Count + 1 > agentInfo.MaxInstances) {
				return LaunchInstanceResult.InstanceLimitExceeded;
			}

			if (usedMemory + memoryAllocation > agentInfo.MaxMemory) {
				return LaunchInstanceResult.MemoryLimitExceeded;
			}
			
			if (usedPorts.Contains(serverPort)) {
				return LaunchInstanceResult.ServerPortAlreadyInUse;
			}

			if (usedPorts.Contains(rconPort)) {
				return LaunchInstanceResult.RconPortAlreadyInUse;
			}

			runningInstanceGuids.Add(instanceGuid);
			usedMemory += memoryAllocation;
			usedPorts.Add(serverPort);
			usedPorts.Add(rconPort);
			
			RefreshAgentStatus();
			
			return new Ticket(instanceGuid, memoryAllocation, serverPort, rconPort);
		}
	}
	
	public void Release(Ticket ticket) {
		lock (this) {
			if (!runningInstanceGuids.Remove(ticket.InstanceGuid)) {
				return;
			}

			usedMemory -= ticket.MemoryAllocation;
			usedPorts.Remove(ticket.ServerPort);
			usedPorts.Remove(ticket.RconPort);
			
			RefreshAgentStatus();
		}
	}
	
	public void RefreshAgentStatus() {
		lock (this) {
			controllerConnection.Send(new ReportAgentStatusMessage(runningInstanceGuids.Count, usedMemory));
		}
	}
	
	public sealed record Ticket(Guid InstanceGuid, RamAllocationUnits MemoryAllocation, ushort ServerPort, ushort RconPort);
}
