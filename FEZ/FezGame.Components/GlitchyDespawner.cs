using System;
using System.Linq;
using Common;
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

internal class GlitchyDespawner : DrawableGameComponent
{
	private const float StarsDuration = 1f;

	private const float FlashDuration = 1f;

	private const float FadeInDuration = 0.5f;

	private ArtObjectInstance AoInstance;

	private TrileInstance TrileInstance;

	private readonly Vector3 TreasureCenter;

	private readonly bool CreateTreasure;

	private readonly bool IsArtObject;

	public bool FlashOnSpawn;

	public ActorType ActorToSpawn = ActorType.SecretCube;

	private Mesh SpawnMesh;

	private Texture2D StarsTexture;

	private DefaultEffect FullbrightEffect;

	private TimeSpan SinceAlive;

	private int sinceColorSwapped;

	private int nextSwapIn;

	private bool redVisible;

	private bool greenVisible;

	private bool blueVisible;

	private bool hasCreatedTreasure;

	[ServiceDependency]
	public ISoundManager SoundManager { get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { get; set; }

	[ServiceDependency]
	public ILightingPostProcess LightingPostProcess { get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	private GlitchyDespawner(Game game)
		: base(game)
	{
		base.UpdateOrder = -2;
		base.DrawOrder = 10;
	}

	public GlitchyDespawner(Game game, ArtObjectInstance instance)
		: this(game)
	{
		AoInstance = instance;
		CreateTreasure = false;
		instance.Hidden = true;
		IsArtObject = true;
	}

	public GlitchyDespawner(Game game, ArtObjectInstance instance, Vector3 treasureCenter)
		: this(game)
	{
		AoInstance = instance;
		TreasureCenter = treasureCenter;
		CreateTreasure = true;
		instance.Hidden = true;
		IsArtObject = true;
	}

	public GlitchyDespawner(Game game, TrileInstance instance)
		: this(game)
	{
		base.UpdateOrder = -2;
		base.DrawOrder = 10;
		TrileInstance = instance;
		CreateTreasure = false;
		instance.Hidden = true;
		IsArtObject = false;
	}

	public GlitchyDespawner(Game game, TrileInstance instance, Vector3 treasureCenter)
		: this(game)
	{
		base.UpdateOrder = -2;
		base.DrawOrder = 10;
		TrileInstance = instance;
		TreasureCenter = treasureCenter;
		CreateTreasure = true;
		instance.Hidden = true;
		IsArtObject = false;
	}

	public override void Initialize()
	{
		base.Initialize();
		if (!IsArtObject)
		{
			LevelMaterializer.CullInstanceOut(TrileInstance);
		}
		SpawnMesh = new Mesh
		{
			SamplerState = SamplerState.PointClamp,
			DepthWrites = false,
			Effect = new CubemappedEffect()
		};
		IIndexedPrimitiveCollection geometry2;
		if (IsArtObject)
		{
			ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Matrix> geometry = AoInstance.ArtObject.Geometry;
			geometry2 = new IndexedUserPrimitives<VertexPositionNormalTextureInstance>(geometry.Vertices, geometry.Indices, geometry.PrimitiveType);
			AoInstance.Material = new Material();
		}
		else
		{
			ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4> geometry3 = TrileInstance.Trile.Geometry;
			geometry2 = new IndexedUserPrimitives<VertexPositionNormalTextureInstance>(geometry3.Vertices, geometry3.Indices, geometry3.PrimitiveType);
		}
		Group group = SpawnMesh.AddGroup();
		group.Geometry = geometry2;
		group.Rotation = (IsArtObject ? AoInstance.Rotation : Quaternion.CreateFromAxisAngle(Vector3.UnitY, TrileInstance.Phi));
		if (!IsArtObject)
		{
			group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, TrileInstance.Phi);
			if (TrileInstance.Trile.ActorSettings.Type == ActorType.CubeShard || TrileInstance.Trile.ActorSettings.Type == ActorType.SecretCube || TrileInstance.Trile.ActorSettings.Type == ActorType.PieceOfHeart)
			{
				group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Left, (float)Math.Asin(Math.Sqrt(2.0) / Math.Sqrt(3.0))) * Quaternion.CreateFromAxisAngle(Vector3.Down, (float)Math.PI / 4f) * group.Rotation;
			}
		}
		SpawnMesh.Position = (IsArtObject ? AoInstance.Position : TrileInstance.Center);
		StarsTexture = CMProvider.Global.Load<Texture2D>("Other Textures/black_hole/Stars");
		FullbrightEffect = new DefaultEffect.Textured
		{
			Fullbright = true
		};
		CMProvider.Global.Load<SoundEffect>("Sounds/MiscActors/GlitchyRespawn").EmitAt(SpawnMesh.Position);
		LightingPostProcess.DrawOnTopLights += DrawLights;
		LevelManager.LevelChanging += Kill;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		SpawnMesh.Dispose();
		FullbrightEffect.Dispose();
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
		SpawnMesh.Position = (IsArtObject ? AoInstance.Position : TrileInstance.Center);
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
		if (SinceAlive.TotalSeconds > 2.0 && !hasCreatedTreasure && CreateTreasure)
		{
			if (!IsArtObject)
			{
				TrileInstance.PhysicsState = null;
				LevelManager.ClearTrile(TrileInstance);
			}
			if (FlashOnSpawn)
			{
				ServiceHelper.AddComponent(new ScreenFade(base.Game)
				{
					FromColor = Color.White,
					ToColor = ColorEx.TransparentWhite,
					Duration = 0.4f
				});
			}
			Trile trile = LevelManager.ActorTriles(ActorToSpawn).FirstOrDefault();
			if (trile != null)
			{
				Vector3 position = TreasureCenter - Vector3.One / 2f;
				LevelManager.ClearTrile(new TrileEmplacement(position));
				IGameLevelManager levelManager = LevelManager;
				TrileInstance obj = new TrileInstance(position, trile.Id)
				{
					OriginalEmplacement = new TrileEmplacement(position)
				};
				TrileInstance trileInstance = obj;
				levelManager.RestoreTrile(obj);
				trileInstance.Foreign = true;
				if (trileInstance.InstanceId == -1)
				{
					LevelMaterializer.CullInstanceIn(trileInstance);
				}
			}
			else
			{
				Logger.Log("Glitchy Despawner", LogSeverity.Warning, "No secret cube trile in trileset!");
			}
			hasCreatedTreasure = true;
		}
		if (SinceAlive.TotalSeconds > 2.5)
		{
			if (IsArtObject)
			{
				LevelManager.RemoveArtObject(AoInstance);
			}
			else
			{
				TrileInstance.PhysicsState = null;
				LevelManager.ClearTrile(TrileInstance);
			}
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
		if (SinceAlive.TotalSeconds > 2.0)
		{
			TargetRenderer.DrawFullscreen(new Color(1f, 1f, 1f, 1f - SpawnMesh.Material.Opacity));
		}
		else if (SinceAlive.TotalSeconds < 1.0)
		{
			TargetRenderer.DrawFullscreen(Color.White);
			base.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
			TargetRenderer.DrawFullscreen(StarsTexture, textureMatrix, new Color(1f, 1f, 1f, alpha));
		}
		else if (SinceAlive.TotalSeconds > 1.0)
		{
			TargetRenderer.DrawFullscreen(new Color(redVisible ? 1 : 0, greenVisible ? 1 : 0, blueVisible ? 1 : 0, 1f));
		}
		base.GraphicsDevice.PrepareStencilWrite(StencilMask.None);
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
			SpawnMesh.Material.Opacity = FezMath.Saturate(((float)SinceAlive.TotalSeconds - 2f) / 0.5f);
			if (IsArtObject)
			{
				AoInstance.Material.Opacity = SpawnMesh.Material.Opacity;
				AoInstance.MarkDirty();
			}
			(SpawnMesh.Effect as CubemappedEffect).Pass = LightingEffectPass.Pre;
			SpawnMesh.Draw();
			(SpawnMesh.Effect as CubemappedEffect).Pass = LightingEffectPass.Main;
		}
	}
}
