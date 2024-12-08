using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Components;

public abstract class LevelHost : DrawableGameComponent
{
	[ServiceDependency]
	public IDebuggingBag DebuggingBag { protected get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { protected get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { protected get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { protected get; set; }

	[ServiceDependency(Optional = true)]
	public IKeyboardStateManager KeyboardProvider { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { private get; set; }

	protected LevelHost(Game game)
		: base(game)
	{
		base.DrawOrder = 5;
	}

	protected override void LoadContent()
	{
		DrawActionScheduler.Schedule(RefreshEffects);
		BaseEffect.InstancingModeChanged += delegate
		{
			DrawActionScheduler.Schedule(RefreshEffects);
		};
		CameraManager.PixelsPerTrixel = 3f;
	}

	private void RefreshEffects()
	{
		LevelMaterializer.TrilesMesh.Effect = new TrileEffect();
		LevelMaterializer.ArtObjectsMesh.Effect = new InstancedArtObjectEffect();
		LevelMaterializer.StaticPlanesMesh.Effect = new InstancedStaticPlaneEffect();
		LevelMaterializer.AnimatedPlanesMesh.Effect = new InstancedAnimatedPlaneEffect();
		LevelMaterializer.NpcMesh.Effect = new AnimatedPlaneEffect
		{
			IgnoreShading = true
		};
	}

	public override void Draw(GameTime gameTime)
	{
		DoDraw();
	}

	protected void DoDraw()
	{
		if (LevelManager.Sky != null && LevelManager.Sky.Name == "GRAVE")
		{
			LevelMaterializer.RenderPass = RenderPass.Ghosts;
			LevelMaterializer.NpcMesh.DepthWrites = false;
			base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
			base.GraphicsDevice.PrepareStencilWrite(StencilMask.Ghosts);
			LevelMaterializer.NpcMesh.Draw();
			base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
			LevelMaterializer.NpcMesh.DepthWrites = true;
		}
		LevelMaterializer.RenderPass = RenderPass.Normal;
		base.GraphicsDevice.GetDssCombiner().DepthBufferEnable = true;
		base.GraphicsDevice.PrepareStencilWrite(StencilMask.Level);
		LevelMaterializer.TrilesMesh.Draw();
		LevelMaterializer.ArtObjectsMesh.Draw();
		base.GraphicsDevice.GetRasterCombiner().SlopeScaleDepthBias = -0.1f;
		base.GraphicsDevice.GetRasterCombiner().DepthBias = (CameraManager.Viewpoint.IsOrthographic() ? (-1E-07f) : (-0.0001f / (CameraManager.FarPlane - CameraManager.NearPlane)));
		LevelMaterializer.StaticPlanesMesh.Draw();
		LevelMaterializer.AnimatedPlanesMesh.Draw();
		base.GraphicsDevice.GetRasterCombiner().DepthBias = 0f;
		base.GraphicsDevice.GetRasterCombiner().SlopeScaleDepthBias = 0f;
		base.GraphicsDevice.PrepareStencilWrite(StencilMask.NoSilhouette);
		LevelMaterializer.NpcMesh.Draw();
		LevelMaterializer.RenderPass = RenderPass.Normal;
		base.GraphicsDevice.PrepareStencilWrite(StencilMask.None);
	}
}
