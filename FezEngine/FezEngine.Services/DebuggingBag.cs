using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace FezEngine.Services;

public class DebuggingBag : IDebuggingBag
{
	private class DebuggingLine
	{
		private readonly object value;

		private DateTime lastUpdateTime;

		public object Value => value;

		public bool Expired => DateTime.Now - lastUpdateTime >= ExpirationTime;

		public float Age => MathHelper.Clamp((float)(DateTime.Now - lastUpdateTime).Ticks / (float)ExpirationTime.Ticks, 0f, 1f);

		public DebuggingLine(object value)
		{
			this.value = value;
			lastUpdateTime = DateTime.Now;
		}

		public void Refresh()
		{
			lastUpdateTime = DateTime.Now;
		}
	}

	private readonly Dictionary<string, DebuggingLine> items;

	private static readonly TimeSpan ExpirationTime = new TimeSpan(0, 0, 5);

	public object this[string name]
	{
		get
		{
			if (items.ContainsKey(name))
			{
				return items[name].Value;
			}
			return null;
		}
	}

	public IEnumerable<string> Keys => items.Keys.OrderBy((string x) => x);

	public DebuggingBag()
	{
		items = new Dictionary<string, DebuggingLine>();
	}

	public void Add(string name, object item)
	{
	}

	public void Empty()
	{
		List<string> list = new List<string>();
		foreach (string key in items.Keys)
		{
			if (items[key].Expired)
			{
				list.Add(key);
			}
		}
		foreach (string item in list)
		{
			items.Remove(item);
		}
	}

	public float GetAge(string name)
	{
		if (items.ContainsKey(name))
		{
			return items[name].Age;
		}
		return 0f;
	}
}
