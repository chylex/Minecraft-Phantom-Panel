namespace Phantom.Utils.Rpc.Runtime.Tls;

public sealed record DisallowedAlgorithmError(string ExpectedAlgorithmName, string ActualAlgorithmName);
