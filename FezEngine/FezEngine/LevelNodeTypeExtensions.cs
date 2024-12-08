using System;

namespace FezEngine;

public static class LevelNodeTypeExtensions
{
	public static float GetSizeFactor(this LevelNodeType nodeType)
	{
		return nodeType switch
		{
			LevelNodeType.Hub => 2f, 
			LevelNodeType.Lesser => 0.5f, 
			LevelNodeType.Node => 1f, 
			_ => throw new InvalidOperationException(), 
		};
	}
}
