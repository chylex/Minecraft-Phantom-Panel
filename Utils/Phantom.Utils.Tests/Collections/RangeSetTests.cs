using NUnit.Framework;
using Phantom.Utils.Collections;
using Range = Phantom.Utils.Collections.RangeSet<int>.Range;

namespace Phantom.Utils.Tests.Collections;

[TestFixture]
public class RangeSetTests {
	[Test]
	public void OneValue() {
		var set = new RangeSet<int>();
		set.Add(5);
		
		Assert.That(set, Is.EqualTo(new[] {
			new Range(Min: 5, Max: 5),
		}));
	}
	
	[Test]
	public void MultipleDisjointValues() {
		var set = new RangeSet<int>();
		set.Add(5);
		set.Add(7);
		set.Add(1);
		set.Add(3);
		
		Assert.That(set, Is.EqualTo(new[] {
			new Range(Min: 1, Max: 1),
			new Range(Min: 3, Max: 3),
			new Range(Min: 5, Max: 5),
			new Range(Min: 7, Max: 7),
		}));
	}
	
	[Test]
	public void ExtendMin() {
		var set = new RangeSet<int>();
		set.Add(5);
		set.Add(4);
		
		Assert.That(set, Is.EqualTo(new[] {
			new Range(Min: 4, Max: 5),
		}));
	}
	
	[Test]
	public void ExtendMax() {
		var set = new RangeSet<int>();
		set.Add(5);
		set.Add(6);
		
		Assert.That(set, Is.EqualTo(new[] {
			new Range(Min: 5, Max: 6),
		}));
	}
	
	[Test]
	public void ExtendMaxAndMerge() {
		var set = new RangeSet<int>();
		set.Add(5);
		set.Add(7);
		set.Add(6);
		
		Assert.That(set, Is.EqualTo(new[] {
			new Range(Min: 5, Max: 7),
		}));
	}
	
	[Test]
	public void MultipleMergingAndDisjointValues() {
		var set = new RangeSet<int>();
		set.Add(1);
		set.Add(2);
		set.Add(5);
		set.Add(4);
		set.Add(10);
		set.Add(7);
		set.Add(9);
		set.Add(11);
		set.Add(16);
		set.Add(12);
		set.Add(3);
		set.Add(14);
		
		Assert.That(set, Is.EqualTo(new[] {
			new Range(Min: 1, Max: 5),
			new Range(Min: 7, Max: 7),
			new Range(Min: 9, Max: 12),
			new Range(Min: 14, Max: 14),
			new Range(Min: 16, Max: 16),
		}));
	}
}
