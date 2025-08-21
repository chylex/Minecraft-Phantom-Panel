using NUnit.Framework;
using Phantom.Utils.Collections;

namespace Phantom.Utils.Tests.Collections;

[TestFixture]
public sealed class RingBufferTests {
	private static RingBuffer<string> PrepareRingBuffer(int capacity, params string[] items) {
		var buffer = new RingBuffer<string>(capacity);
		
		foreach (var item in items) {
			buffer.Add(item);
		}
		
		return buffer;
	}
	
	public sealed class Count {
		[Test]
		public void OneItem() {
			var buffer = PrepareRingBuffer(10, "a");
			Assert.That(buffer, Has.Count.EqualTo(1));
		}
		
		[Test]
		public void MultipleItemsWithinCapacity() {
			var buffer = PrepareRingBuffer(10, "a", "b", "c");
			Assert.That(buffer, Has.Count.EqualTo(3));
		}
		
		[Test]
		public void MultipleItemsOverflowingCapacity() {
			var buffer = PrepareRingBuffer(3, "a", "b", "c", "d", "e", "f");
			Assert.That(buffer, Has.Count.EqualTo(3));
		}
	}
	
	public sealed class Last {
		[Test]
		public void OneItem() {
			var buffer = PrepareRingBuffer(10, "a");
			Assert.That(buffer.Last, Is.EqualTo("a"));
		}
		
		[Test]
		public void MultipleItemsWithinCapacity() {
			var buffer = PrepareRingBuffer(10, "a", "b", "c");
			Assert.That(buffer.Last, Is.EqualTo("c"));
		}
		
		[Test]
		public void MultipleItemsOverflowingCapacity() {
			var buffer = PrepareRingBuffer(3, "a", "b", "c", "d", "e", "f");
			Assert.That(buffer.Last, Is.EqualTo("f"));
		}
	}
	
	public sealed class EnumerateLast {
		[Test]
		public void AddOneItemAndEnumerateOne() {
			var buffer = PrepareRingBuffer(10, "a");
			Assert.That(buffer.EnumerateLast(1), Is.EquivalentTo(new[] { "a" }));
		}
		
		[Test]
		public void AddOneItemAndEnumerateMaxValue() {
			var buffer = PrepareRingBuffer(10, "a");
			Assert.That(buffer.EnumerateLast(uint.MaxValue), Is.EquivalentTo(new[] { "a" }));
		}
		
		[Test]
		public void AddMultipleItemsWithinCapacityAndEnumerateFewer() {
			var buffer = PrepareRingBuffer(10, "a", "b", "c");
			Assert.That(buffer.EnumerateLast(2), Is.EquivalentTo(new[] { "b", "c" }));
		}
		
		[Test]
		public void AddMultipleItemsWithinCapacityAndEnumerateMaxValue() {
			var buffer = PrepareRingBuffer(10, "a", "b", "c");
			Assert.That(buffer.EnumerateLast(uint.MaxValue), Is.EquivalentTo(new[] { "a", "b", "c" }));
		}
		
		[TestCase(3)]
		[TestCase(4)]
		[TestCase(5)]
		public void AddMultipleItemsOverflowingCapacityAndEnumerateFewer(int capacity) {
			var buffer = PrepareRingBuffer(capacity, "a", "b", "c", "d", "e", "f");
			Assert.That(buffer.EnumerateLast(2), Is.EquivalentTo(new[] { "e", "f" }));
		}
		
		[TestCase(3, ExpectedResult = new[] { "d", "e", "f" })]
		[TestCase(4, ExpectedResult = new[] { "c", "d", "e", "f" })]
		[TestCase(5, ExpectedResult = new[] { "b", "c", "d", "e", "f" })]
		public string[] AddMultipleItemsOverflowingCapacityAndEnumerateMaxValue(int capacity) {
			var buffer = PrepareRingBuffer(capacity, "a", "b", "c", "d", "e", "f");
			return buffer.EnumerateLast(uint.MaxValue).ToArray();
		}
	}
}
