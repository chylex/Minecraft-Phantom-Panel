using Phantom.Utils.Logging;
using Phantom.Utils.Tasks;
using Serilog;

namespace Phantom.Utils.Rpc.Message;

sealed class MessageQueues {
	private readonly ILogger logger;
	private readonly TaskManager taskManager;
	private readonly Dictionary<MessageQueueKey, RpcQueue> queues = new ();

	private Task? stopTask;
	
	public MessageQueues(string loggerName) {
		this.logger = PhantomLogger.Create<MessageQueues>(loggerName);
		this.taskManager = new TaskManager(PhantomLogger.Create<TaskManager>(loggerName));
	}

	private RpcQueue GetOrCreateQueue(MessageQueueKey key) {
		if (!queues.TryGetValue(key, out var queue)) {
			queues[key] = queue = new RpcQueue(taskManager, "Message queue for " + key.Name);
		}

		return queue;
	}

	public Task Enqueue(MessageQueueKey key, Func<Task> task) {
		lock (this) {
			return stopTask == null ? GetOrCreateQueue(key).Enqueue(task) : Task.FromException(new OperationCanceledException());
		}
	}

	public Task<T> Enqueue<T>(MessageQueueKey key, Func<Task<T>> task) {
		lock (this) {
			return stopTask == null ? GetOrCreateQueue(key).Enqueue(task) : Task.FromException<T>(new OperationCanceledException());
		}
	}

	internal Task Stop() {
		lock (this) {
			if (stopTask == null) {
				logger.Debug("Stopping " + queues.Count + " message queue(s)...");

				stopTask = Task.WhenAll(queues.Values.Select(static queue => queue.Stop()))
				               .ContinueWith(_ => logger.Debug("All queues stopped."));
				
				queues.Clear();
			}
			
			return stopTask;
		}
	}
}
