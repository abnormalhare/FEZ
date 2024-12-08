using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Common;
using Microsoft.Xna.Framework;

namespace FezEngine.Tools;

public static class RandomHelper
{
	public static Random Random
	{
		get
		{
			LocalDataStoreSlot namedDataSlot = Thread.GetNamedDataSlot("Random");
			object obj;
			if ((obj = Thread.GetData(namedDataSlot)) == null)
			{
				Thread.SetData(namedDataSlot, obj = new Random());
			}
			return obj as Random;
		}
	}

	public static bool Probability(double p)
	{
		return p > Random.NextDouble();
	}

	public static int Sign()
	{
		if (!Probability(0.5))
		{
			return 1;
		}
		return -1;
	}

	public static float Centered(double distance)
	{
		return (float)((Random.NextDouble() - 0.5) * distance * 2.0);
	}

	public static float Centered(double distance, double around)
	{
		return (float)((Random.NextDouble() - 0.5) * distance * 2.0 + around);
	}

	public static float Between(double min, double max)
	{
		return (float)(Random.NextDouble() * (max - min) + min);
	}

	public static float Unit()
	{
		return (float)Random.NextDouble();
	}

	public static T EnumField<T>(bool excludeFirst) where T : struct
	{
		IEnumerable<T> values = Util.GetValues<T>();
		return values.ElementAt(Random.Next(excludeFirst ? 1 : 0, values.Count()));
	}

	public static T EnumField<T>() where T : struct
	{
		return EnumField<T>(excludeFirst: false);
	}

	public static T InList<T>(T[] list)
	{
		return list[Random.Next(0, list.Length)];
	}

	public static T InList<T>(List<T> list)
	{
		return list[Random.Next(0, list.Count)];
	}

	public static T InList<T>(IEnumerable<T> list)
	{
		return list.ElementAt(Random.Next(0, list.Count()));
	}

	public static Vector3 NormalizedVector()
	{
		return Vector3.Normalize(new Vector3(Unit() * 2f - 1f, Unit() * 2f - 1f, Unit() * 2f - 1f));
	}
}
