using System;
using Common;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Components;

public class LightingPostProcess : DrawableGameComponent, ILightingPostProcess
{
	protected RenderTargetHandle lightMapsRth;

	private LightingPostEffect lightingPostEffect;

	private bool hadRt;

	protected virtual bool SkipLighting => false;

	public Texture2D LightmapTexture => lightMapsRth.Target;

	[ServiceDependency]
	public IEngineStateManager EngineState { protected get; set; }

	[ServiceDependency]
	public ITimeManager TimeManager { protected get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { protected get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { protected get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { protected get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderingManager { protected get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { protected get; set; }

	[ServiceDependency]
	public IFogManager FogManager { private get; set; }

	public event Action<GameTime> DrawGeometryLights = Util.NullAction;

	public event Action DrawOnTopLights = Util.NullAction;

	public LightingPostProcess(Game game)
		: base(game)
	{
		base.DrawOrder = 100;
		ServiceHelper.AddService(this);
	}

	protected override void LoadContent()
	{
		base.Enabled = false;
		DrawActionScheduler.Schedule(delegate
		{
			lightingPostEffect = new LightingPostEffect();
			base.Enabled = true;
		});
		lightMapsRth = TargetRenderingManager.TakeTarget();
		TargetRenderingManager.PreDraw += PreDraw;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		TargetRenderingManager.ReturnTarget(lightMapsRth);
		lightMapsRth = null;
	}

	public override void Update(GameTime gameTime)
	{
		if (!LevelManager.SkipPostProcess)
		{
			UpdateLightFilter();
		}
	}

	private void UpdateLightFilter()
	{
		lightingPostEffect.DawnContribution = TimeManager.DawnContribution;
		lightingPostEffect.DuskContribution = TimeManager.DuskContribution;
		lightingPostEffect.NightContribution = TimeManager.NightContribution;
	}

	protected virtual void DoSetup()
	{
	}

	private void PreDraw(GameTime gameTime)
	{
		LevelMaterializer.RegisterSatellites();
		foreach (BackgroundPlane levelPlane in LevelMaterializer.LevelPlanes)
		{
			levelPlane.Update();
		}
		if (EngineState.StereoMode || LevelManager.Quantum)
		{
			return;
		}
		LevelManager.ActualDiffuse = new Color(LevelManager.BaseDiffuse, LevelManager.BaseDiffuse, LevelManager.BaseDiffuse);
		LevelManager.ActualAmbient = new Color(LevelManager.BaseAmbient, LevelManager.BaseAmbient, LevelManager.BaseAmbient);
		DoSetup();
		if (SkipLighting)
		{
			return;
		}
		hadRt = TargetRenderingManager.HasRtInQueue || LevelManager.WaterType == LiquidType.Sewer || LevelManager.WaterType == LiquidType.Lava;
		base.GraphicsDevice.SetRenderTarget(lightMapsRth.Target);
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		graphicsDevice.PrepareDraw();
		base.GraphicsDevice.SetupViewport();
		if (!LevelManager.SkipPostProcess && TimeManager.NightContribution != 0f)
		{
			LevelManager.ActualDiffuse = Color.Lerp(LevelManager.ActualDiffuse, FogManager.Color, TimeManager.NightContribution * 0.4f);
			if (LevelManager.Sky != null && LevelManager.Sky.FoliageShadows)
			{
				LevelManager.ActualAmbient = Color.Lerp(LevelManager.ActualAmbient, Color.Lerp(FogManager.Color, Color.White, 0.5f), TimeManager.NightContribution * 0.5f);
			}
			else
			{
				LevelManager.ActualAmbient = Color.Lerp(LevelManager.ActualAmbient, Color.White, TimeManager.NightContribution * 0.5f);
			}
		}
		if (!LevelManager.SkipPostProcess)
		{
			LevelManager.ActualAmbient = Color.Lerp(LevelManager.ActualAmbient, FogManager.Color, 23f / 160f);
		}
		if (LevelManager.WaterType == LiquidType.Sewer || LevelManager.BlinkingAlpha)
		{
			base.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, new Color(0f, 0f, 0f, 0f), 1f, 0);
		}
		else if (LevelManager.Sky != null && LevelManager.Sky.Name == "INDUS_CITY" && !SkyHost.Instance.flickering)
		{
			base.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, new Color(0, 0, 0, 0), 1f, 0);
		}
		else
		{
			base.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, new Color(0.5f, 0.5f, 0.5f, 0f), 1f, 0);
		}
		graphicsDevice.PrepareStencilWrite(StencilMask.Level);
		LevelMaterializer.RenderPass = RenderPass.Occluders;
		graphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
		DrawLightOccluders(gameTime);
		graphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
		LevelMaterializer.RenderPass = RenderPass.LightInAlphaEmitters;
		graphicsDevice.GetBlendCombiner().AlphaBlendFunction = BlendFunction.Add;
		graphicsDevice.GetBlendCombiner().AlphaSourceBlend = Blend.One;
		graphicsDevice.GetBlendCombiner().AlphaDestinationBlend = Blend.One;
		LevelMaterializer.TrilesMesh.Draw();
		LevelMaterializer.ArtObjectsMesh.Draw();
		graphicsDevice.GetRasterCombiner().SlopeScaleDepthBias = -0.1f;
		graphicsDevice.GetRasterCombiner().DepthBias = (CameraManager.Viewpoint.IsOrthographic() ? (-1E-07f) : (-0.0001f / (CameraManager.FarPlane - CameraManager.NearPlane)));
		graphicsDevice.GetBlendCombiner().BlendingMode = BlendingMode.Opaque;
		LevelMaterializer.AnimatedPlanesMesh.Draw();
		LevelMaterializer.StaticPlanesMesh.Draw();
		LevelMaterializer.NpcMesh.Draw();
		this.DrawGeometryLights(gameTime);
		graphicsDevice.PrepareStencilWrite(StencilMask.None);
		this.DrawOnTopLights();
		graphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
		graphicsDevice.ResetAlphaBlending();
		LevelMaterializer.RenderPass = RenderPass.WorldspaceLightmaps;
		LevelMaterializer.StaticPlanesMesh.Draw();
		LevelMaterializer.AnimatedPlanesMesh.Draw();
		LevelMaterializer.RenderPass = RenderPass.ScreenspaceLightmaps;
		LevelMaterializer.StaticPlanesMesh.Draw();
		LevelMaterializer.AnimatedPlanesMesh.Draw();
		graphicsDevice.GetRasterCombiner().DepthBias = 0f;
		graphicsDevice.GetRasterCombiner().SlopeScaleDepthBias = 0f;
		base.GraphicsDevice.SetRenderTarget(null);
		graphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
	}

	protected virtual void DrawLightOccluders(GameTime gameTime)
	{
	}

	public override void Draw(GameTime gameTime)
	{
		if (!EngineState.StereoMode && !LevelManager.Quantum && !EngineState.Loading && LevelManager.WaterType != LiquidType.Sewer && !EngineState.SkipRendering)
		{
			GraphicsDevice graphicsDevice = base.GraphicsDevice;
			graphicsDevice.GetDssCombiner().DepthBufferEnable = false;
			graphicsDevice.SetColorWriteChannels(ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue);
			if (!LevelManager.SkipPostProcess)
			{
				graphicsDevice.PrepareStencilRead(CompareFunction.LessEqual, StencilMask.Level);
				DrawLightFilter();
			}
			graphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
			graphicsDevice.SetBlendingMode(BlendingMode.Multiply2X);
			TargetRenderingManager.DrawFullscreen(lightMapsRth.Target);
			graphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
			graphicsDevice.PrepareStencilWrite(StencilMask.None);
			graphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
			graphicsDevice.GetDssCombiner().DepthBufferEnable = true;
		}
	}

	private void DrawLightFilter()
	{
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		if (!FezMath.AlmostEqual(lightingPostEffect.DawnContribution, 0f))
		{
			lightingPostEffect.Pass = LightingPostEffect.Passes.Dawn;
			graphicsDevice.SetBlendingMode(BlendingMode.Screen);
			TargetRenderingManager.DrawFullscreen(lightingPostEffect);
		}
		if (!FezMath.AlmostEqual(lightingPostEffect.DuskContribution, 0f))
		{
			lightingPostEffect.Pass = LightingPostEffect.Passes.Dusk_Multiply;
			graphicsDevice.SetBlendingMode(BlendingMode.Multiply);
			TargetRenderingManager.DrawFullscreen(lightingPostEffect);
			lightingPostEffect.Pass = LightingPostEffect.Passes.Dusk_Screen;
			graphicsDevice.SetBlendingMode(BlendingMode.Screen);
			TargetRenderingManager.DrawFullscreen(lightingPostEffect);
		}
		if (!FezMath.AlmostEqual(lightingPostEffect.NightContribution, 0f))
		{
			lightingPostEffect.Pass = LightingPostEffect.Passes.Night;
			graphicsDevice.SetBlendingMode(BlendingMode.Multiply);
			TargetRenderingManager.DrawFullscreen(lightingPostEffect);
		}
	}
}
