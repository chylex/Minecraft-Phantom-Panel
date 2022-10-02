using System.Collections.Immutable;
using System.Text;

namespace Phantom.Common.Data.Agent;

public sealed class AllowedPorts {
	private readonly ImmutableArray<PortRange> allDefinitions;

	private AllowedPorts(ImmutableArray<PortRange> allDefinitions) {
		// TODO normalize and deduplicate ranges
		this.allDefinitions = allDefinitions.Sort(static (def1, def2) => def1.FirstPort - def2.FirstPort);
	}

	public bool Contains(ushort port) {
		return allDefinitions.Any(definition => definition.Contains(port));
	}

	public override string ToString() {
		var builder = new StringBuilder();

		foreach (var definition in allDefinitions) {
			definition.ToString(builder);
			builder.Append(',');
		}

		if (builder.Length > 0) {
			builder.Length--;
		}

		return builder.ToString();
	}

	private readonly record struct PortRange(ushort FirstPort, ushort LastPort) {
		private PortRange(ushort port) : this(port, port) {}

		public bool Contains(ushort port) {
			return port >= FirstPort && port <= LastPort;
		}

		public void ToString(StringBuilder builder) {
			builder.Append(FirstPort);

			if (LastPort != FirstPort) {
				builder.Append('-');
				builder.Append(LastPort);
			}
		}

		public static PortRange Parse(ReadOnlySpan<char> definition) {
			int separatorIndex = definition.IndexOf('-');
			if (separatorIndex == -1) {
				var port = ParsePort(definition.Trim());
				return new PortRange(port);
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

	public static AllowedPorts FromString(ReadOnlySpan<char> definitions) {
		List<PortRange> parsedDefinitions = new ();

		while (!definitions.IsEmpty) {
			int separatorIndex = definitions.IndexOf(',');
			if (separatorIndex == -1) {
				parsedDefinitions.Add(PortRange.Parse(definitions));
				break;
			}
			else {
				parsedDefinitions.Add(PortRange.Parse(definitions[..separatorIndex]));
				definitions = definitions[(separatorIndex + 1)..];
			}
		}

		return new AllowedPorts(parsedDefinitions.ToImmutableArray());
	}

	public static AllowedPorts FromString(string definitions) {
		return FromString((ReadOnlySpan<char>) definitions);
	}
}
