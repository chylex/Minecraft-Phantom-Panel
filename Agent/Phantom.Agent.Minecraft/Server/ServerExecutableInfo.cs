using Phantom.Utils.Cryptography;

namespace Phantom.Agent.Minecraft.Server; 

sealed record ServerExecutableInfo(
	string DownloadUrl,
	Sha1String Hash,
	long Size
);
