using System.Threading.Channels;
using Phantom.Common.Messages.Agent;
using Phantom.Utils.Logging;
using Serilog;

namespace Phantom.Agent.Services.Rpc;

sealed class ControllerSendQueue<TMessage> where TMessage : IMessageToController {
	private readonly ILogger logger;
	private readonly Channel<TMessage> channel;
	private readonly Task sendTask;
	private readonly CancellationTokenSource shutdownTokenSource = new ();
	
	public ControllerSendQueue(ControllerConnection controllerConnection, string loggerName, int capacity, bool singleWriter) {
		this.logger = PhantomLogger.Create<ControllerSendQueue<TMessage>>(loggerName);
		
		this.channel = Channel.CreateBounded<TMessage>(new BoundedChannelOptions(capacity) {
			AllowSynchronousContinuations = false,
			FullMode = BoundedChannelFullMode.DropOldest,
			SingleReader = true,
			SingleWriter = singleWriter,
		});
		
		this.sendTask = Send(controllerConnection, shutdownTokenSource.Token);
	}
	
	private async Task Send(ControllerConnection controllerConnection, CancellationToken cancellationToken) {
		await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken)) {
			await controllerConnection.Send(message, cancellationToken);
		}
	}
	
	public void Enqueue(TMessage message) {
		channel.Writer.TryWrite(message);
	}
	
	public async Task Shutdown(TimeSpan gracefulTimeout) {
		channel.Writer.TryComplete();
		
		try {
			await sendTask.WaitAsync(gracefulTimeout);
		} catch (TimeoutException) {
			logger.Warning("Timed out waiting for queue to finish processing.");
		} catch (Exception) {
			// Ignore.
		}
		
		await shutdownTokenSource.CancelAsync();
		await sendTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
		
		shutdownTokenSource.Dispose();
	}
}
