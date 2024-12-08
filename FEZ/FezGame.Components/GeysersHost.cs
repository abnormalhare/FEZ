using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components;

public class GeysersHost : GameComponent
{
	private class GeyserState
	{
		private const float GeyserFallSpeed = 5f;

		private readonly GeysersHost Host;

		private readonly TrileGroup Group;

		private readonly BackgroundPlane TilePlane;

		private readonly BackgroundPlane TopPlane;

		private TimeSpan SinceStateChange;

		private TimeSpan SinceStartedLift;

		private bool Lifting;

		private bool ReachedApex;

		private float ReachedAtTime;

		private float InitialHeight;

		private SoundEmitter loopEmitter;

		private float heightDelta;

		[ServiceDependency]
		public ILevelMaterializer LevelMaterializer { private get; set; }

		[ServiceDependency]
		public IDefaultCameraManager CameraManager { private get; set; }

		[ServiceDependency]
		public IGameLevelManager LevelManager { private get; set; }

		public GeyserState(TrileGroup group, GeysersHost host)
		{
			ServiceHelper.InjectServices(this);
			Host = host;
			Group = group;
			SinceStateChange = TimeSpan.FromSeconds(0f - group.GeyserOffset);
			Vector3 position = Group.Triles.Aggregate(Vector3.Zero, (Vector3 a, TrileInstance b) => a + b.Center) / group.Triles.Count;
			TopPlane = new BackgroundPlane(LevelMaterializer.AnimatedPlanesMesh, "sewer/sewer_geyser_top", animated: true)
			{
				Position = position,
				ClampTexture = true,
				Crosshatch = true,
				Doublesided = true
			};
			LevelManager.AddPlane(TopPlane);
			TilePlane = new BackgroundPlane(LevelMaterializer.AnimatedPlanesMesh, "sewer/sewer_geyser_tile", animated: true)
			{
				Position = position,
				YTextureRepeat = true,
				Billboard = true
			};
			LevelManager.AddPlane(TilePlane);
			TopPlane.Timing.Step = TilePlane.Timing.Step;
			loopEmitter = Host.LoopSound.EmitAt(position, loop: true, 0f, 0f);
			foreach (TrileInstance trile in group.Triles)
			{
				trile.PhysicsState.IgnoreCollision = true;
				trile.PhysicsState.IgnoreClampToWater = true;
			}
		}

		public void Update(TimeSpan elapsed)
		{
			SinceStateChange += elapsed;
			if (SinceStateChange.Ticks >= 0)
			{
				Lifting = !Lifting;
				if (Lifting)
				{
					foreach (TrileInstance trile in Group.Triles)
					{
						trile.PhysicsState.Floating = false;
						if (!trile.PhysicsState.Puppet)
						{
							InitialHeight = trile.PhysicsState.Center.Y;
						}
					}
					ReachedApex = false;
					SinceStartedLift = TimeSpan.Zero;
					SinceStateChange = TimeSpan.FromSeconds(0f - Group.GeyserLiftFor);
				}
				else
				{
					foreach (TrileInstance trile2 in Group.Triles)
					{
						trile2.PhysicsState.Floating = false;
					}
					SinceStateChange = TimeSpan.FromSeconds(0f - Group.GeyserPauseFor);
					IWaiter waiter = Waiters.Interpolate(Group.GeyserApexHeight / 2f, delegate(float s)
					{
						heightDelta -= s * 1.25f;
					});
					waiter.AutoPause = true;
					waiter.CustomPause = () => !CameraManager.ViewTransitionReached;
				}
			}
			if (Lifting)
			{
				SinceStartedLift += elapsed;
				double num = SinceStartedLift.TotalSeconds / Math.Sqrt(Group.GeyserApexHeight) * 1.5;
				float num2 = Easing.EaseInOut(num, EasingType.Quadratic, EasingType.Sine);
				loopEmitter.VolumeFactor = (float)FezMath.Saturate(num);
				if (!ReachedApex && num >= 1.0)
				{
					ReachedApex = true;
					ReachedAtTime = (float)SinceStateChange.TotalSeconds;
				}
				if (ReachedApex)
				{
					heightDelta = Group.GeyserApexHeight + (float)Math.Sin((SinceStateChange.TotalSeconds - (double)ReachedAtTime) * 4.0) * 1.5f / 16f;
				}
				else
				{
					heightDelta = Group.GeyserApexHeight * num2;
				}
				foreach (TrileInstance trile3 in Group.Triles)
				{
					trile3.PhysicsState.PushedUp = true;
					if (ReachedApex)
					{
						Vector3 center = trile3.PhysicsState.Center;
						trile3.PhysicsState.Center = trile3.PhysicsState.Center * FezMath.XZMask + (InitialHeight + heightDelta) * Vector3.UnitY;
						trile3.PhysicsState.Velocity = trile3.PhysicsState.Center - center;
						trile3.PhysicsState.Center = center;
					}
					else
					{
						Vector3 center2 = trile3.PhysicsState.Center;
						trile3.PhysicsState.Center = trile3.PhysicsState.Center * FezMath.XZMask + (InitialHeight + heightDelta) * Vector3.UnitY;
						trile3.PhysicsState.Velocity = trile3.PhysicsState.Center - center2;
						trile3.PhysicsState.Center = center2;
					}
					trile3.PhysicsState.Velocity += 0.47250003f * (float)elapsed.TotalSeconds * Vector3.Up;
				}
			}
			else
			{
				foreach (TrileInstance trile4 in Group.Triles)
				{
					trile4.PhysicsState.PushedUp = false;
					if (!trile4.PhysicsState.Floating && trile4.PhysicsState.Center.Y - LevelManager.WaterHeight > 0f)
					{
						trile4.PhysicsState.Velocity += 0.47250003f * (float)elapsed.TotalSeconds * Vector3.Up / 2f;
					}
				}
				loopEmitter.VolumeFactor = heightDelta / Group.GeyserApexHeight;
			}
			TopPlane.Timing.Step = TilePlane.Timing.Step;
			float num3 = heightDelta + InitialHeight - 1f - LevelManager.WaterHeight;
			TopPlane.Scale = new Vector3(1f, FezMath.Saturate(num3 + 1f), 1f);
			TopPlane.Position = FezMath.XZMask * TopPlane.Position + (LevelManager.WaterHeight + num3 + (1f - TopPlane.Scale.Y) / 2f) * Vector3.UnitY;
			TopPlane.Visible = TopPlane.Scale.Y > 0f;
			if (num3 <= 0f)
			{
				num3 = 0f;
			}
			TilePlane.Scale = new Vector3(1f, num3 / TilePlane.Size.Y, 1f);
			TilePlane.Position = FezMath.XZMask * TilePlane.Position + (LevelManager.WaterHeight + num3 / 2f - 0.5f) * Vector3.UnitY;
			TilePlane.Visible = TilePlane.Scale.Y > 0f;
			loopEmitter.Position = TopPlane.Position;
		}
	}

	private readonly List<GeyserState> Geysers = new List<GeyserState>();

	private SoundEffect LoopSound;

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public GeysersHost(Game game)
		: base(game)
	{
		base.UpdateOrder = -2;
	}

	public override void Initialize()
	{
		LoopSound = CMProvider.Global.Load<SoundEffect>("Sounds/Sewer/GeyserLoop");
		LevelManager.LevelChanged += TryInitialize;
	}

	private void TryInitialize()
	{
		Geysers.Clear();
		foreach (TrileGroup item in LevelManager.Groups.Values.Where((TrileGroup x) => x.ActorType == ActorType.Geyser))
		{
			Geysers.Add(new GeyserState(item, this));
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused || GameState.InMap || !CameraManager.ActionRunning || !CameraManager.Viewpoint.IsOrthographic() || !CameraManager.ViewTransitionReached)
		{
			return;
		}
		foreach (GeyserState geyser in Geysers)
		{
			geyser.Update(gameTime.ElapsedGameTime);
		}
	}
}
