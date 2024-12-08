using System;
using System.Linq;
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

internal class TempleOfLoveHost : DrawableGameComponent
{
	private enum Phases
	{
		None,
		CrumbleOut,
		ShineReboot
	}

	private static readonly Vector3 HeartCenter = new Vector3(10f, 24.5f, 10f);

	private readonly Vector3[] PieceOffsets = new Vector3[3]
	{
		new Vector3(-0.5f, -0.5f, 0f),
		new Vector3(0.5f, -0.5f, 0f),
		new Vector3(0.5f, 0.5f, 0f)
	};

	private Phases Phase;

	private Mesh WireHeart;

	private Mesh CrumblingHeart;

	private Mesh RaysMesh;

	private Mesh FlareMesh;

	private SoundEffect sRayWhiteout;

	private float TimeAccumulator;

	private float WireHeartFactor = 1f;

	private float PhaseTime;

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	[ServiceDependency]
	public ITrixelParticleSystems ParticleSystems { get; set; }

	public TempleOfLoveHost(Game game)
		: base(game)
	{
		base.DrawOrder = 100;
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void TryInitialize()
	{
		bool enabled = (base.Visible = LevelManager.Name == "TEMPLE_OF_LOVE");
		base.Enabled = enabled;
		Destroy();
		if (base.Enabled && GameState.SaveData.HasDoneHeartReboot)
		{
			BackgroundPlane[] array = LevelManager.BackgroundPlanes.Values.ToArray();
			foreach (BackgroundPlane backgroundPlane in array)
			{
				if (backgroundPlane.ActorType == ActorType.Waterfall || backgroundPlane.ActorType == ActorType.Trickle || backgroundPlane.TextureName.Contains("water") || backgroundPlane.TextureName.Contains("fountain") || backgroundPlane.AttachedPlane.HasValue)
				{
					LevelManager.RemovePlane(backgroundPlane);
				}
			}
			enabled = (base.Visible = false);
			base.Enabled = enabled;
		}
		if (!base.Enabled)
		{
			return;
		}
		sRayWhiteout = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Ending/HexRebuild/RayWhiteout");
		WireHeart = new Mesh();
		CrumblingHeart = new Mesh();
		Color pink = Color.Pink;
		WireHeart.AddGroup().Geometry = new IndexedUserPrimitives<FezVertexPositionColor>(new FezVertexPositionColor[12]
		{
			new FezVertexPositionColor(new Vector3(-1f, -1f, -0.5f), pink),
			new FezVertexPositionColor(new Vector3(-1f, 0f, -0.5f), pink),
			new FezVertexPositionColor(new Vector3(0f, 0f, -0.5f), pink),
			new FezVertexPositionColor(new Vector3(0f, 1f, -0.5f), pink),
			new FezVertexPositionColor(new Vector3(1f, 1f, -0.5f), pink),
			new FezVertexPositionColor(new Vector3(1f, -1f, -0.5f), pink),
			new FezVertexPositionColor(new Vector3(-1f, -1f, 0.5f), pink),
			new FezVertexPositionColor(new Vector3(-1f, 0f, 0.5f), pink),
			new FezVertexPositionColor(new Vector3(0f, 0f, 0.5f), pink),
			new FezVertexPositionColor(new Vector3(0f, 1f, 0.5f), pink),
			new FezVertexPositionColor(new Vector3(1f, 1f, 0.5f), pink),
			new FezVertexPositionColor(new Vector3(1f, -1f, 0.5f), pink)
		}, new int[36]
		{
			0, 1, 1, 2, 2, 3, 3, 4, 4, 5,
			5, 0, 6, 7, 7, 8, 8, 9, 9, 10,
			10, 11, 11, 6, 0, 6, 1, 7, 2, 8,
			3, 9, 4, 10, 5, 11
		}, PrimitiveType.LineList);
		Vector3[] pieceOffsets = PieceOffsets;
		foreach (Vector3 origin in pieceOffsets)
		{
			WireHeart.AddWireframeBox(Vector3.One, origin, new Color(new Vector4(Color.DeepPink.ToVector3(), 0.125f)), centeredOnOrigin: true);
		}
		Trile[] array2 = new Trile[8]
		{
			LevelManager.TrileSet.Triles[244],
			LevelManager.TrileSet.Triles[245],
			LevelManager.TrileSet.Triles[251],
			LevelManager.TrileSet.Triles[246],
			LevelManager.TrileSet.Triles[247],
			LevelManager.TrileSet.Triles[248],
			LevelManager.TrileSet.Triles[249],
			LevelManager.TrileSet.Triles[250]
		};
		int num = 0;
		pieceOffsets = PieceOffsets;
		foreach (Vector3 position in pieceOffsets)
		{
			Trile[] array3 = array2;
			foreach (Trile trile in array3)
			{
				Group group = CrumblingHeart.AddGroup();
				group.Geometry = new IndexedUserPrimitives<VertexPositionNormalTextureInstance>(trile.Geometry.Vertices.ToArray(), trile.Geometry.Indices, trile.Geometry.PrimitiveType);
				group.Position = position;
				group.Enabled = GameState.SaveData.PiecesOfHeart > num;
				group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)Math.PI / 2f);
			}
			num++;
		}
		Mesh wireHeart = WireHeart;
		Quaternion rotation = (CrumblingHeart.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -(float)Math.PI / 4f));
		wireHeart.Rotation = rotation;
		WireHeart.BakeTransform<FezVertexPositionColor>();
		CrumblingHeart.BakeTransform<VertexPositionNormalTextureInstance>();
		foreach (Group group2 in CrumblingHeart.Groups)
		{
			IndexedUserPrimitives<VertexPositionNormalTextureInstance> indexedUserPrimitives = group2.Geometry as IndexedUserPrimitives<VertexPositionNormalTextureInstance>;
			Vector3 zero = Vector3.Zero;
			VertexPositionNormalTextureInstance[] vertices = indexedUserPrimitives.Vertices;
			foreach (VertexPositionNormalTextureInstance vertexPositionNormalTextureInstance in vertices)
			{
				zero += vertexPositionNormalTextureInstance.Position;
			}
			group2.CustomData = zero / indexedUserPrimitives.Vertices.Length;
		}
		RaysMesh = new Mesh
		{
			Blending = BlendingMode.Additive,
			DepthWrites = false
		};
		FlareMesh = new Mesh
		{
			Blending = BlendingMode.Alphablending,
			SamplerState = SamplerState.AnisotropicClamp,
			DepthWrites = false,
			AlwaysOnTop = true
		};
		FlareMesh.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true);
		DrawActionScheduler.Schedule(delegate
		{
			WireHeart.Effect = new DefaultEffect.VertexColored();
			CrumblingHeart.Effect = new DefaultEffect.LitTextured
			{
				Specular = true,
				Emissive = 0.5f,
				AlphaIsEmissive = true
			};
			CrumblingHeart.Texture = LevelMaterializer.TrilesMesh.Texture;
			RaysMesh.Effect = new DefaultEffect.VertexColored();
			FlareMesh.Effect = new DefaultEffect.Textured();
			FlareMesh.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/flare_alpha");
		});
		Mesh wireHeart2 = WireHeart;
		Vector3 position2 = (CrumblingHeart.Position = HeartCenter);
		wireHeart2.Position = position2;
	}

	private void Destroy()
	{
		WireHeart = (CrumblingHeart = null);
		TimeAccumulator = 0f;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused || GameState.InMap || GameState.Loading || GameState.InMenuCube)
		{
			return;
		}
		TimeAccumulator += (float)gameTime.ElapsedGameTime.TotalSeconds;
		Mesh wireHeart = WireHeart;
		Vector3 position = (CrumblingHeart.Position = HeartCenter + Vector3.UnitY * (float)Math.Sin(TimeAccumulator / 2f) / 2f);
		wireHeart.Position = position;
		WireHeart.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)gameTime.ElapsedGameTime.TotalSeconds / 2f);
		CrumblingHeart.Rotation = WireHeart.Rotation;
		if (GameState.SaveData.HasDoneHeartReboot && Phase == Phases.None)
		{
			Phase = Phases.CrumbleOut;
		}
		switch (Phase)
		{
		case Phases.CrumbleOut:
		{
			PhaseTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
			foreach (Group group in CrumblingHeart.Groups)
			{
				Vector3 vector3 = (Vector3)group.CustomData;
				float num3 = 1f - (vector3.Y * 2f + vector3.Z + vector3.X);
				float num4 = (float)Math.Pow(Math.Max(PhaseTime - num3, 0f) * 0.875f, 2.0);
				group.Position = new Vector3(0f, num4, 0f) + vector3 * num4 / 5f;
				group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(vector3), num4 / 10f);
			}
			int num5 = 1 - RandomHelper.Probability(PhaseTime / 7f).AsNumeric();
			WireHeartFactor = MathHelper.Lerp(WireHeartFactor, num5, 0.125f + PhaseTime / 7f * 0.2f);
			if (PhaseTime > 7f)
			{
				PhaseTime = 0f;
				WireHeartFactor = 0f;
				Phase = Phases.ShineReboot;
				SoundManager.PlayNewSong(null, 4f);
				SoundManager.MuteAmbienceTracks();
				SoundManager.KillSounds(4f);
				sRayWhiteout.Emit();
			}
			break;
		}
		case Phases.ShineReboot:
			PhaseTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
			foreach (Group group2 in CrumblingHeart.Groups)
			{
				Vector3 vector2 = (Vector3)group2.CustomData;
				float num = 1f - (vector2.Y * 2f + vector2.Z + vector2.X);
				float num2 = (float)Math.Pow(Math.Max(PhaseTime + 7f - num, 0f) * 0.875f, 2.0);
				group2.Position = new Vector3(0f, num2, 0f) + vector2 * num2 / 5f;
				group2.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(vector2), num2 / 10f);
			}
			UpdateRays((float)gameTime.ElapsedGameTime.TotalSeconds);
			if (PhaseTime > 4f)
			{
				SmoothReboot();
			}
			break;
		}
	}

	private void SmoothReboot()
	{
		ServiceHelper.AddComponent(new Intro(base.Game)
		{
			Fake = true,
			FakeLevel = "GOMEZ_HOUSE",
			Glitch = false
		});
		Waiters.Wait(0.10000000149011612, delegate
		{
			Destroy();
			bool enabled = (base.Visible = false);
			base.Enabled = enabled;
		});
		base.Enabled = false;
	}

	private void UpdateRays(float elapsedSeconds)
	{
		bool num = PhaseTime > 1.5f;
		MakeRay();
		if (num)
		{
			MakeRay();
		}
		for (int num2 = RaysMesh.Groups.Count - 1; num2 >= 0; num2--)
		{
			Group group = RaysMesh.Groups[num2];
			DotHost.RayState rayState = group.CustomData as DotHost.RayState;
			rayState.Age += elapsedSeconds * 0.15f;
			group.Material.Diffuse = Vector3.One * FezMath.Saturate(rayState.Age * 8f);
			group.Scale *= new Vector3(1.5f, 1f, 1f);
			if (rayState.Age > 1f)
			{
				RaysMesh.RemoveGroupAt(num2);
			}
		}
		RaysMesh.AlwaysOnTop = false;
		Mesh flareMesh = FlareMesh;
		Vector3 position = (RaysMesh.Position = HeartCenter);
		flareMesh.Position = position;
		Mesh flareMesh2 = FlareMesh;
		Quaternion rotation2 = (RaysMesh.Rotation = CameraManager.Rotation);
		flareMesh2.Rotation = rotation2;
		FlareMesh.Material.Opacity = Easing.EaseIn(FezMath.Saturate(PhaseTime / 2.5f), EasingType.Cubic);
		FlareMesh.Scale = Vector3.One + RaysMesh.Scale * Easing.EaseIn((PhaseTime - 0.25f) / 1.75f, EasingType.Decic) * 4f;
	}

	private void MakeRay()
	{
		if (RaysMesh.Groups.Count < 150 && RandomHelper.Probability(0.1 + (double)Easing.EaseIn(FezMath.Saturate(PhaseTime / 1.75f), EasingType.Sine) * 0.9))
		{
			float num = (RandomHelper.Probability(0.75) ? 0.1f : 0.4f);
			Group group = RaysMesh.AddGroup();
			group.Geometry = new IndexedUserPrimitives<FezVertexPositionColor>(new FezVertexPositionColor[6]
			{
				new FezVertexPositionColor(new Vector3(0f, num / 2f * 0.5f, 0f), Color.White),
				new FezVertexPositionColor(new Vector3(1f, num / 2f, 0f), Color.White),
				new FezVertexPositionColor(new Vector3(1f, num / 2f * 0.5f, 0f), Color.White),
				new FezVertexPositionColor(new Vector3(1f, (0f - num) / 2f * 0.5f, 0f), Color.White),
				new FezVertexPositionColor(new Vector3(1f, (0f - num) / 2f, 0f), Color.White),
				new FezVertexPositionColor(new Vector3(0f, (0f - num) / 2f * 0.5f, 0f), Color.White)
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
			group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, RandomHelper.Between(0.0, -3.1415927410125732)) * Quaternion.CreateFromAxisAngle(Vector3.Forward, RandomHelper.Between(0.0, 6.2831854820251465));
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (!GameState.Loading)
		{
			GraphicsDevice graphicsDevice = base.GraphicsDevice;
			graphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
			CrumblingHeart.AlwaysOnTop = true;
			CrumblingHeart.Position += CameraManager.InverseView.Forward * 3f;
			CrumblingHeart.Draw();
			graphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
			CrumblingHeart.AlwaysOnTop = false;
			CrumblingHeart.Position -= CameraManager.InverseView.Forward * 3f;
			CrumblingHeart.Draw();
			graphicsDevice.PrepareStencilWrite(StencilMask.Wirecube);
			WireHeart.DepthWrites = false;
			WireHeart.Material.Opacity = (0.05f + Math.Abs((float)Math.Cos(TimeAccumulator * 3f)) * 0.2f) * WireHeartFactor;
			float num = 0.0625f / CameraManager.PixelsPerTrixel * Math.Abs((float)Math.Sin(TimeAccumulator)) * 8f;
			WireHeart.Position += num * Vector3.UnitY;
			WireHeart.Draw();
			WireHeart.Position -= num * Vector3.UnitY;
			WireHeart.Position -= num * Vector3.UnitY;
			WireHeart.Draw();
			WireHeart.Position += num * Vector3.UnitY;
			WireHeart.Position += num * Vector3.UnitX;
			WireHeart.Draw();
			WireHeart.Position -= num * Vector3.UnitX;
			WireHeart.Position -= num * Vector3.UnitX;
			WireHeart.Draw();
			WireHeart.Position += num * Vector3.UnitX;
			WireHeart.Position += num * Vector3.UnitZ;
			WireHeart.Draw();
			WireHeart.Position -= num * Vector3.UnitZ;
			WireHeart.Position -= num * Vector3.UnitZ;
			WireHeart.Draw();
			WireHeart.Position += num * Vector3.UnitZ;
			WireHeart.Material.Opacity = WireHeartFactor;
			WireHeart.Draw();
			graphicsDevice.PrepareStencilWrite(StencilMask.None);
			if (Phase == Phases.ShineReboot)
			{
				RaysMesh.Draw();
				FlareMesh.Draw();
			}
		}
	}
}
