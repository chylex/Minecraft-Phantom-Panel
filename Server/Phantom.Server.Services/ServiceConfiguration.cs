namespace Phantom.Server.Services;

public sealed record ServiceConfiguration(
	byte[] AdministratorToken,
	CancellationToken CancellationToken
);
