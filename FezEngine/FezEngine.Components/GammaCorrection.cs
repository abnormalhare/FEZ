using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Components;

public class GammaCorrection : DrawableGameComponent
{
	private RenderTargetHandle rtHandle;

	private GammaCorrectionEffect gammaCorrectionEffect;

	private bool isHooked;

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderingManager { protected get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { protected get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { protected get; set; }

	public GammaCorrection(Game game)
		: base(game)
	{
		base.DrawOrder = int.MaxValue;
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		DrawActionScheduler.Schedule(delegate
		{
			gammaCorrectionEffect = new GammaCorrectionEffect();
			isHooked = false;
		});
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		if (!EngineState.Loading)
		{
			if (isHooked && (double)SettingsManager.Settings.Brightness == 0.5)
			{
				TargetRenderingManager.UnscheduleHook(rtHandle.Target);
				TargetRenderingManager.ReturnTarget(rtHandle);
				rtHandle = null;
				isHooked = false;
			}
			else if (!isHooked && (double)SettingsManager.Settings.Brightness != 0.5)
			{
				rtHandle = TargetRenderingManager.TakeTarget();
				TargetRenderingManager.ScheduleHook(base.DrawOrder, rtHandle.Target);
				isHooked = true;
			}
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (rtHandle != null && TargetRenderingManager.IsHooked(rtHandle.Target))
		{
			TargetRenderingManager.Resolve(rtHandle.Target, reschedule: true);
			gammaCorrectionEffect.MainBufferTexture = rtHandle.Target;
			gammaCorrectionEffect.Brightness = SettingsManager.Settings.Brightness;
			base.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 0f, 0);
			base.GraphicsDevice.SetupViewport();
			TargetRenderingManager.DrawFullscreen(gammaCorrectionEffect);
		}
	}
}
