using System;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class GameLevelHost : LevelHost
{
	private CombineEffect combineEffect;

	private bool needsReset;

	private RenderTargetHandle backgroundRth;

	private RenderTargetHandle rightRT;

	private RenderTargetHandle leftRT;

	public static GameLevelHost Instance;

	[ServiceDependency]
	public IPlaneParticleSystems PlanePS { private get; set; }

	[ServiceDependency]
	public ITrixelParticleSystems TrixelPS { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public new IGameCameraManager CameraManager { private get; set; }

	public GameLevelHost(Game game)
		: base(game)
	{
		Instance = this;
	}

	public override void Initialize()
	{
		base.Initialize();
		DrawActionScheduler.Schedule(delegate
		{
			combineEffect = new CombineEffect
			{
				RedGamma = 1f
			};
		});
		base.LevelManager.LevelChanged += delegate
		{
			if (base.LevelManager.WaterType == LiquidType.Lava)
			{
				combineEffect.LeftFilter = new Matrix(1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
				combineEffect.RightFilter = new Matrix(0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
			}
			else
			{
				combineEffect.LeftFilter = new Matrix(0.2125f, 0.7154f, 0.0721f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);
				combineEffect.RightFilter = new Matrix(0f, 0f, 0f, 0f, 0.2125f, 0.7154f, 0.0721f, 0f, 0.2125f, 0.7154f, 0.0721f, 0f, 0f, 0f, 0f, 0f);
			}
		};
	}

	public override void Draw(GameTime gameTime)
	{
		if (GameState.Loading || GameState.SkipRendering)
		{
			if (backgroundRth != null && base.TargetRenderer.IsHooked(backgroundRth.Target))
			{
				base.TargetRenderer.Resolve(backgroundRth.Target, reschedule: true);
				base.GraphicsDevice.SetBlendingMode(BlendingMode.Opaque);
				CombineEffect obj = combineEffect;
				Texture2D leftTexture = (combineEffect.RightTexture = backgroundRth.Target);
				obj.LeftTexture = leftTexture;
				base.TargetRenderer.DrawFullscreen(combineEffect);
				base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
			}
		}
		else if (!GameState.StereoMode)
		{
			if (rightRT != null || leftRT != null)
			{
				base.TargetRenderer.ReturnTarget(leftRT);
				base.TargetRenderer.ReturnTarget(rightRT);
				leftRT = (rightRT = null);
			}
			if (backgroundRth != null)
			{
				base.TargetRenderer.Resolve(backgroundRth.Target, reschedule: false);
				base.GraphicsDevice.SetBlendingMode(BlendingMode.Opaque);
				base.TargetRenderer.DrawFullscreen(backgroundRth.Target, Matrix.Identity);
				base.TargetRenderer.ReturnTarget(backgroundRth);
				base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
			}
			backgroundRth = null;
			if (needsReset)
			{
				BaseEffect.EyeSign = Vector3.Zero;
			}
			DoDraw();
		}
		else
		{
			if (rightRT == null || leftRT == null)
			{
				rightRT = base.TargetRenderer.TakeTarget();
				leftRT = base.TargetRenderer.TakeTarget();
			}
			if (backgroundRth == null)
			{
				backgroundRth = base.TargetRenderer.TakeTarget();
				base.TargetRenderer.ScheduleHook(base.DrawOrder, backgroundRth.Target);
			}
			else
			{
				needsReset = true;
				DoStereo(base.LevelManager.Size / 2f, base.LevelManager.Size, DoFullDraw, gameTime);
			}
		}
	}

	private void DoFullDraw(GameTime gameTime)
	{
		DoDraw();
		GomezHost.Instance.DoDraw_Internal(gameTime);
		PlanePS.ForceDraw();
		TrixelPS.ForceDraw();
		if (LiquidHost.Instance.Visible)
		{
			LiquidHost.Instance.DoDraw();
		}
		BlackHolesHost.Instance.DoDraw();
		WarpGateHost.Instance.DoDraw();
	}

	public void DoStereo(Vector3 center, Vector3 size, Action<GameTime> drawMethod, GameTime gameTime, Texture backgroundTexture = null)
	{
		if (backgroundTexture == null)
		{
			base.TargetRenderer.Resolve(backgroundRth.Target, reschedule: true);
			backgroundTexture = backgroundRth.Target;
		}
		if (GameState.FarawaySettings.InTransition && GameState.FarawaySettings.SkyRt != null && base.GraphicsDevice.GetRenderTargets().Length != 0)
		{
			DoStereoTransition(center, size, drawMethod, gameTime);
			return;
		}
		float num = (size * CameraManager.InverseView.Forward).Length() * 1.5f;
		BaseEffect.LevelCenter = center;
		RenderTargetBinding[] renderTargets = base.GraphicsDevice.GetRenderTargets();
		RenderTarget2D renderTarget = ((renderTargets.Length == 0) ? null : (renderTargets[0].RenderTarget as RenderTarget2D));
		base.GraphicsDevice.SetRenderTarget(leftRT.Target);
		base.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Black, 1f, 0);
		base.GraphicsDevice.PrepareDraw();
		BaseEffect.EyeSign = -1f * CameraManager.InverseView.Right / num;
		base.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
		base.GraphicsDevice.PrepareStencilWrite(StencilMask.Sky);
		base.GraphicsDevice.SetBlendingMode(BlendingMode.Opaque);
		base.TargetRenderer.DrawFullscreen(backgroundTexture, new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0.005f, 0f, 1f, 0f, 0f, 0f, 0f, 1f));
		base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
		drawMethod(gameTime);
		base.GraphicsDevice.SetRenderTarget(rightRT.Target);
		base.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Black, 1f, 0);
		base.GraphicsDevice.PrepareDraw();
		BaseEffect.EyeSign = CameraManager.InverseView.Right / num;
		base.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
		base.GraphicsDevice.PrepareStencilWrite(StencilMask.Sky);
		base.GraphicsDevice.SetBlendingMode(BlendingMode.Opaque);
		base.TargetRenderer.DrawFullscreen(backgroundTexture, new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, -0.005f, 0f, 1f, 0f, 0f, 0f, 0f, 1f));
		base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
		drawMethod(gameTime);
		base.GraphicsDevice.SetRenderTarget(renderTarget);
		base.GraphicsDevice.PrepareDraw();
		base.GraphicsDevice.SetBlendingMode(BlendingMode.Opaque);
		combineEffect.LeftTexture = leftRT.Target;
		combineEffect.RightTexture = rightRT.Target;
		base.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 1f, 0);
		base.GraphicsDevice.SetupViewport();
		base.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
		base.GraphicsDevice.SamplerStates[1] = SamplerState.PointClamp;
		base.TargetRenderer.DrawFullscreen(combineEffect);
		base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
	}

	private void DoStereoTransition(Vector3 center, Vector3 size, Action<GameTime> drawMethod, GameTime gameTime)
	{
		float num = (size * CameraManager.InverseView.Forward).Length() * 1.5f;
		BaseEffect.LevelCenter = center;
		RenderTarget2D renderTarget = base.GraphicsDevice.GetRenderTargets()[0].RenderTarget as RenderTarget2D;
		base.GraphicsDevice.SetRenderTarget(leftRT.Target);
		base.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, ColorEx.TransparentBlack, 1f, 0);
		base.GraphicsDevice.PrepareDraw();
		base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
		drawMethod(gameTime);
		base.GraphicsDevice.SetRenderTarget(rightRT.Target);
		base.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, ColorEx.TransparentBlack, 1f, 0);
		base.GraphicsDevice.PrepareDraw();
		BaseEffect.EyeSign = CameraManager.InverseView.Right / num;
		base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
		drawMethod(gameTime);
		base.GraphicsDevice.SetRenderTarget(renderTarget);
		base.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, ColorEx.TransparentBlack, 1f, 0);
		base.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
		base.GraphicsDevice.SamplerStates[1] = SamplerState.PointClamp;
		base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
		combineEffect.LeftTexture = leftRT.Target;
		combineEffect.RightTexture = rightRT.Target;
		base.TargetRenderer.DrawFullscreen(combineEffect);
	}
}
