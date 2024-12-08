using System;
using System.Collections.Generic;
using System.Linq;
using ContentSerialization;
using ContentSerialization.Attributes;
using FezEngine.Structure.Scripting;
using FezEngine.Tools;

namespace FezEngine.Structure;

public class MapNode
{
	public class Connection
	{
		public FaceOrientation Face;

		public MapNode Node;

		[Serialization(Optional = true, DefaultValueOptional = true)]
		public float BranchOversize;

		[Serialization(Ignore = true)]
		public int MultiBranchId;

		[Serialization(Ignore = true)]
		public int MultiBranchCount;

		[Serialization(Ignore = true)]
		public List<int> LinkInstances;
	}

	private static readonly string[] UpLevels = new string[7] { "TREE_ROOTS", "TREE", "TREE_SKY", "FOX", "WATER_TOWER", "PIVOT_WATERTOWER", "VILLAGEVILLE_3D" };

	private static readonly string[] DownLevels = new string[5] { "SEWER_START", "MEMORY_CORE", "ZU_FORK", "STARGATE", "QUANTUM" };

	private static readonly string[] OppositeLevels = new string[15]
	{
		"NUZU_SCHOOL", "NUZU_ABANDONED_A", "ZU_HOUSE_EMPTY_B", "PURPLE_LODGE", "ZU_HOUSE_SCAFFOLDING", "MINE_BOMB_PILLAR", "CMY_B", "INDUSTRIAL_HUB", "SUPERSPIN_CAVE", "GRAVE_LESSER_GATE",
		"THRONE", "VISITOR", "ORRERY", "LAVA_SKULL", "LAVA_FORK"
	};

	private static readonly string[] BackLevels = new string[2] { "ABANDONED_B", "LAVA" };

	private static readonly string[] LeftLevels = new string[0];

	private static readonly string[] FrontLevels = new string[2] { "VILLAGEVILLE_3D", "ZU_LIBRARY" };

	private static readonly string[] RightLevels = new string[5] { "WALL_SCHOOL", "WALL_KITCHEN", "WALL_INTERIOR_HOLE", "WALL_INTERIOR_B", "WALL_INTERIOR_A" };

	private static readonly string[] PuzzleLevels = new string[5] { "ZU_ZUISH", "ZU_UNFOLD", "BELL_TOWER", "CLOCK", "ZU_TETRIS" };

	private static readonly Dictionary<string, float> OversizeLinks = new Dictionary<string, float>
	{
		{ "SEWER_START", 5.5f },
		{ "TREE", 1.25f },
		{ "TREE_SKY", 1f },
		{ "INDUSTRIAL_HUB", 0.5f },
		{ "VILLAGEVILLE_3D", -0.5f },
		{ "WALL_VILLAGE", 0.5f },
		{ "ZU_CITY", 0.5f },
		{ "INDUSTRIAL_CITY", 0.5f },
		{ "MEMORY_CORE", 0.5f },
		{ "BIG_TOWER", 0.5f },
		{ "STARGATE", -0.5f },
		{ "WATERFALL", 0.25f },
		{ "BELL_TOWER", 0.25f },
		{ "LIGHTHOUSE", 0.25f },
		{ "ARCH", 0.25f }
	};

	public string LevelName;

	public LevelNodeType NodeType;

	[Serialization(Optional = true)]
	public WinConditions Conditions;

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool HasLesserGate;

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool HasWarpGate;

	public List<Connection> Connections = new List<Connection>();

	[Serialization(Ignore = true)]
	public bool Valid;

	[Serialization(Ignore = true)]
	public Group Group;

	public MapNode()
	{
		Conditions = new WinConditions();
	}

	public void Fill(MapTree.TreeFillContext context, MapNode parent, FaceOrientation origin)
	{
		if (Valid)
		{
			return;
		}
		TrileSet trileSet = null;
		Level level;
		try
		{
			level = SdlSerializer.Deserialize<Level>(context.ContentRoot + "\\Levels\\" + LevelName + ".lvl.sdl");
			if (level.TrileSetName != null && !context.TrileSetCache.TryGetValue(level.TrileSetName, out trileSet))
			{
				context.TrileSetCache.Add(level.TrileSetName, trileSet = SdlSerializer.Deserialize<TrileSet>(context.ContentRoot + "\\Trile Sets\\" + level.TrileSetName + ".ts.sdl"));
			}
		}
		catch (Exception)
		{
			Console.WriteLine("Warning : Level " + LevelName + " could not be loaded because it has invalid markup. Skipping...");
			Valid = false;
			return;
		}
		NodeType = level.NodeType;
		Conditions.ChestCount = level.ArtObjects.Values.Count((ArtObjectInstance x) => x.ArtObjectName.IndexOf("treasure", StringComparison.InvariantCultureIgnoreCase) != -1) / 2;
		Conditions.ScriptIds = (from x in level.Scripts.Values
			where x.IsWinCondition
			select x.Id).ToList();
		Conditions.SplitUpCount = level.Triles.Values.Union(level.Triles.Values.Where((TrileInstance x) => x.Overlaps).SelectMany((TrileInstance x) => x.OverlappedTriles)).Count((TrileInstance x) => x.TrileId >= 0 && trileSet[x.TrileId].ActorSettings.Type == ActorType.GoldenCube);
		Conditions.CubeShardCount = level.Triles.Values.Count((TrileInstance x) => x.TrileId >= 0 && trileSet[x.TrileId].ActorSettings.Type == ActorType.CubeShard);
		Conditions.OtherCollectibleCount = level.Triles.Values.Count((TrileInstance x) => x.TrileId >= 0 && trileSet[x.TrileId].ActorSettings.Type.IsTreasure() && trileSet[x.TrileId].ActorSettings.Type != ActorType.CubeShard) + level.ArtObjects.Values.Count((ArtObjectInstance x) => x.ArtObjectName == "treasure_mapAO");
		Conditions.LockedDoorCount = level.Triles.Values.Count((TrileInstance x) => x.TrileId >= 0 && trileSet[x.TrileId].ActorSettings.Type == ActorType.Door);
		Conditions.UnlockedDoorCount = level.Triles.Values.Count((TrileInstance x) => x.TrileId >= 0 && trileSet[x.TrileId].ActorSettings.Type == ActorType.UnlockedDoor);
		int num = level.ArtObjects.Count((KeyValuePair<int, ArtObjectInstance> x) => x.Value.ArtObjectName.IndexOf("fork", StringComparison.InvariantCultureIgnoreCase) != -1);
		int num2 = level.ArtObjects.Count((KeyValuePair<int, ArtObjectInstance> x) => x.Value.ArtObjectName.IndexOf("qr", StringComparison.InvariantCultureIgnoreCase) != -1);
		int num3 = level.Volumes.Count((KeyValuePair<int, Volume> x) => x.Value.ActorSettings != null && x.Value.ActorSettings.CodePattern != null && x.Value.ActorSettings.CodePattern.Length != 0);
		int num4 = ((!(LevelName == "OWL")) ? level.NonPlayerCharacters.Count((KeyValuePair<int, NpcInstance> x) => x.Value.Name == "Owl") : 0);
		int num5 = level.ArtObjects.Count((KeyValuePair<int, ArtObjectInstance> x) => x.Value.ArtObjectName.Contains("BIT_DOOR") && !x.Value.ArtObjectName.Contains("BROKEN"));
		int num6 = level.Scripts.Values.Count((Script s) => s.Actions.Any((ScriptAction a) => a.Object.Type == "Level" && a.Operation == "ResolvePuzzle"));
		int num7 = (PuzzleLevels.Contains(LevelName) ? ((!(LevelName == "CLOCK")) ? 1 : 4) : 0);
		Conditions.SecretCount = num + num2 + num3 + num4 + num6 + num7 + num5;
		HasLesserGate = level.ArtObjects.Values.Any((ArtObjectInstance x) => x.ArtObjectName.IndexOf("lesser_gate", StringComparison.InvariantCultureIgnoreCase) != -1 && x.ArtObjectName.IndexOf("base", StringComparison.InvariantCultureIgnoreCase) == -1);
		HasWarpGate = level.ArtObjects.Values.Any((ArtObjectInstance x) => x.ArtObjectName == "GATE_GRAVEAO" || x.ArtObjectName == "GATEAO" || x.ArtObjectName == "GATE_INDUSTRIALAO" || x.ArtObjectName == "GATE_SEWERAO" || x.ArtObjectName == "ZU_GATEAO" || x.ArtObjectName == "GRAVE_GATEAO");
		foreach (Script value5 in level.Scripts.Values)
		{
			foreach (ScriptAction action in value5.Actions)
			{
				if (!(action.Object.Type == "Level") || !action.Operation.Contains("Level"))
				{
					continue;
				}
				Connection connection = new Connection();
				bool flag = true;
				foreach (ScriptTrigger trigger in value5.Triggers)
				{
					if (trigger.Object.Type == "Volume" && trigger.Event == "Enter" && trigger.Object.Identifier.HasValue)
					{
						int value = trigger.Object.Identifier.Value;
						if (!level.Volumes.TryGetValue(value, out var value2))
						{
							Console.WriteLine("Warning : A level-changing script links to a nonexistent volume in " + LevelName + " (Volume Id #" + value + ")");
							flag = false;
						}
						else if (value2.ActorSettings != null && value2.ActorSettings.IsSecretPassage)
						{
							flag = false;
						}
						else
						{
							connection.Face = value2.Orientations.First();
						}
						break;
					}
				}
				if (!flag)
				{
					continue;
				}
				string text = ((action.Operation == "ReturnToLastLevel") ? parent.LevelName : action.Arguments[0]);
				switch (text)
				{
				case "THRONE":
					if (LevelName == "ZU_CITY_RUINS")
					{
						continue;
					}
					break;
				case "PYRAMID":
				case "CABIN_INTERIOR_A":
					continue;
				}
				if (text == "ZU_CITY_RUINS" && LevelName == "THRONE")
				{
					continue;
				}
				if (context.LoadedNodes.TryGetValue(text, out var value3))
				{
					break;
				}
				value3 = new MapNode
				{
					LevelName = text
				};
				context.LoadedNodes.Add(text, value3);
				connection.Node = value3;
				if (connection.Node != parent)
				{
					if (parent != null && origin == connection.Face)
					{
						connection.Face = origin.GetOpposite();
					}
					if (UpLevels.Contains(text))
					{
						connection.Face = FaceOrientation.Top;
					}
					else if (DownLevels.Contains(text))
					{
						connection.Face = FaceOrientation.Down;
					}
					else if (OppositeLevels.Contains(text))
					{
						connection.Face = connection.Face.GetOpposite();
					}
					else if (BackLevels.Contains(text))
					{
						connection.Face = FaceOrientation.Back;
					}
					else if (LeftLevels.Contains(text))
					{
						connection.Face = FaceOrientation.Left;
					}
					else if (RightLevels.Contains(text))
					{
						connection.Face = FaceOrientation.Right;
					}
					else if (FrontLevels.Contains(text))
					{
						connection.Face = FaceOrientation.Front;
					}
					if (OversizeLinks.TryGetValue(text, out var value4))
					{
						connection.BranchOversize = value4;
					}
					Connections.Add(connection);
				}
				break;
			}
		}
		Valid = true;
		foreach (Connection connection2 in Connections)
		{
			connection2.Node.Fill(context, this, connection2.Face);
		}
	}

	public MapNode Clone()
	{
		return new MapNode
		{
			LevelName = LevelName,
			NodeType = NodeType,
			HasWarpGate = HasWarpGate,
			HasLesserGate = HasLesserGate,
			Connections = Connections.Select((Connection x) => new Connection
			{
				Face = x.Face,
				Node = x.Node.Clone(),
				BranchOversize = x.BranchOversize
			}).ToList(),
			Conditions = Conditions.Clone()
		};
	}
}
