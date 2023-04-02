﻿using System.Reflection;
using Phantom.Agent;
using Phantom.Agent.Rpc;
using Phantom.Agent.Services;
using Phantom.Agent.Services.Rpc;
using Phantom.Common.Data.Agent;
using Phantom.Common.Logging;
using Phantom.Utils.Rpc;
using Phantom.Utils.Runtime;

const int ProtocolVersion = 1;

var shutdownCancellationTokenSource = new CancellationTokenSource();
var shutdownCancellationToken = shutdownCancellationTokenSource.Token;

PosixSignals.RegisterCancellation(shutdownCancellationTokenSource, static () => {
	PhantomLogger.Root.InformationHeading("Stopping Phantom Panel agent...");
});

ThreadPool.SetMinThreads(workerThreads: 2, completionPortThreads: 1);

try {
	var fullVersion = AssemblyAttributes.GetFullVersion(Assembly.GetExecutingAssembly());

	PhantomLogger.Root.InformationHeading("Initializing Phantom Panel agent...");
	PhantomLogger.Root.Information("Agent version: {Version}", fullVersion);

	var (serverHost, serverPort, javaSearchPath, agentKeyToken, agentKeyFilePath, agentName, maxInstances, maxMemory, allowedServerPorts, allowedRconPorts, maxConcurrentBackupCompressionTasks) = Variables.LoadOrStop();

	var agentKey = await AgentKey.Load(agentKeyToken, agentKeyFilePath);
	if (agentKey == null) {
		return 1;
	}

	var folders = new AgentFolders("./data", "./temp", javaSearchPath);
	if (!folders.TryCreate()) {
		return 1;
	}

	var agentGuid = await GuidFile.CreateOrLoad(folders.DataFolderPath);
	if (agentGuid == null) {
		return 1;
	}

	var (serverCertificate, agentToken) = agentKey.Value;
	var agentInfo = new AgentInfo(agentGuid.Value, agentName, ProtocolVersion, fullVersion, maxInstances, maxMemory, allowedServerPorts, allowedRconPorts);
	var agentServices = new AgentServices(agentInfo, folders, new AgentServiceConfiguration(maxConcurrentBackupCompressionTasks));

	MessageListener MessageListenerFactory(RpcServerConnection connection) {
		return new MessageListener(connection, agentServices, shutdownCancellationTokenSource);
	}

	PhantomLogger.Root.InformationHeading("Launching Phantom Panel agent...");

	await agentServices.Initialize();

	var rpcDisconnectSemaphore = new SemaphoreSlim(0, 1);
	var rpcConfiguration = new RpcConfiguration(PhantomLogger.Create("Rpc"), PhantomLogger.Create<TaskManager>("Rpc"), serverHost, serverPort, serverCertificate);
	var rpcTask = RpcLauncher.Launch(rpcConfiguration, agentToken, agentInfo, MessageListenerFactory, rpcDisconnectSemaphore, shutdownCancellationToken);
	try {
		await rpcTask.WaitAsync(shutdownCancellationToken);
	} finally {
		shutdownCancellationTokenSource.Cancel();
		await agentServices.Shutdown();

		rpcDisconnectSemaphore.Release();
		await rpcTask;
		rpcDisconnectSemaphore.Dispose();
	}

	return 0;
} catch (OperationCanceledException) {
	return 0;
} catch (StopProcedureException) {
	return 1;
} catch (Exception e) {
	PhantomLogger.Root.Fatal(e, "Caught exception in entry point.");
	return 1;
} finally {
	shutdownCancellationTokenSource.Dispose();

	PhantomLogger.Root.Information("Bye!");
	PhantomLogger.Dispose();
}
