using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class BlackHolesHost : DrawableGameComponent, IBlackHoleManager
{
	private class HoleState
	{
		private readonly BlackHolesHost Host;

		private int instanceIndex;

		public Volume Volume { get; private set; }

		public Vector3[] Offsets { get; private set; }

		public Vector3 Center { get; private set; }

		public SoundEmitter Emitter { get; set; }

		public bool Sucking { get; set; }

		public Vector3 Size { get; private set; }

		public bool Visible { get; set; }

		public bool Enabled
		{
			get
			{
				return Volume.Enabled;
			}
			set
			{
				Volume.Enabled = value;
			}
		}

		public float SinceEnabled { get; set; }

		public float RandomVisibility { get; set; }

		public void SetTextureTransform(Vector2 offset, Vector2 scale)
		{
			Host.InstanceData[instanceIndex].M31 = offset.X;
			Host.InstanceData[instanceIndex].M32 = offset.Y;
			Host.InstanceData[instanceIndex].M41 = scale.X;
			Host.InstanceData[instanceIndex].M42 = scale.Y;
		}

		public void SetDiffuse(Vector3 diffuse)
		{
			Host.InstanceData[instanceIndex].M21 = diffuse.X;
			Host.InstanceData[instanceIndex].M22 = diffuse.Y;
			Host.InstanceData[instanceIndex].M23 = diffuse.Z;
		}

		public void SetPositionForPass(int i)
		{
			Host.InstanceData[instanceIndex].M11 = Offsets[i].X;
			Host.InstanceData[instanceIndex].M12 = Offsets[i].Y;
			Host.InstanceData[instanceIndex].M13 = Offsets[i].Z;
		}

		private HoleState(BlackHolesHost host)
		{
			Host = host;
			Offsets = new Vector3[3];
		}

		public HoleState(Volume volume, BlackHolesHost host)
			: this(host)
		{
			Volume = volume;
			Size = volume.To - volume.From;
		}

		public void Build()
		{
			Center = (Volume.From + Volume.To) / 2f;
			BuildMesh();
		}

		private void BuildMesh()
		{
			Vector3 vector = (Volume.To - Volume.From) / 2f;
			float num = vector.Y * 2f;
			Mesh mesh = new Mesh();
			Mesh mesh2 = new Mesh();
			FaceOrientation[] array = new FaceOrientation[4]
			{
				FaceOrientation.Front,
				FaceOrientation.Right,
				FaceOrientation.Back,
				FaceOrientation.Left
			};
			foreach (FaceOrientation faceOrientation in array)
			{
				Vector3 vector2 = (faceOrientation.GetTangent().IsSide() ? faceOrientation.GetTangent() : faceOrientation.GetBitangent()).AsVector();
				Vector3 vector3 = Center + faceOrientation.AsVector() * vector;
				float num2 = Math.Abs(vector.Dot(vector2)) * 2f;
				Vector3 vector4 = vector3 + (vector - new Vector3(0.5f)) * (-vector2 - Vector3.UnitY);
				Vector3 vector5 = vector3 + (vector - new Vector3(0.5f)) * (-vector2 + Vector3.UnitY);
				for (int j = 0; (float)j < num2; j++)
				{
					Vector3 p = vector4 + j * vector2;
					if (!mesh.Groups.Any((Group g) => FezMath.AlmostEqual(g.Position, p)))
					{
						mesh.AddFace(Vector3.One * 2f, Vector3.Zero, faceOrientation, centeredOnOrigin: true).Position = p;
					}
					p = vector5 + j * vector2;
					if (!mesh.Groups.Any((Group g) => FezMath.AlmostEqual(g.Position, p)))
					{
						mesh.AddFace(Vector3.One * 2f, Vector3.Zero, faceOrientation, centeredOnOrigin: true).Position = p;
					}
				}
				Vector3 vector6 = vector3 + (vector - new Vector3(0.5f)) * (-vector2 - Vector3.UnitY);
				Vector3 vector7 = vector3 + (vector - new Vector3(0.5f)) * (vector2 - Vector3.UnitY);
				for (int k = 0; (float)k < num; k++)
				{
					Vector3 p2 = vector6 + k * Vector3.UnitY;
					if (!mesh.Groups.Any((Group g) => FezMath.AlmostEqual(g.Position, p2)))
					{
						mesh.AddFace(Vector3.One * 2f, Vector3.Zero, faceOrientation, centeredOnOrigin: true).Position = p2;
					}
					p2 = vector7 + k * Vector3.UnitY;
					if (!mesh.Groups.Any((Group g) => FezMath.AlmostEqual(g.Position, p2)))
					{
						mesh.AddFace(Vector3.One * 2f, Vector3.Zero, faceOrientation, centeredOnOrigin: true).Position = p2;
					}
				}
				mesh2.AddFace(num2 * vector2.Abs() + num * Vector3.UnitY, vector3, faceOrientation, Color.White, centeredOnOrigin: true);
			}
			foreach (Group group in mesh.Groups)
			{
				group.TextureMatrix = new Matrix(0.5f, 0f, 0f, 0f, 0f, 0.5f, 0f, 0f, (0.5 > Host.random.NextDouble()) ? 0.5f : 0f, (0.5 > Host.random.NextDouble()) ? 0.5f : 0f, 1f, 0f, 0f, 0f, 0f, 1f);
			}
			mesh2.Collapse<VertexPositionNormalColor>();
			IndexedUserPrimitives<VertexPositionNormalColor> indexedUserPrimitives = mesh2.FirstGroup.Geometry as IndexedUserPrimitives<VertexPositionNormalColor>;
			mesh.CollapseWithNormalTexture<FezVertexPositionNormalTexture>();
			IndexedUserPrimitives<FezVertexPositionNormalTexture> indexedUserPrimitives2 = mesh.FirstGroup.Geometry as IndexedUserPrimitives<FezVertexPositionNormalTexture>;
			instanceIndex = Host.HolesBodyMesh.Groups.Count;
			Host.HolesBodyMesh.AddGroup().Geometry = new IndexedUserPrimitives<VertexPositionInstance>(indexedUserPrimitives.Vertices.Select((VertexPositionNormalColor x) => new VertexPositionInstance(x.Position)
			{
				InstanceIndex = instanceIndex
			}).ToArray(), indexedUserPrimitives.Indices, PrimitiveType.TriangleList);
			Host.HolesFringeMesh.AddGroup().Geometry = new IndexedUserPrimitives<VertexPositionTextureInstance>(indexedUserPrimitives2.Vertices.Select((FezVertexPositionNormalTexture x) => new VertexPositionTextureInstance(x.Position, x.TextureCoordinate)
			{
				InstanceIndex = instanceIndex
			}).ToArray(), indexedUserPrimitives2.Indices, PrimitiveType.TriangleList);
		}
	}

	private static readonly string[] AlwaysEnabledLevels = new string[3] { "NUZU_ABANDONED_B", "STARGATE_RUINS", "WALL_INTERIOR_HOLE" };

	private Mesh HolesBodyMesh;

	private Mesh HolesFringeMesh;

	private Matrix[] InstanceData;

	private readonly List<HoleState> holes = new List<HoleState>();

	private Texture2D starsTexture;

	private Texture2D ripsTexture;

	private Matrix textureMatrix;

	private HoleState currentHole;

	private SoundEffect buzz;

	private SoundEffect[] glitches;

	private readonly Random random = new Random();

	public static BlackHolesHost Instance;

	[ServiceDependency]
	public IDotService DotService { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public ISpeechBubbleManager SpeechBubble { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderingManager { private get; set; }

	[ServiceDependency]
	public ITimeManager TimeManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	public BlackHolesHost(Game game)
		: base(game)
	{
		base.DrawOrder = 50;
		Instance = this;
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		starsTexture = CMProvider.Global.Load<Texture2D>("Other Textures/black_hole/Stars");
		buzz = CMProvider.Global.Load<SoundEffect>("Sounds/Zu/BlackHoleBuzz");
		glitches = new SoundEffect[3]
		{
			CMProvider.Global.Load<SoundEffect>("Sounds/Intro/FezLogoGlitch1"),
			CMProvider.Global.Load<SoundEffect>("Sounds/Intro/FezLogoGlitch2"),
			CMProvider.Global.Load<SoundEffect>("Sounds/Intro/FezLogoGlitch3")
		};
		ripsTexture = CMProvider.Global.Load<Texture2D>("Other Textures/black_hole/Rips");
		HolesFringeMesh = new Mesh
		{
			Effect = new InstancedBlackHoleEffect(body: false),
			SamplerState = SamplerState.PointClamp,
			DepthWrites = false,
			Texture = ripsTexture,
			SkipGroupCheck = true
		};
		HolesBodyMesh = new Mesh
		{
			Effect = new InstancedBlackHoleEffect(body: true),
			DepthWrites = false,
			Material = 
			{
				Diffuse = Vector3.Zero
			},
			SkipGroupCheck = true
		};
		BaseEffect.InstancingModeChanged += RefreshEffects;
		CameraManager.ProjectionChanged += ScaleStarBackground;
		ScaleStarBackground();
	}

	private void RefreshEffects()
	{
		HolesFringeMesh.Effect = new InstancedBlackHoleEffect(body: false);
		HolesBodyMesh.Effect = new InstancedBlackHoleEffect(body: true);
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		BaseEffect.InstancingModeChanged -= RefreshEffects;
	}

	private void ScaleStarBackground()
	{
		if (!CameraManager.ProjectionTransition && CameraManager.Viewpoint.IsOrthographic())
		{
			Viewport viewport = base.GraphicsDevice.Viewport;
			float num = (float)viewport.Width / CameraManager.Radius / 16f * base.GraphicsDevice.GetViewScale();
			float num2 = (float)viewport.Width / (float)starsTexture.Width / num;
			float num3 = (float)viewport.Height / (float)starsTexture.Height / num;
			textureMatrix = new Matrix(num2, 0f, 0f, 0f, 0f, num3, 0f, 0f, (0f - num2) / 2f, (0f - num3) / 2f, 1f, 0f, 0f, 0f, 0f, 1f);
		}
	}

	public override void Initialize()
	{
		base.Initialize();
		bool visible = (base.Enabled = false);
		base.Visible = visible;
		CameraManager.ViewChanged += CullHolesByBounds;
		LevelManager.LevelChanged += TryCreateHoles;
	}

	private void CullHolesByBounds()
	{
		if (!base.Enabled || GameState.Loading)
		{
			return;
		}
		bool inTransition = GameState.FarawaySettings.InTransition;
		bool flag = CameraManager.ProjectionTransition || !CameraManager.Viewpoint.IsOrthographic() || CameraManager.ProjectionTransitionNewlyReached;
		foreach (HoleState hole in holes)
		{
			hole.Visible = flag || (!inTransition && CameraManager.Frustum.Contains(hole.Volume.BoundingBox) != ContainmentType.Disjoint);
			if (hole.Emitter != null && !hole.Emitter.Dead)
			{
				hole.Emitter.FactorizeVolume = hole.Visible;
			}
		}
	}

	private void TryCreateHoles()
	{
		bool visible = (base.Enabled = false);
		base.Visible = visible;
		HolesBodyMesh.ClearGroups();
		HolesFringeMesh.ClearGroups();
		holes.Clear();
		CreateHoles();
		visible = (base.Enabled = holes.Count > 0);
		base.Visible = visible;
		if (base.Enabled)
		{
			ScaleStarBackground();
		}
	}

	private void CreateHoles()
	{
		bool enabled = AlwaysEnabledLevels.Contains(LevelManager.Name) || (!GameState.SaveData.ThisLevel.FirstVisit && !GameState.IsTrialMode && RandomHelper.Probability((float)Math.Min(GameState.SaveData.CubeShards + GameState.SaveData.SecretCubes, 32) / 64f));
		foreach (Volume item in LevelManager.Volumes.Values.Where((Volume x) => x.ActorSettings != null && x.ActorSettings.IsBlackHole))
		{
			HoleState holeState = new HoleState(item, this);
			holeState.Build();
			holeState.Enabled = enabled;
			if (holeState.Enabled)
			{
				holeState.Emitter = buzz.EmitAt(holeState.Center, loop: true, 0f, 0f);
				holeState.Visible = true;
			}
			holes.Add(holeState);
		}
		InstanceData = new Matrix[holes.Count];
		for (int i = 0; i < InstanceData.Length; i++)
		{
			InstanceData[i] = new Matrix(0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, 0f, 0f);
		}
		HolesBodyMesh.Collapse<VertexPositionInstance>();
		HolesFringeMesh.Collapse<VertexPositionTextureInstance>();
	}

	public void DisableAll()
	{
		foreach (HoleState hole in holes)
		{
			if (hole.Emitter != null && !hole.Emitter.Dead)
			{
				hole.Emitter.FadeOutAndDie(0.1f);
			}
			hole.Emitter = null;
			hole.Enabled = false;
		}
	}

	public void EnableAll()
	{
		foreach (HoleState hole in holes)
		{
			if (hole.Emitter == null || hole.Emitter.Dead)
			{
				hole.Emitter = buzz.EmitAt(hole.Center, loop: true);
			}
			hole.Enabled = true;
			hole.SinceEnabled = 0f;
		}
	}

	public void Randomize()
	{
		bool enabled = RandomHelper.Probability((float)Math.Min(GameState.SaveData.CubeShards + GameState.SaveData.SecretCubes, 32) / 64f);
		foreach (HoleState hole in holes)
		{
			hole.Enabled = enabled;
			if (hole.Enabled)
			{
				if (hole.Emitter == null || hole.Emitter.Dead)
				{
					hole.Emitter = buzz.EmitAt(hole.Center, loop: true);
				}
			}
			else
			{
				if (hole.Emitter != null && !hole.Emitter.Dead)
				{
					hole.Emitter.FadeOutAndDie(0.1f);
				}
				hole.Emitter = null;
			}
			hole.SinceEnabled = 0f;
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused || GameState.InMenuCube || GameState.InMap || !CameraManager.Viewpoint.IsOrthographic() || CameraManager.ProjectionTransition)
		{
			return;
		}
		Vector3 vector = CameraManager.Viewpoint.RightVector() * PlayerManager.LookingDirection.Sign();
		if (PlayerManager.Action != ActionType.SuckedIn && currentHole != null)
		{
			currentHole.Sucking = false;
			currentHole = null;
		}
		if (PlayerManager.Action == ActionType.SuckedIn && currentHole == null)
		{
			foreach (HoleState hole in holes)
			{
				if (hole.Volume.ActorSettings.IsBlackHole && hole.Volume.ActorSettings.Sucking)
				{
					PlayerManager.Velocity = Vector3.Zero;
					hole.Volume.ActorSettings.Sucking = false;
					hole.Sucking = true;
					currentHole = hole;
					break;
				}
			}
		}
		if (currentHole != null)
		{
			Vector3 vector2 = -CameraManager.Viewpoint.ForwardVector();
			Vector3 value = currentHole.Center + -0.25f * Vector3.UnitY + 0.375f * vector + (currentHole.Size + new Vector3(0.6f)) * vector2;
			PlayerManager.Position = Vector3.Lerp(PlayerManager.Position, value, 0.025f);
		}
		foreach (HoleState hole2 in holes)
		{
			if (hole2.Visible && hole2.Enabled)
			{
				if (!GameState.SaveData.OneTimeTutorials.ContainsKey("DOT_BLACKHOLE_A") && !GameState.FarawaySettings.InTransition && !PlayerManager.InDoorTransition && PlayerManager.Action.AllowsLookingDirectionChange() && SpeechBubble.Hidden && !PlayerManager.Action.DisallowsRespawn() && CameraManager.ViewTransitionReached && PlayerManager.CarriedInstance == null)
				{
					DotSpeak();
					GameState.SaveData.OneTimeTutorials.Add("DOT_BLACKHOLE_A", value: true);
				}
				hole2.SinceEnabled += (float)gameTime.ElapsedGameTime.TotalSeconds * 0.5f;
				bool flag = (hole2.Sucking ? 0.2 : (gameTime.ElapsedGameTime.TotalSeconds * 0.5)) > random.NextDouble();
				if (flag && hole2.SinceEnabled > 1f)
				{
					glitches[random.Next(glitches.Length)].EmitAt(hole2.Center, loop: false, (float)(random.NextDouble() - 0.5) * 0.1f);
					bool flag2 = 0.5 > random.NextDouble();
					bool flag3 = 0.5 > random.NextDouble();
					hole2.SetTextureTransform(new Vector2(flag2 ? 1 : 0, flag3 ? 1 : 0), new Vector2((!flag2) ? 1 : (-1), (!flag3) ? 1 : (-1)));
				}
				float num = (float)random.NextDouble() * 16f;
				float num2 = 3f / 32f * (flag ? num : 1f) * 2f;
				for (int i = 0; i < 3; i++)
				{
					hole2.Offsets[i].X = (float)(random.NextDouble() - 0.5) * num2;
					hole2.Offsets[i].Y = (float)(random.NextDouble() - 0.5) * num2;
					hole2.Offsets[i].Z = (float)(random.NextDouble() - 0.5) * num2;
				}
			}
		}
	}

	private void DotSpeak()
	{
		DotService.Say("DOT_BLACKHOLE_A", nearGomez: true, hideAfter: false).Ended = delegate
		{
			DotService.Say("DOT_BLACKHOLE_B", nearGomez: true, hideAfter: false).Ended = delegate
			{
				DotService.Say("DOT_BLACKHOLE_C", nearGomez: true, hideAfter: false).Ended = delegate
				{
					DotService.Say("DOT_BLACKHOLE_D", nearGomez: true, hideAfter: false).Ended = delegate
					{
						DotService.Say("DOT_BLACKHOLE_F", nearGomez: true, hideAfter: true);
					};
				};
			};
		};
	}

	public override void Draw(GameTime gameTime)
	{
		if (!GameState.StereoMode)
		{
			DoDraw();
		}
	}

	public void DoDraw()
	{
		if (GameState.Loading)
		{
			return;
		}
		bool flag = false;
		foreach (HoleState hole in holes)
		{
			flag |= hole.Visible && hole.Enabled;
			if (flag)
			{
				break;
			}
		}
		if (!flag)
		{
			return;
		}
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		Vector3 value = TimeManager.CurrentFogColor.ToVector3();
		value = Vector3.Lerp(new Vector3((value.X + value.Y + value.Z) / 3f), value, 0.5f);
		for (int i = 0; i < 3; i++)
		{
			graphicsDevice.SetColorWriteChannels((ColorWriteChannels)((i switch
			{
				1 => 2, 
				0 => 1, 
				_ => 4, 
			}) | 8));
			Vector3 vector = new Vector3((i == 0) ? 1 : 0, (i == 1) ? 1 : 0, (i == 2) ? 1 : 0);
			graphicsDevice.PrepareStencilWrite(StencilMask.BlackHoles);
			graphicsDevice.SetBlendingMode(BlendingMode.Multiply);
			foreach (HoleState hole2 in holes)
			{
				if (hole2.Visible && hole2.Enabled)
				{
					hole2.RandomVisibility = (((double)FezMath.Saturate(hole2.SinceEnabled) > random.NextDouble()) ? 1 : 0);
					if (i == 0)
					{
						hole2.Emitter.VolumeFactor = hole2.RandomVisibility;
					}
					hole2.SetDiffuse(new Vector3(1f - hole2.RandomVisibility));
					hole2.SetPositionForPass(i);
				}
			}
			DrawBatch(HolesBodyMesh);
			if (TimeManager.NightContribution == 0f)
			{
				graphicsDevice.GetDssCombiner().StencilPass = StencilOperation.Keep;
				foreach (HoleState hole3 in holes)
				{
					if (hole3.Visible && hole3.Enabled)
					{
						hole3.SetDiffuse(new Vector3(1f - hole3.RandomVisibility));
						hole3.SetPositionForPass(i);
					}
				}
				DrawBatch(HolesFringeMesh);
				continue;
			}
			graphicsDevice.PrepareStencilRead(CompareFunction.NotEqual, StencilMask.BlackHoles);
			graphicsDevice.SetBlendingMode(BlendingMode.Additive);
			foreach (HoleState hole4 in holes)
			{
				if (hole4.Visible && hole4.Enabled)
				{
					hole4.SetDiffuse((new Vector3(0.75f) - value).Saturate() * vector * new Vector3(hole4.RandomVisibility));
					hole4.SetPositionForPass(i);
				}
			}
			DrawBatch(HolesFringeMesh);
		}
		graphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
		graphicsDevice.SetBlendingMode(BlendingMode.Additive);
		graphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.BlackHoles);
		graphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
		TargetRenderingManager.DrawFullscreen(starsTexture, textureMatrix);
		graphicsDevice.PrepareStencilWrite(StencilMask.None);
		graphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
	}

	private void DrawBatch(Mesh mesh)
	{
		(mesh.Effect as InstancedBlackHoleEffect).SetInstanceData(InstanceData, 0, InstanceData.Length);
		mesh.Draw();
	}
}
