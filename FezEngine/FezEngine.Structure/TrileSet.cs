using System;
using System.Collections.Generic;
using ContentSerialization;
using ContentSerialization.Attributes;
using FezEngine.Effects.Structures;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure;

public class TrileSet : IDeserializationCallback, IDisposable
{
	public string Name { get; set; }

	public Dictionary<int, Trile> Triles { get; set; }

	[Serialization(Ignore = true)]
	public Texture2D TextureAtlas { get; set; }

	public Trile this[int id]
	{
		get
		{
			return Triles[id];
		}
		set
		{
			Triles[id] = value;
		}
	}

	public TrileSet()
	{
		Triles = new Dictionary<int, Trile>();
	}

	public void OnDeserialization()
	{
		foreach (int key in Triles.Keys)
		{
			Triles[key].TrileSet = this;
			Triles[key].Id = key;
		}
	}

	public void Dispose()
	{
		if (TextureAtlas != null)
		{
			TextureAtlas.Unhook();
			TextureAtlas.Dispose();
		}
		foreach (Trile value in Triles.Values)
		{
			value.Dispose();
		}
	}
}
