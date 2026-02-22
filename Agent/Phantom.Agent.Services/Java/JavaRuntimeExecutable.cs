using Phantom.Common.Data.Java;

namespace Phantom.Agent.Services.Java;

sealed record JavaRuntimeExecutable(string ExecutablePath, JavaRuntime Runtime);
