﻿using System.Diagnostics;
using Phantom.Utils.Collections;

namespace Phantom.Agent.Minecraft.Instance; 

public sealed class InstanceSession : IDisposable {
	private readonly RingBuffer<string> outputBuffer = new (10000);
	private event EventHandler<string>? OutputEvent;

	public event EventHandler? SessionEnded;
	public bool HasEnded { get; private set; }

	private readonly Process process;

	internal InstanceSession(Process process) {
		this.process = process;
		this.process.EnableRaisingEvents = true;
		this.process.Exited += ProcessOnExited;
		this.process.OutputDataReceived += HandleOutputLine;
		this.process.ErrorDataReceived += HandleOutputLine;
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

	private void HandleOutputLine(object sender, DataReceivedEventArgs args) {
		if (args.Data is {} line) {
			outputBuffer.Add(line);
			OutputEvent?.Invoke(this, line);
		}
	}

	private void ProcessOnExited(object? sender, EventArgs e) {
		OutputEvent = null;
		HasEnded = true;
		SessionEnded?.Invoke(this, EventArgs.Empty);
	}

	public void Kill() {
		process.Kill(true);
	}

	public async Task WaitForExit(CancellationToken cancellationToken) {
		if (!HasEnded) {
			await process.WaitForExitAsync(cancellationToken);
		}
	}

	public void Dispose() {
		process.Dispose();
		OutputEvent = null;
		SessionEnded = null;
	}
}
