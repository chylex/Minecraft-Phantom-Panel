using System.Collections.Immutable;

namespace Phantom.Common.Data.Agent.Instance;

public interface IInstancePathResolver {
	string? Global(ImmutableArray<string> segments);
	string? Local(ImmutableArray<string> segments);
	string? Runtime(Guid guid);
}
