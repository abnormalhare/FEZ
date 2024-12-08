using System.Collections.Generic;
using FezEngine.Structure;

namespace FezEngine.Effects;

public class NodeGroupData
{
	public MapNode Node;

	public int HighlightInstance;

	public List<int> IconInstances = new List<int>();

	public string LevelName;

	public float Depth;

	public bool Complete;
}
