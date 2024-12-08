using System;
using System.Collections.Generic;
using Common;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Components.Scripting;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class DotHost : DrawableGameComponent, IDotManager
{
	public enum BehaviourType
	{
		FollowGomez,
		ReadyToTalk,
		ClampToTarget,
		RoamInVolume,
		MoveToTargetWithCamera,
		WaitAtTarget,
		SpiralAroundWithCamera,
		ThoughtBubble
	}

	internal class RayState
	{
		public float Age;

		public readonly float Speed = RandomHelper.Between(0.10000000149011612, 1.5);

		public readonly Color Tint = Util.ColorFromHSV(RandomHelper.Between(0.0, 360.0), 1.0, 1.0);
	}

	private List<Vector4> Vertices = new List<Vector4>();

	private int[] FaceVertexIndices;

	private Mesh DotMesh;

	private Mesh RaysMesh;

	private Mesh FlareMesh;

	private IndexedUserPrimitives<FezVertexPositionColor> DotWireGeometry;

	private IndexedUserPrimitives<FezVertexPositionColor> DotFacesGeometry;

	private float Theta;

	private Quaternion CamRotationFollow = Quaternion.Identity;

	private Vector3 InterpolatedPosition;

	private Vector3 InterpolatedScale;

	private float EightShapeStep;

	private Vector3 ToBackFollow;

	private float SinceStartedTransition;

	private float SinceStartedCameraPan;

	private Vector3 PanOrigin;

	private BackgroundPlane HaloPlane;

	private bool BurrowAfterPan;

	private Vector3 SpiralingCenter;

	private Vector3 lastRelativePosition;

	private GlyphTextRenderer GTR;

	private SpriteBatch spriteBatch;

	private Mesh BPromptMesh;

	private Mesh VignetteMesh;

	private SoundEffect sHide;

	private SoundEffect sComeOut;

	private SoundEffect sIdle;

	private SoundEffect sMove;

	private SoundEffect sHeyListen;

	private SoundEmitter eHide;

	private SoundEmitter eIdle;

	private SoundEmitter eMove;

	private SoundEmitter eComeOut;

	private SoundEmitter eHey;

	private BehaviourType _behaviour;

	private BehaviourType lastBehaviour;

	private RenderTarget2D bTexture;

	public bool Burrowing { get; set; }

	public bool ComingOut { get; set; }

	public bool DrawRays { get; set; }

	public object Owner { get; set; }

	public DotFaceButton FaceButton { get; set; }

	public Texture2D DestinationVignette { get; set; }

	public Texture2D DestinationVignetteSony { get; set; }

	public bool Hidden
	{
		get
		{
			if (!base.Visible)
			{
				return !base.Enabled;
			}
			return false;
		}
		set
		{
			bool visible = (base.Enabled = !value);
			base.Visible = visible;
			if (!value)
			{
				Burrowing = false;
			}
			if (HaloPlane != null)
			{
				HaloPlane.Hidden = value;
			}
		}
	}

	public Vector3 Position => DotMesh.Position;

	public float RotationSpeed { get; set; }

	public float Opacity { get; set; }

	public BehaviourType Behaviour
	{
		get
		{
			return _behaviour;
		}
		set
		{
			if (_behaviour != value && lastBehaviour != value && value == BehaviourType.ThoughtBubble)
			{
				UpdateBTexture();
			}
			lastBehaviour = _behaviour;
			_behaviour = value;
		}
	}

	public Vector3 Target { get; set; }

	public string[] Dialog { get; set; }

	public float TimeToWait { get; set; }

	public Volume RoamingVolume { get; set; }

	public float ScaleFactor { get; set; }

	public float ScalePulsing { get; set; }

	public bool AlwaysShowLines { get; set; }

	public float InnerScale { get; set; }

	public bool PreventPoI { get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { get; set; }

	[ServiceDependency]
	public IFontManager Fonts { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	internal IScriptingManager Scripting { get; set; }

	public void Reset()
	{
		RotationSpeed = 1f;
		Opacity = 1f;
		Behaviour = BehaviourType.FollowGomez;
		Target = default(Vector3);
		Dialog = null;
		TimeToWait = 0f;
		ScaleFactor = 1f;
		ScalePulsing = 1f;
		AlwaysShowLines = false;
		InnerScale = 1f;
		EightShapeStep = 0f;
		Hidden = true;
		bool burrowing = (ComingOut = false);
		Burrowing = burrowing;
		RoamingVolume = null;
		PreventPoI = false;
		SinceStartedCameraPan = 0f;
		DrawRays = true;
		Owner = null;
		if (HaloPlane != null)
		{
			HaloPlane.Hidden = true;
		}
		KillSounds();
	}

	private void KillSounds()
	{
		if (eIdle != null && !eIdle.Dead)
		{
			eIdle.FadeOutAndDie(0.1f, autoPause: false);
		}
		if (eMove != null && !eMove.Dead)
		{
			eMove.FadeOutAndDie(0.1f, autoPause: false);
		}
		if (eHide != null && !eHide.Dead)
		{
			eHide.FadeOutAndDie(0.1f, autoPause: false);
		}
		if (eComeOut != null && !eComeOut.Dead)
		{
			eComeOut.FadeOutAndDie(0.1f, autoPause: false);
		}
		if (eHey != null && !eHey.Dead)
		{
			eHey.FadeOutAndDie(0.1f, autoPause: false);
		}
	}

	public void Burrow()
	{
		if (!Burrowing && !Hidden)
		{
			if (eHide != null && !eHide.Dead)
			{
				eHide.FadeOutAndDie(0.1f, autoPause: false);
			}
			eHide = sHide.EmitAt(Position);
			if (eIdle != null && !eIdle.Dead)
			{
				eIdle.FadeOutAndDie(1f, autoPause: false);
				eIdle = null;
			}
			if (eMove != null && !eMove.Dead)
			{
				eMove.FadeOutAndDie(1f, autoPause: false);
			}
			if (eComeOut != null && !eComeOut.Dead)
			{
				eComeOut.FadeOutAndDie(0.1f, autoPause: false);
			}
			if (eHey != null && !eHey.Dead)
			{
				eHey.FadeOutAndDie(0.1f, autoPause: false);
			}
			if (ComingOut)
			{
				SinceStartedTransition = 1f - FezMath.Saturate(SinceStartedTransition);
			}
			else
			{
				SinceStartedTransition = 0f;
			}
			ComingOut = false;
			Burrowing = true;
		}
	}

	public void Hey()
	{
		if (eHey != null && !eHey.Dead)
		{
			eHey.FadeOutAndDie(0.1f, autoPause: false);
		}
		eHey = sHeyListen.EmitAt(Position, RandomHelper.Centered(0.0325));
	}

	public void ComeOut()
	{
		ComeOut(mute: false);
	}

	private void ComeOut(bool mute)
	{
		if (ComingOut || (!Burrowing && !Hidden))
		{
			return;
		}
		if (Burrowing)
		{
			SinceStartedTransition = 1f - FezMath.Saturate(SinceStartedTransition);
		}
		else
		{
			Reset();
			InterpolatedPosition = PlayerManager.Position;
			SinceStartedTransition = 0f;
		}
		if (!mute)
		{
			if (eHide != null && !eHide.Dead)
			{
				eHide.FadeOutAndDie(0.1f, autoPause: false);
			}
			if (eComeOut != null && !eComeOut.Dead)
			{
				eComeOut.FadeOutAndDie(0.1f, autoPause: false);
			}
			if (!Burrowing)
			{
				eComeOut = sComeOut.EmitAt(Position);
			}
			eIdle = sIdle.EmitAt(Position, loop: true);
		}
		EightShapeStep = 0f;
		ComingOut = true;
		Burrowing = false;
		Hidden = false;
	}

	public void MoveWithCamera(Vector3 target, bool burrowAfter)
	{
		PanOrigin = (Hidden ? PlayerManager.Position : DotMesh.Position);
		ComeOut();
		SinceStartedCameraPan = 0f;
		Behaviour = BehaviourType.MoveToTargetWithCamera;
		CameraManager.Constrained = true;
		Target = target;
		BurrowAfterPan = burrowAfter;
		eMove = sMove.EmitAt(Position, loop: true, 0f, 0f);
	}

	public void SpiralAround(Volume volume, Vector3 center, bool hideDot)
	{
		PlayerManager.CanControl = false;
		ComeOut(hideDot);
		if (hideDot)
		{
			HaloPlane.Hidden = true;
			base.Visible = false;
		}
		volume.From = new Vector3(volume.From.X, Math.Max(volume.From.Y, PlayerManager.Position.Y + 4f / CameraManager.PixelsPerTrixel), volume.From.Z);
		PreventPoI = true;
		SinceStartedCameraPan = 0f;
		Behaviour = BehaviourType.SpiralAroundWithCamera;
		CameraManager.Constrained = true;
		RoamingVolume = volume;
		SpiralingCenter = center;
		InterpolatedScale = new Vector3(50f);
		Vector3 vector = (RoamingVolume.BoundingBox.Max - RoamingVolume.BoundingBox.Min).Abs();
		InterpolatedPosition = new Vector3(vector.X / 2f, vector.Y, vector.Z / 2f) + RoamingVolume.BoundingBox.Min;
		if (!hideDot)
		{
			eMove = sMove.EmitAt(Position, loop: true, 0f, 0f);
		}
		Update(new GameTime(), force: true);
		CameraManager.SnapInterpolation();
		LevelMaterializer.Rowify();
	}

	public void ForceDrawOrder(int drawOrder)
	{
		base.DrawOrder = drawOrder;
		OnDrawOrderChanged(this, EventArgs.Empty);
	}

	public void RevertDrawOrder()
	{
		base.DrawOrder = 900;
		OnDrawOrderChanged(this, EventArgs.Empty);
	}

	public DotHost(Game game)
		: base(game)
	{
		base.DrawOrder = 900;
		Reset();
	}

	public override void Initialize()
	{
		base.Initialize();
		GTR = new GlyphTextRenderer(base.Game);
		spriteBatch = new SpriteBatch(base.GraphicsDevice);
		Scripting.CutsceneSkipped += OnCutsceneSkipped;
		Vertices = new List<Vector4>
		{
			new Vector4(-1f, -1f, -1f, -1f),
			new Vector4(1f, -1f, -1f, -1f),
			new Vector4(-1f, 1f, -1f, -1f),
			new Vector4(1f, 1f, -1f, -1f),
			new Vector4(-1f, -1f, 1f, -1f),
			new Vector4(1f, -1f, 1f, -1f),
			new Vector4(-1f, 1f, 1f, -1f),
			new Vector4(1f, 1f, 1f, -1f),
			new Vector4(-1f, -1f, -1f, 1f),
			new Vector4(1f, -1f, -1f, 1f),
			new Vector4(-1f, 1f, -1f, 1f),
			new Vector4(1f, 1f, -1f, 1f),
			new Vector4(-1f, -1f, 1f, 1f),
			new Vector4(1f, -1f, 1f, 1f),
			new Vector4(-1f, 1f, 1f, 1f),
			new Vector4(1f, 1f, 1f, 1f)
		};
		DotMesh = new Mesh
		{
			Effect = new DotEffect(),
			Blending = BlendingMode.Additive,
			DepthWrites = false,
			Culling = CullMode.None,
			AlwaysOnTop = true,
			Material = 
			{
				Opacity = 1f / 3f
			}
		};
		RaysMesh = new Mesh
		{
			Effect = new DefaultEffect.Textured(),
			Texture = CMProvider.Global.Load<Texture2D>("Other Textures/smooth_ray"),
			Blending = BlendingMode.Additive,
			SamplerState = SamplerState.AnisotropicClamp,
			DepthWrites = false,
			AlwaysOnTop = true
		};
		FlareMesh = new Mesh
		{
			Effect = new DefaultEffect.Textured(),
			Texture = CMProvider.Global.Load<Texture2D>("Other Textures/rainbow_flare"),
			Blending = BlendingMode.Additive,
			SamplerState = SamplerState.AnisotropicClamp,
			DepthWrites = false,
			AlwaysOnTop = true
		};
		VignetteMesh = new Mesh
		{
			Effect = new DefaultEffect.Textured
			{
				IgnoreCache = true
			},
			Blending = BlendingMode.Alphablending,
			SamplerState = SamplerStates.PointMipClamp,
			DepthWrites = false,
			AlwaysOnTop = true
		};
		VignetteMesh.AddFace(new Vector3(1f), Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true);
		BPromptMesh = new Mesh
		{
			AlwaysOnTop = true,
			SamplerState = SamplerState.PointClamp,
			Blending = BlendingMode.Alphablending,
			Effect = new DefaultEffect.Textured()
		};
		BPromptMesh.AddFace(new Vector3(1f, 1f, 0f), Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: false);
		FlareMesh.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true);
		DotMesh.AddGroup().Geometry = (DotWireGeometry = new IndexedUserPrimitives<FezVertexPositionColor>(PrimitiveType.LineList));
		DotMesh.AddGroup().Geometry = (DotFacesGeometry = new IndexedUserPrimitives<FezVertexPositionColor>(PrimitiveType.TriangleList));
		DotWireGeometry.Vertices = new FezVertexPositionColor[16];
		for (int i = 0; i < 16; i++)
		{
			DotWireGeometry.Vertices[i].Color = new Color(1f, 1f, 1f, 1f);
		}
		DotWireGeometry.Indices = new int[64]
		{
			0, 1, 0, 2, 2, 3, 3, 1, 4, 5,
			6, 7, 4, 6, 5, 7, 4, 0, 6, 2,
			3, 7, 1, 5, 10, 11, 8, 9, 8, 10,
			9, 11, 12, 14, 14, 15, 15, 13, 12, 13,
			12, 8, 14, 10, 15, 11, 13, 9, 2, 10,
			3, 11, 0, 8, 1, 9, 6, 14, 7, 15,
			4, 12, 5, 13
		};
		DotFacesGeometry.Vertices = new FezVertexPositionColor[96];
		for (int j = 0; j < 4; j++)
		{
			for (int k = 0; k < 6; k++)
			{
				Vector3 vector = Vector3.Zero;
				switch ((k + j * 6) % 6)
				{
				case 0:
					vector = new Vector3(0f, 1f, 0.75f);
					break;
				case 1:
					vector = new Vector3(1f / 6f, 1f, 0.75f);
					break;
				case 2:
					vector = new Vector3(1f / 3f, 1f, 0.75f);
					break;
				case 3:
					vector = new Vector3(0.5f, 1f, 0.75f);
					break;
				case 4:
					vector = new Vector3(2f / 3f, 1f, 0.75f);
					break;
				case 5:
					vector = new Vector3(5f / 6f, 1f, 0.75f);
					break;
				}
				for (int l = 0; l < 4; l++)
				{
					int num = l + k * 4 + j * 24;
					DotFacesGeometry.Vertices[num].Color = new Color(vector.X, vector.Y, vector.Z);
				}
			}
		}
		FaceVertexIndices = new int[96]
		{
			0, 2, 3, 1, 1, 3, 7, 5, 5, 7,
			6, 4, 4, 6, 2, 0, 0, 4, 5, 1,
			2, 6, 7, 3, 8, 10, 11, 9, 9, 11,
			15, 13, 13, 15, 14, 12, 12, 14, 10, 8,
			8, 12, 13, 9, 10, 14, 15, 11, 0, 1,
			9, 8, 0, 2, 10, 8, 2, 3, 11, 10,
			3, 1, 9, 11, 4, 5, 13, 12, 6, 7,
			15, 14, 4, 6, 14, 12, 5, 7, 15, 13,
			4, 0, 8, 12, 6, 2, 10, 14, 3, 7,
			15, 11, 1, 5, 13, 9
		};
		DotFacesGeometry.Indices = new int[144]
		{
			0, 2, 1, 0, 3, 2, 4, 6, 5, 4,
			7, 6, 8, 10, 9, 8, 11, 10, 12, 14,
			13, 12, 15, 14, 16, 17, 18, 16, 18, 19,
			20, 22, 21, 20, 23, 22, 24, 26, 25, 24,
			27, 26, 28, 30, 29, 28, 31, 30, 32, 34,
			33, 32, 35, 34, 36, 38, 37, 36, 39, 38,
			40, 41, 42, 40, 42, 43, 44, 46, 45, 44,
			47, 46, 48, 50, 49, 48, 51, 50, 52, 54,
			53, 52, 55, 54, 56, 58, 57, 56, 59, 58,
			60, 62, 61, 60, 63, 62, 64, 65, 66, 64,
			66, 67, 68, 70, 69, 68, 71, 70, 72, 74,
			73, 72, 75, 74, 76, 78, 77, 76, 79, 78,
			80, 82, 81, 80, 83, 82, 84, 86, 85, 84,
			87, 86, 88, 89, 90, 88, 90, 91, 92, 94,
			93, 92, 95, 94
		};
		sHide = CMProvider.Global.Load<SoundEffect>("Sounds/Dot/Hide");
		sComeOut = CMProvider.Global.Load<SoundEffect>("Sounds/Dot/ComeOut");
		sMove = CMProvider.Global.Load<SoundEffect>("Sounds/Dot/Move");
		sIdle = CMProvider.Global.Load<SoundEffect>("Sounds/Dot/Idle");
		sHeyListen = CMProvider.Global.Load<SoundEffect>("Sounds/Dot/HeyListen");
		LevelManager.LevelChanged += RebuildFlare;
		GamepadState.OnLayoutChanged = (EventHandler)Delegate.Combine(GamepadState.OnLayoutChanged, (EventHandler)delegate
		{
			if (Behaviour == BehaviourType.ThoughtBubble)
			{
				UpdateBTexture();
			}
		});
	}

	private void UpdateBTexture()
	{
		SpriteFont small = Fonts.Small;
		Vector2 vector = small.MeasureString(GTR.FillInGlyphs(" {B} ")) * FezMath.Saturate(Fonts.SmallFactor);
		if (bTexture != null)
		{
			bTexture.Dispose();
		}
		bTexture = new RenderTarget2D(base.GraphicsDevice, (int)vector.X, (int)vector.Y, mipMap: false, base.GraphicsDevice.PresentationParameters.BackBufferFormat, base.GraphicsDevice.PresentationParameters.DepthStencilFormat, 0, RenderTargetUsage.PreserveContents);
		base.GraphicsDevice.SetRenderTarget(bTexture);
		base.GraphicsDevice.PrepareDraw();
		base.GraphicsDevice.Clear(ClearOptions.Target, ColorEx.TransparentWhite, 1f, 0);
		spriteBatch.BeginPoint();
		GTR.DrawString(spriteBatch, small, " {B} ", new Vector2(0f, 0f), Color.White, FezMath.Saturate(Fonts.SmallFactor));
		spriteBatch.End();
		base.GraphicsDevice.SetRenderTarget(null);
		BPromptMesh.Texture = bTexture;
		BPromptMesh.Scale = new Vector3(vector.X / 32f, vector.Y / 32f, 1f);
		if (Culture.IsCJK)
		{
			BPromptMesh.Scale *= 0.75f;
		}
	}

	private void OnCutsceneSkipped()
	{
		if (Behaviour == BehaviourType.SpiralAroundWithCamera)
		{
			EndSpiral();
		}
	}

	private void RebuildFlare()
	{
		lock (LevelManager.BackgroundPlanes)
		{
			if (!LevelManager.BackgroundPlanes.ContainsKey(-2))
			{
				string textureName = LevelManager.GomezHaloName ?? "flare";
				LevelManager.BackgroundPlanes.Add(-2, HaloPlane = new BackgroundPlane(LevelMaterializer.StaticPlanesMesh, textureName, animated: false)
				{
					Id = -2,
					LightMap = true,
					AlwaysOnTop = true,
					Billboard = true,
					Hidden = false,
					Filter = (LevelManager.HaloFiltering ? new Color(0.425f, 0.425f, 0.425f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f)),
					PixelatedLightmap = !LevelManager.HaloFiltering
				});
			}
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (!base.Visible)
		{
			Update(gameTime, force: false);
		}
	}

	private void Update(GameTime gameTime, bool force)
	{
		if (!force && (GameState.Paused || GameState.Loading || GameState.InMenuCube || GameState.InFpsMode || (GameState.TimePaused && !GameState.InMap)))
		{
			return;
		}
		if (Fez.LongScreenshot)
		{
			HaloPlane.Hidden = true;
		}
		float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
		if (base.Visible)
		{
			if (RotationSpeed == 0f)
			{
				Theta = 0f;
			}
			Theta += (float)gameTime.ElapsedGameTime.TotalSeconds * RotationSpeed;
			float num = (float)Math.Cos(Theta);
			float num2 = (float)Math.Sin(Theta);
			Matrix matrix = new Matrix(num, 0f, 0f, num2, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f - num2, 0f, 0f, num);
			for (int i = 0; i < Vertices.Count; i++)
			{
				Vector4 vector = Vector4.Transform(Vertices[i], matrix);
				float num3 = ((vector.W + 1f) / 3f * InnerScale + 0.5f) * (1f / 3f);
				DotWireGeometry.Vertices[i].Position = new Vector3(vector.X, vector.Y, vector.Z) * num3;
			}
			for (int j = 0; j < FaceVertexIndices.Length; j++)
			{
				DotFacesGeometry.Vertices[j].Position = DotWireGeometry.Vertices[FaceVertexIndices[j]].Position;
			}
			CamRotationFollow = Quaternion.Slerp(CamRotationFollow, CameraManager.Rotation, FezMath.GetReachFactor(0.05f, dt));
			float num4 = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds / 3.0) * 0.5f + 1f;
			EightShapeStep += (float)gameTime.ElapsedGameTime.TotalSeconds * num4;
			ToBackFollow = Vector3.Lerp(ToBackFollow, ((PlayerManager.Action != ActionType.RunTurnAround) ? 1 : (-1)) * PlayerManager.LookingDirection.Sign() * CameraManager.Viewpoint.RightVector() * 1.5f, FezMath.GetReachFactor(0.05f, dt));
		}
		Vector3 vector2 = Vector3.Zero;
		switch (Behaviour)
		{
		case BehaviourType.SpiralAroundWithCamera:
		{
			Vector3 vector6 = (RoamingVolume.BoundingBox.Max - RoamingVolume.BoundingBox.Min).Abs();
			float num8 = (RoamingVolume.BoundingBox.Max.Y - PlayerManager.Position.Y) * 0.9f;
			if (num8 == 0f)
			{
				num8 = 1f;
			}
			bool flag2 = SinceStartedCameraPan == 0f;
			SinceStartedCameraPan += (float)gameTime.ElapsedGameTime.TotalSeconds / num8 * 2f;
			float num9 = Easing.EaseOut(FezMath.Saturate(SinceStartedCameraPan), EasingType.Sine);
			double num10 = Math.Round(num8 / 20f) * 6.2831854820251465;
			int distance = CameraManager.Viewpoint.GetDistance(Viewpoint.Front);
			vector2 = new Vector3((float)Math.Sin((double)Easing.EaseIn(num9, EasingType.Sine) * num10 - (double)((float)distance * ((float)Math.PI / 2f))) * vector6.X / 2f + vector6.X / 2f, vector6.Y * (1f - num9), (float)Math.Cos((double)Easing.EaseIn(num9, EasingType.Sine) * num10 - (double)((float)distance * ((float)Math.PI / 2f))) * vector6.Z / 2f + vector6.Z / 2f) + RoamingVolume.BoundingBox.Min;
			Target = new Vector3(SpiralingCenter.X, vector2.Y, SpiralingCenter.Z);
			Vector3 direction = Vector3.Normalize(new Vector3(vector2.X, 0f, vector2.Z) - (RoamingVolume.BoundingBox.Min + vector6 / 2f) * FezMath.XZMask);
			if ((double)num9 > 0.75)
			{
				float num11 = Easing.EaseInOut((num9 - 0.75f) / 0.25f, EasingType.Sine);
				Target = Vector3.Lerp(Target, PlayerManager.Position + Vector3.Up * 4f / CameraManager.PixelsPerTrixel, num11);
				vector2 = Target + CameraManager.Viewpoint.RightVector() * num11;
			}
			CameraManager.Center = Target;
			CameraManager.Direction = direction;
			if ((double)num9 < 0.1)
			{
				Target = new Vector3(vector6.X / 2f, vector6.Y * (1f - num9), vector6.Z / 2f) + RoamingVolume.BoundingBox.Min;
				vector2 = Target;
			}
			if ((double)num9 < 0.75)
			{
				vector2 += CameraManager.InverseView.Right * 5f;
			}
			if (eMove != null && !eMove.Dead)
			{
				Vector3 vector7 = vector2 - CameraManager.InterpolatedCenter;
				float num12 = ((vector7 - lastRelativePosition) * (CameraManager.InverseView.Right + Vector3.UnitY)).Length();
				if (!flag2)
				{
					eMove.VolumeFactor = MathHelper.Lerp(eMove.VolumeFactor, FezMath.Saturate(num12 * 3f) * 0.75f + 0.25f, FezMath.GetReachFactor(0.1f, dt));
				}
				lastRelativePosition = vector7;
			}
			if (num9 >= 1f)
			{
				EndSpiral();
			}
			break;
		}
		case BehaviourType.MoveToTargetWithCamera:
		{
			float num5 = Vector3.Distance(PanOrigin, Target);
			if (num5 == 0f)
			{
				num5 = 1f;
			}
			int num6 = ((!BurrowAfterPan) ? 1 : 2);
			bool flag = SinceStartedCameraPan == 0f;
			SinceStartedCameraPan += (float)gameTime.ElapsedGameTime.TotalSeconds / (num5 / 5f) * (float)num6;
			SinceStartedCameraPan = FezMath.Saturate(SinceStartedCameraPan);
			Vector3 vector4 = Vector3.Lerp(PanOrigin, Target, Easing.EaseInOut(SinceStartedCameraPan, EasingType.Sine));
			if (BurrowAfterPan)
			{
				CameraManager.Center = vector4;
			}
			else
			{
				CameraManager.Center = Vector3.Lerp(CameraManager.Center, vector4, FezMath.GetReachFactor(0.05f, dt));
			}
			if (SinceStartedCameraPan >= 1f)
			{
				EndMoveTo();
			}
			vector2 = vector4 + (float)Math.Sin(EightShapeStep * 2f) * Vector3.UnitY / 2f + (float)Math.Cos(EightShapeStep) * CameraManager.View.Right / 2f;
			if (eMove != null && !eMove.Dead)
			{
				Vector3 vector5 = vector2;
				float num7 = ((vector5 - lastRelativePosition) * (CameraManager.InverseView.Right + Vector3.UnitY)).Length();
				if (!flag)
				{
					eMove.VolumeFactor = MathHelper.Lerp(eMove.VolumeFactor, FezMath.Saturate(num7 * 10f) * 0.75f + 0.25f, FezMath.GetReachFactor(0.1f, dt));
				}
				lastRelativePosition = vector5;
			}
			break;
		}
		case BehaviourType.WaitAtTarget:
			vector2 = Target + (float)Math.Sin(EightShapeStep * 2f) * Vector3.UnitY / 3f + (float)Math.Cos(EightShapeStep) * CameraManager.View.Right / 3f;
			CameraManager.Center = Vector3.Lerp(CameraManager.Center, Target, FezMath.GetReachFactor(0.075f, dt));
			break;
		case BehaviourType.ClampToTarget:
		{
			Vector3 interpolatedPosition = (DotMesh.Position = Target);
			vector2 = (InterpolatedPosition = interpolatedPosition);
			break;
		}
		case BehaviourType.ReadyToTalk:
			vector2 = PlayerManager.Position + PlayerManager.Size * Vector3.UnitY * 0.75f + Vector3.UnitY * 0.5f + (float)Math.Sin(EightShapeStep * 2f) * Vector3.UnitY * 0.1f + (float)Math.Cos(EightShapeStep) * CameraManager.View.Right * 0.1f + ToBackFollow;
			break;
		case BehaviourType.ThoughtBubble:
			vector2 = PlayerManager.Position + PlayerManager.Size * Vector3.UnitY * 0.75f + Vector3.UnitY * 0.5f + (float)Math.Sin(EightShapeStep * 2f) * Vector3.UnitY * 0.1f + (float)Math.Cos(EightShapeStep) * CameraManager.View.Right * 0.1f + ToBackFollow * 1.25f;
			break;
		case BehaviourType.FollowGomez:
			vector2 = PlayerManager.Position + PlayerManager.Size * Vector3.UnitY * 0.5f + Vector3.UnitY * 1.5f + (float)Math.Sin(EightShapeStep * 2f) * Vector3.UnitY * 0.5f + (float)Math.Cos(EightShapeStep) * CameraManager.View.Right - ToBackFollow;
			break;
		case BehaviourType.RoamInVolume:
		{
			Vector3 vector3 = (RoamingVolume.BoundingBox.Max - RoamingVolume.BoundingBox.Min).Abs() / 2f + Vector3.One;
			vector2 = RoamingVolume.From + vector3 + vector3 * ((float)Math.Sin(EightShapeStep * 3f / vector3.Y) * Vector3.UnitY + (float)Math.Cos(EightShapeStep * 1.5f / ((vector3.X + vector3.Z) / 3.142857f)) * CameraManager.View.Right);
			break;
		}
		}
		if (Burrowing || ComingOut)
		{
			float num13 = Vector3.Distance(PlayerManager.Position, vector2);
			if (num13 == 0f)
			{
				num13 = 1f;
			}
			SinceStartedTransition += (float)gameTime.ElapsedGameTime.TotalSeconds / (num13 / 20f);
		}
		Vector3 vector8 = ((Behaviour != BehaviourType.ThoughtBubble) ? new Vector3(MathHelper.Lerp(ScaleFactor * 0.75f, ScaleFactor * 0.75f + (float)Math.Sin(EightShapeStep * 4f / 3f) * 0.2f * (ScaleFactor + 1f) / 2f, ScalePulsing)) : ((DestinationVignette != null) ? new Vector3(2f) : ((FaceButton != 0) ? new Vector3(0.825f * ScaleFactor) : new Vector3(1f * ScaleFactor))));
		if (base.Visible)
		{
			DotMesh.Rotation = CamRotationFollow * Quaternion.CreateFromAxisAngle(Vector3.Right, (float)Math.Asin(Math.Sqrt(2.0) / Math.Sqrt(3.0))) * Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 4f);
		}
		float num14 = Easing.EaseInOut(FezMath.Saturate(SinceStartedTransition), EasingType.Linear);
		if (Burrowing)
		{
			vector2 = Vector3.Lerp(vector2, PlayerManager.Position, num14);
			vector8 = Vector3.Lerp(vector8, Vector3.Zero, num14);
			if (eIdle != null)
			{
				eIdle.VolumeFactor = num14;
			}
			if ((double)InterpolatedScale.X <= 0.01)
			{
				if (eIdle != null && eIdle.Cue != null && !eIdle.Cue.IsDisposed)
				{
					eIdle.Cue.Stop();
				}
				eIdle = null;
				SinceStartedTransition = 0f;
				Reset();
				Burrowing = false;
			}
		}
		else if (ComingOut && Behaviour != BehaviourType.SpiralAroundWithCamera)
		{
			vector2 = Vector3.Lerp(vector2, PlayerManager.Position, 1f - num14);
			vector8 = Vector3.Lerp(vector8, Vector3.Zero, 1f - num14);
			if (eIdle != null)
			{
				eIdle.VolumeFactor = num14;
			}
			if (num14 >= 1f)
			{
				ComingOut = false;
				SinceStartedTransition = 0f;
			}
		}
		float oldFactor = ((Behaviour == BehaviourType.ThoughtBubble) ? 0.2f : 0.05f);
		InterpolatedPosition = Vector3.Lerp(InterpolatedPosition, vector2, FezMath.GetReachFactor(oldFactor, dt));
		oldFactor = ((Behaviour == BehaviourType.ThoughtBubble) ? 0.1f : ((lastBehaviour == BehaviourType.ThoughtBubble) ? 0.075f : 0.05f));
		InterpolatedScale = Vector3.Lerp(InterpolatedScale, vector8, FezMath.GetReachFactor(oldFactor, dt));
		if (base.Visible)
		{
			DotMesh.Position = InterpolatedPosition;
			DotMesh.Scale = InterpolatedScale;
		}
		float viewScale = base.GraphicsDevice.GetViewScale();
		float num15 = (float)base.GraphicsDevice.Viewport.Width / (1280f * viewScale);
		if (GameState.InMap)
		{
			DotMesh.Scale *= CameraManager.Radius / 16f / viewScale / num15;
			float num16 = CameraManager.Radius / 6f / viewScale / num15;
			DotMesh.Position = CameraManager.Center + CameraManager.InverseView.Left * num16 * CameraManager.AspectRatio - new Vector3(0f, num16, 0f);
		}
		if (Behaviour == BehaviourType.ThoughtBubble)
		{
			VignetteMesh.Position = DotMesh.Position;
			VignetteMesh.Rotation = CameraManager.Rotation;
			if (DestinationVignette != null)
			{
				VignetteMesh.Scale = new Vector3(2.65f);
				VignetteMesh.SamplerState = SamplerState.PointClamp;
				if (DestinationVignetteSony != null && GamepadState.Layout != 0)
				{
					VignetteMesh.Texture = DestinationVignetteSony;
				}
				else
				{
					VignetteMesh.Texture = DestinationVignette;
				}
				VignetteMesh.TextureMatrix.Set(Matrix.Identity);
			}
			else if (FaceButton == DotFaceButton.B)
			{
				BPromptMesh.Position = DotMesh.Position + CameraManager.Viewpoint.RightVector() * 0.25f + new Vector3(0f, -0.925f, 0f);
				BPromptMesh.Rotation = CameraManager.Rotation;
			}
		}
		if (eMove != null && !eMove.Dead)
		{
			eMove.Position = Position;
		}
		if (eIdle != null && !eIdle.Dead)
		{
			eIdle.Position = Position;
		}
		if (eComeOut != null && !eComeOut.Dead)
		{
			eComeOut.Position = Position;
		}
		if (eHide != null && !eHide.Dead)
		{
			eHide.Position = Position;
		}
		if (eHey != null && !eHey.Dead)
		{
			eHey.Position = Position;
		}
		if (DrawRays && base.Visible)
		{
			UpdateRays((float)gameTime.ElapsedGameTime.TotalSeconds);
		}
	}

	private void EndSpiral()
	{
		Vector3 target = PlayerManager.Position + Vector3.Up * 4f / CameraManager.PixelsPerTrixel + CameraManager.Viewpoint.RightVector();
		PlayerManager.CanControl = true;
		if (eMove != null && !eMove.Dead)
		{
			eMove.Cue.Stop();
			eMove = null;
		}
		RoamingVolume = null;
		if (!base.Visible)
		{
			Hidden = true;
		}
		Target = target;
		Behaviour = BehaviourType.ReadyToTalk;
		CameraManager.Direction = -CameraManager.Viewpoint.ForwardVector().MaxClampXZ();
		LevelMaterializer.CullInstances();
		CameraManager.Constrained = false;
		LevelMaterializer.UnRowify();
	}

	private void EndMoveTo()
	{
		eMove.FadeOutAndDie(2f, autoPause: false);
		eMove = null;
		if (BurrowAfterPan)
		{
			Burrow();
		}
		Behaviour = BehaviourType.WaitAtTarget;
	}

	private void UpdateRays(float elapsedSeconds)
	{
		if (RandomHelper.Probability(1.764705882352941 * (double)elapsedSeconds))
		{
			float num = 6f + RandomHelper.Centered(4.0);
			float num2 = RandomHelper.Between(0.5, num / 2.5f);
			Group group = RaysMesh.AddGroup();
			group.Geometry = new IndexedUserPrimitives<FezVertexPositionTexture>(new FezVertexPositionTexture[6]
			{
				new FezVertexPositionTexture(new Vector3(0f, num2 / 2f * 0.1f, 0f), new Vector2(0f, 0f)),
				new FezVertexPositionTexture(new Vector3(num, num2 / 2f, 0f), new Vector2(1f, 0f)),
				new FezVertexPositionTexture(new Vector3(num, num2 / 2f * 0.1f, 0f), new Vector2(1f, 0.45f)),
				new FezVertexPositionTexture(new Vector3(num, (0f - num2) / 2f * 0.1f, 0f), new Vector2(1f, 0.55f)),
				new FezVertexPositionTexture(new Vector3(num, (0f - num2) / 2f, 0f), new Vector2(1f, 1f)),
				new FezVertexPositionTexture(new Vector3(0f, (0f - num2) / 2f * 0.1f, 0f), new Vector2(0f, 1f))
			}, new int[12]
			{
				0, 1, 2, 0, 2, 5, 5, 2, 3, 5,
				3, 4
			}, PrimitiveType.TriangleList);
			group.CustomData = new RayState();
			group.Material = new Material
			{
				Diffuse = new Vector3(0f)
			};
			group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Forward, RandomHelper.Between(0.0, 6.2831854820251465));
		}
		bool flag = CameraManager.ViewTransitionStep != 0f && CameraManager.Viewpoint.IsOrthographic() && CameraManager.LastViewpoint.IsOrthographic();
		float num3 = Easing.EaseOut(1f - CameraManager.ViewTransitionStep, EasingType.Quadratic) * (float)CameraManager.Viewpoint.GetDistance(CameraManager.LastViewpoint);
		for (int num4 = RaysMesh.Groups.Count - 1; num4 >= 0; num4--)
		{
			Group group2 = RaysMesh.Groups[num4];
			RayState rayState = group2.CustomData as RayState;
			rayState.Age += elapsedSeconds * 0.15f;
			float num5 = (float)Math.Sin(rayState.Age * ((float)Math.PI * 2f) - (float)Math.PI / 2f) * 0.5f + 0.5f;
			num5 = Easing.EaseOut(num5, EasingType.Quadratic);
			group2.Material.Diffuse = new Vector3(num5 * 0.0375f) + rayState.Tint.ToVector3() * 0.075f * num5;
			float num6 = rayState.Speed;
			if (flag)
			{
				num6 *= 1f + 10f * num3;
			}
			group2.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.Forward, elapsedSeconds * num6 * 0.3f);
			group2.Scale = new Vector3(num5 * 0.75f + 0.25f, num5 * 0.5f + 0.5f, 1f);
			if (rayState.Age > 1f)
			{
				RaysMesh.RemoveGroupAt(num4);
			}
		}
		Mesh flareMesh = FlareMesh;
		Vector3 position2 = (RaysMesh.Position = DotMesh.Position);
		flareMesh.Position = position2;
		Mesh flareMesh2 = FlareMesh;
		Quaternion rotation2 = (RaysMesh.Rotation = CameraManager.Rotation);
		flareMesh2.Rotation = rotation2;
		RaysMesh.Scale = DotMesh.Scale * 0.5f;
		float num7 = MathHelper.Lerp(DotMesh.Scale.X, 1f, 0.325f);
		FlareMesh.Scale = new Vector3(MathHelper.Lerp(num7, (float)Math.Pow(num7 * 2f, 1.5), Opacity));
		num7 = MathHelper.Lerp(DotMesh.Scale.X, 0.75f, 0.75f);
		FlareMesh.Material.Diffuse = new Vector3(0.25f * num7 * FezMath.Saturate(Opacity * 2f));
		HaloPlane.Position = DotMesh.Position;
		if (!base.Visible)
		{
			HaloPlane.Scale = new Vector3(0f);
		}
		else
		{
			HaloPlane.Scale = DotMesh.Scale * 2f;
		}
		if (!LevelManager.HaloFiltering)
		{
			HaloPlane.Position = (HaloPlane.Position * 16f).Round() / 16f;
			HaloPlane.Scale = Vector3.Clamp(HaloPlane.Scale, Vector3.Zero, Vector3.One);
		}
	}

	public override void Draw(GameTime gameTime)
	{
		Update(gameTime, force: false);
		if (Fez.LongScreenshot && eIdle != null)
		{
			eIdle.VolumeFactor = 0f;
		}
		if (GameState.Loading || GameState.InMenuCube || Fez.LongScreenshot)
		{
			return;
		}
		bool num = Behaviour == BehaviourType.ThoughtBubble;
		FlareMesh.Draw();
		if (Opacity == 1f && DrawRays)
		{
			RaysMesh.Draw();
		}
		base.GraphicsDevice.PrepareStencilWrite(StencilMask.Dot);
		(DotMesh.Effect as DotEffect).UpdateHueOffset(gameTime.ElapsedGameTime);
		DotMesh.Blending = BlendingMode.Alphablending;
		DotMesh.Material.Diffuse = new Vector3(0f);
		if (num && (FaceButton == DotFaceButton.Up || DestinationVignette != null))
		{
			DotMesh.Material.Opacity = 1f;
			DotMesh.Draw();
		}
		else
		{
			DotMesh.Material.Opacity = (((double)Opacity > 0.5) ? (Opacity * 0.25f) : 0f);
			DotMesh.Draw();
		}
		if (num && !GameState.InMap)
		{
			if (DestinationVignette != null)
			{
				base.GraphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.Dot);
				VignetteMesh.Draw();
			}
			else if (FaceButton == DotFaceButton.B)
			{
				base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
				BPromptMesh.Draw();
				BPromptMesh.Material.Opacity += (float)gameTime.ElapsedGameTime.TotalSeconds * 3f;
				if (BPromptMesh.Material.Opacity > 1f)
				{
					BPromptMesh.Material.Opacity = 1f;
				}
			}
			else if (FaceButton == DotFaceButton.Up)
			{
				base.GraphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.Dot);
				VignetteMesh.Texture = GTR.GetReplacedGlyphTexture("{UP}");
				VignetteMesh.Scale = new Vector3(0.75f);
				VignetteMesh.Draw();
			}
		}
		else
		{
			BPromptMesh.Material.Opacity = -1.5f;
		}
		if (!num || (FaceButton != 0 && DestinationVignette == null))
		{
			DotMesh.Groups[0].Enabled = true;
			DotMesh.Groups[1].Enabled = false;
			DotMesh.Blending = BlendingMode.Additive;
			float num2 = (float)Math.Pow(Math.Sin(gameTime.TotalGameTime.TotalSeconds * 2.0) * 0.5 + 0.5, 3.0);
			DotMesh.Material.Opacity = 1f;
			DotMesh.Material.Diffuse = new Vector3(AlwaysShowLines ? Opacity : (num2 * 0.5f * Opacity));
			DotMesh.Draw();
			DotMesh.Groups[0].Enabled = false;
			DotMesh.Groups[1].Enabled = true;
			DotMesh.Material.Diffuse = new Vector3(Opacity);
			DotMesh.Draw();
		}
		base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
	}
}
