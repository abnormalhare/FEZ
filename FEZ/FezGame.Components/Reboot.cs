using System;
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

public class Reboot : DrawableGameComponent
{
	private const float WaitTime = 3f;

	private const float TimeUntilLogo = 1f;

	private const float TimeUntilBootup = 4f;

	private Texture2D BootTexture;

	private Texture2D LaserCheckTexture;

	private RebootPOSTEffect effect;

	private TimeSpan SinceCreated;

	private SoundEffect RebootSound;

	private bool hasPlayedSound;

	private readonly string ToLevel = "GOMEZ_INTERIOR_3D";

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public ITimeManager TimeManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public Reboot(Game game, string toLevel)
		: base(game)
	{
		if (toLevel != null)
		{
			ToLevel = toLevel;
		}
		base.DrawOrder = 1005;
	}

	protected override void LoadContent()
	{
		ContentManager contentManager = CMProvider.Get(CM.Reboot);
		BootTexture = contentManager.Load<Texture2D>("Other Textures/reboot/boot");
		LaserCheckTexture = contentManager.Load<Texture2D>("Other Textures/reboot/lasercheck");
		RebootSound = contentManager.Load<SoundEffect>("Sounds/Intro/Reboot");
		effect = new RebootPOSTEffect();
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		effect.Dispose();
		CMProvider.Dispose(CM.Reboot);
	}

	public override void Update(GameTime gameTime)
	{
		if (!GameState.Loading && !GameState.Paused && SinceCreated.TotalSeconds > 7.0)
		{
			if (GameState.InCutscene && Intro.Instance != null)
			{
				ServiceHelper.RemoveComponent(Intro.Instance);
			}
			Intro component = new Intro(base.Game)
			{
				Fake = true,
				FakeLevel = ToLevel,
				Glitch = true
			};
			TimeManager.TimeFactor = TimeManager.DefaultTimeFactor;
			TimeManager.CurrentTime = DateTime.Now;
			ServiceHelper.AddComponent(component);
			Waiters.Wait(0.10000000149011612, delegate
			{
				ServiceHelper.RemoveComponent(this);
			});
			base.Enabled = false;
		}
	}

	public override void Draw(GameTime gameTime)
	{
		SinceCreated += gameTime.ElapsedGameTime;
		base.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
		if (SinceCreated.TotalSeconds > 3.0)
		{
			if (!hasPlayedSound)
			{
				RebootSound.Emit();
				hasPlayedSound = true;
			}
			float num = (float)SinceCreated.TotalSeconds - 3f;
			float viewScale = base.GraphicsDevice.GetViewScale();
			float num2 = base.GraphicsDevice.Viewport.Width;
			float num3 = base.GraphicsDevice.Viewport.Height;
			float num4 = BootTexture.Width;
			float num5 = BootTexture.Height;
			Matrix textureMatrix = new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, -0.5f, -0.5f, 1f, 0f, 0f, 0f, 0f, 0f) * new Matrix(num2 / num4 / 2f / viewScale, 0f, 0f, 0f, 0f, num3 / num5 / 2f / viewScale, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f) * new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0.56f, 0.5f, 1f, 0f, 0f, 0f, 0f, 0f);
			TargetRenderer.DrawFullscreen(BootTexture, textureMatrix);
			float num6 = num / 4f;
			float num7 = ((num6 < 0.2f) ? 0.22f : ((num6 < 0.4f) ? 0.293f : ((num6 < 0.6f) ? 0.361f : ((num6 < 0.7f) ? 0.528f : ((num6 < 0.8f) ? 0.7f : 1f)))));
			effect.PseudoWorld = new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, (0f - num7) * 2f, 0f, 1f);
			TargetRenderer.DrawFullscreen(effect, Color.Black);
			if (num > 1f)
			{
				num4 = LaserCheckTexture.Width;
				num5 = LaserCheckTexture.Height;
				textureMatrix = new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, -0.5f, -0.5f, 1f, 0f, 0f, 0f, 0f, 0f) * new Matrix(num2 / num4 / 2f / viewScale, 0f, 0f, 0f, 0f, num3 / num5 / 2f / viewScale, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f) * new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, -1.15f, 1f, 1f, 0f, 0f, 0f, 0f, 0f);
				TargetRenderer.DrawFullscreen(LaserCheckTexture, textureMatrix);
			}
		}
		else
		{
			TargetRenderer.DrawFullscreen(Color.Black);
		}
	}
}
