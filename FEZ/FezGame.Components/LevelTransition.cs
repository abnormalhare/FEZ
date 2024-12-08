using System;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class LevelTransition : DrawableGameComponent
{
	private enum Phases
	{
		FadeOut,
		LevelChange,
		FadeIn
	}

	private const float FadeSeconds = 1.25f;

	private readonly string ToLevel;

	private Phases Phase;

	private TimeSpan SinceStarted;

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IThreadPool ThreadPool { private get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	public LevelTransition(Game game, string toLevel)
		: base(game)
	{
		ToLevel = toLevel;
		base.DrawOrder = 101;
	}

	public override void Update(GameTime gameTime)
	{
		if (Phase != Phases.LevelChange)
		{
			SinceStarted += gameTime.ElapsedGameTime;
		}
		if (SinceStarted.TotalSeconds > 1.25)
		{
			switch (Phase)
			{
			case Phases.FadeOut:
			{
				Phase = Phases.LevelChange;
				GameState.Loading = true;
				Worker<bool> worker = ThreadPool.Take<bool>(DoLoad);
				worker.Finished += delegate
				{
					ThreadPool.Return(worker);
				};
				worker.Start(context: false);
				break;
			}
			case Phases.FadeIn:
				ServiceHelper.RemoveComponent(this);
				break;
			}
		}
		base.Update(gameTime);
	}

	private void DoLoad(bool dummy)
	{
		LevelManager.ChangeLevel(ToLevel);
		GameState.ScheduleLoadEnd = true;
		Phase = Phases.FadeIn;
		SinceStarted = TimeSpan.Zero;
	}

	public override void Draw(GameTime gameTime)
	{
		double num = SinceStarted.TotalSeconds / 1.25;
		if (Phase == Phases.FadeIn)
		{
			num = 1.0 - num;
		}
		float alpha = FezMath.Saturate(Easing.EaseIn(num, EasingType.Cubic));
		base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
		TargetRenderer.DrawFullscreen(new Color(0f, 0f, 0f, alpha));
	}
}
