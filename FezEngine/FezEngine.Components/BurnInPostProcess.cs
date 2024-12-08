using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Components;

public class BurnInPostProcess : DrawableGameComponent
{
	private Texture2D oldFrameBuffer;

	private RenderTargetHandle newFrameBuffer;

	private RenderTargetHandle ownedHandle;

	private BurnInPostEffect burnInEffect;

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderingManager { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { private get; set; }

	public BurnInPostProcess(Game game)
		: base(game)
	{
		base.DrawOrder = 902;
	}

	public override void Initialize()
	{
		base.Initialize();
		base.Enabled = false;
		LevelManager.LevelChanged += TryCreateTargets;
		TryCreateTargets();
	}

	private void TryCreateTargets()
	{
		if (LevelManager.Name == null)
		{
			return;
		}
		if (LevelManager.BlinkingAlpha)
		{
			if (!base.Enabled)
			{
				ownedHandle = TargetRenderingManager.TakeTarget();
				newFrameBuffer = TargetRenderingManager.TakeTarget();
			}
			base.Enabled = true;
			return;
		}
		if (base.Enabled)
		{
			TargetRenderingManager.ReturnTarget(ownedHandle);
			ownedHandle = null;
			TargetRenderingManager.ReturnTarget(newFrameBuffer);
			newFrameBuffer = null;
		}
		base.Enabled = false;
	}

	protected override void LoadContent()
	{
		DrawActionScheduler.Schedule(delegate
		{
			burnInEffect = new BurnInPostEffect();
		});
	}

	public override void Draw(GameTime gameTime)
	{
		if (!base.Enabled || EngineState.Loading || EngineState.Paused || EngineState.InMap || EngineState.InEditor)
		{
			if (newFrameBuffer != null && TargetRenderingManager.IsHooked(newFrameBuffer.Target))
			{
				TargetRenderingManager.Resolve(newFrameBuffer.Target, reschedule: false);
				base.GraphicsDevice.Clear(Color.Black);
				base.GraphicsDevice.SetupViewport();
				TargetRenderingManager.DrawFullscreen(newFrameBuffer.Target);
			}
			return;
		}
		if (!TargetRenderingManager.IsHooked(newFrameBuffer.Target))
		{
			TargetRenderingManager.ScheduleHook(base.DrawOrder, newFrameBuffer.Target);
			return;
		}
		base.GraphicsDevice.GetDssCombiner().StencilEnable = false;
		base.GraphicsDevice.SetBlendingMode(BlendingMode.Opaque);
		TargetRenderingManager.Resolve(newFrameBuffer.Target, reschedule: true);
		RenderTarget2D renderTarget = ((base.GraphicsDevice.GetRenderTargets().Length == 0) ? null : (base.GraphicsDevice.GetRenderTargets()[0].RenderTarget as RenderTarget2D));
		burnInEffect.NewFrameBuffer = newFrameBuffer.Target;
		base.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
		RenderTargetHandle renderTargetHandle = TargetRenderingManager.TakeTarget();
		base.GraphicsDevice.SetRenderTarget(renderTargetHandle.Target);
		TargetRenderingManager.DrawFullscreen(burnInEffect);
		TargetRenderingManager.ReturnTarget(renderTargetHandle);
		base.GraphicsDevice.SetRenderTarget(ownedHandle.Target);
		base.GraphicsDevice.Clear(Color.Black);
		base.GraphicsDevice.SetupViewport();
		TargetRenderingManager.DrawFullscreen(renderTargetHandle.Target);
		base.GraphicsDevice.SetRenderTarget(renderTarget);
		oldFrameBuffer = ownedHandle.Target;
		burnInEffect.OldFrameBuffer = oldFrameBuffer;
		base.GraphicsDevice.Clear(Color.Black);
		base.GraphicsDevice.SetupViewport();
		TargetRenderingManager.DrawFullscreen(newFrameBuffer.Target);
		base.GraphicsDevice.SetBlendingMode(BlendingMode.Maximum);
		TargetRenderingManager.DrawFullscreen(oldFrameBuffer);
		base.GraphicsDevice.GetDssCombiner().StencilEnable = true;
		base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
	}
}
