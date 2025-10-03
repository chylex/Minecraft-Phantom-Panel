using System.Collections.Immutable;
using NUnit.Framework;
using Phantom.Agent.Minecraft.Java;
using Phantom.Utils.Collections;

namespace Phantom.Agent.Minecraft.Tests.Java;

[TestFixture]
public sealed class JavaPropertiesStreamTests {
	public sealed class Reader {
		private static async Task<ImmutableArray<KeyValuePair<string, string>>> Parse(string contents) {
			using var stream = new MemoryStream(JavaPropertiesStream.Encoding.GetBytes(contents));
			using var properties = new JavaPropertiesStream.Reader(stream);
			return await properties.ReadProperties(CancellationToken.None).ToImmutableArrayAsync();
		}
		
		private static ImmutableArray<KeyValuePair<string, string>> KeyValue(string key, string value) {
			return [new KeyValuePair<string, string>(key, value)];
		}
		
		[TestCase("")]
		[TestCase("\n")]
		public async Task EmptyLinesAreIgnored(string contents) {
			Assert.That(await Parse(contents), Is.EquivalentTo(ImmutableArray<KeyValuePair<string, string>>.Empty));
		}
		
		[TestCase("# Comment")]
		[TestCase("! Comment")]
		[TestCase("# Comment\n! Comment")]
		public async Task CommentsAreIgnored(string contents) {
			Assert.That(await Parse(contents), Is.EquivalentTo(ImmutableArray<KeyValuePair<string, string>>.Empty));
		}
		
		[TestCase("key=value")]
		[TestCase("key= value")]
		[TestCase("key =value")]
		[TestCase("key = value")]
		[TestCase("key:value")]
		[TestCase("key: value")]
		[TestCase("key :value")]
		[TestCase("key : value")]
		[TestCase("key value")]
		[TestCase("key\tvalue")]
		[TestCase("key\fvalue")]
		[TestCase("key \t\fvalue")]
		public async Task SimpleKeyValue(string contents) {
			Assert.That(await Parse(contents), Is.EquivalentTo(KeyValue("key", "value")));
		}
		
		[TestCase("key")]
		[TestCase(" key")]
		[TestCase(" key ")]
		[TestCase("key=")]
		[TestCase("key:")]
		public async Task KeyWithoutValue(string contents) {
			Assert.That(await Parse(contents), Is.EquivalentTo(KeyValue("key", "")));
		}
		
		[TestCase(@"\#key=value", "#key")]
		[TestCase(@"\!key=value", "!key")]
		public async Task KeyBeginsWithEscapedComment(string contents, string expectedKey) {
			Assert.That(await Parse(contents), Is.EquivalentTo(KeyValue(expectedKey, "value")));
		}
		
		[TestCase(@"\=key=value", "=key")]
		[TestCase(@"\:key=value", ":key")]
		[TestCase(@"\ key=value", " key")]
		[TestCase("\\\tkey=value", "\tkey")]
		[TestCase("\\\fkey=value", "\fkey")]
		public async Task KeyBeginsWithEscapedDelimiter(string contents, string expectedKey) {
			Assert.That(await Parse(contents), Is.EquivalentTo(KeyValue(expectedKey, "value")));
		}
		
		[TestCase(@"start\=end=value", "start=end")]
		[TestCase(@"start\:end:value", "start:end")]
		[TestCase(@"start\ end value", "start end")]
		[TestCase(@"start\ \:\=end = value", "start :=end")]
		[TestCase("start\\ \\\t\\\fend = value", "start \t\fend")]
		public async Task KeyContainsEscapedDelimiter(string contents, string expectedKey) {
			Assert.That(await Parse(contents), Is.EquivalentTo(KeyValue(expectedKey, "value")));
		}
		
		[TestCase(@"key = \ value", " value")]
		[TestCase("key = \\\tvalue", "\tvalue")]
		[TestCase("key = \\\fvalue", "\fvalue")]
		[TestCase("key=\\ \\\t\\\fvalue", " \t\fvalue")]
		public async Task ValueBeginsWithEscapedWhitespace(string contents, string expectedValue) {
			Assert.That(await Parse(contents), Is.EquivalentTo(KeyValue("key", expectedValue)));
		}
		
		[TestCase(@"key = value\", "value")]
		public async Task ValueEndsWithTrailingBackslash(string contents, string expectedValue) {
			Assert.That(await Parse(contents), Is.EquivalentTo(KeyValue("key", expectedValue)));
		}
		
		[TestCase("key=\\\0", "\0")]
		[TestCase(@"key=\\", "\\")]
		[TestCase(@"key=\t", "\t")]
		[TestCase(@"key=\n", "\n")]
		[TestCase(@"key=\r", "\r")]
		[TestCase(@"key=\f", "\f")]
		[TestCase(@"key=\u3053\u3093\u306b\u3061\u306f", "こんにちは")]
		[TestCase(@"key=\u3053\u3093\u306B\u3061\u306F", "こんにちは")]
		[TestCase("key=\\\0\\\\\\t\\n\\r\\f\\u3053", "\0\\\t\n\r\fこ")]
		public async Task ValueContainsEscapedSpecialCharacters(string contents, string expectedValue) {
			Assert.That(await Parse(contents), Is.EquivalentTo(KeyValue("key", expectedValue)));
		}
		
		[TestCase("key=first\\\nsecond", "first\nsecond")]
		[TestCase("key=first\\\n    second", "first\nsecond")]
		[TestCase("key=first\\\n#second", "first\n#second")]
		[TestCase("key=first\\\n!second", "first\n!second")]
		public async Task ValueContainsNewLine(string contents, string expectedValue) {
			Assert.That(await Parse(contents), Is.EquivalentTo(KeyValue("key", expectedValue)));
		}
		
		[TestCase("key=first\\\n    \\ second", "first\n second")]
		[TestCase("key=first\\\n    \\\tsecond", "first\n\tsecond")]
		[TestCase("key=first\\\n    \\\fsecond", "first\n\fsecond")]
		[TestCase("key=first\\\n \t\f\\ second", "first\n second")]
		public async Task ValueContainsNewLineWithEscapedLeadingWhitespace(string contents, string expectedValue) {
			Assert.That(await Parse(contents), Is.EquivalentTo(KeyValue("key", expectedValue)));
		}
		
		[Test]
		public async Task ExampleFile() {
			// From Wikipedia: https://en.wikipedia.org/wiki/.properties
			const string ExampleFile = """
			                           # You are reading a comment in ".properties" file.
			                           ! The exclamation mark ('!') can also be used for comments.
			                           # Comments are ignored.
			                           # Blank lines are also ignored.
			                           
			                           # Lines with "properties" contain a key and a value separated by a delimiting character.
			                           # There are 3 delimiting characters: equal ('='), colon (':') and whitespace (' ', '\t' and '\f').
			                           website = https://en.wikipedia.org/
			                           language : English
			                           topic .properties files
			                           # A word on a line will just create a key with no value.
			                           empty
			                           # Whitespace that appears between the key, the delimiter and the value is ignored.
			                           # This means that the following are equivalent (other than for readability).
			                           hello=hello
			                           hello = hello
			                           # To start the value with whitespace, escape it with a backslash ('\').
			                           whitespaceStart = \ <-This space is not ignored.
			                           # Keys with the same name will be overwritten by the key that is the furthest in a file.
			                           # For example the final value for "duplicateKey" will be "second".
			                           duplicateKey = first
			                           duplicateKey = second
			                           # To use the delimiter characters inside a key, you need to escape them with a ('\').
			                           # However, there is no need to do this in the value.
			                           delimiterCharacters\:\=\ = This is the value for the key "delimiterCharacters\:\=\ "
			                           # Adding a backslash ('\') at the end of a line means that the value continues on the next line.
			                           multiline = This line \
			                           continues
			                           # If you want your value to include a backslash ('\'), it should be escaped by another backslash ('\').
			                           path = c:\\wiki\\templates
			                           # This means that if the number of backslashes ('\') at the end of the line is even, the next line is not included in the value. 
			                           # In the following example, the value for "evenKey" is "This is on one line\".
			                           evenKey = This is on one line\\
			                           # This line is a normal comment and is not included in the value for "evenKey".
			                           # If the number of backslash ('\') is odd, then the next line is included in the value.
			                           # In the following example, the value for "oddKey" is "This is line one and\# This is line two".
			                           oddKey = This is line one and\\\
			                           # This is line two
			                           # Whitespace characters at the beginning of a line is removed.
			                           # Make sure to add the spaces you need before the backslash ('\') on the first line. 
			                           # If you add them at the beginning of the next line, they will be removed.
			                           # In the following example, the value for "welcome" is "Welcome to Wikipedia!".
			                           welcome = Welcome to \
			                                     Wikipedia!
			                           # If you need to add newlines and carriage returns, they need to be escaped using ('\n') and ('\r') respectively.
			                           # You can also optionally escape tabs with ('\t') for readability purposes.
			                           valueWithEscapes = This is a newline\n and a carriage return\r and a tab\t.
			                           # You can also use Unicode escape characters (maximum of four hexadecimal digits).
			                           # In the following example, the value for "encodedHelloInJapanese" is "こんにちは".
			                           encodedHelloInJapanese = \u3053\u3093\u306b\u3061\u306f
			                           """;
			
			ImmutableArray<KeyValuePair<string, string>> result = [
				new ("website", "https://en.wikipedia.org/"),
				new ("language", "English"),
				new ("topic", ".properties files"),
				new ("empty", ""),
				new ("hello", "hello"),
				new ("hello", "hello"),
				new ("whitespaceStart", @" <-This space is not ignored."),
				new ("duplicateKey", "first"),
				new ("duplicateKey", "second"),
				new ("delimiterCharacters:= ", @"This is the value for the key ""delimiterCharacters:= """),
				new ("multiline", "This line \ncontinues"),
				new ("path", @"c:\wiki\templates"),
				new ("evenKey", @"This is on one line\"),
				new ("oddKey", "This is line one and\\\n# This is line two"),
				new ("welcome", "Welcome to \nWikipedia!"),
				new ("valueWithEscapes", "This is a newline\n and a carriage return\r and a tab\t."),
				new ("encodedHelloInJapanese", "こんにちは"),
			];
			
			Assert.That(await Parse(ExampleFile), Is.EquivalentTo(result));
		}
	}
	
	public sealed class Writer {
		private static async Task<string> Write(Func<JavaPropertiesStream.Writer, Task> write) {
			using var stream = new MemoryStream();
			
			await using (var writer = new JavaPropertiesStream.Writer(stream)) {
				await write(writer);
			}
			
			return JavaPropertiesStream.Encoding.GetString(stream.ToArray());
		}
		
		[TestCase("one line comment", "# one line comment\n")]
		[TestCase("こんにちは", "# \\u3053\\u3093\\u306B\\u3061\\u306F\n")]
		[TestCase("first line\nsecond line\r\nthird line", "# first line\n# second line\n# third line\n")]
		public async Task Comment(string comment, string contents) {
			Assert.That(await Write(writer => writer.WriteComment(comment, CancellationToken.None)), Is.EqualTo(contents));
		}
		
		[TestCase("key", "value", "key=value\n")]
		[TestCase("key", "", "key=\n")]
		[TestCase("", "value", "=value\n")]
		public async Task SimpleKeyValue(string key, string value, string contents) {
			Assert.That(await Write(writer => writer.WriteProperty(key, value, CancellationToken.None)), Is.EqualTo(contents));
		}
		
		[TestCase("#key", "value", "\\#key=value\n")]
		[TestCase("!key", "value", "\\!key=value\n")]
		public async Task KeyBeginsWithEscapedComment(string key, string value, string contents) {
			Assert.That(await Write(writer => writer.WriteProperty(key, value, CancellationToken.None)), Is.EqualTo(contents));
		}
		
		[TestCase("=key", "value", "\\=key=value\n")]
		[TestCase(":key", "value", "\\:key=value\n")]
		[TestCase(" key", "value", "\\ key=value\n")]
		[TestCase("\tkey", "value", "\\tkey=value\n")]
		[TestCase("\fkey", "value", "\\fkey=value\n")]
		public async Task KeyBeginsWithEscapedDelimiter(string key, string value, string contents) {
			Assert.That(await Write(writer => writer.WriteProperty(key, value, CancellationToken.None)), Is.EqualTo(contents));
		}
		
		[TestCase("start=end", "value", "start\\=end=value\n")]
		[TestCase("start:end", "value", "start\\:end=value\n")]
		[TestCase("start end", "value", "start\\ end=value\n")]
		[TestCase("start :=end", "value", "start\\ \\:\\=end=value\n")]
		[TestCase("start \t\fend", "value", "start\\ \\t\\fend=value\n")]
		public async Task KeyContainsEscapedDelimiter(string key, string value, string contents) {
			Assert.That(await Write(writer => writer.WriteProperty(key, value, CancellationToken.None)), Is.EqualTo(contents));
		}
		
		[TestCase("\\", "value", "\\\\=value\n")]
		[TestCase("\t", "value", "\\t=value\n")]
		[TestCase("\n", "value", "\\n=value\n")]
		[TestCase("\r", "value", "\\r=value\n")]
		[TestCase("\f", "value", "\\f=value\n")]
		[TestCase("こんにちは", "value", "\\u3053\\u3093\\u306B\\u3061\\u306F=value\n")]
		[TestCase("\\\t\n\r\fこ", "value", "\\\\\\t\\n\\r\\f\\u3053=value\n")]
		[TestCase("first-line\nsecond-line\r\nthird-line", "value", "first-line\\nsecond-line\\r\\nthird-line=value\n")]
		public async Task KeyContainsEscapedSpecialCharacters(string key, string value, string contents) {
			Assert.That(await Write(writer => writer.WriteProperty(key, value, CancellationToken.None)), Is.EqualTo(contents));
		}
		
		[TestCase("key", "\\", "key=\\\\\n")]
		[TestCase("key", "\t", "key=\\t\n")]
		[TestCase("key", "\n", "key=\\n\n")]
		[TestCase("key", "\r", "key=\\r\n")]
		[TestCase("key", "\f", "key=\\f\n")]
		[TestCase("key", "こんにちは", "key=\\u3053\\u3093\\u306B\\u3061\\u306F\n")]
		[TestCase("key", "\\\t\n\r\fこ", "key=\\\\\\t\\n\\r\\f\\u3053\n")]
		[TestCase("key", "first line\nsecond line\r\nthird line", "key=first line\\nsecond line\\r\\nthird line\n")]
		public async Task ValueContainsEscapedSpecialCharacters(string key, string value, string contents) {
			Assert.That(await Write(writer => writer.WriteProperty(key, value, CancellationToken.None)), Is.EqualTo(contents));
		}
		
		[Test]
		public async Task ExampleFile() {
			string contents = await Write(static async writer => {
				await writer.WriteComment("Comment", CancellationToken.None);
				await writer.WriteProperty("key", "value", CancellationToken.None);
				await writer.WriteProperty("multiline", "first line\nsecond line", CancellationToken.None);
			});
			
			Assert.That(contents, Is.EqualTo("# Comment\nkey=value\nmultiline=first line\\nsecond line\n"));
		}
	}
}
