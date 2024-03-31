using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data;
using Phantom.Common.Data.Web.Users;
using Phantom.Utils.Actor;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record GetAuthenticatedUser(
	[property: MemoryPackOrder(0)] Guid UserGuid,
	[property: MemoryPackOrder(1)] ImmutableArray<byte> SessionToken
) : IMessageToController, ICanReply<Optional<AuthenticatedUserInfo>>;
