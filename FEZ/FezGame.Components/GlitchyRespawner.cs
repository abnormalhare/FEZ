using System;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class GlitchyRespawner : DrawableGameComponent
{
	private const float StarsDuration = 1f;

	private const float FlashDuration = 1f;

	private const float FadeInDuration = 0.25f;

	private readonly ITargetRenderingManager TargetRenderer;

	private readonly ILightingPostProcess LightingPostProcess;

	private readonly ILevelMaterializer LevelMaterializer;

	private readonly IGameStateManager GameState;

	private readonly IDefaultCameraManager CameraManager;

	private readonly IGameLevelManager LevelManager;

	private readonly IContentManagerProvider CMProvider;

	private readonly TrileInstance Instance;

	private readonly bool EmitOrNot;

	private Mesh SpawnMesh;

	private Texture2D StarsTexture;

	public bool DontCullIn;

	private TimeSpan SinceAlive;

	private static volatile DefaultEffect FullbrightEffect;

	private static volatile CubemappedEffect CubemappedEffect;

	private static readonly object StaticLock = new object();

	private int sinceColorSwapped;

	private int nextSwapIn;

	private bool redVisible;

	private bool greenVisible;

	private bool blueVisible;

	public GlitchyRespawner(Game game, TrileInstance instance)
		: this(game, instance, soundEmitter: true)
	{
	}

	public GlitchyRespawner(Game game, TrileInstance instance, bool soundEmitter)
		: base(game)
	{
		base.UpdateOrder = -2;
		base.DrawOrder = 10;
		Instance = instance;
		EmitOrNot = soundEmitter;
		TargetRenderer = ServiceHelper.Get<ITargetRenderingManager>();
		LightingPostProcess = ServiceHelper.Get<ILightingPostProcess>();
		LevelMaterializer = ServiceHelper.Get<ILevelMaterializer>();
		GameState = ServiceHelper.Get<IGameStateManager>();
		CameraManager = ServiceHelper.Get<IDefaultCameraManager>();
		LevelManager = ServiceHelper.Get<IGameLevelManager>();
		CMProvider = ServiceHelper.Get<IContentManagerProvider>();
	}

	public override void Initialize()
	{
		base.Initialize();
		lock (StaticLock)
		{
			if (FullbrightEffect == null)
			{
				FullbrightEffect = new DefaultEffect.Textured
				{
					Fullbright = true
				};
			}
			if (CubemappedEffect == null)
			{
				CubemappedEffect = new CubemappedEffect();
			}
		}
		SpawnMesh = new Mesh
		{
			SamplerState = SamplerState.PointClamp,
			DepthWrites = false,
			Effect = CubemappedEffect
		};
		ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4> geometry = Instance.Trile.Geometry;
		IndexedUserPrimitives<VertexPositionNormalTextureInstance> geometry2 = new IndexedUserPrimitives<VertexPositionNormalTextureInstance>(geometry.Vertices, geometry.Indices, geometry.PrimitiveType);
		Group group = SpawnMesh.AddGroup();
		group.Geometry = geometry2;
		group.Texture = LevelMaterializer.TrilesMesh.Texture;
		if (Instance.Trile.ActorSettings.Type.IsPickable())
		{
			Instance.Phi = (float)FezMath.Round(Instance.Phi / ((float)Math.PI / 2f)) * ((float)Math.PI / 2f);
		}
		group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, Instance.Phi);
		if (Instance.Trile.ActorSettings.Type == ActorType.CubeShard || Instance.Trile.ActorSettings.Type == ActorType.SecretCube || Instance.Trile.ActorSettings.Type == ActorType.PieceOfHeart)
		{
			group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Left, (float)Math.Asin(Math.Sqrt(2.0) / Math.Sqrt(3.0))) * Quaternion.CreateFromAxisAngle(Vector3.Down, (float)Math.PI / 4f) * group.Rotation;
		}
		Instance.Foreign = true;
		StarsTexture = CMProvider.Global.Load<Texture2D>("Other Textures/black_hole/Stars");
		if (EmitOrNot)
		{
			SoundEmitter soundEmitter = CMProvider.Global.Load<SoundEffect>("Sounds/MiscActors/GlitchyRespawn").EmitAt(Instance.Center);
			soundEmitter.PauseViewTransitions = true;
			soundEmitter.FactorizeVolume = true;
		}
		LightingPostProcess.DrawOnTopLights += DrawLights;
		LevelManager.LevelChanging += Kill;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		SpawnMesh.Dispose(disposeEffect: false);
		LightingPostProcess.DrawOnTopLights -= DrawLights;
		LevelManager.LevelChanging -= Kill;
	}

	private void Kill()
	{
		ServiceHelper.RemoveComponent(this);
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused || GameState.InMap || !CameraManager.ActionRunning || !CameraManager.Viewpoint.IsOrthographic())
		{
			return;
		}
		SinceAlive += gameTime.ElapsedGameTime;
		SpawnMesh.Position = Instance.Center;
		if (sinceColorSwapped++ >= nextSwapIn)
		{
			int num = RandomHelper.Random.Next(0, 4);
			redVisible = num == 0;
			greenVisible = num == 1;
			blueVisible = num == 2;
			if (num == 3)
			{
				int num2 = RandomHelper.Random.Next(0, 3);
				if (num2 == 0)
				{
					blueVisible = (redVisible = true);
				}
				if (num2 == 1)
				{
					greenVisible = (redVisible = true);
				}
				if (num2 == 2)
				{
					blueVisible = (greenVisible = true);
				}
			}
			sinceColorSwapped = 0;
			nextSwapIn = RandomHelper.Random.Next(1, 6);
		}
		if (SinceAlive.TotalSeconds > 2.25)
		{
			Instance.Hidden = false;
			Instance.Enabled = true;
			if (Instance.Trile.ActorSettings.Type.IsPickable())
			{
				Instance.PhysicsState.Respawned = true;
				Instance.PhysicsState.Vanished = false;
			}
			LevelManager.RestoreTrile(Instance);
			LevelMaterializer.UpdateInstance(Instance);
			ServiceHelper.RemoveComponent(this);
		}
	}

	public override void Draw(GameTime gameTime)
	{
		float alpha = FezMath.Saturate(Easing.EaseOut((float)SinceAlive.TotalSeconds / 1f, EasingType.Quintic));
		base.GraphicsDevice.PrepareStencilWrite(StencilMask.Glitch);
		base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
		SpawnMesh.Draw();
		base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
		base.GraphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.Glitch);
		float viewScale = base.GraphicsDevice.GetViewScale();
		float num = CameraManager.Radius / ((float)StarsTexture.Width / 16f) / viewScale;
		float num2 = CameraManager.Radius / CameraManager.AspectRatio / ((float)StarsTexture.Height / 16f) / viewScale;
		Matrix textureMatrix = new Matrix(num, 0f, 0f, 0f, 0f, num2, 0f, 0f, (0f - num) / 2f, (0f - num2) / 2f, 1f, 0f, 0f, 0f, 0f, 1f);
		base.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
		TargetRenderer.DrawFullscreen(StarsTexture, textureMatrix, new Color(1f, 1f, 1f, alpha));
		if (SinceAlive.TotalSeconds > 2.0)
		{
			TargetRenderer.DrawFullscreen(Color.White);
		}
		else if (SinceAlive.TotalSeconds > 1.0)
		{
			TargetRenderer.DrawFullscreen(new Color(redVisible ? 1 : 0, greenVisible ? 1 : 0, blueVisible ? 1 : 0, 1f));
		}
		base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
		base.GraphicsDevice.GetDssCombiner().StencilPass = StencilOperation.Zero;
		TargetRenderer.DrawFullscreen(Color.Black);
		base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
		base.GraphicsDevice.PrepareStencilWrite(StencilMask.None);
		if (SinceAlive.TotalSeconds > 2.0)
		{
			float opacity = FezMath.Saturate(Easing.EaseIn(((float)SinceAlive.TotalSeconds - 2f) / 0.25f, EasingType.Quadratic));
			SpawnMesh.Blending = BlendingMode.Alphablending;
			SpawnMesh.Material.Opacity = opacity;
			SpawnMesh.Material.Diffuse = new Vector3(1f);
			SpawnMesh.Draw();
		}
	}

	private void DrawLights()
	{
		BaseEffect effect = SpawnMesh.Effect;
		Texture texture = SpawnMesh.FirstGroup.Texture;
		SpawnMesh.FirstGroup.Texture = null;
		SpawnMesh.Effect = FullbrightEffect;
		SpawnMesh.Draw();
		SpawnMesh.FirstGroup.Texture = texture;
		SpawnMesh.Effect = effect;
		if (SinceAlive.TotalSeconds > 2.0)
		{
			SpawnMesh.Material.Opacity = FezMath.Saturate(Easing.EaseIn(((float)SinceAlive.TotalSeconds - 2f) / 0.25f, EasingType.Quadratic));
			(SpawnMesh.Effect as CubemappedEffect).Pass = LightingEffectPass.Pre;
			SpawnMesh.Draw();
			(SpawnMesh.Effect as CubemappedEffect).Pass = LightingEffectPass.Main;
		}
	}
}
