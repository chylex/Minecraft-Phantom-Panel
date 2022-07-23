using System.Diagnostics;
using Phantom.Utils.Collections;

namespace Phantom.Agent.Minecraft.Instance; 

public sealed class InstanceSession {
	private readonly RingBuffer<string> outputBuffer = new (10000);
	private event EventHandler<string>? OutputEvent;

	private readonly Process process;

	internal InstanceSession(Process process) {
		this.process = process;
		this.process.OutputDataReceived += HandleOutputLine;
		this.process.ErrorDataReceived += HandleOutputLine;
	}

	public void SendCommand(string command) {
		process.StandardInput.WriteLine(command);
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

	public void WaitForExit() {
		process.WaitForExit();
	}
}
