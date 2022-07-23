using Phantom.Utils.Math;

namespace Phantom.Utils.Collections; 

public sealed class RingBuffer<T> {
	private readonly T[] buffer;
	private int index;
	private long written;
	
	public RingBuffer(int capacity) {
		this.buffer = new T[capacity];
	}

	public void Add(T item) {
		buffer[index] = item;
		index = (index + 1) % buffer.Length;
		++written;
	}

	public IEnumerable<T> GetLast(uint maximumItems) {
		int capacity = buffer.Length;
		long count = Numbers.Min(written, capacity, maximumItems);
		int start = (int) (index - count + capacity);
		
		for (int i = 0; i < count; i++) {
			yield return buffer[(start + i) % capacity];
		}
	}
}
