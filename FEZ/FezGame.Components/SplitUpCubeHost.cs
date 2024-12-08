using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine;
using FezEngine.Components;
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

internal class SplitUpCubeHost : DrawableGameComponent
{
	private class TrailsRenderer : DrawableGameComponent
	{
		private readonly SplitUpCubeHost Host;

		public TrailsRenderer(Game game, SplitUpCubeHost host)
			: base(game)
		{
			Host = host;
			base.DrawOrder = 101;
		}

		public override void Draw(GameTime gameTime)
		{
			if (Host.GameState.Loading || Host.PlayerManager.Action == ActionType.FindingTreasure)
			{
				return;
			}
			GraphicsDevice graphicsDevice = base.GraphicsDevice;
			if (Host.WirecubeVisible)
			{
				graphicsDevice.PrepareStencilWrite(StencilMask.Wirecube);
				Host.WireframeCube.DepthWrites = false;
				switch (Host.LevelManager.WaterType)
				{
				case LiquidType.Lava:
					Host.WireframeCube.Material.Diffuse = new Vector3(255f, 0f, 0f) / 255f;
					break;
				case LiquidType.Sewer:
					Host.WireframeCube.Material.Diffuse = new Vector3(215f, 232f, 148f) / 255f;
					break;
				default:
					Host.WireframeCube.Material.Diffuse = Vector3.One;
					break;
				}
				Host.SplitCollectorEffect.Offset = 0.0625f / Host.CameraManager.PixelsPerTrixel * Math.Abs((float)Math.Sin(Host.timeAcc.TotalSeconds)) * 8f;
				Host.SplitCollectorEffect.VaryingOpacity = 0.05f + Math.Abs((float)Math.Cos(Host.timeAcc.TotalSeconds * 3.0)) * 0.2f;
				Host.WireframeCube.Material.Opacity = Host.WireOpacityFactor;
				Host.WireframeCube.Draw();
			}
			foreach (SwooshingCube trackedCollect in Host.TrackedCollects)
			{
				graphicsDevice.PrepareStencilReadWrite(CompareFunction.NotEqual, StencilMask.Trails);
				trackedCollect.Trail.Draw();
				graphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
				graphicsDevice.GetDssCombiner().DepthBufferWriteEnable = false;
				graphicsDevice.PrepareStencilWrite(StencilMask.None);
				trackedCollect.Trail.Draw();
				graphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
				graphicsDevice.GetDssCombiner().DepthBufferWriteEnable = true;
			}
			graphicsDevice.PrepareStencilWrite(StencilMask.None);
		}
	}

	private class SwooshingCube
	{
		private readonly Vector3 RedTrail = new Vector3(254f, 1f, 0f) / 255f;

		private readonly Vector3 StandardTrail = new Vector3(255f, 230f, 96f) / 255f;

		private readonly Vector3 SewerTrail = new Vector3(215f, 232f, 148f) / 255f;

		private readonly Vector3 CMYTrail = new Vector3(1f, 1f, 0f);

		private const float TrailRadius = 0.5f;

		public readonly Mesh Trail;

		public readonly Mesh Cube;

		public readonly TrileInstance Instance;

		public readonly Vector3SplineInterpolation Spline;

		private readonly IndexedUserPrimitives<FezVertexPositionColor> TrailGeometry;

		private readonly Mesh DestinationMesh;

		private readonly Vector3 sideDirection;

		private readonly Vector3 color;

		private readonly Vector3 positionOffset;

		private Vector3 lastPoint;

		private float lastStep;

		private Quaternion rotation;

		private FezVertexPositionColor[] TrailVertices;

		private int[] TrailIndices;

		[ServiceDependency]
		public IGameCameraManager CameraManager { private get; set; }

		[ServiceDependency]
		public ILevelManager LevelManager { private get; set; }

		[ServiceDependency]
		public ILevelMaterializer LevelMaterializer { private get; set; }

		public SwooshingCube(TrileInstance instance, Mesh destinationMesh, Vector3 Offset, Quaternion Rotation)
		{
			CameraManager = ServiceHelper.Get<IGameCameraManager>();
			LevelManager = ServiceHelper.Get<ILevelManager>();
			LevelMaterializer = ServiceHelper.Get<ILevelMaterializer>();
			rotation = Rotation;
			positionOffset = Offset;
			color = StandardTrail;
			switch (LevelManager.WaterType)
			{
			case LiquidType.Lava:
				color = RedTrail;
				break;
			case LiquidType.Sewer:
				color = SewerTrail;
				break;
			}
			if (LevelManager.BlinkingAlpha)
			{
				color = CMYTrail;
			}
			Trail = new Mesh
			{
				Effect = new DefaultEffect.VertexColored(),
				Culling = CullMode.None,
				Blending = BlendingMode.Additive,
				AlwaysOnTop = true
			};
			Cube = new Mesh
			{
				Texture = LevelMaterializer.TrilesMesh.Texture
			};
			if (LevelManager.WaterType == LiquidType.Sewer || LevelManager.WaterType == LiquidType.Lava || LevelManager.BlinkingAlpha)
			{
				Cube.Effect = new DefaultEffect.Textured
				{
					AlphaIsEmissive = true
				};
			}
			else
			{
				Cube.Effect = new DefaultEffect.LitTextured
				{
					Specular = true,
					Emissive = 0.5f,
					AlphaIsEmissive = true
				};
			}
			ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4> geometry = instance.Trile.Geometry;
			IndexedUserPrimitives<VertexPositionNormalTextureInstance> geometry2 = new IndexedUserPrimitives<VertexPositionNormalTextureInstance>(geometry.Vertices, geometry.Indices, geometry.PrimitiveType);
			Cube.AddGroup().Geometry = geometry2;
			Trail.AddGroup().Geometry = (TrailGeometry = new IndexedUserPrimitives<FezVertexPositionColor>(TrailVertices = new FezVertexPositionColor[0], TrailIndices = new int[0], PrimitiveType.TriangleList));
			Instance = instance;
			Vector3 center = instance.Center;
			lastPoint = center;
			DestinationMesh = destinationMesh;
			sideDirection = ((!RandomHelper.Probability(0.5)) ? 1 : (-1)) * CameraManager.Viewpoint.RightVector();
			Spline = new Vector3SplineInterpolation(TimeSpan.FromSeconds(3.0), default(Vector3), default(Vector3), default(Vector3), default(Vector3), default(Vector3), default(Vector3), default(Vector3), default(Vector3), default(Vector3), default(Vector3));
			Spline.Start();
			AddSegment();
		}

		public void Dispose()
		{
			Cube.Dispose();
			Trail.Dispose();
		}

		private void AddSegment()
		{
			bool flag = TrailVertices.Length == 0;
			Array.Resize(ref TrailVertices, TrailVertices.Length + (flag ? 12 : 6));
			Array.Resize(ref TrailIndices, TrailIndices.Length + 36);
			TrailGeometry.Vertices = TrailVertices;
			TrailGeometry.Indices = TrailIndices;
			int num = TrailVertices.Length - 12;
			for (int i = 0; i < 6; i++)
			{
				int num2 = i * 6 + TrailIndices.Length - 36;
				TrailIndices[num2] = i + num;
				TrailIndices[num2 + 1] = (i + 6) % 12 + num;
				TrailIndices[num2 + 2] = (i + 1) % 12 + num;
				TrailIndices[num2 + 3] = (i + 1) % 12 + num;
				TrailIndices[num2 + 4] = (i + 6) % 12 + num;
				TrailIndices[num2 + 5] = (i + 7) % 12 + num;
			}
			if (flag)
			{
				for (int j = 0; j < 6; j++)
				{
					TrailVertices[j].Position = lastPoint;
					TrailVertices[j].Color = Color.Black;
				}
			}
		}

		private void AlignLastSegment()
		{
			Vector3 current = Spline.Current;
			Vector3 vector = Vector3.Normalize(current - lastPoint);
			float step = Math.Abs(vector.Dot(Vector3.Up));
			Vector3 vector2 = FezMath.Slerp(Vector3.Up, Vector3.Forward, step);
			Vector3 vector3 = Vector3.Normalize(Vector3.Cross(vector2, vector));
			vector2 = Vector3.Normalize(Vector3.Cross(vector, vector3));
			Quaternion quaternion = Quaternion.Inverse(Quaternion.CreateFromRotationMatrix(new Matrix(vector3.X, vector2.X, vector.X, 0f, vector3.Y, vector2.Y, vector.Y, 0f, vector3.Z, vector2.Z, vector.Z, 0f, 0f, 0f, 0f, 1f)));
			int num = TrailVertices.Length - 6;
			for (int i = 0; i < 6; i++)
			{
				float num2 = (float)i / 6f * ((float)Math.PI * 2f);
				TrailVertices[num + i].Position = Vector3.Transform(new Vector3((float)Math.Sin(num2) * 0.5f / 2f, (float)Math.Cos(num2) * 0.5f / 2f, 0f), quaternion) + current;
			}
		}

		private void ColorSegments()
		{
			int num = TrailVertices.Length / 6;
			for (int i = 0; i < num; i++)
			{
				for (int j = 0; j < 6; j++)
				{
					float value = Easing.EaseIn(Math.Max((float)(i - (num - 9)) / 10f, 0f), EasingType.Sine) * (float)Math.Pow(1f - Spline.TotalStep, 0.5);
					TrailVertices[i * 6 + j].Color = new Color(new Vector3(value) * color);
				}
			}
		}

		public void Update(GameTime gameTime)
		{
			for (int i = 0; i < Spline.Points.Length; i++)
			{
				float num = Easing.EaseOut((float)i / (float)(Spline.Points.Length - 1), EasingType.Sine);
				Spline.Points[i] = Vector3.Lerp(Instance.Center, DestinationMesh.Position, num);
				Vector3 zero = Vector3.Zero;
				zero += sideDirection * 3.5f * (0.7f - (float)Math.Sin(num * ((float)Math.PI * 2f) + (float)Math.PI / 4f));
				zero += Vector3.Up * 2f * (0.7f - (float)Math.Cos(num * ((float)Math.PI * 2f) + (float)Math.PI / 4f));
				if (i != 0 && i != Spline.Points.Length - 1)
				{
					Spline.Points[i] += zero;
				}
			}
			Spline.Update(gameTime);
			if ((double)(Spline.TotalStep - lastStep) > 0.025)
			{
				lastPoint = Spline.Current;
				lastStep = Spline.TotalStep;
				AddSegment();
			}
			AlignLastSegment();
			ColorSegments();
			rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)gameTime.ElapsedGameTime.TotalSeconds) * rotation;
			Vector3 vector = Vector3.Transform(positionOffset, rotation);
			Cube.Position = Spline.Current + vector * Spline.TotalStep;
			Cube.Rotation = Quaternion.Slerp(Quaternion.Identity, rotation, Spline.TotalStep);
		}
	}

	private SoundEffect[] CollectSounds;

	private SplitCollectorEffect SplitCollectorEffect;

	private float WireOpacityFactor;

	private bool WirecubeVisible;

	private bool SolidCubesVisible;

	private Mesh WireframeCube;

	private Mesh SolidCubes;

	private readonly List<SwooshingCube> TrackedCollects = new List<SwooshingCube>();

	private TrailsRenderer trailsRenderer;

	private float SinceNoTrails;

	private float SinceCollect;

	private bool AssembleScheduled;

	private readonly List<TrileInstance> TrackedBits = new List<TrileInstance>();

	private Mesh ChimeOutline;

	private float UntilNextShine;

	private TrileInstance ShineOn;

	private SoundEffect sBitChime;

	public const int ShineRate = 7;

	private readonly Vector3[] CubeOffsets = new Vector3[8]
	{
		new Vector3(0.25f, -0.25f, 0.25f),
		new Vector3(-0.25f, -0.25f, 0.25f),
		new Vector3(-0.25f, -0.25f, -0.25f),
		new Vector3(0.25f, -0.25f, -0.25f),
		new Vector3(0.25f, 0.25f, 0.25f),
		new Vector3(-0.25f, 0.25f, 0.25f),
		new Vector3(-0.25f, 0.25f, -0.25f),
		new Vector3(0.25f, 0.25f, -0.25f)
	};

	private TimeSpan timeAcc;

	[ServiceDependency]
	public ISoundManager SoundManager { get; set; }

	[ServiceDependency]
	public ITimeManager TimeManager { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public ILightingPostProcess LightingPostProcess { get; set; }

	[ServiceDependency]
	public IGomezService GomezService { get; set; }

	[ServiceDependency]
	public ISpeechBubbleManager SpeechBubble { get; set; }

	public SplitUpCubeHost(Game game)
		: base(game)
	{
		base.DrawOrder = 75;
	}

	public override void Initialize()
	{
		base.Initialize();
		ServiceHelper.AddComponent(trailsRenderer = new TrailsRenderer(base.Game, this));
		LevelManager.LevelChanged += TryInitialize;
		TrailsRenderer obj = trailsRenderer;
		bool flag2 = (base.Visible = false);
		bool visible = (base.Enabled = flag2);
		obj.Visible = visible;
		SoundManager.SongChanged += RefreshSounds;
		LightingPostProcess.DrawGeometryLights += DrawLights;
	}

	protected override void LoadContent()
	{
		SolidCubes = new Mesh
		{
			Blending = BlendingMode.Alphablending
		};
		ChimeOutline = new Mesh
		{
			DepthWrites = false
		};
		ChimeOutline.AddWireframePolygon(Color.Yellow, new Vector3(0f, 0.7071068f, 0f), new Vector3(0.7071068f, 0f, 0f), new Vector3(0f, -0.7071068f, 0f), new Vector3(-0.7071068f, 0f, 0f), new Vector3(0f, 0.7071068f, 0f));
		DrawActionScheduler.Schedule(delegate
		{
			ChimeOutline.Effect = new DefaultEffect.VertexColored
			{
				Fullbright = true,
				AlphaIsEmissive = false
			};
			SolidCubes.Effect = new DefaultEffect.LitTextured
			{
				Specular = true,
				Emissive = 0.5f,
				AlphaIsEmissive = true
			};
		});
		ChimeOutline.AddWireframePolygon(new Color(Color.Yellow.ToVector3() * (1f / 3f)), new Vector3(0f, 0.7071068f, 0f), new Vector3(0.7071068f, 0f, 0f), new Vector3(0f, -0.7071068f, 0f), new Vector3(-0.7071068f, 0f, 0f), new Vector3(0f, 0.7071068f, 0f));
		ChimeOutline.AddWireframePolygon(new Color(Color.Yellow.ToVector3() * (1f / 9f)), new Vector3(0f, 0.7071068f, 0f), new Vector3(0.7071068f, 0f, 0f), new Vector3(0f, -0.7071068f, 0f), new Vector3(-0.7071068f, 0f, 0f), new Vector3(0f, 0.7071068f, 0f));
		ChimeOutline.AddWireframePolygon(new Color(Color.Yellow.ToVector3() * (1f / 27f)), new Vector3(0f, 0.7071068f, 0f), new Vector3(0.7071068f, 0f, 0f), new Vector3(0f, -0.7071068f, 0f), new Vector3(-0.7071068f, 0f, 0f), new Vector3(0f, 0.7071068f, 0f));
		sBitChime = CMProvider.Global.Load<SoundEffect>("Sounds/Collects/BitChime");
	}

	private void RefreshSounds()
	{
		CollectSounds = new SoundEffect[8];
		TrackedSong currentlyPlayingSong = SoundManager.CurrentlyPlayingSong;
		ShardNotes[] array = ((currentlyPlayingSong == null) ? new ShardNotes[8]
		{
			ShardNotes.C2,
			ShardNotes.D2,
			ShardNotes.E2,
			ShardNotes.F2,
			ShardNotes.G2,
			ShardNotes.A2,
			ShardNotes.B2,
			ShardNotes.C3
		} : currentlyPlayingSong.Notes);
		for (int i = 0; i < array.Length; i++)
		{
			CollectSounds[i] = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Collects/SplitUpCube/" + array[i]);
		}
	}

	private void TryInitialize()
	{
		Vector3[] cubeOffsets;
		if (WireframeCube == null)
		{
			WireframeCube = new Mesh
			{
				Material = 
				{
					Diffuse = Vector3.One,
					Opacity = 1f
				},
				Blending = BlendingMode.Alphablending
			};
			DrawActionScheduler.Schedule(delegate
			{
				WireframeCube.Effect = (SplitCollectorEffect = new SplitCollectorEffect());
			});
			for (int i = 0; i < 7; i++)
			{
				WireframeCube.AddWireframeBox(Vector3.One, Vector3.Zero, new Color(i switch
				{
					1 => 0f, 
					0 => 1f, 
					_ => 0.5f, 
				}, i switch
				{
					3 => 0f, 
					2 => 1f, 
					_ => 0.5f, 
				}, i switch
				{
					5 => 0f, 
					4 => 1f, 
					_ => 0.5f, 
				}, (i == 6) ? 1f : 0f), centeredOnOrigin: true);
				cubeOffsets = CubeOffsets;
				foreach (Vector3 origin in cubeOffsets)
				{
					WireframeCube.AddWireframeBox(Vector3.One / 2f, origin, new Color(i switch
					{
						1 => 0f, 
						0 => 1f, 
						_ => 0.5f, 
					}, i switch
					{
						3 => 0f, 
						2 => 1f, 
						_ => 0.5f, 
					}, i switch
					{
						5 => 0f, 
						4 => 1f, 
						_ => 0.5f, 
					}, (i == 6) ? 0.625f : 0.375f), centeredOnOrigin: true);
				}
			}
			WireframeCube.CollapseToBuffer<FezVertexPositionColor>();
		}
		SolidCubesVisible = true;
		if (TrackedCollects.Count > 0)
		{
			GameState.SaveData.CollectedParts += TrackedCollects.Count;
			Waiters.Wait(0.5, delegate
			{
				Waiters.Wait(() => PlayerManager.CanControl && PlayerManager.Grounded, delegate
				{
					GomezService.OnCollectedSplitUpCube();
					GameState.OnHudElementChanged();
					GameState.Save();
					TryAssembleCube();
				});
			});
		}
		foreach (SwooshingCube trackedCollect in TrackedCollects)
		{
			trackedCollect.Dispose();
		}
		TrackedCollects.Clear();
		bool flag2;
		bool visible;
		if (LevelManager.TrileSet == null)
		{
			TrailsRenderer obj = trailsRenderer;
			flag2 = (base.Visible = false);
			visible = (base.Enabled = flag2);
			obj.Visible = visible;
			return;
		}
		Trile goldenCubeTrile = LevelManager.ActorTriles(ActorType.GoldenCube).FirstOrDefault();
		IEnumerable<TrileInstance> source = LevelManager.Triles.Values.Union(LevelManager.Triles.SelectMany(delegate(KeyValuePair<TrileEmplacement, TrileInstance> x)
		{
			IEnumerable<TrileInstance> overlappedTriles = x.Value.OverlappedTriles;
			return overlappedTriles ?? Enumerable.Empty<TrileInstance>();
		}));
		TrailsRenderer obj2 = trailsRenderer;
		flag2 = (base.Visible = goldenCubeTrile != null && (source.Count((TrileInstance x) => x.TrileId == goldenCubeTrile.Id) != 0 || AssembleScheduled || GameState.SaveData.CollectedParts == 8));
		visible = (base.Enabled = flag2);
		obj2.Visible = visible;
		if (!base.Enabled)
		{
			return;
		}
		RefreshSounds();
		TrackedBits.Clear();
		TrackedBits.AddRange(source.Where((TrileInstance x) => x.TrileId == goldenCubeTrile.Id));
		SolidCubes.ClearGroups();
		ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4> geometry = goldenCubeTrile.Geometry;
		SolidCubes.Position = Vector3.Zero;
		SolidCubes.Rotation = Quaternion.Identity;
		cubeOffsets = CubeOffsets;
		foreach (Vector3 position in cubeOffsets)
		{
			Group group = SolidCubes.AddGroup();
			group.Geometry = new IndexedUserPrimitives<VertexPositionNormalTextureInstance>(geometry.Vertices.ToArray(), geometry.Indices, geometry.PrimitiveType);
			group.Position = position;
			group.BakeTransform<VertexPositionNormalTextureInstance>();
			group.Enabled = false;
		}
		DrawActionScheduler.Schedule(delegate
		{
			SolidCubes.Texture = LevelMaterializer.TrilesMesh.Texture;
		});
		Mesh solidCubes = SolidCubes;
		Quaternion rotation = (WireframeCube.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Right, (float)Math.Asin(Math.Sqrt(2.0) / Math.Sqrt(3.0))) * Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 4f));
		solidCubes.Rotation = rotation;
		WireOpacityFactor = 1f;
		SinceNoTrails = 3f;
		ShineOn = null;
		UntilNextShine = 7f;
		if (LevelManager.WaterType == LiquidType.Sewer || LevelManager.WaterType == LiquidType.Lava || LevelManager.BlinkingAlpha)
		{
			if (SolidCubes.Effect is DefaultEffect.LitTextured)
			{
				DrawActionScheduler.Schedule(delegate
				{
					SolidCubes.Effect = new DefaultEffect.Textured
					{
						AlphaIsEmissive = true,
						IgnoreCache = true
					};
				});
			}
		}
		else if (SolidCubes.Effect is DefaultEffect.Textured)
		{
			DrawActionScheduler.Schedule(delegate
			{
				SolidCubes.Effect = new DefaultEffect.LitTextured
				{
					Specular = true,
					Emissive = 0.5f,
					AlphaIsEmissive = true,
					IgnoreCache = true
				};
			});
		}
		TryAssembleCube();
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.TimePaused || !CameraManager.Viewpoint.IsOrthographic() || AssembleScheduled)
		{
			return;
		}
		ShineOnYouCrazyDiamonds((float)gameTime.ElapsedGameTime.TotalSeconds);
		if (GameState.SaveData.CollectedParts + TrackedCollects.Count != 8 && PlayerManager.Action != ActionType.GateWarp && PlayerManager.Action != ActionType.LesserWarp && !PlayerManager.Action.IsSwimming())
		{
			TrileInstance collect = PlayerManager.AxisCollision[VerticalDirection.Up].Surface ?? PlayerManager.AxisCollision[VerticalDirection.Down].Surface;
			if (collect != null && collect.Trile.ActorSettings.Type == ActorType.GoldenCube && !TrackedCollects.Any((SwooshingCube x) => x.Instance == collect) && !LevelManager.Triles.Values.Any((TrileInstance x) => x.Overlaps && x.OverlappedTriles.Contains(collect) && x.Position == collect.Position))
			{
				int num = GameState.SaveData.CollectedParts + TrackedCollects.Count;
				CollectSounds[num].Emit();
				TrackedCollects.Add(new SwooshingCube(collect, SolidCubes, CubeOffsets[num], SolidCubes.Rotation));
				GameState.SaveData.ThisLevel.DestroyedTriles.Add(collect.OriginalEmplacement);
				GameState.SaveData.ThisLevel.FilledConditions.SplitUpCount++;
				LevelManager.ClearTrile(collect);
				TrackedBits.Remove(collect);
				if (SinceNoTrails == 3f)
				{
					SinceCollect = 0f;
				}
			}
		}
		for (int num2 = TrackedCollects.Count - 1; num2 >= 0; num2--)
		{
			if (TrackedCollects[num2].Spline.Reached)
			{
				GameState.SaveData.CollectedParts++;
				GomezService.OnCollectedSplitUpCube();
				TrackedCollects.RemoveAt(num2);
				GameState.OnHudElementChanged();
				GameState.Save();
				TryAssembleCube();
			}
			SinceNoTrails = 0f;
		}
		SinceNoTrails = Math.Min(3f, SinceNoTrails + (float)gameTime.ElapsedGameTime.TotalSeconds);
		SinceCollect = Math.Min(1f, SinceCollect + (float)gameTime.ElapsedGameTime.TotalSeconds);
		for (int i = 0; i < 8; i++)
		{
			SolidCubes.Groups[i].Enabled = GameState.SaveData.CollectedParts > i;
		}
		WirecubeVisible = SinceNoTrails < 3f;
		WireOpacityFactor = (1f - FezMath.Saturate((SinceNoTrails - 1f) / 1f)) * SinceCollect;
		if (GameState.SaveData.CollectedParts + TrackedCollects.Count == 0)
		{
			WireOpacityFactor = 0f;
		}
		SolidCubes.Material.Opacity = WireOpacityFactor;
		WirecubeVisible = WireOpacityFactor != 0f;
		SolidCubesVisible = WirecubeVisible;
	}

	private void ShineOnYouCrazyDiamonds(float elapsedTime)
	{
		UntilNextShine -= elapsedTime;
		if (UntilNextShine <= 0f && TrackedBits.Count > 0 && PlayerManager.CanControl && CameraManager.ViewTransitionReached)
		{
			UntilNextShine = 7f;
			ChimeOutline.Scale = new Vector3(0.1f);
			ChimeOutline.Groups[0].Scale = Vector3.One;
			ChimeOutline.Groups[1].Scale = Vector3.One;
			ChimeOutline.Groups[2].Scale = Vector3.One;
			ChimeOutline.Groups[3].Scale = Vector3.One;
			ShineOn = RandomHelper.InList(TrackedBits);
			sBitChime.EmitAt(ShineOn.Center);
		}
		if (ShineOn != null)
		{
			ChimeOutline.Position = ShineOn.Center;
			ChimeOutline.Rotation = CameraManager.Rotation;
			ChimeOutline.Scale = new Vector3(Easing.EaseInOut(FezMath.Saturate(7f - UntilNextShine), EasingType.Quadratic) * 10f + Easing.EaseIn(7f - UntilNextShine, EasingType.Quadratic) * 7f) * 0.75f;
			ChimeOutline.Groups[0].Scale /= 1.002f;
			ChimeOutline.Groups[1].Scale /= 1.006f;
			ChimeOutline.Groups[2].Scale /= 1.012f;
			ChimeOutline.Groups[3].Scale /= 1.018f;
			ChimeOutline.Material.Diffuse = new Vector3(Easing.EaseIn(FezMath.Saturate(1f - ChimeOutline.Scale.X / 40f), EasingType.Quadratic) * (1f - TimeManager.NightContribution * 0.65f) * (1f - TimeManager.DawnContribution * 0.7f) * (1f - TimeManager.DuskContribution * 0.7f));
			ChimeOutline.Blending = BlendingMode.Additive;
			if (ChimeOutline.Scale.X > 40f)
			{
				ShineOn = null;
			}
		}
	}

	private void TryAssembleCube()
	{
		if (AssembleScheduled || GameState.SaveData.CollectedParts != 8)
		{
			return;
		}
		AssembleScheduled = true;
		Waiters.Wait(() => !GameState.Loading && PlayerManager.Action.AllowsLookingDirectionChange() && SpeechBubble.Hidden && !GameState.ForceTimePaused && PlayerManager.CanControl && !PlayerManager.Action.DisallowsRespawn() && CameraManager.ViewTransitionReached && !PlayerManager.InDoorTransition && PlayerManager.CarriedInstance == null, delegate
		{
			Waiters.Wait(0.0, delegate
			{
				Vector3 vector = CameraManager.Viewpoint.DepthMask();
				Vector3 vector2 = CameraManager.Viewpoint.ForwardVector();
				TrileInstance trileInstance = new TrileInstance((PlayerManager.Position + Vector3.UnitY * ((float)Math.Sin(timeAcc.TotalSeconds * 3.1415927410125732) * 0.1f + 2f) - FezMath.HalfVector) * (Vector3.One - vector) - vector2 * (LevelManager.Size / 2f - vector * 2f) + vector * LevelManager.Size / 2f, LevelManager.TrileSet.Triles.Values.Last((Trile x) => x.ActorSettings.Type == ActorType.CubeShard).Id);
				LevelManager.RestoreTrile(trileInstance);
				LevelMaterializer.CullInstanceIn(trileInstance);
				PlayerManager.ForcedTreasure = trileInstance;
				PlayerManager.Action = ActionType.FindingTreasure;
				AssembleScheduled = false;
			});
		});
	}

	private void ClearDepth(Mesh mesh)
	{
		bool depthWrites = mesh.DepthWrites;
		ColorWriteChannels colorWriteChannels = base.GraphicsDevice.GetBlendCombiner().ColorWriteChannels;
		mesh.DepthWrites = true;
		base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
		mesh.AlwaysOnTop = true;
		mesh.Position += CameraManager.InverseView.Forward * 2f;
		mesh.Draw();
		mesh.AlwaysOnTop = false;
		mesh.Position -= CameraManager.InverseView.Forward * 2f;
		base.GraphicsDevice.SetColorWriteChannels(colorWriteChannels);
		mesh.DepthWrites = depthWrites;
	}

	private void DrawLights(GameTime gameTime)
	{
		if (GameState.Loading || !base.Visible || PlayerManager.Action == ActionType.FindingTreasure)
		{
			return;
		}
		if (ShineOn != null)
		{
			ChimeOutline.Draw();
		}
		if (SolidCubes.Material.Opacity < 0.25f)
		{
			return;
		}
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		foreach (SwooshingCube trackedCollect in TrackedCollects)
		{
			ClearDepth(trackedCollect.Cube);
		}
		if (SolidCubesVisible)
		{
			ClearDepth(SolidCubes);
			graphicsDevice.PrepareStencilWrite(StencilMask.Level);
			(SolidCubes.Effect as DefaultEffect).Pass = LightingEffectPass.Pre;
			SolidCubes.Draw();
			(SolidCubes.Effect as DefaultEffect).Pass = LightingEffectPass.Main;
		}
		graphicsDevice.PrepareStencilWrite(StencilMask.Level);
		foreach (SwooshingCube trackedCollect2 in TrackedCollects)
		{
			(trackedCollect2.Cube.Effect as DefaultEffect).Pass = LightingEffectPass.Pre;
			trackedCollect2.Cube.Draw();
			(trackedCollect2.Cube.Effect as DefaultEffect).Pass = LightingEffectPass.Main;
		}
		graphicsDevice.PrepareStencilWrite(StencilMask.None);
	}

	public override void Draw(GameTime gameTime)
	{
		if (GameState.Loading || PlayerManager.Action == ActionType.FindingTreasure)
		{
			return;
		}
		if (SolidCubesVisible || WirecubeVisible)
		{
			Vector3 position = GomezHost.Instance.PlayerMesh.Position;
			Mesh solidCubes = SolidCubes;
			Vector3 position2 = (WireframeCube.Position = position + Vector3.UnitY * ((float)Math.Sin(timeAcc.TotalSeconds * 3.1415927410125732) * 0.1f + 2f));
			solidCubes.Position = position2;
			Mesh solidCubes2 = SolidCubes;
			Quaternion rotation = (WireframeCube.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)gameTime.ElapsedGameTime.TotalSeconds) * WireframeCube.Rotation);
			solidCubes2.Rotation = rotation;
			SolidCubes.Position += PlayerManager.SplitUpCubeCollectorOffset;
			WireframeCube.Position += PlayerManager.SplitUpCubeCollectorOffset;
			timeAcc += gameTime.ElapsedGameTime;
		}
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		if (WirecubeVisible)
		{
			ClearDepth(WireframeCube);
		}
		foreach (SwooshingCube trackedCollect in TrackedCollects)
		{
			trackedCollect.Update(gameTime);
			ClearDepth(trackedCollect.Cube);
		}
		if (SolidCubesVisible)
		{
			ClearDepth(SolidCubes);
			graphicsDevice.PrepareStencilWrite(StencilMask.Level);
			SolidCubes.Draw();
		}
		graphicsDevice.PrepareStencilWrite(StencilMask.Level);
		foreach (SwooshingCube trackedCollect2 in TrackedCollects)
		{
			trackedCollect2.Cube.Draw();
		}
		graphicsDevice.PrepareStencilWrite(StencilMask.None);
		if (ShineOn != null)
		{
			ChimeOutline.Draw();
		}
		if (SolidCubes.Effect.IgnoreCache)
		{
			SolidCubes.Effect.IgnoreCache = false;
		}
	}
}
