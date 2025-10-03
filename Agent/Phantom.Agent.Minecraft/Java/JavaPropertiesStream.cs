using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Phantom.Utils.Collections;

namespace Phantom.Agent.Minecraft.Java;

static class JavaPropertiesStream {
	internal static readonly Encoding Encoding = Encoding.GetEncoding("ISO-8859-1");
	
	private static FileStreamOptions CreateFileStreamOptions(FileMode mode, FileAccess access) {
		return new FileStreamOptions {
			Mode = mode,
			Access = access,
			Share = FileShare.Read,
			Options = FileOptions.SequentialScan,
		};
	}
	
	internal sealed class Reader : IDisposable {
		private static readonly SearchValues<char> LineStartWhitespace = SearchValues.Create(' ', '\t', '\f');
		private static readonly SearchValues<char> KeyValueDelimiter = SearchValues.Create('=', ':', ' ', '\t', '\f');
		private static readonly SearchValues<char> Backslash = SearchValues.Create('\\');
		
		private readonly StreamReader reader;
		
		public Reader(Stream stream) {
			this.reader = new StreamReader(stream, Encoding, leaveOpen: false);
		}
		
		public Reader(string path) {
			this.reader = new StreamReader(path, Encoding, detectEncodingFromByteOrderMarks: false, CreateFileStreamOptions(FileMode.Open, FileAccess.Read));
		}
		
		public async IAsyncEnumerable<KeyValuePair<string, string>> ReadProperties([EnumeratorCancellation] CancellationToken cancellationToken) {
			await foreach (string line in ReadLogicalLines(cancellationToken)) {
				yield return ParseLine(line.AsSpan());
			}
		}
		
		private async IAsyncEnumerable<string> ReadLogicalLines([EnumeratorCancellation] CancellationToken cancellationToken) {
			StringBuilder nextLogicalLine = new StringBuilder();
			
			while (await reader.ReadLineAsync(cancellationToken) is {} line) {
				var span = line.AsSpan();
				int startIndex = span.IndexOfAnyExcept(LineStartWhitespace);
				if (startIndex == -1) {
					continue;
				}
				
				if (nextLogicalLine.Length == 0 && (span[0] == '#' || span[0] == '!')) {
					continue;
				}
				
				span = span[startIndex..];
				
				if (IsEndEscaped(span)) {
					nextLogicalLine.Append(span[..^1]);
					nextLogicalLine.Append('\n');
				}
				else {
					nextLogicalLine.Append(span);
					yield return nextLogicalLine.ToString();
					nextLogicalLine.Clear();
				}
			}
			
			if (nextLogicalLine.Length > 0) {
				yield return nextLogicalLine.ToString(startIndex: 0, nextLogicalLine.Length - 1); // Remove trailing new line.
			}
		}
		
		private static KeyValuePair<string, string> ParseLine(ReadOnlySpan<char> line) {
			int delimiterIndex = -1;
			
			foreach (int candidateIndex in line.IndicesOf(KeyValueDelimiter)) {
				if (candidateIndex == 0 || !IsEndEscaped(line[..candidateIndex])) {
					delimiterIndex = candidateIndex;
					break;
				}
			}
			
			if (delimiterIndex == -1) {
				return new KeyValuePair<string, string>(line.ToString(), string.Empty);
			}
			
			string key = ReadPropertyComponent(line[..delimiterIndex]);
			
			line = line[(delimiterIndex + 1)..];
			int valueStartIndex = line.IndexOfAnyExcept(KeyValueDelimiter);
			string value = valueStartIndex == -1 ? string.Empty : ReadPropertyComponent(line[valueStartIndex..]);
			
			return new KeyValuePair<string, string>(key, value);
		}
		
		private static string ReadPropertyComponent(ReadOnlySpan<char> component) {
			StringBuilder builder = new StringBuilder();
			int nextStartIndex = 0;
			
			foreach (int backslashIndex in component.IndicesOf(Backslash)) {
				if (backslashIndex == component.Length - 1) {
					break;
				}
				
				if (backslashIndex < nextStartIndex) {
					continue;
				}
				
				builder.Append(component[nextStartIndex..backslashIndex]);
				
				int escapedIndex = backslashIndex + 1;
				int escapedLength = 1;
				
				char c = component[escapedIndex];
				switch (c) {
					case 't':
						builder.Append('\t');
						break;
					
					case 'n':
						builder.Append('\n');
						break;
					
					case 'r':
						builder.Append('\r');
						break;
					
					case 'f':
						builder.Append('\f');
						break;
					
					case 'u':
						escapedLength += 4;
						
						int hexRangeStart = escapedIndex + 1;
						int hexRangeEnd = hexRangeStart + 4;
						
						if (hexRangeEnd - 1 < component.Length) {
							var hexString = component[hexRangeStart..hexRangeEnd];
							int hexValue = int.Parse(hexString, NumberStyles.HexNumber);
							builder.Append((char) hexValue);
						}
						else {
							throw new FormatException("Malformed \\uxxxx encoding.");
						}
						
						break;
					
					default:
						builder.Append(c);
						break;
				}
				
				nextStartIndex = escapedIndex + escapedLength;
			}
			
			builder.Append(component[nextStartIndex..]);
			return builder.ToString();
		}
		
		private static bool IsEndEscaped(ReadOnlySpan<char> span) {
			if (span.EndsWith('\\')) {
				int trailingBackslashCount = span.Length - span.TrimEnd('\\').Length;
				return trailingBackslashCount % 2 == 1;
			}
			else {
				return false;
			}
		}
		
		public void Dispose() {
			reader.Dispose();
		}
	}
	
	internal sealed class Writer : IAsyncDisposable {
		private const string CommentStart = "# ";
		
		private readonly StreamWriter writer;
		private readonly Memory<char> oneCharBuffer = new char[1];
		
		public Writer(Stream stream) {
			this.writer = new StreamWriter(stream, Encoding, leaveOpen: false);
		}
		
		public Writer(string path) {
			this.writer = new StreamWriter(path, Encoding, CreateFileStreamOptions(FileMode.Create, FileAccess.Write));
		}
		
		public async Task WriteComment(string comment, CancellationToken cancellationToken) {
			await Write(CommentStart, cancellationToken);
			
			for (int index = 0; index < comment.Length; index++) {
				char c = comment[index];
				switch (c) {
					case var _ when c > 31 && c < 127:
						await Write(c, cancellationToken);
						break;
					
					case '\n':
					case '\r':
						await Write(c: '\n', cancellationToken);
						await Write(CommentStart, cancellationToken);
						
						if (index < comment.Length - 1 && comment[index + 1] == '\n') {
							index++;
						}
						
						break;
					
					default:
						await Write("\\u", cancellationToken);
						await Write(((int) c).ToString("X4"), cancellationToken);
						break;
				}
			}
			
			await Write(c: '\n', cancellationToken);
		}
		
		public async Task WriteProperty(string key, string value, CancellationToken cancellationToken) {
			await WritePropertyComponent(key, escapeSpaces: true, cancellationToken);
			await Write(c: '=', cancellationToken);
			await WritePropertyComponent(value, escapeSpaces: false, cancellationToken);
			await Write(c: '\n', cancellationToken);
		}
		
		private async Task WritePropertyComponent(string component, bool escapeSpaces, CancellationToken cancellationToken) {
			for (int index = 0; index < component.Length; index++) {
				char c = component[index];
				switch (c) {
					case '\\':
					case '#':
					case '!':
					case '=':
					case ':':
					case ' ' when escapeSpaces || index == 0:
						await Write(c: '\\', cancellationToken);
						await Write(c, cancellationToken);
						break;
					
					case var _ when c > 31 && c < 127:
						await Write(c, cancellationToken);
						break;
					
					case '\t':
						await Write("\\t", cancellationToken);
						break;
					
					case '\n':
						await Write("\\n", cancellationToken);
						break;
					
					case '\r':
						await Write("\\r", cancellationToken);
						break;
					
					case '\f':
						await Write("\\f", cancellationToken);
						break;
					
					default:
						await Write("\\u", cancellationToken);
						await Write(((int) c).ToString("X4"), cancellationToken);
						break;
				}
			}
		}
		
		private Task Write(char c, CancellationToken cancellationToken) {
			oneCharBuffer.Span[0] = c;
			return writer.WriteAsync(oneCharBuffer, cancellationToken);
		}
		
		private Task Write(string value, CancellationToken cancellationToken) {
			return writer.WriteAsync(value.AsMemory(), cancellationToken);
		}
		
		public async ValueTask DisposeAsync() {
			await writer.DisposeAsync();
		}
	}
}
