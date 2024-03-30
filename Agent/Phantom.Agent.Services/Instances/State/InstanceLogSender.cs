using System.Collections.Immutable;
using System.Threading.Channels;
using Phantom.Agent.Rpc;
using Phantom.Common.Messages.Agent.ToController;
using Phantom.Utils.Logging;
using Phantom.Utils.Tasks;

namespace Phantom.Agent.Services.Instances.State;

sealed class InstanceLogSender : CancellableBackgroundTask {
	private static readonly BoundedChannelOptions BufferOptions = new (capacity: 100) {
		SingleReader = true,
		SingleWriter = true,
		FullMode = BoundedChannelFullMode.DropNewest
	};
	
	private static readonly TimeSpan SendDelay = TimeSpan.FromMilliseconds(200);

	private readonly ControllerConnection controllerConnection;
	private readonly Guid instanceGuid;
	private readonly Channel<string> outputChannel;
	
	private int droppedLinesSinceLastSend;

	public InstanceLogSender(ControllerConnection controllerConnection, TaskManager taskManager, Guid instanceGuid, string loggerName) : base(PhantomLogger.Create<InstanceLogSender>(loggerName), taskManager, "Instance log sender for " + loggerName) {
		this.controllerConnection = controllerConnection;
		this.instanceGuid = instanceGuid;
		this.outputChannel = Channel.CreateBounded<string>(BufferOptions, OnLineDropped);
		Start();
	}
	
	protected override async Task RunTask() {
		var lineReader = outputChannel.Reader;
		var lineBuilder = ImmutableArray.CreateBuilder<string>();
		
		try {
			while (await lineReader.WaitToReadAsync(CancellationToken)) {
				await Task.Delay(SendDelay, CancellationToken);
				SendOutputToServer(ReadLinesFromChannel(lineReader, lineBuilder));
			}
		} catch (OperationCanceledException) {
			// Ignore.
		}

		// Flush remaining lines.
		SendOutputToServer(ReadLinesFromChannel(lineReader, lineBuilder));
	}

	private ImmutableArray<string> ReadLinesFromChannel(ChannelReader<string> reader, ImmutableArray<string>.Builder builder) {
		builder.Clear();
		
		while (reader.TryRead(out string? line)) {
			builder.Add(line);
		}
		
		int droppedLines = Interlocked.Exchange(ref droppedLinesSinceLastSend, 0);
		if (droppedLines > 0) {
			builder.Add($"Dropped {droppedLines} {(droppedLines == 1 ? "line" : "lines")} due to buffer overflow.");
		}
		
		return builder.ToImmutable();
	}

	private void SendOutputToServer(ImmutableArray<string> lines) {
		if (!lines.IsEmpty) {
			controllerConnection.Send(new InstanceOutputMessage(instanceGuid, lines));
		}
	}

	private void OnLineDropped(string line) {
		Logger.Warning("Buffer is full, dropped line: {Line}", line);
		Interlocked.Increment(ref droppedLinesSinceLastSend);
	}

	public void Enqueue(string line) {
		outputChannel.Writer.TryWrite(line);
	}

	protected override void Dispose() {
		if (!outputChannel.Writer.TryComplete()) {
			Logger.Error("Could not mark channel as completed.");
		}
	}
}
