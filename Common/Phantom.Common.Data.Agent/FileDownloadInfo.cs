using MemoryPack;
using Phantom.Utils.Cryptography;

namespace Phantom.Common.Data.Agent;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial class FileDownloadInfo {
	[MemoryPackOrder(0)]
	public string Url { get; }
	
	[MemoryPackOrder(1)]
	[MemoryPackInclude]
	private readonly string? hash;
	
	[MemoryPackIgnore]
	public Sha1String? Hash => hash == null ? null : Sha1String.FromString(hash);
	
	public FileDownloadInfo(string url, Sha1String? hash = null) : this(url, hash?.ToString()) {}
	
	[MemoryPackConstructor]
	private FileDownloadInfo(string url, string? hash) {
		this.Url = url;
		this.hash = hash;
	}
}
