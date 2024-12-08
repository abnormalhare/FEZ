using System;
using System.Collections.Generic;
using System.Linq;
using Common;
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class MenuCube : DrawableGameComponent
{
	private Viewpoint OriginalViewpoint;

	private Vector3 OriginalCenter;

	private Quaternion OriginalRotation;

	private float OriginalPixPerTrix;

	private Vector3 OriginalDirection;

	private ArtObjectInstance AoInstance;

	private ArtObjectInstance ZoomedArtifact;

	private List<bool> AoVisibility;

	private Mesh GoldenCubes;

	private Mesh Maps;

	private Mesh AntiCubes;

	private Mesh HidingPlanes;

	private Mesh Highlights;

	private Mesh TomePages;

	private RenderTargetHandle InRtHandle;

	private RenderTargetHandle OutRtHandle;

	private FastBlurEffect BlurEffect;

	private bool Resolved;

	private bool ScheduleExit;

	private bool TomeZoom;

	private bool TomeOpen;

	private bool NumberZoom;

	private bool LetterZoom;

	private int TomePageIndex;

	private SoundEffect enterSound;

	private SoundEffect exitSound;

	private SoundEffect zoomInSound;

	private SoundEffect zoomOutSound;

	private SoundEffect rotateLeftSound;

	private SoundEffect rotateRightSound;

	private SoundEffect cursorSound;

	private SoundEffect sBackground;

	private SoundEmitter eBackground;

	private Texture2D oldTextureCache;

	private int Turns;

	private MenuCubeFace Face;

	private MenuCubeFace LastFace;

	private readonly Dictionary<MenuCubeFace, Vector2> HighlightPosition = new Dictionary<MenuCubeFace, Vector2>(MenuCubeFaceComparer.Default)
	{
		{
			MenuCubeFace.Maps,
			new Vector2(0f, 0f)
		},
		{
			MenuCubeFace.Artifacts,
			new Vector2(0f, 0f)
		}
	};

	private readonly List<ArtObjectInstance> ArtifactAOs = new List<ArtObjectInstance>();

	private ArtObjectInstance TomeCoverAo;

	private ArtObjectInstance TomeBackAo;

	private bool wasLowPass;

	public static MenuCube Instance;

	private static readonly CodeInput[] LetterCode = new CodeInput[8]
	{
		CodeInput.SpinLeft,
		CodeInput.SpinRight,
		CodeInput.SpinRight,
		CodeInput.SpinLeft,
		CodeInput.SpinRight,
		CodeInput.SpinLeft,
		CodeInput.SpinLeft,
		CodeInput.SpinLeft
	};

	private static readonly CodeInput[] NumberCode = new CodeInput[8]
	{
		CodeInput.SpinRight,
		CodeInput.SpinRight,
		CodeInput.SpinRight,
		CodeInput.SpinLeft,
		CodeInput.SpinRight,
		CodeInput.SpinRight,
		CodeInput.SpinLeft,
		CodeInput.SpinLeft
	};

	private bool letterCodeDone;

	private bool numberCodeDone;

	private readonly List<CodeInput> codeInputs = new List<CodeInput>();

	private bool zoomed;

	private bool zooming;

	private MenuCubeFace zoomedFace;

	private Vector3 originalObjectPosition;

	private readonly Vector3[] originalMapPositions = new Vector3[6];

	private IWaiter tomeOpenWaiter;

	private Group[] mystery2Groups = new Group[3];

	private Texture2D mystery2Xbox;

	private Texture2D mystery2Sony;

	[ServiceDependency]
	public ILevelService LevelService { private get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public IGameService GameService { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IInputManager InputManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IFontManager FontManager { private get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderingManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public MenuCube(Game game)
		: base(game)
	{
		base.UpdateOrder = -9;
		base.DrawOrder = 1000;
		Instance = this;
	}

	public override void Initialize()
	{
		base.Initialize();
		GameState.MenuCubeIsZoomed = false;
		PlayerManager.CanControl = false;
		ArtObject artObject = CMProvider.Global.Load<ArtObject>("Art Objects/MENU_CUBEAO");
		bool flag = true;
		if (LevelManager.WaterType == LiquidType.Sewer)
		{
			oldTextureCache = artObject.Cubemap;
			artObject.Cubemap = CMProvider.Global.Load<Texture2D>("Art Objects/MENU_CUBE_GB");
		}
		else if (LevelManager.WaterType == LiquidType.Lava)
		{
			oldTextureCache = artObject.Cubemap;
			artObject.Cubemap = CMProvider.Global.Load<Texture2D>("Art Objects/MENU_CUBE_VIRTUAL");
		}
		else if (LevelManager.BlinkingAlpha)
		{
			oldTextureCache = artObject.Cubemap;
			artObject.Cubemap = CMProvider.Global.Load<Texture2D>("Art Objects/MENU_CUBE_CMY");
		}
		else
		{
			flag = false;
		}
		if (flag)
		{
			new ArtObjectMaterializer(artObject).RecomputeTexCoords(widen: false);
		}
		int num = IdentifierPool.FirstAvailable(LevelManager.ArtObjects);
		AoInstance = new ArtObjectInstance(artObject)
		{
			Id = num,
			Position = PlayerManager.Center
		};
		AoInstance.Initialize();
		AoInstance.Material = new Material();
		LevelManager.ArtObjects.Add(num, AoInstance);
		AoInstance.Scale = new Vector3(0f);
		OriginalViewpoint = CameraManager.Viewpoint;
		OriginalCenter = CameraManager.Center;
		OriginalPixPerTrix = CameraManager.PixelsPerTrixel;
		OriginalRotation = FezMath.QuaternionFromPhi(CameraManager.Viewpoint.ToPhi());
		RenderTarget2D renderTarget = ((base.GraphicsDevice.GetRenderTargets().Length == 0) ? null : (base.GraphicsDevice.GetRenderTargets()[0].RenderTarget as RenderTarget2D));
		FillInPlanes();
		CreateGoldenCubeFace();
		CreateMapsFace();
		CreateArtifactsFace();
		CreateAntiCubeFace();
		CreateHighlights();
		CreateTomePages();
		base.GraphicsDevice.SetRenderTarget(renderTarget);
		Mesh antiCubes = AntiCubes;
		Mesh maps = Maps;
		Mesh hidingPlanes = HidingPlanes;
		Vector3 vector = (GoldenCubes.Position = AoInstance.Position);
		Vector3 vector3 = (hidingPlanes.Position = vector);
		Vector3 position2 = (maps.Position = vector3);
		antiCubes.Position = position2;
		Mesh antiCubes2 = AntiCubes;
		Mesh maps2 = Maps;
		Mesh hidingPlanes2 = HidingPlanes;
		Mesh goldenCubes = GoldenCubes;
		Vector3 vector6 = (AoInstance.Scale = new Vector3(0f));
		vector = (goldenCubes.Scale = vector6);
		vector3 = (hidingPlanes2.Scale = vector);
		position2 = (maps2.Scale = vector3);
		antiCubes2.Scale = position2;
		TransformArtifacts();
		BlurEffect = new FastBlurEffect();
		enterSound = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/EnterMenucubeOrMap");
		exitSound = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/ExitMenucubeOrMap");
		zoomInSound = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/ZoomIn");
		zoomOutSound = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/ZoomOut");
		rotateLeftSound = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/RotateLeft");
		rotateRightSound = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/RotateRight");
		cursorSound = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/MoveCursorMenucube");
		sBackground = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/MenuCubeBackground");
		eBackground = sBackground.Emit(loop: true);
		enterSound.Emit();
		AoVisibility = new List<bool>();
		AoInstance.Hidden = true;
		AoInstance.Visible = false;
		GameService.CloseScroll(null);
		GameState.ShowScroll(Face.GetTitle(), 0f, onTop: true);
		wasLowPass = SoundManager.IsLowPass;
		if (!wasLowPass)
		{
			SoundManager.FadeFrequencies(lowPass: true);
		}
		InRtHandle = TargetRenderingManager.TakeTarget();
		OutRtHandle = TargetRenderingManager.TakeTarget();
		TargetRenderingManager.ScheduleHook(base.DrawOrder, InRtHandle.Target);
	}

	private void CreateTomePages()
	{
		TomePages = new Mesh
		{
			Effect = new DefaultEffect.LitTextured(),
			Texture = CMProvider.Global.Load<Texture2D>("Other Textures/PAGES/tome_pages"),
			Blending = BlendingMode.Alphablending,
			SamplerState = SamplerState.PointClamp
		};
		Vector3 origin = new Vector3(0f, -0.875f, 0f);
		Vector3 size = new Vector3(0.875f, 0.875f, 0f);
		TomePages.AddFace(size, origin, FaceOrientation.Front, centeredOnOrigin: false);
		TomePages.AddFace(size, origin, FaceOrientation.Back, centeredOnOrigin: false);
		TomePages.CollapseWithNormalTexture<FezVertexPositionNormalTexture>();
		TomePages.Groups[0].Texture = CMProvider.Global.Load<Texture2D>("Other Textures/PAGES/blank");
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				TomePages.AddFace(size, origin, FaceOrientation.Front, centeredOnOrigin: false).TextureMatrix = new Matrix(0.25f, 0f, 0f, 0f, 0f, 0.25f, 0f, 0f, (float)i / 2f, (float)j / 4f, 1f, 0f, 0f, 0f, 0f, 0f);
				TomePages.AddFace(size, origin, FaceOrientation.Back, centeredOnOrigin: false).TextureMatrix = new Matrix(0.25f, 0f, 0f, 0f, 0f, 0.25f, 0f, 0f, ((float)i + 0.5f) / 2f, (float)j / 4f, 1f, 0f, 0f, 0f, 0f, 0f);
				TomePages.CollapseWithNormalTexture<FezVertexPositionNormalTexture>(TomePages.Groups.Count - 2, 2).Enabled = false;
			}
		}
	}

	private void FillInPlanes()
	{
		bool fullbright = true;
		Color color;
		if (LevelManager.WaterType == LiquidType.Sewer)
		{
			color = new Color(32, 70, 49);
		}
		else if (LevelManager.WaterType == LiquidType.Lava)
		{
			color = Color.Black;
		}
		else if (LevelManager.BlinkingAlpha)
		{
			color = Color.Black;
		}
		else
		{
			fullbright = false;
			color = new Color(56, 40, 95);
		}
		HidingPlanes = new Mesh
		{
			Effect = new DefaultEffect.LitVertexColored
			{
				Fullbright = fullbright
			},
			Material = AoInstance.Material,
			Blending = BlendingMode.Alphablending
		};
		Vector3 size = AoInstance.ArtObject.Size;
		int num = 0;
		foreach (MenuCubeFace value in Util.GetValues<MenuCubeFace>())
		{
			if (value == MenuCubeFace.AntiCubes && GameState.SaveData.SecretCubes <= 0)
			{
				continue;
			}
			Vector3 vector = value.GetForward().Abs();
			for (int i = 0; i < value.GetCount() && i < 32; i++)
			{
				int num2 = i;
				if (num2 >= 14)
				{
					num2 += 2;
				}
				if (num2 >= 20)
				{
					num2 += 2;
				}
				Vector3 vector2 = value.GetDepth() * value.GetForward() / 16f;
				Vector3 vector3 = value.GetSize() * (Vector3.One - vector) / 16f;
				Vector3 vector4 = -vector2 / 2f + vector3 * value.GetRight() / 2f;
				Vector3 vector5 = -vector2 / 2f + vector3 * -value.GetRight() / 2f;
				Vector3 vector6 = -vector2 / 2f + vector3 * Vector3.Up / 2f;
				Vector3 vector7 = -vector2 / 2f + vector3 * Vector3.Down / 2f;
				Vector3 vector8 = -value.GetForward() * value.GetDepth() / 32f;
				Vector3 vector9 = Vector3.Up * vector3 / 2f;
				Vector3 vector10 = -value.GetForward() * value.GetDepth() / 32f;
				Vector3 vector11 = Vector3.Up * vector3 / 2f;
				Vector3 vector12 = -value.GetRight() * vector3 / 2f;
				Vector3 vector13 = value.GetForward() * value.GetDepth() / 32f;
				Vector3 vector14 = -value.GetRight() * vector3 / 2f;
				Vector3 vector15 = value.GetForward() * value.GetDepth() / 32f;
				Group group = HidingPlanes.AddGroup();
				group.Geometry = new IndexedUserPrimitives<VertexPositionNormalColor>(new VertexPositionNormalColor[16]
				{
					new VertexPositionNormalColor(vector4 - vector8 + vector9, -value.GetRight(), color),
					new VertexPositionNormalColor(vector4 - vector8 - vector9, -value.GetRight(), color),
					new VertexPositionNormalColor(vector4 + vector8 + vector9, -value.GetRight(), color),
					new VertexPositionNormalColor(vector4 + vector8 - vector9, -value.GetRight(), color),
					new VertexPositionNormalColor(vector5 - vector10 + vector11, value.GetRight(), color),
					new VertexPositionNormalColor(vector5 - vector10 - vector11, value.GetRight(), color),
					new VertexPositionNormalColor(vector5 + vector10 + vector11, value.GetRight(), color),
					new VertexPositionNormalColor(vector5 + vector10 - vector11, value.GetRight(), color),
					new VertexPositionNormalColor(vector6 - vector12 + vector13, Vector3.Down, color),
					new VertexPositionNormalColor(vector6 - vector12 - vector13, Vector3.Down, color),
					new VertexPositionNormalColor(vector6 + vector12 + vector13, Vector3.Down, color),
					new VertexPositionNormalColor(vector6 + vector12 - vector13, Vector3.Down, color),
					new VertexPositionNormalColor(vector7 - vector14 + vector15, Vector3.Up, color),
					new VertexPositionNormalColor(vector7 - vector14 - vector15, Vector3.Up, color),
					new VertexPositionNormalColor(vector7 + vector14 + vector15, Vector3.Up, color),
					new VertexPositionNormalColor(vector7 + vector14 - vector15, Vector3.Up, color)
				}, new int[24]
				{
					0, 1, 2, 2, 1, 3, 4, 6, 5, 5,
					6, 7, 8, 9, 10, 10, 9, 11, 13, 12,
					14, 13, 14, 15
				}, PrimitiveType.TriangleList);
				int num3 = (int)Math.Sqrt(value.GetCount());
				int num4 = num2 % num3;
				int num5 = num2 / num3;
				Vector3 position = (float)(num4 * value.GetSpacing()) / 16f * value.GetRight() + (float)(num5 * value.GetSpacing()) / 16f * -Vector3.UnitY + size / 2f * (value.GetForward() + Vector3.Up - value.GetRight()) + value.GetForward() * -8f / 16f + (Vector3.Down + value.GetRight()) * value.GetOffset() / 16f;
				group.Position = position;
			}
			HidingPlanes.CollapseToBufferWithNormal<VertexPositionNormalColor>(num, HidingPlanes.Groups.Count - num).CustomData = value;
			num++;
		}
	}

	private void CreateGoldenCubeFace()
	{
		Vector3 size = AoInstance.ArtObject.Size;
		Trile trile = LevelManager.ActorTriles(ActorType.CubeShard).FirstOrDefault();
		bool flag = LevelManager.WaterType == LiquidType.Sewer || LevelManager.WaterType == LiquidType.Lava || LevelManager.BlinkingAlpha;
		GoldenCubes = new Mesh
		{
			Effect = (flag ? ((DefaultEffect)new DefaultEffect.Textured()) : ((DefaultEffect)new DefaultEffect.LitTextured())),
			Texture = LevelMaterializer.TrilesMesh.Texture,
			Blending = BlendingMode.Opaque,
			Material = AoInstance.Material
		};
		if (trile == null)
		{
			return;
		}
		ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4> geometry = trile.Geometry;
		int offset = MenuCubeFace.CubeShards.GetOffset();
		int spacing = MenuCubeFace.CubeShards.GetSpacing();
		for (int i = 0; i < GameState.SaveData.CubeShards; i++)
		{
			int num = i;
			if (num >= 14)
			{
				num += 2;
			}
			if (num >= 20)
			{
				num += 2;
			}
			Group group = GoldenCubes.AddGroup();
			group.Geometry = new IndexedUserPrimitives<VertexPositionNormalTextureInstance>(geometry.Vertices.ToArray(), geometry.Indices, geometry.PrimitiveType);
			int num2 = num % 6;
			int num3 = num / 6;
			group.Position = size / 2f * (Vector3.UnitZ + Vector3.UnitY - Vector3.UnitX) + (float)(offset + num2 * spacing) / 16f * Vector3.UnitX + (float)(offset + num3 * spacing) / 16f * -Vector3.UnitY + 0.5f * -Vector3.UnitZ;
			group.Scale = new Vector3(0.5f);
		}
		Group cubesGroup = GoldenCubes.CollapseToBufferWithNormal<VertexPositionNormalTextureInstance>();
		GoldenCubes.CustomRenderingHandler = delegate(Mesh m, BaseEffect e)
		{
			foreach (Group group3 in m.Groups)
			{
				(e as DefaultEffect).AlphaIsEmissive = group3 == cubesGroup;
				group3.Draw(e);
			}
		};
		Group group2 = GoldenCubes.AddFace(new Vector3(0.5f, 0.5f, 1f), Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true);
		group2.Position = size / 2f * (Vector3.UnitZ + Vector3.UnitY) + 1.3125f * -Vector3.UnitY + 0.1875f * -Vector3.UnitX + 0.499f * -Vector3.UnitZ;
		group2.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/hud/tiny_key");
		group2.Blending = BlendingMode.Alphablending;
		string text = GameState.SaveData.Keys.ToString();
		Vector2 vector = FontManager.Small.MeasureString(text);
		Vector3 size2 = new Vector3(vector / 16f / 2f - 0.0625f * Vector2.One, 1f);
		if (Culture.IsCJK)
		{
			size2 /= 3f;
		}
		group2 = GoldenCubes.AddFace(size2, Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true);
		group2.Position = size / 2f * (Vector3.UnitZ + Vector3.UnitY) + 1.25f * -Vector3.UnitY + 7f / 32f * Vector3.UnitX + 0.499f * -Vector3.UnitZ;
		group2.Blending = BlendingMode.Alphablending;
		if (Culture.IsCJK)
		{
			group2.SamplerState = SamplerState.AnisotropicClamp;
		}
		RenderTarget2D renderTarget2D = new RenderTarget2D(base.GraphicsDevice, (int)vector.X, (int)vector.Y, mipMap: false, base.GraphicsDevice.PresentationParameters.BackBufferFormat, base.GraphicsDevice.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);
		using (SpriteBatch spriteBatch = new SpriteBatch(base.GraphicsDevice))
		{
			base.GraphicsDevice.SetRenderTarget(renderTarget2D);
			base.GraphicsDevice.Clear(ClearOptions.Target, ColorEx.TransparentWhite, 1f, 0);
			spriteBatch.BeginPoint();
			float num4 = (Culture.IsCJK ? (FontManager.TopSpacing * 2f) : FontManager.TopSpacing);
			spriteBatch.DrawString(FontManager.Small, text, Vector2.Zero, Color.White, 0f, new Vector2(0f, (0f - num4) * 4f / 5f), 1f, SpriteEffects.None, 0f);
			spriteBatch.End();
			base.GraphicsDevice.SetRenderTarget(null);
			group2.Texture = renderTarget2D;
		}
		text = GameState.SaveData.CubeShards.ToString();
		int num5 = 2;
		vector = FontManager.Small.MeasureString(text) * num5;
		size2 = new Vector3(vector / 16f / 2f - 0.0625f * Vector2.One, 1f);
		if (Culture.IsCJK)
		{
			size2 /= 3.25f;
		}
		group2 = GoldenCubes.AddFace(size2, Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true);
		group2.Position = size / 2f * Vector3.UnitZ + 0.499f * -Vector3.UnitZ;
		group2.Blending = BlendingMode.Alphablending;
		if (Culture.IsCJK)
		{
			group2.SamplerState = SamplerState.AnisotropicClamp;
		}
		renderTarget2D = new RenderTarget2D(base.GraphicsDevice, (int)Math.Ceiling(vector.X), (int)Math.Ceiling(vector.Y), mipMap: false, base.GraphicsDevice.PresentationParameters.BackBufferFormat, base.GraphicsDevice.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);
		using SpriteBatch spriteBatch2 = new SpriteBatch(base.GraphicsDevice);
		base.GraphicsDevice.SetRenderTarget(renderTarget2D);
		base.GraphicsDevice.Clear(ClearOptions.Target, ColorEx.TransparentWhite, 1f, 0);
		spriteBatch2.BeginPoint();
		spriteBatch2.DrawString(FontManager.Small, text, Vector2.Zero, Color.White, 0f, new Vector2(0f, (0f - FontManager.TopSpacing) * 4f / 5f), num5, SpriteEffects.None, 0f);
		spriteBatch2.End();
		base.GraphicsDevice.SetRenderTarget(null);
		group2.Texture = renderTarget2D;
	}

	private void CreateMapsFace()
	{
		bool fullbright = LevelManager.WaterType == LiquidType.Sewer || LevelManager.WaterType == LiquidType.Lava || LevelManager.BlinkingAlpha;
		Maps = new Mesh
		{
			Effect = new DefaultEffect.LitTextured
			{
				Fullbright = fullbright
			},
			Blending = BlendingMode.Alphablending,
			Material = AoInstance.Material,
			SamplerState = SamplerState.PointClamp
		};
		Quaternion quaternion = Quaternion.CreateFromAxisAngle(Vector3.Up, -(float)Math.PI / 2f);
		int num = 0;
		foreach (string map in GameState.SaveData.Maps)
		{
			Texture2D texture = CMProvider.Global.Load<Texture2D>("Other Textures/maps/" + map + "_1");
			Texture2D texture2 = CMProvider.Global.Load<Texture2D>("Other Textures/maps/" + map + "_2");
			int num2 = (int)Math.Sqrt(MenuCubeFace.Maps.GetCount());
			Vector2 vector = new Vector2(num % num2, num / num2);
			Vector3 size = AoInstance.ArtObject.Size;
			Vector3 vector2 = vector.X * (float)MenuCubeFace.Maps.GetSpacing() / 16f * MenuCubeFace.Maps.GetRight() + vector.Y * (float)MenuCubeFace.Maps.GetSpacing() / 16f * -Vector3.UnitY + size / 2f * (MenuCubeFace.Maps.GetForward() + Vector3.Up - MenuCubeFace.Maps.GetRight()) + -MenuCubeFace.Maps.GetForward() * MenuCubeFace.Maps.GetDepth() / 16f / 2f + (Vector3.Down + MenuCubeFace.Maps.GetRight()) * MenuCubeFace.Maps.GetOffset() / 16f;
			Group group = Maps.AddGroup();
			group.Geometry = new IndexedUserPrimitives<FezVertexPositionNormalTexture>(new FezVertexPositionNormalTexture[4]
			{
				new FezVertexPositionNormalTexture(new Vector3(-1f, 0.5f, 0f), new Vector3(0f, 0f, -1.5f), new Vector2(1f, 0f)),
				new FezVertexPositionNormalTexture(new Vector3(-1f, -0.5f, 0f), new Vector3(0f, 0f, -1.5f), new Vector2(1f, 1f)),
				new FezVertexPositionNormalTexture(new Vector3(0f, 0.5f, 0f), new Vector3(0f, 0f, -1.5f), new Vector2(0.625f, 0f)),
				new FezVertexPositionNormalTexture(new Vector3(0f, -0.5f, 0f), new Vector3(0f, 0f, -1.5f), new Vector2(0.625f, 1f))
			}, new int[6] { 0, 1, 2, 2, 1, 3 }, PrimitiveType.TriangleList);
			group.Scale = new Vector3(0.375f, 1f, 1f) * 1.5f;
			group.Texture = texture;
			group.Position = vector2 + MenuCubeFace.Maps.GetRight() * 0.125f * 1.5f;
			group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 8f) * quaternion;
			group = Maps.CloneGroup(group);
			group.InvertNormals<FezVertexPositionNormalTexture>();
			group.Texture = texture2;
			group.CullMode = CullMode.CullClockwiseFace;
			group.TextureMatrix = new Matrix(-1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 1f, 0f, 1f, 0f, 0f, 0f, 0f, 0f);
			group = Maps.AddGroup();
			group.Geometry = new IndexedUserPrimitives<FezVertexPositionNormalTexture>(new FezVertexPositionNormalTexture[4]
			{
				new FezVertexPositionNormalTexture(new Vector3(-0.5f, 0.5f, 0f), new Vector3(0f, 0f, -1.5f), new Vector2(0.625f, 0f)),
				new FezVertexPositionNormalTexture(new Vector3(-0.5f, -0.5f, 0f), new Vector3(0f, 0f, -1.5f), new Vector2(0.625f, 1f)),
				new FezVertexPositionNormalTexture(new Vector3(0.5f, 0.5f, 0f), new Vector3(0f, 0f, -1.5f), new Vector2(0.375f, 0f)),
				new FezVertexPositionNormalTexture(new Vector3(0.5f, -0.5f, 0f), new Vector3(0f, 0f, -1.5f), new Vector2(0.375f, 1f))
			}, new int[6] { 0, 1, 2, 2, 1, 3 }, PrimitiveType.TriangleList);
			group.Scale = new Vector3(0.25f, 1f, 1f) * 1.5f;
			group.Texture = texture;
			group.Position = vector2;
			group.Rotation = quaternion;
			group = Maps.CloneGroup(group);
			group.InvertNormals<FezVertexPositionNormalTexture>();
			group.Texture = texture2;
			group.CullMode = CullMode.CullClockwiseFace;
			group.TextureMatrix = new Matrix(-1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 1f, 0f, 1f, 0f, 0f, 0f, 0f, 0f);
			group = Maps.AddGroup();
			group.Geometry = new IndexedUserPrimitives<FezVertexPositionNormalTexture>(new FezVertexPositionNormalTexture[4]
			{
				new FezVertexPositionNormalTexture(new Vector3(0f, 0.5f, 0f), new Vector3(0f, 0f, -1.5f), new Vector2(0.375f, 0f)),
				new FezVertexPositionNormalTexture(new Vector3(0f, -0.5f, 0f), new Vector3(0f, 0f, -1.5f), new Vector2(0.375f, 1f)),
				new FezVertexPositionNormalTexture(new Vector3(1f, 0.5f, 0f), new Vector3(0f, 0f, -1.5f), new Vector2(0f, 0f)),
				new FezVertexPositionNormalTexture(new Vector3(1f, -0.5f, 0f), new Vector3(0f, 0f, -1.5f), new Vector2(0f, 1f))
			}, new int[6] { 0, 1, 2, 2, 1, 3 }, PrimitiveType.TriangleList);
			group.Scale = new Vector3(0.375f, 1f, 1f) * 1.5f;
			group.Texture = texture;
			group.Position = vector2 - MenuCubeFace.Maps.GetRight() * 0.125f * 1.5f;
			group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 8f) * quaternion;
			group = Maps.CloneGroup(group);
			group.InvertNormals<FezVertexPositionNormalTexture>();
			group.Texture = texture2;
			group.CullMode = CullMode.CullClockwiseFace;
			group.TextureMatrix = new Matrix(-1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 1f, 0f, 1f, 0f, 0f, 0f, 0f, 0f);
			if (map == "MAP_MYSTERY")
			{
				mystery2Groups[0] = Maps.Groups[Maps.Groups.Count - 5];
				mystery2Groups[1] = Maps.Groups[Maps.Groups.Count - 3];
				mystery2Groups[2] = Maps.Groups[Maps.Groups.Count - 1];
				mystery2Xbox = texture2;
				mystery2Sony = CMProvider.Global.Load<Texture2D>("Other Textures/maps/MAP_MYSTERY_2_SONY");
				GamepadState.OnLayoutChanged = (EventHandler)Delegate.Combine(GamepadState.OnLayoutChanged, new EventHandler(UpdateControllerTexture));
				UpdateControllerTexture(null, null);
			}
			num++;
		}
	}

	private void CreateArtifactsFace()
	{
		foreach (ActorType artifact in GameState.SaveData.Artifacts)
		{
			if (artifact == ActorType.Tome)
			{
				ArtObject artObject = CMProvider.Global.Load<ArtObject>("Art Objects/TOME_BAO");
				int num = IdentifierPool.FirstAvailable(LevelManager.ArtObjects);
				TomeBackAo = new ArtObjectInstance(artObject)
				{
					Id = num,
					Position = PlayerManager.Center
				};
				TomeBackAo.Initialize();
				TomeBackAo.Material = AoInstance.Material;
				TomeBackAo.Hidden = true;
				LevelManager.ArtObjects.Add(num, TomeBackAo);
				ArtifactAOs.Add(TomeBackAo);
				artObject = CMProvider.Global.Load<ArtObject>("Art Objects/TOME_COVERAO");
				num = IdentifierPool.FirstAvailable(LevelManager.ArtObjects);
				TomeCoverAo = new ArtObjectInstance(artObject)
				{
					Id = num,
					Position = PlayerManager.Center
				};
				TomeCoverAo.Initialize();
				TomeCoverAo.Material = AoInstance.Material;
				TomeCoverAo.Hidden = true;
				LevelManager.ArtObjects.Add(num, TomeCoverAo);
				ArtifactAOs.Add(TomeCoverAo);
			}
			else
			{
				ArtObject artObject2 = CMProvider.Global.Load<ArtObject>("Art Objects/" + artifact.GetArtObjectName());
				int num2 = IdentifierPool.FirstAvailable(LevelManager.ArtObjects);
				ArtObjectInstance artObjectInstance = new ArtObjectInstance(artObject2)
				{
					Id = num2,
					Position = PlayerManager.Center
				};
				artObjectInstance.Initialize();
				artObjectInstance.Material = AoInstance.Material;
				artObjectInstance.Hidden = true;
				LevelManager.ArtObjects.Add(num2, artObjectInstance);
				ArtifactAOs.Add(artObjectInstance);
			}
		}
	}

	private void CreateAntiCubeFace()
	{
		Vector3 size = AoInstance.ArtObject.Size;
		Vector3 forward = MenuCubeFace.AntiCubes.GetForward();
		Vector3 right = MenuCubeFace.AntiCubes.GetRight();
		int offset = MenuCubeFace.AntiCubes.GetOffset();
		int spacing = MenuCubeFace.AntiCubes.GetSpacing();
		bool flag = LevelManager.WaterType == LiquidType.Sewer || LevelManager.WaterType == LiquidType.Lava || LevelManager.BlinkingAlpha;
		if (GameState.SaveData.SecretCubes == 0)
		{
			AntiCubes = new Mesh
			{
				Texture = CMProvider.Global.Load<Texture2D>("Other Textures/MENU_CUBE_COVER"),
				Blending = BlendingMode.Alphablending,
				Material = AoInstance.Material,
				SamplerState = SamplerState.PointClamp
			};
			if (LevelManager.WaterType == LiquidType.Sewer)
			{
				AntiCubes.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/MENU_CUBE_COVER_GB");
			}
			else if (LevelManager.WaterType == LiquidType.Lava)
			{
				AntiCubes.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/MENU_CUBE_COVER_VIRTUAL");
			}
			else if (LevelManager.BlinkingAlpha)
			{
				AntiCubes.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/MENU_CUBE_COVER_CMY");
			}
			else
			{
				AntiCubes.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/MENU_CUBE_COVER");
			}
			AntiCubes.Effect = (flag ? ((DefaultEffect)new DefaultEffect.Textured()) : ((DefaultEffect)new DefaultEffect.LitTextured()));
			AntiCubes.AddFace(size, size * forward / 2f, FezMath.OrientationFromDirection(forward), centeredOnOrigin: true);
			return;
		}
		Trile trile = LevelManager.ActorTriles(ActorType.SecretCube).FirstOrDefault();
		if (trile == null)
		{
			AntiCubes = new Mesh
			{
				Effect = new DefaultEffect.LitTextured(),
				Texture = LevelMaterializer.TrilesMesh.Texture,
				Blending = BlendingMode.Alphablending,
				Material = AoInstance.Material
			};
			Logger.Log("MenuCube", "No anti-cube trile in " + LevelManager.TrileSet.Name);
		}
		else
		{
			ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4> geometry = trile.Geometry;
			AntiCubes = new Mesh
			{
				Effect = (flag ? ((DefaultEffect)new DefaultEffect.Textured()) : ((DefaultEffect)new DefaultEffect.LitTextured())),
				Texture = LevelMaterializer.TrilesMesh.Texture,
				Blending = BlendingMode.Opaque,
				Material = AoInstance.Material
			};
			for (int i = 0; i < GameState.SaveData.SecretCubes; i++)
			{
				int num = i;
				if (num >= 14)
				{
					num += 2;
				}
				if (num >= 20)
				{
					num += 2;
				}
				Group group = AntiCubes.AddGroup();
				group.Geometry = new IndexedUserPrimitives<VertexPositionNormalTextureInstance>(geometry.Vertices.ToArray(), geometry.Indices, geometry.PrimitiveType);
				int num2 = num % 6;
				int num3 = num / 6;
				group.Position = size / 2f * (forward + Vector3.UnitY - right) + (float)(offset + num2 * spacing) / 16f * right + (float)(offset + num3 * spacing) / 16f * -Vector3.UnitY + 0.5f * -forward;
				group.Scale = new Vector3(0.5f);
				group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 2f * (float)RandomHelper.Random.Next(0, 4));
			}
			Group cubesGroup = AntiCubes.CollapseToBufferWithNormal<VertexPositionNormalTextureInstance>();
			AntiCubes.CustomRenderingHandler = delegate(Mesh m, BaseEffect e)
			{
				foreach (Group group3 in m.Groups)
				{
					(e as DefaultEffect).AlphaIsEmissive = group3 == cubesGroup;
					group3.Draw(e);
				}
			};
		}
		string text = GameState.SaveData.SecretCubes.ToString();
		int num4 = 2;
		Vector2 vector = FontManager.Small.MeasureString(text) * num4;
		Vector2 vector2 = vector / 16f / 2f - 0.0625f * Vector2.One;
		if (Culture.IsCJK)
		{
			vector2 /= 3.25f;
		}
		Group group2 = GoldenCubes.AddFace(new Vector3(1f, vector2.Y, vector2.X), Vector3.Zero, FezMath.OrientationFromDirection(forward), centeredOnOrigin: true);
		group2.Position = size / 2f * forward + 0.499f * -forward;
		group2.Blending = BlendingMode.Alphablending;
		if (Culture.IsCJK)
		{
			group2.SamplerState = SamplerState.AnisotropicClamp;
		}
		RenderTarget2D renderTarget2D = new RenderTarget2D(base.GraphicsDevice, (int)Math.Ceiling(vector.X), (int)Math.Ceiling(vector.Y), mipMap: false, base.GraphicsDevice.PresentationParameters.BackBufferFormat, base.GraphicsDevice.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);
		using SpriteBatch spriteBatch = new SpriteBatch(base.GraphicsDevice);
		base.GraphicsDevice.SetRenderTarget(renderTarget2D);
		base.GraphicsDevice.Clear(ClearOptions.Target, ColorEx.TransparentWhite, 1f, 0);
		spriteBatch.BeginPoint();
		spriteBatch.DrawString(FontManager.Small, text, Vector2.Zero, Color.White, 0f, new Vector2(0f, (0f - FontManager.TopSpacing) * 4f / 5f), num4, SpriteEffects.None, 0f);
		spriteBatch.End();
		base.GraphicsDevice.SetRenderTarget(null);
		group2.Texture = renderTarget2D;
	}

	private void CreateHighlights()
	{
		Highlights = new Mesh
		{
			Effect = new DefaultEffect.VertexColored(),
			Material = AoInstance.Material,
			Blending = BlendingMode.Alphablending
		};
		Color color = (LevelManager.BlinkingAlpha ? Color.Yellow : Color.White);
		for (int i = 0; i < 4; i++)
		{
			Highlights.AddGroup();
		}
		CreateFaceHighlights(MenuCubeFace.Maps, color);
		CreateFaceHighlights(MenuCubeFace.Artifacts, color);
	}

	private void CreateFaceHighlights(MenuCubeFace cf, Color color)
	{
		Vector3 vector = color.ToVector3();
		Vector3 size = AoInstance.ArtObject.Size;
		for (int i = 0; i < 4; i++)
		{
			Highlights.AddWireframeFace(new Vector3((float)cf.GetSize() * 1.25f / 16f) * ((Vector3.UnitY + cf.GetRight().Abs()) * (0.95f - (float)i * 0.05f)), Vector3.Zero, FezMath.OrientationFromDirection(cf.GetForward()), new Color(vector.X, vector.Y, vector.Z, 1f - (float)i / 4f), centeredOnOrigin: true).Position = size / 2f * (cf.GetForward() + Vector3.Up - cf.GetRight()) + cf.GetForward() * -0.4375f + (Vector3.Down + cf.GetRight()) * cf.GetOffset() / 16f;
		}
	}

	private void StartInTransition()
	{
		GameState.SkipRendering = true;
		LevelManager.SkipInvalidation = true;
		float num = (float)base.GraphicsDevice.Viewport.Width / (1280f * base.GraphicsDevice.GetViewScale());
		base.GraphicsDevice.SetupViewport();
		CameraManager.Radius = 26.25f * num;
		CameraManager.ChangeViewpoint(Viewpoint.Perspective, 1.5f);
		GameState.SkyOpacity = 0f;
		Quaternion phi180 = OriginalRotation * Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI);
		Vector3 aoMaxPos = PlayerManager.Center + (AoInstance.ArtObject.Size * Vector3.UnitY / 2f + Vector3.UnitY);
		BlurEffect.BlurWidth = 0f;
		Waiters.Interpolate(0.75, delegate(float s)
		{
			if (base.Enabled)
			{
				float num2 = Easing.EaseOut(s, EasingType.Cubic);
				AoInstance.Material.Opacity = num2;
				AoInstance.MarkDirty();
				Mesh antiCubes = AntiCubes;
				Mesh maps = Maps;
				Mesh hidingPlanes = HidingPlanes;
				Mesh highlights = Highlights;
				Mesh goldenCubes = GoldenCubes;
				Vector3 vector2 = (AoInstance.Scale = new Vector3(num2));
				Vector3 vector4 = (goldenCubes.Scale = vector2);
				Vector3 vector6 = (highlights.Scale = vector4);
				Vector3 vector8 = (hidingPlanes.Scale = vector6);
				Vector3 scale = (maps.Scale = vector8);
				antiCubes.Scale = scale;
				Mesh goldenCubes2 = GoldenCubes;
				Mesh antiCubes2 = AntiCubes;
				Mesh maps2 = Maps;
				Mesh hidingPlanes2 = HidingPlanes;
				Mesh highlights2 = Highlights;
				vector2 = (AoInstance.Position = PlayerManager.Center + num2 * (AoInstance.ArtObject.Size * Vector3.UnitY / 2f + Vector3.UnitY));
				vector4 = (highlights2.Position = vector2);
				vector6 = (hidingPlanes2.Position = vector4);
				vector8 = (maps2.Position = vector6);
				scale = (antiCubes2.Position = vector8);
				goldenCubes2.Position = scale;
				Mesh antiCubes3 = AntiCubes;
				Mesh maps3 = Maps;
				Mesh hidingPlanes3 = HidingPlanes;
				Mesh highlights3 = Highlights;
				Mesh goldenCubes3 = GoldenCubes;
				Quaternion quaternion2 = (AoInstance.Rotation = Quaternion.Slerp(phi180, OriginalRotation, num2));
				Quaternion quaternion4 = (goldenCubes3.Rotation = quaternion2);
				Quaternion quaternion6 = (highlights3.Rotation = quaternion4);
				Quaternion quaternion8 = (hidingPlanes3.Rotation = quaternion6);
				Quaternion rotation = (maps3.Rotation = quaternion8);
				antiCubes3.Rotation = rotation;
				CameraManager.Center = Vector3.Lerp(PlayerManager.Center, aoMaxPos, num2);
				CameraManager.SnapInterpolation();
				BlurEffect.BlurWidth = num2;
				TransformArtifacts();
			}
		}, delegate
		{
			BlurEffect.BlurWidth = 1f;
		});
	}

	private void StartOutTransition()
	{
		CameraManager.PixelsPerTrixel = OriginalPixPerTrix;
		CameraManager.Center = OriginalCenter;
		CameraManager.ChangeViewpoint((OriginalViewpoint == Viewpoint.None) ? CameraManager.LastViewpoint : OriginalViewpoint, 0f);
		CameraManager.SnapInterpolation();
		GameState.SkipRendering = false;
		LevelManager.SkipInvalidation = false;
		GameState.SkyOpacity = 1f;
		foreach (ArtObjectInstance artifactAO in ArtifactAOs)
		{
			artifactAO.SoftDispose();
			LevelManager.ArtObjects.Remove(artifactAO.Id);
		}
		exitSound.Emit();
		GameService.CloseScroll(null);
		GameState.InMenuCube = false;
		GameState.DisallowRotation = false;
		Waiters.Interpolate(0.5, delegate(float s)
		{
			float num = 1f - Easing.EaseOut(s, EasingType.Cubic);
			AoInstance.Material.Opacity = num;
			BlurEffect.BlurWidth = num;
		}, delegate
		{
			ServiceHelper.RemoveComponent(this);
		});
	}

	protected override void Dispose(bool disposing)
	{
		if (oldTextureCache != null)
		{
			AoInstance.ArtObject.Cubemap = oldTextureCache;
			new ArtObjectMaterializer(AoInstance.ArtObject).RecomputeTexCoords(widen: true);
		}
		LevelManager.ArtObjects.Remove(AoInstance.Id);
		AoInstance.SoftDispose();
		GameState.SkyOpacity = 1f;
		PlayerManager.CanControl = true;
		if (InRtHandle != null)
		{
			TargetRenderingManager.UnscheduleHook(InRtHandle.Target);
			TargetRenderingManager.ReturnTarget(InRtHandle);
		}
		InRtHandle = null;
		if (OutRtHandle != null)
		{
			TargetRenderingManager.UnscheduleHook(OutRtHandle.Target);
			TargetRenderingManager.ReturnTarget(OutRtHandle);
		}
		OutRtHandle = null;
		HidingPlanes.Dispose();
		GoldenCubes.Dispose();
		AntiCubes.Dispose();
		TomePages.Dispose();
		Maps.Dispose();
		Highlights.Dispose();
		BlurEffect.Dispose();
		if (eBackground != null && !eBackground.Dead)
		{
			eBackground.FadeOutAndDie(0.25f, autoPause: false);
			eBackground = null;
		}
		GameService.CloseScroll(null);
		if (!wasLowPass)
		{
			SoundManager.FadeFrequencies(lowPass: false);
		}
		Instance = null;
		base.Dispose(disposing);
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused || GameState.Loading || GameState.InMap)
		{
			return;
		}
		if (!GameState.InMenuCube)
		{
			ServiceHelper.RemoveComponent(this);
			return;
		}
		if (!GameState.MenuCubeIsZoomed && !CameraManager.ProjectionTransition && (InputManager.Back == FezButtonState.Pressed || InputManager.CancelTalk == FezButtonState.Pressed || InputManager.OpenInventory == FezButtonState.Pressed))
		{
			ScheduleExit = true;
			base.Enabled = false;
			Resolved = false;
			TargetRenderingManager.ScheduleHook(base.DrawOrder, OutRtHandle.Target);
			return;
		}
		bool flag = CameraManager.Viewpoint.IsOrthographic();
		bool menuCubeIsZoomed = GameState.MenuCubeIsZoomed;
		if (InputManager.RotateRight == FezButtonState.Pressed)
		{
			if (TomeOpen)
			{
				if (TomePageIndex > 0)
				{
					if (TomePageIndex != 0)
					{
						TomePageIndex--;
					}
					int tpi = TomePageIndex;
					Waiters.Interpolate(0.25, delegate(float s)
					{
						s = Easing.EaseOut(FezMath.Saturate(1f - s), EasingType.Quadratic);
						TomePages.Groups[tpi].Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)Math.PI * -3f / 4f * s * (1f - (float)tpi / 8f * 0.05f));
					}, delegate
					{
						TomePages.Groups[tpi].Rotation = Quaternion.Identity;
						if (tpi != 8)
						{
							TomePages.Groups[tpi + 1].Enabled = false;
						}
					});
				}
			}
			else
			{
				rotateRightSound.Emit();
				if (!flag && !menuCubeIsZoomed)
				{
					LastFace = Face;
					Face++;
					if (Face > MenuCubeFace.AntiCubes)
					{
						Face = MenuCubeFace.CubeShards;
					}
					Turns++;
					GameService.CloseScroll(null);
					if (Face != MenuCubeFace.AntiCubes || GameState.SaveData.SecretCubes > 0)
					{
						GameState.ShowScroll(Face.GetTitle(), 0f, onTop: true);
					}
					foreach (Group group in HidingPlanes.Groups)
					{
						MenuCubeFace menuCubeFace = (MenuCubeFace)group.CustomData;
						group.Enabled = menuCubeFace == Face || menuCubeFace == LastFace;
					}
				}
				if (CameraManager.Viewpoint.IsOrthographic() && !menuCubeIsZoomed)
				{
					OriginalRotation *= Quaternion.CreateFromAxisAngle(Vector3.Up, -(float)Math.PI / 2f);
				}
			}
		}
		else if (InputManager.RotateLeft == FezButtonState.Pressed)
		{
			if (TomeOpen)
			{
				if (TomePageIndex <= 8)
				{
					int tpi2 = TomePageIndex;
					if (TomePageIndex <= 8)
					{
						TomePageIndex++;
					}
					Waiters.Interpolate(0.25, delegate(float s)
					{
						s = Easing.EaseOut(FezMath.Saturate(s), EasingType.Quadratic);
						if (tpi2 != 8)
						{
							TomePages.Groups[tpi2 + 1].Enabled = true;
						}
						TomePages.Groups[tpi2].Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)Math.PI * -3f / 4f * s * (1f - (float)tpi2 / 8f * 0.05f));
					}, delegate
					{
						TomePages.Groups[tpi2].Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)Math.PI * -3f / 4f * (1f - (float)tpi2 / 8f * 0.05f));
					});
				}
			}
			else
			{
				rotateLeftSound.Emit();
				if (!flag && !menuCubeIsZoomed)
				{
					LastFace = Face;
					Face--;
					if (Face < MenuCubeFace.CubeShards)
					{
						Face = MenuCubeFace.AntiCubes;
					}
					Turns--;
					GameService.CloseScroll(null);
					if (Face != MenuCubeFace.AntiCubes || GameState.SaveData.SecretCubes > 0)
					{
						GameState.ShowScroll(Face.GetTitle(), 0f, onTop: true);
					}
					foreach (Group group2 in HidingPlanes.Groups)
					{
						MenuCubeFace menuCubeFace2 = (MenuCubeFace)group2.CustomData;
						group2.Enabled = menuCubeFace2 == Face || menuCubeFace2 == LastFace;
					}
				}
				if (CameraManager.Viewpoint.IsOrthographic() && !menuCubeIsZoomed)
				{
					OriginalRotation *= Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 2f);
				}
			}
		}
		UpdateHighlights((float)gameTime.TotalGameTime.TotalSeconds);
		Quaternion quaternion = Quaternion.Slerp(GoldenCubes.Rotation, OriginalRotation, 0.0875f);
		Mesh antiCubes = AntiCubes;
		Mesh maps = Maps;
		Mesh hidingPlanes = HidingPlanes;
		Mesh highlights = Highlights;
		ArtObjectInstance aoInstance = AoInstance;
		Quaternion quaternion3 = (GoldenCubes.Rotation = quaternion);
		Quaternion quaternion5 = (aoInstance.Rotation = quaternion3);
		Quaternion quaternion7 = (highlights.Rotation = quaternion5);
		Quaternion quaternion9 = (hidingPlanes.Rotation = quaternion7);
		Quaternion rotation = (maps.Rotation = quaternion9);
		antiCubes.Rotation = rotation;
		Mesh antiCubes2 = AntiCubes;
		Mesh maps2 = Maps;
		Mesh hidingPlanes2 = HidingPlanes;
		Vector3 vector = (GoldenCubes.Position = AoInstance.Position);
		Vector3 vector3 = (hidingPlanes2.Position = vector);
		Vector3 position2 = (maps2.Position = vector3);
		antiCubes2.Position = position2;
		TransformArtifacts();
		HandleSelection();
		TestForTempleOfLove();
	}

	private void TestForTempleOfLove()
	{
		if (LevelManager.Name != "TEMPLE_OF_LOVE" || GameState.SaveData.PiecesOfHeart < 3 || GameState.SaveData.HasDoneHeartReboot || Face != MenuCubeFace.Artifacts || !GameState.MenuCubeIsZoomed || (!LetterZoom && !NumberZoom) || (letterCodeDone && numberCodeDone))
		{
			return;
		}
		CodeInput codeInput = CodeInput.None;
		if (InputManager.Jump == FezButtonState.Pressed)
		{
			codeInput = CodeInput.Jump;
		}
		else if (InputManager.RotateRight == FezButtonState.Pressed)
		{
			codeInput = CodeInput.SpinRight;
		}
		else if (InputManager.RotateLeft == FezButtonState.Pressed)
		{
			codeInput = CodeInput.SpinLeft;
		}
		else if (InputManager.Left == FezButtonState.Pressed)
		{
			codeInput = CodeInput.Left;
		}
		else if (InputManager.Right == FezButtonState.Pressed)
		{
			codeInput = CodeInput.Right;
		}
		else if (InputManager.Up == FezButtonState.Pressed)
		{
			codeInput = CodeInput.Up;
		}
		else if (InputManager.Down == FezButtonState.Pressed)
		{
			codeInput = CodeInput.Down;
		}
		if (codeInput == CodeInput.None)
		{
			return;
		}
		codeInputs.Add(codeInput);
		if (codeInputs.Count > 8)
		{
			codeInputs.RemoveAt(0);
		}
		if (!letterCodeDone && LetterZoom)
		{
			letterCodeDone = PatternTester.Test(codeInputs, LetterCode);
		}
		if (!numberCodeDone && NumberZoom)
		{
			numberCodeDone = PatternTester.Test(codeInputs, NumberCode);
		}
		if (letterCodeDone && numberCodeDone)
		{
			GameState.SaveData.HasDoneHeartReboot = true;
			LevelService.ResolvePuzzleSoundOnly();
			zooming = true;
			zoomed = false;
			DoArtifactZoom(ZoomedArtifact);
			Waiters.Wait(() => !GameState.MenuCubeIsZoomed, delegate
			{
				ScheduleExit = true;
				base.Enabled = false;
				Resolved = false;
				TargetRenderingManager.ScheduleHook(base.DrawOrder, OutRtHandle.Target);
			});
		}
	}

	private void UpdateHighlights(float elapsedSeconds)
	{
		if (!GameState.MenuCubeIsZoomed && Face != 0 && Face != MenuCubeFace.AntiCubes)
		{
			MenuCubeFace face = Face;
			int num = (int)Math.Sqrt(face.GetCount());
			if (InputManager.Right == FezButtonState.Pressed && HighlightPosition[Face].X + 1f < (float)num)
			{
				MoveAndRotate(face, Vector2.UnitX);
			}
			if (InputManager.Left == FezButtonState.Pressed && HighlightPosition[Face].X - 1f >= 0f)
			{
				MoveAndRotate(face, -Vector2.UnitX);
			}
			if (InputManager.Up == FezButtonState.Pressed && HighlightPosition[Face].Y - 1f >= 0f)
			{
				MoveAndRotate(face, -Vector2.UnitY);
			}
			if (InputManager.Down == FezButtonState.Pressed && HighlightPosition[Face].Y + 1f < (float)num)
			{
				MoveAndRotate(face, Vector2.UnitY);
			}
			int face2 = (int)Face;
			for (int i = 0; i < 4; i++)
			{
				int index = face2 * 4 + i;
				Highlights.Groups[index].Scale = ((float)Math.Sin(elapsedSeconds * 5f) * 0.1f * (1f / (float)(i + 1)) + 1f) * (Vector3.UnitY + Face.GetRight().Abs()) + Face.GetForward().Abs();
			}
		}
	}

	private void MoveAndRotate(MenuCubeFace cf, Vector2 diff)
	{
		Vector2 op = HighlightPosition[cf];
		HighlightPosition[cf] = (HighlightPosition[cf] + diff).Round();
		int sgn = Math.Sign(Vector2.Dot(diff, Vector2.One));
		Vector3 axis = ((diff.X != 0f) ? Vector3.Up : cf.GetRight());
		Vector3 scale = AoInstance.ArtObject.Size;
		cursorSound.Emit();
		Waiters.Interpolate(0.15, delegate(float s)
		{
			for (int i = 0; i < 4; i++)
			{
				Group group = Highlights.Groups[(int)cf * 4 + i];
				Vector2 vector = HighlightPosition[cf] - diff;
				if (op != vector)
				{
					break;
				}
				s = Easing.EaseOut(FezMath.Saturate(s), EasingType.Sine);
				group.Position = (vector.X + diff.X * s) * (float)cf.GetSpacing() / 16f * cf.GetRight() + (vector.Y + diff.Y * s) * (float)cf.GetSpacing() / 16f * -Vector3.UnitY + scale / 2f * (cf.GetForward() + Vector3.Up - cf.GetRight()) + cf.GetForward() * (-0.4375f + (float)cf.GetSize() / 2f / 16f * (float)Math.Sin(s * (float)Math.PI)) + (Vector3.Down + cf.GetRight()) * cf.GetOffset() / 16f;
				group.Rotation = Quaternion.CreateFromAxisAngle(axis, s * (float)Math.PI * (float)sgn);
			}
		}, delegate
		{
			for (int j = 0; j < 4; j++)
			{
				Highlights.Groups[(int)cf * 4 + j].Rotation = Quaternion.Identity;
			}
		});
	}

	private void TransformArtifacts()
	{
		if (GameState.MenuCubeIsZoomed)
		{
			return;
		}
		int num = 0;
		foreach (ArtObjectInstance artifactAO in ArtifactAOs)
		{
			int num2 = (int)Math.Sqrt(MenuCubeFace.Artifacts.GetCount());
			Vector2 vector = new Vector2(num % num2, num / num2);
			Vector3 size = AoInstance.ArtObject.Size;
			Vector2 artifactOffset = artifactAO.ArtObject.ActorType.GetArtifactOffset();
			Vector3 value = (vector.X * (float)MenuCubeFace.Artifacts.GetSpacing() - artifactOffset.X) / 16f * MenuCubeFace.Artifacts.GetRight() + (vector.Y * (float)MenuCubeFace.Artifacts.GetSpacing() - artifactOffset.Y) / 16f * -Vector3.UnitY + size / 2f * (MenuCubeFace.Artifacts.GetForward() + Vector3.Up - MenuCubeFace.Artifacts.GetRight()) + -MenuCubeFace.Artifacts.GetForward() * MenuCubeFace.Artifacts.GetDepth() / 16f * 1.25f + (Vector3.Down + MenuCubeFace.Artifacts.GetRight()) * MenuCubeFace.Artifacts.GetOffset() / 16f;
			artifactAO.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI) * AoInstance.Rotation;
			artifactAO.Position = AoInstance.Position + Vector3.Transform(Vector3.Transform(value, AoInstance.Rotation), Matrix.CreateScale(AoInstance.Scale));
			artifactAO.Scale = AoInstance.Scale;
			if (artifactAO.ArtObjectName != "TOME_BAO")
			{
				num++;
			}
		}
	}

	private void HandleSelection()
	{
		float num = (float)base.GraphicsDevice.Viewport.Width / (1280f * base.GraphicsDevice.GetViewScale());
		float OriginalRadius = 17.82f * num;
		if ((!zooming && Face == MenuCubeFace.CubeShards) || Face == MenuCubeFace.AntiCubes)
		{
			return;
		}
		if ((TomeZoom && InputManager.GrabThrow == FezButtonState.Pressed) || (TomeOpen && InputManager.CancelTalk == FezButtonState.Pressed))
		{
			TomeOpen = !TomeOpen;
			if (TomeOpen)
			{
				CameraManager.OriginalDirection = OriginalDirection;
				GameState.DisallowRotation = true;
			}
			else
			{
				GameState.DisallowRotation = false;
				for (int num2 = TomePageIndex - 1; num2 >= 0; num2--)
				{
					int i1 = num2;
					Waiters.Interpolate(0.25f + (float)(8 - num2) / 8f * 0.15f, delegate(float s)
					{
						s = Easing.EaseOut(FezMath.Saturate(1f - s), EasingType.Quadratic);
						TomePages.Groups[i1].Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)Math.PI * -3f / 4f * s * (1f - (float)i1 / 8f * 0.05f));
					}, delegate
					{
						TomePages.Groups[i1].Rotation = Quaternion.Identity;
						if (i1 != 8)
						{
							TomePages.Groups[i1 + 1].Enabled = false;
						}
					});
				}
				TomePageIndex = 0;
			}
			IWaiter thisWaiter = null;
			thisWaiter = (tomeOpenWaiter = Waiters.Interpolate(TomeOpen ? 0.875f : 0.425f, delegate(float s)
			{
				if (tomeOpenWaiter == thisWaiter)
				{
					if (!TomeOpen)
					{
						s = 1f - s;
					}
					s = Easing.EaseOut(FezMath.Saturate(s), EasingType.Quadratic);
					DoTomeOpen(TomeCoverAo, s);
				}
			}, delegate
			{
				if (tomeOpenWaiter == thisWaiter)
				{
					DoTomeOpen(TomeCoverAo, TomeOpen ? 1 : 0);
					tomeOpenWaiter = null;
				}
			}));
		}
		else
		{
			if (TomeOpen || zooming || ((GameState.MenuCubeIsZoomed || InputManager.Jump != FezButtonState.Pressed) && (!GameState.MenuCubeIsZoomed || (InputManager.Back != FezButtonState.Pressed && InputManager.CancelTalk != FezButtonState.Pressed))))
			{
				return;
			}
			zoomed = !zoomed;
			if (zoomed)
			{
				GameService.CloseScroll(null);
				GameState.MenuCubeIsZoomed = true;
				zoomedFace = Face;
				OriginalDirection = CameraManager.OriginalDirection;
				Mesh antiCubes = AntiCubes;
				BlendingMode? blending = (GoldenCubes.Blending = BlendingMode.Alphablending);
				antiCubes.Blending = blending;
			}
			else
			{
				Mesh antiCubes2 = AntiCubes;
				BlendingMode? blending = (GoldenCubes.Blending = BlendingMode.Opaque);
				antiCubes2.Blending = blending;
				CameraManager.OriginalDirection = OriginalDirection;
				GameState.ShowScroll(Face.GetTitle(), 0f, onTop: true);
				TomeOpen = false;
			}
			int oid = (int)((double)HighlightPosition[zoomedFace].X + (double)HighlightPosition[zoomedFace].Y * Math.Sqrt(zoomedFace.GetCount()));
			zooming = true;
			switch (zoomedFace)
			{
			case MenuCubeFace.Artifacts:
			{
				int num3 = ArtifactAOs.Count;
				if (GameState.SaveData.Artifacts.Contains(ActorType.Tome))
				{
					num3--;
				}
				if (num3 <= oid)
				{
					zooming = false;
					zoomed = false;
					GameState.MenuCubeIsZoomed = false;
					return;
				}
				for (int m = 0; m <= oid; m++)
				{
					if (m != oid && ArtifactAOs[m].ArtObjectName == "TOME_BAO")
					{
						oid++;
					}
				}
				DoArtifactZoom(ArtifactAOs[oid]);
				if (ArtifactAOs[oid].ArtObjectName == "TOME_BAO")
				{
					DoArtifactZoom(ArtifactAOs[oid + 1]);
				}
				break;
			}
			case MenuCubeFace.Maps:
			{
				if (GameState.SaveData.Maps.Count <= oid)
				{
					zooming = false;
					zoomed = false;
					GameState.MenuCubeIsZoomed = false;
					return;
				}
				if (zoomed)
				{
					for (int j = 0; j < 6; j++)
					{
						Maps.Groups[oid * 6 + j].Material = new Material();
						originalMapPositions[j] = Maps.Groups[oid * 6 + j].Position;
					}
				}
				Vector2 vector = HighlightPosition[zoomedFace];
				Vector3 middleOffset = vector.X * (float)zoomedFace.GetSpacing() / 16f * zoomedFace.GetRight() + vector.Y * (float)zoomedFace.GetSpacing() / 16f * Vector3.Down + (Vector3.Down + zoomedFace.GetRight()) * zoomedFace.GetOffset() / 16f;
				middleOffset = AoInstance.ArtObject.Size / 2f * (zoomedFace.GetRight() + Vector3.Down) - middleOffset;
				Waiters.Interpolate(0.25, delegate(float s)
				{
					s = Easing.EaseOut(s, EasingType.Quadratic);
					if (!zoomed)
					{
						s = 1f - s;
					}
					Vector3 zero = Vector3.Zero;
					for (int k = 0; k < 6; k++)
					{
						Group group = Maps.Groups[oid * 6 + k];
						AoInstance.Material.Opacity = FezMath.Saturate(1f - s * 1.5f);
						AoInstance.MarkDirty();
						group.Position = Vector3.Lerp(originalMapPositions[k], originalMapPositions[k] + zoomedFace.GetForward() * 12f + middleOffset, s);
						zero += group.Position;
					}
					zero /= 6f;
					CameraManager.Center = Vector3.Lerp(AoInstance.Position, AoInstance.Position + Vector3.Transform(zero, AoInstance.Rotation), s);
					CameraManager.Radius = MathHelper.Lerp(OriginalRadius, OriginalRadius / 7f, Easing.EaseIn(s, EasingType.Cubic));
				}, delegate
				{
					if (!zoomed)
					{
						for (int l = 0; l < 6; l++)
						{
							Maps.Groups[oid * 6 + l].Material = AoInstance.Material;
						}
						GameState.MenuCubeIsZoomed = false;
					}
					zooming = false;
				});
				break;
			}
			}
			if (zoomed)
			{
				zoomInSound.Emit();
			}
			else
			{
				zoomOutSound.Emit();
			}
		}
	}

	private void DoTomeOpen(ArtObjectInstance ao, float s)
	{
		Vector3 vector = Vector3.Transform(zoomedFace.GetForward(), AoInstance.Rotation);
		Vector3 vector2 = Vector3.Transform(zoomedFace.GetRight(), AoInstance.Rotation);
		Vector3 vector3 = -vector * 13f / 16f + vector2 * 2f / 16f;
		(Matrix.CreateFromQuaternion(Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI) * AoInstance.Rotation) * Matrix.CreateTranslation(vector3) * Matrix.CreateFromAxisAngle(Vector3.UnitY, (float)Math.PI * -3f / 4f * s) * Matrix.CreateTranslation(AoInstance.Position + vector * 12f - vector3)).Decompose(out var _, out var rotation, out var translation);
		ao.Position = translation;
		ao.Rotation = rotation;
		TomePages.Position = translation;
	}

	private void DoArtifactZoom(ArtObjectInstance ao)
	{
		float num = (float)base.GraphicsDevice.Viewport.Width / (1280f * base.GraphicsDevice.GetViewScale());
		float OriginalRadius = 17.82f * num;
		if (zoomed)
		{
			ao.Material = new Material();
			originalObjectPosition = ao.Position;
			TomeZoom |= ao.ArtObjectName == "TOME_BAO";
			NumberZoom |= ao.ArtObjectName == "NUMBER_CUBEAO";
			LetterZoom |= ao.ArtObjectName == "LETTER_CUBEAO";
			ZoomedArtifact = ao;
		}
		else
		{
			NumberZoom = (LetterZoom = (TomeZoom = false));
			codeInputs.Clear();
			ZoomedArtifact = null;
		}
		Waiters.Interpolate(0.25, delegate(float s)
		{
			s = Easing.EaseOut(s, EasingType.Quadratic);
			if (!zoomed)
			{
				s = 1f - s;
			}
			Vector2 artifactOffset = ao.ArtObject.ActorType.GetArtifactOffset();
			AoInstance.Material.Opacity = FezMath.Saturate(1f - s * 1.5f);
			AoInstance.MarkDirty();
			ao.Position = Vector3.Lerp(originalObjectPosition, AoInstance.Position + Vector3.Transform(zoomedFace.GetForward(), AoInstance.Rotation) * 12f, s);
			CameraManager.Center = Vector3.Lerp(AoInstance.Position, ao.Position - Vector3.Transform(FezMath.XZMask * artifactOffset.X / 16f, AoInstance.Rotation) - Vector3.UnitY * artifactOffset.Y / 16f, s);
			CameraManager.Radius = MathHelper.Lerp(OriginalRadius, OriginalRadius / 4.25f, Easing.EaseIn(s, EasingType.Quadratic));
		}, delegate
		{
			if (!zoomed)
			{
				ao.Material = AoInstance.Material;
				GameState.MenuCubeIsZoomed = false;
			}
			zooming = false;
		});
	}

	public override void Draw(GameTime gameTime)
	{
		if (Instance == null)
		{
			return;
		}
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue);
		if (TargetRenderingManager.IsHooked(InRtHandle.Target) && !Resolved)
		{
			TargetRenderingManager.Resolve(InRtHandle.Target, reschedule: false);
			Resolved = true;
			StartInTransition();
		}
		if (ScheduleExit && Resolved)
		{
			graphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
			base.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
			TargetRenderingManager.DrawFullscreen(OutRtHandle.Target, new Color(1f, 1f, 1f, AoInstance.Material.Opacity));
			return;
		}
		AoVisibility.Clear();
		foreach (ArtObjectInstance levelArtObject in LevelMaterializer.LevelArtObjects)
		{
			AoVisibility.Add(levelArtObject.Visible);
			levelArtObject.Visible = false;
			levelArtObject.ArtObject.Group.Enabled = false;
		}
		graphicsDevice.SetBlendingMode(BlendingMode.Opaque);
		RenderTargetHandle renderTargetHandle = null;
		if (GameState.StereoMode)
		{
			renderTargetHandle = TargetRenderingManager.TakeTarget();
		}
		RenderTarget2D renderTarget = ((base.GraphicsDevice.GetRenderTargets().Length != 0) ? (base.GraphicsDevice.GetRenderTargets()[0].RenderTarget as RenderTarget2D) : (GameState.StereoMode ? renderTargetHandle.Target : null));
		BlurEffect.Pass = BlurPass.Horizontal;
		RenderTargetHandle renderTargetHandle2 = TargetRenderingManager.TakeTarget();
		base.GraphicsDevice.SetRenderTarget(renderTargetHandle2.Target);
		base.GraphicsDevice.Clear(Color.Black);
		base.GraphicsDevice.SetupViewport();
		base.GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
		TargetRenderingManager.DrawFullscreen(BlurEffect, InRtHandle.Target);
		BlurEffect.Pass = BlurPass.Vertical;
		base.GraphicsDevice.SetRenderTarget(renderTarget);
		base.GraphicsDevice.Clear(Color.Black);
		base.GraphicsDevice.SetupViewport();
		base.GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
		TargetRenderingManager.DrawFullscreen(BlurEffect, renderTargetHandle2.Target);
		TargetRenderingManager.ReturnTarget(renderTargetHandle2);
		if (GameState.StereoMode)
		{
			base.GraphicsDevice.SetRenderTarget(null);
			base.GraphicsDevice.Clear(Color.Black);
		}
		base.GraphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Black, 1f, 0);
		if (GameState.StereoMode)
		{
			GameLevelHost.Instance.DoStereo(CameraManager.Center, AoInstance.ArtObject.Size * 1.5f, DrawMenuCube, gameTime, renderTargetHandle.Target);
		}
		else
		{
			DrawMenuCube(gameTime);
		}
		if (GameState.StereoMode)
		{
			TargetRenderingManager.ReturnTarget(renderTargetHandle);
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
		if (TargetRenderingManager.IsHooked(OutRtHandle.Target) && !Resolved)
		{
			TargetRenderingManager.Resolve(OutRtHandle.Target, reschedule: false);
			base.GraphicsDevice.Clear(Color.Black);
			base.GraphicsDevice.SetupViewport();
			TargetRenderingManager.DrawFullscreen(OutRtHandle.Target);
			Resolved = true;
			StartOutTransition();
		}
		base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
	}

	private void DrawMenuCube(GameTime gameTime)
	{
		if (GameState.StereoMode)
		{
			LevelManager.ActualDiffuse = new Color(new Vector3(0.8f));
			LevelManager.ActualAmbient = new Color(new Vector3(0.2f));
		}
		RasterizerCombiner rasterCombiner = base.GraphicsDevice.GetRasterCombiner();
		bool flag = LevelManager.BaseDiffuse != 0f;
		foreach (ArtObjectInstance artifactAO in ArtifactAOs)
		{
			artifactAO.Visible = true;
			artifactAO.ArtObject.Group.Enabled = true;
			if (flag)
			{
				artifactAO.ForceShading = true;
			}
		}
		AoInstance.Visible = true;
		AoInstance.ArtObject.Group.Enabled = true;
		if (flag)
		{
			AoInstance.ForceShading = true;
		}
		LevelMaterializer.ArtObjectsMesh.Draw();
		if (flag)
		{
			AoInstance.ForceShading = false;
		}
		foreach (ArtObjectInstance artifactAO2 in ArtifactAOs)
		{
			if (flag)
			{
				artifactAO2.ForceShading = false;
			}
		}
		rasterCombiner.DepthBias = (CameraManager.Viewpoint.IsOrthographic() ? (-1E-06f) : (-0.001f / (CameraManager.FarPlane - CameraManager.NearPlane)));
		HidingPlanes.Draw();
		rasterCombiner.DepthBias = 0f;
		GoldenCubes.Draw();
		Highlights.Draw();
		Maps.Draw();
		rasterCombiner.DepthBias = (CameraManager.Viewpoint.IsOrthographic() ? (-1E-06f) : (-0.001f / (CameraManager.FarPlane - CameraManager.NearPlane)));
		if (TomeOpen || (tomeOpenWaiter != null && TomeZoom))
		{
			TomePages.Rotation = TomeBackAo.Rotation;
			TomePages.Position = TomeBackAo.Position + Vector3.Transform(zoomedFace.GetForward(), AoInstance.Rotation) * 13f / 16f;
			TomePages.Draw();
		}
		AntiCubes.Draw();
		rasterCombiner.DepthBias = 0f;
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
