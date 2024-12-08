using System.Collections.Generic;
using ContentSerialization;
using ContentSerialization.Attributes;
using FezEngine.Structure.Scripting;
using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

public class Level : IDeserializationCallback
{
	[Serialization(CollectionItemName = "Trile")]
	public Dictionary<TrileEmplacement, TrileInstance> Triles;

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool Flat { get; set; }

	public string Name { get; set; }

	public TrileFace StartingPosition { get; set; }

	public Vector3 Size { get; set; }

	[Serialization(Optional = true)]
	public string SequenceSamplesPath { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool SkipPostProcess { get; set; }

	[Serialization(Optional = true)]
	public float BaseDiffuse { get; set; }

	[Serialization(Optional = true)]
	public float BaseAmbient { get; set; }

	[Serialization(Optional = true)]
	public string GomezHaloName { get; set; }

	[Serialization(Optional = true)]
	public bool HaloFiltering { get; set; }

	[Serialization(Optional = true)]
	public bool BlinkingAlpha { get; set; }

	[Serialization(Optional = true)]
	public bool Loops { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public LiquidType WaterType { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public float WaterHeight { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool Descending { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool Rainy { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool LowPass { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public LevelNodeType NodeType { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public int FAPFadeOutStart { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public int FAPFadeOutLength { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool Quantum { get; set; }

	public string SkyName { get; set; }

	public string TrileSetName { get; set; }

	[Serialization(Optional = true)]
	public string SongName { get; set; }

	[Serialization(Ignore = true)]
	public Sky Sky { get; set; }

	[Serialization(Ignore = true)]
	public TrileSet TrileSet { get; set; }

	[Serialization(Ignore = true)]
	public TrackedSong Song { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public Dictionary<int, Volume> Volumes { get; set; }

	[Serialization(CollectionItemName = "Object", Optional = true, DefaultValueOptional = true)]
	public Dictionary<int, ArtObjectInstance> ArtObjects { get; set; }

	[Serialization(CollectionItemName = "Plane", Optional = true, DefaultValueOptional = true)]
	public Dictionary<int, BackgroundPlane> BackgroundPlanes { get; set; }

	[Serialization(CollectionItemName = "Script", Optional = true, DefaultValueOptional = true)]
	public Dictionary<int, Script> Scripts { get; set; }

	[Serialization(CollectionItemName = "Group", Optional = true, DefaultValueOptional = true)]
	public Dictionary<int, TrileGroup> Groups { get; set; }

	[Serialization(CollectionItemName = "Npc", Optional = true, DefaultValueOptional = true)]
	public Dictionary<int, NpcInstance> NonPlayerCharacters { get; set; }

	[Serialization(CollectionItemName = "Path", Optional = true, DefaultValueOptional = true)]
	public Dictionary<int, MovementPath> Paths { get; set; }

	[Serialization(CollectionItemName = "Loop", Optional = true, DefaultValueOptional = true)]
	public List<string> MutedLoops { get; set; }

	[Serialization(CollectionItemName = "Track", Optional = true, DefaultValueOptional = true)]
	public List<AmbienceTrack> AmbienceTracks { get; set; }

	public Level()
	{
		Triles = new Dictionary<TrileEmplacement, TrileInstance>();
		Volumes = new Dictionary<int, Volume>();
		ArtObjects = new Dictionary<int, ArtObjectInstance>();
		BackgroundPlanes = new Dictionary<int, BackgroundPlane>();
		Groups = new Dictionary<int, TrileGroup>();
		Scripts = new Dictionary<int, Script>();
		NonPlayerCharacters = new Dictionary<int, NpcInstance>();
		Paths = new Dictionary<int, MovementPath>();
		MutedLoops = new List<string>();
		AmbienceTracks = new List<AmbienceTrack>();
		BaseDiffuse = 1f;
		BaseAmbient = 0.35f;
		HaloFiltering = true;
	}

	public void OnDeserialization()
	{
		foreach (TrileEmplacement key2 in Triles.Keys)
		{
			TrileInstance trileInstance = Triles[key2];
			if (Triles[key2].Emplacement != key2)
			{
				Triles[key2].Emplacement = key2;
			}
			trileInstance.Update();
			trileInstance.OriginalEmplacement = key2;
			if (!trileInstance.Overlaps)
			{
				continue;
			}
			foreach (TrileInstance overlappedTrile in trileInstance.OverlappedTriles)
			{
				overlappedTrile.OriginalEmplacement = key2;
			}
		}
		foreach (int key3 in Scripts.Keys)
		{
			Scripts[key3].Id = key3;
		}
		foreach (int key4 in Volumes.Keys)
		{
			Volumes[key4].Id = key4;
		}
		foreach (int key5 in NonPlayerCharacters.Keys)
		{
			NonPlayerCharacters[key5].Id = key5;
		}
		foreach (int key6 in ArtObjects.Keys)
		{
			ArtObjects[key6].Id = key6;
		}
		foreach (int key7 in BackgroundPlanes.Keys)
		{
			BackgroundPlanes[key7].Id = key7;
		}
		foreach (int key8 in Paths.Keys)
		{
			Paths[key8].Id = key8;
		}
		foreach (int key9 in Groups.Keys)
		{
			TrileGroup trileGroup = Groups[key9];
			trileGroup.Id = key9;
			TrileEmplacement[] array = new TrileEmplacement[trileGroup.Triles.Count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = trileGroup.Triles[i].Emplacement;
			}
			trileGroup.Triles.Clear();
			TrileEmplacement[] array2 = array;
			foreach (TrileEmplacement key in array2)
			{
				trileGroup.Triles.Add(Triles[key]);
			}
		}
	}
}
