using MemoryPack;
using Phantom.Utils.Cryptography;
using Phantom.Utils.IO;

namespace Phantom.Common.Data.Minecraft; 

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial class FileDownloadInfo {
	[MemoryPackOrder(0)]
	public string DownloadUrl { get; }
	
	[MemoryPackOrder(1)]
	[MemoryPackInclude]
	private readonly string hash;
	
	[MemoryPackIgnore]
	public Sha1String Hash => Sha1String.FromString(hash);
	
	[MemoryPackOrder(2)]
	public FileSize Size { get; }
	
	public FileDownloadInfo(string downloadUrl, Sha1String hash, FileSize size) : this(downloadUrl, hash.ToString(), size) {}

	[MemoryPackConstructor]
	private FileDownloadInfo(string downloadUrl, string hash, FileSize size) {
		this.DownloadUrl = downloadUrl;
		this.hash = hash;
		this.Size = size;
	}
}
