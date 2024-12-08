using System;
using System.Linq;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components;

internal class ClockTowerHost : GameComponent
{
	private ArtObjectInstance RedAo;

	private ArtObjectInstance BlueAo;

	private ArtObjectInstance GreenAo;

	private ArtObjectInstance WhiteAo;

	private Quaternion RedOriginalRotation;

	private Quaternion BlueOriginalRotation;

	private Quaternion GreenOriginalRotation;

	private Quaternion WhiteOriginalRotation;

	private Vector3 RedOriginalPosition;

	private Vector3 BlueOriginalPosition;

	private Vector3 GreenOriginalPosition;

	private Vector3 WhiteOriginalPosition;

	private TrileGroup RedGroup;

	private TrileGroup BlueGroup;

	private TrileGroup GreenGroup;

	private TrileGroup WhiteGroup;

	private TrileInstance RedTopMost;

	private TrileInstance BlueTopMost;

	private TrileInstance GreenTopMost;

	private TrileInstance WhiteTopMost;

	private TrileInstance RedSecret;

	private TrileInstance BlueSecret;

	private TrileInstance GreenSecret;

	private TrileInstance WhiteSecret;

	private SoundEffect sTickTock;

	private SoundEmitter eTickTock;

	private float lastRedAngle;

	[ServiceDependency]
	public ILevelService LevelService { get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	public ClockTowerHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		TryInitialize();
		LevelManager.LevelChanged += TryInitialize;
	}

	private void TryInitialize()
	{
		base.Enabled = LevelManager.Name == "CLOCK";
		RedAo = (BlueAo = (GreenAo = (WhiteAo = null)));
		sTickTock = null;
		eTickTock = null;
		if (!base.Enabled)
		{
			return;
		}
		foreach (ArtObjectInstance value in LevelManager.ArtObjects.Values)
		{
			if (value.ArtObjectName == "CLOCKHAND_RAO")
			{
				RedAo = value;
			}
			if (value.ArtObjectName == "CLOCKHAND_GAO")
			{
				GreenAo = value;
			}
			if (value.ArtObjectName == "CLOCKHAND_BAO")
			{
				BlueAo = value;
			}
			if (value.ArtObjectName == "CLOCKHAND_WAO")
			{
				WhiteAo = value;
			}
		}
		RedOriginalRotation = RedAo.Rotation;
		BlueOriginalRotation = BlueAo.Rotation;
		GreenOriginalRotation = GreenAo.Rotation;
		WhiteOriginalRotation = WhiteAo.Rotation;
		RedOriginalPosition = RedAo.Position + 1.125f * Vector3.UnitX;
		GreenOriginalPosition = GreenAo.Position - 1.125f * Vector3.UnitX;
		BlueOriginalPosition = BlueAo.Position + 1.125f * Vector3.UnitZ;
		WhiteOriginalPosition = WhiteAo.Position - 1.125f * Vector3.UnitZ;
		RedGroup = LevelManager.Groups[23];
		BlueGroup = LevelManager.Groups[24];
		GreenGroup = LevelManager.Groups[25];
		WhiteGroup = LevelManager.Groups[26];
		RedTopMost = RedGroup.Triles.First((TrileInstance x) => x.Emplacement.Y == 58);
		BlueTopMost = BlueGroup.Triles.First((TrileInstance x) => x.Emplacement.Y == 58);
		GreenTopMost = GreenGroup.Triles.First((TrileInstance x) => x.Emplacement.Y == 58);
		WhiteTopMost = WhiteGroup.Triles.First((TrileInstance x) => x.Emplacement.Y == 58);
		if (GameState.SaveData.ThisLevel.InactiveArtObjects.Contains(RedAo.Id))
		{
			RedAo.Enabled = false;
			LevelManager.RemoveArtObject(RedAo);
		}
		if (GameState.SaveData.ThisLevel.InactiveArtObjects.Contains(GreenAo.Id))
		{
			GreenAo.Enabled = false;
			LevelManager.RemoveArtObject(GreenAo);
		}
		if (GameState.SaveData.ThisLevel.InactiveArtObjects.Contains(BlueAo.Id))
		{
			BlueAo.Enabled = false;
			LevelManager.RemoveArtObject(BlueAo);
		}
		if (GameState.SaveData.ThisLevel.InactiveArtObjects.Contains(WhiteAo.Id))
		{
			WhiteAo.Enabled = false;
			LevelManager.RemoveArtObject(WhiteAo);
		}
		DateTime dateTime = DateTime.FromFileTimeUtc(GameState.SaveData.CreationTime);
		TimeSpan timeSpan = TimeSpan.FromTicks((DateTime.UtcNow - dateTime).Ticks);
		if (RedAo.Enabled)
		{
			float angle = (lastRedAngle = FezMath.WrapAngle((float)FezMath.Round(timeSpan.TotalSeconds) / 60f * ((float)Math.PI * 2f)));
			RedAo.Rotation = Quaternion.CreateFromAxisAngle(-Vector3.UnitZ, angle) * RedOriginalRotation;
		}
		if (WhiteAo.Enabled || BlueAo.Enabled || GreenAo.Enabled || RedAo.Enabled)
		{
			sTickTock = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/MiscActors/TickTockLoop");
			Waiters.Wait(FezMath.Frac(timeSpan.TotalSeconds), delegate
			{
				eTickTock = sTickTock.EmitAt(new Vector3(41.5f, 61.5f, 35.5f), loop: true);
			});
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (!GameState.Paused && !GameState.InMap && CameraManager.ActionRunning && CameraManager.Viewpoint.IsOrthographic() && !GameState.Loading && PlayerManager.Action != ActionType.FindingTreasure && !GameState.FarawaySettings.InTransition)
		{
			DateTime dateTime = DateTime.FromFileTimeUtc(GameState.SaveData.CreationTime);
			TimeSpan timeSpan = TimeSpan.FromTicks(TimeSpan.FromTicks((DateTime.UtcNow - dateTime).Ticks).Ticks % 6048000000000L);
			if (RedAo.Enabled)
			{
				float num = (lastRedAngle = FezMath.CurveAngle(lastRedAngle, FezMath.WrapAngle((float)FezMath.Round(timeSpan.TotalSeconds) / 60f * ((float)Math.PI * 2f)), 0.1f));
				RedAo.Rotation = Quaternion.CreateFromAxisAngle(-Vector3.UnitZ, num) * RedOriginalRotation;
				RedAo.Position = RedOriginalPosition + new Vector3(0f - (float)Math.Cos(num), (float)Math.Sin(num), 0f) * 1.45f;
				RedSecret = TestSecretFor(num >= 1.1957964f && num <= 1.6957964f, RedAo, RedSecret, RedTopMost);
			}
			if (BlueAo.Enabled)
			{
				float num2 = FezMath.WrapAngle((float)timeSpan.TotalMinutes / 60f * ((float)Math.PI * 2f));
				BlueAo.Rotation = Quaternion.CreateFromAxisAngle(-Vector3.UnitX, num2) * BlueOriginalRotation;
				BlueAo.Position = BlueOriginalPosition + new Vector3(0f, (float)Math.Sin(num2), (float)Math.Cos(num2)) * 1.45f;
				BlueSecret = TestSecretFor(FezMath.AlmostEqual(num2, (float)Math.PI / 2f, 0.125f), BlueAo, BlueSecret, BlueTopMost);
			}
			if (GreenAo.Enabled)
			{
				float num3 = FezMath.WrapAngle((float)timeSpan.TotalHours / 24f * ((float)Math.PI * 2f));
				GreenAo.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, num3) * GreenOriginalRotation;
				GreenAo.Position = GreenOriginalPosition + new Vector3((float)Math.Cos(num3), (float)Math.Sin(num3), 0f) * 1.45f;
				GreenSecret = TestSecretFor(FezMath.AlmostEqual(num3, (float)Math.PI / 2f, 0.125f), GreenAo, GreenSecret, GreenTopMost);
			}
			if (WhiteAo.Enabled)
			{
				float num4 = FezMath.WrapAngle((float)timeSpan.TotalDays / 7f * ((float)Math.PI * 2f));
				WhiteAo.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, num4) * WhiteOriginalRotation;
				WhiteAo.Position = WhiteOriginalPosition + new Vector3(0f, (float)Math.Sin(num4), 0f - (float)Math.Cos(num4)) * 1.45f;
				WhiteSecret = TestSecretFor(FezMath.AlmostEqual(num4, (float)Math.PI / 2f, 0.125f), WhiteAo, WhiteSecret, WhiteTopMost);
			}
		}
	}

	private TrileInstance TestSecretFor(bool condition, ArtObjectInstance ao, TrileInstance secretTrile, TrileInstance topMost)
	{
		Vector3 vector = topMost.Position + Vector3.Up * 1.5f;
		if (condition)
		{
			if (secretTrile != null && secretTrile.Collected)
			{
				ServiceHelper.AddComponent(new GlitchyDespawner(base.Game, ao));
				GameState.SaveData.ThisLevel.InactiveArtObjects.Add(ao.Id);
				ao.Enabled = false;
				TestAllSolved();
				LevelService.ResolvePuzzle();
				return null;
			}
			if (secretTrile == null)
			{
				secretTrile = new TrileInstance(vector, LevelManager.ActorTriles(ActorType.SecretCube).FirstOrDefault().Id);
				ServiceHelper.AddComponent(new GlitchyRespawner(base.Game, secretTrile)
				{
					DontCullIn = true
				});
			}
			secretTrile.Position = vector;
			if (!secretTrile.Hidden)
			{
				LevelManager.UpdateInstance(secretTrile);
			}
		}
		else if (secretTrile != null)
		{
			if (secretTrile.Collected)
			{
				ServiceHelper.AddComponent(new GlitchyDespawner(base.Game, ao));
				GameState.SaveData.ThisLevel.InactiveArtObjects.Add(ao.Id);
				ao.Enabled = false;
				TestAllSolved();
				LevelService.ResolvePuzzle();
				return null;
			}
			ServiceHelper.AddComponent(new GlitchyDespawner(base.Game, secretTrile));
			TrileInstance rs = secretTrile;
			Vector3 p = vector;
			Waiters.Interpolate(2.5, delegate
			{
				rs.Position = p;
			});
			return null;
		}
		return secretTrile;
	}

	private void TestAllSolved()
	{
		if (!WhiteAo.Enabled && !BlueAo.Enabled && !GreenAo.Enabled && !RedAo.Enabled)
		{
			eTickTock.FadeOutAndDie(1f);
			eTickTock = null;
			Volume volume = LevelManager.Volumes[6];
			if (volume.Enabled)
			{
				volume.Enabled = false;
				GameState.SaveData.ThisLevel.InactiveVolumes.Add(volume.Id);
			}
		}
	}
}
