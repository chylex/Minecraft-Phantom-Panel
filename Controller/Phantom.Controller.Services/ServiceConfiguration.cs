namespace Phantom.Controller.Services;

public sealed record ServiceConfiguration(
	string Version,
	byte[] AdministratorToken,
	CancellationToken CancellationToken
);
