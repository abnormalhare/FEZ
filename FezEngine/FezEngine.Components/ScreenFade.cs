using System;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Components;

public class ScreenFade : DrawableGameComponent
{
	public Action Faded;

	public Action ScreenCaptured;

	private Texture capturedScreen;

	private RenderTargetHandle RtHandle;

	private TimeSpan Elapsed;

	public Color FromColor { get; set; }

	public Color ToColor { get; set; }

	public float Duration { get; set; }

	public bool EaseOut { get; set; }

	public EasingType EasingType { get; set; }

	public bool IsDisposed { get; set; }

	public Func<bool> WaitUntil { private get; set; }

	public bool CaptureScreen { get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	public ScreenFade(Game game)
		: base(game)
	{
		base.DrawOrder = 1000;
		EasingType = EasingType.Cubic;
	}

	public override void Initialize()
	{
		base.Initialize();
		if (CaptureScreen)
		{
			RtHandle = TargetRenderer.TakeTarget();
			TargetRenderer.ScheduleHook(base.DrawOrder, RtHandle.Target);
		}
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (RtHandle != null)
		{
			TargetRenderer.ReturnTarget(RtHandle);
			RtHandle = null;
		}
		Faded = null;
		ScreenCaptured = null;
		WaitUntil = null;
		capturedScreen = null;
		IsDisposed = true;
	}

	public override void Draw(GameTime gameTime)
	{
		if (RtHandle != null && TargetRenderer.IsHooked(RtHandle.Target))
		{
			TargetRenderer.Resolve(RtHandle.Target, reschedule: false);
			capturedScreen = RtHandle.Target;
			if (ScreenCaptured != null)
			{
				ScreenCaptured();
			}
		}
		base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
		if (capturedScreen != null)
		{
			base.GraphicsDevice.SetupViewport();
			TargetRenderer.DrawFullscreen(capturedScreen);
		}
		Elapsed += gameTime.ElapsedGameTime;
		float num = (float)Elapsed.TotalSeconds / Duration;
		num = FezMath.Saturate(EaseOut ? Easing.EaseOut(num, EasingType) : Easing.EaseIn(num, EasingType));
		if (num == 1f && (WaitUntil == null || WaitUntil()))
		{
			if (Faded != null)
			{
				Faded();
				Faded = null;
			}
			WaitUntil = null;
			ServiceHelper.RemoveComponent(this);
		}
		TargetRenderer.DrawFullscreen(Color.Lerp(FromColor, ToColor, num));
	}
}
