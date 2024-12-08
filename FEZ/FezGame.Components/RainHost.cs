using System;
using System.Linq;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class RainHost : DrawableGameComponent
{
	public class RainTransition : DrawableGameComponent
	{
		public readonly PlaneParticleSystem RainPS;

		[ServiceDependency]
		public IDefaultCameraManager CameraManager { private get; set; }

		public RainTransition(Game game, PlaneParticleSystem rainPS)
			: base(game)
		{
			RainPS = rainPS;
		}

		public override void Update(GameTime gameTime)
		{
			RainPS.HalfUpdate = true;
			RainPS.Update(gameTime.ElapsedGameTime);
			RainPS.HalfUpdate = false;
		}

		public override void Draw(GameTime gameTime)
		{
			base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
			RainPS.Draw();
		}
	}

	private readonly Color RainColor = new Color(145, 182, 255, 32);

	private PlaneParticleSystem RainPS;

	private RainTransition Transition;

	private int flashes;

	private float flashSpeed;

	private float flashOpacity;

	private TimeSpan flashIn;

	private TimeSpan sinceFlash;

	private TimeSpan distantIn;

	private Mesh lightning;

	private float lightningSideOffset;

	private TrileInstance[] trileTops;

	private SoundEffect[] lightningSounds;

	private SoundEffect[] thunderDistant;

	private SoundEffect sPreThunder;

	private SoundEmitter ePreThunder;

	private bool doThunder;

	private Vector3 transitionCenter;

	private string LastLevelName;

	public static RainHost Instance;

	private static Func<Vector3, Vector3, Vector3> RainScaling => (Vector3 b, Vector3 v) => new Vector3(0.0625f, RandomHelper.Between(48.0, 96.0) / 16f, 0.0625f);

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IPlaneParticleSystems PlaneSystems { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public ITargetRenderingManager ImmediateRenderer { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public RainHost(Game game)
		: base(game)
	{
		Instance = this;
	}

	public override void Initialize()
	{
		base.Initialize();
		base.DrawOrder = 500;
		base.Enabled = false;
		LevelManager.LevelChanging += TryKeepSounds;
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void TryKeepSounds()
	{
		if (base.Enabled && LevelManager.Name != LastLevelName)
		{
			ContentManager global = CMProvider.Global;
			global.Load<SoundEffect>("Sounds/Graveyard/Thunder1");
			global.Load<SoundEffect>("Sounds/Graveyard/Thunder2");
			global.Load<SoundEffect>("Sounds/Graveyard/Thunder3");
			global.Load<SoundEffect>("Sounds/Graveyard/Thunder4");
			global.Load<SoundEffect>("Sounds/Graveyard/ThunderDistant01");
			global.Load<SoundEffect>("Sounds/Graveyard/ThunderDistant02");
			global.Load<SoundEffect>("Sounds/Graveyard/ThunderDistant03");
			global.Load<SoundEffect>("Sounds/Graveyard/ThunderRumble");
			if (!LevelManager.Rainy)
			{
				lightningSounds = null;
				sPreThunder = null;
			}
		}
		LastLevelName = LevelManager.Name;
	}

	private void TryInitialize()
	{
		RainPS = null;
		ePreThunder = null;
		if (lightning != null)
		{
			lightning.Dispose();
		}
		lightning = null;
		trileTops = null;
		bool visible = (base.Enabled = LevelManager.Rainy);
		base.Visible = visible;
		if (!base.Enabled)
		{
			return;
		}
		doThunder = LevelManager.Name != "INDUSTRIAL_CITY";
		if (Transition == null)
		{
			Vector3 vector = RainColor.ToVector3();
			PlaneParticleSystemSettings settings = new PlaneParticleSystemSettings
			{
				Velocity = new Vector3(0f, -50f, 0f),
				SpawningSpeed = 60f,
				ParticleLifetime = 0.6f,
				SpawnBatchSize = 10,
				SizeBirth = new VaryingVector3
				{
					Function = RainScaling
				},
				FadeOutDuration = 0.1f,
				ColorLife = new VaryingColor
				{
					Base = RainColor,
					Variation = new Color(0, 0, 0, 24)
				},
				ColorDeath = new Color(vector.X, vector.Y, vector.Z, 0f),
				ColorBirth = new Color(vector.X, vector.Y, vector.Z, 0f),
				BlendingMode = BlendingMode.Alphablending,
				Billboarding = true,
				NoLightDraw = true
			};
			PlaneSystems.Add(RainPS = new PlaneParticleSystem(base.Game, 500, settings));
			DrawActionScheduler.Schedule(delegate
			{
				RainPS.Settings.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/rain/rain");
				RainPS.RefreshTexture();
			});
			for (int i = 0; i < 75; i++)
			{
				PlaneSystems.RainSplash(Vector3.Zero).FadeOutAndDie(0f);
			}
		}
		ContentManager contentManager = ((LevelManager.Name == null) ? CMProvider.Global : CMProvider.CurrentLevel);
		lightning = new Mesh();
		DrawActionScheduler.Schedule(delegate
		{
			lightning.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/rain/lightning_a");
			lightning.Effect = new DefaultEffect.Textured();
		});
		lightning.AddFace(Vector3.One * new Vector3(16f, 32f, 16f), Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true, doublesided: true);
		if (lightningSounds == null)
		{
			lightningSounds = new SoundEffect[4]
			{
				contentManager.Load<SoundEffect>("Sounds/Graveyard/Thunder1"),
				contentManager.Load<SoundEffect>("Sounds/Graveyard/Thunder2"),
				contentManager.Load<SoundEffect>("Sounds/Graveyard/Thunder3"),
				contentManager.Load<SoundEffect>("Sounds/Graveyard/Thunder4")
			};
		}
		if (thunderDistant == null)
		{
			thunderDistant = new SoundEffect[3]
			{
				contentManager.Load<SoundEffect>("Sounds/Graveyard/ThunderDistant01"),
				contentManager.Load<SoundEffect>("Sounds/Graveyard/ThunderDistant02"),
				contentManager.Load<SoundEffect>("Sounds/Graveyard/ThunderDistant03")
			};
		}
		if (sPreThunder == null)
		{
			sPreThunder = contentManager.Load<SoundEffect>("Sounds/Graveyard/ThunderRumble");
		}
		trileTops = LevelManager.Triles.Values.Where((TrileInstance x) => x.Enabled && !x.Trile.Immaterial && !x.Trile.ActorSettings.Type.IsClimbable() && (x.Trile.Geometry == null || !x.Trile.Geometry.Empty || x.Trile.Faces[FaceOrientation.Front] != CollisionType.None) && NoTop(x)).ToArray();
		flashIn = TimeSpan.FromSeconds(RandomHelper.Between(4.0, 6.0));
		distantIn = TimeSpan.FromSeconds(RandomHelper.Between(3.0, 6.0));
	}

	private bool NoTop(TrileInstance instance)
	{
		FaceOrientation face = FaceOrientation.Top;
		TrileEmplacement id = instance.Emplacement.GetTraversal(ref face);
		TrileInstance trileInstance = LevelManager.TrileInstanceAt(ref id);
		if (trileInstance != null && trileInstance.Enabled)
		{
			return trileInstance.Trile.Immaterial;
		}
		return true;
	}

	public void ForceFlash()
	{
		flashIn = TimeSpan.Zero;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading)
		{
			return;
		}
		if (Transition != null)
		{
			if (GameState.FarawaySettings.InTransition)
			{
				if (!GameState.Paused && GameState.FarawaySettings.DestinationCrossfadeStep > 0f)
				{
					SpawnSplashes();
				}
				return;
			}
			RainPS = Transition.RainPS;
			PlaneSystems.Add(RainPS);
			RainPS.MoveActiveParticles(transitionCenter - CameraManager.Center);
			RainPS.SetViewProjectionSticky(enabled: false);
			ServiceHelper.RemoveComponent(Transition);
			Transition = null;
		}
		Vector3 center = CameraManager.Center;
		float num = (float)base.GraphicsDevice.Viewport.Width / (1280f * base.GraphicsDevice.GetViewScale());
		int num2 = 60;
		Vector3 vector = new Vector3((float)num2 * num, (float)num2 / CameraManager.AspectRatio, (float)num2 * num) / 2f;
		RainPS.Settings.SpawnVolume = new BoundingBox
		{
			Min = center - vector * FezMath.XZMask,
			Max = center + vector * new Vector3(1f, 2f, 1f)
		};
		if (!GameState.Paused && CameraManager.ActionRunning && !GameState.InMenuCube && !GameState.InMap && !GameState.InFpsMode)
		{
			SpawnSplashes();
		}
	}

	private void SpawnSplashes()
	{
		int num = 0;
		int num2 = 0;
		while (num2 < 3 && num < 25)
		{
			TrileInstance trileInstance = RandomHelper.InList(trileTops);
			if (trileInstance.InstanceId >= 0 && CameraManager.Frustum.Contains(trileInstance.Center) != 0)
			{
				PlaneSystems.RainSplash(new Vector3(RandomHelper.Unit() / 2f, 0.5f, RandomHelper.Unit() / 2f) * trileInstance.TransformedSize + trileInstance.Center);
				num2++;
			}
			else
			{
				num++;
			}
		}
		if (RandomHelper.Probability(0.009999999776482582))
		{
			PlaneSystems.RainSplash(new Vector3(RandomHelper.Unit() / 2f, 0.625f, RandomHelper.Unit() / 2f) * PlayerManager.Size + PlayerManager.Center);
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (GameState.Loading)
		{
			return;
		}
		TimeSpan elapsedGameTime = gameTime.ElapsedGameTime;
		if (!GameState.Paused && CameraManager.Viewpoint.IsOrthographic() && CameraManager.ActionRunning && !GameState.InMap)
		{
			flashIn -= elapsedGameTime;
			distantIn -= elapsedGameTime;
		}
		if (!doThunder)
		{
			return;
		}
		if (distantIn.Ticks <= 0)
		{
			distantIn = TimeSpan.FromSeconds(RandomHelper.Between(3.0, 6.0));
			Vector3 position = CameraManager.Center + LevelManager.Size / 2f * CameraManager.Viewpoint.ForwardVector() + RandomHelper.Centered(1.0) * CameraManager.Radius * 0.5f * CameraManager.Viewpoint.RightVector() + CameraManager.Center * Vector3.UnitY;
			RandomHelper.InList(thunderDistant).EmitAt(position, RandomHelper.Centered(0.05000000074505806), RandomHelper.Between(0.75, 1.0)).NoAttenuation = true;
		}
		if (flashIn.Ticks <= 0)
		{
			flashes = RandomHelper.Random.Next(1, 3);
			sinceFlash = TimeSpan.FromSeconds(-0.20000000298023224);
			flashSpeed = RandomHelper.Between(1.5, 3.5);
			flashOpacity = RandomHelper.Between(0.2, 0.4);
			flashIn = TimeSpan.FromSeconds(RandomHelper.Between(4.0, 6.0));
			lightning.Rotation = CameraManager.Rotation;
			lightningSideOffset = RandomHelper.Centered(1.0);
			lightning.Position = CameraManager.Center + LevelManager.Size / 2f * CameraManager.Viewpoint.ForwardVector() + lightningSideOffset * CameraManager.Radius * 0.5f * CameraManager.Viewpoint.RightVector() + RandomHelper.Centered(10.0) * Vector3.UnitY;
			if (ePreThunder != null)
			{
				ePreThunder.Cue.Stop();
				ePreThunder = null;
			}
			Vector3 position2 = lightning.Position * FezMath.XZMask + CameraManager.Center * Vector3.UnitY;
			(ePreThunder = sPreThunder.EmitAt(position2, RandomHelper.Centered(0.02500000037252903))).NoAttenuation = true;
		}
		if (sinceFlash.TotalSeconds < 0.20000000298023224)
		{
			float num = Easing.EaseOut(FezMath.Saturate(1f - (float)(sinceFlash.TotalSeconds / 0.20000000298023224)), EasingType.Quadratic);
			num *= flashOpacity;
			base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.Sky);
			ImmediateRenderer.DrawFullscreen(new Color(1f, 1f, 1f, num));
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.Trails);
			ImmediateRenderer.DrawFullscreen(new Color(1f, 1f, 1f, num));
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.SkyLayer1);
			ImmediateRenderer.DrawFullscreen(new Color(0.9f, 0.9f, 0.9f, num));
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.SkyLayer2);
			ImmediateRenderer.DrawFullscreen(new Color(0.8f, 0.8f, 0.8f, num));
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.SkyLayer3);
			ImmediateRenderer.DrawFullscreen(new Color(0.7f, 0.7f, 0.7f, num));
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Less, StencilMask.SkyLayer3);
			ImmediateRenderer.DrawFullscreen(new Color(0f, 0f, 0f, num));
			sinceFlash += TimeSpan.FromTicks((long)((float)elapsedGameTime.Ticks * flashSpeed));
			if (flashes == 1 && (double)(num / flashOpacity) < 0.5)
			{
				lightning.Material.Opacity = 1f;
				base.GraphicsDevice.PrepareStencilRead(CompareFunction.NotEqual, StencilMask.SkyLayer3);
				lightning.Draw();
			}
			else if (flashes == 0 && sinceFlash.Ticks < 0)
			{
				lightning.Material.Opacity = (float)(sinceFlash.TotalSeconds / -0.20000000298023224);
				base.GraphicsDevice.PrepareStencilRead(CompareFunction.NotEqual, StencilMask.SkyLayer3);
				lightning.Draw();
			}
			base.GraphicsDevice.PrepareStencilWrite(StencilMask.None);
		}
		if (!(sinceFlash.TotalSeconds > 0.20000000298023224) || flashes == 0)
		{
			return;
		}
		flashes--;
		sinceFlash = TimeSpan.FromSeconds(-0.20000000298023224);
		flashSpeed = ((flashes == 0) ? 1f : RandomHelper.Between(2.0, 4.0));
		flashOpacity = ((flashes == 0) ? 1f : RandomHelper.Between(0.2, 0.4));
		if (flashes == 0)
		{
			if (ePreThunder != null && !ePreThunder.Dead)
			{
				ePreThunder.Cue.Stop();
			}
			ePreThunder = null;
			Vector3 position3 = lightning.Position * FezMath.XZMask + CameraManager.Center * Vector3.UnitY;
			SoundEmitter emitter = RandomHelper.InList(lightningSounds).EmitAt(position3, RandomHelper.Centered(0.02500000037252903));
			emitter.NoAttenuation = true;
			emitter.Persistent = true;
			Waiters.DoUntil(() => emitter.Dead, delegate
			{
				emitter.Position = Vector3.Lerp(emitter.Position, CameraManager.Center, 0.025f);
			}).AutoPause = true;
		}
	}

	public void StartTransition()
	{
		ServiceHelper.AddComponent(Transition = new RainTransition(base.Game, RainPS));
		PlaneSystems.Remove(RainPS, returnToPool: false);
		RainPS.SetViewProjectionSticky(enabled: true);
		transitionCenter = CameraManager.Center;
	}
}
