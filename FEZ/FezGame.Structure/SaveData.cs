using System;
using System.Collections.Generic;
using FezEngine;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Structure;

public class SaveData
{
	public bool IsNew;

	public long CreationTime;

	public long PlayTime;

	public long? SinceLastSaved;

	public bool CanNewGamePlus;

	public bool IsNewGamePlus;

	public bool Finished32;

	public bool Finished64;

	public bool HasFPView;

	public bool HasStereo3D;

	public bool HasDoneHeartReboot;

	public string Level;

	public Viewpoint View;

	public Vector3 Ground;

	public TimeSpan TimeOfDay = TimeSpan.FromHours(12.0);

	public List<string> UnlockedWarpDestinations = new List<string> { "NATURE_HUB" };

	public int Keys;

	public int CubeShards;

	public int SecretCubes;

	public int CollectedParts;

	public int CollectedOwls;

	public int PiecesOfHeart;

	public List<string> Maps = new List<string>();

	public List<ActorType> Artifacts = new List<ActorType>();

	public List<string> EarnedAchievements = new List<string>();

	public List<string> EarnedGamerPictures = new List<string>();

	public bool ScoreDirty;

	public string ScriptingState;

	public bool FezHidden;

	public float? GlobalWaterLevelModifier;

	public bool HasHadMapHelp;

	public bool CanOpenMap;

	public bool AchievementCheatCodeDone;

	public bool MapCheatCodeDone;

	public bool AnyCodeDeciphered;

	public Dictionary<string, LevelSaveData> World = new Dictionary<string, LevelSaveData>();

	public Dictionary<string, bool> OneTimeTutorials;

	public bool HasNewGamePlus
	{
		get
		{
			if (!Finished32)
			{
				return Finished64;
			}
			return true;
		}
	}

	public LevelSaveData ThisLevel
	{
		get
		{
			if (Level != null && World.TryGetValue(Level, out var value))
			{
				return value;
			}
			return LevelSaveData.Default;
		}
	}

	public SaveData()
	{
		CreationTime = DateTime.Now.ToFileTimeUtc();
		Clear();
	}

	public void Clear()
	{
		Level = null;
		View = Viewpoint.None;
		CanNewGamePlus = false;
		IsNewGamePlus = false;
		Finished32 = (Finished64 = false);
		HasFPView = false;
		HasDoneHeartReboot = false;
		Ground = Vector3.Zero;
		TimeOfDay = TimeSpan.FromHours(12.0);
		UnlockedWarpDestinations = new List<string> { "NATURE_HUB" };
		SecretCubes = (CubeShards = (Keys = 0));
		CollectedParts = (CollectedOwls = 0);
		PiecesOfHeart = 0;
		Maps = new List<string>();
		Artifacts = new List<ActorType>();
		ScoreDirty = false;
		ScriptingState = null;
		FezHidden = false;
		GlobalWaterLevelModifier = null;
		HasHadMapHelp = (CanOpenMap = false);
		MapCheatCodeDone = (AchievementCheatCodeDone = false);
		ScoreDirty = false;
		World = new Dictionary<string, LevelSaveData>();
		OneTimeTutorials = new Dictionary<string, bool>
		{
			{ "DOT_LOCKED_DOOR_A", false },
			{ "DOT_NUT_N_BOLT_A", false },
			{ "DOT_PIVOT_A", false },
			{ "DOT_TIME_SWITCH_A", false },
			{ "DOT_TOMBSTONE_A", false },
			{ "DOT_TREASURE", false },
			{ "DOT_VALVE_A", false },
			{ "DOT_WEIGHT_SWITCH_A", false },
			{ "DOT_LESSER_A", false },
			{ "DOT_WARP_A", false },
			{ "DOT_BOMB_A", false },
			{ "DOT_CLOCK_A", false },
			{ "DOT_CRATE_A", false },
			{ "DOT_TELESCOPE_A", false },
			{ "DOT_WELL_A", false },
			{ "DOT_WORKING", false }
		};
		IsNew = true;
	}

	public void CloneInto(SaveData d)
	{
		d.AchievementCheatCodeDone = AchievementCheatCodeDone;
		d.AnyCodeDeciphered = AnyCodeDeciphered;
		d.CanNewGamePlus = CanNewGamePlus;
		d.CanOpenMap = CanOpenMap;
		d.CollectedOwls = CollectedOwls;
		d.CollectedParts = CollectedParts;
		d.CreationTime = CreationTime;
		d.CubeShards = CubeShards;
		d.FezHidden = FezHidden;
		d.Finished32 = Finished32;
		d.Finished64 = Finished64;
		d.GlobalWaterLevelModifier = GlobalWaterLevelModifier;
		d.Ground = Ground;
		d.HasDoneHeartReboot = HasDoneHeartReboot;
		d.HasFPView = HasFPView;
		d.HasHadMapHelp = HasHadMapHelp;
		d.HasStereo3D = HasStereo3D;
		d.IsNew = IsNew;
		d.IsNewGamePlus = IsNewGamePlus;
		d.Keys = Keys;
		d.Level = Level;
		d.MapCheatCodeDone = MapCheatCodeDone;
		d.PiecesOfHeart = PiecesOfHeart;
		d.PlayTime = PlayTime;
		d.ScoreDirty = ScoreDirty;
		d.ScriptingState = ScriptingState;
		d.SecretCubes = SecretCubes;
		d.SinceLastSaved = SinceLastSaved;
		d.TimeOfDay = TimeOfDay;
		d.View = View;
		try
		{
			d.Artifacts.Clear();
			d.Artifacts.AddRange(Artifacts);
			d.EarnedAchievements.Clear();
			d.EarnedAchievements.AddRange(EarnedAchievements);
			d.EarnedGamerPictures.Clear();
			d.EarnedGamerPictures.AddRange(EarnedGamerPictures);
			d.Maps.Clear();
			d.Maps.AddRange(Maps);
			d.UnlockedWarpDestinations.Clear();
			d.UnlockedWarpDestinations.AddRange(UnlockedWarpDestinations);
			d.OneTimeTutorials.Clear();
			foreach (string key in OneTimeTutorials.Keys)
			{
				d.OneTimeTutorials.Add(key, OneTimeTutorials[key]);
			}
			foreach (string key2 in World.Keys)
			{
				if (!d.World.ContainsKey(key2))
				{
					d.World.Add(key2, new LevelSaveData());
				}
				World[key2].CloneInto(d.World[key2]);
			}
			foreach (string key3 in d.World.Keys)
			{
				if (!World.ContainsKey(key3))
				{
					d.World.Remove(key3);
				}
			}
		}
		catch (InvalidOperationException)
		{
			CloneInto(d);
		}
	}
}
