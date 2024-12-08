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
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components.Actions;

internal class OpenTreasure : PlayerAction
{
	private struct TreasureActorSettings
	{
		public ArtObjectInstance AoInstance;

		public ArtObject ArtObject;
	}

	private static readonly TimeSpan OpeningDuration = TimeSpan.FromSeconds(4.0);

	private const float StarPulsateSpeed = 5f;

	private const float StarRotateSpeed = 1f;

	private const float ItemRotateSpeed = 2f;

	private readonly List<Mesh> FloatingMaps = new List<Mesh>();

	private readonly List<ArtObjectInstance> LevelChests = new List<ArtObjectInstance>();

	private float SinceCreated;

	private TimeSpan sinceActive;

	private ArtObjectInstance chestAO;

	private Trile treasureTrile;

	private TrileInstance treasureInstance;

	private ArtObject treasureAo;

	private ArtObjectInstance treasureAoInstance;

	private Vector3 treasureOrigin;

	private Vector3 aoOrigin;

	private Quaternion aoInitialRotation;

	private bool restored;

	private bool reculled;

	private TimeSpan sinceCollect;

	private float lastZoom;

	private bool treasureIsAo;

	private bool treasureIsMap;

	private bool treasureIsMail;

	private ActorType treasureActorType;

	private bool WasConstrained;

	private Vector2? OldPan;

	private Vector3 OldCenter;

	private float OldPixPerTrix;

	private TrileInstance oldGround;

	private float oldGroundHeight;

	private float oldDepth;

	private SoundEffect treasureGetSound;

	private SoundEffect assembleSound;

	private readonly Mesh lightBox;

	private readonly Mesh fadedStar;

	private readonly Mesh solidStar;

	private readonly Mesh flare;

	private readonly Mesh map;

	private readonly Mesh mail;

	private bool hasHooked;

	private float lastSin;

	private Group[] mystery2Groups = new Group[3];

	private Texture2D mystery2Xbox;

	private Texture2D mystery2Sony;

	[ServiceDependency]
	public IArtObjectService ArtObjectService { private get; set; }

	[ServiceDependency]
	public ILightingPostProcess LightingPostProcess { private get; set; }

	[ServiceDependency]
	public IDotService DotService { private get; set; }

	public OpenTreasure(Game game)
		: base(game)
	{
		base.DrawOrder = 50;
		lightBox = new Mesh
		{
			Blending = BlendingMode.Additive,
			SamplerState = SamplerState.LinearClamp,
			DepthWrites = false,
			Scale = new Vector3(1.6f, 1.5f, 1.2f),
			TextureMatrix = new Matrix(1f, 0f, 0f, 0f, 0f, -1f, 0f, 0f, 0f, 1f, 1f, 0f, 0f, 0f, 0f, 0f)
		};
		lightBox.AddFace(Vector3.One, -Vector3.UnitZ / 2f + Vector3.UnitY / 2f, FaceOrientation.Back, centeredOnOrigin: true);
		lightBox.AddFace(Vector3.One, Vector3.UnitZ / 2f + Vector3.UnitY / 2f, FaceOrientation.Front, centeredOnOrigin: true);
		lightBox.AddFace(Vector3.One, Vector3.Left / 2f + Vector3.UnitY / 2f, FaceOrientation.Left, centeredOnOrigin: true);
		lightBox.AddFace(Vector3.One, Vector3.Right / 2f + Vector3.UnitY / 2f, FaceOrientation.Right, centeredOnOrigin: true);
		flare = new Mesh
		{
			Blending = BlendingMode.Additive,
			SamplerState = SamplerState.LinearClamp,
			DepthWrites = false
		};
		flare.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true, doublesided: true);
		fadedStar = new Mesh
		{
			Blending = BlendingMode.Alphablending,
			DepthWrites = false
		};
		solidStar = new Mesh
		{
			Blending = BlendingMode.Alphablending,
			DepthWrites = false
		};
		Color color = new Color(1f, 1f, 0.3f, 0f);
		for (int i = 0; i < 8; i++)
		{
			float num = (float)i * ((float)Math.PI * 2f) / 8f;
			float num2 = ((float)i + 0.5f) * ((float)Math.PI * 2f) / 8f;
			fadedStar.AddColoredTriangle(Vector3.Zero, new Vector3((float)Math.Sin(num), (float)Math.Cos(num), 0f), new Vector3((float)Math.Sin(num2), (float)Math.Cos(num2), 0f), new Color(1f, 1f, 1f, 0.7f), color, color);
			solidStar.AddColoredTriangle(Vector3.Zero, new Vector3((float)Math.Sin(num), (float)Math.Cos(num), 0f), new Vector3((float)Math.Sin(num2), (float)Math.Cos(num2), 0f), new Color(1f, 1f, 1f, 0.7f), new Color(1f, 1f, 1f, 0.7f), new Color(1f, 1f, 1f, 0.7f));
		}
		map = new Mesh
		{
			Blending = BlendingMode.Alphablending,
			SamplerState = SamplerState.PointClamp
		};
		BuildMap(map);
		mail = new Mesh
		{
			Blending = BlendingMode.Alphablending,
			SamplerState = SamplerState.PointClamp,
			Scale = new Vector3(1.5f)
		};
		Group group = mail.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Back, centeredOnOrigin: true);
		group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, -(float)Math.PI / 2f);
		group = mail.CloneGroup(group);
		group.CullMode = CullMode.CullClockwiseFace;
		group.TextureMatrix = new Matrix(-1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 1f, 0f, 1f, 0f, 0f, 0f, 0f, 0f);
		DrawActionScheduler.Schedule(delegate
		{
			map.Effect = new DefaultEffect.LitTextured();
			mail.Effect = new DefaultEffect.LitTextured
			{
				Emissive = 0.5f
			};
		});
	}

	private static void BuildMap(Mesh mesh)
	{
		Quaternion quaternion = Quaternion.CreateFromAxisAngle(Vector3.Up, -(float)Math.PI / 2f);
		Vector3 zero = Vector3.Zero;
		Group group = mesh.AddGroup();
		group.Geometry = new IndexedUserPrimitives<FezVertexPositionNormalTexture>(new FezVertexPositionNormalTexture[4]
		{
			new FezVertexPositionNormalTexture(new Vector3(-1f, 0.5f, 0f), new Vector3(0f, 0f, -1f), new Vector2(1f, 0f)),
			new FezVertexPositionNormalTexture(new Vector3(-1f, -0.5f, 0f), new Vector3(0f, 0f, -1f), new Vector2(1f, 1f)),
			new FezVertexPositionNormalTexture(new Vector3(0f, 0.5f, 0f), new Vector3(0f, 0f, -1f), new Vector2(0.625f, 0f)),
			new FezVertexPositionNormalTexture(new Vector3(0f, -0.5f, 0f), new Vector3(0f, 0f, -1f), new Vector2(0.625f, 1f))
		}, new int[6] { 0, 1, 2, 2, 1, 3 }, PrimitiveType.TriangleList);
		group.Scale = new Vector3(0.375f, 1f, 1f) * 1.5f;
		group.Position = zero + MenuCubeFace.Maps.GetRight() * 0.125f * 1.5f;
		group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 8f) * quaternion;
		group = mesh.CloneGroup(group);
		group.CullMode = CullMode.CullClockwiseFace;
		group.InvertNormals<FezVertexPositionNormalTexture>();
		group.TextureMatrix = new Matrix(-1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 1f, 0f, 1f, 0f, 0f, 0f, 0f, 0f);
		group = mesh.AddGroup();
		group.Geometry = new IndexedUserPrimitives<FezVertexPositionNormalTexture>(new FezVertexPositionNormalTexture[4]
		{
			new FezVertexPositionNormalTexture(new Vector3(-0.5f, 0.5f, 0f), new Vector3(0f, 0f, -1f), new Vector2(0.625f, 0f)),
			new FezVertexPositionNormalTexture(new Vector3(-0.5f, -0.5f, 0f), new Vector3(0f, 0f, -1f), new Vector2(0.625f, 1f)),
			new FezVertexPositionNormalTexture(new Vector3(0.5f, 0.5f, 0f), new Vector3(0f, 0f, -1f), new Vector2(0.375f, 0f)),
			new FezVertexPositionNormalTexture(new Vector3(0.5f, -0.5f, 0f), new Vector3(0f, 0f, -1f), new Vector2(0.375f, 1f))
		}, new int[6] { 0, 1, 2, 2, 1, 3 }, PrimitiveType.TriangleList);
		group.Scale = new Vector3(0.25f, 1f, 1f) * 1.5f;
		group.Position = zero;
		group.Rotation = quaternion;
		group = mesh.CloneGroup(group);
		group.CullMode = CullMode.CullClockwiseFace;
		group.InvertNormals<FezVertexPositionNormalTexture>();
		group.TextureMatrix = new Matrix(-1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 1f, 0f, 1f, 0f, 0f, 0f, 0f, 0f);
		group = mesh.AddGroup();
		group.Geometry = new IndexedUserPrimitives<FezVertexPositionNormalTexture>(new FezVertexPositionNormalTexture[4]
		{
			new FezVertexPositionNormalTexture(new Vector3(0f, 0.5f, 0f), new Vector3(0f, 0f, -1f), new Vector2(0.375f, 0f)),
			new FezVertexPositionNormalTexture(new Vector3(0f, -0.5f, 0f), new Vector3(0f, 0f, -1f), new Vector2(0.375f, 1f)),
			new FezVertexPositionNormalTexture(new Vector3(1f, 0.5f, 0f), new Vector3(0f, 0f, -1f), new Vector2(0f, 0f)),
			new FezVertexPositionNormalTexture(new Vector3(1f, -0.5f, 0f), new Vector3(0f, 0f, -1f), new Vector2(0f, 1f))
		}, new int[6] { 0, 1, 2, 2, 1, 3 }, PrimitiveType.TriangleList);
		group.Scale = new Vector3(0.375f, 1f, 1f) * 1.5f;
		group.Position = zero - MenuCubeFace.Maps.GetRight() * 0.125f * 1.5f;
		group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 8f) * quaternion;
		group = mesh.CloneGroup(group);
		group.CullMode = CullMode.CullClockwiseFace;
		group.InvertNormals<FezVertexPositionNormalTexture>();
		group.TextureMatrix = new Matrix(-1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 1f, 0f, 1f, 0f, 0f, 0f, 0f, 0f);
	}

	public override void Initialize()
	{
		base.SoundManager.SongChanged += RefreshSounds;
		base.LevelManager.LevelChanged += delegate
		{
			foreach (int inactiveArtObject in base.GameState.SaveData.ThisLevel.InactiveArtObjects)
			{
				if (inactiveArtObject >= 0 && base.LevelManager.ArtObjects.TryGetValue(inactiveArtObject, out var value) && value.ArtObject.ActorType == ActorType.TreasureChest)
				{
					Vector3 vector = -FezMath.AlmostClamp(Vector3.Transform(Vector3.UnitZ, value.Rotation));
					value.Position += 1.375f * vector - 0.25f * Vector3.UnitY;
					value.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)Math.PI * -3f / 4f);
					value.ActorSettings.Inactive = true;
				}
			}
			LevelChests.Clear();
			LevelChests.AddRange(base.LevelManager.ArtObjects.Values.Where((ArtObjectInstance x) => x.ArtObject.ActorType == ActorType.TreasureChest));
			FloatingMaps.Clear();
			ArtObjectInstance[] array = base.LevelManager.ArtObjects.Values.Where((ArtObjectInstance x) => x.ArtObject.ActorType == ActorType.TreasureMap).ToArray();
			foreach (ArtObjectInstance artObjectInstance in array)
			{
				if (base.GameState.SaveData.ThisLevel.InactiveArtObjects.Contains(artObjectInstance.Id))
				{
					base.LevelManager.ArtObjects.Remove(artObjectInstance.Id);
					artObjectInstance.Dispose();
				}
				else
				{
					Mesh m = new Mesh
					{
						Blending = BlendingMode.Alphablending,
						SamplerState = SamplerState.PointClamp
					};
					FloatingMaps.Add(m);
					BuildMap(m);
					artObjectInstance.Position += Vector3.UnitY * 0.5f;
					m.Position = artObjectInstance.Position;
					string mapName = artObjectInstance.ActorSettings.TreasureMapName;
					DrawActionScheduler.Schedule(delegate
					{
						m.Effect = new DefaultEffect.LitTextured();
						Texture2D texture2D = base.CMProvider.Global.Load<Texture2D>("Other Textures/maps/" + mapName + "_1");
						Texture2D texture2D2 = base.CMProvider.Global.Load<Texture2D>("Other Textures/maps/" + mapName + "_2");
						for (int j = 0; j < m.Groups.Count; j++)
						{
							m.Groups[j].Texture = ((j % 2 == 0) ? texture2D : texture2D2);
						}
					});
					m.CustomData = new TreasureActorSettings
					{
						ArtObject = artObjectInstance.ArtObject,
						AoInstance = artObjectInstance
					};
					base.LevelManager.ArtObjects.Remove(artObjectInstance.Id);
					artObjectInstance.Dispose();
				}
			}
			RefreshSounds();
		};
		base.Initialize();
	}

	private void RefreshSounds()
	{
		AssembleChords assembleChords = base.SoundManager.CurrentlyPlayingSong?.AssembleChord ?? AssembleChords.C_maj;
		assembleSound = base.CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Collects/SplitUpCube/Assemble_" + assembleChords);
	}

	protected override void LoadContent()
	{
		treasureGetSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Collects/OpenTreasure");
		DrawActionScheduler.Schedule(delegate
		{
			lightBox.Effect = new DefaultEffect.Textured();
			flare.Effect = new DefaultEffect.Textured();
			fadedStar.Effect = new DefaultEffect.VertexColored();
			solidStar.Effect = new DefaultEffect.VertexColored();
			lightBox.Texture = base.CMProvider.Global.Load<Texture2D>("Background Planes/gradient_down");
			flare.Texture = base.CMProvider.Global.Load<Texture2D>("Background Planes/flare");
		});
	}

	protected override void TestConditions()
	{
		if (base.PlayerManager.Action == ActionType.OpeningTreasure || base.PlayerManager.Action == ActionType.FindingTreasure || base.PlayerManager.Action == ActionType.ReadingSign || base.PlayerManager.Action == ActionType.FreeFalling || base.PlayerManager.Action == ActionType.Dying || base.PlayerManager.Action == ActionType.LesserWarp || base.PlayerManager.Action == ActionType.GateWarp || base.GameState.InFpsMode)
		{
			return;
		}
		TrileInstance surface = base.PlayerManager.AxisCollision[VerticalDirection.Up].Surface;
		if (surface != null && !surface.Hidden && surface.Trile.ActorSettings.Type.IsTreasure())
		{
			treasureIsMap = (treasureIsAo = false);
			treasureIsAo = false;
			chestAO = null;
			treasureInstance = surface;
			treasureTrile = surface.Trile;
			treasureActorType = treasureTrile.ActorSettings.Type;
			base.PlayerManager.Action = ActionType.FindingTreasure;
			sinceCollect = TimeSpan.Zero;
			oldDepth = base.PlayerManager.Position.Dot(base.CameraManager.Viewpoint.DepthMask());
			base.PlayerManager.Position = base.PlayerManager.Position * base.CameraManager.Viewpoint.ScreenSpaceMask() + treasureInstance.Position * base.CameraManager.Viewpoint.DepthMask() + 2f * -base.CameraManager.Viewpoint.ForwardVector();
			return;
		}
		foreach (Mesh floatingMap in FloatingMaps)
		{
			Vector3 vector = new Vector3(0.75f);
			Vector3 a = (floatingMap.Position - base.PlayerManager.Center).Abs();
			if (a.Dot(base.CameraManager.Viewpoint.SideMask()) >= vector.X || a.Y >= vector.Y)
			{
				continue;
			}
			Vector3 vector2 = base.CameraManager.Viewpoint.ForwardVector();
			NearestTriles nearestTriles = base.LevelManager.NearestTrile(base.PlayerManager.Position);
			if (nearestTriles.Deep != null)
			{
				Vector3 vector3 = nearestTriles.Deep.Center - nearestTriles.Deep.TransformedSize * vector2 / 2f;
				if ((floatingMap.Position - vector2 - vector3).Dot(vector2) > 0f)
				{
					continue;
				}
			}
			treasureIsMap = true;
			treasureIsAo = false;
			chestAO = null;
			treasureAo = ((TreasureActorSettings)floatingMap.CustomData).ArtObject;
			treasureAoInstance = ((TreasureActorSettings)floatingMap.CustomData).AoInstance;
			treasureActorType = treasureAo.ActorType;
			base.PlayerManager.Action = ActionType.FindingTreasure;
			sinceCollect = TimeSpan.Zero;
			oldDepth = base.PlayerManager.Position.Dot(base.CameraManager.Viewpoint.DepthMask());
			base.PlayerManager.Position = base.PlayerManager.Position * base.CameraManager.Viewpoint.ScreenSpaceMask() + floatingMap.Position * base.CameraManager.Viewpoint.DepthMask() + 2f * -base.CameraManager.Viewpoint.ForwardVector();
			return;
		}
		if (!base.PlayerManager.Grounded || base.PlayerManager.Background)
		{
			return;
		}
		chestAO = null;
		foreach (ArtObjectInstance levelChest in LevelChests)
		{
			if (!levelChest.Visible || levelChest.ActorSettings.Inactive)
			{
				continue;
			}
			Vector3 vector4 = levelChest.ArtObject.Size / 2f;
			Vector3 a2 = (levelChest.Position - base.PlayerManager.Center).Abs();
			if (a2.Dot(base.CameraManager.Viewpoint.SideMask()) >= vector4.X || a2.Y >= vector4.Y)
			{
				continue;
			}
			Vector3 vector5 = base.CameraManager.Viewpoint.ForwardVector();
			NearestTriles nearestTriles2 = base.LevelManager.NearestTrile(base.PlayerManager.Position);
			if (nearestTriles2.Deep != null)
			{
				Vector3 vector6 = nearestTriles2.Deep.Center - nearestTriles2.Deep.TransformedSize * vector5 / 2f;
				if ((levelChest.Position - vector5 - vector6).Dot(vector5) > 0f)
				{
					continue;
				}
			}
			if (FezMath.OrientationFromDirection(FezMath.AlmostClamp(Vector3.Transform(Vector3.UnitZ, levelChest.Rotation))).AsViewpoint() == base.CameraManager.Viewpoint && levelChest.ActorSettings.ContainedTrile != 0)
			{
				chestAO = levelChest;
				sinceCollect = TimeSpan.Zero;
				break;
			}
		}
		if (chestAO != null && base.InputManager.GrabThrow == FezButtonState.Pressed)
		{
			base.GomezService.OnOpenTreasure();
			Volume volume;
			if ((volume = base.PlayerManager.CurrentVolumes.FirstOrDefault((Volume x) => x.ActorSettings != null && x.ActorSettings.IsPointOfInterest && Vector3.DistanceSquared(x.BoundingBox.GetCenter(), chestAO.Position) < 2f)) != null)
			{
				volume.Enabled = false;
				base.GameState.SaveData.ThisLevel.InactiveVolumes.Add(volume.Id);
			}
			base.PlayerManager.Action = ActionType.OpeningTreasure;
		}
	}

	protected override void Begin()
	{
		bool flag = base.PlayerManager.Action == ActionType.OpeningTreasure;
		sinceActive = (flag ? TimeSpan.FromSeconds(-1.0) : TimeSpan.Zero);
		if (flag)
		{
			oldDepth = base.PlayerManager.Position.Dot(base.CameraManager.Viewpoint.DepthMask());
			base.PlayerManager.Position = base.PlayerManager.Position * base.CameraManager.Viewpoint.ScreenSpaceMask() + chestAO.Position * base.CameraManager.Viewpoint.DepthMask() + base.CameraManager.Viewpoint.ForwardVector() * -1.5f;
			base.PlayerManager.LookingDirection = HorizontalDirection.Right;
			aoOrigin = chestAO.Position;
			chestAO.ActorSettings.Inactive = true;
		}
		else
		{
			if (base.PlayerManager.ForcedTreasure != null)
			{
				treasureIsMap = false;
				treasureInstance = base.PlayerManager.ForcedTreasure;
				treasureIsAo = false;
				treasureIsMail = false;
				chestAO = null;
				treasureTrile = treasureInstance.Trile;
				treasureActorType = treasureTrile.ActorSettings.Type;
				base.PlayerManager.Action = ActionType.FindingTreasure;
				sinceCollect = TimeSpan.Zero;
				oldDepth = base.PlayerManager.Position.Dot(base.CameraManager.Viewpoint.DepthMask());
				base.PlayerManager.Position = base.PlayerManager.Position * base.CameraManager.Viewpoint.ScreenSpaceMask() + treasureInstance.Position * base.CameraManager.Viewpoint.DepthMask() + 2f * -base.CameraManager.Viewpoint.ForwardVector();
			}
			aoOrigin = (treasureIsMap ? treasureAoInstance.Position : treasureInstance.Position);
			sinceActive = TimeSpan.FromSeconds(OpeningDuration.TotalSeconds * 0.6000000238418579);
			assembleSound.Emit();
		}
		if (!flag)
		{
			base.PlayerManager.Velocity = new Vector3(0f, 0.05f, 0f);
		}
		WasConstrained = base.CameraManager.Constrained;
		if (WasConstrained)
		{
			OldCenter = base.CameraManager.Center;
			OldPixPerTrix = base.CameraManager.PixelsPerTrixel;
		}
		OldPan = base.CameraManager.PanningConstraints;
		base.CameraManager.Constrained = true;
		base.CameraManager.PanningConstraints = null;
		base.CameraManager.Center = aoOrigin;
		if (!flag)
		{
			lastZoom = base.CameraManager.PixelsPerTrixel;
			base.CameraManager.PixelsPerTrixel = 4f;
		}
		if (flag)
		{
			Mesh mesh = lightBox;
			Mesh mesh2 = solidStar;
			Mesh mesh3 = fadedStar;
			Quaternion quaternion = (flare.Rotation = chestAO.Rotation);
			Quaternion quaternion3 = (mesh3.Rotation = quaternion);
			Quaternion quaternion5 = (mesh2.Rotation = quaternion3);
			Quaternion quaternion7 = (mesh.Rotation = quaternion5);
			aoInitialRotation = quaternion7;
		}
		else
		{
			Mesh mesh4 = solidStar;
			Mesh mesh5 = fadedStar;
			Quaternion quaternion5 = (flare.Rotation = Quaternion.Inverse(base.CameraManager.Rotation));
			Quaternion quaternion7 = (mesh5.Rotation = quaternion5);
			mesh4.Rotation = quaternion7;
		}
		lightBox.Position = aoOrigin - Vector3.UnitY / 2f;
		reculled = (restored = false);
		if (flag)
		{
			treasureAo = null;
			treasureTrile = null;
			treasureIsMap = (treasureIsAo = false);
			treasureActorType = chestAO.ActorSettings.ContainedTrile;
			if (chestAO.ActorSettings.ContainedTrile == ActorType.TreasureMap)
			{
				treasureIsMap = true;
			}
			else if (chestAO.ActorSettings.ContainedTrile == ActorType.Mail)
			{
				treasureIsMail = true;
			}
			else if (chestAO.ActorSettings.ContainedTrile.SupportsArtObjects())
			{
				treasureAo = base.CMProvider.Global.Load<ArtObject>("Art Objects/" + chestAO.ActorSettings.ContainedTrile.GetArtObjectName());
				treasureIsAo = true;
			}
			else
			{
				treasureTrile = base.LevelManager.ActorTriles(chestAO.ActorSettings.ContainedTrile).LastOrDefault();
			}
			Waiters.Wait(1.0, delegate
			{
				treasureGetSound.Emit();
			});
		}
		if (!flag && treasureIsMap)
		{
			treasureOrigin = aoOrigin - new Vector3(0f, 0.125f, 0f);
			string treasureMapName = treasureAoInstance.ActorSettings.TreasureMapName;
			Texture2D texture2D = base.CMProvider.Global.Load<Texture2D>("Other Textures/maps/" + treasureMapName + "_1");
			Texture2D texture2D2 = base.CMProvider.Global.Load<Texture2D>("Other Textures/maps/" + treasureMapName + "_2");
			for (int i = 0; i < map.Groups.Count; i++)
			{
				map.Groups[i].Texture = ((i % 2 == 0) ? texture2D : texture2D2);
			}
			if (treasureMapName == "MAP_MYSTERY")
			{
				mystery2Groups[0] = map.Groups[1];
				mystery2Groups[1] = map.Groups[3];
				mystery2Groups[2] = map.Groups[5];
				mystery2Xbox = texture2D2;
				mystery2Sony = base.CMProvider.Global.Load<Texture2D>("Other Textures/maps/MAP_MYSTERY_2_SONY");
				GamepadState.OnLayoutChanged = (EventHandler)Delegate.Combine(GamepadState.OnLayoutChanged, new EventHandler(UpdateControllerTexture));
				UpdateControllerTexture(null, null);
			}
			Mesh mesh6 = FloatingMaps.First((Mesh x) => ((TreasureActorSettings)x.CustomData).AoInstance == treasureAoInstance);
			map.Position = mesh6.Position - base.CameraManager.Viewpoint.ForwardVector() * 0.5f;
			map.Rotation = mesh6.Rotation;
			FloatingMaps.Remove(mesh6);
		}
		oldGround = null;
		if (base.PlayerManager.Grounded)
		{
			oldGround = base.PlayerManager.Ground.First;
			oldGroundHeight = base.PlayerManager.Ground.First.Center.Y;
		}
		base.SoundManager.FadeVolume(1f, 0.125f, 2f);
	}

	protected override void End()
	{
		if (oldGround != null)
		{
			base.PlayerManager.Position += (oldGround.Center.Y - oldGroundHeight) * Vector3.UnitY;
		}
		base.PlayerManager.Position = base.PlayerManager.Position * base.CameraManager.Viewpoint.ScreenSpaceMask() + oldDepth * base.CameraManager.Viewpoint.DepthMask();
	}

	public override void Update(GameTime gameTime)
	{
		if (!hasHooked)
		{
			LightingPostProcess.DrawOnTopLights += DoDraw;
			hasHooked = true;
		}
		if (!base.GameState.Paused && !base.GameState.InMenuCube && !base.GameState.InMap && !base.GameState.InFpsMode && base.CameraManager.Viewpoint.IsOrthographic() && base.CameraManager.ActionRunning && !base.GameState.Loading && FloatingMaps.Count > 0)
		{
			SinceCreated += (float)gameTime.ElapsedGameTime.TotalSeconds;
			Quaternion quaternion = Quaternion.CreateFromRotationMatrix(Matrix.CreateLookAt(Vector3.One, Vector3.Zero, Vector3.Up));
			float num = (float)Math.Sin(SinceCreated * (float)Math.PI) * 0.1f;
			float num2 = num - lastSin;
			lastSin = num;
			foreach (Mesh floatingMap in FloatingMaps)
			{
				floatingMap.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, FezMath.WrapAngle((0f - SinceCreated) * 2f)) * quaternion;
				floatingMap.Position += num2 * Vector3.UnitY;
			}
		}
		base.Update(gameTime);
	}

	protected override bool Act(TimeSpan elapsed)
	{
		sinceActive += elapsed;
		float num = (float)sinceActive.TotalSeconds;
		float num2 = FezMath.Saturate((float)sinceActive.Ticks / (float)OpeningDuration.Ticks);
		float num3 = MathHelper.Lerp(num2, Easing.EaseInOut(num2, EasingType.Sine, EasingType.Linear), 0.5f) * (float)Math.Pow(0.5, 0.5);
		if ((double)num2 > Math.Pow(0.5, 0.5))
		{
			num3 = (float)Math.Pow(num2, 2.0);
		}
		bool flag = base.PlayerManager.Action == ActionType.OpeningTreasure;
		if (flag)
		{
			chestAO.Position = aoOrigin + num3 * 1.375f * base.CameraManager.Viewpoint.ForwardVector() + (float)Math.Sin(num3 * ((float)Math.PI / 2f) * 3f) * 4f / 16f * Vector3.UnitY;
			chestAO.Rotation = aoInitialRotation * Quaternion.CreateFromAxisAngle(Vector3.UnitX, (0f - num3) * ((float)Math.PI / 2f) * 3f / 2f);
			float num4 = num3 * ((float)Math.PI * 2f) + base.CameraManager.Viewpoint.ToPhi();
			base.CameraManager.Direction = new Vector3((float)Math.Sin(num4), 0f, (float)Math.Cos(num4));
			base.CameraManager.Center = aoOrigin + Vector3.UnitY * 1.5f * num2;
		}
		if (!flag)
		{
			base.PlayerManager.Velocity *= 0.95f;
		}
		lightBox.Material.Diffuse = new Vector3(FezMath.Saturate(num3 * 1.5f) * 0.75f);
		lightBox.Material.Diffuse *= new Vector3(1f, 1f, 0.5f);
		lightBox.Scale = new Vector3(1.6f, FezMath.Saturate(num3 * 1.5f) * 1.5f, 1.2f);
		Vector3 vector3;
		Vector3 position;
		if (num3 > 0.5f)
		{
			if (!restored && chestAO != null)
			{
				restored = true;
				if (treasureIsAo)
				{
					treasureOrigin = aoOrigin - new Vector3(0f, 0.125f, 0f);
					int num5 = IdentifierPool.FirstAvailable(base.LevelManager.ArtObjects);
					treasureAoInstance = new ArtObjectInstance(treasureAo)
					{
						Id = num5
					};
					base.LevelManager.ArtObjects.Add(num5, treasureAoInstance);
					treasureAoInstance.Initialize();
				}
				else if (treasureIsMail)
				{
					treasureOrigin = aoOrigin - new Vector3(0f, 0.125f, 0f);
					string treasureMapName = chestAO.ActorSettings.TreasureMapName;
					Texture2D texture = base.CMProvider.Global.Load<Texture2D>("Other Textures/mail/" + treasureMapName + "_1");
					Texture2D texture2 = base.CMProvider.Global.Load<Texture2D>("Other Textures/mail/" + treasureMapName + "_2");
					mail.Groups[0].Texture = texture;
					mail.Groups[1].Texture = texture2;
				}
				else if (treasureIsMap)
				{
					treasureOrigin = aoOrigin - new Vector3(0f, 0.125f, 0f);
					string treasureMapName2 = chestAO.ActorSettings.TreasureMapName;
					Texture2D texture2D = base.CMProvider.Global.Load<Texture2D>("Other Textures/maps/" + treasureMapName2 + "_1");
					Texture2D texture2D2 = base.CMProvider.Global.Load<Texture2D>("Other Textures/maps/" + treasureMapName2 + "_2");
					for (int i = 0; i < map.Groups.Count; i++)
					{
						map.Groups[i].Texture = ((i % 2 == 0) ? texture2D : texture2D2);
					}
					if (treasureMapName2 == "MAP_MYSTERY")
					{
						mystery2Groups[0] = map.Groups[1];
						mystery2Groups[1] = map.Groups[3];
						mystery2Groups[2] = map.Groups[5];
						mystery2Xbox = texture2D2;
						mystery2Sony = base.CMProvider.Global.Load<Texture2D>("Other Textures/maps/MAP_MYSTERY_2_SONY");
						GamepadState.OnLayoutChanged = (EventHandler)Delegate.Combine(GamepadState.OnLayoutChanged, new EventHandler(UpdateControllerTexture));
						UpdateControllerTexture(null, null);
					}
				}
				else
				{
					treasureOrigin = aoOrigin - treasureTrile.Size / 2f - new Vector3(0f, 0.125f, 0f);
					base.LevelManager.ClearTrile(new TrileEmplacement(treasureOrigin));
					base.LevelManager.RestoreTrile(treasureInstance = new TrileInstance(treasureOrigin, treasureTrile.Id));
					base.LevelMaterializer.CullInstanceIn(treasureInstance, forceAdd: true);
				}
			}
			float num6 = 2f;
			if (!flag)
			{
				num6 = ((!treasureIsMap) ? ((Easing.EaseIn((1f - num3) * 2f, EasingType.Quadratic) * 2f + 0.5f) * 2f) : ((Easing.EaseIn((1f - num3) * 2f, EasingType.Quadratic) * 4f + 0.5f) * 2f));
			}
			else if (treasureIsAo)
			{
				treasureAoInstance.Position = treasureOrigin + Vector3.UnitY * (num3 - 0.5f) * 4f;
			}
			else if (treasureIsMap)
			{
				map.Position = treasureOrigin + Vector3.UnitY * (num3 - 0.5f) * 4f;
			}
			else if (treasureIsMail)
			{
				mail.Position = treasureOrigin + Vector3.UnitY * (num3 - 0.5f) * 4f;
			}
			else
			{
				treasureInstance.Position = treasureOrigin + Vector3.UnitY * (num3 - 0.5f) * 4f;
			}
			if (treasureIsAo)
			{
				Quaternion quaternion = Quaternion.CreateFromAxisAngle(Vector3.Right, 0f - (float)Math.Asin(Math.Sqrt(2.0) / Math.Sqrt(3.0))) * Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 4f);
				treasureAoInstance.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, FezMath.WrapAngle((0f - num) * num6)) * quaternion;
			}
			else if (treasureIsMap)
			{
				if (flag)
				{
					Quaternion quaternion2 = Quaternion.CreateFromAxisAngle(Vector3.Right, (float)Math.Asin(Math.Sqrt(2.0) / Math.Sqrt(3.0)));
					map.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, FezMath.WrapAngle((0f - num) * num6)) * quaternion2;
				}
				else
				{
					map.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, num6 * 1f / 30f) * map.Rotation;
				}
			}
			else if (treasureIsMail)
			{
				Quaternion quaternion3 = Quaternion.CreateFromAxisAngle(Vector3.Right, (float)Math.PI / 8f);
				mail.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, FezMath.WrapAngle((0f - num) * num6)) * quaternion3;
			}
			else
			{
				treasureInstance.Phi = FezMath.WrapAngle((0f - num) * num6);
				base.LevelManager.UpdateInstance(treasureInstance);
			}
			Vector3 vector = (treasureIsMail ? mail.Position : (treasureIsMap ? map.Position : (treasureIsAo ? treasureAoInstance.Position : treasureInstance.Center)));
			Mesh mesh = flare;
			Mesh mesh2 = solidStar;
			vector3 = (fadedStar.Position = vector + Vector3.Normalize(-base.CameraManager.Direction) * 0.5f);
			position = (mesh2.Position = vector3);
			mesh.Position = position;
			if (treasureIsAo)
			{
				treasureAoInstance.Position += Vector3.Transform(-treasureActorType.GetArtifactOffset().XYX() * new Vector3(1f, -1f, 1f) / 16f, treasureAoInstance.Rotation);
			}
		}
		float num7 = FezMath.Saturate((num3 - 0.95f) * 19.999996f);
		fadedStar.Material.Opacity = ((treasureIsMail || treasureIsMap || treasureIsAo || treasureTrile.ActorSettings.Type.IsCubeShard()) ? num7 : 0f);
		flare.Material.Diffuse = new Vector3(num7 / 3f);
		Vector3 vector5 = MathHelper.Lerp(base.CameraManager.Radius * 0.6f, base.CameraManager.Radius * 0.5f, (float)Math.Sin(sinceActive.TotalSeconds * 5.0) / 2f + 0.5f) * Vector3.One;
		Mesh mesh3 = flare;
		Mesh mesh4 = solidStar;
		vector3 = (fadedStar.Scale = vector5 * num7);
		position = (mesh4.Scale = vector3);
		mesh3.Scale = position;
		Mesh mesh5 = solidStar;
		Quaternion rotation = (fadedStar.Rotation = base.CameraManager.Rotation * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, num * 1f));
		mesh5.Rotation = rotation;
		if (base.LevelManager.WaterType == LiquidType.Sewer)
		{
			lightBox.Material.Diffuse *= new Vector3(58f / 85f, 0.76862746f, 0.2509804f);
			flare.Material.Diffuse = new Vector3(58f / 85f, 0.76862746f, 0.2509804f) * new Vector3(num7 / 3f);
			flare.Scale *= 0.75f;
			solidStar.Enabled = true;
			solidStar.Material.Opacity = fadedStar.Material.Opacity * 0.75f;
			solidStar.Scale = base.CameraManager.Radius * Vector3.One;
			fadedStar.Material.Opacity = 0f;
			solidStar.Material.Diffuse = (new Vector3(58f / 85f, 0.76862746f, 0.2509804f) + new Vector3(43f / 51f, 0.9098039f, 0.5803922f)) / 2f;
		}
		else if (base.LevelManager.WaterType == LiquidType.Lava)
		{
			lightBox.Material.Diffuse *= new Vector3(0.99607843f, 0.003921569f, 0f);
			flare.Material.Diffuse = new Vector3(0.99607843f, 0.003921569f, 0f) * num7 / 2.5f;
			flare.Scale *= 0.75f;
			solidStar.Enabled = true;
			solidStar.Material.Opacity = fadedStar.Material.Opacity * 0.75f;
			solidStar.Scale = base.CameraManager.Radius * Vector3.One;
			fadedStar.Material.Opacity = 0f;
			solidStar.Material.Diffuse = new Vector3(0.99607843f, 0.003921569f, 0f);
		}
		else
		{
			solidStar.Enabled = false;
		}
		if (num3 == 1f)
		{
			sinceCollect += elapsed;
			if (!reculled && FezMath.AlmostEqual(base.CameraManager.View.Forward, FezMath.AlmostClamp(base.CameraManager.View.Forward)))
			{
				base.LevelMaterializer.CullInstances();
				reculled = true;
			}
			if (sinceCollect.TotalSeconds > 3.0 || base.InputManager.Jump == FezButtonState.Pressed || base.InputManager.GrabThrow == FezButtonState.Pressed)
			{
				base.SoundManager.FadeVolume(0.125f, 1f, 2f);
				if (flag)
				{
					base.GameState.SaveData.ThisLevel.InactiveArtObjects.Add(chestAO.Id);
					base.GameState.SaveData.ThisLevel.FilledConditions.ChestCount++;
					ArtObjectService.OnTreasureOpened(chestAO.Id);
				}
				else if (base.PlayerManager.ForcedTreasure == null)
				{
					if (treasureIsMap)
					{
						base.GameState.SaveData.ThisLevel.InactiveArtObjects.Add(treasureAoInstance.Id);
						base.GameState.SaveData.ThisLevel.FilledConditions.OtherCollectibleCount++;
					}
					else
					{
						base.GameState.SaveData.ThisLevel.DestroyedTriles.Add(treasureInstance.OriginalEmplacement);
						if (!treasureInstance.Foreign)
						{
							if (treasureInstance.Trile.ActorSettings.Type == ActorType.CubeShard)
							{
								base.GameState.SaveData.ThisLevel.FilledConditions.CubeShardCount++;
							}
							else
							{
								base.GameState.SaveData.ThisLevel.FilledConditions.OtherCollectibleCount++;
							}
						}
					}
				}
				if (!flag)
				{
					base.CameraManager.PixelsPerTrixel = lastZoom;
				}
				base.CameraManager.Constrained = WasConstrained;
				if (WasConstrained)
				{
					base.CameraManager.Center = OldCenter;
					base.CameraManager.PixelsPerTrixel = OldPixPerTrix;
				}
				base.CameraManager.PanningConstraints = OldPan;
				base.PlayerManager.Action = ActionType.Idle;
				switch (treasureActorType)
				{
				case ActorType.SecretCube:
					base.GameState.SaveData.SecretCubes++;
					if (base.GameState.SaveData.SecretCubes > 32)
					{
						base.GameState.SaveData.SecretCubes = 32;
					}
					base.GameState.SaveData.ScoreDirty = true;
					treasureInstance.Collected = true;
					if (treasureInstance.GlobalSpawn)
					{
						base.GomezService.OnCollectedGlobalAnti();
					}
					else
					{
						base.GomezService.OnCollectedAnti();
					}
					SpeedRun.AddCube(anti: true);
					if (base.GameState.SaveData.SecretCubes == 1)
					{
						DotService.Say("DOT_ANTI_A", nearGomez: true, hideAfter: false).Ended = delegate
						{
							DotService.Say("DOT_ANTI_B", nearGomez: true, hideAfter: false).Ended = delegate
							{
								DotService.Say("DOT_ANTI_C", nearGomez: true, hideAfter: false).Ended = delegate
								{
									DotService.Say("DOT_ANTI_D", nearGomez: true, hideAfter: true).Ended = CheckCubes;
								};
							};
						};
					}
					else
					{
						CheckCubes();
					}
					break;
				case ActorType.CubeShard:
					base.GameState.SaveData.CubeShards++;
					if (base.GameState.SaveData.CubeShards > 32)
					{
						base.GameState.SaveData.CubeShards = 32;
					}
					if (base.PlayerManager.ForcedTreasure != null)
					{
						base.GameState.SaveData.CollectedParts = 0;
					}
					base.GameState.SaveData.ScoreDirty = true;
					base.GomezService.OnCollectedShard();
					CheckCubes();
					SpeedRun.AddCube(anti: false);
					break;
				case ActorType.PieceOfHeart:
					base.GameState.SaveData.PiecesOfHeart++;
					if (base.GameState.SaveData.PiecesOfHeart > 3)
					{
						base.GameState.SaveData.PiecesOfHeart = 3;
					}
					base.GameState.SaveData.ScoreDirty = true;
					base.GomezService.OnCollectedPieceOfHeart();
					DotService.Say("DOT_HEART_A", nearGomez: true, hideAfter: false).Ended = delegate
					{
						DotService.Say("DOT_HEART_B", nearGomez: true, hideAfter: false).Ended = delegate
						{
							DotService.Say("DOT_HEART_C", nearGomez: true, hideAfter: true);
						};
					};
					break;
				case ActorType.SkeletonKey:
					base.GameState.SaveData.Keys++;
					if (!base.GameState.SaveData.OneTimeTutorials.ContainsKey("DOT_KEY_A"))
					{
						base.GameState.SaveData.OneTimeTutorials.Add("DOT_KEY_A", value: true);
						DotService.Say("DOT_KEY_A", nearGomez: true, hideAfter: false).Ended = delegate
						{
							DotService.Say("DOT_KEY_B", nearGomez: true, hideAfter: true);
						};
					}
					break;
				case ActorType.NumberCube:
					base.GameState.SaveData.Artifacts.Add(treasureActorType);
					DotService.Say("DOT_ANCIENT_ARTIFACT", nearGomez: true, hideAfter: false).Ended = delegate
					{
						DotService.Say("DOT_NUMBERS_A", nearGomez: true, hideAfter: true);
					};
					break;
				case ActorType.TriSkull:
					base.GameState.SaveData.Artifacts.Add(treasureActorType);
					DotService.Say("DOT_ANCIENT_ARTIFACT", nearGomez: true, hideAfter: false).Ended = delegate
					{
						DotService.Say("DOT_TRISKULL_A", nearGomez: true, hideAfter: false).Ended = delegate
						{
							DotService.Say("DOT_TRISKULL_B", nearGomez: true, hideAfter: false).Ended = delegate
							{
								DotService.Say("DOT_TRISKULL_C", nearGomez: true, hideAfter: false).Ended = delegate
								{
									DotService.Say("DOT_TRISKULL_D", nearGomez: true, hideAfter: true);
								};
							};
						};
					};
					break;
				case ActorType.LetterCube:
					base.GameState.SaveData.Artifacts.Add(treasureActorType);
					DotService.Say("DOT_ANCIENT_ARTIFACT", nearGomez: true, hideAfter: false).Ended = delegate
					{
						DotService.Say("DOT_ALPHABET_A", nearGomez: true, hideAfter: true);
					};
					break;
				case ActorType.Tome:
					base.GameState.SaveData.Artifacts.Add(treasureActorType);
					DotService.Say("DOT_ANCIENT_ARTIFACT", nearGomez: true, hideAfter: false).Ended = delegate
					{
						DotService.Say("DOT_TOME_A", nearGomez: true, hideAfter: false).Ended = delegate
						{
							DotService.Say("DOT_TOME_B", nearGomez: true, hideAfter: true);
						};
					};
					break;
				case ActorType.TreasureMap:
					if (!flag)
					{
						base.GameState.SaveData.Maps.Add(treasureAoInstance.ActorSettings.TreasureMapName);
					}
					else
					{
						base.GameState.SaveData.Maps.Add(chestAO.ActorSettings.TreasureMapName);
					}
					if (base.GameState.SaveData.Maps.Count != 1)
					{
						break;
					}
					DotService.Say("DOT_TREASURE_MAP_A", nearGomez: true, hideAfter: false).Ended = delegate
					{
						DotService.Say("DOT_TREASURE_MAP_B", nearGomez: true, hideAfter: false).Ended = delegate
						{
							DotService.Say("DOT_TREASURE_MAP_C", nearGomez: true, hideAfter: false).Ended = delegate
							{
								DotService.Say("DOT_TREASURE_MAP_D", nearGomez: true, hideAfter: true);
							};
						};
					};
					break;
				}
				if (treasureActorType != ActorType.PieceOfHeart)
				{
					base.GameState.OnHudElementChanged();
				}
				chestAO = null;
				if (base.PlayerManager.ForcedTreasure != null)
				{
					base.PlayerManager.ForcedTreasure.Phi = 0f;
					base.PlayerManager.ForcedTreasure.Collected = true;
					base.LevelManager.UpdateInstance(base.PlayerManager.ForcedTreasure);
					base.LevelManager.ClearTrile(base.PlayerManager.ForcedTreasure);
					base.PlayerManager.ForcedTreasure = null;
					treasureInstance = null;
				}
				else if (treasureIsAo)
				{
					treasureAoInstance.SoftDispose();
					base.LevelManager.ArtObjects.Remove(treasureAoInstance.Id);
					treasureAoInstance = null;
				}
				else if (!treasureIsMap && !treasureIsMail)
				{
					treasureInstance.Phi = 0f;
					treasureInstance.Collected = true;
					base.LevelManager.UpdateInstance(treasureInstance);
					base.LevelManager.ClearTrile(treasureInstance);
					treasureInstance = null;
				}
				base.GameState.Save();
			}
		}
		base.PlayerManager.Animation.Timing.Update(elapsed, 0.9f);
		return false;
	}

	private void CheckCubes()
	{
		switch (base.GameState.SaveData.CubeShards + base.GameState.SaveData.SecretCubes)
		{
		case 4:
			DotService.Say("DOT_CUBES_FOUR_A", nearGomez: true, hideAfter: false).Ended = delegate
			{
				DotService.Say("DOT_CUBES_FOUR_B", nearGomez: true, hideAfter: false).Ended = delegate
				{
					DotService.Say("DOT_CUBES_FOUR_C", nearGomez: true, hideAfter: true);
				};
			};
			break;
		case 8:
			DotService.Say("DOT_CUBES_EIGHT_A", nearGomez: true, hideAfter: false).Ended = delegate
			{
				DotService.Say("DOT_CUBES_EIGHT_B", nearGomez: true, hideAfter: true);
			};
			break;
		case 16:
			DotService.Say("DOT_CUBES_SIXTEEN_A", nearGomez: true, hideAfter: false).Ended = delegate
			{
				DotService.Say("DOT_CUBES_SIXTEEN_B", nearGomez: true, hideAfter: false).Ended = delegate
				{
					DotService.Say("DOT_CUBES_SIXTEEN_C", nearGomez: true, hideAfter: false).Ended = delegate
					{
						DotService.Say("DOT_CUBES_SIXTEEN_D", nearGomez: true, hideAfter: true);
					};
				};
			};
			break;
		case 32:
			DotService.Say("DOT_CUBES_THIRTYTWO_A", nearGomez: true, hideAfter: false).Ended = delegate
			{
				DotService.Say("DOT_CUBES_THIRTYTWO_B", nearGomez: true, hideAfter: false).Ended = delegate
				{
					DotService.Say("DOT_CUBES_THIRTYTWO_C", nearGomez: true, hideAfter: true);
				};
			};
			break;
		case 64:
			DotService.Say("DOT_CUBES_SIXTYFOUR_A", nearGomez: true, hideAfter: false).Ended = delegate
			{
				DotService.Say("DOT_CUBES_SIXTYFOUR_B", nearGomez: true, hideAfter: false).Ended = delegate
				{
					DotService.Say("DOT_CUBES_SIXTYFOUR_C", nearGomez: true, hideAfter: false).Ended = delegate
					{
						DotService.Say("DOT_CUBES_SIXTYFOUR_D", nearGomez: true, hideAfter: true);
					};
				};
			};
			break;
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (base.GameState.Loading)
		{
			return;
		}
		foreach (Mesh floatingMap in FloatingMaps)
		{
			floatingMap.Draw();
		}
		base.GraphicsDevice.GetRasterCombiner();
		DoDraw(lightPrePass: false);
	}

	private void DoDraw()
	{
		base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
		foreach (Mesh floatingMap in FloatingMaps)
		{
			(floatingMap.Effect as DefaultEffect).Pass = LightingEffectPass.Pre;
			floatingMap.Draw();
			(floatingMap.Effect as DefaultEffect).Pass = LightingEffectPass.Main;
		}
		DoDraw(lightPrePass: true);
	}

	private void DoDraw(bool lightPrePass)
	{
		if (IsActionAllowed(base.PlayerManager.Action) && !(base.LevelManager.BlinkingAlpha && lightPrePass))
		{
			bool flag = base.PlayerManager.Action == ActionType.OpeningTreasure;
			if (lightPrePass)
			{
				base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
				(flare.Effect as DefaultEffect).Pass = LightingEffectPass.Pre;
				(lightBox.Effect as DefaultEffect).Pass = LightingEffectPass.Pre;
				(map.Effect as DefaultEffect).Pass = LightingEffectPass.Pre;
				(mail.Effect as DefaultEffect).Pass = LightingEffectPass.Pre;
				flare.Blending = BlendingMode.Alphablending;
				lightBox.Blending = BlendingMode.Alphablending;
				flare.AlwaysOnTop = true;
			}
			base.GraphicsDevice.PrepareStencilWrite(StencilMask.Trails);
			if (flag)
			{
				lightBox.Draw();
			}
			if (treasureIsMap && (restored || !flag))
			{
				map.Draw();
			}
			if (treasureIsMail && restored)
			{
				mail.Draw();
			}
			if (!lightPrePass)
			{
				fadedStar.Draw();
				solidStar.Draw();
			}
			if (base.LevelManager.WaterType != LiquidType.Sewer && !base.LevelManager.BlinkingAlpha)
			{
				base.GraphicsDevice.GetDssCombiner().StencilEnable = false;
				flare.Draw();
				base.GraphicsDevice.GetDssCombiner().StencilEnable = true;
			}
			base.GraphicsDevice.PrepareStencilWrite(StencilMask.None);
			if (lightPrePass)
			{
				(flare.Effect as DefaultEffect).Pass = LightingEffectPass.Main;
				(lightBox.Effect as DefaultEffect).Pass = LightingEffectPass.Main;
				(map.Effect as DefaultEffect).Pass = LightingEffectPass.Main;
				(mail.Effect as DefaultEffect).Pass = LightingEffectPass.Main;
				flare.Blending = BlendingMode.Additive;
				lightBox.Blending = BlendingMode.Additive;
				flare.AlwaysOnTop = false;
			}
		}
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.OpeningTreasure)
		{
			return type == ActionType.FindingTreasure;
		}
		return true;
	}

	private void UpdateControllerTexture(object sender, EventArgs e)
	{
		if (GamepadState.Layout == GamepadState.GamepadLayout.Xbox360)
		{
			Group[] array = mystery2Groups;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Texture = mystery2Xbox;
			}
		}
		else
		{
			Group[] array = mystery2Groups;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Texture = mystery2Sony;
			}
		}
	}
}
