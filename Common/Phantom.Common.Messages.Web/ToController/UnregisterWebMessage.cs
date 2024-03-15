using MemoryPack;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record UnregisterWebMessage : IMessageToController;
