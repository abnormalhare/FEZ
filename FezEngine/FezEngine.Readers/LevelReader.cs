using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class LevelReader : ContentTypeReader<Level>
{
	public static bool MinimalRead;

	protected override Level Read(ContentReader input, Level existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new Level();
		}
		existingInstance.Name = input.ReadObject<string>();
		existingInstance.Size = input.ReadVector3();
		existingInstance.StartingPosition = input.ReadObject(existingInstance.StartingPosition);
		existingInstance.SequenceSamplesPath = input.ReadObject<string>();
		existingInstance.Flat = input.ReadBoolean();
		existingInstance.SkipPostProcess = input.ReadBoolean();
		existingInstance.BaseDiffuse = input.ReadSingle();
		existingInstance.BaseAmbient = input.ReadSingle();
		existingInstance.GomezHaloName = input.ReadObject<string>();
		existingInstance.HaloFiltering = input.ReadBoolean();
		existingInstance.BlinkingAlpha = input.ReadBoolean();
		existingInstance.Loops = input.ReadBoolean();
		existingInstance.WaterType = input.ReadObject<LiquidType>();
		existingInstance.WaterHeight = input.ReadSingle();
		existingInstance.SkyName = input.ReadString();
		existingInstance.TrileSetName = input.ReadObject<string>();
		existingInstance.Volumes = input.ReadObject(existingInstance.Volumes);
		existingInstance.Scripts = input.ReadObject(existingInstance.Scripts);
		existingInstance.SongName = input.ReadObject<string>();
		existingInstance.FAPFadeOutStart = input.ReadInt32();
		existingInstance.FAPFadeOutLength = input.ReadInt32();
		if (!MinimalRead)
		{
			existingInstance.Triles = input.ReadObject(existingInstance.Triles);
			existingInstance.ArtObjects = input.ReadObject(existingInstance.ArtObjects);
			existingInstance.BackgroundPlanes = input.ReadObject(existingInstance.BackgroundPlanes);
			existingInstance.Groups = input.ReadObject(existingInstance.Groups);
			existingInstance.NonPlayerCharacters = input.ReadObject(existingInstance.NonPlayerCharacters);
			existingInstance.Paths = input.ReadObject(existingInstance.Paths);
			existingInstance.Descending = input.ReadBoolean();
			existingInstance.Rainy = input.ReadBoolean();
			existingInstance.LowPass = input.ReadBoolean();
			existingInstance.MutedLoops = input.ReadObject(existingInstance.MutedLoops);
			existingInstance.AmbienceTracks = input.ReadObject(existingInstance.AmbienceTracks);
			existingInstance.NodeType = input.ReadObject<LevelNodeType>();
			existingInstance.Quantum = input.ReadBoolean();
		}
		existingInstance.OnDeserialization();
		return existingInstance;
	}
}
