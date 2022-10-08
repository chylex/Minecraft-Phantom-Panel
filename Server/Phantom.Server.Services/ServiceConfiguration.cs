using Phantom.Utils.Threading;

namespace Phantom.Server.Services;

public sealed record ServiceConfiguration(
	byte[] AdministratorToken,
	TaskManager TaskManager,
	CancellationToken CancellationToken
);
