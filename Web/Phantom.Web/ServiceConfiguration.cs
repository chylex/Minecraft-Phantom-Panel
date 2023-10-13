namespace Phantom.Web;

public sealed record ServiceConfiguration(
	string Version,
	byte[] AdministratorToken,
	CancellationToken CancellationToken
);
