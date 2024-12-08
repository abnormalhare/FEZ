using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using FezEngine;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;
using FezEngine.Tools;
using FezGame.Components;
using FezGame.Components.Scripting;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Services;

public class GameLevelManager : LevelManager, IGameLevelManager, ILevelManager
{
	private readonly List<string> DotLoadLevels = new List<string>
	{
		"MEMORY_CORE+NATURE_HUB", "NATURE_HUB+MEMORY_CORE", "MEMORY_CORE+ZU_CITY", "ZU_CITY+MEMORY_CORE", "MEMORY_CORE+WALL_VILLAGE", "WALL_VILLAGE+MEMORY_CORE", "MEMORY_CORE+INDUSTRIAL_CITY", "INDUSTRIAL_CITY+MEMORY_CORE", "PIVOT_WATERTOWER+INDUSTRIAL_HUB", "INDUSTRIAL_HUB+PIVOT_WATERTOWER",
		"WELL_2+SEWER_START", "SEWER_START+WELL_2", "GRAVE_CABIN+GRAVEYARD_GATE", "GRAVEYARD_GATE+GRAVE_CABIN", "TREE+TREE_SKY", "TREE_SKY+TREE", "WATERFALL+MINE_A", "MINE_A+WATERFALL", "SEWER_TO_LAVA+LAVA", "LAVA+SEWER_TO_LAVA"
	};

	private Level oldLevel;

	private readonly Dictionary<TrileInstance, TrileGroup> pickupGroups = new Dictionary<TrileInstance, TrileGroup>();

	public bool SongChanged { get; set; }

	public string LastLevelName { get; set; }

	public bool DestinationIsFarAway { get; set; }

	public int? DestinationVolumeId { get; set; }

	public bool WentThroughSecretPassage { get; set; }

	public IDictionary<TrileInstance, TrileGroup> PickupGroups => pickupGroups;

	[ServiceDependency]
	public IDotManager DotManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IPhysicsManager PhysicsManager { private get; set; }

	[ServiceDependency]
	public new IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public ICollisionManager CollisionManager { private get; set; }

	[ServiceDependency]
	public ITimeManager TimeManager { private get; set; }

	[ServiceDependency]
	public IGameService GameService { private get; set; }

	[ServiceDependency]
	public ILevelService LevelService { private get; set; }

	public GameLevelManager(Game game)
		: base(game)
	{
	}

	public override void Load(string levelName)
	{
		levelName = levelName.Replace('\\', '/');
		string text = levelName;
		Level level;
		using (MemoryContentManager memoryContentManager = new MemoryContentManager(base.Game.Services, base.Game.Content.RootDirectory))
		{
			if (!string.IsNullOrEmpty(base.Name))
			{
				levelName = base.Name.Substring(0, base.Name.LastIndexOf("/") + 1) + levelName.Substring(levelName.LastIndexOf("/") + 1);
			}
			if (!MemoryContentManager.AssetExists("Levels\\" + levelName.Replace('/', '\\')))
			{
				levelName = text;
			}
			try
			{
				level = memoryContentManager.Load<Level>("Levels/" + levelName);
			}
			catch (Exception e)
			{
				Logger.LogError(e);
				oldLevel = new Level();
				return;
			}
		}
		level.Name = levelName;
		ContentManager forLevel = base.CMProvider.GetForLevel(levelName);
		foreach (ArtObjectInstance value in level.ArtObjects.Values)
		{
			value.ArtObject = forLevel.Load<ArtObject>(string.Format("{0}/{1}", "Art Objects", value.ArtObjectName));
		}
		if (level.Sky == null)
		{
			level.Sky = forLevel.Load<Sky>("Skies/" + level.SkyName);
		}
		if (level.TrileSetName != null)
		{
			level.TrileSet = forLevel.Load<TrileSet>("Trile Sets/" + level.TrileSetName);
		}
		if (level.SongName != null)
		{
			level.Song = forLevel.Load<TrackedSong>("Music/" + level.SongName);
			level.Song.Initialize();
		}
		if (levelData != null)
		{
			GameState.SaveData.ThisLevel.FirstVisit = false;
		}
		ClearArtSatellites();
		oldLevel = levelData ?? new Level();
		levelData = level;
	}

	public override void Rebuild()
	{
		OnSkyChanged();
		base.LevelMaterializer.ClearBatches();
		base.LevelMaterializer.RebuildTriles(levelData.TrileSet, levelData.TrileSet == oldLevel.TrileSet);
		base.LevelMaterializer.RebuildInstances();
		if (!base.Quantum)
		{
			base.LevelMaterializer.CleanUp();
		}
		base.LevelMaterializer.InitializeArtObjects();
		foreach (BackgroundPlane value in levelData.BackgroundPlanes.Values)
		{
			value.HostMesh = (value.Animated ? base.LevelMaterializer.AnimatedPlanesMesh : base.LevelMaterializer.StaticPlanesMesh);
			value.Initialize();
		}
		lock (levelData.BackgroundPlanes)
		{
			if (!levelData.BackgroundPlanes.ContainsKey(-1) && base.GomezHaloName != null)
			{
				levelData.BackgroundPlanes.Add(-1, new BackgroundPlane(base.LevelMaterializer.StaticPlanesMesh, base.GomezHaloName, animated: false)
				{
					Id = -1,
					LightMap = true,
					AlwaysOnTop = true,
					Billboard = true,
					Filter = (base.HaloFiltering ? new Color(0.425f, 0.425f, 0.425f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f)),
					PixelatedLightmap = !base.HaloFiltering
				});
			}
		}
		pickupGroups.Clear();
		foreach (TrileGroup value2 in base.Groups.Values)
		{
			if (value2.ActorType == ActorType.SuckBlock || !value2.Triles.All((TrileInstance x) => x.Trile.ActorSettings.Type.IsPickable() && x.Trile.ActorSettings.Type != ActorType.Couch))
			{
				continue;
			}
			foreach (TrileInstance trile in value2.Triles)
			{
				pickupGroups.Add(trile, value2);
			}
		}
		SongChanged = base.Song == null != (SoundManager.CurrentlyPlayingSong == null) || (base.Song != null && SoundManager.CurrentlyPlayingSong != null && base.Song.Name != SoundManager.CurrentlyPlayingSong.Name);
		if (SongChanged)
		{
			SoundManager.ScriptChangedSong = false;
			if (!GameState.InCutscene || GameState.IsTrialMode)
			{
				Waiters.Wait(() => !GameState.Loading && !GameState.FarawaySettings.InTransition, delegate
				{
					if (!SoundManager.ScriptChangedSong)
					{
						SoundManager.PlayNewSong(8f);
					}
					SoundManager.ScriptChangedSong = false;
				});
			}
		}
		else if (base.Song != null)
		{
			if (!GameState.DotLoading)
			{
				SoundManager.UpdateSongActiveTracks();
			}
			else
			{
				Waiters.Wait(() => !GameState.Loading, delegate
				{
					SoundManager.UpdateSongActiveTracks();
				});
			}
		}
		SoundManager.FadeFrequencies(base.LowPass, 2f);
		SoundManager.UnmuteAmbienceTracks();
		if (!GameState.InCutscene || GameState.IsTrialMode)
		{
			Waiters.Wait(() => !GameState.Loading && !GameState.FarawaySettings.InTransition, delegate
			{
				SoundManager.PlayNewAmbience();
			});
		}
		oldLevel = null;
		base.FullPath = base.Name;
	}

	public void ChangeLevel(string levelName)
	{
		GameState.SaveToCloud();
		GameState.DotLoading = DotLoadLevels.Contains(base.Name + "+" + levelName) || PlayerManager.Action == ActionType.LesserWarp || PlayerManager.Action == ActionType.GateWarp;
		if (GameState.DotLoading)
		{
			SoundManager.PlayNewSong(null, 1f);
			List<AmbienceTrack> ambienceTracks = levelData.AmbienceTracks;
			levelData.AmbienceTracks = new List<AmbienceTrack>();
			SoundManager.PlayNewAmbience();
			levelData.AmbienceTracks = ambienceTracks;
		}
		GameService.CloseScroll(null);
		if (levelName == base.Name && DestinationVolumeId.HasValue && base.Volumes.ContainsKey(DestinationVolumeId.Value))
		{
			LastLevelName = base.Name;
			Volume volume = base.Volumes[DestinationVolumeId.Value];
			Viewpoint viewpoint = volume.Orientations.FirstOrDefault().AsViewpoint();
			CameraManager.ChangeViewpoint(viewpoint, 1.5f);
			Vector3 position = (volume.BoundingBox.Min + volume.BoundingBox.Max) / 2f + new Vector3(0.001f);
			position.Y = volume.BoundingBox.Min.Y - 0.25f;
			TrileInstance deep = NearestTrile(position, QueryOptions.None, viewpoint).Deep;
			GameState.SaveData.Ground = deep.Center;
			GameState.SaveData.View = viewpoint;
			float y = PlayerManager.Position.Y;
			PlayerManager.Position = deep.Center + (deep.TransformedSize / 2f + PlayerManager.Size / 2f) * Vector3.UnitY * Math.Sign(CollisionManager.GravityFactor);
			PlayerManager.WallCollision = default(MultipleHits<CollisionResult>);
			PlayerManager.Ground = default(MultipleHits<TrileInstance>);
			PlayerManager.Velocity = 3.15f * (float)Math.Sign(CollisionManager.GravityFactor) * 0.15f * (1f / 60f) * -Vector3.UnitY;
			PhysicsManager.Update(PlayerManager);
			PlayerManager.Velocity = 3.15f * (float)Math.Sign(CollisionManager.GravityFactor) * 0.15f * (1f / 60f) * -Vector3.UnitY;
			Vector3 originalCenter = CameraManager.Center;
			float diff = PlayerManager.Position.Y - y;
			Waiters.Interpolate(1.5, delegate(float s)
			{
				CameraManager.Center = new Vector3(originalCenter.X, originalCenter.Y + diff / 2f * Easing.EaseInOut(s, EasingType.Sine), originalCenter.Z);
			});
			OnLevelChanging();
			OnLevelChanged();
			return;
		}
		bool flag = GameState.SaveData.World.Count > 0;
		string level = GameState.SaveData.Level;
		if (flag)
		{
			LastLevelName = base.Name;
		}
		else
		{
			LastLevelName = null;
		}
		Load(levelName);
		Rebuild();
		if (!GameState.SaveData.World.ContainsKey(base.Name))
		{
			GameState.SaveData.World.Add(base.Name, new LevelSaveData
			{
				FirstVisit = true
			});
		}
		GameState.SaveData.Level = base.Name;
		OnLevelChanging();
		LevelSaveData thisLevel = GameState.SaveData.ThisLevel;
		foreach (TrileEmplacement destroyedTrile in thisLevel.DestroyedTriles)
		{
			ClearTrile(destroyedTrile);
		}
		foreach (int inactiveArtObject in thisLevel.InactiveArtObjects)
		{
			if (inactiveArtObject < 0)
			{
				RemoveArtObject(base.ArtObjects[-(inactiveArtObject + 1)]);
			}
		}
		TrileInstance trileInstance = ((flag && level == levelName) ? ActualInstanceAt(GameState.SaveData.Ground) : null);
		float? num = null;
		Viewpoint spawnView = ((flag && level == levelName) ? GameState.SaveData.View : Viewpoint.Left);
		bool flag2 = false;
		if (LastLevelName != null)
		{
			Volume volume2 = null;
			if (DestinationVolumeId.HasValue && DestinationVolumeId.Value != -1 && base.Volumes.ContainsKey(DestinationVolumeId.Value))
			{
				volume2 = base.Volumes[DestinationVolumeId.Value];
				flag2 = true;
				DestinationVolumeId = null;
			}
			else
			{
				string text = LastLevelName.Replace('\\', '/');
				string trimmedLln = text.Substring(text.LastIndexOf('/') + 1);
				foreach (Script item in from s in base.Scripts.Values
					where s.Triggers.Any((ScriptTrigger t) => t.Object.Type == "Volume")
					where s.Actions.Any((ScriptAction a) => a.Object.Type == "Level" && a.Operation.Contains("ChangeLevel") && (a.Arguments[0] == LastLevelName || a.Arguments[0] == trimmedLln))
					select s)
				{
					int value = item.Triggers.Where((ScriptTrigger x) => x.Object.Type == "Volume").First().Object.Identifier.Value;
					if (base.Volumes.ContainsKey(value))
					{
						volume2 = base.Volumes[value];
						flag2 = true;
					}
				}
			}
			if (flag2 && volume2 != null)
			{
				Vector3 vector = (volume2.BoundingBox.Min + volume2.BoundingBox.Max) / 2f + new Vector3(0.001f);
				vector.Y = volume2.BoundingBox.Min.Y - 0.25f;
				spawnView = volume2.Orientations.FirstOrDefault().AsViewpoint();
				num = vector.Dot(spawnView.SideMask());
				float num2 = (volume2.BoundingBox.Max - volume2.BoundingBox.Min).Dot(spawnView.DepthMask()) / 2f + 0.5f;
				Vector3 vector2 = vector + num2 * -spawnView.ForwardVector();
				foreach (TrileEmplacement item2 in thisLevel.InactiveTriles.Union(thisLevel.DestroyedTriles))
				{
					if (Vector3.DistanceSquared(item2.AsVector, vector2) < 2f)
					{
						vector2 -= spawnView.ForwardVector();
						break;
					}
				}
				trileInstance = ActualInstanceAt(vector2);
				if (trileInstance == null)
				{
					trileInstance = NearestTrile(vector, QueryOptions.None, spawnView).Deep;
				}
			}
		}
		InstanceFace instanceFace = new InstanceFace();
		if (!flag || trileInstance == null)
		{
			if (base.StartingPosition != null)
			{
				instanceFace.Instance = TrileInstanceAt(ref base.StartingPosition.Id);
				instanceFace.Face = base.StartingPosition.Face;
			}
			else
			{
				instanceFace.Face = spawnView.VisibleOrientation();
			}
			if (instanceFace.Instance == null)
			{
				instanceFace.Instance = (from x in base.Triles.Values
					where !FezMath.In(x.GetRotatedFace(spawnView.VisibleOrientation()), CollisionType.None, CollisionType.Immaterial, CollisionTypeComparer.Default)
					orderby Math.Abs((x.Center - base.Size / 2f).Dot(spawnView.ScreenSpaceMask()))
					select x).FirstOrDefault();
			}
			trileInstance = instanceFace.Instance;
			spawnView = instanceFace.Face.AsViewpoint();
		}
		CameraManager.Constrained = false;
		CameraManager.PanningConstraints = null;
		if (trileInstance != null)
		{
			GameState.SaveData.Ground = trileInstance.Center;
		}
		GameState.SaveData.View = spawnView;
		GameState.SaveData.TimeOfDay = TimeManager.CurrentTime.TimeOfDay;
		if (flag2)
		{
			PlayerManager.CheckpointGround = null;
		}
		PlayerManager.RespawnAtCheckpoint();
		if (num.HasValue)
		{
			PlayerManager.Position = PlayerManager.Position * (Vector3.One - spawnView.SideMask()) + num.Value * spawnView.SideMask();
		}
		PlayerManager.Action = ActionType.Idle;
		PlayerManager.WallCollision = default(MultipleHits<CollisionResult>);
		PlayerManager.Ground = default(MultipleHits<TrileInstance>);
		PlayerManager.Velocity = 3.15f * (float)Math.Sign(CollisionManager.GravityFactor) * 0.15f * (1f / 60f) * -Vector3.UnitY;
		PhysicsManager.Update(PlayerManager);
		PlayerManager.Velocity = 3.15f * (float)Math.Sign(CollisionManager.GravityFactor) * 0.15f * (1f / 60f) * -Vector3.UnitY;
		IGameCameraManager cameraManager = CameraManager;
		Vector3 interpolatedCenter = (CameraManager.Center = PlayerManager.Center);
		cameraManager.InterpolatedCenter = interpolatedCenter;
		OnLevelChanged();
		LevelService.OnStart();
		ScriptingHost.Instance.ForceUpdate(new GameTime());
		if (!PlayerManager.SpinThroughDoor)
		{
			if (!CameraManager.Constrained)
			{
				CameraManager.Center = PlayerManager.Center + 4f * (float)((!base.Descending) ? 1 : (-1)) / CameraManager.PixelsPerTrixel * Vector3.UnitY;
				CameraManager.SnapInterpolation();
			}
			if (!GameState.FarawaySettings.InTransition)
			{
				base.LevelMaterializer.ForceCull();
			}
		}
		else if (!CameraManager.Constrained)
		{
			IGameCameraManager cameraManager2 = CameraManager;
			interpolatedCenter = (CameraManager.Center = PlayerManager.Center + 4f * (float)((!base.Descending) ? 1 : (-1)) / CameraManager.PixelsPerTrixel * Vector3.UnitY);
			cameraManager2.InterpolatedCenter = interpolatedCenter;
		}
		if (base.Name != "HEX_REBUILD" && base.Name != "DRUM" && base.Name != "VILLAGEVILLE_3D_END_64" && base.Name != "VILLAGEVILLE_3D_END_32")
		{
			GameState.Save();
		}
		GC.Collect(3);
	}

	public void ChangeSky(Sky sky)
	{
		levelData.Sky = sky;
		OnSkyChanged();
	}

	public override void RecordMoveToEnd(int groupId)
	{
		GameState.SaveData.ThisLevel.InactiveGroups.Add(groupId);
		GameState.Save();
	}

	public override bool IsPathRecorded(int groupId)
	{
		return GameState.SaveData.ThisLevel.InactiveGroups.Contains(groupId);
	}

	public void Reset()
	{
		ClearArtSatellites();
		if (levelData.TrileSet != null)
		{
			base.LevelMaterializer.DestroyMaterializers(levelData.TrileSet);
		}
		levelData = new Level
		{
			Name = string.Empty
		};
		levelData.Sky = base.CMProvider.Global.Load<Sky>("Skies/Default");
		OnSkyChanged();
		levelData.TrileSet = null;
		base.LevelMaterializer.RebuildInstances();
		base.LevelMaterializer.CullInstances();
		LastLevelName = null;
		OnLevelChanging();
		OnLevelChanged();
		base.LevelMaterializer.TrilesMesh.Texture = base.CMProvider.Global.Load<Texture2D>("Other Textures/FullWhite");
	}

	public void RemoveArtObject(ArtObjectInstance aoInstance)
	{
		if (aoInstance.ActorSettings.AttachedGroup.HasValue)
		{
			int value = aoInstance.ActorSettings.AttachedGroup.Value;
			TrileInstance[] array = base.Groups[aoInstance.ActorSettings.AttachedGroup.Value].Triles.ToArray();
			foreach (TrileInstance instance in array)
			{
				ClearTrile(instance);
			}
			base.Groups.Remove(value);
		}
		base.ArtObjects.Remove(aoInstance.Id);
		aoInstance.Dispose();
		base.LevelMaterializer.RegisterSatellites();
	}

	public override bool WasPathSupposedToBeRecorded(int id)
	{
		switch (base.Name)
		{
		case "OWL":
			if (id == 0 && GameState.SaveData.ThisLevel.ScriptingState == "4")
			{
				RecordMoveToEnd(id);
				return true;
			}
			break;
		case "ARCH":
			if (id == 3 && GameState.SaveData.ThisLevel.InactiveGroups.Contains(4))
			{
				RecordMoveToEnd(id);
				return true;
			}
			break;
		case "WATERFALL":
			if (id == 1 && GameState.SaveData.ThisLevel.InactiveVolumes.Contains(19))
			{
				RecordMoveToEnd(id);
				return true;
			}
			break;
		}
		return false;
	}
}
