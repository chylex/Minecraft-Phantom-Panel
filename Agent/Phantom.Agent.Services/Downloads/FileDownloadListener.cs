namespace Phantom.Agent.Services.Downloads;

sealed record FileDownloadListener(EventHandler<DownloadProgressEventArgs> DownloadProgressEventHandler, CancellationToken CancellationToken);
