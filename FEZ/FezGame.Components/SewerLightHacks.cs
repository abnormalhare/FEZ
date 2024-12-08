using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class SewerLightHacks : DrawableGameComponent
{
	private SewerHaxEffect Effect;

	[ServiceDependency]
	public IGameStateManager GameState { get; private set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { get; private set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { get; private set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { get; private set; }

	[ServiceDependency]
	public ILightingPostProcess LightingPostProcess { get; private set; }

	public SewerLightHacks(Game game)
		: base(game)
	{
		base.DrawOrder = 49;
	}

	public override void Initialize()
	{
		base.Initialize();
		DrawActionScheduler.Schedule(delegate
		{
			Effect = new SewerHaxEffect();
		});
		LevelManager.LevelChanged += TryInitialize;
	}

	private void TryInitialize()
	{
		base.Visible = LevelManager.WaterType == LiquidType.Sewer;
		if (base.Visible)
		{
			LevelManager.BaseAmbient = 1f;
			LevelManager.BaseDiffuse = 0f;
			LevelManager.HaloFiltering = false;
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (GameState.Loading || GameState.StereoMode)
		{
			return;
		}
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		graphicsDevice.PrepareStencilWrite(StencilMask.None);
		graphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
		LevelMaterializer.StaticPlanesMesh.AlwaysOnTop = true;
		LevelMaterializer.StaticPlanesMesh.DepthWrites = false;
		foreach (BackgroundPlane levelPlane in LevelMaterializer.LevelPlanes)
		{
			levelPlane.Group.Enabled = levelPlane.Id < 0;
		}
		LevelMaterializer.StaticPlanesMesh.Draw();
		LevelMaterializer.StaticPlanesMesh.AlwaysOnTop = false;
		LevelMaterializer.StaticPlanesMesh.DepthWrites = true;
		foreach (BackgroundPlane levelPlane2 in LevelMaterializer.LevelPlanes)
		{
			levelPlane2.Group.Enabled = true;
		}
		graphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
		graphicsDevice.PrepareStencilRead(CompareFunction.LessEqual, StencilMask.Sky);
		TargetRenderer.DrawFullscreen(Effect, LightingPostProcess.LightmapTexture);
		graphicsDevice.PrepareStencilWrite(StencilMask.None);
	}
}
