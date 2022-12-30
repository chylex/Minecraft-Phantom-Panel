using System.Text;
using MemoryPack;

namespace Phantom.Common.Data;

[MemoryPackable]
readonly partial record struct PortRange(
	[property: MemoryPackOrder(0)] ushort FirstPort,
	[property: MemoryPackOrder(1)] ushort LastPort
) {
	internal bool Contains(ushort port) {
		return port >= FirstPort && port <= LastPort;
	}

	internal void ToString(StringBuilder builder) {
		builder.Append(FirstPort);

		if (LastPort != FirstPort) {
			builder.Append('-');
			builder.Append(LastPort);
		}
	}

	internal static PortRange Parse(ReadOnlySpan<char> definition) {
		int separatorIndex = definition.IndexOf('-');
		if (separatorIndex == -1) {
			var port = ParsePort(definition.Trim());
			return new PortRange(port, port);
		}

		var firstPort = ParsePort(definition[..separatorIndex].Trim());
		var lastPort = ParsePort(definition[(separatorIndex + 1)..].Trim());
		if (lastPort < firstPort) {
			throw new FormatException("Invalid port range '" + firstPort + "-" + lastPort + "'.");
		}
		else {
			return new PortRange(firstPort, lastPort);
		}
	}

	private static ushort ParsePort(ReadOnlySpan<char> port) {
		try {
			return ushort.Parse(port);
		} catch (Exception) {
			throw new FormatException("Invalid port '" + port.ToString() + "'.");
		}
	}
}
