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
using FezGame.Services;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class WorldMap : DrawableGameComponent
{
	private struct QualifiedNode
	{
		public MapNode Node;

		public float Depth;

		public float ScreenDistance;

		public float Transparency;
	}

	public class NodeGroupDataComparer : IComparer<Group>
	{
		public static readonly NodeGroupDataComparer Default = new NodeGroupDataComparer();

		public int Compare(Group x, Group y)
		{
			return -((NodeGroupData)x.CustomData).Depth.CompareTo(((NodeGroupData)y.CustomData).Depth);
		}
	}

	private const float LinkThickness = 0.05375f;

	private static readonly float[] ZoomCycle = new float[5] { 80f, 40f, 20f, 10f, 5f };

	private static readonly string[] DotDialogue = new string[6] { "DOT_MAP_A", "DOT_MAP_B", "DOT_MAP_C", "DOT_MAP_D", "DOT_MAP_E", "DOT_MAP_F" };

	private bool ShowAll;

	private bool AllVisited;

	private Vector3 OriginalCenter;

	private Quaternion OriginalRotation;

	private float OriginalPixPerTrix;

	private Vector3 OriginalDirection;

	private Viewpoint OriginalViewpoint;

	private ProjectedNodeEffect NodeEffect;

	private MapTree MapTree;

	private Mesh NodesMesh;

	private Mesh LinksMesh;

	private Mesh ButtonsMesh;

	private Mesh WavesMesh;

	private Mesh IconsMesh;

	private Mesh LegendMesh;

	private ShaderInstancedIndexedPrimitives<VertexPositionColorTextureInstance, Matrix> GoldenHighlightsGeometry;

	private ShaderInstancedIndexedPrimitives<VertexPositionColorTextureInstance, Matrix> WhiteHighlightsGeometry;

	private ShaderInstancedIndexedPrimitives<VertexPositionColorTextureInstance, Matrix> LinksGeometry;

	private ShaderInstancedIndexedPrimitives<VertexPositionTextureInstance, Matrix> IconsGeometry;

	private List<float> IconsTrailingOffset;

	private Matrix[] IconsOriginalInstances;

	private MapNode CurrentNode;

	private MapNode LastFocusedNode;

	private MapNode FocusNode;

	private GlyphTextRenderer GTR;

	private TimeSpan SinceStarted;

	private RenderTargetHandle FadeInRtHandle;

	private RenderTargetHandle FadeOutRtHandle;

	private SpriteBatch SpriteBatch;

	private static StarField Starfield;

	private SoundEffect sTextNext;

	private SoundEffect sRotateLeft;

	private SoundEffect sRotateRight;

	private SoundEffect sBackground;

	private SoundEffect sZoomIn;

	private SoundEffect sZoomOut;

	private SoundEffect sEnter;

	private SoundEffect sExit;

	private SoundEffect sMagnet;

	private SoundEffect sBeacon;

	private SoundEmitter eBackground;

	private Texture2D ShineTex;

	private Texture2D GrabbedCursor;

	private Texture2D CanClickCursor;

	private Texture2D ClickedCursor;

	private Texture2D PointerCursor;

	private bool CursorSelectable;

	private float SinceMouseMoved = 3f;

	private bool Resolved;

	private int ZoomLevel = ZoomCycle.Length / 2;

	private int DotDialogueIndex;

	private bool FinishedInTransition;

	private bool ScheduleExit;

	private string CurrentLevelName;

	private bool wasLowPass;

	public static WorldMap Instance;

	private static readonly Vector3 GoldColor = new Color(255, 190, 36).ToVector3();

	private readonly List<QualifiedNode> closestNodes = new List<QualifiedNode>();

	private bool chosenByMouseClick;

	private bool blockViewPicking;

	private readonly List<MapNode> nextToCover = new List<MapNode>();

	private readonly List<MapNode> toCover = new List<MapNode>();

	private readonly HashSet<MapNode> hasCovered = new HashSet<MapNode>();

	private Group sewerQRGroup;

	private Group zuhouseQRGroup;

	private Texture sewerQRXbox;

	private Texture zuhouseQRXbox;

	private Texture sewerQRSony;

	private Texture zuhouseQRSony;

	[ServiceDependency]
	public IMouseStateManager MouseState { protected get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderingManager { get; set; }

	[ServiceDependency]
	public IInputManager InputManager { get; set; }

	[ServiceDependency]
	public IGameService GameService { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IFontManager FontManager { get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { get; set; }

	[ServiceDependency]
	public IDotManager DotManager { get; set; }

	[ServiceDependency]
	public ISpeechBubbleManager SpeechBubble { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public WorldMap(Game game)
		: base(game)
	{
		base.UpdateOrder = -10;
		base.DrawOrder = 1000;
		Instance = this;
	}

	public static void PreInitialize()
	{
		StarField obj = new StarField(ServiceHelper.Game)
		{
			FollowCamera = true
		};
		Starfield = obj;
		ServiceHelper.AddComponent(obj);
	}

	public override void Initialize()
	{
		base.Initialize();
		string text = LevelManager.Name.Replace('\\', '/');
		CurrentLevelName = text.Substring(text.LastIndexOf('/') + 1);
		if (CurrentLevelName == "CABIN_INTERIOR_A")
		{
			CurrentLevelName = "CABIN_INTERIOR_B";
		}
		MapTree = CMProvider.Global.Load<MapTree>("MapTree").Clone();
		if (Starfield == null)
		{
			StarField obj = new StarField(base.Game)
			{
				FollowCamera = true
			};
			Starfield = obj;
			ServiceHelper.AddComponent(obj);
		}
		LastFocusedNode = (FocusNode = (CurrentNode = MapTree.Root));
		NodeEffect = new ProjectedNodeEffect();
		NodesMesh = new Mesh
		{
			Effect = NodeEffect,
			Blending = BlendingMode.Alphablending,
			SamplerState = SamplerStates.PointMipClamp
		};
		LinksMesh = new Mesh
		{
			Effect = new InstancedMapEffect(),
			Blending = BlendingMode.Alphablending,
			Culling = CullMode.None
		};
		LinksGeometry = CreateLinksGroup(Color.White, isComplete: false, CullMode.CullCounterClockwiseFace);
		List<Matrix> list = new List<Matrix>();
		BuildNodes(MapTree.Root, null, null, Vector3.Zero, list);
		GoldenHighlightsGeometry = CreateLinksGroup(new Color(GoldColor), isComplete: true, CullMode.CullClockwiseFace);
		List<Matrix> list2 = new List<Matrix>();
		WhiteHighlightsGeometry = CreateLinksGroup(Color.White, isComplete: false, CullMode.CullClockwiseFace);
		List<Matrix> list3 = new List<Matrix>();
		foreach (Group group3 in NodesMesh.Groups)
		{
			NodeGroupData nodeGroupData = group3.CustomData as NodeGroupData;
			MapNode node = nodeGroupData.Node;
			Vector3 vector = new Vector3(node.NodeType.GetSizeFactor() + 0.125f);
			Vector3 position = nodeGroupData.Node.Group.Position;
			Vector3 one = Vector3.One;
			if (GameState.SaveData.World.TryGetValue(GameState.IsTrialMode ? ("trial/" + node.LevelName) : node.LevelName, out var value) && value.FilledConditions.Fullfills(node.Conditions))
			{
				nodeGroupData.Complete = true;
				one = GoldColor;
				nodeGroupData.HighlightInstance = list2.Count;
				list2.Add(new Matrix(position.X, position.Y, position.Z, 0f, one.X, one.Y, one.Z, 1f, vector.X, vector.Y, vector.Z, 0f, 0f, 0f, 0f, 0f));
			}
			else
			{
				nodeGroupData.HighlightInstance = list3.Count;
				list3.Add(new Matrix(position.X, position.Y, position.Z, 0f, one.X, one.Y, one.Z, 1f, vector.X, vector.Y, vector.Z, 0f, 0f, 0f, 0f, 0f));
			}
		}
		LinksGeometry.Instances = list.ToArray();
		LinksGeometry.InstanceCount = list.Count;
		LinksGeometry.UpdateBuffers();
		GoldenHighlightsGeometry.Instances = list2.ToArray();
		GoldenHighlightsGeometry.InstanceCount = list2.Count;
		GoldenHighlightsGeometry.UpdateBuffers();
		WhiteHighlightsGeometry.Instances = list3.ToArray();
		WhiteHighlightsGeometry.InstanceCount = list3.Count;
		WhiteHighlightsGeometry.UpdateBuffers();
		CreateIcons();
		PlayerManager.CanControl = false;
		OriginalCenter = CameraManager.Center;
		OriginalPixPerTrix = CameraManager.PixelsPerTrixel;
		OriginalViewpoint = CameraManager.Viewpoint;
		OriginalRotation = Quaternion.Identity;
		OriginalDirection = CameraManager.Direction;
		SpriteBatch = new SpriteBatch(base.GraphicsDevice);
		GameService.CloseScroll(null);
		float aspectRatio = base.GraphicsDevice.Viewport.AspectRatio;
		float num = (float)base.GraphicsDevice.Viewport.Height / (720f * base.GraphicsDevice.GetViewScale());
		float num2 = 22.5f * aspectRatio;
		float num3 = 22.5f;
		ButtonsMesh = new Mesh
		{
			Effect = new AnimatedPlaneEffect
			{
				ForcedProjectionMatrix = Matrix.CreateOrthographic(num2, num3, 0.1f, 100f),
				ForcedViewMatrix = Matrix.CreateLookAt(new Vector3(0f, 0f, 10f), Vector3.Zero, Vector3.Up)
			},
			AlwaysOnTop = true,
			DepthWrites = false,
			SamplerState = SamplerState.PointClamp,
			Position = new Vector3(num2 / 2f * 0.75f, num3 / 2f * 0.85f - 1f, 0f)
		};
		Group group = ButtonsMesh.AddFace(new Vector3(0.333f + 0.666f / num, 7.375f, 1f), new Vector3(FezMath.Saturate(num - 1f) * 0.333f, -5.875f, 0f), FaceOrientation.Front, centeredOnOrigin: false);
		group.Material = new Material
		{
			Diffuse = new Vector3(0.0625f)
		};
		group.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/fullwhite");
		LegendMesh = new Mesh
		{
			Effect = new DefaultEffect.Textured
			{
				ForcedProjectionMatrix = Matrix.CreateOrthographic(num2, num3, 0.1f, 100f),
				ForcedViewMatrix = Matrix.CreateLookAt(new Vector3(0f, 0f, 10f), Vector3.Zero, Vector3.Up)
			},
			AlwaysOnTop = true,
			DepthWrites = false,
			SamplerState = SamplerState.PointClamp,
			Position = new Vector3((0f - num2) / 2f * 0.9f, (0f - num3) / 2f * 0.9f, 0f),
			Blending = BlendingMode.Alphablending
		};
		Group group2 = LegendMesh.AddFace(new Vector3(0.333f + 0.666f / num, 5.75f, 1f), new Vector3(0f, 0f, 0f), FaceOrientation.Front, centeredOnOrigin: false);
		group2.Material = new Material
		{
			Diffuse = new Vector3(0.0625f)
		};
		group2.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/FullWhite");
		LegendMesh.AddFace(new Vector3(1f, 8f, 1f), new Vector3(0.25f, -2.5f, 0f), FaceOrientation.Front, centeredOnOrigin: false).Texture = CMProvider.Global.Load<Texture2D>("Other Textures/map_controls/icons_legend");
		WavesMesh = new Mesh
		{
			Effect = new DefaultEffect.Textured(),
			Blending = BlendingMode.Alphablending,
			SamplerState = SamplerState.LinearClamp,
			Texture = CMProvider.Global.Load<Texture2D>("Other Textures/map_controls/cube_outline")
		};
		for (int i = 0; i < 4; i++)
		{
			WavesMesh.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true).Material = new Material();
		}
		GTR = new GlyphTextRenderer(base.Game);
		ZoomLevel = 2;
		if (!GameState.SaveData.HasHadMapHelp)
		{
			DotManager.ForceDrawOrder(base.DrawOrder + 1);
			SpeechBubble.ForceDrawOrder(base.DrawOrder + 1);
			DotManager.Behaviour = DotHost.BehaviourType.ClampToTarget;
			DotManager.Hidden = false;
		}
		else
		{
			DotManager.Hidden = true;
		}
		sTextNext = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/TextNext");
		sRotateLeft = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/RotateLeft");
		sRotateRight = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/RotateRight");
		sEnter = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/EnterMenucubeOrMap");
		sExit = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/ExitMenucubeOrMap");
		sZoomIn = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/ZoomIn");
		sZoomOut = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/ZoomOut");
		sMagnet = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/WorldMapMagnet");
		sBeacon = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/MapBeacon");
		sBackground = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/MapBackground");
		eBackground = sBackground.Emit(loop: true);
		sEnter.Emit();
		CameraManager.OriginalDirection = CameraManager.Direction;
		ShineTex = CMProvider.Global.Load<Texture2D>("Other Textures/map_screens/shine_rays");
		PointerCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_POINTER");
		CanClickCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_CLICKER_A");
		ClickedCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_CLICKER_B");
		GrabbedCursor = CMProvider.Global.Load<Texture2D>("Other Textures/cursor/CURSOR_GRABBER");
		wasLowPass = SoundManager.IsLowPass;
		if (!wasLowPass)
		{
			SoundManager.FadeFrequencies(lowPass: true);
		}
		FadeOutRtHandle = TargetRenderingManager.TakeTarget();
		FadeInRtHandle = TargetRenderingManager.TakeTarget();
		TargetRenderingManager.ScheduleHook(base.DrawOrder, FadeInRtHandle.Target);
		Starfield.Opacity = (LinksMesh.Material.Opacity = (NodesMesh.Material.Opacity = (LegendMesh.Material.Opacity = 0f)));
	}

	private ShaderInstancedIndexedPrimitives<VertexPositionColorTextureInstance, Matrix> CreateLinksGroup(Color color, bool isComplete, CullMode cullMode)
	{
		Group group = LinksMesh.AddGroup();
		ShaderInstancedIndexedPrimitives<VertexPositionColorTextureInstance, Matrix> shaderInstancedIndexedPrimitives = (ShaderInstancedIndexedPrimitives<VertexPositionColorTextureInstance, Matrix>)(group.Geometry = new ShaderInstancedIndexedPrimitives<VertexPositionColorTextureInstance, Matrix>(PrimitiveType.TriangleList, 58));
		shaderInstancedIndexedPrimitives.Vertices = new VertexPositionColorTextureInstance[8]
		{
			new VertexPositionColorTextureInstance(new Vector3(-1f, -1f, -1f) / 2f, color, Vector2.Zero),
			new VertexPositionColorTextureInstance(new Vector3(1f, -1f, -1f) / 2f, color, Vector2.Zero),
			new VertexPositionColorTextureInstance(new Vector3(1f, 1f, -1f) / 2f, color, Vector2.Zero),
			new VertexPositionColorTextureInstance(new Vector3(-1f, 1f, -1f) / 2f, color, Vector2.Zero),
			new VertexPositionColorTextureInstance(new Vector3(-1f, -1f, 1f) / 2f, color, Vector2.Zero),
			new VertexPositionColorTextureInstance(new Vector3(1f, -1f, 1f) / 2f, color, Vector2.Zero),
			new VertexPositionColorTextureInstance(new Vector3(1f, 1f, 1f) / 2f, color, Vector2.Zero),
			new VertexPositionColorTextureInstance(new Vector3(-1f, 1f, 1f) / 2f, color, Vector2.Zero)
		};
		shaderInstancedIndexedPrimitives.Indices = new int[36]
		{
			0, 1, 2, 0, 2, 3, 1, 5, 6, 1,
			6, 2, 0, 7, 4, 0, 3, 7, 3, 2,
			6, 3, 6, 7, 4, 6, 5, 4, 7, 6,
			0, 5, 1, 0, 4, 5
		};
		group.CustomData = isComplete;
		group.CullMode = cullMode;
		return shaderInstancedIndexedPrimitives;
	}

	private void CreateIcons()
	{
		IconsMesh = new Mesh
		{
			Effect = new InstancedMapEffect
			{
				Billboard = true
			},
			AlwaysOnTop = true,
			DepthWrites = false,
			SamplerState = SamplerState.PointClamp,
			Texture = CMProvider.Global.Load<Texture2D>("Other Textures/map_controls/icons")
		};
		Group group = IconsMesh.AddGroup();
		IconsGeometry = new ShaderInstancedIndexedPrimitives<VertexPositionTextureInstance, Matrix>(PrimitiveType.TriangleList, 58);
		group.Geometry = IconsGeometry;
		IconsGeometry.Vertices = new VertexPositionTextureInstance[4]
		{
			new VertexPositionTextureInstance(new Vector3(-0.5f, 0f, 0f), new Vector2(0f, 0f)),
			new VertexPositionTextureInstance(new Vector3(0.5f, 0f, 0f), new Vector2(0.625f, 0f)),
			new VertexPositionTextureInstance(new Vector3(0.5f, -1f, 0f), new Vector2(0.625f, 1f)),
			new VertexPositionTextureInstance(new Vector3(-0.5f, -1f, 0f), new Vector2(0f, 1f))
		};
		IconsGeometry.Indices = new int[6] { 0, 1, 3, 3, 1, 2 };
		List<Matrix> list = new List<Matrix>();
		IconsTrailingOffset = new List<float>();
		foreach (Group group2 in NodesMesh.Groups)
		{
			NodeGroupData nodeGroupData = (NodeGroupData)group2.CustomData;
			MapNode node = nodeGroupData.Node;
			if (!GameState.SaveData.World.TryGetValue(GameState.IsTrialMode ? ("trial/" + node.LevelName) : node.LevelName, out var value))
			{
				if (!ShowAll || !AllVisited)
				{
					continue;
				}
				value = new LevelSaveData();
			}
			float num = 0f;
			Vector3 vector = group2.Position + node.NodeType.GetSizeFactor() / 2f * new Vector3(1f, 1f, -1f) + 0.2f * new Vector3(1f, 0f, -1f);
			if (node.HasWarpGate)
			{
				nodeGroupData.IconInstances.Add(list.Count);
				list.Add(new Matrix(vector.X, vector.Y, vector.Z, 0f, 1f, 0f, 0f, 0f, 0.25f, 0.225f, 0.25f, 0f, 0f, 0f, 1f, 9f / 64f));
				IconsTrailingOffset.Add(num);
				num += 0.9f;
			}
			if (node.HasLesserGate)
			{
				nodeGroupData.IconInstances.Add(list.Count);
				list.Add(new Matrix(vector.X, vector.Y, vector.Z, 0f, 1f, 0f, 0f, 0f, 0.25f, 0.175f, 0.25f, 0f, 0f, 9f / 64f, 1f, 7f / 64f));
				IconsTrailingOffset.Add(num);
				num += 0.7f;
			}
			if (nodeGroupData.Node.Conditions.ChestCount > value.FilledConditions.ChestCount)
			{
				nodeGroupData.IconInstances.Add(list.Count);
				list.Add(new Matrix(vector.X, vector.Y, vector.Z, 0f, 1f, 0f, 0f, 0f, 0.25f, 0.15f, 0.25f, 0f, 0f, 0.25f, 1f, 3f / 32f));
				IconsTrailingOffset.Add(num);
				num += 0.6f;
			}
			if (nodeGroupData.Node.Conditions.LockedDoorCount > value.FilledConditions.LockedDoorCount)
			{
				nodeGroupData.IconInstances.Add(list.Count);
				list.Add(new Matrix(vector.X, vector.Y, vector.Z, 0f, 1f, 0f, 0f, 0f, 0.25f, 0.225f, 0.25f, 0f, 0f, 11f / 32f, 1f, 9f / 64f));
				IconsTrailingOffset.Add(num);
				num += 0.9f;
			}
			if (nodeGroupData.Node.Conditions.CubeShardCount > value.FilledConditions.CubeShardCount)
			{
				nodeGroupData.IconInstances.Add(list.Count);
				list.Add(new Matrix(vector.X, vector.Y, vector.Z, 0f, 1f, 0f, 0f, 0f, 0.25f, 0.225f, 0.25f, 0f, 0f, 31f / 64f, 1f, 9f / 64f));
				IconsTrailingOffset.Add(num);
				num += 0.9f;
			}
			if (nodeGroupData.Node.Conditions.SplitUpCount > value.FilledConditions.SplitUpCount)
			{
				nodeGroupData.IconInstances.Add(list.Count);
				list.Add(new Matrix(vector.X, vector.Y, vector.Z, 0f, 1f, 0f, 0f, 0f, 0.25f, 0.275f, 0.25f, 0f, 0f, 0.625f, 1f, 11f / 64f));
				IconsTrailingOffset.Add(num);
				num += 1.1f;
			}
			if (nodeGroupData.Node.Conditions.SecretCount + nodeGroupData.Node.Conditions.ScriptIds.Count > value.FilledConditions.SecretCount + value.FilledConditions.ScriptIds.Count)
			{
				nodeGroupData.IconInstances.Add(list.Count);
				list.Add(new Matrix(vector.X, vector.Y, vector.Z, 0f, 1f, 0f, 0f, 0f, 0.25f, 0.275f, 0.25f, 0f, 0f, 51f / 64f, 1f, 11f / 64f));
				IconsTrailingOffset.Add(num);
			}
		}
		IconsGeometry.Instances = list.ToArray();
		IconsOriginalInstances = list.ToArray();
		IconsGeometry.InstanceCount = list.Count;
		IconsGeometry.UpdateBuffers();
	}

	protected override void Dispose(bool disposing)
	{
		GameState.InMap = false;
		GameState.SkipRendering = false;
		PlayerManager.CanControl = true;
		if (FadeOutRtHandle != null)
		{
			TargetRenderingManager.ReturnTarget(FadeOutRtHandle);
		}
		FadeOutRtHandle = null;
		if (FadeInRtHandle != null)
		{
			TargetRenderingManager.ReturnTarget(FadeInRtHandle);
		}
		FadeInRtHandle = null;
		if (eBackground != null && !eBackground.Dead)
		{
			eBackground.FadeOutAndDie(0.25f);
			eBackground = null;
		}
		LinksMesh.Dispose();
		NodesMesh.Dispose();
		IconsMesh.Dispose();
		ButtonsMesh.Dispose();
		InputManager.StrictRotation = false;
		if (!wasLowPass)
		{
			SoundManager.FadeFrequencies(lowPass: false);
		}
		Instance = null;
		base.Dispose(disposing);
	}

	private void BuildNodes(MapNode node, MapNode.Connection parentConnection, MapNode parentNode, Vector3 offset, List<Matrix> instances)
	{
		Group group = null;
		bool flag = GameState.SaveData.World.ContainsKey(GameState.IsTrialMode ? ("trial/" + node.LevelName) : node.LevelName);
		if ((parentNode != null && GameState.SaveData.World.ContainsKey(GameState.IsTrialMode ? ("trial/" + parentNode.LevelName) : parentNode.LevelName)) || node.Connections.Any((MapNode.Connection connection) => GameState.SaveData.World.ContainsKey(GameState.IsTrialMode ? ("trial/" + connection.Node.LevelName) : connection.Node.LevelName)) || flag || AllVisited || ShowAll)
		{
			group = NodesMesh.AddFlatShadedBox(new Vector3(node.NodeType.GetSizeFactor()), Vector3.Zero, Color.White, centeredOnOrigin: true);
			group.Position = offset;
			group.CustomData = new NodeGroupData
			{
				Node = node,
				LevelName = node.LevelName
			};
			group.Material = new Material();
			node.Group = group;
		}
		if (node.LevelName == CurrentLevelName)
		{
			LastFocusedNode = (FocusNode = (CurrentNode = node));
		}
		if ((flag || AllVisited || ShowAll) && group != null)
		{
			if (MemoryContentManager.AssetExists("Other Textures/map_screens/" + node.LevelName))
			{
				group.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/map_screens/" + node.LevelName);
			}
			if (node.LevelName == "SEWER_QR")
			{
				sewerQRGroup = group;
				sewerQRXbox = group.Texture;
				sewerQRSony = CMProvider.Global.Load<Texture2D>("Other Textures/map_screens/" + node.LevelName + "_SONY");
				GamepadState.OnLayoutChanged = (EventHandler)Delegate.Combine(GamepadState.OnLayoutChanged, new EventHandler(UpdateControllerTexture));
				UpdateControllerTexture(null, null);
			}
			else if (node.LevelName == "ZU_HOUSE_QR")
			{
				zuhouseQRGroup = group;
				zuhouseQRXbox = group.Texture;
				zuhouseQRSony = CMProvider.Global.Load<Texture2D>("Other Textures/map_screens/" + node.LevelName + "_SONY");
				GamepadState.OnLayoutChanged = (EventHandler)Delegate.Combine(GamepadState.OnLayoutChanged, new EventHandler(UpdateControllerTexture));
				UpdateControllerTexture(null, null);
			}
		}
		foreach (MapNode.Connection c in node.Connections)
		{
			if (c.Node.NodeType == LevelNodeType.Lesser && node.Connections.Any((MapNode.Connection x) => x.Face == c.Face && c.Node.NodeType != LevelNodeType.Lesser))
			{
				if (!node.Connections.Any((MapNode.Connection x) => x.Face == FaceOrientation.Top))
				{
					c.Face = FaceOrientation.Top;
				}
				else if (!node.Connections.Any((MapNode.Connection x) => x.Face == FaceOrientation.Down))
				{
					c.Face = FaceOrientation.Down;
				}
			}
		}
		foreach (MapNode.Connection c2 in node.Connections)
		{
			c2.MultiBranchId = node.Connections.Where((MapNode.Connection x) => x.Face == c2.Face).Max((MapNode.Connection x) => x.MultiBranchId) + 1;
			c2.MultiBranchCount = node.Connections.Count((MapNode.Connection x) => x.Face == c2.Face);
		}
		float num = 0f;
		foreach (MapNode.Connection item in node.Connections.OrderByDescending((MapNode.Connection x) => x.Node.NodeType.GetSizeFactor()))
		{
			if (parentConnection != null && item.Face == parentConnection.Face.GetOpposite())
			{
				item.Face = item.Face.GetOpposite();
			}
			bool num2 = AllVisited || flag || GameState.SaveData.World.ContainsKey(GameState.IsTrialMode ? ("trial/" + item.Node.LevelName) : item.Node.LevelName);
			float num3 = 3f + (node.NodeType.GetSizeFactor() + item.Node.NodeType.GetSizeFactor()) / 2f;
			if ((node.NodeType == LevelNodeType.Hub || item.Node.NodeType == LevelNodeType.Hub) && node.NodeType != LevelNodeType.Lesser && item.Node.NodeType != LevelNodeType.Lesser)
			{
				num3 += 1f;
			}
			if ((node.NodeType == LevelNodeType.Lesser || item.Node.NodeType == LevelNodeType.Lesser) && item.MultiBranchCount == 1)
			{
				num3 -= (float)(item.Face.IsSide() ? 1 : 2);
			}
			num3 *= 1.25f + item.BranchOversize;
			float num4 = num3 * 0.375f;
			if (item.Node.NodeType == LevelNodeType.Node && node.NodeType == LevelNodeType.Node)
			{
				num4 *= 1.5f;
			}
			Vector3 vector = item.Face.AsVector();
			Vector3 vector2 = Vector3.Zero;
			if (item.MultiBranchCount > 1)
			{
				vector2 = ((float)(item.MultiBranchId - 1) - (float)(item.MultiBranchCount - 1) / 2f) * (FezMath.XZMask - item.Face.AsVector().Abs()) * num4;
			}
			BuildNodes(item.Node, item, node, offset + vector * num3 + vector2, instances);
			if (num2)
			{
				if (item.LinkInstances == null)
				{
					item.LinkInstances = new List<int>();
				}
				if (item.MultiBranchCount > 1)
				{
					num = Math.Max(num, num3 / 2f);
					Vector3 vector3 = vector * num + new Vector3(0.05375f);
					Vector3 vector4 = vector * num / 2f + offset;
					item.LinkInstances.Add(instances.Count);
					instances.Add(new Matrix(vector4.X, vector4.Y, vector4.Z, 0f, 1f, 1f, 1f, 1f, vector3.X, vector3.Y, vector3.Z, 0f, 0f, 0f, 0f, 0f));
					vector3 = vector2 + new Vector3(0.05375f);
					vector4 = vector2 / 2f + offset + vector * num;
					item.LinkInstances.Add(instances.Count);
					instances.Add(new Matrix(vector4.X, vector4.Y, vector4.Z, 0f, 1f, 1f, 1f, 1f, vector3.X, vector3.Y, vector3.Z, 0f, 0f, 0f, 0f, 0f));
					float num5 = num3 - num;
					vector3 = vector * num5 + new Vector3(0.05375f);
					vector4 = vector * num5 / 2f + offset + vector * num + vector2;
					item.LinkInstances.Add(instances.Count);
					instances.Add(new Matrix(vector4.X, vector4.Y, vector4.Z, 0f, 1f, 1f, 1f, 1f, vector3.X, vector3.Y, vector3.Z, 0f, 0f, 0f, 0f, 0f));
				}
				else
				{
					Vector3 vector5 = vector * num3 + new Vector3(0.05375f);
					Vector3 vector6 = vector * num3 / 2f + offset;
					item.LinkInstances.Add(instances.Count);
					instances.Add(new Matrix(vector6.X, vector6.Y, vector6.Z, 0f, 1f, 1f, 1f, 1f, vector5.X, vector5.Y, vector5.Z, 0f, 0f, 0f, 0f, 0f));
				}
				DoSpecial(item, offset, vector, num3, instances);
			}
			item.Node.Connections.Add(new MapNode.Connection
			{
				Node = node,
				Face = item.Face.GetOpposite(),
				BranchOversize = item.BranchOversize,
				LinkInstances = item.LinkInstances
			});
		}
	}

	private static void DoSpecial(MapNode.Connection c, Vector3 offset, Vector3 faceVector, float sizeFactor, List<Matrix> instances)
	{
		if (c.Node.LevelName == "LIGHTHOUSE_SPIN")
		{
			Vector3 backward = Vector3.Backward;
			float num = 3.425f;
			Vector3 vector = backward * num + new Vector3(0.05375f);
			Vector3 vector2 = backward * num / 2f + offset + faceVector * sizeFactor;
			c.LinkInstances.Add(instances.Count);
			instances.Add(new Matrix(vector2.X, vector2.Y, vector2.Z, 0f, 1f, 1f, 1f, 1f, vector.X, vector.Y, vector.Z, 0f, 0f, 0f, 0f, 0f));
		}
		if (c.Node.LevelName == "LIGHTHOUSE_HOUSE_A")
		{
			Vector3 right = Vector3.Right;
			float num2 = 5f;
			Vector3 vector3 = right * num2 + new Vector3(0.05375f);
			Vector3 vector4 = right * num2 / 2f + offset + faceVector * sizeFactor;
			c.LinkInstances.Add(instances.Count);
			instances.Add(new Matrix(vector4.X, vector4.Y, vector4.Z, 0f, 1f, 1f, 1f, 1f, vector3.X, vector3.Y, vector3.Z, 0f, 0f, 0f, 0f, 0f));
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused || GameState.Loading || (!Resolved && !ScheduleExit))
		{
			return;
		}
		SinceStarted += gameTime.ElapsedGameTime;
		SinceMouseMoved += (float)gameTime.ElapsedGameTime.TotalSeconds;
		if (MouseState.Movement.X != 0 || MouseState.Movement.Y != 0)
		{
			SinceMouseMoved = 0f;
		}
		Vector3 right = CameraManager.InverseView.Right;
		Vector3 up = CameraManager.InverseView.Up;
		Vector3 forward = CameraManager.InverseView.Forward;
		if (!GameState.InMap)
		{
			ServiceHelper.RemoveComponent(this);
			return;
		}
		if (InputManager.Back == FezButtonState.Pressed || (InputManager.CancelTalk == FezButtonState.Pressed && SpeechBubble.Hidden))
		{
			Exit();
			return;
		}
		float viewScale = base.GraphicsDevice.GetViewScale();
		float num = (float)base.GraphicsDevice.Viewport.Width / (1280f * viewScale);
		CameraManager.Radius = ZoomCycle[ZoomLevel] * num;
		float radius = CameraManager.Radius;
		bool flag = MouseState.RightButton.State == MouseButtonStates.Dragging;
		if (FinishedInTransition)
		{
			if (InputManager.RotateRight == FezButtonState.Pressed)
			{
				sRotateRight.Emit();
			}
			if (InputManager.RotateLeft == FezButtonState.Pressed)
			{
				sRotateLeft.Emit();
			}
			if (!FezMath.AlmostEqual(InputManager.Movement, Vector2.Zero))
			{
				CameraManager.Center += InputManager.Movement.X * right * 0.015f * radius / viewScale + InputManager.Movement.Y * up * 0.015f * radius / viewScale;
			}
			if (flag)
			{
				CameraManager.Center += -MouseState.Movement.X * right * 0.0008f * radius / (viewScale * viewScale) + MouseState.Movement.Y * up * 0.0008f * radius / (viewScale * viewScale);
			}
			else if (MouseState.RightButton.State == MouseButtonStates.DragEnded)
			{
				blockViewPicking = false;
			}
			blockViewPicking |= flag;
			if (InputManager.MapZoomIn == FezButtonState.Pressed && ZoomLevel != ZoomCycle.Length - 1)
			{
				ZoomLevel++;
				sZoomIn.Emit();
				ShadeNeighbourNodes();
			}
			if (InputManager.MapZoomOut == FezButtonState.Pressed && ZoomLevel != 0)
			{
				ZoomLevel--;
				sZoomOut.Emit();
				ShadeNeighbourNodes();
			}
			if (MouseState.LeftButton.State == MouseButtonStates.Pressed)
			{
				MousePicking();
			}
			ViewPicking();
		}
		foreach (Group group in ButtonsMesh.Groups)
		{
			if (group.CustomData is AnimatedTexture animatedTexture)
			{
				animatedTexture.Timing.Update(gameTime.ElapsedGameTime);
				int width = animatedTexture.Texture.Width;
				int height = animatedTexture.Texture.Height;
				int frame = animatedTexture.Timing.Frame;
				Rectangle rectangle = animatedTexture.Offsets[frame];
				group.TextureMatrix.Set(new Matrix((float)rectangle.Width / (float)width, 0f, 0f, 0f, 0f, (float)rectangle.Height / (float)height, 0f, 0f, (float)rectangle.X / (float)width, (float)rectangle.Y / (float)height, 1f, 0f, 0f, 0f, 0f, 0f));
			}
		}
		if (CurrentNode.Group != null)
		{
			double num2 = SinceStarted.TotalSeconds * 0.44999998807907104;
			for (int i = 0; i < WavesMesh.Groups.Count; i++)
			{
				float num3 = Easing.Ease(FezMath.Frac((float)num2 + (float)i / (float)(WavesMesh.Groups.Count * 2)), -0.5f, EasingType.Sine);
				float x = WavesMesh.Groups[i].Scale.X;
				WavesMesh.Groups[i].Scale = new Vector3(num3 * 5f * CurrentNode.NodeType.GetSizeFactor());
				if (i == 0 && x > WavesMesh.Groups[i].Scale.X)
				{
					sBeacon.EmitAt(WavesMesh.Position).OverrideMap = true;
				}
				WavesMesh.Groups[i].Material.Opacity = 1f - (num3 - 0.4f) / 0.6f;
			}
		}
		float num4 = (float)base.GraphicsDevice.Viewport.Width / (viewScale / 1280f);
		ScaleIcons();
		foreach (Group group2 in NodesMesh.Groups)
		{
			NodeGroupData nodeGroupData = group2.CustomData as NodeGroupData;
			ShaderInstancedIndexedPrimitives<VertexPositionColorTextureInstance, Matrix> shaderInstancedIndexedPrimitives = (nodeGroupData.Complete ? GoldenHighlightsGeometry : WhiteHighlightsGeometry);
			float num5 = nodeGroupData.Node.NodeType.GetSizeFactor() + 0.125f;
			Matrix matrix = shaderInstancedIndexedPrimitives.Instances[nodeGroupData.HighlightInstance];
			Vector3 vector = new Vector3(matrix.M31, matrix.M32, matrix.M33);
			if (FocusNode == nodeGroupData.Node)
			{
				vector = Vector3.Lerp(vector, new Vector3(1.25f * num5), 0.25f);
			}
			else if (!FezMath.AlmostEqual(vector.X, 1f))
			{
				vector = Vector3.Lerp(vector, new Vector3(num5), 0.25f);
			}
			Vector3 vector2 = group2.Position + nodeGroupData.Node.NodeType.GetSizeFactor() / 2f * new Vector3(1f, 1f, -1f) * (vector / num5) + 0.2f * new Vector3(1f, 0f, -1f);
			float iconScale = GetIconScale(viewScale, radius / viewScale / num4);
			shaderInstancedIndexedPrimitives.Instances[nodeGroupData.HighlightInstance].M31 = vector.X;
			shaderInstancedIndexedPrimitives.Instances[nodeGroupData.HighlightInstance].M32 = vector.Y;
			shaderInstancedIndexedPrimitives.Instances[nodeGroupData.HighlightInstance].M33 = vector.Z;
			shaderInstancedIndexedPrimitives.InstancesDirty = true;
			foreach (int iconInstance in nodeGroupData.IconInstances)
			{
				Vector3 vector3 = IconsTrailingOffset[iconInstance] * (iconScale / 4f) * CameraManager.InverseView.Down;
				IconsGeometry.Instances[iconInstance].M11 = vector2.X + vector3.X;
				IconsGeometry.Instances[iconInstance].M12 = vector2.Y + vector3.Y;
				IconsGeometry.Instances[iconInstance].M13 = vector2.Z + vector3.Z;
			}
			IconsGeometry.InstancesDirty = true;
		}
		if (FocusNode != null && FocusNode.Group != null && !flag)
		{
			Vector3 vector4 = Vector3.Transform(Vector3.Zero, FocusNode.Group.WorldMatrix);
			Vector3 vector5 = CameraManager.Center - vector4;
			float num6 = vector5.Length();
			if (!FezMath.AlmostEqual(num6, 0f))
			{
				Vector3 vector6 = vector5 / num6;
				float num7 = MathHelper.Clamp(num6 * 2f, 0f, (!chosenByMouseClick) ? 1 : 3);
				CameraManager.Center -= vector6 * num7 * radius * 0.005f;
				CameraManager.Center += (vector4 - CameraManager.Center).Dot(forward) * forward;
			}
			if (blockViewPicking && (CameraManager.InterpolatedCenter - vector4).Length() < 0.5f)
			{
				blockViewPicking = false;
			}
		}
		ShadeNeighbourNodes();
		if (DotDialogueIndex >= DotDialogue.Length || DotManager.Hidden || GameState.SaveData.HasHadMapHelp)
		{
			return;
		}
		if (SpeechBubble.Hidden)
		{
			SpeechBubble.ChangeText(GameText.GetString(DotDialogue[DotDialogueIndex]));
		}
		else if (InputManager.CancelTalk == FezButtonState.Pressed)
		{
			sTextNext.Emit();
			DotDialogueIndex++;
			if (DotDialogueIndex == DotDialogue.Length)
			{
				DotManager.Burrow();
				GameState.SaveData.HasHadMapHelp = true;
			}
			SpeechBubble.Hide();
		}
	}

	private void ViewPicking()
	{
		CursorSelectable = false;
		Vector3 right = CameraManager.InverseView.Right;
		Vector3 up = CameraManager.InverseView.Up;
		Vector3 forward = CameraManager.InverseView.Forward;
		closestNodes.Clear();
		float minDepth = float.MaxValue;
		float num = float.MinValue;
		float minDist = float.MaxValue;
		float num2 = float.MinValue;
		Vector3 position = base.GraphicsDevice.Viewport.Unproject(new Vector3(MouseState.Position.X, MouseState.Position.Y, 0f), CameraManager.Projection, CameraManager.View, Matrix.Identity);
		Ray ray = new Ray(position, forward);
		Ray ray2 = new Ray(CameraManager.Position - forward * CameraManager.Radius, forward);
		foreach (Group group in NodesMesh.Groups)
		{
			float opacity = group.Material.Opacity;
			if (!(opacity < 0.01f))
			{
				NodeGroupData nodeGroupData = group.CustomData as NodeGroupData;
				float sizeFactor = nodeGroupData.Node.NodeType.GetSizeFactor();
				float num3 = (sizeFactor - 0.5f) / 2f + 1f;
				num3 *= MathHelper.Lerp(opacity, 1f, 0.5f);
				Vector3 vector = Vector3.Transform(Vector3.Zero, group.WorldMatrix);
				BoundingBox box = new BoundingBox(vector - new Vector3(num3), vector + new Vector3(num3));
				float num4 = (nodeGroupData.Depth = (vector - CameraManager.Position).Dot(forward));
				if (ray.Intersects(box).HasValue)
				{
					CursorSelectable = true;
				}
				if (!blockViewPicking && ray2.Intersects(box).HasValue)
				{
					minDepth = Math.Min(num4, minDepth);
					num = Math.Max(num4, num);
					Vector3 a = vector - CameraManager.Position;
					float num5 = new Vector2(a.Dot(right), a.Dot(up)).Length() * sizeFactor;
					minDist = Math.Min(num5, minDist);
					num2 = Math.Max(num5, num2);
					closestNodes.Add(new QualifiedNode
					{
						Node = nodeGroupData.Node,
						Depth = num4,
						ScreenDistance = num5,
						Transparency = FezMath.AlmostClamp(1f - opacity)
					});
				}
			}
		}
		if (blockViewPicking)
		{
			return;
		}
		if (closestNodes.Count > 0)
		{
			float depthRange = num - minDepth;
			float distRange = num2 - minDist;
			QualifiedNode qualifiedNode = closestNodes.OrderBy((QualifiedNode n) => n.Transparency * 2f + (n.ScreenDistance - minDist) / distRange + (n.Depth - minDepth) / depthRange / 2f).FirstOrDefault();
			MapNode focusNode = FocusNode;
			LastFocusedNode = (FocusNode = qualifiedNode.Node);
			if (FocusNode != null && FocusNode != focusNode)
			{
				sMagnet.Emit();
			}
			chosenByMouseClick = false;
		}
		else
		{
			FocusNode = null;
		}
		NodesMesh.Groups.Sort(NodeGroupDataComparer.Default);
	}

	private void MousePicking()
	{
		Vector3 right = CameraManager.InverseView.Right;
		Vector3 up = CameraManager.InverseView.Up;
		Vector3 forward = CameraManager.InverseView.Forward;
		closestNodes.Clear();
		float minDepth = float.MaxValue;
		float num = float.MinValue;
		float minDist = float.MaxValue;
		float num2 = float.MinValue;
		Vector3 position = base.GraphicsDevice.Viewport.Unproject(new Vector3(MouseState.Position.X, MouseState.Position.Y, 0f), CameraManager.Projection, CameraManager.View, Matrix.Identity);
		Ray ray = new Ray(position, forward);
		foreach (Group group in NodesMesh.Groups)
		{
			float opacity = group.Material.Opacity;
			if (!(opacity < 0.01f))
			{
				NodeGroupData nodeGroupData = group.CustomData as NodeGroupData;
				float sizeFactor = nodeGroupData.Node.NodeType.GetSizeFactor();
				float value = sizeFactor * 0.625f;
				Vector3 vector = Vector3.Transform(Vector3.Zero, group.WorldMatrix);
				BoundingBox box = new BoundingBox(vector - new Vector3(value), vector + new Vector3(value));
				float num3 = (nodeGroupData.Depth = (vector - CameraManager.Position).Dot(forward));
				if (ray.Intersects(box).HasValue)
				{
					minDepth = Math.Min(num3, minDepth);
					num = Math.Max(num3, num);
					Vector3 a = vector - CameraManager.Position;
					float num4 = new Vector2(a.Dot(right), a.Dot(up)).Length() * sizeFactor;
					minDist = Math.Min(num4, minDist);
					num2 = Math.Max(num4, num2);
					closestNodes.Add(new QualifiedNode
					{
						Node = nodeGroupData.Node,
						Depth = num3,
						ScreenDistance = num4,
						Transparency = FezMath.AlmostClamp(1f - opacity)
					});
				}
			}
		}
		if (closestNodes.Count > 0)
		{
			float depthRange = num - minDepth;
			float distRange = num2 - minDist;
			QualifiedNode qualifiedNode = closestNodes.OrderBy((QualifiedNode n) => n.Transparency * 2f + (n.ScreenDistance - minDist) / distRange + (n.Depth - minDepth) / depthRange / 2f).FirstOrDefault();
			MapNode focusNode = FocusNode;
			LastFocusedNode = (FocusNode = qualifiedNode.Node);
			if (FocusNode != null && FocusNode != focusNode)
			{
				sMagnet.Emit();
			}
			chosenByMouseClick = true;
			blockViewPicking = true;
		}
		NodesMesh.Groups.Sort(NodeGroupDataComparer.Default);
	}

	private float GetIconScale(float viewScale, float radius)
	{
		if (viewScale > 1f)
		{
			if (radius > 16f && radius <= 40f)
			{
				return (radius - 16f) / 24f * 0.25f + 1f;
			}
			if (radius > 40f)
			{
				return (radius - 40f) / 40f * 1.25f + 1.25f;
			}
			return 1f;
		}
		if (radius > 16f && radius <= 40f)
		{
			return (radius - 16f) / 24f * 1.5f + 1f;
		}
		if (radius > 40f)
		{
			return (radius - 40f) / 40f * 2.5f + 2.5f;
		}
		return 1f;
	}

	private void ScaleIcons()
	{
		float viewScale = base.GraphicsDevice.GetViewScale();
		float num = (float)base.GraphicsDevice.Viewport.Width / (1280f * viewScale);
		float radius = CameraManager.Radius / viewScale / num;
		float iconScale = GetIconScale(viewScale, radius);
		for (int i = 0; i < IconsGeometry.Instances.Length; i++)
		{
			IconsGeometry.Instances[i].M31 = IconsOriginalInstances[i].M31 * iconScale;
			IconsGeometry.Instances[i].M32 = IconsOriginalInstances[i].M32 * iconScale;
			IconsGeometry.Instances[i].M33 = IconsOriginalInstances[i].M33 * iconScale;
			IconsGeometry.InstancesDirty = true;
		}
	}

	private void ShadeNeighbourNodes()
	{
		int num = 0;
		MapNode item = FocusNode ?? LastFocusedNode;
		nextToCover.Clear();
		nextToCover.Add(item);
		toCover.Clear();
		hasCovered.Clear();
		hasCovered.Add(item);
		float num2 = 5f - (float)ZoomLevel;
		float num3 = 1f + (float)Math.Round((num2 > 3f) ? Math.Pow(num2 - 1.75f, 2.5) : ((double)num2));
		while (nextToCover.Count > 0)
		{
			toCover.Clear();
			toCover.AddRange(nextToCover);
			nextToCover.Clear();
			foreach (MapNode item2 in toCover)
			{
				Group group = item2.Group;
				if (group == null)
				{
					continue;
				}
				float value = ((item2 == CurrentNode) ? 1f : FezMath.Saturate((num3 - (float)num) / num3));
				group.Material.Opacity = MathHelper.Lerp(group.Material.Opacity, value, 0.2f);
				group.Enabled = group.Material.Opacity > 0.01f;
				NodeGroupData nodeGroupData = (NodeGroupData)group.CustomData;
				(nodeGroupData.Complete ? GoldenHighlightsGeometry : WhiteHighlightsGeometry).Instances[nodeGroupData.HighlightInstance].M24 = group.Material.Opacity;
				(nodeGroupData.Complete ? GoldenHighlightsGeometry : WhiteHighlightsGeometry).InstancesDirty = true;
				foreach (int iconInstance in nodeGroupData.IconInstances)
				{
					IconsGeometry.Instances[iconInstance].M24 = MathHelper.Lerp(IconsGeometry.Instances[iconInstance].M24, group.Material.Opacity, 0.2f);
				}
				IconsGeometry.InstancesDirty = true;
				float value2 = FezMath.Saturate((num3 - (float)((num != 0) ? (num + 1) : 0)) / num3);
				foreach (MapNode.Connection connection in item2.Connections)
				{
					if (hasCovered.Contains(connection.Node))
					{
						continue;
					}
					if (connection.LinkInstances != null)
					{
						foreach (int linkInstance in connection.LinkInstances)
						{
							LinksGeometry.Instances[linkInstance].M24 = MathHelper.Lerp(LinksGeometry.Instances[linkInstance].M24, value2, 0.2f);
						}
					}
					nextToCover.Add(connection.Node);
					hasCovered.Add(connection.Node);
				}
				LinksGeometry.InstancesDirty = true;
			}
			num++;
		}
	}

	private void Exit()
	{
		ScheduleExit = true;
		Resolved = false;
		TargetRenderingManager.ScheduleHook(base.DrawOrder, FadeOutRtHandle.Target);
		GameService.CloseScroll(null);
	}

	private void StartInTransition()
	{
		GameState.SkipRendering = true;
		CameraManager.PixelsPerTrixel = 3f;
		Starfield.Opacity = (LinksMesh.Material.Opacity = (NodesMesh.Material.Opacity = (LegendMesh.Material.Opacity = 0f)));
		CameraManager.ChangeViewpoint(Viewpoint.Front, 0f);
		Quaternion phi180 = OriginalRotation * Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI);
		Vector3 aoMaxPos = PlayerManager.Center + 6f * Vector3.UnitY;
		Waiters.Interpolate(0.75, delegate(float s)
		{
			if (base.Enabled)
			{
				float num = Easing.EaseOut(s, EasingType.Cubic);
				Starfield.Opacity = (LinksMesh.Material.Opacity = (NodesMesh.Material.Opacity = (LegendMesh.Material.Opacity = num)));
				Mesh iconsMesh = IconsMesh;
				Mesh linksMesh = LinksMesh;
				Vector3 vector2 = (NodesMesh.Scale = new Vector3(0.5f + num / 2f));
				Vector3 scale = (linksMesh.Scale = vector2);
				iconsMesh.Scale = scale;
				Mesh iconsMesh2 = IconsMesh;
				Mesh linksMesh2 = LinksMesh;
				vector2 = (NodesMesh.Position = PlayerManager.Center + num * 6f * Vector3.UnitY);
				scale = (linksMesh2.Position = vector2);
				iconsMesh2.Position = scale;
				Mesh iconsMesh3 = IconsMesh;
				Mesh linksMesh3 = LinksMesh;
				Quaternion quaternion2 = (NodesMesh.Rotation = Quaternion.Slerp(phi180, OriginalRotation, num));
				Quaternion rotation = (linksMesh3.Rotation = quaternion2);
				iconsMesh3.Rotation = rotation;
				Vector3 value = ((CurrentNode.Group == null) ? Vector3.Zero : CurrentNode.Group.Position);
				CameraManager.Center = Vector3.Lerp(OriginalCenter, aoMaxPos + Vector3.Transform(value, NodesMesh.Rotation), num);
			}
		}, delegate
		{
			FinishedInTransition = true;
		});
	}

	private void StartOutTransition()
	{
		GameState.SkipRendering = false;
		if (!GameState.SaveData.HasHadMapHelp)
		{
			DotManager.RevertDrawOrder();
			SpeechBubble.RevertDrawOrder();
			DotManager.Burrow();
			SpeechBubble.Hide();
		}
		sExit.Emit();
		CameraManager.PixelsPerTrixel = OriginalPixPerTrix;
		GameState.InMap = false;
		CameraManager.ChangeViewpoint(OriginalViewpoint, 0f);
		CameraManager.Center = OriginalCenter + 6f * Vector3.UnitY;
		CameraManager.Direction = OriginalDirection;
		CameraManager.SnapInterpolation();
		Waiters.Interpolate(0.75, delegate(float s)
		{
			float amount = 1f - Easing.EaseInOut(s, EasingType.Sine, EasingType.Quadratic);
			CameraManager.Center = Vector3.Lerp(OriginalCenter, OriginalCenter + 6f * Vector3.UnitY, amount);
			NodesMesh.Material.Opacity = 1f - Easing.EaseOut(s, EasingType.Quadratic);
		}, delegate
		{
			ServiceHelper.RemoveComponent(this);
		});
		base.Enabled = false;
	}

	public override void Draw(GameTime gameTime)
	{
		if (Instance == null)
		{
			return;
		}
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		if (GameState.StereoMode)
		{
			BaseEffect.EyeSign = Vector3.Zero;
		}
		if (TargetRenderingManager.IsHooked(FadeInRtHandle.Target) && !Resolved && !ScheduleExit)
		{
			TargetRenderingManager.Resolve(FadeInRtHandle.Target, reschedule: false);
			base.GraphicsDevice.Clear(Color.Black);
			base.GraphicsDevice.SetupViewport();
			Resolved = true;
			GameState.InMap = true;
			InputManager.StrictRotation = true;
			StartInTransition();
		}
		if (ScheduleExit && Resolved)
		{
			graphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
			graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
			TargetRenderingManager.DrawFullscreen(FadeOutRtHandle.Target, new Color(1f, 1f, 1f, NodesMesh.Material.Opacity));
			return;
		}
		if (!SpeechBubble.Hidden)
		{
			SpeechBubble.Origin = DotManager.Position - new Vector3(0f, 1f / CameraManager.Radius * 2f * base.GraphicsDevice.GetViewScale(), 0f);
		}
		if (ScheduleExit && !Resolved)
		{
			base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue);
		}
		float opacity = NodesMesh.Material.Opacity;
		foreach (Group group in ButtonsMesh.Groups)
		{
			group.Material.Opacity = opacity;
		}
		if (base.Enabled)
		{
			if (opacity < 1f)
			{
				base.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 1f, 0);
				graphicsDevice.SetBlendingMode(BlendingMode.Opaque);
				base.GraphicsDevice.SetupViewport();
				TargetRenderingManager.DrawFullscreen(FadeInRtHandle.Target);
			}
		}
		else
		{
			for (int i = 0; i < WavesMesh.Groups.Count; i++)
			{
				WavesMesh.Groups[i].Material.Opacity *= 0.9f;
			}
		}
		graphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
		TargetRenderingManager.DrawFullscreen(new Color(0f, 0f, 0f, opacity));
		base.GraphicsDevice.SetupViewport();
		Starfield.Draw();
		base.GraphicsDevice.Clear(ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Black, 1f, 0);
		NodesMesh.Draw();
		LinksMesh.Draw();
		base.GraphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.Trails);
		base.GraphicsDevice.SetBlendingMode(BlendingMode.Additive);
		TargetRenderingManager.DrawFullscreen(ShineTex, new Color(0.5f, 0.435f, 0.285f));
		base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
		base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
		if (CurrentNode.Group != null)
		{
			WavesMesh.Position = Vector3.Transform(Vector3.Zero, CurrentNode.Group.WorldMatrix) - CameraManager.Direction;
			WavesMesh.Rotation = CameraManager.Rotation;
		}
		WavesMesh.Draw();
		IconsMesh.Draw();
		ButtonsMesh.Draw();
		LegendMesh.Draw();
		if (Culture.IsCJK)
		{
			SpriteBatch.BeginLinear();
		}
		else
		{
			SpriteBatch.BeginPoint();
		}
		bool flag = base.GraphicsDevice.DisplayMode.Width < 1280;
		float viewScale = base.GraphicsDevice.GetViewScale();
		float num = (float)base.GraphicsDevice.Viewport.Width / (1280f * viewScale);
		float num2 = (float)base.GraphicsDevice.Viewport.Height / (720f * viewScale);
		SpriteFont spriteFont = (Culture.IsCJK ? FontManager.Small : FontManager.Big);
		float num3 = (Culture.IsCJK ? (FontManager.SmallFactor * 0.8f) : (flag ? 1.5f : 1f));
		num3 *= viewScale;
		string[] array = new string[5]
		{
			StaticText.GetString("MapPan"),
			StaticText.GetString("MapZoom"),
			StaticText.GetString("MapSpin"),
			StaticText.GetString("MapLook"),
			StaticText.GetString("MapBack")
		};
		float[] array2 = new float[5] { 48f, 48f, 41f, 37f, 45f };
		float num4 = (Culture.IsCJK ? (-15f) : (-25f)) * num2;
		float num5 = 0f;
		int num6 = 0;
		string[] array3 = array;
		foreach (string text in array3)
		{
			num5 = Math.Max(num5, spriteFont.MeasureString(text).X * num3);
			GTR.DrawStringLFLeftAlign(SpriteBatch, spriteFont, text, new Color(1f, 1f, 1f, opacity), new Vector2(1280f * num - 150f * num - (GamepadState.AnyConnected ? 0f : 30f), 30f + 100f * num2 + num4) * viewScale, num3);
			num4 += array2[num6++] * num2;
		}
		ButtonsMesh.Groups[0].Scale = new Vector3(num5 / 1280f * 40f / viewScale + (GamepadState.AnyConnected ? 3.125f : 4f), 0.975f, 1f);
		ButtonsMesh.Groups[0].Position = new Vector3(2.7f - ButtonsMesh.Groups[0].Scale.X, 0f, 0f);
		string[] obj = new string[7]
		{
			StaticText.GetString("MapLegendWarpGate"),
			StaticText.GetString("MapLegendSmallGate"),
			StaticText.GetString("MapLegendTreasure"),
			StaticText.GetString("MapLegendLockedDoor"),
			StaticText.GetString("MapLegendCube"),
			StaticText.GetString("MapLegendBits"),
			StaticText.GetString("MapLegendSecret")
		};
		num4 = (Culture.IsCJK ? 3 : 0);
		num5 = 0f;
		float num7 = 500f + num2 * 7f;
		float num8 = 24f;
		array3 = obj;
		foreach (string text2 in array3)
		{
			num5 = Math.Max(num5, spriteFont.MeasureString(text2).X * num3);
			GTR.DrawString(SpriteBatch, spriteFont, text2, new Vector2(64f * num + 40f * num2, (num7 + num4) * num2) * viewScale, new Color(1f, 1f, 1f, opacity), num3);
			num4 += num8;
		}
		LegendMesh.Groups[0].Scale = new Vector3(num5 / 1280f * 40f / viewScale + 1.75f, 1f, 1f);
		float num9 = (GamepadState.AnyConnected ? 2f : 1.5f) * num2;
		float num10 = (GamepadState.AnyConnected ? 170 : 180);
		num10 *= num;
		num10 -= FezMath.Max(num - 1f, 0f) * 20f;
		if (Culture.IsCJK)
		{
			SpriteBatch.End();
			SpriteBatch.BeginPoint();
		}
		Texture2D replacedGlyphTexture = GTR.GetReplacedGlyphTexture("{LS}");
		SpriteBatch.Draw(replacedGlyphTexture, new Vector2(1280f * num - num10 + (float)(64 - replacedGlyphTexture.Width) / 2f * 1.5f, 57f * num2) * viewScale, null, Color.White, 0f, Vector2.Zero, num9 * viewScale, SpriteEffects.None, 0f);
		replacedGlyphTexture = GTR.GetReplacedGlyphTexture("{RB}");
		SpriteBatch.Draw(replacedGlyphTexture, new Vector2(1280f * num - num10 + (float)(64 - replacedGlyphTexture.Width) / 2f * 1.5f, 107f * num2 - 6f * num9) * viewScale, null, Color.White, 0f, Vector2.Zero, num9 * viewScale, SpriteEffects.None, 0f);
		replacedGlyphTexture = GTR.GetReplacedGlyphTexture("{LB}");
		SpriteBatch.Draw(replacedGlyphTexture, new Vector2(1280f * num - num10 + (float)(64 - replacedGlyphTexture.Width) / 2f * 1.5f, 107f * num2 + 6f * num9) * viewScale, null, Color.White, 0f, Vector2.Zero, num9 * viewScale, SpriteEffects.None, 0f);
		replacedGlyphTexture = GTR.GetReplacedGlyphTexture("{LT}");
		SpriteBatch.Draw(replacedGlyphTexture, new Vector2(1280f * num - num10 + (float)(64 - replacedGlyphTexture.Width) / 2f * 1.5f + (GamepadState.AnyConnected ? (-5f * num9) : 0f), 155f * num2 + (GamepadState.AnyConnected ? 0f : (-6f * num9))) * viewScale, null, Color.White, 0f, Vector2.Zero, num9 * viewScale, SpriteEffects.None, 0f);
		replacedGlyphTexture = GTR.GetReplacedGlyphTexture("{RT}");
		SpriteBatch.Draw(replacedGlyphTexture, new Vector2(1280f * num - num10 + (float)(64 - replacedGlyphTexture.Width) / 2f * 1.5f + (GamepadState.AnyConnected ? (5f * num9) : 0f), 155f * num2 + (GamepadState.AnyConnected ? 0f : (6f * num9))) * viewScale, null, Color.White, 0f, Vector2.Zero, num9 * viewScale, SpriteEffects.None, 0f);
		replacedGlyphTexture = GTR.GetReplacedGlyphTexture("{RS}");
		SpriteBatch.Draw(replacedGlyphTexture, new Vector2(1280f * num - num10 + (float)(64 - replacedGlyphTexture.Width) / 2f * 1.5f, 195f * num2) * viewScale, null, Color.White, 0f, Vector2.Zero, num9 * viewScale, SpriteEffects.None, 0f);
		replacedGlyphTexture = GTR.GetReplacedGlyphTexture("{B}");
		SpriteBatch.Draw(replacedGlyphTexture, new Vector2(1280f * num - num10 + (float)(64 - replacedGlyphTexture.Width) / 2f * 1.5f, (float)(233 + (GamepadState.AnyConnected ? (-5) : 0)) * num2) * viewScale, null, Color.White, 0f, Vector2.Zero, num9 * viewScale, SpriteEffects.None, 0f);
		SpriteBatch.End();
		SpriteBatch.BeginPoint();
		float num11 = viewScale * 2f;
		Point point = MouseState.PositionInViewport();
		SpriteBatch.Draw((MouseState.LeftButton.State == MouseButtonStates.Dragging || MouseState.RightButton.State == MouseButtonStates.Dragging) ? GrabbedCursor : ((!CursorSelectable) ? PointerCursor : ((MouseState.LeftButton.State == MouseButtonStates.Down) ? ClickedCursor : CanClickCursor)), new Vector2((float)point.X - num11 * 11.5f, (float)point.Y - num11 * 8.5f), null, new Color(1f, 1f, 1f, FezMath.Saturate(1f - (SinceMouseMoved - 2f))), 0f, Vector2.Zero, num11, SpriteEffects.None, 0f);
		SpriteBatch.End();
		if (TargetRenderingManager.IsHooked(FadeOutRtHandle.Target) && !Resolved && ScheduleExit)
		{
			TargetRenderingManager.Resolve(FadeOutRtHandle.Target, reschedule: false);
			base.GraphicsDevice.Clear(Color.Black);
			base.GraphicsDevice.SetupViewport();
			TargetRenderingManager.DrawFullscreen(FadeOutRtHandle.Target);
			Resolved = true;
			base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
			StartOutTransition();
		}
	}

	private void UpdateControllerTexture(object sender, EventArgs e)
	{
		bool flag = GamepadState.Layout == GamepadState.GamepadLayout.Xbox360;
		if (sewerQRGroup != null)
		{
			if (flag)
			{
				sewerQRGroup.Texture = sewerQRXbox;
			}
			else
			{
				sewerQRGroup.Texture = sewerQRSony;
			}
		}
		if (zuhouseQRGroup != null)
		{
			if (flag)
			{
				zuhouseQRGroup.Texture = zuhouseQRXbox;
			}
			else
			{
				zuhouseQRGroup.Texture = zuhouseQRSony;
			}
		}
	}
}
