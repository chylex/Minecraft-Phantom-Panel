using System.Collections.Immutable;
using MemoryPack;
using Phantom.Common.Data.Web.Users;
using Phantom.Utils.Actor;

namespace Phantom.Common.Messages.Web.ToController;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record GetRolesMessage : IMessageToController, ICanReply<ImmutableArray<RoleInfo>>;
