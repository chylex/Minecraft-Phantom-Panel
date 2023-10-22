namespace Phantom.Agent.Minecraft.Server;

public sealed class DownloadProgressEventArgs : EventArgs {
	public ulong DownloadedBytes { get; }
	public ulong TotalBytes { get; }
	
	internal DownloadProgressEventArgs(ulong downloadedBytes, ulong totalBytes) {
		DownloadedBytes = downloadedBytes;
		TotalBytes = totalBytes;
	}
}
