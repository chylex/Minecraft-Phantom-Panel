using System.Diagnostics;

namespace Phantom.Utils.Runtime;

public sealed class Process : IDisposable {
	public readonly record struct Output(string Line, bool IsError);
	
	public event EventHandler<Output>? OutputReceived;
	
	public event EventHandler Exited {
		add {
			wrapped.EnableRaisingEvents = true;
			wrapped.Exited += value;
		}
		remove {
			wrapped.Exited -= value;
		}
	}

	public bool HasExited => wrapped.HasExited;
	public int ExitCode => wrapped.ExitCode;
	public StreamWriter StandardInput => wrapped.StandardInput;

	private readonly System.Diagnostics.Process wrapped;

	internal Process(System.Diagnostics.Process wrapped) {
		this.wrapped = wrapped;
	}

	public void Start() {
		if (!OperatingSystem.IsWindows()) {
			this.wrapped.OutputDataReceived += OnStandardOutputDataReceived;
			this.wrapped.ErrorDataReceived += OnStandardErrorDataReceived;
		}
			
		this.wrapped.Start();

		// https://github.com/dotnet/runtime/issues/81896
		if (OperatingSystem.IsWindows()) {
			Task.Factory.StartNew(ReadStandardOutputSynchronously, TaskCreationOptions.LongRunning);
			Task.Factory.StartNew(ReadStandardErrorSynchronously, TaskCreationOptions.LongRunning);
		}
		else {
			this.wrapped.BeginOutputReadLine();
			this.wrapped.BeginErrorReadLine();
		}
	}

	private void OnStandardOutputDataReceived(object sender, DataReceivedEventArgs e) {
		OnStandardStreamDataReceived(e.Data, isError: false);
	}

	private void OnStandardErrorDataReceived(object sender, DataReceivedEventArgs e) {
		OnStandardStreamDataReceived(e.Data, isError: true);
	}

	private void OnStandardStreamDataReceived(string? line, bool isError) {
		if (line != null) {
			OutputReceived?.Invoke(this, new Output(line, isError));
		}
	}

	private void ReadStandardOutputSynchronously() {
		ReadStandardStreamSynchronously(wrapped.StandardOutput, isError: false);
	}

	private void ReadStandardErrorSynchronously() {
		ReadStandardStreamSynchronously(wrapped.StandardError, isError: true);
	}

	private void ReadStandardStreamSynchronously(StreamReader reader, bool isError) {
		try {
			while (reader.ReadLine() is {} line) {
				OutputReceived?.Invoke(this, new Output(line, isError));
			}
		} catch (ObjectDisposedException) {
			// Ignore.
		}
	}

	public Task WaitForExitAsync(CancellationToken cancellationToken) {
		return wrapped.WaitForExitAsync(cancellationToken);
	}

	public void Kill(bool entireProcessTree = false) {
		wrapped.Kill(entireProcessTree);
	}

	public void Dispose() {
		wrapped.Dispose();
	}
}
