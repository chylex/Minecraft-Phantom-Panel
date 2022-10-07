using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using MessagePack;

namespace Phantom.Common.Data;

[MessagePackObject]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public sealed class AllowedPorts {
	[Key(0)]
	public ImmutableArray<PortRange> AllDefinitions { get; }

	public AllowedPorts(ImmutableArray<PortRange> allDefinitions) {
		// TODO normalize and deduplicate ranges
		this.AllDefinitions = allDefinitions.Sort(static (def1, def2) => def1.FirstPort - def2.FirstPort);
	}

	public bool Contains(ushort port) {
		return AllDefinitions.Any(definition => definition.Contains(port));
	}

	public override string ToString() {
		var builder = new StringBuilder();

		foreach (var definition in AllDefinitions) {
			definition.ToString(builder);
			builder.Append(',');
		}

		if (builder.Length > 0) {
			builder.Length--;
		}

		return builder.ToString();
	}

	[MessagePackObject]
	public readonly record struct PortRange(
		[property: Key(0)] ushort FirstPort,
		[property: Key(1)] ushort LastPort
	) {
		private PortRange(ushort port) : this(port, port) {}

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
