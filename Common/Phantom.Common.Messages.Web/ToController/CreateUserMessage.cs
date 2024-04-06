using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data;
using Phantom.Common.Data.Web.Users;
using Phantom.Utils.Actor;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record CreateUserMessage(
	[property: MemoryPackOrder(0)] ImmutableArray<byte> AuthToken,
	[property: MemoryPackOrder(1)] string Username,
	[property: MemoryPackOrder(2)] string Password
) : IMessageToController, ICanReply<Result<CreateUserResult, UserActionFailure>>;
