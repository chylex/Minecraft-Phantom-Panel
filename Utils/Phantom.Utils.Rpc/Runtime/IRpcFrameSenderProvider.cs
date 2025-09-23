using System.Threading.Channels;

namespace Phantom.Utils.Rpc.Runtime;

interface IRpcFrameSenderProvider<TMessageBase> {
	Task NewValueReady(CancellationToken cancellationToken);
	Task<RpcFrameSender<TMessageBase>> GetNewValue(CancellationToken cancellationToken);
	
	sealed record Constant(RpcFrameSender<TMessageBase> FrameSender) : IRpcFrameSenderProvider<TMessageBase> {
		public Task NewValueReady(CancellationToken cancellationToken) {
			return Task.Delay(Timeout.Infinite, cancellationToken);
		}
		
		public Task<RpcFrameSender<TMessageBase>> GetNewValue(CancellationToken cancellationToken) {
			return Task.FromResult(FrameSender);
		}
	}
	
	sealed class Mutable : IRpcFrameSenderProvider<TMessageBase>, IDisposable {
		private readonly Channel<RpcFrameSender<TMessageBase>> channel = Channel.CreateBounded<RpcFrameSender<TMessageBase>>(new BoundedChannelOptions(capacity: 1) {
			AllowSynchronousContinuations = false,
			FullMode = BoundedChannelFullMode.DropOldest,
			SingleReader = true,
			SingleWriter = false, // Technically there should only be a single writer, but it's external so this is safer.
		});
		
		public async Task NewValueReady(CancellationToken cancellationToken) {
			await channel.Reader.WaitToReadAsync(cancellationToken);
		}
		
		public async Task<RpcFrameSender<TMessageBase>> GetNewValue(CancellationToken cancellationToken) {
			return await channel.Reader.ReadAsync(cancellationToken);
		}
		
		public void SetNewValue(RpcFrameSender<TMessageBase> frameSender) {
			channel.Writer.TryWrite(frameSender);
		}
		
		public void Dispose() {
			channel.Writer.TryComplete();
		}
	}
}
