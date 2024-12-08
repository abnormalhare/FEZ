using System;
using System.Collections.Generic;
using Common;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Components;

public class CloudShadowsHost : DrawableGameComponent
{
	private readonly Dictionary<Group, Axis> axisPerGroup = new Dictionary<Group, Axis>();

	private CloudShadowEffect shadowEffect;

	private Mesh shadowMesh;

	private SkyHost Host;

	private float SineAccumulator;

	private float SineSpeed;

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { private get; set; }

	[ServiceDependency]
	public ITimeManager TimeManager { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public ILightingPostProcess LightingPostProcess { private get; set; }

	public CloudShadowsHost(Game game, SkyHost host)
		: base(game)
	{
		base.DrawOrder = 100;
		Host = host;
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		shadowMesh = new Mesh
		{
			DepthWrites = false,
			AlwaysOnTop = true,
			SamplerState = SamplerState.LinearWrap
		};
		foreach (FaceOrientation value in Util.GetValues<FaceOrientation>())
		{
			if (value.IsSide())
			{
				axisPerGroup.Add(shadowMesh.AddFace(Vector3.One, Vector3.Zero, value, centeredOnOrigin: true), (value.AsAxis() == Axis.X) ? Axis.Z : Axis.X);
			}
		}
		DrawActionScheduler.Schedule(delegate
		{
			shadowMesh.Effect = (shadowEffect = new CloudShadowEffect());
		});
		LevelManager.SkyChanged += InitializeShadows;
		InitializeShadows();
		LightingPostProcess.DrawOnTopLights += DrawLights;
	}

	private void InitializeShadows()
	{
		if (LevelManager.Name == null || LevelManager.Sky == null || LevelManager.Sky.Shadows == null)
		{
			shadowMesh.Texture.Set(null);
			shadowMesh.Enabled = false;
			return;
		}
		shadowMesh.Enabled = true;
		DrawActionScheduler.Schedule(delegate
		{
			string text = "Skies/" + LevelManager.Sky.Name + "/";
			shadowMesh.Texture = CMProvider.CurrentLevel.Load<Texture2D>(text + LevelManager.Sky.Shadows);
		});
		shadowMesh.Scale = LevelManager.Size + new Vector3(65f, 65f, 65f);
		shadowMesh.Position = LevelManager.Size / 2f;
		int num = 0;
		foreach (Group key in axisPerGroup.Keys)
		{
			key.Material = new Material();
			Axis axis = axisPerGroup[key];
			float num2 = shadowMesh.Scale.Dot(axis.GetMask()) / 32f;
			float num3 = shadowMesh.Scale.Y / shadowMesh.Scale.Dot(axis.GetMask());
			key.TextureMatrix = new Matrix(num2, 0f, 0f, 0f, 0f, num2 * num3, 0f, 0f, (float)num / 2f, 0f, 1f, 0f, 0f, 0f, 0f, 1f);
			num++;
		}
		SineAccumulator = 0f;
	}

	public override void Update(GameTime gameTime)
	{
		if (!shadowMesh.Enabled || EngineState.Paused || EngineState.InMap || EngineState.Loading)
		{
			return;
		}
		if (LevelManager.Sky != null && !LevelManager.Sky.FoliageShadows)
		{
			float m = (0f - (float)gameTime.ElapsedGameTime.TotalSeconds) * 0.01f * TimeManager.TimeFactor / 360f * LevelManager.Sky.WindSpeed;
			foreach (Group key in axisPerGroup.Keys)
			{
				if (axisPerGroup[key] != CameraManager.Viewpoint.VisibleAxis())
				{
					key.TextureMatrix.Set(key.TextureMatrix.Value.Value + new Matrix(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, m, 0f, 0f, 0f, 0f, 0f, 0f, 0f));
				}
			}
		}
		else
		{
			float value = RandomHelper.Between(0.0, (float)gameTime.ElapsedGameTime.TotalSeconds);
			SineSpeed = MathHelper.Lerp(SineSpeed, value, 0.1f);
			SineAccumulator += SineSpeed;
		}
		foreach (Group key2 in axisPerGroup.Keys)
		{
			Vector3 mask = axisPerGroup[key2].GetMask();
			key2.Material.Opacity = (1f - Math.Abs(CameraManager.View.Forward.Dot(mask))) * LevelManager.Sky.ShadowOpacity;
			if (!LevelManager.Sky.FoliageShadows)
			{
				key2.Material.Opacity *= (float)(int)LevelManager.ActualDiffuse.G / 255f;
			}
			if (CameraManager.ProjectionTransition)
			{
				key2.Material.Opacity *= (CameraManager.Viewpoint.IsOrthographic() ? CameraManager.ViewTransitionStep : (1f - CameraManager.ViewTransitionStep));
			}
			else if (CameraManager.Viewpoint == Viewpoint.Perspective)
			{
				key2.Material.Opacity = 0f;
			}
		}
	}

	private void DrawLights()
	{
		if (!shadowMesh.Enabled || EngineState.Loading)
		{
			return;
		}
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		graphicsDevice.PrepareStencilRead(CompareFunction.LessEqual, StencilMask.Level);
		EngineState.SkyRender = true;
		Vector3 viewOffset = CameraManager.ViewOffset;
		CameraManager.ViewOffset -= viewOffset;
		if (LevelManager.Sky != null && LevelManager.Sky.FoliageShadows)
		{
			shadowEffect.Pass = CloudShadowPasses.Canopy;
			graphicsDevice.SetBlendingMode(BlendingMode.Minimum);
			float num = (float)Math.Sin(SineAccumulator);
			foreach (Group key in axisPerGroup.Keys)
			{
				float m = key.TextureMatrix.Value.Value.M11;
				float m2 = key.TextureMatrix.Value.Value.M11;
				key.TextureMatrix.Set(new Matrix(m, 0f, 0f, 0f, 0f, m2, 0f, 0f, num / 100f, num / 100f, 1f, 0f, 0f, 0f, 0f, 1f));
			}
			shadowMesh.Draw();
			num = (float)Math.Cos(SineAccumulator);
			foreach (Group key2 in axisPerGroup.Keys)
			{
				float m3 = key2.TextureMatrix.Value.Value.M11;
				float m4 = key2.TextureMatrix.Value.Value.M11;
				key2.TextureMatrix.Set(new Matrix(m3, 0f, 0f, 0f, 0f, m4, 0f, 0f, (0f - num) / 100f + 0.1f, num / 100f + 0.1f, 1f, 0f, 0f, 0f, 0f, 1f));
			}
			shadowMesh.Draw();
		}
		else
		{
			shadowEffect.Pass = CloudShadowPasses.Standard;
			float depthBias = graphicsDevice.GetRasterCombiner().DepthBias;
			float slopeScaleDepthBias = graphicsDevice.GetRasterCombiner().SlopeScaleDepthBias;
			graphicsDevice.GetRasterCombiner().DepthBias = 0f;
			graphicsDevice.GetRasterCombiner().SlopeScaleDepthBias = 0f;
			Color color = new Color(LevelManager.ActualAmbient.ToVector3() / 2f);
			graphicsDevice.SetBlendingMode(BlendingMode.Subtract);
			TargetRenderer.DrawFullscreen(color);
			graphicsDevice.SetBlendingMode(BlendingMode.Multiply);
			shadowMesh.Draw();
			graphicsDevice.SetBlendingMode(BlendingMode.Additive);
			TargetRenderer.DrawFullscreen(color);
			graphicsDevice.GetRasterCombiner().DepthBias = depthBias;
			graphicsDevice.GetRasterCombiner().SlopeScaleDepthBias = slopeScaleDepthBias;
		}
		graphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
		CameraManager.ViewOffset += viewOffset;
		EngineState.SkyRender = false;
		graphicsDevice.PrepareStencilWrite(StencilMask.None);
	}

	public override void Draw(GameTime gameTime)
	{
		if (EngineState.Loading || LevelManager.Sky == null || Host.BgLayers == null)
		{
			return;
		}
		base.GraphicsDevice.SamplerStates[0] = (LevelManager.Sky.VerticalTiling ? SamplerState.PointWrap : SamplerStates.PointUWrapVClamp);
		foreach (Group group in Host.BgLayers.Groups)
		{
			group.Enabled = group.AlwaysOnTop ?? false;
		}
		Host.BgLayers.Draw();
	}
}
