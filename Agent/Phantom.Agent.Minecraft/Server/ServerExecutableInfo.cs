using Phantom.Utils.Cryptography;
using Phantom.Utils.IO;

namespace Phantom.Agent.Minecraft.Server; 

sealed record ServerExecutableInfo(
	string DownloadUrl,
	Sha1String Hash,
	FileSize Size
);
