using MemoryPack;

namespace Phantom.Common.Messages.Agent.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record UnregisterAgentMessage : IMessageToController;
