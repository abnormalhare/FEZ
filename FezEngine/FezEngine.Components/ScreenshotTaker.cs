using System.IO;
using FezEngine.Services;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FezEngine.Components;

public class ScreenshotTaker : DrawableGameComponent
{
	private int counter;

	private bool screenshotScheduled;

	private RenderTargetHandle rt;

	[ServiceDependency]
	public IKeyboardStateManager KeyboardProvider { private get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TRM { private get; set; }

	public ScreenshotTaker(Game game)
		: base(game)
	{
		base.DrawOrder = 32767;
	}

	public override void Initialize()
	{
		base.Initialize();
		KeyboardProvider.RegisterKey(Keys.F2);
	}

	public override void Update(GameTime gameTime)
	{
		screenshotScheduled |= KeyboardProvider.GetKeyState(Keys.F2) == FezButtonState.Pressed;
		if (screenshotScheduled)
		{
			rt = TRM.TakeTarget();
			TRM.ScheduleHook(base.DrawOrder, rt.Target);
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (screenshotScheduled && rt != null && TRM.IsHooked(rt.Target))
		{
			TRM.Resolve(rt.Target, reschedule: false);
			using (FileStream stream = new FileStream($"C:\\Screenshot_{counter++:000}.png", FileMode.Create))
			{
				rt.Target.SaveAsPng(stream, base.GraphicsDevice.Viewport.Width, base.GraphicsDevice.Viewport.Height);
			}
			TRM.ReturnTarget(rt);
			rt = null;
			screenshotScheduled = false;
		}
	}
}
