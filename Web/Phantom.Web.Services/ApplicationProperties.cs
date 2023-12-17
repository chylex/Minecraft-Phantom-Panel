namespace Phantom.Web.Services;

public sealed record ApplicationProperties(
	string Version,
	byte[] AdministratorToken
);
