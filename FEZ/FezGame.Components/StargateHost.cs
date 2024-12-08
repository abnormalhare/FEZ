using System;
using System.Collections.Generic;
using FezEngine;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class StargateHost : DrawableGameComponent
{
	private class MaskRenderer : DrawableGameComponent
	{
		private Texture2D PyramidWarp;

		private Mesh PyramidMask;

		private Mesh WarpCube;

		public Vector3 Center;

		[ServiceDependency]
		public IGameStateManager GameState { private get; set; }

		[ServiceDependency]
		public IContentManagerProvider CMProvider { private get; set; }

		[ServiceDependency]
		public IGameCameraManager CameraManager { private get; set; }

		public MaskRenderer(Game game)
			: base(game)
		{
			base.DrawOrder = 6;
		}

		protected override void LoadContent()
		{
			base.LoadContent();
			PyramidMask = new Mesh
			{
				DepthWrites = false
			};
			PyramidMask.AddFace(new Vector3(8f), Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true, doublesided: true);
			WarpCube = new Mesh
			{
				DepthWrites = false
			};
			WarpCube.AddFace(new Vector3(16f), 8f * Vector3.UnitZ, FaceOrientation.Front, centeredOnOrigin: true, doublesided: false);
			WarpCube.AddFace(new Vector3(16f), 8f * Vector3.Right, FaceOrientation.Right, centeredOnOrigin: true, doublesided: false);
			WarpCube.AddFace(new Vector3(16f), 8f * Vector3.Left, FaceOrientation.Left, centeredOnOrigin: true, doublesided: false);
			WarpCube.AddFace(new Vector3(16f), 8f * -Vector3.UnitZ, FaceOrientation.Back, centeredOnOrigin: true, doublesided: false);
			DrawActionScheduler.Schedule(delegate
			{
				PyramidMask.Effect = new DefaultEffect.Textured();
				WarpCube.Effect = new DefaultEffect.Textured();
				PyramidWarp = CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/warp/pyramid");
				WarpCube.Texture = PyramidWarp;
			});
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (PyramidMask != null)
			{
				PyramidMask.Dispose();
			}
			PyramidMask = null;
			if (WarpCube != null)
			{
				WarpCube.Dispose();
			}
			WarpCube = null;
			PyramidWarp = null;
		}

		public override void Draw(GameTime gameTime)
		{
			if (!GameState.Loading && !GameState.Paused && !GameState.InMap && !GameState.InMenuCube)
			{
				Vector3 a = Center - CameraManager.InterpolatedCenter;
				Vector2 vector = a.Dot(CameraManager.View.Right) * Vector2.UnitX + a.Y * Vector2.UnitY;
				Mesh warpCube = WarpCube;
				Vector3 position = (PyramidMask.Position = Center);
				warpCube.Position = position;
				WarpCube.TextureMatrix = new Matrix(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, vector.X / 48f, (0f - vector.Y) / 48f + 0.1f, 1f, 0f, 0f, 0f, 0f, 1f);
				base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
				base.GraphicsDevice.PrepareStencilWrite(StencilMask.WarpGate);
				PyramidMask.Draw();
				base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
				base.GraphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.WarpGate);
				WarpCube.Draw();
				base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
			}
		}
	}

	private readonly List<bool> AoVisibility = new List<bool>();

	private float SinceStarted;

	private float SpinSpeed;

	private ArtObjectInstance[] Rings;

	private readonly Texture2D[] OriginalTextures = new Texture2D[4];

	private Texture2D WhiteTex;

	private Mesh TrialRaysMesh;

	private Mesh TrialFlareMesh;

	private float TrialTimeAccumulator;

	private MaskRenderer maskRenderer;

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	public StargateHost(Game game)
		: base(game)
	{
		base.DrawOrder = 200;
	}

	public override void Initialize()
	{
		base.Initialize();
		WhiteTex = CMProvider.Global.Load<Texture2D>("Other Textures/FullWhite");
		bool enabled = (base.Visible = false);
		base.Enabled = enabled;
		LevelManager.LevelChanged += TryInitialize;
	}

	private void TryInitialize()
	{
		Rings = null;
		if (TrialRaysMesh != null)
		{
			TrialRaysMesh.Dispose();
		}
		if (TrialFlareMesh != null)
		{
			TrialFlareMesh.Dispose();
		}
		TrialRaysMesh = (TrialFlareMesh = null);
		TrialTimeAccumulator = (SinceStarted = 0f);
		SpinSpeed = 0f;
		if (maskRenderer != null)
		{
			ServiceHelper.RemoveComponent(maskRenderer);
			maskRenderer = null;
		}
		bool enabled = (base.Visible = LevelManager.Name == "STARGATE");
		base.Enabled = enabled;
		if (!base.Enabled)
		{
			return;
		}
		TrialRaysMesh = new Mesh
		{
			Blending = BlendingMode.Additive,
			SamplerState = SamplerState.AnisotropicClamp,
			DepthWrites = false,
			AlwaysOnTop = true
		};
		TrialFlareMesh = new Mesh
		{
			Blending = BlendingMode.Alphablending,
			SamplerState = SamplerState.AnisotropicClamp,
			DepthWrites = false,
			AlwaysOnTop = true
		};
		TrialFlareMesh.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Right, centeredOnOrigin: true);
		DrawActionScheduler.Schedule(delegate
		{
			TrialRaysMesh.Effect = new DefaultEffect.Textured();
			TrialRaysMesh.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/smooth_ray");
			TrialFlareMesh.Effect = new DefaultEffect.Textured();
			TrialFlareMesh.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/flare_alpha");
		});
		Rings = new ArtObjectInstance[4]
		{
			LevelManager.ArtObjects[5],
			LevelManager.ArtObjects[6],
			LevelManager.ArtObjects[7],
			LevelManager.ArtObjects[8]
		};
		ArtObjectInstance[] rings = Rings;
		for (int i = 0; i < rings.Length; i++)
		{
			rings[i].Material = new Material();
		}
		ServiceHelper.AddComponent(maskRenderer = new MaskRenderer(base.Game));
		maskRenderer.Center = Rings[0].Position;
		if (GameState.SaveData.ThisLevel.InactiveArtObjects.Contains(0))
		{
			base.Enabled = false;
			LevelManager.Scripts[4].Disabled = false;
			LevelManager.Scripts[5].Disabled = false;
			rings = Rings;
			foreach (ArtObjectInstance aoInstance in rings)
			{
				LevelManager.RemoveArtObject(aoInstance);
			}
		}
		else
		{
			LevelManager.Scripts[4].Disabled = true;
			LevelManager.Scripts[5].Disabled = true;
			maskRenderer.Visible = false;
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (!GameState.Loading && !GameState.Paused && !GameState.InMap && !GameState.InMenuCube)
		{
			SinceStarted += (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (SinceStarted > 8f && SinceStarted < 19f)
			{
				SpinSpeed = Easing.EaseIn(FezMath.Saturate((SinceStarted - 8f) / 5f), EasingType.Sine) * 0.005f;
			}
			else if (SinceStarted > 19f)
			{
				SpinSpeed = 0.005f + Easing.EaseIn(FezMath.Saturate((SinceStarted - 19f) / 20f), EasingType.Quadratic) * 0.5f;
			}
			if (SinceStarted > 33f && Rings != null)
			{
				TrialTimeAccumulator += (float)gameTime.ElapsedGameTime.TotalSeconds;
				UpdateRays((float)gameTime.ElapsedGameTime.TotalSeconds);
			}
			if (Rings != null)
			{
				Rings[0].Rotation = Quaternion.CreateFromAxisAngle(Vector3.Right, SpinSpeed) * Rings[0].Rotation;
				Rings[1].Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, SpinSpeed) * Rings[1].Rotation;
				Rings[2].Rotation = Quaternion.CreateFromAxisAngle(Vector3.Left, SpinSpeed) * Rings[2].Rotation;
				Rings[3].Rotation = Quaternion.CreateFromAxisAngle(Vector3.Down, SpinSpeed) * Rings[3].Rotation;
			}
		}
	}

	private void UpdateRays(float elapsedSeconds)
	{
		if (TrialRaysMesh.Groups.Count < 50 && RandomHelper.Probability(0.2))
		{
			float num = 6f + RandomHelper.Centered(4.0);
			float num2 = RandomHelper.Between(0.5, num / 2.5f);
			Group group = TrialRaysMesh.AddGroup();
			group.Geometry = new IndexedUserPrimitives<FezVertexPositionTexture>(new FezVertexPositionTexture[6]
			{
				new FezVertexPositionTexture(new Vector3(0f, num2 / 2f * 0.1f, 0f), new Vector2(0f, 0f)),
				new FezVertexPositionTexture(new Vector3(num, num2 / 2f, 0f), new Vector2(1f, 0f)),
				new FezVertexPositionTexture(new Vector3(num, num2 / 2f * 0.1f, 0f), new Vector2(1f, 0.45f)),
				new FezVertexPositionTexture(new Vector3(num, (0f - num2) / 2f * 0.1f, 0f), new Vector2(1f, 0.55f)),
				new FezVertexPositionTexture(new Vector3(num, (0f - num2) / 2f, 0f), new Vector2(1f, 1f)),
				new FezVertexPositionTexture(new Vector3(0f, (0f - num2) / 2f * 0.1f, 0f), new Vector2(0f, 1f))
			}, new int[12]
			{
				0, 1, 2, 0, 2, 5, 5, 2, 3, 5,
				3, 4
			}, PrimitiveType.TriangleList);
			group.CustomData = new DotHost.RayState();
			group.Material = new Material
			{
				Diffuse = new Vector3(0f)
			};
			group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Forward, RandomHelper.Between(0.0, 6.2831854820251465));
		}
		for (int num3 = TrialRaysMesh.Groups.Count - 1; num3 >= 0; num3--)
		{
			Group group2 = TrialRaysMesh.Groups[num3];
			DotHost.RayState rayState = group2.CustomData as DotHost.RayState;
			rayState.Age += elapsedSeconds * 0.15f;
			float num4 = (float)Math.Sin(rayState.Age * ((float)Math.PI * 2f) - (float)Math.PI / 2f) * 0.5f + 0.5f;
			num4 = Easing.EaseOut(num4, EasingType.Quintic);
			num4 = Easing.EaseOut(num4, EasingType.Quintic);
			group2.Material.Diffuse = Vector3.Lerp(Vector3.One, rayState.Tint.ToVector3(), 0.05f) * 0.15f * num4;
			float speed = rayState.Speed;
			group2.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.Forward, elapsedSeconds * speed * (0.1f + Easing.EaseIn(TrialTimeAccumulator / 3f, EasingType.Quadratic) * 0.2f));
			group2.Scale = new Vector3(num4 * 0.75f + 0.25f, num4 * 0.5f + 0.5f, 1f);
			if (rayState.Age > 1f)
			{
				TrialRaysMesh.RemoveGroupAt(num3);
			}
		}
		Mesh trialFlareMesh = TrialFlareMesh;
		Vector3 position2 = (TrialRaysMesh.Position = Rings[0].Position);
		trialFlareMesh.Position = position2;
		Mesh trialFlareMesh2 = TrialFlareMesh;
		Quaternion rotation2 = (TrialRaysMesh.Rotation = CameraManager.Rotation);
		trialFlareMesh2.Rotation = rotation2;
		float num5 = Easing.EaseIn(TrialTimeAccumulator / 2f, EasingType.Quadratic);
		TrialRaysMesh.Scale = new Vector3(num5 + 1f);
		TrialFlareMesh.Material.Opacity = 0.125f + Easing.EaseIn(FezMath.Saturate((TrialTimeAccumulator - 2f) / 3f), EasingType.Cubic) * 0.875f;
		TrialFlareMesh.Scale = Vector3.One + TrialRaysMesh.Scale * Easing.EaseIn(Math.Max(TrialTimeAccumulator - 2.5f, 0f) / 1.5f, EasingType.Cubic) * 4f;
	}

	public override void Draw(GameTime gameTime)
	{
		if (GameState.Loading)
		{
			return;
		}
		if (SinceStarted > 19f && Rings != null)
		{
			AoVisibility.Clear();
			foreach (ArtObjectInstance levelArtObject in LevelMaterializer.LevelArtObjects)
			{
				AoVisibility.Add(levelArtObject.Visible);
				levelArtObject.Visible = false;
				levelArtObject.ArtObject.Group.Enabled = false;
			}
			for (int i = 0; i < 4; i++)
			{
				ArtObjectInstance artObjectInstance = Rings[i];
				OriginalTextures[i] = artObjectInstance.ArtObject.Group.TextureMap;
				artObjectInstance.Visible = true;
				artObjectInstance.ArtObject.Group.Enabled = true;
				artObjectInstance.ArtObject.Group.Texture = WhiteTex;
				artObjectInstance.Material.Opacity = Easing.EaseIn(FezMath.Saturate((SinceStarted - 19f) / 18f), EasingType.Cubic);
				artObjectInstance.Update();
			}
			LevelMaterializer.ArtObjectsMesh.Draw();
			for (int j = 0; j < 4; j++)
			{
				Rings[j].ArtObject.Group.Texture = OriginalTextures[j];
				OriginalTextures[j] = null;
				Rings[j].Material.Opacity = 1f;
			}
			int num = 0;
			foreach (ArtObjectInstance levelArtObject2 in LevelMaterializer.LevelArtObjects)
			{
				levelArtObject2.Visible = AoVisibility[num++];
				if (levelArtObject2.Visible)
				{
					levelArtObject2.ArtObject.Group.Enabled = true;
				}
			}
		}
		if (SinceStarted > 36.75f)
		{
			if (Rings != null)
			{
				ArtObjectInstance[] rings = Rings;
				foreach (ArtObjectInstance aoInstance in rings)
				{
					LevelManager.RemoveArtObject(aoInstance);
				}
				maskRenderer.Visible = true;
				LevelManager.Scripts[4].Disabled = false;
				LevelManager.Scripts[5].Disabled = false;
				GameState.SaveData.ThisLevel.InactiveArtObjects.Add(0);
			}
			Rings = null;
			float num2 = Easing.EaseIn(1f - FezMath.Saturate((SinceStarted - 36.75f) / 6f), EasingType.Sine);
			if (!FezMath.AlmostEqual(num2, 0f))
			{
				TargetRenderer.DrawFullscreen(new Color(1f, 1f, 1f, num2));
			}
		}
		else if (SinceStarted > 33f)
		{
			float alpha = FezMath.Saturate(Easing.EaseIn((TrialTimeAccumulator - 3f) / 0.75f, EasingType.Quintic));
			TargetRenderer.DrawFullscreen(new Color(1f, 1f, 1f, alpha));
			TrialRaysMesh.Draw();
			TrialFlareMesh.Draw();
		}
	}
}
