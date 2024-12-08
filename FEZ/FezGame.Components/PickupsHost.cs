using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FezEngine;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components;

public class PickupsHost : DrawableGameComponent
{
	private const float SubmergedPortion = 0.8125f;

	private SoundEffect vaseBreakSound;

	private SoundEffect thudSound;

	private AnimatedTexture largeDust;

	private AnimatedTexture smallDust;

	private List<PickupState> PickupStates;

	private float sinceLevelChanged;

	private readonly ManualResetEvent initLock = new ManualResetEvent(initialState: false);

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IPhysicsManager PhysicsManager { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	[ServiceDependency]
	public ITrixelParticleSystems ParticleSystemManager { private get; set; }

	[ServiceDependency]
	public ICollisionManager CollisionManager { private get; set; }

	[ServiceDependency]
	public IPlaneParticleSystems PlaneParticleSystems { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency(Optional = true)]
	public IDebuggingBag DebuggingBag { private get; set; }

	public PickupsHost(Game game)
		: base(game)
	{
		base.UpdateOrder = -1;
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += InitializePickups;
		InitializePickups();
		CameraManager.ViewpointChanged += delegate
		{
			if (!GameState.Loading && CameraManager.Viewpoint.IsOrthographic() && CameraManager.LastViewpoint != CameraManager.Viewpoint)
			{
				PauseGroupOverlaps(force: false);
				LevelManager.ScreenInvalidated += DetectBackground;
			}
		};
		CollisionManager.GravityChanged += delegate
		{
			foreach (PickupState pickupState in PickupStates)
			{
				pickupState.Instance.PhysicsState.Ground = default(MultipleHits<TrileInstance>);
			}
		};
	}

	private void DetectBackground()
	{
		if (LevelManager.Name != "LAVA")
		{
			foreach (TrileGroup value in LevelManager.PickupGroups.Values)
			{
				foreach (TrileInstance trile in value.Triles)
				{
					trile.PhysicsState.UpdatingPhysics = true;
				}
				foreach (TrileInstance trile2 in value.Triles)
				{
					if (!trile2.PhysicsState.IgnoreCollision)
					{
						PhysicsManager.DetermineInBackground(trile2.PhysicsState, allowEnterInBackground: true, viewpointChanged: true, keepInFront: false);
					}
				}
				foreach (TrileInstance trile3 in value.Triles)
				{
					trile3.PhysicsState.UpdatingPhysics = false;
				}
			}
		}
		if (PickupStates == null)
		{
			return;
		}
		foreach (PickupState pickupState in PickupStates)
		{
			if (pickupState.Group == null && pickupState.Instance.PhysicsState != null)
			{
				PhysicsManager.DetermineInBackground(pickupState.Instance.PhysicsState, allowEnterInBackground: true, viewpointChanged: true, keepInFront: false);
			}
		}
	}

	private void PauseGroupOverlaps(bool force)
	{
		if ((!force && GameState.Loading) || !CameraManager.Viewpoint.IsOrthographic() || LevelManager.PickupGroups.Count == 0)
		{
			return;
		}
		Vector3 b = CameraManager.Viewpoint.ForwardVector();
		Vector3 b2 = CameraManager.Viewpoint.SideMask();
		Vector3 vector = CameraManager.Viewpoint.ScreenSpaceMask();
		foreach (TrileGroup item in LevelManager.PickupGroups.Values.Distinct())
		{
			float num = float.MaxValue;
			float? num2 = null;
			foreach (TrileInstance trile in item.Triles)
			{
				num = Math.Min(num, trile.Center.Dot(b));
				if (!trile.PhysicsState.Puppet)
				{
					num2 = trile.Center.Dot(b2);
				}
			}
			foreach (PickupState pickupState in PickupStates)
			{
				if (pickupState.Group != item)
				{
					continue;
				}
				TrileInstance instance = pickupState.Instance;
				bool flag = !FezMath.AlmostEqual(instance.Center.Dot(b), num);
				instance.PhysicsState.Paused = flag;
				if (flag)
				{
					instance.PhysicsState.Puppet = true;
					pickupState.LastMovement = Vector3.Zero;
					continue;
				}
				pickupState.VisibleOverlapper = null;
				foreach (PickupState pickupState2 in PickupStates)
				{
					if (FezMath.AlmostEqual(pickupState2.Instance.Center * vector, pickupState.Instance.Center * vector))
					{
						pickupState2.VisibleOverlapper = pickupState;
					}
				}
				if (num2.HasValue && FezMath.AlmostEqual(instance.Center.Dot(b2), num2.Value))
				{
					instance.PhysicsState.Puppet = false;
				}
			}
		}
	}

	protected override void LoadContent()
	{
		largeDust = CMProvider.Global.Load<AnimatedTexture>("Background Planes/dust_large");
		smallDust = CMProvider.Global.Load<AnimatedTexture>("Background Planes/dust_small");
		vaseBreakSound = CMProvider.Global.Load<SoundEffect>("Sounds/MiscActors/VaseBreak");
		thudSound = CMProvider.Global.Load<SoundEffect>("Sounds/MiscActors/HitFloor");
	}

	private void InitializePickups()
	{
		sinceLevelChanged = 0f;
		if (LevelManager.TrileSet == null)
		{
			initLock.Reset();
			PickupStates = null;
			initLock.Set();
			return;
		}
		initLock.Reset();
		if (PickupStates != null)
		{
			foreach (PickupState pickupState in PickupStates)
			{
				pickupState.Instance.PhysicsState.ShouldRespawn = false;
			}
		}
		PickupStates = new List<PickupState>(from t in LevelManager.TrileSet.Triles.Values.Where((Trile t) => t.ActorSettings.Type.IsPickable()).SelectMany((Trile t) => t.Instances)
			select new PickupState(t, LevelManager.PickupGroups.ContainsKey(t) ? LevelManager.PickupGroups[t] : null));
		foreach (PickupState item in PickupStates.Where((PickupState x) => x.Group != null))
		{
			int groupId = item.Group.Id;
			item.AttachedAOs = LevelMaterializer.LevelArtObjects.Where((ArtObjectInstance x) => x.ActorSettings.AttachedGroup == groupId).ToArray();
			if (item.Group.Triles.Count == 1)
			{
				item.Group = null;
			}
		}
		foreach (TrileInstance item2 in from x in PickupStates
			where x.Instance.PhysicsState == null
			select x.Instance)
		{
			item2.PhysicsState = new InstancePhysicsState(item2)
			{
				Ground = new MultipleHits<TrileInstance>
				{
					NearLow = LevelManager.ActualInstanceAt(item2.Center - item2.Trile.Size.Y * Vector3.UnitY)
				}
			};
		}
		bool ignoreCollision = LevelManager.WaterType == LiquidType.Sewer && !FezMath.In(LevelManager.Name, "SEWER_PIVOT", "SEWER_TREASURE_TWO");
		foreach (TrileGroup item3 in LevelManager.PickupGroups.Values.Where((TrileGroup g) => g.Triles.All((TrileInstance t) => !t.PhysicsState.Puppet)).Distinct())
		{
			foreach (TrileInstance trile in item3.Triles)
			{
				trile.PhysicsState.IgnoreCollision = ignoreCollision;
				trile.PhysicsState.Center += 0.002f * FezMath.XZMask;
				trile.PhysicsState.UpdateInstance();
				LevelManager.UpdateInstance(trile);
				trile.PhysicsState.Puppet = true;
			}
			item3.Triles[item3.Triles.Count / 2].PhysicsState.Puppet = false;
			item3.InMidAir = true;
		}
		PauseGroupOverlaps(force: true);
		DetectBackground();
		initLock.Set();
	}

	public override void Update(GameTime gameTime)
	{
		if (CameraManager.Viewpoint == Viewpoint.Perspective || !CameraManager.ActionRunning || GameState.Paused || GameState.InMap || GameState.Loading || PickupStates == null || PickupStates.Count == 0)
		{
			return;
		}
		sinceLevelChanged += (float)gameTime.ElapsedGameTime.TotalSeconds;
		initLock.WaitOne();
		for (int num = PickupStates.Count - 1; num >= 0; num--)
		{
			if (PickupStates[num].Instance.PhysicsState == null)
			{
				PickupStates.RemoveAt(num);
			}
		}
		foreach (PickupState pickupState2 in PickupStates)
		{
			if (pickupState2.Instance.PhysicsState.StaticGrounds)
			{
				pickupState2.Instance.PhysicsState.GroundMovement = Vector3.Zero;
			}
		}
		PickupStates.Sort(MovingGroundsPickupComparer.Default);
		UpdatePickups((float)gameTime.ElapsedGameTime.TotalSeconds);
		foreach (TrileGroup value in LevelManager.PickupGroups.Values)
		{
			if (value.InMidAir)
			{
				foreach (TrileInstance trile in value.Triles)
				{
					if (trile.PhysicsState.Paused || !trile.PhysicsState.Grounded)
					{
						continue;
					}
					value.InMidAir = false;
					if (!trile.PhysicsState.Puppet)
					{
						break;
					}
					trile.PhysicsState.Puppet = false;
					foreach (TrileInstance trile2 in value.Triles)
					{
						if (trile2 != trile)
						{
							trile2.PhysicsState.Puppet = true;
						}
					}
					break;
				}
				continue;
			}
			value.InMidAir = true;
			foreach (TrileInstance trile3 in value.Triles)
			{
				value.InMidAir &= !trile3.PhysicsState.Grounded;
			}
		}
		foreach (PickupState pickupState3 in PickupStates)
		{
			if (pickupState3.Group != null && !pickupState3.Instance.PhysicsState.Puppet)
			{
				PickupState pickupState = pickupState3;
				foreach (PickupState pickupState4 in PickupStates)
				{
					if (pickupState4.Group == pickupState.Group && pickupState4 != pickupState)
					{
						pickupState4.Instance.PhysicsState.Center += pickupState.LastMovement - pickupState4.LastMovement;
						pickupState4.Instance.PhysicsState.Background = pickupState.Instance.PhysicsState.Background;
						pickupState4.Instance.PhysicsState.Velocity = pickupState.Instance.PhysicsState.Velocity;
						pickupState4.Instance.PhysicsState.UpdateInstance();
						LevelManager.UpdateInstance(pickupState4.Instance);
						pickupState4.LastMovement = Vector3.Zero;
						pickupState4.FloatMalus = pickupState.FloatMalus;
						pickupState4.FloatSeed = pickupState.FloatSeed;
					}
				}
			}
			if (pickupState3.VisibleOverlapper != null)
			{
				PickupState visibleOverlapper = pickupState3.VisibleOverlapper;
				InstancePhysicsState physicsState = pickupState3.Instance.PhysicsState;
				physicsState.Background = visibleOverlapper.Instance.PhysicsState.Background;
				physicsState.Ground = visibleOverlapper.Instance.PhysicsState.Ground;
				physicsState.Floating = visibleOverlapper.Instance.PhysicsState.Floating;
				Array.Copy(physicsState.CornerCollision, pickupState3.Instance.PhysicsState.CornerCollision, 4);
				physicsState.GroundMovement = visibleOverlapper.Instance.PhysicsState.GroundMovement;
				physicsState.Sticky = visibleOverlapper.Instance.PhysicsState.Sticky;
				physicsState.WallCollision = visibleOverlapper.Instance.PhysicsState.WallCollision;
				physicsState.PushedDownBy = visibleOverlapper.Instance.PhysicsState.PushedDownBy;
				pickupState3.LastGroundedCenter = visibleOverlapper.LastGroundedCenter;
				pickupState3.LastVelocity = visibleOverlapper.LastVelocity;
				pickupState3.TouchesWater = visibleOverlapper.TouchesWater;
			}
		}
		foreach (PickupState pickupState5 in PickupStates)
		{
			if (pickupState5.Instance.PhysicsState != null && (pickupState5.Instance.PhysicsState.Grounded || PlayerManager.CarriedInstance == pickupState5.Instance || pickupState5.Instance.PhysicsState.Floating))
			{
				pickupState5.FlightApex = pickupState5.Instance.Center.Y;
				pickupState5.LastGroundedCenter = pickupState5.Instance.Center;
			}
		}
		initLock.Set();
	}

	private void UpdatePickups(float elapsedSeconds)
	{
		Vector3 vector = Vector3.UnitY * CameraManager.Radius / CameraManager.AspectRatio;
		foreach (PickupState pickupState in PickupStates)
		{
			TrileInstance instance = pickupState.Instance;
			InstancePhysicsState physicsState = instance.PhysicsState;
			ActorType type = instance.Trile.ActorSettings.Type;
			if (physicsState.Paused || (!physicsState.ShouldRespawn && (!instance.Enabled || instance == PlayerManager.CarriedInstance || (physicsState.Static && !pickupState.TouchesWater))))
			{
				continue;
			}
			TryFloat(pickupState, elapsedSeconds);
			if (!physicsState.Vanished && (!pickupState.TouchesWater || !type.IsBuoyant()))
			{
				physicsState.Velocity += 3.15f * CollisionManager.GravityFactor * 0.15f * elapsedSeconds * Vector3.Down;
			}
			bool grounded = instance.PhysicsState.Grounded;
			Vector3 center = physicsState.Center;
			PhysicsManager.Update(physicsState, simple: false, pickupState.Group == null && (!physicsState.Floating || !FezMath.AlmostEqual(physicsState.Velocity.X, 0f) || !FezMath.AlmostEqual(physicsState.Velocity.Z, 0f)));
			pickupState.LastMovement = physicsState.Center - center;
			if (physicsState.NoVelocityClamping)
			{
				physicsState.NoVelocityClamping = false;
				physicsState.Velocity = Vector3.Zero;
			}
			if (pickupState.AttachedAOs != null)
			{
				ArtObjectInstance[] attachedAOs = pickupState.AttachedAOs;
				for (int i = 0; i < attachedAOs.Length; i++)
				{
					attachedAOs[i].Position += pickupState.LastMovement;
				}
			}
			if ((pickupState.LastGroundedCenter.Y - instance.Position.Y) * (float)Math.Sign(CollisionManager.GravityFactor) > vector.Y)
			{
				physicsState.Vanished = true;
			}
			else if (LevelManager.Loops)
			{
				while (instance.Position.Y < 0f)
				{
					instance.Position += LevelManager.Size * Vector3.UnitY;
				}
				while (instance.Position.Y > LevelManager.Size.Y)
				{
					instance.Position -= LevelManager.Size * Vector3.UnitY;
				}
			}
			if (physicsState.Floating && physicsState.Grounded && !physicsState.PushedUp)
			{
				physicsState.Floating = (pickupState.TouchesWater = instance.Position.Y <= LevelManager.WaterHeight - 0.8125f + pickupState.FloatMalus);
			}
			physicsState.ForceNonStatic = false;
			if (type.IsFragile())
			{
				if (!instance.PhysicsState.Grounded)
				{
					pickupState.FlightApex = Math.Max(pickupState.FlightApex, instance.Center.Y);
				}
				else if (!instance.PhysicsState.Respawned && pickupState.FlightApex - instance.Center.Y > BreakHeight(type))
				{
					PlayBreakSound(type, instance.Position);
					instance.PhysicsState.Vanished = true;
					ParticleSystemManager.Add(new TrixelParticleSystem(base.Game, new TrixelParticleSystem.Settings
					{
						ExplodingInstance = instance,
						EnergySource = instance.Center - Vector3.Normalize(pickupState.LastVelocity) * instance.TransformedSize / 2f,
						ParticleCount = 30,
						MinimumSize = 1,
						MaximumSize = 8,
						GravityModifier = 1f,
						Energy = 0.25f,
						BaseVelocity = pickupState.LastVelocity * 0.5f
					}));
					LevelMaterializer.CullInstanceOut(instance);
					if (type == ActorType.Vase)
					{
						instance.PhysicsState = null;
						LevelManager.ClearTrile(instance);
						break;
					}
					instance.Enabled = false;
					instance.PhysicsState.ShouldRespawn = true;
				}
			}
			TryPushHorizontalStack(pickupState, elapsedSeconds);
			if (physicsState.Static)
			{
				Vector3 lastVelocity = (physicsState.Velocity = Vector3.Zero);
				pickupState.LastMovement = (pickupState.LastVelocity = lastVelocity);
				physicsState.Respawned = false;
			}
			if (physicsState.Vanished)
			{
				physicsState.ShouldRespawn = true;
			}
			if (physicsState.ShouldRespawn && PlayerManager.Action != ActionType.FreeFalling)
			{
				physicsState.Center = pickupState.OriginalCenter + new Vector3(0.001f);
				physicsState.UpdateInstance();
				physicsState.Velocity = Vector3.Zero;
				physicsState.ShouldRespawn = false;
				pickupState.LastVelocity = Vector3.Zero;
				pickupState.TouchesWater = false;
				physicsState.Floating = false;
				physicsState.PushedDownBy = null;
				instance.Enabled = false;
				instance.Hidden = true;
				physicsState.Ground = new MultipleHits<TrileInstance>
				{
					NearLow = LevelManager.ActualInstanceAt(physicsState.Center - instance.Trile.Size.Y * Vector3.UnitY)
				};
				ServiceHelper.AddComponent(new GlitchyRespawner(ServiceHelper.Game, instance));
			}
			physicsState.UpdateInstance();
			LevelManager.UpdateInstance(instance);
			if (!grounded && instance.PhysicsState.Grounded && Math.Abs(pickupState.LastVelocity.Y) > 0.05f)
			{
				float num = pickupState.LastVelocity.Dot(CameraManager.Viewpoint.RightVector());
				float num2 = FezMath.Saturate(pickupState.LastVelocity.Y / (-0.2f * (float)Math.Sign(CollisionManager.GravityFactor)));
				AnimatedTexture animatedTexture;
				if (instance.Trile.ActorSettings.Type.IsHeavy())
				{
					if (num2 > 0.5f)
					{
						animatedTexture = largeDust;
					}
					else
					{
						animatedTexture = smallDust;
						num2 *= 2f;
					}
				}
				else
				{
					animatedTexture = smallDust;
				}
				num2 = Math.Max(num2, 0.4f);
				SpawnDust(instance, num2, animatedTexture, num >= 0f, num <= 0f);
				if (animatedTexture == largeDust && num != 0f)
				{
					SpawnDust(instance, num2, smallDust, num < 0f, num > 0f);
				}
				thudSound.EmitAt(instance.Position, num2 * -0.6f + 0.3f, num2);
			}
			if (physicsState.Grounded && physicsState.Ground.First.PhysicsState != null)
			{
				physicsState.Ground.First.PhysicsState.PushedDownBy = instance;
			}
			pickupState.LastVelocity = instance.PhysicsState.Velocity;
		}
	}

	private void TryFloat(PickupState pickup, float elapsedSeconds)
	{
		TrileInstance instance = pickup.Instance;
		InstancePhysicsState physicsState = instance.PhysicsState;
		ActorType type = instance.Trile.ActorSettings.Type;
		if (physicsState.Grounded && physicsState.Ground.First.PhysicsState != null)
		{
			physicsState.Ground.First.PhysicsState.PushedDownBy = null;
		}
		if (LevelManager.WaterType == LiquidType.None || physicsState.Grounded)
		{
			return;
		}
		DetermineFloatMalus(pickup);
		if (!physicsState.Floating)
		{
			if (instance.Position.Y <= LevelManager.WaterHeight - 0.8125f + pickup.FloatMalus - 0.0625f)
			{
				if (!pickup.TouchesWater)
				{
					if (Math.Abs(physicsState.Velocity.Y) > 0.025f && sinceLevelChanged > 1f)
					{
						PlaneParticleSystems.Splash(physicsState, outwards: false, 0.25f);
					}
					physicsState.Velocity *= new Vector3(1f, 0.35f, 1f);
				}
				pickup.TouchesWater = true;
			}
			else
			{
				if (pickup.TouchesWater)
				{
					if ((double)Math.Abs(physicsState.Velocity.Y) < 0.005)
					{
						physicsState.Floating = true;
						float num = LevelManager.WaterHeight - 0.8125f + pickup.FloatMalus - 0.0625f;
						float num2 = (instance.Position.Y - num) / (3f / 32f);
						if (num2 > -1f && num2 < 1f)
						{
							pickup.FloatSeed = (float)Math.Asin(num2);
						}
					}
					else
					{
						physicsState.Velocity *= new Vector3(1f, 0.35f, 1f);
					}
				}
				pickup.TouchesWater = false;
			}
		}
		if (pickup.TouchesWater && !physicsState.Floating)
		{
			physicsState.Velocity *= new Vector3(0.9f);
			if (type.IsBuoyant())
			{
				float num3 = LevelManager.WaterHeight - 0.8125f + pickup.FloatMalus - instance.Position.Y;
				if (sinceLevelChanged <= 1f && LevelManager.WaterType == LiquidType.Sewer && !physicsState.IgnoreClampToWater && Math.Abs(num3) > 0.5f)
				{
					physicsState.NoVelocityClamping = true;
					physicsState.Velocity += num3 * Vector3.UnitY;
				}
				else
				{
					physicsState.Velocity += num3 * 0.00875f * Vector3.UnitY;
				}
			}
		}
		physicsState.Sticky = physicsState.Floating;
		if (physicsState.Floating && !physicsState.PushedUp)
		{
			pickup.FloatSeed = FezMath.WrapAngle(pickup.FloatSeed + elapsedSeconds * 2f);
			DetermineFloatMalus(pickup);
			Vector3 vector = instance.Position * FezMath.XZMask + Vector3.UnitY * (LevelManager.WaterHeight - 0.8125f + (float)Math.Sin(pickup.FloatSeed) * 1.5f / 16f + pickup.FloatMalus);
			physicsState.Velocity = vector - instance.Position + physicsState.Velocity * FezMath.XZMask * 0.6f;
		}
		if (sinceLevelChanged <= 1f && !physicsState.Floating && !pickup.TouchesWater && LevelManager.WaterType == LiquidType.Sewer && !physicsState.IgnoreClampToWater)
		{
			float num4 = LevelManager.WaterHeight - 0.8125f + pickup.FloatMalus - instance.Position.Y;
			if (Math.Abs(num4) > 0.5f)
			{
				physicsState.NoVelocityClamping = true;
				physicsState.Velocity += num4 * Vector3.UnitY;
			}
		}
	}

	private void TryPushHorizontalStack(PickupState state, float elapsedSeconds)
	{
		TrileInstance instance = state.Instance;
		TrileGroup group = state.Group;
		if (!instance.PhysicsState.WallCollision.AnyCollided())
		{
			return;
		}
		Vector3 vector = -instance.PhysicsState.WallCollision.First.Response.Sign();
		instance.PhysicsState.Velocity = Vector3.Zero;
		TrileInstance trileInstance = instance;
		while (trileInstance != null && trileInstance.PhysicsState.WallCollision.AnyCollided())
		{
			MultipleHits<CollisionResult> wallCollision = trileInstance.PhysicsState.WallCollision;
			TrileInstance destination = wallCollision.First.Destination;
			if (destination.PhysicsState != null && destination.Trile.ActorSettings.Type.IsPickable() && (group == null || !group.Triles.Contains(destination)))
			{
				Vector3 vector2 = -wallCollision.First.Response;
				if (vector2.Sign() != vector || vector2 == Vector3.Zero)
				{
					trileInstance = null;
					continue;
				}
				trileInstance = destination;
				Vector3 velocity = trileInstance.PhysicsState.Velocity;
				trileInstance.PhysicsState.Velocity = vector2;
				Vector3 center = trileInstance.PhysicsState.Center;
				if (trileInstance.PhysicsState.Grounded)
				{
					trileInstance.PhysicsState.Velocity += 3.15f * (float)Math.Sign(CollisionManager.GravityFactor) * 0.15f * elapsedSeconds * Vector3.Down;
				}
				PhysicsManager.Update(trileInstance.PhysicsState, simple: false, keepInFront: false);
				if (instance.PhysicsState.Grounded)
				{
					trileInstance.PhysicsState.Velocity = velocity;
				}
				trileInstance.PhysicsState.UpdateInstance();
				LevelManager.UpdateInstance(trileInstance);
				foreach (PickupState pickupState in PickupStates)
				{
					if (pickupState.Instance.PhysicsState.Ground.NearLow == trileInstance || pickupState.Instance.PhysicsState.Ground.FarHigh == trileInstance)
					{
						Vector3 velocity2 = (trileInstance.PhysicsState.Center - center) / 0.85f;
						pickupState.Instance.PhysicsState.Velocity = velocity2;
					}
				}
			}
			else
			{
				trileInstance = null;
			}
		}
	}

	private void SpawnDust(TrileInstance instance, float opacity, AnimatedTexture animation, bool onRight, bool onLeft)
	{
		float num = instance.Center.Y - instance.TransformedSize.Y / 2f * (float)Math.Sign(CollisionManager.GravityFactor) + (float)animation.FrameHeight / 32f * (float)Math.Sign(CollisionManager.GravityFactor);
		float num2 = instance.TransformedSize.Dot(CameraManager.Viewpoint.SideMask()) / 2f + (float)animation.FrameWidth / 32f * 2f / 3f;
		if (instance.Trile.ActorSettings.Type.IsBomb())
		{
			num2 -= 0.25f;
		}
		opacity = 1f;
		Vector3 vector = CameraManager.Viewpoint.RightVector();
		Vector3 vector2 = CameraManager.Viewpoint.ForwardVector();
		bool b = CollisionManager.GravityFactor < 0f;
		if (onRight)
		{
			IGameLevelManager levelManager = LevelManager;
			BackgroundPlane obj = new BackgroundPlane(LevelMaterializer.AnimatedPlanesMesh, animation)
			{
				OriginalRotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)b.AsNumeric() * (float)Math.PI),
				Doublesided = true,
				Loop = false,
				Opacity = opacity,
				Timing = 
				{
					Step = 0f
				}
			};
			BackgroundPlane backgroundPlane = obj;
			levelManager.AddPlane(obj);
			backgroundPlane.Position = instance.Center * FezMath.XZMask + vector * num2 + num * Vector3.UnitY - vector2;
			backgroundPlane.Billboard = true;
		}
		if (onLeft)
		{
			IGameLevelManager levelManager2 = LevelManager;
			BackgroundPlane obj2 = new BackgroundPlane(LevelMaterializer.AnimatedPlanesMesh, animation)
			{
				OriginalRotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI) * Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)b.AsNumeric() * (float)Math.PI),
				Doublesided = true,
				Loop = false,
				Opacity = opacity,
				Timing = 
				{
					Step = 0f
				}
			};
			BackgroundPlane backgroundPlane2 = obj2;
			levelManager2.AddPlane(obj2);
			backgroundPlane2.Position = instance.Center * FezMath.XZMask - vector * num2 + num * Vector3.UnitY - vector2;
			backgroundPlane2.Billboard = true;
		}
	}

	private void DetermineFloatMalus(PickupState pickup)
	{
		TrileInstance instance = pickup.Instance;
		int num = 0;
		Vector3 vector = CameraManager.Viewpoint.ScreenSpaceMask();
		TrileInstance trileInstance = instance;
		Vector3 b = instance.Center * vector;
		do
		{
			TrileInstance nearLow = PlayerManager.Ground.NearLow;
			TrileInstance farHigh = PlayerManager.Ground.FarHigh;
			TrileInstance heldInstance = PlayerManager.HeldInstance;
			if (nearLow == trileInstance || (nearLow != null && FezMath.AlmostEqual(nearLow.Center * vector, b)) || farHigh == trileInstance || (farHigh != null && FezMath.AlmostEqual(farHigh.Center * vector, b)) || heldInstance == trileInstance || (heldInstance != null && FezMath.AlmostEqual(heldInstance.Center * vector, b)))
			{
				num++;
			}
			if (instance.PhysicsState.PushedDownBy != null)
			{
				num++;
				trileInstance = trileInstance.PhysicsState.PushedDownBy;
			}
			else
			{
				trileInstance = null;
			}
		}
		while (trileInstance != null);
		pickup.FloatMalus = MathHelper.Lerp(pickup.FloatMalus, -0.25f * (float)num, 0.1f);
		if (num == 0 || pickup.Group == null)
		{
			return;
		}
		foreach (TrileInstance trile in pickup.Group.Triles)
		{
			trile.PhysicsState.Puppet = true;
		}
		pickup.Instance.PhysicsState.Puppet = false;
	}

	private static float BreakHeight(ActorType type)
	{
		switch (type)
		{
		case ActorType.PickUp:
		case ActorType.TntPickup:
		case ActorType.SinkPickup:
			return 7f;
		case ActorType.Vase:
			return 1f;
		default:
			throw new InvalidOperationException();
		}
	}

	private void PlayBreakSound(ActorType type, Vector3 position)
	{
		switch (type)
		{
		default:
			_ = 18;
			break;
		case ActorType.Vase:
			vaseBreakSound.EmitAt(position);
			break;
		case ActorType.PickUp:
			break;
		}
	}
}
