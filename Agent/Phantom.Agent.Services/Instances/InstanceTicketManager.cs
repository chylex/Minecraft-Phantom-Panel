using System.Collections.Immutable;
using Phantom.Agent.Services.Rpc;
using Phantom.Common.Data;
using Phantom.Common.Data.Agent;
using Phantom.Common.Data.Agent.Instance;
using Phantom.Common.Data.Replies;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Services.Instances;

sealed class InstanceTicketManager(AgentInfo agentInfo, ControllerConnection controllerConnection) {
	private static readonly ILogger Logger = PhantomLogger.Create<InstanceTicketManager>();
	
	private readonly ControllerSendQueue<ReportAgentStatusMessage> reportStatusQueue = new (controllerConnection, nameof(InstanceTicketManager), capacity: 1, singleWriter: true);
	
	private readonly HashSet<Guid> activeTicketGuids = [];
	private readonly HashSet<ushort> usedPorts = [];
	private RamAllocationUnits usedMemory = new ();
	
	public Result<Ticket, LaunchInstanceResult> Reserve(InstanceInfo info) {
		var memoryAllocation = info.MemoryAllocation;
		
		if (!agentInfo.AllowedServerPorts.Contains(info.ServerPort)) {
			return LaunchInstanceResult.ServerPortNotAllowed;
		}
		
		if (info.AdditionalPorts.Any(port => !agentInfo.AllowedAdditionalPorts.Contains(port))) {
			return LaunchInstanceResult.AdditionalPortNotAllowed;
		}
		
		lock (this) {
			if (activeTicketGuids.Count + 1 > agentInfo.MaxInstances) {
				return LaunchInstanceResult.InstanceLimitExceeded;
			}
			
			if (usedMemory + memoryAllocation > agentInfo.MaxMemory) {
				return LaunchInstanceResult.MemoryLimitExceeded;
			}
			
			if (usedPorts.Contains(info.ServerPort)) {
				return LaunchInstanceResult.ServerPortAlreadyInUse;
			}
			
			if (info.AdditionalPorts.Any(port => usedPorts.Contains(port))) {
				return LaunchInstanceResult.AdditionalPortAlreadyInUse;
			}
			
			var ticket = new Ticket(Guid.NewGuid(), memoryAllocation, info.ServerPort, info.AdditionalPorts);
			
			activeTicketGuids.Add(ticket.TicketGuid);
			usedMemory += memoryAllocation;
			usedPorts.Add(ticket.ServerPort);
			usedPorts.UnionWith(ticket.AdditionalPorts);
			
			RefreshAgentStatus();
			Logger.Debug("Reserved ticket {TicketGuid} (server port {ServerPort}, additional ports [{AdditionalPorts}], memory allocation {MemoryAllocation} MB).", ticket.TicketGuid, ticket.ServerPort, string.Join(", ", ticket.AdditionalPorts), ticket.MemoryAllocation.InMegabytes);
			
			return ticket;
		}
	}
	
	public bool IsValid(Ticket ticket) {
		lock (this) {
			return activeTicketGuids.Contains(ticket.TicketGuid);
		}
	}
	
	public void Release(Ticket ticket) {
		lock (this) {
			if (!activeTicketGuids.Remove(ticket.TicketGuid)) {
				return;
			}
			
			usedMemory -= ticket.MemoryAllocation;
			usedPorts.Remove(ticket.ServerPort);
			usedPorts.ExceptWith(ticket.AdditionalPorts);
			
			RefreshAgentStatus();
			Logger.Debug("Released ticket {TicketGuid} (server port {ServerPort}, additional ports [{AdditionalPorts}], memory allocation {MemoryAllocation} MB).", ticket.TicketGuid, ticket.ServerPort, string.Join(", ", ticket.AdditionalPorts), ticket.MemoryAllocation.InMegabytes);
		}
	}
	
	public void RefreshAgentStatus() {
		lock (this) {
			reportStatusQueue.Enqueue(new ReportAgentStatusMessage(activeTicketGuids.Count, usedMemory));
		}
	}
	
	public async Task Shutdown() {
		await reportStatusQueue.Shutdown(TimeSpan.FromSeconds(5));
	}
	
	public sealed record Ticket(Guid TicketGuid, RamAllocationUnits MemoryAllocation, ushort ServerPort, ImmutableSortedSet<ushort> AdditionalPorts);
}
