namespace Phantom.Common.Data.Agent.Instance;

public interface IInstanceValueResolver {
	string? Path(IInstancePath value);
}
