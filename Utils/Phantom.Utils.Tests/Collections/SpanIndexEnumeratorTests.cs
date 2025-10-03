using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using Phantom.Utils.Collections;

namespace Phantom.Utils.Tests.Collections;

[TestFixture]
[SuppressMessage("Performance", "CA1861")]
public sealed class SpanIndexEnumeratorTests {
	private static SearchValues<char> Search => SearchValues.Create(' ', '-');
	
	private static List<int> Indices(string str) {
		List<int> indices = [];
		
		foreach (int index in str.AsSpan().IndicesOf(Search)) {
			indices.Add(index);
		}
		
		return indices;
	}
	
	[Test]
	public void Empty() {
		Assert.That(Indices(""), Is.EquivalentTo(Array.Empty<int>()));
	}
	
	[Test]
	public void OnlyFirstIndex() {
		Assert.That(Indices(" "), Is.EquivalentTo(new[] { 0 }));
	}
	
	[Test]
	public void OnlyMiddleIndex() {
		Assert.That(Indices("ab cd"), Is.EquivalentTo(new[] { 2 }));
	}
	
	[Test]
	public void OnlyLastIndex() {
		Assert.That(Indices("abc "), Is.EquivalentTo(new[] { 3 }));
	}
	
	[Test]
	public void FirstAndLastIndex() {
		Assert.That(Indices(" abc-"), Is.EquivalentTo(new[] { 0, 4 }));
	}
	
	[Test]
	public void AllIndices() {
		Assert.That(Indices("-  -  -"), Is.EquivalentTo(new[] { 0, 1, 2, 3, 4, 5, 6 }));
	}
}
