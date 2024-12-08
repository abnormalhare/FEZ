using System;
using System.Collections.Generic;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class BombsHost : DrawableGameComponent
{
	private class BombState
	{
		public TimeSpan SincePickup;

		public BackgroundPlane Explosion;

		public BackgroundPlane Flare;

		public Group Flash;

		public bool IsChainsploding;

		public SoundEmitter Emitter;

		public BombState ChainsplodedBy;
	}

	private class DestructibleGroup
	{
		public List<TrileInstance> AllTriles;

		public TrileGroup Group;

		public float? RespawnIn;
	}

	private readonly Color FlashColor = new Color(255, 0, 0, 128);

	private readonly TimeSpan FlashTime = TimeSpan.FromSeconds(4.0);

	private readonly TimeSpan ExplodeStart = TimeSpan.FromSeconds(6.0);

	private readonly TimeSpan ChainsplodeDelay = TimeSpan.FromSeconds(0.25);

	private AnimatedTexture bombAnimation;

	private AnimatedTexture bigBombAnimation;

	private AnimatedTexture tntAnimation;

	private Texture2D flare;

	private readonly Dictionary<TrileInstance, BombState> bombStates = new Dictionary<TrileInstance, BombState>();

	private readonly List<DestructibleGroup> destructibleGroups = new List<DestructibleGroup>();

	private readonly Dictionary<TrileInstance, DestructibleGroup> indexedDg = new Dictionary<TrileInstance, DestructibleGroup>();

	private Mesh flashesMesh;

	private SoundEffect explodeSound;

	private SoundEffect crystalsplodeSound;

	private SoundEffect countdownSound;

	private static readonly Point[] SmallBombOffsets = new Point[9]
	{
		new Point(0, 0),
		new Point(1, 0),
		new Point(-1, 0),
		new Point(0, 1),
		new Point(0, -1),
		new Point(1, 1),
		new Point(1, -1),
		new Point(-1, 1),
		new Point(-1, -1)
	};

	private static readonly Point[] BigBombOffsets = new Point[25]
	{
		new Point(0, 0),
		new Point(1, 0),
		new Point(2, 0),
		new Point(-1, 0),
		new Point(-2, 0),
		new Point(0, 1),
		new Point(0, -1),
		new Point(0, 2),
		new Point(0, -2),
		new Point(-2, -2),
		new Point(-1, -2),
		new Point(-2, -1),
		new Point(-1, -1),
		new Point(2, -2),
		new Point(1, -2),
		new Point(2, -1),
		new Point(1, -1),
		new Point(-2, 2),
		new Point(-1, 2),
		new Point(-2, 1),
		new Point(-1, 1),
		new Point(2, 2),
		new Point(1, 2),
		new Point(2, 1),
		new Point(1, 1)
	};

	private readonly List<TrileInstance> bsToRemove = new List<TrileInstance>();

	private readonly List<KeyValuePair<TrileInstance, BombState>> bsToAdd = new List<KeyValuePair<TrileInstance, BombState>>();

	private const float H = 0.499f;

	private static readonly Vector3[] CornerNeighbors = new Vector3[5]
	{
		new Vector3(0.499f, 1f, 0.499f),
		new Vector3(0.499f, 1f, -0.499f),
		new Vector3(-0.499f, 1f, 0.499f),
		new Vector3(-0.499f, 1f, -0.499f),
		new Vector3(0f, 1f, 0f)
	};

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	[ServiceDependency]
	public ITrixelParticleSystems TrixelParticleSystems { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public ITrixelParticleSystems ParticleSystemManager { private get; set; }

	public BombsHost(Game game)
		: base(game)
	{
		base.DrawOrder = 10;
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += TryInitialize;
	}

	private void TryInitialize()
	{
		flashesMesh.ClearGroups();
		bombStates.Clear();
		indexedDg.Clear();
		destructibleGroups.Clear();
		foreach (TrileGroup value in LevelManager.Groups.Values)
		{
			if (value.Triles.Count == 0 || !value.Triles[0].Trile.ActorSettings.Type.IsDestructible() || !value.Triles[value.Triles.Count - 1].Trile.ActorSettings.Type.IsDestructible())
			{
				continue;
			}
			DestructibleGroup destructibleGroup = new DestructibleGroup
			{
				AllTriles = new List<TrileInstance>(value.Triles),
				Group = value
			};
			destructibleGroups.Add(destructibleGroup);
			FaceOrientation face = FaceOrientation.Down;
			foreach (TrileInstance trile in value.Triles)
			{
				indexedDg.Add(trile, destructibleGroup);
				TrileEmplacement id = trile.Emplacement.GetTraversal(ref face);
				TrileInstance trileInstance = LevelManager.TrileInstanceAt(ref id);
				if (trileInstance != null && !trileInstance.Trile.ActorSettings.Type.IsDestructible() && trileInstance.PhysicsState == null)
				{
					trileInstance.PhysicsState = new InstancePhysicsState(trileInstance);
				}
			}
		}
	}

	protected override void LoadContent()
	{
		explodeSound = CMProvider.Global.Load<SoundEffect>("Sounds/MiscActors/BombExplode");
		crystalsplodeSound = CMProvider.Global.Load<SoundEffect>("Sounds/MiscActors/TntExplode");
		countdownSound = CMProvider.Global.Load<SoundEffect>("Sounds/MiscActors/BombCountdown");
		bombAnimation = CMProvider.Global.Load<AnimatedTexture>("Background Planes/BombExplosion");
		bigBombAnimation = CMProvider.Global.Load<AnimatedTexture>("Background Planes/BigBombExplosion");
		tntAnimation = CMProvider.Global.Load<AnimatedTexture>("Background Planes/TntExplosion");
		flashesMesh = new Mesh
		{
			AlwaysOnTop = true,
			Blending = BlendingMode.Alphablending
		};
		DrawActionScheduler.Schedule(delegate
		{
			flare = CMProvider.Global.Load<Texture2D>("Background Planes/Flare");
			flashesMesh.Effect = new DefaultEffect.VertexColored();
		});
	}

	public override void Update(GameTime gameTime)
	{
		if (CameraManager.Viewpoint == Viewpoint.Perspective || !CameraManager.ActionRunning || GameState.Paused || GameState.InMap || CameraManager.RequestedViewpoint != 0 || GameState.Loading)
		{
			return;
		}
		foreach (DestructibleGroup destructibleGroup in destructibleGroups)
		{
			if (!destructibleGroup.RespawnIn.HasValue)
			{
				continue;
			}
			destructibleGroup.RespawnIn -= (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (!(destructibleGroup.RespawnIn.Value <= 0f))
			{
				continue;
			}
			bool flag = true;
			foreach (TrileInstance allTrile in destructibleGroup.AllTriles)
			{
				if (!allTrile.Enabled || allTrile.Hidden || allTrile.Removed)
				{
					allTrile.Enabled = false;
					allTrile.Hidden = true;
					ServiceHelper.AddComponent(new GlitchyRespawner(ServiceHelper.Game, allTrile, flag || RandomHelper.Probability(0.25)));
					flag = false;
				}
			}
			destructibleGroup.RespawnIn = null;
		}
		TrileInstance carriedInstance = PlayerManager.CarriedInstance;
		if (carriedInstance != null && carriedInstance.Trile.ActorSettings.Type.IsBomb() && !bombStates.ContainsKey(carriedInstance))
		{
			bool foreign = (carriedInstance.PhysicsState.Respawned = false);
			carriedInstance.Foreign = foreign;
			bombStates.Add(carriedInstance, new BombState());
		}
		bool flag3 = false;
		bool flag4 = false;
		foreach (TrileInstance key in bombStates.Keys)
		{
			BombState bombState = bombStates[key];
			if (!PlayerManager.Action.IsEnteringDoor())
			{
				bombState.SincePickup += gameTime.ElapsedGameTime;
			}
			bool flag5 = key.Trile.ActorSettings.Type == ActorType.BigBomb;
			bool flag6 = key.Trile.ActorSettings.Type == ActorType.TntBlock || key.Trile.ActorSettings.Type == ActorType.TntPickup;
			if (key.Trile.ActorSettings.Type.IsBomb() && key.Hidden)
			{
				bsToRemove.Add(key);
				if (bombState.Flash != null)
				{
					flashesMesh.RemoveGroup(bombState.Flash);
					bombState.Flash = null;
				}
				if (bombState.Emitter != null && bombState.Emitter.Cue != null)
				{
					bombState.Emitter.Cue.Stop();
				}
				continue;
			}
			if (bombState.SincePickup > FlashTime && bombState.Explosion == null)
			{
				if (bombState.Flash == null)
				{
					bombState.Flash = flashesMesh.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Front, FlashColor, centeredOnOrigin: true);
					if (key.Trile.ActorSettings.Type.IsBomb() && !bombState.IsChainsploding)
					{
						bombState.Emitter = countdownSound.EmitAt(key.Center);
						bombState.Emitter.PauseViewTransitions = true;
					}
				}
				double num = bombState.SincePickup.TotalSeconds;
				if (num > ExplodeStart.TotalSeconds - 1.0)
				{
					num *= 2.0;
				}
				bombState.Flash.Enabled = FezMath.Frac(num) < 0.5;
				if (bombState.Flash.Enabled)
				{
					bombState.Flash.Position = key.Center;
					bombState.Flash.Rotation = CameraManager.Rotation;
				}
			}
			if (bombState.SincePickup > ExplodeStart && bombState.Explosion == null)
			{
				if ((flag6 && !flag3) || (!flag6 && !flag4))
				{
					(flag6 ? crystalsplodeSound : explodeSound).EmitAt(key.Center, RandomHelper.Centered(0.025));
					if (flag6)
					{
						flag3 = true;
					}
					else
					{
						flag4 = true;
					}
				}
				if (bombState.ChainsplodedBy != null && bombState.ChainsplodedBy.Emitter != null)
				{
					bombState.ChainsplodedBy.Emitter.FadeOutAndDie(0f);
				}
				float distance = (flag5 ? 0.6f : 0.3f) * FezMath.Saturate(1f - (key.Center - PlayerManager.Center).Length() / 15f);
				if (CamShake.CurrentCamShake == null)
				{
					ServiceHelper.AddComponent(new CamShake(base.Game)
					{
						Duration = TimeSpan.FromSeconds(0.75),
						Distance = distance
					});
				}
				else
				{
					CamShake.CurrentCamShake.Reset();
				}
				ParticleSystemManager.PropagateEnergy(key.Center, flag5 ? 6 : 3);
				flashesMesh.RemoveGroup(bombState.Flash);
				bombState.Flash = null;
				switch (key.Trile.ActorSettings.Type)
				{
				case ActorType.BigBomb:
					bombState.Explosion = new BackgroundPlane(LevelMaterializer.AnimatedPlanesMesh, bigBombAnimation)
					{
						ActorType = ActorType.Bomb
					};
					break;
				case ActorType.TntBlock:
				case ActorType.TntPickup:
					bombState.Explosion = new BackgroundPlane(LevelMaterializer.AnimatedPlanesMesh, tntAnimation)
					{
						ActorType = ActorType.Bomb
					};
					break;
				default:
					bombState.Explosion = new BackgroundPlane(LevelMaterializer.AnimatedPlanesMesh, bombAnimation)
					{
						ActorType = ActorType.Bomb
					};
					break;
				}
				bombState.Explosion.Timing.Loop = false;
				bombState.Explosion.Billboard = true;
				bombState.Explosion.Fullbright = true;
				bombState.Explosion.OriginalRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)RandomHelper.Random.Next(0, 4) * ((float)Math.PI / 2f));
				bombState.Explosion.Timing.Restart();
				LevelManager.AddPlane(bombState.Explosion);
				bombState.Flare = new BackgroundPlane(LevelMaterializer.StaticPlanesMesh, flare)
				{
					AlwaysOnTop = true,
					LightMap = true,
					AllowOverbrightness = true,
					Billboard = true
				};
				LevelManager.AddPlane(bombState.Flare);
				bombState.Flare.Scale = Vector3.One * (flag5 ? 3f : 1.5f);
				BackgroundPlane explosion = bombState.Explosion;
				Vector3 position = (bombState.Flare.Position = key.Center + (-0.5f + RandomHelper.Centered(0.0010000000474974513)) * CameraManager.Viewpoint.ForwardVector());
				explosion.Position = position;
				float num2 = (flag5 ? 3f : 1.5f);
				float num3 = ((PlayerManager.Position - key.Center) * CameraManager.Viewpoint.ScreenSpaceMask()).Length();
				if ((PlayerManager.CarriedInstance == key || num3 < num2) && PlayerManager.Action != ActionType.Dying)
				{
					PlayerManager.Action = ActionType.Suffering;
				}
				if ((key.Trile.ActorSettings.Type == ActorType.TntBlock || bombState.IsChainsploding) && key.InstanceId != -1)
				{
					ParticleSystemManager.Add(new TrixelParticleSystem(base.Game, new TrixelParticleSystem.Settings
					{
						ExplodingInstance = key,
						EnergySource = key.Center,
						MaximumSize = 7,
						Energy = (flag6 ? 3f : 1.5f),
						Darken = true,
						ParticleCount = 4 + 12 / Math.Max(1, TrixelParticleSystems.Count - 3)
					}));
				}
				if (key.Trile.ActorSettings.Type.IsPickable())
				{
					key.Enabled = false;
					LevelMaterializer.GetTrileMaterializer(key.Trile).UpdateInstance(key);
				}
				else
				{
					ClearDestructible(key, skipRecull: false);
				}
				DropSupportedTriles(key);
				DestroyNeighborhood(key, bombState);
			}
			if (bombState.Explosion == null)
			{
				continue;
			}
			bombState.Flare.Filter = Color.Lerp(flag6 ? new Color(0.5f, 1f, 0.25f) : new Color(1f, 0.5f, 0.25f), Color.Black, bombState.Explosion.Timing.NormalizedStep);
			if (bombState.Explosion.Timing.Ended)
			{
				bsToRemove.Add(key);
				if (key.PhysicsState != null)
				{
					key.PhysicsState.ShouldRespawn = key.Trile.ActorSettings.Type.IsPickable();
				}
				LevelManager.RemovePlane(bombState.Explosion);
				LevelManager.RemovePlane(bombState.Flare);
			}
		}
		foreach (TrileInstance item in bsToRemove)
		{
			bombStates.Remove(item);
		}
		bsToRemove.Clear();
		foreach (KeyValuePair<TrileInstance, BombState> item2 in bsToAdd)
		{
			if (!bombStates.ContainsKey(item2.Key))
			{
				bombStates.Add(item2.Key, item2.Value);
			}
		}
		bsToAdd.Clear();
	}

	private void ClearDestructible(TrileInstance instance, bool skipRecull)
	{
		if (indexedDg.TryGetValue(instance, out var value))
		{
			int count = LevelManager.Groups.Count;
			LevelManager.ClearTrile(instance, skipRecull);
			if (count != LevelManager.Groups.Count)
			{
				foreach (TrileInstance allTrile in value.AllTriles)
				{
					GameState.SaveData.ThisLevel.DestroyedTriles.Add(allTrile.OriginalEmplacement);
					indexedDg.Remove(allTrile);
				}
				destructibleGroups.Remove(value);
				value.RespawnIn = null;
			}
			else
			{
				value.RespawnIn = 1.5f;
			}
		}
		else
		{
			LevelManager.ClearTrile(instance, skipRecull);
		}
	}

	private void DestroyNeighborhood(TrileInstance instance, BombState state)
	{
		Vector3 vector = CameraManager.Viewpoint.SideMask();
		Vector3 vector2 = CameraManager.Viewpoint.ForwardVector();
		bool flag = vector.X != 0f;
		bool flag2 = flag;
		int num = (flag2 ? ((int)vector2.Z) : ((int)vector2.X));
		Point point = new Point(flag ? instance.Emplacement.X : instance.Emplacement.Z, instance.Emplacement.Y);
		Point[] obj = ((instance.Trile.ActorSettings.Type == ActorType.BigBomb) ? BigBombOffsets : SmallBombOffsets);
		LevelManager.WaitForScreenInvalidation();
		Point[] array = obj;
		for (int i = 0; i < array.Length; i++)
		{
			Point point2 = array[i];
			bool chainsploded = false;
			bool needsRecull = false;
			Point key = new Point(point.X + point2.X, point.Y + point2.Y);
			if (!LevelManager.ScreenSpaceLimits.TryGetValue(key, out var value))
			{
				continue;
			}
			value.End += num;
			TrileEmplacement id = new TrileEmplacement(flag ? key.X : value.Start, key.Y, flag2 ? value.Start : key.X);
			while ((flag2 ? id.Z : id.X) != value.End)
			{
				TrileInstance nearestNeighbor = LevelManager.TrileInstanceAt(ref id);
				if (TryExplodeAt(state, nearestNeighbor, ref chainsploded, ref needsRecull))
				{
					break;
				}
				if (flag2)
				{
					id.Z += num;
				}
				else
				{
					id.X += num;
				}
			}
			if (needsRecull)
			{
				LevelManager.RecullAt(id);
				TrixelParticleSystems.UnGroundAll();
			}
		}
	}

	private bool TryExplodeAt(BombState state, TrileInstance nearestNeighbor, ref bool chainsploded, ref bool needsRecull)
	{
		if (nearestNeighbor != null && nearestNeighbor.Enabled && !nearestNeighbor.Trile.Immaterial)
		{
			if (!nearestNeighbor.Trile.ActorSettings.Type.IsChainsploding() && !nearestNeighbor.Trile.ActorSettings.Type.IsDestructible())
			{
				return true;
			}
			if (!bombStates.ContainsKey(nearestNeighbor))
			{
				if (nearestNeighbor.Trile.ActorSettings.Type.IsBomb())
				{
					nearestNeighbor.PhysicsState.Respawned = false;
				}
				if (!chainsploded)
				{
					bsToAdd.Add(new KeyValuePair<TrileInstance, BombState>(nearestNeighbor, new BombState
					{
						SincePickup = state.SincePickup - ChainsplodeDelay,
						IsChainsploding = true,
						ChainsplodedBy = state
					}));
					chainsploded = true;
				}
				else
				{
					ClearDestructible(nearestNeighbor, skipRecull: true);
					LevelMaterializer.CullInstanceOut(nearestNeighbor);
					DropSupportedTriles(nearestNeighbor);
					needsRecull = true;
				}
				return true;
			}
		}
		return false;
	}

	private void DropSupportedTriles(TrileInstance instance)
	{
		Vector3[] cornerNeighbors = CornerNeighbors;
		foreach (Vector3 vector in cornerNeighbors)
		{
			Vector3 position = instance.Center + instance.TransformedSize * vector;
			TrileInstance trileInstance = LevelManager.ActualInstanceAt(position);
			if (trileInstance != null && trileInstance.PhysicsState != null)
			{
				MultipleHits<TrileInstance> ground = trileInstance.PhysicsState.Ground;
				if (ground.NearLow == instance)
				{
					trileInstance.PhysicsState.Ground = new MultipleHits<TrileInstance>
					{
						FarHigh = ground.FarHigh
					};
				}
				if (ground.FarHigh == instance)
				{
					trileInstance.PhysicsState.Ground = new MultipleHits<TrileInstance>
					{
						NearLow = ground.NearLow
					};
				}
			}
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (CameraManager.Viewpoint != Viewpoint.Perspective && bombStates.Count != 0)
		{
			GraphicsDevice graphicsDevice = base.GraphicsDevice;
			graphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.Bomb);
			flashesMesh.Draw();
			graphicsDevice.PrepareStencilWrite(StencilMask.None);
		}
	}
}
