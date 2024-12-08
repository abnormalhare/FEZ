using System;
using System.Linq;
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

public class LoadingScreen : DrawableGameComponent
{
	private const float FadeTime = 0.5f;

	private Mesh mesh;

	private float sinceBgShown;

	private float sinceCubeShown = -0.5f;

	private float bgOpacity;

	private float cubeOpacity;

	private Texture2D starBack;

	private FakeDot fakeDot;

	private SoundEffect sDrone;

	private SoundEffectInstance iDrone;

	private bool loadingCubeWasUnlit;

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	public IDotManager DotManager { get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { get; set; }

	public LoadingScreen(Game game)
		: base(game)
	{
		base.DrawOrder = 2100;
	}

	protected override void LoadContent()
	{
		TrileSet ts = CMProvider.Global.Load<TrileSet>("Trile Sets/LOADING");
		mesh = new Mesh
		{
			Blending = BlendingMode.Alphablending,
			SamplerState = SamplerState.PointClamp,
			AlwaysOnTop = false,
			DepthWrites = true,
			Rotation = Quaternion.CreateFromAxisAngle(Vector3.Right, (float)Math.Asin(Math.Sqrt(2.0) / Math.Sqrt(3.0))) * Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 4f)
		};
		DrawActionScheduler.Schedule(delegate
		{
			mesh.Texture = ts.TextureAtlas;
			loadingCubeWasUnlit = false;
			mesh.Effect = new DefaultEffect.LitTextured
			{
				Specular = true,
				AlphaIsEmissive = true,
				Emissive = 0.5f,
				ForcedViewMatrix = Matrix.CreateLookAt(new Vector3(0f, 0f, 10f), Vector3.Zero, Vector3.Up)
			};
			starBack = CMProvider.Global.Load<Texture2D>("Other Textures/hud/starback");
		});
		Group group = mesh.AddGroup();
		ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4> geometry = ts.Triles[0].Geometry;
		group.Geometry = new IndexedUserPrimitives<VertexPositionNormalTextureInstance>(geometry.Vertices.ToArray(), geometry.Indices, geometry.PrimitiveType);
		group.Scale = new Vector3(0.95f);
		sDrone = CMProvider.Global.Load<SoundEffect>("Sounds/Intro/FezLogoDrone");
		ServiceHelper.AddComponent(fakeDot = new FakeDot(ServiceHelper.Game));
		LevelManager.LevelChanged += delegate
		{
			DrawActionScheduler.Schedule(delegate
			{
				bool flag = LevelManager.WaterType == LiquidType.Sewer || LevelManager.WaterType == LiquidType.Lava || LevelManager.BlinkingAlpha;
				if (loadingCubeWasUnlit != flag || mesh.Effect == null)
				{
					if (mesh.Effect != null)
					{
						mesh.Effect.Dispose();
					}
					if (flag)
					{
						mesh.Effect = new DefaultEffect.Textured
						{
							AlphaIsEmissive = true
						};
					}
					else
					{
						mesh.Effect = new DefaultEffect.LitTextured
						{
							Specular = true,
							Emissive = 0.5f,
							AlphaIsEmissive = true
						};
					}
					mesh.Effect.ForcedViewMatrix = Matrix.CreateLookAt(new Vector3(0f, 0f, 10f), Vector3.Zero, Vector3.Up);
					mesh.TextureMatrix.Dirty = true;
					loadingCubeWasUnlit = flag;
				}
			});
		};
	}

	private void CreateDrone()
	{
		iDrone = sDrone.CreateInstance();
		iDrone.IsLooped = true;
		iDrone.Volume = 0f;
		iDrone.Play();
	}

	private void KillDrone()
	{
		Waiters.Interpolate(1.0, delegate(float s)
		{
			iDrone.Volume = FezMath.Saturate(1f - s);
		}, delegate
		{
			iDrone.Stop();
			iDrone.Dispose();
			iDrone = null;
		});
		foreach (SoundEmitter emitter in SoundManager.Emitters)
		{
			emitter.VolumeMaster = 1f;
		}
		SoundManager.MusicVolumeFactor = 1f;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.SkipLoadScreen)
		{
			return;
		}
		if (GameState.SkipLoadBackground)
		{
			sinceBgShown = 0f;
		}
		float num = (float)gameTime.ElapsedGameTime.TotalSeconds;
		if ((GameState.Loading && (!GameState.ScheduleLoadEnd || GameState.DotLoading)) || GameState.ForceLoadIcon)
		{
			if (!GameState.SkipLoadBackground && bgOpacity < 1f)
			{
				sinceBgShown += num;
			}
			if (cubeOpacity < 1f && !GameState.DotLoading)
			{
				sinceCubeShown += num;
				cubeOpacity = FezMath.Saturate(sinceCubeShown / 0.5f);
			}
		}
		else
		{
			sinceCubeShown = -0.5f;
			if (!GameState.SkipLoadBackground && bgOpacity > 0f)
			{
				sinceBgShown -= num * 1.25f;
			}
			if (cubeOpacity > 0f)
			{
				cubeOpacity -= num * 1.25f / 0.5f;
				cubeOpacity = FezMath.Saturate(cubeOpacity);
			}
		}
		float num2 = bgOpacity;
		bgOpacity = FezMath.Saturate(sinceBgShown / 0.5f);
		if (num2 == 1f && bgOpacity < num2 && iDrone != null)
		{
			KillDrone();
		}
		if (GameState.DotLoading)
		{
			fakeDot.Update(gameTime);
		}
	}

	public override void Draw(GameTime gameTime)
	{
		GameState.LoadingVisible = false;
		if ((!GameState.DotLoading && cubeOpacity == 0f) || GameState.SkipLoadScreen || GameState.FarawaySettings.InTransition)
		{
			return;
		}
		GameState.LoadingVisible = true;
		if (!GameState.SkipLoadBackground)
		{
			if (GameState.DotLoading)
			{
				float m = Math.Max(base.GraphicsDevice.Viewport.AspectRatio / 1.7777778f, 1f);
				float m2 = Math.Max(1f / base.GraphicsDevice.Viewport.AspectRatio / 0.5625f, 1f);
				base.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
				TargetRenderer.DrawFullscreen(starBack, new Matrix(m, 0f, 0f, 0f, 0f, m2, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 0f), new Color(1f, 1f, 1f, bgOpacity));
			}
			else
			{
				TargetRenderer.DrawFullscreen(new Color(0f, 0f, 0f, bgOpacity));
			}
		}
		if (!GameState.DotLoading)
		{
			base.GraphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Black, 1f, 0);
			float num = (float)base.GraphicsDevice.Viewport.Width / (float)base.GraphicsDevice.Viewport.Height;
			mesh.Effect.ForcedProjectionMatrix = Matrix.CreateOrthographic(14f * num, 14f, 0.1f, 100f);
			mesh.Position = new Vector3(5.5f * num, -4.5f, 0f);
			mesh.Material.Opacity = cubeOpacity;
			mesh.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)gameTime.ElapsedGameTime.TotalSeconds * 3f) * mesh.Rotation;
			mesh.FirstGroup.Position = new Vector3(0f, (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 3.1415927410125732) * 0.2f, 0f);
			mesh.Draw();
			return;
		}
		if (iDrone == null)
		{
			CreateDrone();
		}
		fakeDot.Opacity = bgOpacity;
		fakeDot.Draw(gameTime);
		iDrone.Volume = bgOpacity;
		if (GameState.ScheduleLoadEnd)
		{
			SoundManager.MusicVolumeFactor = 1f - bgOpacity;
		}
		else
		{
			SoundManager.MusicVolumeFactor = Math.Min(SoundManager.MusicVolumeFactor, 1f - bgOpacity);
		}
		if (!GameState.ScheduleLoadEnd)
		{
			return;
		}
		foreach (SoundEmitter emitter in SoundManager.Emitters)
		{
			if (!emitter.Dead)
			{
				emitter.VolumeMaster = 1f - bgOpacity;
				emitter.Update();
			}
		}
	}
}
