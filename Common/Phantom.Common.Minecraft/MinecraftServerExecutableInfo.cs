using Phantom.Utils.Cryptography;
using Phantom.Utils.IO;

namespace Phantom.Common.Minecraft; 

public sealed record MinecraftServerExecutableInfo(
	string DownloadUrl,
	Sha1String Hash,
	FileSize Size
);
