using System;
using System.Collections.Generic;
using System.Linq;

namespace FezEngine.Structure;

public class IdentifierPool
{
	private int maximum;

	private readonly List<int> available = new List<int>();

	public int Take()
	{
		if (available.Count == 0)
		{
			available.Add(maximum++);
		}
		return available.First();
	}

	public void Return(int id)
	{
		available.Add(id);
	}

	public static int FirstAvailable<T>(IDictionary<int, T> values)
	{
		int num = -1;
		foreach (int key in values.Keys)
		{
			num = Math.Max(num, key);
		}
		return num + 1;
	}
}
