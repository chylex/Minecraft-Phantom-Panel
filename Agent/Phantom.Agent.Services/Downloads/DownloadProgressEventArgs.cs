namespace Phantom.Agent.Services.Downloads;

sealed class DownloadProgressEventArgs : EventArgs {
	public ulong DownloadedBytes { get; }
	public ulong? TotalBytes { get; }
	
	internal DownloadProgressEventArgs(ulong downloadedBytes, ulong? totalBytes) {
		DownloadedBytes = downloadedBytes;
		TotalBytes = totalBytes;
	}
}
