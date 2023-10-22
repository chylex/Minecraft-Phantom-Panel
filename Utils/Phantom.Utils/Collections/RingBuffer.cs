namespace Phantom.Utils.Collections;

public sealed class RingBuffer<T> {
	private readonly T[] buffer;
	
	private int writeIndex;

	public RingBuffer(int capacity) {
		if (capacity < 0) {
			throw new ArgumentException("Capacity must not be negative.", nameof(capacity));
		}
		
		this.buffer = new T[capacity];
	}
	
	public int Capacity => buffer.Length;
	public int Count { get; private set; }

	public T Last => Count == 0 ? throw new InvalidOperationException("Ring buffer is empty.") : buffer[IndexOfItemFromEnd(1)];

	private int IndexOfItemFromEnd(int offset) {
		return (writeIndex - offset + Capacity) % Capacity;
	}
	
	public void Add(T item) {
		if (Capacity == 0) {
			throw new InvalidOperationException("Ring buffer has no capacity.");
		}
		
		buffer[writeIndex++] = item;
		Count = Math.Max(writeIndex, Count);
		writeIndex %= Capacity;
	}

	public void Clear() {
		Count = 0;
		writeIndex = 0;
	}

	public IEnumerable<T> EnumerateLast(uint maximumItems) {
		if (Capacity == 0) {
			yield break;
		}
		
		int totalItemsToReturn = (int) Math.Min(maximumItems, Count);
		
		// Yield items until we hit the end of the buffer.
		
		int startIndex = IndexOfItemFromEnd(totalItemsToReturn);
		int endOrMaxIndex = Math.Min(startIndex + totalItemsToReturn, Count);
		
		for (int i = startIndex; i < endOrMaxIndex; i++) {
			yield return buffer[i];
		}
		
		// Wrap around and yield remaining items.
		
		int remainingItemsToReturn = totalItemsToReturn - (endOrMaxIndex - startIndex);
		
		for (int i = 0; i < remainingItemsToReturn; i++) {
			yield return buffer[i];
		}
	}
}
