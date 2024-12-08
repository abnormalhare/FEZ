using System.Collections.Generic;
using ContentSerialization;

namespace FezEngine.Structure;

public class MapTree
{
	public class TreeFillContext
	{
		public string ContentRoot;

		public readonly Dictionary<string, MapNode> LoadedNodes = new Dictionary<string, MapNode>();

		public readonly Dictionary<string, TrileSet> TrileSetCache = new Dictionary<string, TrileSet>();
	}

	private const string Hub = "NATURE_HUB";

	public MapNode Root;

	public void Fill(string contentRoot)
	{
		TreeFillContext treeFillContext = new TreeFillContext
		{
			ContentRoot = contentRoot
		};
		Root = new MapNode
		{
			LevelName = "NATURE_HUB"
		};
		treeFillContext.LoadedNodes.Add("NATURE_HUB", Root);
		Root.Fill(treeFillContext, null, FaceOrientation.Front);
		SdlSerializer.Serialize(contentRoot + "\\MapTree.map.sdl", this);
	}

	public MapTree Clone()
	{
		return new MapTree
		{
			Root = Root.Clone()
		};
	}
}
