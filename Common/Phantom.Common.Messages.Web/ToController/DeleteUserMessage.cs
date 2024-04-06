using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data;
using Phantom.Common.Data.Web.Users;
using Phantom.Utils.Actor;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record DeleteUserMessage(
	[property: MemoryPackOrder(0)] ImmutableArray<byte> AuthToken,
	[property: MemoryPackOrder(1)] Guid SubjectUserGuid
) : IMessageToController, ICanReply<Result<DeleteUserResult, UserActionFailure>>;
