using System.Collections.Generic;

namespace FezEngine.Tools;

public class Pool<T> where T : class, new()
{
	private readonly Stack<T> stack;

	private int size;

	public int Size
	{
		get
		{
			return size;
		}
		set
		{
			int num = value - size;
			if (num > 0)
			{
				for (int i = 0; i < num; i++)
				{
					stack.Push(new T());
				}
			}
			size = value;
		}
	}

	public int Available => stack.Count;

	public Pool()
		: this(0)
	{
	}

	public Pool(int size)
	{
		stack = new Stack<T>(size);
		Size = size;
	}

	public T Take()
	{
		if (stack.Count <= 0)
		{
			return new T();
		}
		return stack.Pop();
	}

	public void Return(T item)
	{
		stack.Push(item);
	}
}
