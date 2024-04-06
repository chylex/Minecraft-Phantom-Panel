using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data;
using Phantom.Common.Data.Instance;
using Phantom.Common.Data.Web.Instance;
using Phantom.Common.Data.Web.Users;
using Phantom.Utils.Actor;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record CreateOrUpdateInstanceMessage(
	[property: MemoryPackOrder(0)] ImmutableArray<byte> AuthToken,
	[property: MemoryPackOrder(1)] Guid InstanceGuid,
	[property: MemoryPackOrder(2)] InstanceConfiguration Configuration
) : IMessageToController, ICanReply<Result<CreateOrUpdateInstanceResult, UserInstanceActionFailure>>;
