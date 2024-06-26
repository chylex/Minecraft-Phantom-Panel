﻿using Phantom.Utils.Collections;
using Phantom.Utils.Processes;
using Phantom.Utils.Tasks;

namespace Phantom.Agent.Minecraft.Instance;

public sealed class InstanceProcess : IDisposable {
	public InstanceProperties InstanceProperties { get; }
	
	private readonly RingBuffer<string> outputBuffer = new (100);
	private event EventHandler<string>? OutputEvent;

	public event EventHandler? Ended;
	public bool HasEnded { get; private set; }

	private readonly Process process;
	private readonly TaskCompletionSource processExited = AsyncTasks.CreateCompletionSource();

	internal InstanceProcess(InstanceProperties instanceProperties, Process process) {
		this.InstanceProperties = instanceProperties;
		this.process = process;
		this.process.Exited += ProcessOnExited;
		this.process.OutputReceived += ProcessOutputReceived;
	}

	public async Task SendCommand(string command, CancellationToken cancellationToken) {
		await process.StandardInput.WriteLineAsync(command.AsMemory(), cancellationToken);
	}

	public void AddOutputListener(EventHandler<string> listener, uint maxLinesToReadFromHistory = uint.MaxValue) {
		OutputEvent += listener;
		
		foreach (var line in outputBuffer.EnumerateLast(maxLinesToReadFromHistory)) {
			listener(this, line);
		}
	}

	public void RemoveOutputListener(EventHandler<string> listener) {
		OutputEvent -= listener;
	}

	private void ProcessOutputReceived(object? sender, Process.Output output) {
		outputBuffer.Add(output.Line);
		OutputEvent?.Invoke(this, output.Line);
	}

	private void ProcessOnExited(object? sender, EventArgs e) {
		OutputEvent = null;
		HasEnded = true;
		Ended?.Invoke(this, EventArgs.Empty);
		processExited.SetResult();
	}

	public void Kill() {
		process.Kill(true);
	}

	public async Task WaitForExit(TimeSpan timeout) {
		await processExited.Task.WaitAsync(timeout);
	}

	public void Dispose() {
		process.Dispose();
		OutputEvent = null;
		Ended = null;
	}
}
