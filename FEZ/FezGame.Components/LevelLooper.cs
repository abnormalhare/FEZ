using System;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class LevelLooper : DrawableGameComponent
{
	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { private get; set; }

	[ServiceDependency]
	public ILightingPostProcess LightingPostProcess { private get; set; }

	public LevelLooper(Game game)
		: base(game)
	{
		base.UpdateOrder = -1;
		base.DrawOrder = 6;
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += delegate
		{
			CameraManager.ViewOffset = Vector3.Zero;
		};
		LightingPostProcess.DrawGeometryLights += Draw;
	}

	public override void Update(GameTime gameTime)
	{
		if (LevelManager.Loops && PlayerManager.Action != ActionType.FreeFalling && !GameState.Loading && !GameState.InMap && !GameState.Paused)
		{
			while (PlayerManager.Position.Y < 0f)
			{
				PlayerManager.Position += LevelManager.Size * Vector3.UnitY;
				CameraManager.Center += LevelManager.Size * Vector3.UnitY;
				CameraManager.ViewOffset += LevelManager.Size * Vector3.UnitY;
			}
			while (PlayerManager.Position.Y > LevelManager.Size.Y)
			{
				PlayerManager.Position -= LevelManager.Size * Vector3.UnitY;
				CameraManager.Center -= LevelManager.Size * Vector3.UnitY;
				CameraManager.ViewOffset -= LevelManager.Size * Vector3.UnitY;
				PlayerManager.IgnoreFreefall = true;
			}
		}
	}

	public override void Draw(GameTime gameTime)
	{
		Draw();
	}

	private void Draw()
	{
		if (LevelManager.Loops && !GameState.Loading)
		{
			float num = LevelManager.Size.Y * (float)((PlayerManager.Position.Y < LevelManager.Size.Y / 2f) ? 1 : (-1));
			GameState.LoopRender = true;
			if (LoopVisible())
			{
				CameraManager.ViewOffset += num * Vector3.UnitY;
				DrawLoop();
				CameraManager.ViewOffset -= num * Vector3.UnitY;
			}
			GameState.LoopRender = false;
		}
	}

	private bool LoopVisible()
	{
		if (!CameraManager.Viewpoint.IsOrthographic())
		{
			return true;
		}
		Vector3 value = CameraManager.Viewpoint.RightVector().Abs();
		BoundingFrustum frustum = CameraManager.Frustum;
		BoundingBox boundingBox = default(BoundingBox);
		boundingBox.Min.X = (0f - frustum.Left.D) * frustum.Left.DotNormal(value);
		boundingBox.Min.Y = (0f - frustum.Bottom.D) * frustum.Bottom.Normal.Y;
		boundingBox.Max.X = (0f - frustum.Right.D) * frustum.Right.DotNormal(value);
		boundingBox.Max.Y = (0f - frustum.Top.D) * frustum.Top.Normal.Y;
		BoundingBox boundingBox2 = boundingBox;
		Vector3 vector = FezMath.Min(boundingBox2.Min, boundingBox2.Max);
		Vector3 vector2 = FezMath.Max(boundingBox2.Min, boundingBox2.Max);
		Rectangle rectangle = default(Rectangle);
		rectangle.X = (int)Math.Floor(vector.X);
		rectangle.Y = (int)Math.Floor(vector.Y);
		rectangle.Width = (int)Math.Ceiling(vector2.X - vector.X);
		rectangle.Height = (int)Math.Ceiling(vector2.Y - vector.Y);
		Rectangle rectangle2 = rectangle;
		if (rectangle2.Y >= 0)
		{
			return (float)(rectangle2.Y + rectangle2.Height) >= LevelManager.Size.Y;
		}
		return true;
	}

	private void DrawLoop()
	{
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		bool num = LevelMaterializer.RenderPass == RenderPass.LightInAlphaEmitters;
		if (!num)
		{
			LevelMaterializer.RenderPass = RenderPass.Normal;
		}
		if (!num)
		{
			graphicsDevice.PrepareStencilWrite(StencilMask.Level);
		}
		LevelMaterializer.TrilesMesh.Draw();
		LevelMaterializer.ArtObjectsMesh.Draw();
		if (num)
		{
			graphicsDevice.GetRasterCombiner().DepthBias = -0.0001f;
		}
		LevelMaterializer.StaticPlanesMesh.Draw();
		LevelMaterializer.AnimatedPlanesMesh.Draw();
		if (num)
		{
			graphicsDevice.GetRasterCombiner().DepthBias = 0f;
		}
		if (!num)
		{
			graphicsDevice.PrepareStencilWrite(StencilMask.NoSilhouette);
		}
		LevelMaterializer.NpcMesh.Draw();
		if (!num)
		{
			graphicsDevice.PrepareStencilWrite(StencilMask.None);
		}
	}
}
