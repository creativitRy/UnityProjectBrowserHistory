using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace CloudCanards.Core.Algorithms
{
	/// <summary>
	/// A fast implementation of a deque that has a limited capacity. Capacity needs to be a power of 2. Capacity cannot be changed once set.
	/// </summary>
	/// <typeparam name="T">Type to store (must not be a reference type and must not contain any reference type members at any level of nesting)</typeparam>
	public class LimitedDeque<T>
	{
		private int _start;
		private readonly int _mask;
		private readonly T[] _array;

		public int Count { get; private set; }

		/// <summary>
		/// Constructs a new deque
		/// </summary>
		/// <param name="maxCapacity">maximum count of this deque. Needs to be a power of 2</param>
		public LimitedDeque(int maxCapacity)
		{
#if UNITY_EDITOR
			Assert.IsTrue(maxCapacity > 1);
			Assert.IsTrue((maxCapacity & (maxCapacity - 1)) == 0); // is power of 2
#endif

			_array = new T[maxCapacity];
			_start = 0;
			_mask = maxCapacity - 1;
			Count = 0;
		}

		public T this[int index]
		{
			get { return _array[GetIndex(index)]; }
			set { _array[GetIndex(index)] = value; }
		}

		/// <summary>
		/// Adds a new item to the front of the deque
		/// </summary>
		/// <param name="value">value to add</param>
		public void AddFirst(T value)
		{
			_array[GetIndex(_mask)] = value;
			_start = (_start + _mask) & _mask;
			Count = Math.Min(_array.Length, Count + 1);
		}

		/// <summary>
		/// Adds a new item to the end of the deque
		/// </summary>
		/// <param name="value">value to add</param>
		public void AddLast(T value)
		{
			_array[GetIndex(Count)] = value;
			if (Count < _array.Length)
				Count++;
			else
				_start = (_start + 1) & _mask;
		}

		/// <summary>
		/// Gets the first item in the deque
		/// </summary>
		/// <returns>first item</returns>
		public T GetFirst()
		{
			return _array[_start];
		}

		/// <summary>
		/// Gets the last item in the deque
		/// </summary>
		/// <returns>last item</returns>
		public T GetLast()
		{
			return _array[GetIndex(Count - 1)];
		}

		/// <summary>
		/// Removes the first item in the deque
		/// </summary>
		/// <returns>removed value</returns>
		public T RemoveFirst()
		{
			Assert.IsTrue(Count > 0);

			var value = _array[_start];
			Count--;
			_start = (_start + 1) & _mask;

			return value;
		}

		/// <summary>
		/// Removes the last item in the deque
		/// </summary>
		/// <returns>removed value</returns>
		public T RemoveLast()
		{
			Assert.IsTrue(Count > 0);

			Count--;
			var value = _array[GetIndex(Count)];

			return value;
		}

		/// <summary>
		/// Loops from the first item to the last item
		/// </summary>
		/// <returns>enumerator</returns>
		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0, j = _start; i < Count; i++, j = (j + 1) & _mask)
			{
				yield return _array[j];
			}
		}

		public IEnumerator<T> GetReverseEnumerator()
		{
			for (int i = Count - 1, j = GetIndex(Count - 1); i >= 0; i--, j = (j + _mask) & _mask)
			{
				yield return _array[j];
			}
		}

		/// <summary>
		/// Sets size to 0
		/// </summary>
		public void Clear()
		{
			_start = 0;
			Count = 0;
		}

		private int GetIndex(int index)
		{
			return (_start + index) & _mask;
		}

		public bool IsFull() => Count > _mask;
	}
}