using System.Collections.Immutable;
using System.Text;
using MemoryPack;

namespace Phantom.Common.Data;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial class AllowedPorts {
	[MemoryPackOrder(0)]
	[MemoryPackInclude]
	private	readonly ImmutableArray<PortRange> allDefinitions;

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

	private static AllowedPorts FromString(ReadOnlySpan<char> definitions) {
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
		return FromString(definitions.AsSpan());
	}
}
